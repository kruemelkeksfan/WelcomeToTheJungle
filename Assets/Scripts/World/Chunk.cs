using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
	[SerializeField] private Transform chunkCenter = null;
	[SerializeField] private float chunkSize = 50.0f;
	[SerializeField] private float bushSize = 2.0f;
	[SerializeField] private float treeSize = 20.0f;
	[SerializeField] private float bushChance = 0.8f;
	[SerializeField] private float treeChance = 0.8f;
	[SerializeField] private GameObject[] bushPrefabs = null;
	[SerializeField] private GameObject[] treePrefabs = null;
	[SerializeField] private float renderDistance = 1000.0f;
	[SerializeField] private float renderDistanceUpdateInterval = 2.0f;
	private Vector2[] directions = { new Vector2(-1.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, -1.0f), new Vector2(-1.0f, -1.0f) };
	private GameObject chunkMesh = null;
	private List<GameObject> trees = null;
	private List<GameObject> bushes = null;
	private Transform[] players = null;

	private void Start()
	{
		chunkMesh = GetComponentInChildren<MeshRenderer>().gameObject;
		trees = new List<GameObject>(4);
		bushes = new List<GameObject>(16);

		int minimumTreeOffset = Mathf.RoundToInt(treeSize / 2.0f);
		int maximumTreeOffset = Mathf.RoundToInt(chunkSize / 2.0f - minimumTreeOffset);
		int minimumBushOffset = Mathf.RoundToInt(bushSize / 2.0f);
		int maximumBushOffset = Mathf.RoundToInt(minimumTreeOffset - minimumBushOffset);
		foreach(Vector2 treeDirection in directions)
		{
			Vector3 treePosition = chunkCenter.position + new Vector3(Random.Range(minimumTreeOffset, maximumTreeOffset) * treeDirection.x, 0.0f, Random.Range(minimumTreeOffset, maximumTreeOffset) * treeDirection.y);
			if(Random.value < treeChance)
			{
				GameObject tree = GameObject.Instantiate(treePrefabs[Random.Range(0, treePrefabs.Length)], treePosition, Quaternion.Euler(new Vector3(0.0f, Random.Range(0, 8) * 45.0f, 0.0f)), transform);
				trees.Add(tree);
			}

			foreach(Vector2 bushDirection in directions)
			{
				if(Random.value < bushChance)
				{
					Vector3 bushPosition = treePosition + new Vector3(Random.Range(minimumBushOffset, maximumBushOffset) * bushDirection.x, 0.0f, Random.Range(minimumBushOffset, maximumBushOffset) * bushDirection.y);

					Quaternion rotation = Quaternion.identity;
					RaycastHit hit;
					if(Physics.Raycast(bushPosition + Vector3.up * bushSize * 2.0f, Vector3.down, out hit, bushSize * 2.0f))
					{
						if(hit.transform.gameObject != gameObject)
						{
							continue;
						}
					}

					GameObject bush = GameObject.Instantiate(bushPrefabs[Random.Range(0, bushPrefabs.Length)], bushPosition, rotation, transform);
					bush.transform.Rotate(Vector3.up, Random.Range(0, 8) * 45.0f, 0.0f);
					bushes.Add(bush);
				}
			}
		}

		renderDistance = renderDistance * renderDistance;

		// TODO: Don't use Find 2000x on Startup (once for each Chunk)
		PlayerController[] playerControllers = FindObjectsOfType<PlayerController>();
		players = new Transform[playerControllers.Length];
		for(int i = 0; i < players.Length; ++i)
		{
			players[i] = playerControllers[i].transform;
		}

		StartCoroutine(CheckRenderDistance());
	}

	private IEnumerator CheckRenderDistance()
	{
		yield return new WaitForSeconds(Random.value * renderDistanceUpdateInterval);

		// TODO: Intermediate Visibility Stage with simplified Models
		while(true)
		{
			bool outOfRange = true;
			foreach(Transform player in players)
			{
				if((player.position - transform.position).sqrMagnitude < renderDistance)
				{
					chunkMesh.SetActive(true);
					foreach(GameObject tree in trees)
					{
						tree.SetActive(true);
					}
					foreach(GameObject bush in bushes)
					{
						bush.SetActive(true);
					}

					outOfRange = false;
					break;
				}
			}

			if(outOfRange)
			{
				chunkMesh.SetActive(false);
				foreach(GameObject tree in trees)
				{
					tree.SetActive(false);
				}
				foreach(GameObject bush in bushes)
				{
					bush.SetActive(false);
				}
			}

			yield return new WaitForSeconds(renderDistanceUpdateInterval);
		}
	}
}

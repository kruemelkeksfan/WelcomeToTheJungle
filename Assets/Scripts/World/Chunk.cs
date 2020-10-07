using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
	private static List<Transform> players = new List<Transform>();
	private static Vector2[] directions = { Vector2.zero, new Vector2(-1.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, -1.0f), new Vector2(-1.0f, -1.0f) };
	[SerializeField] private GameObject modelParent = null;
	[SerializeField] private GameObject chunkCenter = null;
	[SerializeField] private float chunkSize = 50.0f;
	[SerializeField] private float bushSize = 2.0f;
	[SerializeField] private float treeSize = 20.0f;
	[SerializeField] private float bushChance = 0.8f;
	[SerializeField] private float treeChance = 0.8f;
	[SerializeField] private GameObject[] bushPrefabs = null;
	[SerializeField] private GameObject[] treePrefabs = null;
	[SerializeField] private float renderDistance = 1000.0f;
	[SerializeField] private float renderDistanceUpdateInterval = 2.0f;
	private NetworkController network = null;

	public static void AddPlayer(Transform player)
	{
		if(!players.Contains(player))
		{
			players.Add(player);
		}
	}

	public static void RemovePlayer(Transform player)
	{
		players.Remove(player);
	}

	private void Start()
	{
		network = NetworkController.instance;
		if(network != null && network.IsHost)
		{
			int minimumTreeOffset = Mathf.RoundToInt(treeSize * 0.5f);
			int maximumTreeOffset = Mathf.RoundToInt(chunkSize * 0.5f - minimumTreeOffset);
			int minimumBushOffset = Mathf.RoundToInt(bushSize * 0.5f);
			int maximumBushOffset = Mathf.RoundToInt(minimumTreeOffset - minimumBushOffset);
			int plantCounter = 0;
			foreach(Vector2 treeDirection in directions)
			{
				Vector3 treePosition = chunkCenter.transform.position + new Vector3(Random.Range(minimumTreeOffset, maximumTreeOffset) * treeDirection.x, 0.0f, Random.Range(minimumTreeOffset, maximumTreeOffset) * treeDirection.y);
				if(Random.value < treeChance)
				{
					GameObject tree = GameObject.Instantiate(treePrefabs[Random.Range(0, treePrefabs.Length)], treePosition, Quaternion.Euler(new Vector3(0.0f, Random.Range(0, 8) * 45.0f, 0.0f)), modelParent.transform);
					tree.transform.localScale *= 1.0f - 0.001f * plantCounter++;
				}

				foreach(Vector2 bushDirection in directions)
				{
					if(bushDirection != Vector2.zero && Random.value < bushChance)
					{
						Vector3 bushPosition = treePosition + new Vector3(Random.Range(minimumBushOffset, maximumBushOffset) * bushDirection.x, 0.0f, Random.Range(minimumBushOffset, maximumBushOffset) * bushDirection.y);

						RaycastHit hit;
						if(Physics.Raycast(bushPosition + Vector3.up * bushSize * 2.0f, Vector3.down, out hit, bushSize * 2.0f))
						{
							if(hit.transform.gameObject != gameObject)
							{
								continue;
							}
						}

						GameObject bush = GameObject.Instantiate(bushPrefabs[Random.Range(0, bushPrefabs.Length)], bushPosition, Quaternion.Euler(new Vector3(0.0f, Random.Range(0, 8) * 45.0f, 0.0f)), modelParent.transform);
						bush.transform.localScale *= 1.0f - 0.001f * plantCounter++;
					}
				}
			}
		}

		renderDistance = renderDistance * renderDistance;

		StartCoroutine(CheckRenderDistance());
	}

	private IEnumerator CheckRenderDistance()
	{
		yield return new WaitForSeconds(Random.value * renderDistanceUpdateInterval);

		// TODO: Intermediate Visibility Stage with simplified Models
		// TODO: Alternative Algithm: Use Hashmap with x/y-Coordinates and Loop over Coordinates in Square around Player to enable them,
		//	think of something clever to disable them, e.g. write SetActive(false) in Code on Object, so that it is only called while Object is enabled,
		//	requires timing to have the SetActive(true) after the SetActive(false), maybe refresh a timer on SetActive(true) and only SetActive(false) when time is up
		while(true)
		{
			bool outOfRange = true;
			foreach(Transform player in players)
			{
				if((player.position - transform.position).sqrMagnitude < renderDistance)
				{
					modelParent.SetActive(true);

					outOfRange = false;
					break;
				}
			}

			if(outOfRange)
			{
				modelParent.SetActive(false);
			}

			yield return new WaitForSeconds(renderDistanceUpdateInterval);
		}
	}
}

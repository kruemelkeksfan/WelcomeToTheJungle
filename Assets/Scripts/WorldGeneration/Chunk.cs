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
    private Vector2[] directions = { new Vector2(-1.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, -1.0f), new Vector2(-1.0f, -1.0f) };

    private void Start()
    {
        float minimumTreeOffset = treeSize / 2.0f;
        float maximumTreeOffset = chunkSize / 2.0f - minimumTreeOffset;
        float minimumBushOffset = bushSize / 2.0f;
        float maximumBushOffset = minimumTreeOffset - minimumBushOffset;
        foreach(Vector2 treeDirection in directions)
		{
            Vector3 treePosition = chunkCenter.position + new Vector3(Random.Range(minimumTreeOffset, maximumTreeOffset) * treeDirection.x, 0.0f, Random.Range(minimumTreeOffset, maximumTreeOffset) * treeDirection.y);
            PlacePlant(treePosition, treeChance, treePrefabs);

            foreach(Vector2 bushDirection in directions)
		    {
                Vector3 bushPosition = treePosition + new Vector3(Random.Range(minimumBushOffset, maximumBushOffset) * bushDirection.x, 0.0f, Random.Range(minimumBushOffset, maximumBushOffset) * bushDirection.y);
                PlacePlant(bushPosition, bushChance, bushPrefabs);
            }
		}
    }

    private void PlacePlant(Vector3 position, float plantChance, GameObject[] plantPrefabs)
	{
        if(Random.value < plantChance)
		{
            GameObject plant = GameObject.Instantiate(plantPrefabs[Random.Range(0, plantPrefabs.Length)], position, Quaternion.identity);
		}
	}
}

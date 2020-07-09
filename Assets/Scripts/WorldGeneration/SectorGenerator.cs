﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SectorGenerator : MonoBehaviour
{
    [SerializeField] private int width = 100;
    [SerializeField] private int height = 100;
    [Tooltip("Determines the Size of Terrain Features, low Values break Unitys PerlinNoise")]
    [SerializeField] private float terrainSmoothness = 10.0f;
    [Tooltip("Determines the Height Difference between Hills and Valleys")]
    [SerializeField] private float terrainSteepness = 10.0f;
    [Tooltip("The Building Blocks of which the Terrain will consist, should be a rectangular GameObject with exactly 1 Collider which must be on the Root GameObject")]
    [SerializeField] private GameObject chunkPrefab = null;

    private void Start()
    {
        GameObject chunkProbe = GameObject.Instantiate(chunkPrefab);
        float chunkSize = chunkProbe.GetComponent<Collider>().bounds.size.x;
        GameObject.Destroy(chunkProbe, 0.0f);

        for(int y = 0; y < height; ++y)
		{
            for(int x = 0; x < width; ++x)
			{
                GameObject.Instantiate(chunkPrefab, new Vector3(x * chunkSize, Mathf.PerlinNoise((x * chunkSize) / terrainSmoothness, (y * chunkSize) / terrainSmoothness) * terrainSteepness, y * chunkSize), Quaternion.identity);
			}
        }
    }
}

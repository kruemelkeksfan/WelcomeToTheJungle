using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sector : MonoBehaviour
{
    [SerializeField] private float plantChance = 0.4f;
    [SerializeField] private Transform[] plantSpots = null;
    [SerializeField] private GameObject[] plantPrefabs = null;

    private void Start()
    {
        foreach(Transform plantSpot in plantSpots)
        {
            if(Random.value < plantChance)
			{
                GameObject plant = GameObject.Instantiate(plantPrefabs[Random.Range(0, plantPrefabs.Length - 1)], plantSpot);
			}
		}
    }
}

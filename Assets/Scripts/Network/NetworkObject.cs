using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkObject : MonoBehaviour
{
    [SerializeField] protected NetworkController network = null;
    private static uint idCounter = 0;
    private static uint maxId = uint.MaxValue / 2;
    private uint id = 0;

    private void Start()
    {
        id = idCounter++;
        if(idCounter > maxId)
		{
            Debug.LogWarning("Network IDs nearly exhausted, newest ID is " + id);
		}

        network.SendToHost(); // TODO: Position, Rotation and Velocities
    }
}

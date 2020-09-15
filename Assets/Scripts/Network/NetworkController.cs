using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkController : MonoBehaviour
{
    public bool IsHost { get; set; } = false;
    public bool IsClient { get; set; } = false;

    private void Start()
    {
        // TODO: Start Receive Thread
    }

    public void SendToHost()
	{

	}

    public void SendToClient()
	{

	}
}

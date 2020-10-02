using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkObject : MonoBehaviour
{
	[SerializeField] private string path = "";
	private static uint idCounter = 0;
	private static uint maxId = uint.MaxValue / 2;
	protected NetworkController network = null;

	public uint ID { get; private set; } = 0;

	protected virtual void Start()
	{
		network = NetworkController.instance;
		if(network != null && network.IsHost)
		{
			ID = ++idCounter;
			if(idCounter > maxId)
			{
				Debug.LogWarning("Network IDs nearly exhausted, newest ID is " + ID);
			}

			network.AddNetworkObject(this);
		}
	}

	private void OnDestroy()
	{
		network.RemoveNetworkObject(this);
		if(ID == 0)
		{
			Debug.LogError("ID: " + ID + " " + name);
		}
	}

	public string GetResourcePath()
	{
		return path;
	}

	public void SetID(uint id)
	{
		network = NetworkController.instance;
		if(network != null && network.IsClient)
		{
			this.ID = id;
			network.AddNetworkObject(this);
		}
		else
		{
			Debug.LogError("Can not assign a custom NetworkObject-ID on a Host!");
		}
	}
}

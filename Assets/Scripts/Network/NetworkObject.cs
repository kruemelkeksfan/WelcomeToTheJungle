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
		SetID();
	}

	private void OnDestroy()
	{
		network.RemoveNetworkObject(this);
		if(ID == 0)
		{
			Debug.LogError("NetworkObject " + name + " had no ID!");
		}
	}

	public string GetResourcePath()
	{
		return path;
	}

	public void SetID(uint id = 0)
	{
		if(ID == 0)
		{
			network = NetworkController.instance;
			if(network != null)
			{
				if(network.IsHost)
				{
					if(id != 0)
					{
						Debug.LogWarning("Trying to set explicit NetworkObject ID " + id + " on Host!");
					}

					ID = ++idCounter;
					if(idCounter > maxId)
					{
						Debug.LogWarning("Network IDs nearly exhausted, newest ID is " + ID);
					}

					network.AddNetworkObject(this);
				}
				else if(network.IsClient)
				{
					if(id != 0)
					{
						this.ID = id;
						network.AddNetworkObject(this);
					}
					else
					{
						Debug.LogError("Trying to set NetworkObject ID without providing an ID on Client!");
					}
				}
				else
				{
					Debug.LogError("Trying to set NetworkObject ID without being Host nor Client!");
				}
			}
			else
			{
				Debug.LogError("Trying to set ID on NetworkObject without NetworkController!!");
			}
		}
	}
}

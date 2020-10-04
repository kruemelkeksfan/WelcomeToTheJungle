using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkController : MonoBehaviour
{
	public const int MTU = 1400;
	public const int OBJECTS_PER_PACKET = 6;
	public static NetworkController instance = null;
	[SerializeField] private float timeoutDuration = 2.0f;
	[SerializeField] private GameObject playerPrefab = null;
	[SerializeField] private Vector3 spawnPoint = Vector3.zero;
	private UTF8Encoding utf = null;
	private int port = 2060;
	private bool verbose = false;
	private Socket socket = null;
	private byte[] recBuffer = new byte[MTU];
	private IPEndPoint host = null;
	private Dictionary<string, IPEndPoint> clients = null;
	private ConcurrentQueue<Tuple<IPEndPoint, string>> recMessages = null;
	private Dictionary<uint, NetworkObject> networkObjects = null;
	private Dictionary<Tuple<string, uint>, Tuple<float, int>> unconfirmedInstantiations = null;
	private Dictionary<string, GameObject> instantiationPrefabs = null;
	private Dictionary<string, NetworkInputController> players = null;
	private IEnumerator responseCoroutine = null;
	private ErrorController error = null;

	public string Username { get; set; } = null;
	public bool IsHost { get; set; } = false;
	public bool IsClient { get; set; } = false;

	private void Awake()
	{
		DontDestroyOnLoad(gameObject);
		instance = this;
		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
		utf = new UTF8Encoding(true, true);

		int portSetting = SettingHelper.ReadSettingInt("NetworkSettings.cfg", "Port");
		if(portSetting > 0)
		{
			port = portSetting;
		}
		verbose = SettingHelper.ReadSettingBool("NetworkSettings.cfg", "VerboseLogging");

		socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		socket.Bind(new IPEndPoint(IPAddress.Any, port));

		clients = new Dictionary<string, IPEndPoint>();
		clients["unknown"] = null;
		recMessages = new ConcurrentQueue<Tuple<IPEndPoint, string>>();
		networkObjects = new Dictionary<uint, NetworkObject>();
		unconfirmedInstantiations = new Dictionary<Tuple<string, uint>, Tuple<float, int>>();
		instantiationPrefabs = new Dictionary<string, GameObject>();
		players = new Dictionary<string, NetworkInputController>();
		error = GameObject.Find("ErrorPanel")?.GetComponentInChildren<ErrorController>();

		Username = Dns.GetHostName();

#if(UNITY_SERVER)
		IsHost = true;
		IsClient = false;
		SceneManager.LoadScene("MainScene", LoadSceneMode.Single);
#endif

		Thread receiveThread = new Thread(Receive);
		receiveThread.IsBackground = true;
		receiveThread.Start();
	}

	private void FixedUpdate()
	{
		while(recMessages.Count > 0)
		{
			if(responseCoroutine != null)
			{
				StopCoroutine(responseCoroutine);
				responseCoroutine = null;
			}

			error = GameObject.Find("ErrorPanel")?.GetComponentInChildren<ErrorController>();

			Tuple<IPEndPoint, string> recMessage;
			recMessages.TryDequeue(out recMessage);
			if(verbose)
			{
				Debug.Log("Received Network Message: " + recMessage);
			}

			string[] message = recMessage.Item2.Split(' ');
			// Client
			if(IsClient && message.Length == 1 && message[0] == "UsernameInUse")
			{
				error?.AddError("Username is already in Use!");
			}
			else if(IsClient && message.Length == 2 && message[0] == "LoadScene")
			{
				SceneManager.LoadScene(message[1], LoadSceneMode.Single);
				break;
			}
			else if(IsClient && !IsHost && message.Length == 2 && message[0] == "Instantiate")
			{
				InstantiateFromString(message[1]);
			}
			else if(IsClient && !IsHost && message.Length == 3 && message[0] == "InstantiatePlayer")
			{
				GameObject player = InstantiateFromString(message[2]);
				player.name = message[1];

				if(message[1] == Username)
				{
					player.GetComponentInChildren<Camera>().enabled = true;
					player.GetComponentInChildren<AudioListener>().enabled = true;
					player.GetComponentInChildren<KeyboardInputController>().enabled = true;
					Chunk.AddPlayer(player.transform);
				}
			}
			else if(IsClient && message.Length == 14 && message[0] == "PositionUpdate")
			{
				uint id;
				float positionX;
				float positionY;
				float positionZ;
				float rotationX;
				float rotationY;
				float rotationZ;
				float velocityX;
				float velocityY;
				float velocityZ;
				float rotationVelocityX;
				float rotationVelocityY;
				float rotationVelocityZ;
				if(uint.TryParse(message[1], out id)
					&& float.TryParse(message[2], out positionX) && float.TryParse(message[3], out positionY) && float.TryParse(message[4], out positionZ)
					&& float.TryParse(message[5], out rotationX) && float.TryParse(message[6], out rotationY) && float.TryParse(message[7], out rotationZ)
					&& float.TryParse(message[8], out velocityX) && float.TryParse(message[9], out velocityY) && float.TryParse(message[10], out velocityZ)
					&& float.TryParse(message[11], out rotationVelocityX) && float.TryParse(message[12], out rotationVelocityY) && float.TryParse(message[13], out rotationVelocityZ)
					&& networkObjects.ContainsKey(id) && networkObjects[id] is NetworkRigidbody)
				{
					((NetworkRigidbody) networkObjects[id]).UpdatePosition(positionX, positionY, positionZ, rotationX, rotationY, rotationZ, velocityX, velocityY, velocityZ, rotationVelocityX, rotationVelocityY, rotationVelocityZ);
				}
			}
			// Host
			else if(IsHost && message.Length == 2 && message[1] == "Join")
			{
				if(!clients.ContainsKey(message[0]))
				{
					clients.Add(message[0], recMessage.Item1);

					SendToClient(message[0], "LoadScene " + SceneManager.GetActiveScene().name);

					int objectCounter = 0;
					string packetContent = "Instantiate ";
					foreach(NetworkObject networkObject in networkObjects.Values)
					{
						packetContent += (objectCounter > 0 ? ";" : "") + GetInstantiationString(networkObject);
						++objectCounter;
						if(objectCounter >= OBJECTS_PER_PACKET)
						{
							SendToClient(message[0], packetContent);
							objectCounter = 0;
							packetContent = "Instantiate ";
						}

						unconfirmedInstantiations[new Tuple<string, uint>(message[0], networkObject.ID)] = new Tuple<float, int>(Time.time, GetInstantiationHash(networkObject));
					}
					if(objectCounter > 0)
					{
						SendToClient(message[0], packetContent);
					}

					NetworkObject player = Instantiate(playerPrefab, spawnPoint, Quaternion.identity).GetComponent<NetworkObject>();
					player.gameObject.name = message[0];
					player.SetID();
					players.Add(message[0], player.GetComponent<NetworkInputController>());
					Chunk.AddPlayer(player.transform);
					SendToClients("InstantiatePlayer " + message[0] + " " + GetInstantiationString(player));
					unconfirmedInstantiations[new Tuple<string, uint>(message[0], player.ID)] = new Tuple<float, int>(Time.time, GetInstantiationHash(player));

					StartCoroutine(WaitForConfirmation());
				}
				else
				{
					clients["unknown"] = recMessage.Item1;
					SendToClient("unknown", "UsernameInUse");
					Debug.LogWarning("Username " + message[0] + " is already in Use!");
				}
			}
			else if(IsHost && message.Length == 4 && message[1] == "ConfirmInstantiation")
			{
				uint id;
				if(uint.TryParse(message[2], out id) && unconfirmedInstantiations.ContainsKey(new Tuple<string, uint>(message[0], id)))
				{
					int checksum;
					if(int.TryParse(message[3], out checksum) && checksum == unconfirmedInstantiations[new Tuple<string, uint>(message[0], id)].Item2)
					{
						unconfirmedInstantiations.Remove(new Tuple<string, uint>(message[0], id));
					}
				}
			}
			else if(IsHost && message.Length == 6 && message[1] == "MovementUpdate")
			{
				float rotationX;
				float rotationY;
				float movementX;
				float movementY;
				if(float.TryParse(message[2], out rotationX) && float.TryParse(message[3], out rotationY) && float.TryParse(message[4], out movementX) && float.TryParse(message[5], out movementY))
				{
					players[message[0]].UpdateMovement(rotationX, rotationY, movementX, movementY);
				}
			}
			else if(IsHost && message.Length == 3 && message[1] == "Input")
			{
				players[message[0]].ProcessInput(message[2]);
			}
			else
			{
				Debug.LogWarning("Invalid Network Message: " + recMessage);
			}
		}
	}

	private void OnApplicationQuit()
	{
		socket.Close();
		instance = null;
	}

	public void AddNetworkObject(NetworkObject networkObject)
	{
		networkObjects.Add(networkObject.ID, networkObject);
	}

	public void RemoveNetworkObject(NetworkObject networkObject)
	{
		networkObjects.Remove(networkObject.ID);
	}

	public void Join()
	{
		if(!host.Address.Equals(IPAddress.Loopback) || port != host.Port)
		{
			SendToHost(Username, "Join");
		}
		else
		{
			StartCoroutine(SpawnHostPlayer());
		}
	}

	public void SendMovementUpdate(Vector2 rotation, Vector2 movement)
	{
		if(!IsHost)
		{
			SendToHost(Username, "MovementUpdate " + rotation.x + " " + rotation.y + " " + movement.x + " " + movement.y);
		}
	}

	public void SendInput(string input)
	{
		if(!IsHost)
		{
			SendToHost(Username, "Input " + input);
		}
	}

	public void SendPositionUpdate(uint id, Vector3 position, Vector3 rotation, Vector3 velocity, Vector3 rotationVelocity)
	{
		if(IsHost)
		{
			SendToClients("PositionUpdate " + id + " " + position.x + " " + position.y + " " + position.z + " " + rotation.x + " " + rotation.y + " " + rotation.z + " "
				+ velocity.x + " " + velocity.y + " " + velocity.z + " " + rotationVelocity.x + " " + rotationVelocity.y + " " + rotationVelocity.z);
		}
	}

	public void SetHost(string ip, string port)
	{
		host = null;                                                        // Reset Host to avoid connecting to the old Host after Errors were thrown

		IPAddress address;
		if(IPAddress.TryParse(ip, out address) || ip == "")
		{
			if(ip == "")
			{
				address = IPAddress.Loopback;
			}

			int portNumber;
			if(int.TryParse(port, out portNumber) || port == "")
			{
				if(port == "")
				{
					portNumber = this.port;
				}

				host = new IPEndPoint(address, portNumber);
			}
			else
			{
				error?.AddError("Invalid Port!");
			}
		}
		else
		{
			error?.AddError("Invalid IP!");
		}
	}

	private void SendToHost(string clientname, string message)
	{
		if(host != null)
		{
			socket.SendTo(utf.GetBytes(clientname + " " + message), host);
			if(verbose)
			{
				Debug.Log("Sent Network Message: " + message + " to Host " + host);
			}

			responseCoroutine = WaitForResponse();
			StartCoroutine(responseCoroutine);
		}
	}

	private void SendToClient(string client, string message)
	{
		if(clients.ContainsKey(client))
		{
			socket.SendTo(utf.GetBytes(message), clients[client]);
			if(verbose)
			{
				Debug.Log("Sent Network Message: " + message + " to Client " + clients[client]);
			}
		}
	}

	private void SendToClients(string message)
	{
		Byte[] sendBytes = utf.GetBytes(message);
		foreach(IPEndPoint client in clients.Values)
		{
			if(client != null)
			{
				socket.SendTo(sendBytes, client);
			}
		}
		if(verbose)
		{
			Debug.Log("Sent Network Message: " + message + " to all Clients!");
		}
	}

	private void Receive()
	{
		while(true)
		{
			recBuffer = new byte[1024];
			EndPoint sender = new IPEndPoint(IPAddress.Any, 0);
			try
			{
				socket.ReceiveFrom(recBuffer, ref sender);
			}
			catch(SocketException exc)
			{
				if(exc.SocketErrorCode != SocketError.Interrupted)
				{
					Debug.LogError(exc.ToString());
				}
				return;
			}
			IPEndPoint senderIP;
			if((senderIP = (sender as IPEndPoint)) == null)
			{
				Debug.LogError("EndPoint could not be casted to IPEndPoint in NetworkController!");
				return;
			}

			int bufferLength = Array.IndexOf<byte>(recBuffer, 0);
			if(bufferLength > 0)
			{
				recMessages.Enqueue(new Tuple<IPEndPoint, string>(senderIP, utf.GetString(recBuffer, 0, bufferLength)));
			}
			else
			{
				recMessages.Enqueue(new Tuple<IPEndPoint, string>(senderIP, utf.GetString(recBuffer)));
			}
		}
	}

	private string GetInstantiationString(NetworkObject instantiationObject)
	{
		Vector3 position = instantiationObject.transform.position;
		Vector3 rotation = instantiationObject.transform.rotation.eulerAngles;
		position = new Vector3((float)Math.Round(position.x, 4), (float)Math.Round(position.y, 4), (float)Math.Round(position.z, 4));
		rotation = new Vector3((float)Math.Round(rotation.x, 4), (float)Math.Round(rotation.y, 4), (float)Math.Round(rotation.z, 4));
		instantiationObject.transform.position = position;
		instantiationObject.transform.rotation = Quaternion.Euler(rotation);
		return instantiationObject.GetResourcePath() + "|" + instantiationObject.ID + "|" + position.x + "|" + position.y + "|" + position.z + "|" + rotation.x + "|" + rotation.y + "|" + rotation.z;
	}

	private GameObject InstantiateFromString(string instantiationString)
	{
		string[] gameObjects = instantiationString.Split(';');
		GameObject instance = null;
		foreach(string gameObject in gameObjects)
		{
			string[] objectData = gameObject.Split('|');
			if(objectData.Length == 8)
			{
				uint id = 0;
				Vector3 position = Vector3.zero;
				Vector3 rotation = Vector3.zero;
				try
				{
					id = uint.Parse(objectData[1]);
					position = new Vector3(float.Parse(objectData[2]), float.Parse(objectData[3]), float.Parse(objectData[4]));
					rotation = new Vector3(float.Parse(objectData[5]), float.Parse(objectData[6]), float.Parse(objectData[7]));
				}
				catch(FormatException)
				{
					Debug.LogError("Invalid Data in Resource " + gameObject);
					return null;
				}

				if(!instantiationPrefabs.ContainsKey(objectData[0]))
				{
					GameObject prefabInstance = Resources.Load<GameObject>(objectData[0]);
					if(prefabInstance == null)
					{
						Debug.LogError("Could not find Resource " + objectData[0]);
						return null;
					}
					instantiationPrefabs.Add(objectData[0], prefabInstance);
				}

				instance = Instantiate(instantiationPrefabs[objectData[0]], position, Quaternion.Euler(rotation));
				instance.GetComponent<NetworkObject>().SetID(id);

				SendToHost(Username, "ConfirmInstantiation " + id + " " + GetInstantiationHash(instance.GetComponent<NetworkObject>()));
			}
			else
			{
				Debug.LogError("Wrong Number of Data-Elements in " + gameObject);
				return null;
			}
		}

		return instance;
	}

	private int GetInstantiationHash(NetworkObject instantiationObject)
	{
		unchecked
		{
			return (int)((instantiationObject.transform.position.x + instantiationObject.transform.position.y + instantiationObject.transform.position.z
			+ instantiationObject.transform.rotation.x + instantiationObject.transform.rotation.y + instantiationObject.transform.rotation.z) * 10000);
		}
	}

	private IEnumerator SpawnHostPlayer()
	{
		yield return null;
		GameObject player = Instantiate(playerPrefab, spawnPoint, Quaternion.identity);
		player.gameObject.name = Username;
		player.GetComponent<NetworkObject>().SetID();
		player.GetComponentInChildren<Camera>().enabled = true;
		player.GetComponentInChildren<AudioListener>().enabled = true;
		player.GetComponentInChildren<KeyboardInputController>().enabled = true;
		Chunk.AddPlayer(player.transform);
	}

	private IEnumerator WaitForResponse()
	{
		yield return new WaitForSeconds(timeoutDuration);
		error?.AddError("Server does not respond!");
	}

	private IEnumerator WaitForConfirmation()
	{
		do
		{
			yield return new WaitForSeconds((unconfirmedInstantiations.Count / 1000.0f) * timeoutDuration);

			foreach(KeyValuePair<Tuple<string, uint>, Tuple<float, int>> instantiationData in unconfirmedInstantiations)
			{
				if(Time.time > instantiationData.Value.Item1 + timeoutDuration)
				{
					Debug.LogError("Object " + instantiationData.Key + " was not sent successfully!");
				}
			}
		}
		while(unconfirmedInstantiations.Count > 0);
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonController : MonoBehaviour
{
	[SerializeField] private InputField usernameInput = null;
	[SerializeField] private InputField ipInput = null;
	[SerializeField] private InputField portInput = null;
	private NetworkController network = null;

	private void Start()
	{
		network = NetworkController.instance;
	}

	public void LoadScene(string name)
	{
		SceneManager.LoadScene(name, LoadSceneMode.Single);
	}

	public void ConnectToLocalHostScene()
	{
		if(usernameInput.text != "")
		{
			network.Username = usernameInput.text;
		}
		network.SetHost("", "");
		network.Join();
	}

	public void ConnectToHostScene()
	{
		if(usernameInput.text != "")
		{
			network.Username = usernameInput.text;
		}
		string port = "";
		if(portInput.text != "")
		{
			port = portInput.text;
		}
		network.SetHost(ipInput.text, port);
		network.Join();
	}

	public void Quit()
	{
		Application.Quit();
	}
}

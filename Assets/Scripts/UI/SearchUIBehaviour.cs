using System.Collections.Generic;
using UnityEngine;

using LobbyUtils;
using UnityEngine.UI;

using UnityEngine.SceneManagement;

using System.Net;

public class SearchUIBehaviour : MonoBehaviour
{
	private static readonly string LOBBY_SCENE = "Assets/Scenes/LobbyScene.unity";

	[SerializeField]
	private GameObject joinGameButton = null;

	[SerializeField]
	private GameObject IPInputField = null;

	[SerializeField]
	private GameObject clientObject = null;

	private ClientLobbyReceiveComponent client;
	private string ipString;

	private void Start()
	{
		IPInputField.GetComponent<InputField>().onEndEdit.AddListener(OnSetIP);
	}

	public void Init(ClientLobbyReceiveComponent _client)
	{
		client = _client;
	}
	
	public void OnJoinClick()
	{
		GameObject newClient = Instantiate(clientObject);
		//newClient.GetComponent<ClientConnectionsComponent>().Init(false, IPAddress.Parse(ipString), 9000);
		newClient.GetComponent<ClientConnectionsComponent>().Init(false, IPAddress.Parse(ipString));
		DontDestroyOnLoad(newClient);


		SceneManager.LoadScene(LOBBY_SCENE);
	}

	public void OnSetIP(string _ipString)
	{
		ipString = _ipString;
	}
}

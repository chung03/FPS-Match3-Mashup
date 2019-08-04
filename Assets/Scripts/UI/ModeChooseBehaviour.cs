﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using System.Net;

public class ModeChooseBehaviour : MonoBehaviour
{
	private static readonly string LOBBY_SCENE = "Assets/Scenes/LobbyScene.unity";
	private static readonly string SEARCH_SCENE = "Assets/Scenes/SearchScene.unity";

	[SerializeField]
	private GameObject serverObject = null;

	[SerializeField]
	private GameObject clientObject = null;

	public void BeServer()
	{
		GameObject newServer = Instantiate(serverObject);
		newServer.GetComponent<ServerConnectionsComponent>().Init();
		DontDestroyOnLoad(newServer);

		GameObject newClient = Instantiate(clientObject);
		newClient.GetComponent<ClientConnectionsComponent>().Init(true, IPAddress.Loopback);
		DontDestroyOnLoad(newClient);

		SceneManager.LoadScene(LOBBY_SCENE);
	}

	public void BeClient()
	{
		SceneManager.LoadScene(SEARCH_SCENE);
	}
}

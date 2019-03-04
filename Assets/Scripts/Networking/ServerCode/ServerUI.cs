using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerUI : MonoBehaviour
{
	[SerializeField]
	private GameObject lobbyUI;

	public void SetUI(string sceneName)
	{

		Debug.Log("ServerUI::SetUI called. sceneName = " + sceneName);

		if (sceneName == "LobbyScene")
		{
			LobbyUIBehaviour lobby = Instantiate(lobbyUI).GetComponent<LobbyUIBehaviour>();
			lobby.SetUI(true);
		}
	}
}

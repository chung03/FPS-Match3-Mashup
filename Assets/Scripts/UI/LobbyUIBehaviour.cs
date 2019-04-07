using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyUIBehaviour : MonoBehaviour
{
	[SerializeField]
	private GameObject startGameButton;

	[SerializeField]
	private GameObject[] teamStats;

	private ClientLobbyComponent client;

	// Start is called before the first frame update
	private void Start()
    {
        
    }

    // Update is called once per frame
    private void Update()
    {
        
    }

	public void SetUI(bool isServer)
	{
		if (isServer)
		{
			startGameButton.SetActive(true);
		}
	}

	public void AddPlayer(int team)
	{

	}

	public void Init(ClientLobbyComponent _client)
	{
		client = _client;
	}

	public void OnReadyClick()
	{
		client.ChangeReadyStatus();
	}

	public void OnTeamClick()
	{
		client.ChangeTeam();
	}

	public void OnPlayerTypeClick()
	{
		client.ChangePlayerType();
	}

	public void OnStartGameClick()
	{
		client.SendStartGame();
	}
}

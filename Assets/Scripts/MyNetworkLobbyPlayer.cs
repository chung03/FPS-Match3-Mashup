using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Util;

public class MyNetworkLobbyPlayer : NetworkLobbyPlayer
{
	[SyncVar]
	private int team = 0;

	[SyncVar]
	private PLAYER_TYPE playerType = PLAYER_TYPE.TYPE_1;

    // Start is called before the first frame update
    private void Start()
    {
		if (this.isLocalPlayer)
		{
			Debug.Log("[LobbyPlayer] Player #" + this.playerControllerId + " clicked READY!");
			//SendReadyToBeginMessage();
		}
	}

    // Update is called once per frame
    private void Update()
    {
        
    }

	public void SetTeam(int _team)
	{
		team = _team;
	}

	public int GetTeam()
	{
		return team;
	}

	public void SetPlayerType(PLAYER_TYPE _playerType)
	{
		playerType = _playerType;
	}

	public PLAYER_TYPE GetPlayerType()
	{
		return playerType;
	}
}

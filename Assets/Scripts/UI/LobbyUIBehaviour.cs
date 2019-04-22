using System.Collections.Generic;
using UnityEngine;

using Util;
using UnityEngine.UI;

public class LobbyUIBehaviour : MonoBehaviour
{
	[SerializeField]
	private GameObject startGameButton;

	[SerializeField]
	private GameObject nameInputField;

	[SerializeField]
	private GameObject[] teamStats;

	private ClientLobbyComponent client;

	private void Start()
	{
		nameInputField.GetComponent<InputField>().onEndEdit.AddListener(OnSetName);
	}

	public void SetUI(bool isServer)
	{
		if (isServer)
		{
			startGameButton.SetActive(true);
		}
	}

	public void Init(ClientLobbyComponent _client)
	{
		client = _client;
	}

	public void UpdateUI(List<LobbyPlayerInfo> allPlayerInfo)
	{
		// Prototype hack to get UI up and running.
		// Should be replaced with more efficient and maintainable code later

		const string PLAYER_STATS = "PlayerStats";
		const string TEAM_1_STATS = "Team1Stats";
		const string TEAM_2_STATS = "Team2Stats";

		GameObject playerStatsObj = transform.Find(PLAYER_STATS).gameObject;

		GameObject team1Obj = playerStatsObj.transform.Find(TEAM_1_STATS).gameObject;
		GameObject team2Obj = playerStatsObj.transform.Find(TEAM_2_STATS).gameObject;

		// Go through all players looking for team players and then updating UI.
		SetTeamUI(team1Obj, 0, allPlayerInfo);
		SetTeamUI(team2Obj, 1, allPlayerInfo);
	}

	private void SetTeamUI(GameObject teamObj, int team, List<LobbyPlayerInfo> allPlayerInfo)
	{
		const string PLAYER_PREFIX = "Player ";

		const string NAME_TEXT = "Name Text";
		const string READY_TEXT = "Ready Text";
		const string PLAYER_TYPE_TEXT = "Player Type Text";

		// Go through all players looking for team 2 players and then updating UI.
		int numTeamFound = 0;
		for (int i = 0; i < CONSTANTS.MAX_NUM_PLAYERS; ++i)
		{
			if (allPlayerInfo[i] != null && allPlayerInfo[i].team == team)
			{
				numTeamFound++;

				// Set UI element active in case it was disabled before
				GameObject playerObj = teamObj.transform.Find(PLAYER_PREFIX + numTeamFound).gameObject;
				playerObj.SetActive(true);

				GameObject readyTextObj = playerObj.transform.Find(READY_TEXT).gameObject;
				if (allPlayerInfo[i].isReady == 0)
				{
					readyTextObj.GetComponent<Text>().text = "Not Ready";
				}
				else
				{
					readyTextObj.GetComponent<Text>().text = "Ready";
				}

				GameObject nameTextObj = playerObj.transform.Find(NAME_TEXT).gameObject;
				nameTextObj.GetComponent<Text>().text = allPlayerInfo[i].name;

				GameObject playerTypeTextObj = playerObj.transform.Find(PLAYER_TYPE_TEXT).gameObject;
				playerTypeTextObj.GetComponent<Text>().text = allPlayerInfo[i].playerType.ToString();
			}
		}

		// Disable unused player slots to make UI easier to debug and understand at a glance
		for (int i = numTeamFound; i < CONSTANTS.MAX_NUM_PLAYERS / 2; ++i)
		{
			GameObject playerObj = teamObj.transform.Find(PLAYER_PREFIX + (i + 1)).gameObject;
			playerObj.SetActive(false);
		}
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

	public void OnSetName(string newName)
	{
		client.ChangeName(newName);
	}
}

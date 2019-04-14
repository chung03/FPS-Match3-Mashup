using System.Net;
using System.Collections.Generic;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using UnityEngine.Assertions;
using Util;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ClientLobbyComponent : MonoBehaviour
{
	private static readonly string PLAY_SCENE = "Assets/Scenes/PlayScene.unity";

	// This is the player's ID. This is all it needs to identify itself and to find its player info.
	private int m_PlayerID;

	// This stores the infor for all players, including this one.
	private List<LobbyPlayerInfo> m_AllPlayerInfo;
	private Queue<byte> sendQueue;

	// A Pair of Dictionaries to make it easier to map Index and PlayerID
	// ID -> Connection Index
	private Dictionary<int, int> IdToIndexDictionary;
	// Connection Index -> ID
	private Dictionary<int, int> IndexToIdDictionary;

	private ClientConnectionsComponent connectionsComponent;

	[SerializeField]
	private GameObject lobbyUIObj;
	private GameObject lobbyUIInstance;


	private void Start()
	{
		//Debug.Log("ClientLobbyComponent::Start Called");
		m_PlayerID = -1;
		m_AllPlayerInfo = new List<LobbyPlayerInfo>(ServerLobbyComponent.MAX_NUM_PLAYERS);
		for (int index = 0; index < ServerLobbyComponent.MAX_NUM_PLAYERS; ++index)
		{
			m_AllPlayerInfo.Add(null);
		}

		lobbyUIInstance = Instantiate(lobbyUIObj);
		lobbyUIInstance.GetComponent<LobbyUIBehaviour>().SetUI(connectionsComponent.IsHost());
		lobbyUIInstance.GetComponent<LobbyUIBehaviour>().Init(this);

		sendQueue = new Queue<byte>();

		IdToIndexDictionary = new Dictionary<int, int>();
		IndexToIdDictionary = new Dictionary<int, int>();
	}

	public void Init(ClientConnectionsComponent connHolder)
	{
		//Debug.Log("ClientLobbyComponent::Init Called");
		connectionsComponent = connHolder;
	}

	public void ChangeTeam()
	{
		sendQueue.Enqueue((byte)LOBBY_CLIENT_REQUESTS.CHANGE_TEAM);
	}

	public void ChangeReadyStatus()
	{
		sendQueue.Enqueue((byte)LOBBY_CLIENT_REQUESTS.READY);
	}

	public void ChangePlayerType()
	{
		sendQueue.Enqueue((byte)LOBBY_CLIENT_REQUESTS.CHANGE_PLAYER_TYPE);
	}

	public void SendStartGame()
	{
		sendQueue.Enqueue((byte)LOBBY_CLIENT_REQUESTS.START_GAME);
	}

	void Update()
	{
		ref UdpCNetworkDriver driver = ref connectionsComponent.GetDriver();
		ref NetworkConnection connection = ref connectionsComponent.GetConnection();

		driver.ScheduleUpdate().Complete();

		//Debug.Log("ClientLobbyComponent::Update connection.IsCreated = " + connection.IsCreated);

		if (!connection.IsCreated)
		{
			Debug.Log("ClientLobbyComponent::Update Something went wrong during connect");
			return;
		}

		// ***** Receive data *****
		HandleReceiveData(ref connection, ref driver, m_AllPlayerInfo);

		// ***** Process data *****

		// ***** Update UI ****
		UpdateUI();

		// ***** Accept Player Input ****

		// ***** Send data *****
		HandleSendData(ref connection, ref driver, m_AllPlayerInfo);
	}

	private void HandleReceiveData(ref NetworkConnection connection, ref UdpCNetworkDriver driver, List<LobbyPlayerInfo> allPlayerInfo)
	{
		//Debug.Log("ClientLobbyComponent::HandleReceiveData Called");

		NetworkEvent.Type cmd;
		DataStreamReader stream;
		while ((cmd = connection.PopEvent(driver, out stream)) !=
			NetworkEvent.Type.Empty)
		{
			if (cmd == NetworkEvent.Type.Connect)
			{
				Debug.Log("ClientLobbyComponent::HandleReceiveData We are now connected to the server");

				// Get ID
				sendQueue.Enqueue((byte)LOBBY_CLIENT_REQUESTS.GET_ID);
			}
			else if (cmd == NetworkEvent.Type.Data)
			{
				ReadServerBytes(allPlayerInfo, stream);
			}
			else if (cmd == NetworkEvent.Type.Disconnect)
			{
				Debug.Log("ClientLobbyComponent::HandleReceiveData Client got disconnected from server");
				connection = default;
			}
		}
	}

	private void ReadServerBytes(List<LobbyPlayerInfo> playerList, DataStreamReader stream)
	{
		var readerCtx = default(DataStreamReader.Context);


		Debug.Log("ClientLobbyComponent::ReadServerBytes stream.Length = " + stream.Length);

		byte[] bytes = stream.ReadBytesAsArray(ref readerCtx, stream.Length);

		// Must always manually move index for bytes
		for (int i = 0; i < stream.Length;)
		{
			// Unsafely assuming that everything is working as expected and there are no attackers.
			byte serverCmd = bytes[i];
			++i;

			Debug.Log("ClientLobbyComponent::ReadServerBytes Got " + serverCmd + " from the Server");

			if (serverCmd == (byte)LOBBY_SERVER_COMMANDS.READY)
			{
				byte readyStatus = bytes[i];
				++i;

				int playerIndex = IdToIndexDictionary[m_PlayerID];
				playerList[playerIndex].isReady = readyStatus;

				Debug.Log("ClientLobbyComponent::ReadServerBytes Client ready state set to " + readyStatus);
			}
			else if (serverCmd == (byte)LOBBY_SERVER_COMMANDS.CHANGE_TEAM)
			{
				byte newTeam = bytes[i];
				++i;

				int playerIndex = IdToIndexDictionary[m_PlayerID];
				playerList[playerIndex].team = newTeam;

				Debug.Log("ClientLobbyComponent::ReadServerBytes Client team was set to " + newTeam);
			}
			else if (serverCmd == (byte)LOBBY_SERVER_COMMANDS.SET_ID)
			{
				byte newID = bytes[i];
				++i;

				m_PlayerID = newID;

				Debug.Log("ClientLobbyComponent::ReadServerBytes Client ID was set to " + m_PlayerID);
			}
			else if (serverCmd == (byte)LOBBY_SERVER_COMMANDS.SET_ALL_PLAYER_STATES)
			{
				byte numPlayers = bytes[i];
				++i;

				// Do a for loop iterating over the bytes using the same counter
				for (int player = 0; player < numPlayers; ++player)
				{
					// Unsafely assuming that everything is working as expected and there are no attackers.
					Debug.Log("ClientLobbyComponent::ReadServerBytes Data for client " + player + " received");

					byte isReady = bytes[i];
					++i;
					byte team = bytes[i];
					++i;
					byte playerType = bytes[i];
					++i;
					byte playerId = bytes[i];
					++i;

					if (playerList[player] == null)
					{
						playerList[player] = new LobbyPlayerInfo();
					}

					playerList[player].isReady = isReady;
					playerList[player].team = team;
					playerList[player].playerType = (PLAYER_TYPE)playerType;
					playerList[player].playerID = playerId;
					playerList[player].name = "Player " + playerId;

					/*
					if (IdToIndexDictionary.ContainsKey(playerId))
					{
						IdToIndexDictionary.Remove(playerId);
					}

					if (IndexToIdDictionary.ContainsKey(player))
					{
						IndexToIdDictionary.Remove(player);
					}

					IdToIndexDictionary.Add(playerId, player);
					IndexToIdDictionary.Add(player, playerId);
					*/
				}

				for (int player = numPlayers; player < ServerLobbyComponent.MAX_NUM_PLAYERS; ++player)
				{
					playerList[player] = null;
				}

				// Update Dictionaries after all the player data has been received
				IdToIndexDictionary.Clear();
				IndexToIdDictionary.Clear();

				for (int index = 0; index < playerList.Count; ++index)
				{
					if (playerList[index] == null)
					{
						continue;
					}

					IdToIndexDictionary.Add(playerList[index].playerID, index);
					IndexToIdDictionary.Add(index, playerList[index].playerID);
				}
			}
			else if (serverCmd == (byte)LOBBY_SERVER_COMMANDS.START_GAME)
			{
				SceneManager.LoadScene(PLAY_SCENE);
			}
		}
	}

	private void UpdateUI()
	{
		// Prototype hack to get UI up and running.
		// Should be replaced with more efficient and maintainable code later

		const string PLAYER_STATS = "PlayerStats";
		const string TEAM_1_STATS = "Team1Stats";
		const string TEAM_2_STATS = "Team2Stats";

		GameObject playerStatsObj = lobbyUIInstance.transform.Find(PLAYER_STATS).gameObject;

		GameObject team1Obj = playerStatsObj.transform.Find(TEAM_1_STATS).gameObject;
		GameObject team2Obj = playerStatsObj.transform.Find(TEAM_2_STATS).gameObject;

		// Go through all players looking for team players and then updating UI.
		SetTeamUI(team1Obj, 0);
		SetTeamUI(team2Obj, 1);
	}

	private void SetTeamUI(GameObject teamObj, int team)
	{
		const string PLAYER_PREFIX = "Player ";

		const string NAME_TEXT = "Name Text";
		const string READY_TEXT = "Ready Text";
		const string PLAYER_TYPE_TEXT = "Player Type Text";

		// Go through all players looking for team 2 players and then updating UI.
		int numTeamFound = 0;
		for (int i = 0; i < ServerLobbyComponent.MAX_NUM_PLAYERS; ++i)
		{
			if (m_AllPlayerInfo[i] != null && m_AllPlayerInfo[i].team == team)
			{
				numTeamFound++;

				// Set UI element active in case it was disabled before
				GameObject playerObj = teamObj.transform.Find(PLAYER_PREFIX + numTeamFound).gameObject;
				playerObj.SetActive(true);

				GameObject readyTextObj = playerObj.transform.Find(READY_TEXT).gameObject;
				if (m_AllPlayerInfo[i].isReady == 0)
				{
					readyTextObj.GetComponent<Text>().text = "Not Ready";
				}
				else
				{
					readyTextObj.GetComponent<Text>().text = "Ready";
				}

				GameObject nameTextObj = playerObj.transform.Find(NAME_TEXT).gameObject;
				nameTextObj.GetComponent<Text>().text = m_AllPlayerInfo[i].name;

				GameObject playerTypeTextObj = playerObj.transform.Find(PLAYER_TYPE_TEXT).gameObject;
				playerTypeTextObj.GetComponent<Text>().text = m_AllPlayerInfo[i].playerType.ToString();
			}
		}

		// Disable unused player slots to make UI easier to debug and understand at a glance
		for (int i = numTeamFound; i < ServerLobbyComponent.MAX_NUM_PLAYERS / 2; ++i)
		{
			GameObject playerObj = teamObj.transform.Find(PLAYER_PREFIX + (i + 1)).gameObject;
			playerObj.SetActive(false);
		}
	}

	private void HandleSendData(ref NetworkConnection connection, ref UdpCNetworkDriver driver, List<LobbyPlayerInfo> allPlayerInfo)
	{
		if (sendQueue.Count <= 0)
		{
			return;
		}

		// Send eveyrthing in the queue
		using (var writer = new DataStreamWriter(sendQueue.Count, Allocator.Temp))
		{
			while (sendQueue.Count > 0)
			{
				writer.Write(sendQueue.Dequeue());
			}

			connection.Send(driver, writer);
		}
	}
}

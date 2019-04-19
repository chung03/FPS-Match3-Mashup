using System.Net;
using System.Collections.Generic;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using UnityEngine.Assertions;
using Util;
using UnityEngine.SceneManagement;


public class ClientLobbyComponent : MonoBehaviour
{
	private static readonly string PLAY_SCENE = "Assets/Scenes/PlayScene.unity";

	// This is the player's ID. This is all it needs to identify itself and to find its player info.
	private int m_PlayerID;

	// This stores the infor for all players, including this one.
	private List<LobbyPlayerInfo> m_AllPlayerInfo;

	// A Pair of Dictionaries to make it easier to map Index and PlayerID
	// ID -> Connection Index
	private Dictionary<int, int> IdToIndexDictionary;
	// Connection Index -> ID
	private Dictionary<int, int> IndexToIdDictionary;

	private ClientConnectionsComponent connectionsComponent;
	private ClientLobbySend clientLobbySend;

	[SerializeField]
	private GameObject lobbyUIObj;
	private LobbyUIBehaviour lobbyUIInstance;


	private void Start()
	{
		//Debug.Log("ClientLobbyComponent::Start Called");
		m_PlayerID = -1;
		m_AllPlayerInfo = new List<LobbyPlayerInfo>(ServerLobbyComponent.MAX_NUM_PLAYERS);
		for (int index = 0; index < ServerLobbyComponent.MAX_NUM_PLAYERS; ++index)
		{
			m_AllPlayerInfo.Add(null);
		}

		lobbyUIInstance = Instantiate(lobbyUIObj).GetComponent<LobbyUIBehaviour>();
		lobbyUIInstance.SetUI(connectionsComponent.IsHost());
		lobbyUIInstance.Init(this);


		IdToIndexDictionary = new Dictionary<int, int>();
		IndexToIdDictionary = new Dictionary<int, int>();
	}

	public void Init(ClientConnectionsComponent connHolder)
	{
		//Debug.Log("ClientLobbyComponent::Init Called");
		connectionsComponent = connHolder;
		clientLobbySend = GetComponent<ClientLobbySend>();
	}
	
	///////////////////////////////////////////////
	// These functions are for hooking into the lobby UI
	public void ChangeTeam()
	{
		clientLobbySend.SendDataWhenReady((byte)LOBBY_CLIENT_REQUESTS.CHANGE_TEAM);
	}

	public void ChangeReadyStatus()
	{
		clientLobbySend.SendDataWhenReady((byte)LOBBY_CLIENT_REQUESTS.READY);
	}

	public void ChangePlayerType()
	{
		clientLobbySend.SendDataWhenReady((byte)LOBBY_CLIENT_REQUESTS.CHANGE_PLAYER_TYPE);
	}

	public void SendStartGame()
	{
		clientLobbySend.SendDataWhenReady((byte)LOBBY_CLIENT_REQUESTS.START_GAME);
	}
	///////////////////////////////////////////////

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

		// ***** Update UI ****
		lobbyUIInstance.UpdateUI(m_AllPlayerInfo);

		// ***** Send data *****
		clientLobbySend.SendDataIfReady(ref connection, ref driver, m_AllPlayerInfo);
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
				clientLobbySend.SendDataWhenReady((byte)LOBBY_CLIENT_REQUESTS.GET_ID);
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

					if (playerList[player] == null)
					{
						playerList[player] = new LobbyPlayerInfo();
					}

					byte playerDiffMask = bytes[i];
					++i;

					if ((playerDiffMask & CONSTANTS.PLAYER_ID_MASK) > 0)
					{
						playerList[player].playerID = bytes[i];
						++i;
					}

					if ((playerDiffMask & CONSTANTS.PLAYER_TYPE_MASK) > 0)
					{
						playerList[player].playerType = (PLAYER_TYPE)bytes[i];
						++i;
					}

					if ((playerDiffMask & CONSTANTS.READY_MASK) > 0)
					{
						playerList[player].isReady = bytes[i];
						++i;
					}

					if ((playerDiffMask & CONSTANTS.TEAM_MASK) > 0)
					{
						playerList[player].team = bytes[i];
						++i;
					}

					playerList[player].name = "Player " + playerList[player].playerID;
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
			else if (serverCmd == (byte)LOBBY_SERVER_COMMANDS.HEARTBEAT)
			{
				Debug.Log("ClientLobbyComponent::ReadServerBytes Received heartbeat from server");
			}
		}
	}
}

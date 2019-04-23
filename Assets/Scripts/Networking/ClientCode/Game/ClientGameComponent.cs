using System.Collections.Generic;
using UnityEngine;

using Unity.Networking.Transport;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using LobbyUtils;
using UnityEngine.SceneManagement;

using System.Text;

public class ClientGameComponent : MonoBehaviour
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
	private ClientGameSend clientGameSend;

	[SerializeField]
	private GameObject gameUIObj;
	private GameUIBehaviour gameUIInstance;


	private void Start()
	{
		//Debug.Log("ClientGameComponent::Start Called");
		m_PlayerID = -1;
		m_AllPlayerInfo = new List<LobbyPlayerInfo>(CONSTANTS.MAX_NUM_PLAYERS);
		for (int index = 0; index < CONSTANTS.MAX_NUM_PLAYERS; ++index)
		{
			m_AllPlayerInfo.Add(null);
		}

		gameUIInstance = Instantiate(gameUIObj).GetComponent<GameUIBehaviour>();
		gameUIInstance.SetUI(connectionsComponent.IsHost());
		gameUIInstance.Init(this);


		IdToIndexDictionary = new Dictionary<int, int>();
		IndexToIdDictionary = new Dictionary<int, int>();
	}

	public void Init(ClientConnectionsComponent connHolder)
	{
		//Debug.Log("ClientGameComponent::Init Called");
		connectionsComponent = connHolder;
		clientGameSend = GetComponent<ClientGameSend>();
	}

	///////////////////////////////////////////////
	// These functions are for hooking into the lobby UI
	public void ChangeTeam()
	{
		clientGameSend.SendDataWhenReady((byte)LOBBY_CLIENT_REQUESTS.CHANGE_TEAM);
	}

	public void ChangeReadyStatus()
	{
		clientGameSend.SendDataWhenReady((byte)LOBBY_CLIENT_REQUESTS.READY);
	}

	public void ChangePlayerType()
	{
		clientGameSend.SendDataWhenReady((byte)LOBBY_CLIENT_REQUESTS.CHANGE_PLAYER_TYPE);
	}

	public void SendStartGame()
	{
		clientGameSend.SendDataWhenReady((byte)LOBBY_CLIENT_REQUESTS.START_GAME);
	}
	///////////////////////////////////////////////

	void Update()
	{
		ref UdpCNetworkDriver driver = ref connectionsComponent.GetDriver();
		ref NetworkConnection connection = ref connectionsComponent.GetConnection();

		driver.ScheduleUpdate().Complete();

		//Debug.Log("ClientGameComponent::Update connection.IsCreated = " + connection.IsCreated);

		if (!connection.IsCreated)
		{
			Debug.Log("ClientGameComponent::Update Something went wrong during connect");
			return;
		}

		// ***** Receive data *****
		HandleReceiveData(ref connection, ref driver, m_AllPlayerInfo);

		// ***** Update UI ****
		gameUIInstance.UpdateUI(m_AllPlayerInfo);

		// ***** Send data *****
		clientGameSend.SendDataIfReady(ref connection, ref driver, m_AllPlayerInfo);
	}

	private void HandleReceiveData(ref NetworkConnection connection, ref UdpCNetworkDriver driver, List<LobbyPlayerInfo> allPlayerInfo)
	{
		//Debug.Log("ClientGameComponent::HandleReceiveData Called");

		NetworkEvent.Type cmd;
		DataStreamReader stream;
		while ((cmd = connection.PopEvent(driver, out stream)) !=
			NetworkEvent.Type.Empty)
		{
			if (cmd == NetworkEvent.Type.Connect)
			{
				Debug.Log("ClientGameComponent::HandleReceiveData We are now connected to the server");

				// Get ID
				clientGameSend.SendDataWhenReady((byte)LOBBY_CLIENT_REQUESTS.GET_ID);
			}
			else if (cmd == NetworkEvent.Type.Data)
			{
				ReadServerBytes(allPlayerInfo, stream);
			}
			else if (cmd == NetworkEvent.Type.Disconnect)
			{
				Debug.Log("ClientGameComponent::HandleReceiveData Client got disconnected from server");
				connection = default;
			}
		}
	}

	private void ReadServerBytes(List<LobbyPlayerInfo> playerList, DataStreamReader stream)
	{
		var readerCtx = default(DataStreamReader.Context);

		Debug.Log("ClientGameComponent::ReadServerBytes stream.Length = " + stream.Length);

		byte[] bytes = stream.ReadBytesAsArray(ref readerCtx, stream.Length);

		// Must always manually move index for bytes
		for (int i = 0; i < stream.Length;)
		{
			// Unsafely assuming that everything is working as expected and there are no attackers.
			byte serverCmd = bytes[i];
			++i;

			Debug.Log("ClientGameComponent::ReadServerBytes Got " + serverCmd + " from the Server");

			if (serverCmd == (byte)LOBBY_SERVER_COMMANDS.READY)
			{
				i += HandleReadyCommand(i, bytes, playerList);
			}
			else if (serverCmd == (byte)LOBBY_SERVER_COMMANDS.CHANGE_TEAM)
			{
				i += HandleChangeTeamCommand(i, bytes, playerList);
			}
			else if (serverCmd == (byte)LOBBY_SERVER_COMMANDS.SET_ID)
			{
				i += HandleSetIdCommand(i, bytes, playerList);
			}
			else if (serverCmd == (byte)LOBBY_SERVER_COMMANDS.SET_ALL_PLAYER_STATES)
			{
				i += HandlePlayerStatesCommand(i, bytes, playerList);
			}
			else if (serverCmd == (byte)LOBBY_SERVER_COMMANDS.START_GAME)
			{
				SceneManager.LoadScene(PLAY_SCENE);
			}
			else if (serverCmd == (byte)LOBBY_SERVER_COMMANDS.HEARTBEAT)
			{
				Debug.Log("ClientGameComponent::ReadServerBytes Received heartbeat from server");
			}
		}
	}

	// Returns the number of bytes read from the bytes array
	private int HandleReadyCommand(int index, byte[] bytes, List<LobbyPlayerInfo> playerList)
	{
		int bytesRead = 0;

		byte readyStatus = bytes[index];
		++bytesRead;

		int playerIndex = IdToIndexDictionary[m_PlayerID];
		playerList[playerIndex].isReady = readyStatus;

		Debug.Log("ClientGameComponent::HandleReadyCommand Client ready state set to " + readyStatus);

		return bytesRead;
	}

	private int HandleChangeTeamCommand(int index, byte[] bytes, List<LobbyPlayerInfo> playerList)
	{
		int bytesRead = 0;

		byte newTeam = bytes[index];
		++bytesRead;

		int playerIndex = IdToIndexDictionary[m_PlayerID];
		playerList[playerIndex].team = newTeam;

		Debug.Log("ClientGameComponent::HandleChangeTeamCommand Client team was set to " + newTeam);

		return bytesRead;
	}

	private int HandleSetIdCommand(int index, byte[] bytes, List<LobbyPlayerInfo> playerList)
	{
		int bytesRead = 0;

		byte newID = bytes[index];
		++bytesRead;

		m_PlayerID = newID;

		Debug.Log("ClientGameComponent::HandleSetIdCommand Client ID was set to " + m_PlayerID);

		return bytesRead;
	}


	private int HandlePlayerStatesCommand(int index, byte[] bytes, List<LobbyPlayerInfo> playerList)
	{
		int bytesRead = 0;

		byte numPlayers = bytes[index];
		++bytesRead;

		// Do a for loop iterating over the bytes using the same counter
		for (int player = 0; player < numPlayers; ++player)
		{
			// Unsafely assuming that everything is working as expected and there are no attackers.
			Debug.Log("ClientGameComponent::HandlePlayerStatesCommand Data for client " + player + " received");

			if (playerList[player] == null)
			{
				playerList[player] = new LobbyPlayerInfo();
			}

			byte playerDiffMask = bytes[index + bytesRead];
			++bytesRead;

			if ((playerDiffMask & CONSTANTS.NAME_MASK) > 0)
			{
				// Get length of name
				byte nameBytesLength = bytes[index + bytesRead];
				++bytesRead;

				// Extract name into byte array
				byte[] nameAsBytes = new byte[nameBytesLength];
				for (int nameIndex = 0; nameIndex < nameBytesLength; ++nameIndex)
				{
					nameAsBytes[nameIndex] = bytes[index + bytesRead];
					++bytesRead;
				}

				// Convert from bytes to string
				playerList[player].name = Encoding.UTF8.GetString(nameAsBytes);
			}

			if ((playerDiffMask & CONSTANTS.PLAYER_ID_MASK) > 0)
			{
				playerList[player].playerID = bytes[index + bytesRead];
				++bytesRead;
			}

			if ((playerDiffMask & CONSTANTS.PLAYER_TYPE_MASK) > 0)
			{
				playerList[player].playerType = (PLAYER_TYPE)bytes[index + bytesRead];
				++bytesRead;
			}

			if ((playerDiffMask & CONSTANTS.READY_MASK) > 0)
			{
				playerList[player].isReady = bytes[index + bytesRead];
				++bytesRead;
			}

			if ((playerDiffMask & CONSTANTS.TEAM_MASK) > 0)
			{
				playerList[player].team = bytes[index + bytesRead];
				++bytesRead;
			}
		}

		for (int player = numPlayers; player < CONSTANTS.MAX_NUM_PLAYERS; ++player)
		{
			playerList[player] = null;
		}

		// Update Dictionaries after all the player data has been received
		IdToIndexDictionary.Clear();
		IndexToIdDictionary.Clear();

		for (int playerIndex = 0; playerIndex < playerList.Count; ++playerIndex)
		{
			if (playerList[playerIndex] == null)
			{
				continue;
			}

			IdToIndexDictionary.Add(playerList[playerIndex].playerID, playerIndex);
			IndexToIdDictionary.Add(playerIndex, playerList[playerIndex].playerID);
		}

		return bytesRead;
	}
}

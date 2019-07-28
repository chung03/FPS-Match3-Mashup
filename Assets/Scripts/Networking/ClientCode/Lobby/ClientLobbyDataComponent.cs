using System.Collections.Generic;
using UnityEngine;
using LobbyUtils;
using CommonNetworkingUtils;
using UnityEngine.SceneManagement;

using Unity.Networking.Transport;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using System.Text;

public class ClientLobbyDataComponent : MonoBehaviour
{
	private static readonly string PLAY_SCENE = "Assets/Scenes/PlayScene.unity";

	// This is the player's ID. This is all it needs to identify itself and to find its player info.
	private int m_PlayerID;

	// This stores the infor for all players, including this one.
	private List<PersistentPlayerInfo> m_AllPlayerInfo;

	// A Pair of Dictionaries to make it easier to map Index and PlayerID
	// ID -> Connection Index
	private Dictionary<int, int> IdToIndexDictionary;
	// Connection Index -> ID
	private Dictionary<int, int> IndexToIdDictionary;

	private Dictionary<int, ClientHandleIncomingBytes> CommandToFunctionDictionary;

	private ClientConnectionsComponent connectionsComponent;
	private ClientLobbySend clientLobbySend;

	[SerializeField]
	private GameObject lobbyUIObj;
	private LobbyUIBehaviour lobbyUIInstance;

	private void Start()
	{
		//Debug.Log("ClientLobbyDataComponent::Start Called");
		m_PlayerID = -1;
		m_AllPlayerInfo = new List<PersistentPlayerInfo>(CONSTANTS.MAX_NUM_PLAYERS);
		for (int index = 0; index < CONSTANTS.MAX_NUM_PLAYERS; ++index)
		{
			m_AllPlayerInfo.Add(null);
		}

		lobbyUIInstance = Instantiate(lobbyUIObj).GetComponent<LobbyUIBehaviour>();
		lobbyUIInstance.SetUI(connectionsComponent.IsHost());
		lobbyUIInstance.Init(this);

		// Initialize the byteHandling Table
		CommandToFunctionDictionary = new Dictionary<int, ClientHandleIncomingBytes>();
		CommandToFunctionDictionary.Add((int)LOBBY_SERVER_COMMANDS.READY, HandleReadyCommand);
		CommandToFunctionDictionary.Add((int)LOBBY_SERVER_COMMANDS.CHANGE_TEAM, HandleChangeTeamCommand);
		CommandToFunctionDictionary.Add((int)LOBBY_SERVER_COMMANDS.SET_ID, HandleSetIdCommand);
		CommandToFunctionDictionary.Add((int)LOBBY_SERVER_COMMANDS.SET_ALL_PLAYER_STATES, HandlePlayerStatesCommand);
		CommandToFunctionDictionary.Add((int)LOBBY_SERVER_COMMANDS.START_GAME, HandleStartCommand);
		CommandToFunctionDictionary.Add((int)LOBBY_SERVER_COMMANDS.HEARTBEAT, HandleHeartBeat);


		IdToIndexDictionary = new Dictionary<int, int>();
		IndexToIdDictionary = new Dictionary<int, int>();
	}

	public void Init(ClientConnectionsComponent connHolder)
	{
		connectionsComponent = connHolder;
		clientLobbySend = GetComponent<ClientLobbySend>();
	}

	private void Update()
	{
		ref UdpCNetworkDriver driver = ref connectionsComponent.GetDriver();
		ref NetworkConnection connection = ref connectionsComponent.GetConnection();

		// ***** Update UI ****
		lobbyUIInstance.UpdateUI(m_AllPlayerInfo);

		// ***** Send data *****
		clientLobbySend.SendDataIfReady(ref connection, ref driver, m_AllPlayerInfo);
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

	public void ChangeName(string _name)
	{
		clientLobbySend.SendDataWhenReady((byte)LOBBY_CLIENT_REQUESTS.CHANGE_NAME);

		byte[] nameBytes = Encoding.UTF8.GetBytes(_name);

		clientLobbySend.SendDataWhenReady((byte)nameBytes.Length);
		for (int i = 0; i < nameBytes.Length; ++i)
		{
			clientLobbySend.SendDataWhenReady(nameBytes[i]);
		}
	}

	public void SendStartGame()
	{
		clientLobbySend.SendDataWhenReady((byte)LOBBY_CLIENT_REQUESTS.START_GAME);
	}

	// Returns the number of bytes read from the bytes array
	public int HandleStartCommand(int index, byte[] bytes)
	{
		int playerIndex = IdToIndexDictionary[m_PlayerID];
		connectionsComponent.SavePlayerInfo(m_AllPlayerInfo[playerIndex]);
		SceneManager.LoadScene(PLAY_SCENE);
		return 0;
	}

	public int HandleHeartBeat(int index, byte[] bytes)
	{
		Debug.Log("ClientLobbyDataComponent::ReadServerBytes Received heartbeat from server");
		return 0;
	}

	// Returns the number of bytes read from the bytes array
	public int HandleReadyCommand(int index, byte[] bytes)
	{
		int bytesRead = 0;

		byte readyStatus = bytes[index];
		++bytesRead;

		int playerIndex = IdToIndexDictionary[m_PlayerID];
		m_AllPlayerInfo[playerIndex].isReady = readyStatus;

		Debug.Log("ClientLobbyDataComponent::HandleReadyCommand Client ready state set to " + readyStatus);

		return bytesRead;
	}

	public int HandleChangeTeamCommand(int index, byte[] bytes)
	{
		int bytesRead = 0;

		byte newTeam = bytes[index];
		++bytesRead;

		int playerIndex = IdToIndexDictionary[m_PlayerID];
		m_AllPlayerInfo[playerIndex].team = newTeam;

		Debug.Log("ClientLobbyDataComponent::HandleChangeTeamCommand Client team was set to " + newTeam);

		return bytesRead;
	}

	public int HandleSetIdCommand(int index, byte[] bytes)
	{
		int bytesRead = 0;

		byte newID = bytes[index];
		++bytesRead;

		m_PlayerID = newID;

		Debug.Log("ClientLobbyDataComponent::HandleSetIdCommand Client ID was set to " + m_PlayerID);

		return bytesRead;
	}


	public int HandlePlayerStatesCommand(int index, byte[] bytes)
	{
		int initialIndex = index;

		byte numPlayers = bytes[index];
		++index;

		// Do a for loop iterating over the bytes using the same counter
		for (int player = 0; player < numPlayers; ++player)
		{
			// Unsafely assuming that everything is working as expected and there are no attackers.
			Debug.Log("ClientLobbyDataComponent::HandlePlayerStatesCommand Data for client " + player + " received");

			if (m_AllPlayerInfo[player] == null)
			{
				m_AllPlayerInfo[player] = new PersistentPlayerInfo();
			}

			byte playerDeltaSize = bytes[index];
			++index;

			byte[] deltaBytes = new byte[playerDeltaSize];

			System.Array.Copy(bytes, index, deltaBytes, 0, playerDeltaSize);

			m_AllPlayerInfo[player].ApplyDelta(deltaBytes, false);

			index += playerDeltaSize;
		}

		for (int player = numPlayers; player < CONSTANTS.MAX_NUM_PLAYERS; ++player)
		{
			m_AllPlayerInfo[player] = null;
		}

		// Update Dictionaries after all the player data has been received
		IdToIndexDictionary.Clear();
		IndexToIdDictionary.Clear();

		for (int playerIndex = 0; playerIndex < m_AllPlayerInfo.Count; ++playerIndex)
		{
			if (m_AllPlayerInfo[playerIndex] == null)
			{
				continue;
			}

			IdToIndexDictionary.Add(m_AllPlayerInfo[playerIndex].playerID, playerIndex);
			IndexToIdDictionary.Add(playerIndex, m_AllPlayerInfo[playerIndex].playerID);
		}

		return index - initialIndex;
	}

	public void ProcessServerBytes(byte[] bytes)
	{
		// Must always manually move index for bytes
		for (int i = 0; i < bytes.Length;)
		{
			// Unsafely assuming that everything is working as expected and there are no attackers.
			byte serverCmd = bytes[i];
			++i;

			Debug.Log("ClientLobbyComponent::ReadServerBytes Got " + serverCmd + " from the Server");

			i += CommandToFunctionDictionary[serverCmd](i, bytes);
		}
	}
}

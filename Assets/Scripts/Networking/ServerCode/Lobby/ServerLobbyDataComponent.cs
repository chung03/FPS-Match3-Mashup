using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonNetworkingUtils;
using Unity.Networking.Transport;
using Unity.Collections;



using UnityEngine.Assertions;
using LobbyUtils;

public class ServerLobbyDataComponent : MonoBehaviour
{
	private enum LOBBY_SERVER_PROCESS
	{
		START_GAME,
		CHANGE_PLAYER_TYPE
	}

	// Current Player States
	public List<PersistentPlayerInfo> m_PlayerList;

	// Used for calculating deltas and ultimately save on network bandwidth
	public List<PersistentPlayerInfo> m_PreviousStatePlayerList;

	// A Pair of Dictionaries to make it easier to map Index and PlayerID
	// ID -> Connection Index
	private Dictionary<byte, int> IdToIndexDictionary;
	// Connection Index -> ID
	private Dictionary<int, byte> IndexToIdDictionary;

	// Command and the player ID
	private Queue<KeyValuePair<LOBBY_SERVER_PROCESS, int>> commandProcessingQueue;

	private ServerConnectionsComponent connectionsComponent;
	private ServerLobbySend serverLobbySend;

	int numTeam1Players = 0;
	int numTeam2Players = 0;

	private Dictionary<LOBBY_CLIENT_REQUESTS, ServerHandleIncomingBytes> CommandToFunctionDictionary;

	private void Start()
	{
		//Debug.Log("ServerLobbyDataComponent::Start Called");
		m_PlayerList = new List<PersistentPlayerInfo>(CONSTANTS.MAX_NUM_PLAYERS);
		m_PreviousStatePlayerList = new List<PersistentPlayerInfo>(CONSTANTS.MAX_NUM_PLAYERS);

		IdToIndexDictionary = new Dictionary<byte, int>();
		IndexToIdDictionary = new Dictionary<int, byte>();

		commandProcessingQueue = new Queue<KeyValuePair<LOBBY_SERVER_PROCESS, int>>();

		CommandToFunctionDictionary = new Dictionary<LOBBY_CLIENT_REQUESTS, ServerHandleIncomingBytes>();
		CommandToFunctionDictionary.Add(LOBBY_CLIENT_REQUESTS.READY, ChangePlayerReady);
		CommandToFunctionDictionary.Add(LOBBY_CLIENT_REQUESTS.CHANGE_TEAM, ChangePlayerTeam);
		CommandToFunctionDictionary.Add(LOBBY_CLIENT_REQUESTS.CHANGE_PLAYER_TYPE, ChangePlayerType);
		CommandToFunctionDictionary.Add(LOBBY_CLIENT_REQUESTS.GET_ID, GetPlayerID);
		CommandToFunctionDictionary.Add(LOBBY_CLIENT_REQUESTS.START_GAME, StartGame);
		CommandToFunctionDictionary.Add(LOBBY_CLIENT_REQUESTS.CHANGE_NAME, ChangePlayerName);
		CommandToFunctionDictionary.Add(LOBBY_CLIENT_REQUESTS.HEARTBEAT, HeartBeat);
	}


	public void Init(ServerConnectionsComponent connHolder)
	{
		//Debug.Log("ServerLobbyDataComponent::Init Called");
		connectionsComponent = connHolder;
		serverLobbySend = GetComponent<ServerLobbySend>();
	}

	void Update()
	{
		ref UdpNetworkDriver driver = ref connectionsComponent.GetDriver();
		ref NativeList<NetworkConnection> connections = ref connectionsComponent.GetConnections();

		// ***** Process data *****
		ProcessData(ref connections, ref driver);

		// ***** Send data *****
		serverLobbySend.SendDataIfReady(ref connections, ref driver, m_PlayerList);
	}

	public void AddPlayerToTeam()
	{
		m_PlayerList.Add(new PersistentPlayerInfo());
		m_PlayerList[m_PlayerList.Count - 1].playerID = connectionsComponent.GetNextPlayerID();
		m_PlayerList[m_PlayerList.Count - 1].name = "Player " + m_PlayerList[m_PlayerList.Count - 1].playerID;
		IdToIndexDictionary.Add(m_PlayerList[m_PlayerList.Count - 1].playerID, m_PlayerList.Count - 1);
		IndexToIdDictionary.Add(m_PlayerList.Count - 1, m_PlayerList[m_PlayerList.Count - 1].playerID);

		// Automatically put the player on an empty team. Put on team 1 first if possible, otherwise put on team 2.
		if (numTeam1Players < 3)
		{
			m_PlayerList[m_PlayerList.Count - 1].team = 0;
			++numTeam1Players;
		}
		else if (numTeam2Players < 3)
		{
			m_PlayerList[m_PlayerList.Count - 1].team = 1;
			++numTeam2Players;
		}
		else
		{
			Debug.Log("ServerLobbyDataComponent::HandleConnections SHOULD NOT BE IN THIS STATE! TEAMS ARE FULL BUT THERE ARE LESS THAN MAX NUMBER OF PLAYERS CONNECTED? NOT POSSIBLE. numTeam1Players = " + numTeam1Players + ", numTeam2Players = " + numTeam2Players);
		}

		// Send all current player data to the new connection
		SendCurrentPlayerStateDataToNewPlayerWhenReady(m_PlayerList.Count - 1);
	}

	public void RemovePlayerFromTeam(int index)
	{
		// Correct number of players on team now.

		if (m_PlayerList[index].team == 0)
		{
			--numTeam1Players;
		}
		else if (m_PlayerList[index].team == 1)
		{
			--numTeam2Players;
		}
		else
		{
			Debug.Log("ServerLobbyDataComponent::HandleConnections Removing a player not one team 1 or team 2. playerList[i].team = " + m_PlayerList[index].team);
		}

		serverLobbySend.ResetIndividualPlayerQueue(index);

		m_PlayerList.RemoveAtSwapBack(index);

		IdToIndexDictionary.Clear();
		IndexToIdDictionary.Clear();

		for (int i = 0; i < m_PlayerList.Count; i++)
		{
			IdToIndexDictionary.Add(m_PlayerList[i].playerID, i);
			IndexToIdDictionary.Add(i, m_PlayerList[i].playerID);
		}
	}

	private int ChangePlayerReady(int index, byte[] bytes, int playerIndex)
	{
		if (m_PlayerList[playerIndex].isReady == 0)
		{
			m_PlayerList[playerIndex].isReady = 1;
		}
		else
		{
			m_PlayerList[playerIndex].isReady = 0;
		}

		Debug.Log("ServerLobbyDataComponent::ChangePlayerReady Player " + m_PlayerList[playerIndex].playerID + " ready state set to " + m_PlayerList[playerIndex].isReady);

		return 0;
	}

	private int ChangePlayerTeam(int index, byte[] bytes, int playerIndex)
	{
		if (m_PlayerList[playerIndex].team == 0 && numTeam2Players < 3)
		{
			m_PlayerList[playerIndex].team = 1;
			--numTeam1Players;
			++numTeam2Players;

			Debug.Log("ServerLobbyDataComponent::ChangePlayerTeam Client " + m_PlayerList[playerIndex].playerID + " team was set to 1");
		}
		else if (m_PlayerList[playerIndex].team == 1 && numTeam1Players < 3)
		{
			m_PlayerList[playerIndex].team = 0;
			++numTeam1Players;
			--numTeam2Players;

			Debug.Log("ServerLobbyDataComponent::ChangePlayerTeam Client " + m_PlayerList[playerIndex].playerID + " team was set to 0");
		}
		else
		{
			Debug.Log("ServerLobbyDataComponent::ChangePlayerTeam SHOULD NOT HAPPEN! Player " + m_PlayerList[playerIndex].playerID + " tried to change teams but something strange happened. players team = " + m_PlayerList[playerIndex].team + ", numTeam1Players = " + numTeam1Players + ", numTeam2Players = " + numTeam2Players);
		}

		return 0;
	}

	private int ChangePlayerType(int index, byte[] bytes, int playerIndex)
	{
		commandProcessingQueue.Enqueue(new KeyValuePair<LOBBY_SERVER_PROCESS, int>(LOBBY_SERVER_PROCESS.CHANGE_PLAYER_TYPE, playerIndex));

		return 0;
	}

	private int GetPlayerID(int index, byte[] bytes, int playerIndex)
	{
		Debug.Log("ServerLobbyDataComponent::GetPlayerID Client " + playerIndex + " sent request for its ID");

		serverLobbySend.SendDataToPlayerWhenReady((byte)LOBBY_SERVER_COMMANDS.SET_ID, playerIndex);
		serverLobbySend.SendDataToPlayerWhenReady(IndexToIdDictionary[playerIndex], playerIndex);

		return 0;
	}

	private int StartGame(int index, byte[] bytes, int playerIndex)
	{
		commandProcessingQueue.Enqueue(new KeyValuePair<LOBBY_SERVER_PROCESS, int>(LOBBY_SERVER_PROCESS.START_GAME, CONSTANTS.SEND_ALL_PLAYERS));

		return 0;
	}

	private int ChangePlayerName(int index, byte[] bytes, int playerIndex)
	{
		int afterStringReadIndex = index;

		m_PlayerList[playerIndex].name = DataUtils.ReadString(ref afterStringReadIndex, bytes);

		Debug.Log("ServerLobbyDataComponent::ChangePlayerName Client " + playerIndex + " name set to " + m_PlayerList[playerIndex].name);

		return afterStringReadIndex - index;
	}

	private int HeartBeat(int index, byte[] bytes, int playerIndex)
	{
		Debug.Log("ServerLobbyDataComponent::HeartBeat Client " + playerIndex + " sent heartbeat");
		serverLobbySend.SendDataToPlayerWhenReady((byte)LOBBY_SERVER_COMMANDS.HEARTBEAT, playerIndex);

		return 0;
	}

	public void ProcessData(ref NativeList<NetworkConnection> connections, ref UdpNetworkDriver driver)
	{
		while (commandProcessingQueue.Count > 0)
		{
			KeyValuePair<LOBBY_SERVER_PROCESS, int> processCommand = commandProcessingQueue.Dequeue();

			if (processCommand.Value == CONSTANTS.SEND_ALL_PLAYERS)
			{
				if (processCommand.Key == LOBBY_SERVER_PROCESS.START_GAME)
				{
					bool isReadytoStart = true;

					for (int playerNum = 0; playerNum < connections.Length; ++playerNum)
					{
						if (m_PlayerList[playerNum].playerType == PLAYER_TYPE.NONE
							|| m_PlayerList[playerNum].isReady == 0)
						{
							isReadytoStart = false;
						}
					}

					if (isReadytoStart)
					{
						connectionsComponent.SaveGameInfo(m_PlayerList);
						serverLobbySend.SendDataToPlayerWhenReady((byte)LOBBY_SERVER_COMMANDS.START_GAME, CONSTANTS.SEND_ALL_PLAYERS);
					}
				}
			}
			else
			{
				// Make sure that changing player types doesn't result in some invalid team configuration
				if (processCommand.Key == LOBBY_SERVER_PROCESS.CHANGE_PLAYER_TYPE)
				{
					int numShootersFound = 0;
					int numMatch3Found = 0;

					int currPlayerIndex = processCommand.Value;

					PersistentPlayerInfo currPlayer = m_PlayerList[currPlayerIndex];

					PLAYER_TYPE newplayerType = (PLAYER_TYPE)((int)(currPlayer.playerType + 1) % (int)PLAYER_TYPE.PLAYER_TYPES);

					// Get number of types of players on this player's team
					for (int playerIndex = 0; playerIndex < m_PlayerList.Count; ++playerIndex)
					{
						// Skip over self
						if (currPlayerIndex == playerIndex)
						{
							continue;
						}

						if (m_PlayerList[playerIndex].team == currPlayer.team)
						{
							if (m_PlayerList[playerIndex].playerType == PLAYER_TYPE.SHOOTER)
							{
								++numShootersFound;
							}
							else if (m_PlayerList[playerIndex].playerType == PLAYER_TYPE.MATCH3)
							{
								++numMatch3Found;
							}
						}
					}

					// Debug.Log("ServerLobbyDataComponent::ProcessData numShootersFound = " + numShootersFound + ", numMatch3Found = " + numMatch3Found);

					// Now figure out what type the player can be
					if (newplayerType == PLAYER_TYPE.SHOOTER
						&& numShootersFound >= 2)
					{
						newplayerType = (PLAYER_TYPE)((int)(newplayerType + 1) % (int)PLAYER_TYPE.PLAYER_TYPES);
					}

					if (newplayerType == PLAYER_TYPE.MATCH3
						&& numMatch3Found >= 1)
					{
						newplayerType = (PLAYER_TYPE)((int)(newplayerType + 1) % (int)PLAYER_TYPE.PLAYER_TYPES);
					}

					currPlayer.playerType = newplayerType;

					Debug.Log("ServerLobbyDataComponent::ProcessData Client " + currPlayerIndex + " player type was set to " + currPlayer.playerType.ToString());
				}
			}

		}
	}

	public void ProcessClientBytes(int playerIndex, byte[] bytes)
	{
		Debug.Log("ServerLobbyComponent::ReadClientBytes bytes.Length = " + bytes.Length);

		for (int i = 0; i < bytes.Length;)
		{
			LOBBY_CLIENT_REQUESTS clientCmd = (LOBBY_CLIENT_REQUESTS)bytes[i];

			// Unsafely assuming that everything is working as expected and there are no attackers.
			++i;
			
			Debug.Log("ServerLobbyComponent::ReadClientBytes Got " + clientCmd + " from the Client");

			i += CommandToFunctionDictionary[clientCmd](i, bytes, playerIndex);
		}
	}

	public void SendPlayerDiffs()
	{
		// No players, so nothing to do
		if (m_PlayerList.Count <= 0)
		{
			return;
		}

		// Send player state to all players
		// Calculate and send Delta

		serverLobbySend.SendDataToPlayerWhenReady((byte)LOBBY_SERVER_COMMANDS.SET_ALL_PLAYER_STATES, CONSTANTS.SEND_ALL_PLAYERS);
		serverLobbySend.SendDataToPlayerWhenReady((byte)m_PlayerList.Count, CONSTANTS.SEND_ALL_PLAYERS);

		// Send data for players that were here before
		for (int playerNum = 0; playerNum < Mathf.Min(m_PlayerList.Count, m_PreviousStatePlayerList.Count); playerNum++)
		{
			List<byte> deltaInfo = m_PlayerList[playerNum].GetDeltaBytes(false);
			m_PlayerList[playerNum].SetDeltaToZero();

			// Tell Client what changed
			serverLobbySend.SendDataToPlayerWhenReady(deltaInfo, CONSTANTS.SEND_ALL_PLAYERS);
		}

		// Send full data for new players
		SendFullPlayerState(m_PlayerList, CONSTANTS.SEND_ALL_PLAYERS, m_PreviousStatePlayerList.Count, m_PlayerList.Count);

		// Save previous state so that we can create deltas later
		m_PreviousStatePlayerList = DeepClone(m_PlayerList);
	}

	private void SendFullPlayerState(List<PersistentPlayerInfo> playerList, int connectionIndex, int beginningIndex, int endIndex)
	{
		// Send full data for new players
		for (int playerNum = beginningIndex; playerNum < endIndex; playerNum++)
		{
			List<byte> deltaInfo = playerList[playerNum].GetDeltaBytes(true);
			playerList[playerNum].SetDeltaToZero();

			serverLobbySend.SendDataToPlayerWhenReady(deltaInfo, connectionIndex);
		}
	}

	private List<PersistentPlayerInfo> DeepClone(List<PersistentPlayerInfo> list)
	{
		List<PersistentPlayerInfo> ret = new List<PersistentPlayerInfo>();

		foreach (PersistentPlayerInfo player in list)
		{
			ret.Add(player.Clone());
		}

		return ret;
	}

	// Send current player states to a new connection.
	// This will bring the connection up to date and able to use the deltas in the next update
	public void SendCurrentPlayerStateDataToNewPlayerWhenReady(int connectionIndex)
	{
		// Nothing to send if no players
		if (m_PreviousStatePlayerList.Count <= 0)
		{
			return;
		}

		serverLobbySend.SendDataToPlayerWhenReady((byte)LOBBY_SERVER_COMMANDS.SET_ALL_PLAYER_STATES, connectionIndex);
		serverLobbySend.SendDataToPlayerWhenReady((byte)m_PreviousStatePlayerList.Count, connectionIndex);
		SendFullPlayerState(m_PreviousStatePlayerList, connectionIndex, 0, m_PreviousStatePlayerList.Count);
	}
}

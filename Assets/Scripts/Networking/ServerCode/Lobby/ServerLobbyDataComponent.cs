using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonNetworkingUtils;
using Unity.Networking.Transport;
using Unity.Collections;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

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

	private Dictionary<int, ServerHandleIncomingBytes> CommandToFunctionDictionary;

	private void Start()
	{
		//Debug.Log("ServerLobbyDataComponent::Start Called");
		m_PlayerList = new List<PersistentPlayerInfo>(CONSTANTS.MAX_NUM_PLAYERS);

		IdToIndexDictionary = new Dictionary<byte, int>();
		IndexToIdDictionary = new Dictionary<int, byte>();

		commandProcessingQueue = new Queue<KeyValuePair<LOBBY_SERVER_PROCESS, int>>();
	}


	public void Init(ServerConnectionsComponent connHolder)
	{
		//Debug.Log("ServerLobbyDataComponent::Init Called");
		connectionsComponent = connHolder;
		serverLobbySend = GetComponent<ServerLobbySend>();
	}

	void Update()
	{
		ref UdpCNetworkDriver driver = ref connectionsComponent.GetDriver();
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
		serverLobbySend.SendCurrentPlayerStateDataToNewPlayerWhenReady(m_PlayerList.Count - 1);
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
			IdToIndexDictionary.Add(m_PlayerList[index].playerID, index);
			IndexToIdDictionary.Add(index, m_PlayerList[index].playerID);
		}
	}

	public int ChangePlayerReady(int index, byte[] bytes, int playerIndex)
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

	public int ChangePlayerTeam(int index, byte[] bytes, int playerIndex)
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

	public int ChangePlayerType(int index, byte[] bytes, int playerIndex)
	{
		commandProcessingQueue.Enqueue(new KeyValuePair<LOBBY_SERVER_PROCESS, int>(LOBBY_SERVER_PROCESS.CHANGE_PLAYER_TYPE, playerIndex));

		return 0;
	}

	public int GetPlayerID(int index, byte[] bytes, int playerIndex)
	{
		Debug.Log("ServerLobbyDataComponent::GetPlayerID Client " + playerIndex + " sent request for its ID");

		serverLobbySend.SendDataToPlayerWhenReady((byte)LOBBY_SERVER_COMMANDS.SET_ID, playerIndex);
		serverLobbySend.SendDataToPlayerWhenReady(IndexToIdDictionary[playerIndex], playerIndex);

		return 0;
	}

	public int StartGame(int index, byte[] bytes, int playerIndex)
	{
		commandProcessingQueue.Enqueue(new KeyValuePair<LOBBY_SERVER_PROCESS, int>(LOBBY_SERVER_PROCESS.START_GAME, CONSTANTS.SEND_ALL_PLAYERS));

		return 0;
	}

	public int ChangePlayerName(int index, byte[] bytes, int playerIndex)
	{
		int afterStringReadIndex = index;

		m_PlayerList[playerIndex].name = DataUtils.ReadString(ref afterStringReadIndex, bytes);

		Debug.Log("ServerLobbyDataComponent::ChangePlayerName Client " + playerIndex + " name set to " + m_PlayerList[playerIndex].name);

		return afterStringReadIndex - index;
	}

	public int HeartBeat(int index, byte[] bytes, int playerIndex)
	{
		Debug.Log("ServerLobbyDataComponent::HeartBeat Client " + playerIndex + " sent heartbeat");
		serverLobbySend.SendDataToPlayerWhenReady((byte)LOBBY_SERVER_COMMANDS.HEARTBEAT, playerIndex);

		return 0;
	}

	public void ProcessData(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver)
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
}

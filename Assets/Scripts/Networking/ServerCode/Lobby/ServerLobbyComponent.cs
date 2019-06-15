using System.Collections.Generic;
using UnityEngine;
using CommonNetworkingUtils;

using Unity.Networking.Transport;
using Unity.Collections;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using UnityEngine.Assertions;
using LobbyUtils;

public class ServerLobbyComponent : MonoBehaviour
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
		//Debug.Log("ServerLobbyComponent::Start Called");
		m_PlayerList = new List<PersistentPlayerInfo>(CONSTANTS.MAX_NUM_PLAYERS);

		IdToIndexDictionary = new Dictionary<byte, int>();
		IndexToIdDictionary = new Dictionary<int, byte>();

		commandProcessingQueue = new Queue<KeyValuePair<LOBBY_SERVER_PROCESS, int>>();

		CommandToFunctionDictionary = new Dictionary<int, ServerHandleIncomingBytes>();
		CommandToFunctionDictionary.Add((int)LOBBY_CLIENT_REQUESTS.READY, ChangePlayerReady);
		CommandToFunctionDictionary.Add((int)LOBBY_CLIENT_REQUESTS.CHANGE_TEAM, ChangePlayerTeam);
		CommandToFunctionDictionary.Add((int)LOBBY_CLIENT_REQUESTS.CHANGE_PLAYER_TYPE, ChangePlayerType);
		CommandToFunctionDictionary.Add((int)LOBBY_CLIENT_REQUESTS.GET_ID, GetPlayerID);
		CommandToFunctionDictionary.Add((int)LOBBY_CLIENT_REQUESTS.START_GAME, StartGame);
		CommandToFunctionDictionary.Add((int)LOBBY_CLIENT_REQUESTS.CHANGE_NAME, ChangePlayerName);
		CommandToFunctionDictionary.Add((int)LOBBY_CLIENT_REQUESTS.HEARTBEAT, HeartBeat);
	}


	public void Init(ServerConnectionsComponent connHolder)
	{
		//Debug.Log("ServerLobbyComponent::Init Called");
		connectionsComponent = connHolder;
		serverLobbySend = GetComponent<ServerLobbySend>();
	}

	void Update()
	{
		ref UdpCNetworkDriver driver = ref connectionsComponent.GetDriver();
		ref NativeList<NetworkConnection> connections = ref connectionsComponent.GetConnections();

		driver.ScheduleUpdate().Complete();

		// ***** Handle Connections *****
		HandleConnections(ref connections, ref driver, m_PlayerList);

		// ***** Receive data *****
		HandleReceiveData(ref connections, ref driver, m_PlayerList);

		// ***** Process data *****
		ProcessData(ref connections, ref driver, m_PlayerList);

		// ***** Send data *****
		serverLobbySend.SendDataIfReady(ref connections, ref driver, m_PlayerList);
	}

	private void HandleConnections(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver, List<PersistentPlayerInfo> playerList)
	{
		//Debug.Log("ServerLobbyComponent::HandleConnections Called");

		// Clean up connections
		bool connectionsChanged = false;
		for (int i = 0; i < connections.Length; i++)
		{
			if (!connections[i].IsCreated)
			{
				Debug.Log("ServerLobbyComponent::HandleConnections Removing a connection");
				connectionsChanged = true;


				// Correct number of players on team now.

				if (playerList[i].team == 0)
				{
					--numTeam1Players;
				}
				else if (playerList[i].team == 1)
				{
					--numTeam2Players;
				}
				else
				{
					Debug.Log("ServerLobbyComponent::HandleConnections Removing a player not one team 1 or team 2. playerList[i].team = " + playerList[i].team);
				}

				serverLobbySend.ResetIndividualPlayerQueue(i);

				connections.RemoveAtSwapBack(i);
				playerList.RemoveAtSwapBack(i);
				--i;
			}
		}

		// Update the dictionary with new IDs
		if (connectionsChanged)
		{
			IdToIndexDictionary.Clear();
			IndexToIdDictionary.Clear();

			for (int i = 0; i < playerList.Count; i++)
			{
				IdToIndexDictionary.Add(playerList[i].playerID, i);
				IndexToIdDictionary.Add(i, playerList[i].playerID);
			}
		}

		// Accept new connections
		NetworkConnection c;
		while ((c = driver.Accept()) != default)
		{
			if (connections.Length >= CONSTANTS.MAX_NUM_PLAYERS)
			{
				Debug.Log("ServerLobbyComponent::HandleConnections Too many connections, rejecting latest one");
				driver.Disconnect(c);
				continue;
			}

			Debug.Log("ServerLobbyComponent::HandleConnections Accepted a connection");

			connections.Add(c);
			playerList.Add(new PersistentPlayerInfo());
			playerList[playerList.Count - 1].playerID = connectionsComponent.GetNextPlayerID();
			playerList[playerList.Count - 1].name = "Player " + playerList[playerList.Count - 1].playerID;
			IdToIndexDictionary.Add(playerList[playerList.Count - 1].playerID, playerList.Count - 1);
			IndexToIdDictionary.Add(playerList.Count - 1, playerList[playerList.Count - 1].playerID);

			// Automatically put the player on an empty team. Put on team 1 first if possible, otherwise put on team 2.
			if (numTeam1Players < 3)
			{
				playerList[playerList.Count - 1].team = 0;
				++numTeam1Players;
			}
			else if (numTeam2Players < 3)
			{
				playerList[playerList.Count - 1].team = 1;
				++numTeam2Players;
			}
			else
			{
				Debug.Log("ServerLobbyComponent::HandleConnections SHOULD NOT BE IN THIS STATE! TEAMS ARE FULL BUT THERE ARE LESS THAN MAX NUMBER OF PLAYERS CONNECTED? NOT POSSIBLE. numTeam1Players = " + numTeam1Players + ", numTeam2Players = " + numTeam2Players);
			}

			// Send all current player data to the new connection
			serverLobbySend.SendCurrentPlayerStateDataToNewPlayerWhenReady(connections.Length - 1);
		}
	}

	private void HandleReceiveData(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver, List<PersistentPlayerInfo> playerList)
	{
		for (int index = 0; index < connections.Length; ++index)
		{
			if (!connections.IsCreated)
			{
				Debug.Log("ServerLobbyComponent::HandleReceiveData connections[" + index + "] was not created");
				Assert.IsTrue(true);
			}

			NetworkEvent.Type cmd;
			DataStreamReader stream;
			while ((cmd = driver.PopEventForConnection(connections[index], out stream)) !=
					NetworkEvent.Type.Empty)
			{
				if (cmd == NetworkEvent.Type.Data)
				{
					var readerCtx = default(DataStreamReader.Context);
					byte[] bytes = stream.ReadBytesAsArray(ref readerCtx, stream.Length);
					
					ReadClientBytes(index, playerList, bytes);
				}
				else if (cmd == NetworkEvent.Type.Disconnect)
				{
					Debug.Log("ServerLobbyComponent::HandleReceiveData Client disconnected from server");
					connections[index] = default;
				}
				else
				{
					Debug.Log("ServerLobbyComponent::HandleReceiveData Unhandled Network Event: " + cmd);
				}
			}

			//Debug.Log("ServerLobbyComponent::HandleReceiveData Finished processing connection[" + index + "]");
		}
	}

	private void ReadClientBytes(int playerIndex, List<PersistentPlayerInfo> playerList, byte[] bytes)
	{
		Debug.Log("ServerLobbyComponent::ReadClientBytes bytes.Length = " + bytes.Length);

		for (int i = 0; i < bytes.Length;)
		{
			byte clientCmd = bytes[i];

			// Unsafely assuming that everything is working as expected and there are no attackers.
			++i;

			Debug.Log("ServerLobbyComponent::ReadClientBytes Got " + clientCmd + " from the Client");

			CommandToFunctionDictionary[clientCmd](ref i, bytes, playerList, playerIndex);
		}
	}

	private void ChangePlayerReady(ref int index, byte[] bytes, List<PersistentPlayerInfo> playerInfo, int playerIndex)
	{
		if (playerInfo[playerIndex].isReady == 0)
		{
			playerInfo[playerIndex].isReady = 1;
		}
		else
		{
			playerInfo[playerIndex].isReady = 0;
		}

		Debug.Log("ServerLobbyComponent::ChangePlayerReady Player " + playerInfo[playerIndex].playerID + " ready state set to " + playerInfo[playerIndex].isReady);
	}

	private void ChangePlayerTeam(ref int index, byte[] bytes, List<PersistentPlayerInfo> playerInfo, int playerIndex)
	{
		if (playerInfo[playerIndex].team == 0 && numTeam2Players < 3)
		{
			playerInfo[playerIndex].team = 1;
			--numTeam1Players;
			++numTeam2Players;

			Debug.Log("ServerLobbyComponent::ChangePlayerTeam Client " + playerInfo[playerIndex].playerID + " team was set to 1");
		}
		else if (playerInfo[playerIndex].team == 1 && numTeam1Players < 3)
		{
			playerInfo[playerIndex].team = 0;
			++numTeam1Players;
			--numTeam2Players;

			Debug.Log("ServerLobbyComponent::ChangePlayerTeam Client " + playerInfo[playerIndex].playerID + " team was set to 0");
		}
		else
		{
			Debug.Log("ServerLobbyComponent::ChangePlayerTeam SHOULD NOT HAPPEN! Player " + playerInfo[playerIndex].playerID + " tried to change teams but something strange happened. players team = " + playerInfo[playerIndex].team + ", numTeam1Players = " + numTeam1Players + ", numTeam2Players = " + numTeam2Players);
		}
	}

	private void ChangePlayerType(ref int index, byte[] bytes, List<PersistentPlayerInfo> playerInfo, int playerIndex)
	{
		commandProcessingQueue.Enqueue(new KeyValuePair<LOBBY_SERVER_PROCESS, int>(LOBBY_SERVER_PROCESS.CHANGE_PLAYER_TYPE, playerIndex));
	}

	private void GetPlayerID(ref int index, byte[] bytes, List<PersistentPlayerInfo> playerInfo, int playerIndex)
	{
		Debug.Log("ServerLobbyComponent::GetPlayerID Client " + playerIndex + " sent request for its ID");

		serverLobbySend.SendDataToPlayerWhenReady((byte)LOBBY_SERVER_COMMANDS.SET_ID, playerIndex);
		serverLobbySend.SendDataToPlayerWhenReady(IndexToIdDictionary[playerIndex], playerIndex);
	}

	private void StartGame(ref int index, byte[] bytes, List<PersistentPlayerInfo> playerInfo, int playerIndex)
	{
		commandProcessingQueue.Enqueue(new KeyValuePair<LOBBY_SERVER_PROCESS, int>(LOBBY_SERVER_PROCESS.START_GAME, CONSTANTS.SEND_ALL_PLAYERS));
	}

	private void ChangePlayerName(ref int index, byte[] bytes, List<PersistentPlayerInfo> playerInfo, int playerIndex)
	{
		playerInfo[playerIndex].name = DataUtils.ReadString(ref index, bytes);

		Debug.Log("ServerLobbyComponent::ChangePlayerName Client " + playerIndex + " name set to " + playerInfo[playerIndex].name);
	}

	private void HeartBeat(ref int index, byte[] bytes, List<PersistentPlayerInfo> playerInfo, int playerIndex)
	{
		Debug.Log("ServerLobbyComponent::HeartBeat Client " + playerIndex + " sent heartbeat");
		serverLobbySend.SendDataToPlayerWhenReady((byte)LOBBY_SERVER_COMMANDS.HEARTBEAT, playerIndex);
	}

	private void ProcessData(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver, List<PersistentPlayerInfo> playerList)
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
						if (playerList[playerNum].playerType == PLAYER_TYPE.NONE
							|| playerList[playerNum].isReady == 0)
						{
							isReadytoStart = false;
						}
					}

					if (isReadytoStart)
					{
						connectionsComponent.SaveGameInfo(playerList);
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

					PersistentPlayerInfo currPlayer = playerList[currPlayerIndex];

					PLAYER_TYPE newplayerType = (PLAYER_TYPE)((int)(currPlayer.playerType + 1) % (int)PLAYER_TYPE.PLAYER_TYPES);

					// Get number of types of players on this player's team
					for (int playerIndex = 0; playerIndex < playerList.Count; ++playerIndex)
					{
						// Skip over self
						if (currPlayerIndex == playerIndex)
						{
							continue;
						}

						if (playerList[playerIndex].team == currPlayer.team)
						{
							if (playerList[playerIndex].playerType == PLAYER_TYPE.SHOOTER)
							{
								++numShootersFound;
							}
							else if (playerList[playerIndex].playerType == PLAYER_TYPE.MATCH3)
							{
								++numMatch3Found;
							}
						}
					}

					// Debug.Log("ServerLobbyComponent::ProcessData numShootersFound = " + numShootersFound + ", numMatch3Found = " + numMatch3Found);

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

					Debug.Log("ServerLobbyComponent::ProcessData Client " + currPlayerIndex + " player type was set to " + currPlayer.playerType.ToString());
				}
			}
			
		}
	}
}

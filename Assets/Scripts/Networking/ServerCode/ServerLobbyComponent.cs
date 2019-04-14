using System.Net;
using System.Collections.Generic;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using UnityEngine.Assertions;
using Util;
using UnityEngine.SceneManagement;

public class ServerLobbyComponent : MonoBehaviour
{
	private enum LOBBY_SERVER_PROCESS
	{
		START_GAME,
		CHANGE_PLAYER_TYPE
	}

	public static readonly int MAX_NUM_PLAYERS = 6;
	public static readonly int SEND_ALL_PLAYERS = -1;

	public List<LobbyPlayerInfo> m_PlayerList;

	// A Pair of Dictionaries to make it easier to map Index and PlayerID
	// ID -> Connection Index
	private Dictionary<byte, int> IdToIndexDictionary;
	// Connection Index -> ID
	private Dictionary<int, byte> IndexToIdDictionary;

	// Byte to send and the player ID
	public List<Queue<byte>> individualSendQueues;
	private Queue<byte> allSendQueue;

	// Command and the player ID
	private Queue<KeyValuePair<LOBBY_SERVER_PROCESS, int>> commandProcessingQueue;

	private ServerConnectionsComponent connectionsComponent;

	int numTeam1Players = 0;
	int numTeam2Players = 0;
	 
	private void Start()
	{
		//Debug.Log("ServerLobbyComponent::Start Called");
		m_PlayerList = new List<LobbyPlayerInfo>(MAX_NUM_PLAYERS);

		// Initialize send queues for all possible players
		individualSendQueues = new List<Queue<byte>>(MAX_NUM_PLAYERS);
		for (int i = 0; i < MAX_NUM_PLAYERS; ++i)
		{
			individualSendQueues.Add(new Queue<byte>());
		}

		allSendQueue = new Queue<byte>();
		commandProcessingQueue = new Queue<KeyValuePair<LOBBY_SERVER_PROCESS, int>>();
		IdToIndexDictionary = new Dictionary<byte, int>();
		IndexToIdDictionary = new Dictionary<int, byte>();
	}


	public void Init(ServerConnectionsComponent connHolder)
	{
		//Debug.Log("ServerLobbyComponent::Init Called");
		connectionsComponent = connHolder;
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
		HandleSendData(ref connections, ref driver, m_PlayerList);
	}

	private void HandleConnections(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver, List<LobbyPlayerInfo> playerList)
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

				individualSendQueues[i] = new Queue<byte>();

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
			if (connections.Length >= MAX_NUM_PLAYERS)
			{
				Debug.Log("ServerLobbyComponent::HandleConnections Too many connections, rejecting latest one");
				driver.Disconnect(c);
				continue;
			}

			Debug.Log("ServerLobbyComponent::HandleConnections Accepted a connection");

			connections.Add(c);
			playerList.Add(new LobbyPlayerInfo());
			playerList[playerList.Count - 1].playerID = connectionsComponent.GetNextPlayerID();
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
				Debug.Log("ServerLobbyComponent::HandleConnections SHOULD NOT BE IN THIS STATE! TEAMS ARE FULL BUT THERE ARE LESS THAN MAX NUMBER OF PLAYERS CONNECTED? NOT POSSIBLE");
			}
		}
	}

	private void HandleReceiveData(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver, List<LobbyPlayerInfo> playerList)
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

	private void ReadClientBytes(int index, List<LobbyPlayerInfo> playerList, byte[] bytes)
	{
		Debug.Log("ServerLobbyComponent::ReadClientBytes bytes.Length = " + bytes.Length);

		for (int i = 0; i < bytes.Length;)
		{
			byte clientCmd = bytes[i];

			// Unsafely assuming that everything is working as expected and there are no attackers.
			++i;

			Debug.Log("ServerLobbyComponent::ReadClientBytes Got " + clientCmd + " from the Client");

			if (clientCmd == (byte)LOBBY_CLIENT_REQUESTS.READY)
			{
				if (playerList[index].isReady == 0)
				{
					playerList[index].isReady = 1;
				}
				else
				{
					playerList[index].isReady = 0;
				}

				Debug.Log("ServerLobbyComponent::ReadClientBytes Client " + index + " ready state set to " + playerList[index].isReady);
			}
			else if (clientCmd == (byte)LOBBY_CLIENT_REQUESTS.CHANGE_TEAM)
			{
				if (playerList[index].team == 0 && numTeam2Players < 3)
				{
					playerList[index].team = 1;
					++numTeam1Players;
					--numTeam2Players;

					Debug.Log("ServerLobbyComponent::ReadClientBytes Client " + index + " team was set to 1");
				}
				else if (playerList[index].team == 1 && numTeam1Players < 3)
				{
					playerList[index].team = 0;
					--numTeam1Players;
					++numTeam2Players;

					Debug.Log("ServerLobbyComponent::ReadClientBytes Client " + index + " team was set to 0");
				}

			}
			else if (clientCmd == (byte)LOBBY_CLIENT_REQUESTS.CHANGE_PLAYER_TYPE)
			{
				commandProcessingQueue.Enqueue(new KeyValuePair<LOBBY_SERVER_PROCESS, int>(LOBBY_SERVER_PROCESS.CHANGE_PLAYER_TYPE, index));
			}
			else if (clientCmd == (byte)LOBBY_CLIENT_REQUESTS.GET_ID)
			{
				Debug.Log("ServerLobbyComponent::ReadClientBytes Client " + index + " sent request for its ID");

				individualSendQueues[index].Enqueue((byte)LOBBY_SERVER_COMMANDS.SET_ID);
				individualSendQueues[index].Enqueue(IndexToIdDictionary[index]);
			}
			else if (clientCmd == (byte)LOBBY_CLIENT_REQUESTS.START_GAME)
			{
				commandProcessingQueue.Enqueue(new KeyValuePair<LOBBY_SERVER_PROCESS, int> (LOBBY_SERVER_PROCESS.START_GAME, SEND_ALL_PLAYERS));
			}
		}
	}

	private void ProcessData(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver, List<LobbyPlayerInfo> playerList)
	{
		while (commandProcessingQueue.Count > 0)
		{
			KeyValuePair<LOBBY_SERVER_PROCESS, int> processCommand = commandProcessingQueue.Dequeue();

			if (processCommand.Value == SEND_ALL_PLAYERS)
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
						allSendQueue.Enqueue((byte)LOBBY_SERVER_COMMANDS.START_GAME);
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

					LobbyPlayerInfo currPlayer = playerList[currPlayerIndex];

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

	
	private void HandleSendData(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver, List<LobbyPlayerInfo> playerList)
	{
		// Send player state to all players
		// For now, send entire lobby state to all players
		for (int index = 0; index < connections.Length; ++index)
		{
			if (!connections.IsCreated)
			{
				Debug.Log("ServerLobbyComponent::HandleReceiveData connections[" + index + "] was not created");
				Assert.IsTrue(true);
			}

			// Send state of all players
			using (var writer = new DataStreamWriter(MAX_NUM_PLAYERS * 5 * sizeof(byte), Allocator.Temp))
			{
				writer.Write((byte)LOBBY_SERVER_COMMANDS.SET_ALL_PLAYER_STATES);
				writer.Write((byte)playerList.Count);

				// Send data for present players
				for ( int playerNum = 0; playerNum < playerList.Count; playerNum++)
				{
					// Write player info
					writer.Write(playerList[playerNum].isReady);
					writer.Write(playerList[playerNum].team);
					writer.Write((byte)playerList[playerNum].playerType);
					writer.Write(playerList[playerNum].playerID);
				}

				connections[index].Send(driver, writer);
			}
		}

		// Send messages meant for individual players
		for (int index = 0; index < connections.Length; ++index)
		{
			Queue<byte> playerQueue = individualSendQueues[index];

			if (playerQueue.Count > 0)
			{
				// Send eveyrthing in the queue
				using (var writer = new DataStreamWriter(playerQueue.Count, Allocator.Temp))
				{
					while (playerQueue.Count > 0)
					{
						byte data = playerQueue.Dequeue();

						if (!connections[index].IsCreated)
						{
							Debug.Log("ServerLobbyComponent::HandleReceiveData connections[" + index + "] was not created");
							Assert.IsTrue(true);
						}

						writer.Write(data);
					}

					connections[index].Send(driver, writer);
				}
			}
		}
		

		// Send things in the queue meant for all players
		if (allSendQueue.Count > 0)
		{
			// Send eveyrthing in the queue
			using (var writer = new DataStreamWriter(allSendQueue.Count, Allocator.Temp))
			{
				while (allSendQueue.Count > 0)
				{
					byte byteToSend = allSendQueue.Dequeue();

					for (int index = 0; index < connections.Length; ++index)
					{
						if (!connections.IsCreated)
						{
							Debug.Log("ServerLobbyComponent::HandleReceiveData connections[" + index + "] was not created");
							Assert.IsTrue(true);
						}

						writer.Write(byteToSend);
						connections[index].Send(driver, writer);
					}
				}
			}
		}
	}
}

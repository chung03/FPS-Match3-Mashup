using System.Collections.Generic;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using UnityEngine.Assertions;
using GameUtils;
using CommonNetworkingUtils;

public class ServerGameComponent : MonoBehaviour
{
	private enum GAME_SERVER_PROCESS
	{
		START_GAME,
		CHANGE_PLAYER_TYPE
	}

	// Current Player States
	public List<GamePlayerInfo> m_PlayerList;

	// A Pair of Dictionaries to make it easier to map Index and PlayerID
	// ID -> Connection Index
	private Dictionary<byte, int> IdToIndexDictionary;
	// Connection Index -> ID
	private Dictionary<int, byte> IndexToIdDictionary;

	// Player ID -> Objects they own
	private Dictionary<byte, int> IdToOwnedObjectsDictionary;
	// Object ID -> Object
	private Dictionary<int, byte> IdtoObjectsDictionary;

	// Command and the player ID
	private Queue<KeyValuePair<GAME_SERVER_PROCESS, int>> commandProcessingQueue;

	private ServerConnectionsComponent connectionsComponent;
	private ServerGameSend serverGameSend;

	int numTeam1Players = 0;
	int numTeam2Players = 0;

	private int nextObjectId = 1;

	private Dictionary<int, ServerHandleIncomingBytes> CommandToFunctionDictionary;

	private void Start()
	{
		//Debug.Log("ServerGameComponent::Start Called");
		m_PlayerList = new List<GamePlayerInfo>(CONSTANTS.MAX_NUM_PLAYERS);

		IdToIndexDictionary = new Dictionary<byte, int>();
		IndexToIdDictionary = new Dictionary<int, byte>();

		IdToOwnedObjectsDictionary = new Dictionary<byte, int>();
		IdtoObjectsDictionary = new Dictionary<int, byte>();

		commandProcessingQueue = new Queue<KeyValuePair<GAME_SERVER_PROCESS, int>>();

		CommandToFunctionDictionary = new Dictionary<int, ServerHandleIncomingBytes>();
		//CommandToFunctionDictionary.Add((int)LOBBY_CLIENT_REQUESTS.READY, ChangePlayerReady);
		//CommandToFunctionDictionary.Add((int)LOBBY_CLIENT_REQUESTS.HEARTBEAT, HeartBeat);
	}


	public void Init(ServerConnectionsComponent connHolder)
	{
		//Debug.Log("ServerGameComponent::Init Called");
		connectionsComponent = connHolder;
		serverGameSend = GetComponent<ServerGameSend>();


	}

	private int GetNextObjectId()
	{
		return nextObjectId++;
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
		serverGameSend.SendDataIfReady(ref connections, ref driver, m_PlayerList);
	}

	private void HandleConnections(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver, List<GamePlayerInfo> playerList)
	{
		//Debug.Log("ServerGameComponent::HandleConnections Called");

		// Clean up connections
		bool connectionsChanged = false;
		for (int i = 0; i < connections.Length; i++)
		{
			if (!connections[i].IsCreated)
			{
				Debug.Log("ServerGameComponent::HandleConnections Removing a connection");
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
					Debug.Log("ServerGameComponent::HandleConnections Removing a player not one team 1 or team 2. playerList[i].team = " + playerList[i].team);
				}

				serverGameSend.ResetIndividualPlayerQueue(i);

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
				Debug.Log("ServerGameComponent::HandleConnections Too many connections, rejecting latest one");
				driver.Disconnect(c);
				continue;
			}

			Debug.Log("ServerGameComponent::HandleConnections Accepted a connection");

			connections.Add(c);
			playerList.Add(new GamePlayerInfo());
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
				Debug.Log("ServerGameComponent::HandleConnections SHOULD NOT BE IN THIS STATE! TEAMS ARE FULL BUT THERE ARE LESS THAN MAX NUMBER OF PLAYERS CONNECTED? NOT POSSIBLE. numTeam1Players = " + numTeam1Players + ", numTeam2Players = " + numTeam2Players);
			}

			// Send all current player data to the new connection
			serverGameSend.SendCurrentPlayerStateDataToNewPlayerWhenReady(connections.Length - 1);
		}
	}

	private void HandleReceiveData(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver, List<GamePlayerInfo> playerList)
	{
		for (int index = 0; index < connections.Length; ++index)
		{
			if (!connections.IsCreated)
			{
				Debug.Log("ServerGameComponent::HandleReceiveData connections[" + index + "] was not created");
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
					Debug.Log("ServerGameComponent::HandleReceiveData Client disconnected from server");
					connections[index] = default;
				}
				else
				{
					Debug.Log("ServerGameComponent::HandleReceiveData Unhandled Network Event: " + cmd);
				}
			}

			//Debug.Log("ServerGameComponent::HandleReceiveData Finished processing connection[" + index + "]");
		}
	}

	private void ReadClientBytes(int playerIndex, List<GamePlayerInfo> playerList, byte[] bytes)
	{
		Debug.Log("ServerGameComponent::ReadClientBytes bytes.Length = " + bytes.Length);

		for (int i = 0; i < bytes.Length;)
		{
			byte clientCmd = bytes[i];

			// Unsafely assuming that everything is working as expected and there are no attackers.
			++i;

			Debug.Log("ServerGameComponent::ReadClientBytes Got " + clientCmd + " from the Client");

			if (clientCmd == (byte)GAME_CLIENT_REQUESTS.CREATE_ENTITY_WITH_OWNERSHIP)
			{
				int newObjectId = GetNextObjectId();

				CREATE_ENTITY_TYPES newObjectType = (CREATE_ENTITY_TYPES)bytes[i];
				++i;

				serverGameSend.SendDataToPlayerWhenReady((byte)GAME_SERVER_COMMANDS.CREATE_ENTITY_WITH_OWNERSHIP, playerIndex);
				serverGameSend.SendDataToPlayerWhenReady((byte)newObjectType, playerIndex);
				serverGameSend.SendDataToPlayerWhenReady((byte)newObjectId, playerIndex);
			}
			else if (clientCmd == (byte)GAME_CLIENT_REQUESTS.HEARTBEAT)
			{
				Debug.Log("ServerGameComponent::ReadClientBytes Client " + playerIndex + " sent heartbeat");
				serverGameSend.SendDataToPlayerWhenReady((byte)GAME_SERVER_COMMANDS.HEARTBEAT, playerIndex);
			}
		}
	}

	private void ProcessData(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver, List<GamePlayerInfo> playerList)
	{
		while (commandProcessingQueue.Count > 0)
		{
			KeyValuePair<GAME_SERVER_PROCESS, int> processCommand = commandProcessingQueue.Dequeue();

			

		}
	}
}

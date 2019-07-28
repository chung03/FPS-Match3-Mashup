using System.Collections.Generic;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using UnityEngine.Assertions;
using GameUtils;
using CommonNetworkingUtils;

public class ServerGameDataComponent : MonoBehaviour
{
	private enum GAME_SERVER_PROCESS
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

	// List of Object with Deltas. ID -> Object
	private Dictionary<int, ObjectWithDelta> IdToObjectsDictionary;

	private void Start()
	{
		//Debug.Log("ServerGameDataComponent::Start Called");
		m_PlayerList = new List<PersistentPlayerInfo>(CONSTANTS.MAX_NUM_PLAYERS);

		IdToIndexDictionary = new Dictionary<byte, int>();
		IndexToIdDictionary = new Dictionary<int, byte>();

		IdToOwnedObjectsDictionary = new Dictionary<byte, int>();
		IdtoObjectsDictionary = new Dictionary<int, byte>();

		commandProcessingQueue = new Queue<KeyValuePair<GAME_SERVER_PROCESS, int>>();

		IdToObjectsDictionary = new Dictionary<int, ObjectWithDelta>();
	}


	public void Init(ServerConnectionsComponent connHolder)
	{
		//Debug.Log("ServerGameDataComponent::Init Called");
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

		// ***** Process data *****
		ProcessData(ref connections, ref driver);

		// ***** Send data *****
		serverGameSend.SendDataIfReady(ref connections, ref driver, IdToObjectsDictionary);
	}
	

	public int HandleCreateEntityWithOwnership(int index, byte[] bytes, int playerIndex)
	{
		int bytesRead = 0;

		int newObjectId = GetNextObjectId();

		CREATE_ENTITY_TYPES newObjectType = (CREATE_ENTITY_TYPES)bytes[index];
		++bytesRead;

		if (newObjectType == CREATE_ENTITY_TYPES.FPS_PLAYER)
		{
			FPSPlayerData newData = new FPSPlayerData();
			newData.SetObjectId(newObjectId);

			IdToObjectsDictionary.Add(newObjectId, newData);
		}

		serverGameSend.SendDataToPlayerWhenReady((byte)GAME_SERVER_COMMANDS.CREATE_ENTITY_WITH_OWNERSHIP, playerIndex);
		serverGameSend.SendDataToPlayerWhenReady((byte)newObjectType, playerIndex);
		serverGameSend.SendDataToPlayerWhenReady((byte)newObjectId, playerIndex);

		//Debug.Log("ServerLobbyComponent::ChangePlayerReady Player " + playerInfo[playerIndex].playerID + " ready state set to " + playerInfo[playerIndex].isReady);

		return bytesRead;
	}

	public int HandleSetAllObjectStatesCommand(int index, byte[] bytes, int playerIndex)
	{
		Debug.Log("ServerGameDataComponent::HandleSetAllObjectStatesCommand Start");

		int bytesRead = 0;

		byte numObjects = bytes[index];
		++bytesRead;

		for (int objectNum = 0; objectNum < numObjects; ++objectNum)
		{
			byte objectId = bytes[index + bytesRead];
			++bytesRead;

			byte numBytesInDelta = bytes[index + bytesRead];
			++bytesRead;

			byte[] deltaBytes = new byte[numBytesInDelta];

			System.Array.Copy(bytes, index + bytesRead, deltaBytes, 0, numBytesInDelta);

			bytesRead += numBytesInDelta;

			// If the object hasn't been created yet, the don't do anything for now.
			// In future, should probably through some sort of error
			if (IdToObjectsDictionary.ContainsKey(objectId))
			{
				ObjectWithDelta obj = IdToObjectsDictionary[objectId];
				obj.ApplyDelta(deltaBytes, true);
			}
		}

		Debug.Log("ServerGameDataComponent::HandleSetAllObjectStatesCommand Finished");
		return bytesRead;
	}


	public int HeartBeat(int index, byte[] bytes, int playerIndex)
	{
		// Debug.Log("ServerGameDataComponent::ReadClientBytes Client " + playerIndex + " sent heartbeat");
		serverGameSend.SendDataToPlayerWhenReady((byte)GAME_SERVER_COMMANDS.HEARTBEAT, playerIndex);

		return 0;
	}

	public void ProcessData(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver)
	{
		while (commandProcessingQueue.Count > 0)
		{
			KeyValuePair<GAME_SERVER_PROCESS, int> processCommand = commandProcessingQueue.Dequeue();



		}
	}
}

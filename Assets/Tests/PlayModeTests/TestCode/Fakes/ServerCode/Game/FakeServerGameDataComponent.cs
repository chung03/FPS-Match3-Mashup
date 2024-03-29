﻿using System.Collections.Generic;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;



using UnityEngine.Assertions;
using GameUtils;
using CommonNetworkingUtils;

public class FakeServerGameDataComponent : MonoBehaviour
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

	private Dictionary<GAME_CLIENT_REQUESTS, ServerHandleIncomingBytes> CommandToFunctionDictionary;

	private FakeServerConnectionsComponent connectionsComponent;
	private FakeServerGameSend serverGameSend;

	int numTeam1Players = 0;
	int numTeam2Players = 0;

	private int nextObjectId = 1;

	// List of Object with Deltas. ID -> Object
	private Dictionary<int, ObjectWithDelta> IdToObjectsDictionary;

	private void Start()
	{
		//Debug.Log("FakeServerGameDataComponent::Start Called");
		m_PlayerList = connectionsComponent.GetGameInfo();

		IdToIndexDictionary = new Dictionary<byte, int>();
		IndexToIdDictionary = new Dictionary<int, byte>();

		ReconstructIndexIdDictionaries();

		IdToOwnedObjectsDictionary = new Dictionary<byte, int>();
		IdtoObjectsDictionary = new Dictionary<int, byte>();

		commandProcessingQueue = new Queue<KeyValuePair<GAME_SERVER_PROCESS, int>>();

		CommandToFunctionDictionary = new Dictionary<GAME_CLIENT_REQUESTS, ServerHandleIncomingBytes>();
		CommandToFunctionDictionary.Add(GAME_CLIENT_REQUESTS.CREATE_ENTITY_WITH_OWNERSHIP, HandleCreateEntityWithOwnership);
		CommandToFunctionDictionary.Add(GAME_CLIENT_REQUESTS.SET_ALL_OBJECT_STATES, HandleSetAllObjectStatesCommand);
		CommandToFunctionDictionary.Add(GAME_CLIENT_REQUESTS.HEARTBEAT, HeartBeat);

		IdToObjectsDictionary = new Dictionary<int, ObjectWithDelta>();
	}


	public void Init(FakeServerConnectionsComponent connHolder)
	{
		//Debug.Log("FakeServerGameDataComponent::Init Called");
		connectionsComponent = connHolder;
		serverGameSend = GetComponent<FakeServerGameSend>();
	}

	private int GetNextObjectId()
	{
		return nextObjectId++;
	}

	void Update()
	{
		ref UdpNetworkDriver driver = ref connectionsComponent.GetDriver();
		ref NativeList<NetworkConnection> connections = ref connectionsComponent.GetConnections();

		// ***** Process data *****
		ProcessData(ref connections, ref driver);

		// ***** Send data *****
		serverGameSend.SendDataIfReady(ref connections, ref driver, IdToObjectsDictionary);
	}

	public void RemovePlayer(int index)
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
			Debug.Log("FakeServerGameDataComponent::RemovePlayer Removing a player not one team 1 or team 2. m_PlayerList["+index+"].team = " + m_PlayerList[index].team);
		}

		serverGameSend.ResetIndividualPlayerQueue(index);

		m_PlayerList.RemoveAtSwapBack(index);

		ReconstructIndexIdDictionaries();
	}
	

	private void ReconstructIndexIdDictionaries()
	{
		IdToIndexDictionary.Clear();
		IndexToIdDictionary.Clear();

		for (int i = 0; i < m_PlayerList.Count; i++)
		{
			IdToIndexDictionary.Add(m_PlayerList[i].playerID, i);
			IndexToIdDictionary.Add(i, m_PlayerList[i].playerID);
		}
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

		// Create the entity with ownership on the owning player
		serverGameSend.SendDataToPlayerWhenReady((byte)GAME_SERVER_COMMANDS.CREATE_ENTITY_WITH_OWNERSHIP, playerIndex);
		serverGameSend.SendDataToPlayerWhenReady((byte)newObjectType, playerIndex);
		serverGameSend.SendDataToPlayerWhenReady((byte)newObjectId, playerIndex);

		// Create the entity without ownership on every other player
		for (int i = 0; i < m_PlayerList.Count; ++i)
		{
			int otherPlayerIndex = IdToIndexDictionary[m_PlayerList[i].playerID];

			// Make sure we don't send data to the player with ownership again
			if (otherPlayerIndex == playerIndex )
			{
				continue;
			}

			serverGameSend.SendDataToPlayerWhenReady((byte)GAME_SERVER_COMMANDS.CREATE_ENTITY, otherPlayerIndex);
			serverGameSend.SendDataToPlayerWhenReady((byte)newObjectType, otherPlayerIndex);
			serverGameSend.SendDataToPlayerWhenReady((byte)newObjectId, otherPlayerIndex);
		}

		//Debug.Log("ServerLobbyComponent::ChangePlayerReady Player " + playerInfo[playerIndex].playerID + " ready state set to " + playerInfo[playerIndex].isReady);

		return bytesRead;
	}

	public int HandleSetAllObjectStatesCommand(int index, byte[] bytes, int playerIndex)
	{
		Debug.Log("FakeServerGameDataComponent::HandleSetAllObjectStatesCommand Start");

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
				obj.SetDeltaToZero();
				obj.ApplyDelta(deltaBytes);
			}
		}

		Debug.Log("FakeServerGameDataComponent::HandleSetAllObjectStatesCommand Finished");
		return bytesRead;
	}


	public int HeartBeat(int index, byte[] bytes, int playerIndex)
	{
		// Debug.Log("FakeServerGameDataComponent::ReadClientBytes Client " + playerIndex + " sent heartbeat");
		serverGameSend.SendDataToPlayerWhenReady((byte)GAME_SERVER_COMMANDS.HEARTBEAT, playerIndex);

		return 0;
	}

	public Type CreateServerOwnedEntity<Type>(CREATE_ENTITY_TYPES newEntityType) where Type : ObjectWithDelta
	{
		Type retObj = default;

		int newObjectId = GetNextObjectId();
		retObj.SetObjectId(newObjectId);
		IdToObjectsDictionary.Add(newObjectId, retObj);

		// Create the entity without ownership on every other player
		for (int i = 0; i < m_PlayerList.Count; ++i)
		{
			int otherPlayerIndex = IdToIndexDictionary[m_PlayerList[i].playerID];

			serverGameSend.SendDataToPlayerWhenReady((byte)GAME_SERVER_COMMANDS.CREATE_ENTITY, otherPlayerIndex);
			serverGameSend.SendDataToPlayerWhenReady((byte)retObj.GetEntityType(), otherPlayerIndex);
			serverGameSend.SendDataToPlayerWhenReady((byte)newObjectId, otherPlayerIndex);
		}

		return retObj;
	}

	public void ProcessData(ref NativeList<NetworkConnection> connections, ref UdpNetworkDriver driver)
	{
		while (commandProcessingQueue.Count > 0)
		{
			KeyValuePair<GAME_SERVER_PROCESS, int> processCommand = commandProcessingQueue.Dequeue();



		}
	}

	public void ProcessClientBytes(int playerIndex, byte[] bytes)
	{
		Debug.Log("ServerGameReceiveComponent::ReadClientBytes bytes.Length = " + bytes.Length);

		for (int i = 0; i < bytes.Length;)
		{
			GAME_CLIENT_REQUESTS clientCmd = (GAME_CLIENT_REQUESTS)bytes[i];

			// Unsafely assuming that everything is working as expected and there are no attackers.
			++i;

			Debug.Log("ServerGameReceiveComponent::ReadClientBytes Got " + clientCmd + " from the Client");

			i += CommandToFunctionDictionary[clientCmd](i, bytes, playerIndex);
		}
	}
}

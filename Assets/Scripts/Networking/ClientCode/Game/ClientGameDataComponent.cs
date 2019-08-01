using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameUtils;
using CommonNetworkingUtils;

using Unity.Networking.Transport;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

public class ClientGameDataComponent : MonoBehaviour
{
	private static readonly string PLAY_SCENE = "Assets/Scenes/PlayScene.unity";

	// This is the player's ID. This is all it needs to identify itself and to find its player info.
	private int m_PlayerID;

	// This is the player's info from the lobby. This is all it needs to identify itself and to find its player info.
	private PersistentPlayerInfo m_PlayerInfo;

	// This stores the infor for all players, including this one.
	private List<PersistentPlayerInfo> m_AllPlayerInfo;

	// A Pair of Dictionaries to make it easier to map Index and PlayerID
	// ID -> Connection Index
	private Dictionary<int, int> IdToIndexDictionary;
	// Connection Index -> ID
	private Dictionary<int, int> IndexToIdDictionary;

	private ClientConnectionsComponent connectionsComponent;
	private ClientGameSend clientGameSend;

	// List of Object with Deltas which client will update. ID -> Object
	private Dictionary<int, ObjectWithDelta> IdToClientControlledObjectDictionary;
	private Dictionary<int, ObjectWithDelta> IdToServerControlledObjectDictionary;

	private Dictionary<GAME_SERVER_COMMANDS, ClientHandleIncomingBytes> CommandToFunctionDictionary;

	[SerializeField]
	private GameObject gameUIObj;
	private GameUIBehaviour gameUIInstance;

	[SerializeField]
	private GameObject FPSPlayerObj;

	[SerializeField]
	private GameObject Match3PlayerObj;

	private void Start()
	{
		m_AllPlayerInfo = new List<PersistentPlayerInfo>(CONSTANTS.MAX_NUM_PLAYERS);
		for (int index = 0; index < CONSTANTS.MAX_NUM_PLAYERS; ++index)
		{
			m_AllPlayerInfo.Add(null);
		}

		gameUIInstance = Instantiate(gameUIObj).GetComponent<GameUIBehaviour>();
		gameUIInstance.SetUI(connectionsComponent.IsHost());
		gameUIInstance.Init(this);

		IdToIndexDictionary = new Dictionary<int, int>();
		IndexToIdDictionary = new Dictionary<int, int>();

		IdToClientControlledObjectDictionary = new Dictionary<int, ObjectWithDelta>();
		IdToServerControlledObjectDictionary = new Dictionary<int, ObjectWithDelta>();

		// Initialize the byteHandling Table
		CommandToFunctionDictionary = new Dictionary<GAME_SERVER_COMMANDS, ClientHandleIncomingBytes>();
		CommandToFunctionDictionary.Add(GAME_SERVER_COMMANDS.CREATE_ENTITY_WITH_OWNERSHIP, HandleCreateEntityOwnershipCommand);
		CommandToFunctionDictionary.Add(GAME_SERVER_COMMANDS.CREATE_ENTITY, HandleCreateEntityCommand);
		CommandToFunctionDictionary.Add(GAME_SERVER_COMMANDS.SET_ALL_OBJECT_STATES, HandleSetAllObjectStatesCommand);
		CommandToFunctionDictionary.Add(GAME_SERVER_COMMANDS.HEARTBEAT, HandleHeartBeat);
	}

	public void Init(ClientConnectionsComponent connHolder, PersistentPlayerInfo playerInfo)
	{
		connectionsComponent = connHolder;
		clientGameSend = GetComponent<ClientGameSend>();

		m_PlayerInfo = playerInfo;

		if (m_PlayerInfo.playerType == LobbyUtils.PLAYER_TYPE.SHOOTER)
		{
			clientGameSend.SendDataWhenReady((byte)GAME_CLIENT_REQUESTS.CREATE_ENTITY_WITH_OWNERSHIP);
			clientGameSend.SendDataWhenReady((byte)CREATE_ENTITY_TYPES.FPS_PLAYER);
		}
		else
		{
			Instantiate(Match3PlayerObj);
		}
	}

	private void Update()
	{
		ref UdpCNetworkDriver driver = ref connectionsComponent.GetDriver();
		ref NetworkConnection connection = ref connectionsComponent.GetConnection();

		// ***** Update UI ****
		// gameUIInstance.UpdateUI(m_AllPlayerInfo);

		// ***** Send data *****
		clientGameSend.SendDataIfReady(ref connection, ref driver, IdToClientControlledObjectDictionary);
	}


	public int HandleHeartBeat(int index, byte[] bytes)
	{
		Debug.Log("ClientGameDataComponent::HandleHeartBeat Received heartbeat from server");
		return 0;
	}

	// Returns the number of bytes read from the bytes array
	public int HandleCreateEntityOwnershipCommand(int index, byte[] bytes)
	{
		int bytesRead = 0;

		byte objectType = bytes[index];
		++bytesRead;

		byte newId = bytes[index + bytesRead];
		++bytesRead;

		if (objectType == (byte)CREATE_ENTITY_TYPES.FPS_PLAYER)
		{
			FPSPlayer fpsPlayer = Instantiate(FPSPlayerObj).GetComponent<FPSPlayer>();
			fpsPlayer.Init(this, newId, true);

			IdToClientControlledObjectDictionary.Add(newId, fpsPlayer.GetData());
		}
		else if (objectType == (byte)CREATE_ENTITY_TYPES.MATCH3_PLAYER)
		{
			/*
			FPSPlayer fpsPlayer = Instantiate(FPSPlayerObj).GetComponent<FPSPlayer>();
			fpsPlayer.Init(this, newId);
			*/
		}


		Debug.Log("ClientGameDataComponent::HandleCreateEntityOwnershipCommand Object ID was set to " + newId + " and Object type is " + objectType);
		return bytesRead;
	}

	public int HandleCreateEntityCommand(int index, byte[] bytes)
	{
		int bytesRead = 0;

		byte objectType = bytes[index];
		++bytesRead;

		byte newId = bytes[index + bytesRead];
		++bytesRead;

		if (objectType == (byte)CREATE_ENTITY_TYPES.FPS_PLAYER)
		{
			FPSPlayer fpsPlayer = Instantiate(FPSPlayerObj).GetComponent<FPSPlayer>();
			fpsPlayer.Init(this, newId, false);

			// Since the player doesn't control this, remove input. Also remove camera
			//Destroy(fpsPlayer.GetComponent<FPSPlayerInput>());
			//Destroy(fpsPlayer.transform.Find("Camera"));

			//IdToClientControlledObjectDictionary.Add(newId, fpsPlayer.GetData());
		}
		else if (objectType == (byte)CREATE_ENTITY_TYPES.MATCH3_PLAYER)
		{
			/*
			FPSPlayer fpsPlayer = Instantiate(FPSPlayerObj).GetComponent<FPSPlayer>();
			fpsPlayer.Init(this, newId);
			*/
		}


		Debug.Log("ClientGameDataComponent::HandleCreateEntityOwnershipCommand Object ID was set to " + newId + " and Object type is " + objectType);
		return bytesRead;
	}

	public int HandleSetAllObjectStatesCommand(int index, byte[] bytes)
	{
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
			if (IdToServerControlledObjectDictionary.ContainsKey(objectId))
			{
				ObjectWithDelta obj = IdToServerControlledObjectDictionary[objectId];
				obj.ApplyDelta(deltaBytes, false);
			}
		}

		Debug.Log("ClientGameDataComponent::HandleSetAllObjectStatesCommand Finished");
		return bytesRead;
	}

	public void AddObjectWithDeltaClient(ObjectWithDelta newObj)
	{
		//IdToClientControlledObjectDictionary.Add(newObj.GetObjectId(), newObj);
		IdToServerControlledObjectDictionary.Add(newObj.GetObjectId(), newObj);
	}

	public void ProcessServerBytes(byte[] bytes)
	{
		// Must always manually move index for bytes
		for (int i = 0; i < bytes.Length;)
		{
			// Unsafely assuming that everything is working as expected and there are no attackers.
			GAME_SERVER_COMMANDS serverCmd = (GAME_SERVER_COMMANDS)bytes[i];
			++i;

			Debug.Log("ClientGameDataComponent::ReadServerBytes Got " + serverCmd + " from the Server");

			i += CommandToFunctionDictionary[serverCmd](i, bytes);
		}
	}
}

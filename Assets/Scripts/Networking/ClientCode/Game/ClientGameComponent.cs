using System.Collections.Generic;
using UnityEngine;

using Unity.Networking.Transport;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using GameUtils;
using CommonNetworkingUtils;
using UnityEngine.SceneManagement;

using System.Text;

public class ClientGameComponent : MonoBehaviour
{

	private static readonly string PLAY_SCENE = "Assets/Scenes/PlayScene.unity";

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

	[SerializeField]
	private GameObject gameUIObj;
	private GameUIBehaviour gameUIInstance;

	[SerializeField]
	private GameObject FPSPlayerObj;

	[SerializeField]
	private GameObject Match3PlayerObj;

	// List of Object with Deltas which client will update. ID -> Object
	private Dictionary<int, ObjectWithDelta> IdToClientControlledObjectDictionary;
	private Dictionary<int, ObjectWithDelta> IdToServerControlledObjectDictionary;
	
	private Dictionary<GAME_SERVER_COMMANDS, ClientHandleIncomingBytes> CommandToFunctionDictionary;

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
		CommandToFunctionDictionary.Add(GAME_SERVER_COMMANDS.HEARTBEAT, HandleHeartBeat);
	}

	public void Init(ClientConnectionsComponent connHolder, PersistentPlayerInfo playerInfo)
	{
		//Debug.Log("ClientGameComponent::Init Called");
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
		// gameUIInstance.UpdateUI(m_AllPlayerInfo);

		// ***** Send data *****
		clientGameSend.SendDataIfReady(ref connection, ref driver, m_AllPlayerInfo);
	}

	private void HandleReceiveData(ref NetworkConnection connection, ref UdpCNetworkDriver driver, List<PersistentPlayerInfo> allPlayerInfo)
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
				clientGameSend.SendDataWhenReady((byte)GAME_CLIENT_REQUESTS.GET_ID);
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

	private void ReadServerBytes(List<PersistentPlayerInfo> playerList, DataStreamReader stream)
	{
		var readerCtx = default(DataStreamReader.Context);

		Debug.Log("ClientGameComponent::ReadServerBytes stream.Length = " + stream.Length);

		byte[] bytes = stream.ReadBytesAsArray(ref readerCtx, stream.Length);

		// Must always manually move index for bytes
		for (int i = 0; i < stream.Length;)
		{
			// Unsafely assuming that everything is working as expected and there are no attackers.
			GAME_SERVER_COMMANDS serverCmd = (GAME_SERVER_COMMANDS)bytes[i];
			++i;

			Debug.Log("ClientGameComponent::ReadServerBytes Got " + serverCmd + " from the Server");

			i += CommandToFunctionDictionary[serverCmd](i, bytes, playerList);
		}
	}

	private int HandleHeartBeat(int index, byte[] bytes, List<PersistentPlayerInfo> playerList)
	{
		Debug.Log("ClientGameComponent::HandleHeartBeat Received heartbeat from server");
		return 0;
	}

	// Returns the number of bytes read from the bytes array
	private int HandleCreateEntityOwnershipCommand(int index, byte[] bytes, List<PersistentPlayerInfo> playerList)
	{
		int bytesRead = 0;

		byte objectType = bytes[index];
		++bytesRead;

		byte newId = bytes[index];
		++bytesRead;

		if (objectType == (byte)CREATE_ENTITY_TYPES.FPS_PLAYER)
		{
			FPSPlayer fpsPlayer = Instantiate(FPSPlayerObj).GetComponent<FPSPlayer>();
			fpsPlayer.Init(this, newId);
		}
		else if (objectType == (byte)CREATE_ENTITY_TYPES.MATCH3_PLAYER)
		{
			/*
			FPSPlayer fpsPlayer = Instantiate(FPSPlayerObj).GetComponent<FPSPlayer>();
			fpsPlayer.Init(this, newId);
			*/
		}


		Debug.Log("ClientGameComponent::HandleCreateEntityOwnershipCommand Object ID was set to " + newId + " and Object type is " + objectType);
		return bytesRead;
	}

	public void AddObjectWithDeltaClient(ObjectWithDelta newObj)
	{
		IdToClientControlledObjectDictionary.Add(0, newObj);
		IdToServerControlledObjectDictionary.Add(0, newObj);
	}
}

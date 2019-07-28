using System.Collections.Generic;
using UnityEngine;

using Unity.Networking.Transport;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using GameUtils;
using CommonNetworkingUtils;
using UnityEngine.SceneManagement;

using System.Text;

public class ClientGameReceiveComponent : MonoBehaviour
{
	private static readonly string PLAY_SCENE = "Assets/Scenes/PlayScene.unity";

	private ClientConnectionsComponent connectionsComponent;
	private ClientGameSend clientGameSend;
	private ClientGameDataComponent clientGameData;
	
	private Dictionary<GAME_SERVER_COMMANDS, ClientHandleIncomingBytes> CommandToFunctionDictionary;

	private void Start()
	{
		// Initialize the byteHandling Table
		CommandToFunctionDictionary = new Dictionary<GAME_SERVER_COMMANDS, ClientHandleIncomingBytes>();
		CommandToFunctionDictionary.Add(GAME_SERVER_COMMANDS.CREATE_ENTITY_WITH_OWNERSHIP, clientGameData.HandleCreateEntityOwnershipCommand);
		CommandToFunctionDictionary.Add(GAME_SERVER_COMMANDS.SET_ALL_OBJECT_STATES, clientGameData.HandleSetAllObjectStatesCommand);
		CommandToFunctionDictionary.Add(GAME_SERVER_COMMANDS.HEARTBEAT, clientGameData.HandleHeartBeat);
	}

	public void Init(ClientConnectionsComponent connHolder)
	{
		//Debug.Log("ClientGameComponent::Init Called");
		connectionsComponent = connHolder;
		clientGameSend = GetComponent<ClientGameSend>();
		clientGameData = GetComponent<ClientGameDataComponent>();
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
		HandleReceiveData(ref connection, ref driver);
	}

	private void HandleReceiveData(ref NetworkConnection connection, ref UdpCNetworkDriver driver)
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
				ReadServerBytes(stream);
			}
			else if (cmd == NetworkEvent.Type.Disconnect)
			{
				Debug.Log("ClientGameComponent::HandleReceiveData Client got disconnected from server");
				connection = default;
			}
		}
	}

	private void ReadServerBytes(DataStreamReader stream)
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

			i += CommandToFunctionDictionary[serverCmd](i, bytes);
		}
	}
}

using System.Collections.Generic;
using UnityEngine;

using Unity.Networking.Transport;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using LobbyUtils;
using CommonNetworkingUtils;


public class ClientLobbyReceiveComponent : MonoBehaviour
{
	private ClientConnectionsComponent connectionsComponent;
	private ClientLobbySend clientLobbySend;
	private ClientLobbyDataComponent clientLobbyData;
	
	private Dictionary<int, ClientHandleIncomingBytes> CommandToFunctionDictionary;

	private void Start()
	{
		// Initialize the byteHandling Table
		CommandToFunctionDictionary = new Dictionary<int, ClientHandleIncomingBytes>();
		CommandToFunctionDictionary.Add((int)LOBBY_SERVER_COMMANDS.READY, clientLobbyData.HandleReadyCommand);
		CommandToFunctionDictionary.Add((int)LOBBY_SERVER_COMMANDS.CHANGE_TEAM, clientLobbyData.HandleChangeTeamCommand);
		CommandToFunctionDictionary.Add((int)LOBBY_SERVER_COMMANDS.SET_ID, clientLobbyData.HandleSetIdCommand);
		CommandToFunctionDictionary.Add((int)LOBBY_SERVER_COMMANDS.SET_ALL_PLAYER_STATES, clientLobbyData.HandlePlayerStatesCommand);
		CommandToFunctionDictionary.Add((int)LOBBY_SERVER_COMMANDS.START_GAME, clientLobbyData.HandleStartCommand);
		CommandToFunctionDictionary.Add((int)LOBBY_SERVER_COMMANDS.HEARTBEAT, clientLobbyData.HandleHeartBeat);
	}

	public void Init(ClientConnectionsComponent connHolder)
	{
		//Debug.Log("ClientLobbyComponent::Init Called");
		connectionsComponent = connHolder;
		clientLobbySend = GetComponent<ClientLobbySend>();
		clientLobbyData = GetComponent<ClientLobbyDataComponent>();
	}

	private void Update()
	{
		ref UdpCNetworkDriver driver = ref connectionsComponent.GetDriver();
		ref NetworkConnection connection = ref connectionsComponent.GetConnection();

		driver.ScheduleUpdate().Complete();

		//Debug.Log("ClientLobbyComponent::Update connection.IsCreated = " + connection.IsCreated);

		if (!connection.IsCreated)
		{
			Debug.Log("ClientLobbyComponent::Update Something went wrong during connect");
			return;
		}

		// ***** Receive data *****
		HandleReceiveData(ref connection, ref driver);
	}

	private void HandleReceiveData(ref NetworkConnection connection, ref UdpCNetworkDriver driver)
	{
		//Debug.Log("ClientLobbyComponent::HandleReceiveData Called");

		NetworkEvent.Type cmd;
		DataStreamReader stream;
		while ((cmd = connection.PopEvent(driver, out stream)) !=
			NetworkEvent.Type.Empty)
		{
			if (cmd == NetworkEvent.Type.Connect)
			{
				Debug.Log("ClientLobbyComponent::HandleReceiveData We are now connected to the server");

				// Get ID
				clientLobbySend.SendDataWhenReady((byte)LOBBY_CLIENT_REQUESTS.GET_ID);
			}
			else if (cmd == NetworkEvent.Type.Data)
			{
				ReadServerBytes(stream);
			}
			else if (cmd == NetworkEvent.Type.Disconnect)
			{
				Debug.Log("ClientLobbyComponent::HandleReceiveData Client got disconnected from server");
				connection = default;
			}
		}
	}

	private void ReadServerBytes(DataStreamReader stream)
	{
		var readerCtx = default(DataStreamReader.Context);

		Debug.Log("ClientLobbyComponent::ReadServerBytes stream.Length = " + stream.Length);

		byte[] bytes = stream.ReadBytesAsArray(ref readerCtx, stream.Length);

		// Must always manually move index for bytes
		for (int i = 0; i < stream.Length;)
		{
			// Unsafely assuming that everything is working as expected and there are no attackers.
			byte serverCmd = bytes[i];
			++i;

			Debug.Log("ClientLobbyComponent::ReadServerBytes Got " + serverCmd + " from the Server");

			i += CommandToFunctionDictionary[serverCmd](i, bytes);
		}
	}
}

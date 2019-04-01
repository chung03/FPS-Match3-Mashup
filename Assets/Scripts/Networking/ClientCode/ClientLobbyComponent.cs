using System.Net;
using System.Collections.Generic;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using UnityEngine.Assertions;
using Util;
using UnityEngine.SceneManagement;

public class ClientLobbyComponent : MonoBehaviour
{	
	// This is the player's ID. This is all it needs to identify itself and to find its player info.
	private int m_PlayerID;

	// This stores the infor for all players, including this one.
	private List<LobbyPlayerInfo> m_AllPlayerInfo;

	ClientConnectionsComponent connectionsComponent;

	[SerializeField]
	private GameObject lobbyUIObj;
	private GameObject lobbyUIInstance;


	private void Start()
	{
		//Debug.Log("ClientLobbyComponent::Start Called");
		m_PlayerID = -1;
		m_AllPlayerInfo = new List<LobbyPlayerInfo>(ServerLobbyComponent.MAX_NUM_PLAYERS);

		for (int index = 0; index < ServerLobbyComponent.MAX_NUM_PLAYERS; ++index)
		{
			m_AllPlayerInfo.Add(new LobbyPlayerInfo());
		}

		lobbyUIInstance = Instantiate(lobbyUIObj);
		lobbyUIInstance.GetComponent<LobbyUIBehaviour>().SetUI(connectionsComponent.IsHost());
	}

	public void Init(ClientConnectionsComponent connHolder)
	{
		//Debug.Log("ClientLobbyComponent::Init Called");
		connectionsComponent = connHolder;
	}

	void Update()
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
		HandleReceiveData(ref connection, ref driver, m_AllPlayerInfo);

		// ***** Process data *****

		// ***** Update UI ****
		UpdateUI();

		// ***** Accept Player Input ****

		// ***** Send data *****
		HandleSendData(ref connection, ref driver, m_AllPlayerInfo);
	}

	private void HandleReceiveData(ref NetworkConnection connection, ref UdpCNetworkDriver driver, List<LobbyPlayerInfo> allPlayerInfo)
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
				
				// Send initial state
				using (var writer = new DataStreamWriter(16, Allocator.Temp))
				{
					 writer.Write((byte)LOBBY_CLIENT_REQUESTS.READY);
					 writer.Write((byte)0);

					 writer.Write((byte)LOBBY_CLIENT_REQUESTS.CHANGE_TEAM);
					 writer.Write((byte)1);

					writer.Write((byte)LOBBY_CLIENT_REQUESTS.GET_ID);

					//byte[] bytes = { (byte)LOBBY_COMMANDS.READY, 0 , (byte)LOBBY_COMMANDS.CHANGE_TEAM , 1};
					//writer.Write(bytes, 4);

					connection.Send(driver, writer);
				}
			}
			else if (cmd == NetworkEvent.Type.Data)
			{
				ReadServerBytes(allPlayerInfo, stream);
			}
			else if (cmd == NetworkEvent.Type.Disconnect)
			{
				Debug.Log("ClientLobbyComponent::HandleReceiveData Client got disconnected from server");
				connection = default(NetworkConnection);
			}
		}
	}

	private void ReadServerBytes(List<LobbyPlayerInfo> playerList, DataStreamReader stream)
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

			if (serverCmd == (byte)LOBBY_SERVER_COMMANDS.READY)
			{
				byte readyStatus = bytes[i];
				++i;

				playerList[m_PlayerID].isReady = readyStatus;

				Debug.Log("ClientLobbyComponent::ReadServerBytes Client ready state set to " + readyStatus);
			}
			else if (serverCmd == (byte)LOBBY_SERVER_COMMANDS.CHANGE_TEAM)
			{
				byte newTeam = bytes[i];
				++i;

				playerList[m_PlayerID].team = newTeam;

				Debug.Log("ClientLobbyComponent::ReadServerBytes Client team was set to " + newTeam);
			}
			else if (serverCmd == (byte)LOBBY_SERVER_COMMANDS.SET_ID)
			{
				byte newID = bytes[i];
				++i;

				m_PlayerID = newID;

				Debug.Log("ClientLobbyComponent::ReadServerBytes Client ID was set to " + m_PlayerID);
			}
			else if (serverCmd == (byte)LOBBY_SERVER_COMMANDS.SET_ALL_PLAYER_STATES)
			{
				// Do a for loop iterating over the bytes using the same counter
				for (int player = 0; player < ServerLobbyComponent.MAX_NUM_PLAYERS; ++player)
				{
					// Unsafely assuming that everything is working as expected and there are no attackers.
					byte isPlayerInSlot = bytes[i];
					++i;

					if (isPlayerInSlot != 0)
					{
						Debug.Log("ClientLobbyComponent::ReadServerBytes Data for client " + player + " received");

						byte isReady = bytes[i];
						++i;
						byte team = bytes[i];
						++i;

						playerList[player].isReady = isReady;
						playerList[player].team = team;
					}
				}
			}
		}
	}

	private void UpdateUI()
	{
		
	}

	private void HandleSendData(ref NetworkConnection connection, ref UdpCNetworkDriver driver, List<LobbyPlayerInfo> allPlayerInfo)
	{
	}
}

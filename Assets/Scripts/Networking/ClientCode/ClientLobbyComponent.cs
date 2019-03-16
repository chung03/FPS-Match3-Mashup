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
	public LobbyPlayerInfo m_Player;

	ClientConnectionsComponent connectionsComponent;

	private void Start()
	{
		//Debug.Log("ClientLobbyComponent::Start Called");
		m_Player = new LobbyPlayerInfo();
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
		HandleReceiveData(ref connection, ref driver, m_Player);

		// ***** Process data *****

		// ***** Update UI ****

		// ***** Accept Player Input ****

		// ***** Send data *****
		HandleSendData(ref connection, ref driver, m_Player);
	}

	private void HandleReceiveData(ref NetworkConnection connection, ref UdpCNetworkDriver driver, LobbyPlayerInfo playerList)
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
					 writer.Write((byte)LOBBY_COMMANDS.READY);
					 writer.Write((byte)0);

					 writer.Write((byte)LOBBY_COMMANDS.CHANGE_TEAM);
					 writer.Write((byte)1);

					//byte[] bytes = { (byte)LOBBY_COMMANDS.READY, 0 , (byte)LOBBY_COMMANDS.CHANGE_TEAM , 1};
					//writer.Write(bytes, 4);

					connection.Send(driver, writer);
				}
			}
			else if (cmd == NetworkEvent.Type.Data)
			{
				var readerCtx = default(DataStreamReader.Context);
				uint value = stream.ReadUInt(ref readerCtx);
				Debug.Log("ClientLobbyComponent::HandleReceiveData Got the value = " + value + " back from the server");
				connection.Disconnect(driver);
				connection = default(NetworkConnection);
			}
			else if (cmd == NetworkEvent.Type.Disconnect)
			{
				Debug.Log("ClientLobbyComponent::HandleReceiveData Client got disconnected from server");
				connection = default(NetworkConnection);
			}
		}
	}

	private void HandleSendData(ref NetworkConnection connection, ref UdpCNetworkDriver driver, LobbyPlayerInfo playerList)
	{
	}
}

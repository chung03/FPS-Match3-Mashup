using System.Net;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;
using System.Collections.Generic;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using UnityEngine.Assertions;
using LobbyUtils;

public class FakeLobbyServer : MonoBehaviour
{
	public static readonly int MAX_NUM_PLAYERS = 6;

	public UdpCNetworkDriver m_Driver;
	public UdpCNetworkDriver m_BroadcastDriver;
	public NativeList<NetworkConnection> m_Connections;

	List<PersistentPlayerInfo> persistencePlayerInfo = null;

	private byte nextPlayerID = 1;

	[SerializeField]
	private GameObject serverLobbyObj = null;

	[SerializeField]
	private GameObject serverGameObj = null;

	[SerializeField]
	private int connectTimeoutMs = 5000;

	[SerializeField]
	private int disconnectTimeoutMs = 5000;

	private void Start()
	{
		NetworkConfigParameter config = new NetworkConfigParameter();
		config.connectTimeoutMS = connectTimeoutMs;
		config.disconnectTimeoutMS = disconnectTimeoutMs;

		m_Driver = new UdpCNetworkDriver(config);
		if (m_Driver.Bind(new IPEndPoint(IPAddress.Any, 9000)) != 0)
		{
			Debug.Log("ServerConnectionsComponent::Start Failed to bind to port 9000");
		}
		else
		{
			m_Driver.Listen();
		}

		m_Connections = new NativeList<NetworkConnection>(MAX_NUM_PLAYERS, Allocator.Persistent);
		
	}

	void Update()
	{

		m_Driver.ScheduleUpdate().Complete();

		// ***** Handle Connections *****
		HandleConnections(m_Connections, m_Driver);

		// ***** Receive data *****
		HandleReceiveData(m_Connections, m_Driver);
	}

	private void OnDestroy()
	{
		m_Driver.Dispose();
		m_Connections.Dispose();
	}

	private void HandleConnections(NativeList<NetworkConnection> connections, UdpCNetworkDriver driver)
	{
		//Debug.Log("ServerLobbyComponent::HandleConnections Called");

		// Clean up connections
		for (int i = 0; i < connections.Length; i++)
		{
			if (!connections[i].IsCreated)
			{
				Debug.Log("ServerLobbyComponent::HandleConnections Removing a connection");

				//serverLobbyDataComponent.RemovePlayerFromTeam(i);
				connections.RemoveAtSwapBack(i);
				--i;
			}
		}

		// Accept new connections
		NetworkConnection c;
		while ((c = driver.Accept()) != default)
		{
			if (connections.Length >= CONSTANTS.MAX_NUM_PLAYERS)
			{
				Debug.Log("ServerLobbyComponent::HandleConnections Too many connections, rejecting latest one");
				driver.Disconnect(c);
				continue;
			}

			Debug.Log("ServerLobbyComponent::HandleConnections Accepted a connection");

			connections.Add(c);
			//serverLobbyDataComponent.AddPlayerToTeam();
		}
	}

	private void HandleReceiveData(NativeList<NetworkConnection> connections, UdpCNetworkDriver driver)
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

					//serverLobbyDataComponent.ProcessClientBytes(index, bytes);
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

	public byte GetNextPlayerID()
	{
		return nextPlayerID++;
	}

	// Only supposed to be called from ServerGame to get info for connections
	public List<PersistentPlayerInfo> GetGameInfo()
	{
		return persistencePlayerInfo;
	}
}

using System.Net;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;
using System.Collections.Generic;



using UnityEngine.Assertions;
using LobbyUtils;

public class FakeLobbyServer : MonoBehaviour
{
	public static readonly int MAX_NUM_PLAYERS = 6;

	public UdpNetworkDriver m_Driver;
	public UdpNetworkDriver m_BroadcastDriver;
	public NativeList<NetworkConnection> m_Connections;

	List<PersistentPlayerInfo> persistencePlayerInfo = null;

	private byte nextPlayerID = 1;

	[SerializeField]
	private int connectTimeoutMs = 5000;

	[SerializeField]
	private int disconnectTimeoutMs = 5000;

	private void Start()
	{
		//Debug.Log("FakeLobbyServer::Start Called");

		NetworkConfigParameter config = new NetworkConfigParameter();
		config.connectTimeoutMS = connectTimeoutMs;
		config.disconnectTimeoutMS = disconnectTimeoutMs;

		NetworkEndPoint addr = NetworkEndPoint.AnyIpv4;
		addr.Port = 9000;

		m_Driver = new UdpNetworkDriver(config);
		if (m_Driver.Bind(addr) != 0)
		{
			Debug.Log("FakeLobbyServer::Start Failed to bind to port 9000");
		}
		else
		{
			m_Driver.Listen();
		}

		m_Connections = new NativeList<NetworkConnection>(MAX_NUM_PLAYERS, Allocator.Persistent);
		
	}

	private void FixedUpdate()
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

	private void HandleConnections(NativeList<NetworkConnection> connections, UdpNetworkDriver driver)
	{
		//Debug.Log("FakeLobbyServer::HandleConnections Called");

		// Clean up connections
		for (int i = 0; i < connections.Length; i++)
		{
			if (!connections[i].IsCreated)
			{
				Debug.Log("FakeLobbyServer::HandleConnections Removing a connection");

				//serverLobbyDataComponent.RemovePlayerFromTeam(i);
				connections.RemoveAtSwapBack(i);
				--i;
			}
		}

		// Accept new connections
		while (true)
		{
			NetworkConnection con = driver.Accept();

			if (connections.Length >= CONSTANTS.MAX_NUM_PLAYERS)
			{
				Debug.Log("FakeLobbyServer::HandleConnections Too many connections, rejecting latest one");
				driver.Disconnect(con);
				continue;
			}

			// "Nothing more to accept" is signaled by returning an invalid connection from accept
			if (!con.IsCreated)
			{
				Debug.Log("FakeLobbyServer::HandleConnections No new connections");
				break;
			}

			Debug.Log("FakeLobbyServer::HandleConnections Accepted a connection");

			connections.Add(con);
		}
	}

	private void HandleReceiveData(NativeList<NetworkConnection> connections, UdpNetworkDriver driver)
	{
		for (int index = 0; index < connections.Length; ++index)
		{
			if (!connections.IsCreated)
			{
				Debug.Log("FakeLobbyServer::HandleReceiveData connections[" + index + "] was not created");
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
					Debug.Log("FakeLobbyServer::HandleReceiveData Client disconnected from server");
					connections[index] = default;
				}
				else
				{
					Debug.Log("FakeLobbyServer::HandleReceiveData Unhandled Network Event: " + cmd);
				}
			}

			//Debug.Log("FakeLobbyServer::HandleReceiveData Finished processing connection[" + index + "]");
		}
	}

	public byte GetNextPlayerID()
	{
		return nextPlayerID++;
	}

	// Function to be called by tests
	public NativeList<NetworkConnection> GetConnections()
	{
		return m_Connections;
	}
}

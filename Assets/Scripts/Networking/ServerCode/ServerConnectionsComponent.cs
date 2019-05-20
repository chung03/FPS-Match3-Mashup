using System.Net;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;
using System.Collections.Generic;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using UnityEngine.SceneManagement;

using LobbyUtils;

public class ServerConnectionsComponent : MonoBehaviour
{
	public static readonly int MAX_NUM_PLAYERS = 6;

	public UdpCNetworkDriver m_Driver;
	public UdpCNetworkDriver m_BroadcastDriver;
	public NativeList<NetworkConnection> m_Connections;

	List<PersistentPlayerInfo> persistencePlayerInfo;

	private byte nextPlayerID = 1;

	[SerializeField]
	private GameObject serverLobbyObj;

	[SerializeField]
	private GameObject serverGameObj;

	[SerializeField]
	private int connectTimeoutMs = 5000;

	[SerializeField]
	private int disconnectTimeoutMs = 5000;

	private void Start()
	{
		//Debug.Log("ServerConnectionsComponent::Start called");

		// Set timeout to something larger
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
		
		/*
		m_BroadcastDriver = new UdpCNetworkDriver(config);
		if (m_BroadcastDriver.Bind(new IPEndPoint(IPAddress.Any, 6677)) != 0)
		{
			Debug.Log("ServerConnectionsComponent::Start Failed to bind to port 6677");
		}
		else
		{
			m_BroadcastDriver.Listen();
		}
		*/
	}

	private void OnDestroy()
	{
		m_Driver.Dispose();
		m_Connections.Dispose();
	}

	public ref UdpCNetworkDriver GetDriver()
	{
		return ref m_Driver;
	}

	public ref NativeList<NetworkConnection> GetConnections()
	{
		return ref m_Connections;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		Debug.Log("ServerConnectionsComponent::OnSceneLoaded called");
		if (scene.name == "LobbyScene")
		{
			GameObject server = Instantiate(serverLobbyObj);
			server.GetComponent<ServerLobbyComponent>().Init(this);
		}
		else if (scene.name == "PlayScene")
		{
			GameObject server = Instantiate(serverGameObj);
			server.GetComponent<ServerGameComponent>().Init(this);
		}
	}

	public void Init()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	public byte GetNextPlayerID()
	{
		return nextPlayerID++;
	}

	// Only supposed to be called from ServerLobby to set info for connections
	public void SaveGameInfo(List<LobbyPlayerInfo> playerList)
	{
		persistencePlayerInfo = new List<PersistentPlayerInfo>();

		for (int i = 0; i < playerList.Count; ++i)
		{
			PersistentPlayerInfo info = new PersistentPlayerInfo();
			info.name = playerList[i].name;
			info.playerID = playerList[i].playerID;
			info.playerType = playerList[i].playerType;
			info.team = playerList[i].team;

			persistencePlayerInfo.Add(info);
		}
	}

	// Only supposed to be called from ServerGame to get info for connections
	public List<PersistentPlayerInfo> GetGameInfo()
	{
		return persistencePlayerInfo;
	}
}

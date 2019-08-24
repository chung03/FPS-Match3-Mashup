using System.Net;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;

//

using UnityEngine.SceneManagement;
using GameUtils;

public class ClientConnectionsComponent : MonoBehaviour
{
	public UdpNetworkDriver m_Driver;
	public UdpNetworkDriver m_BroadcastDriver;
	public NetworkConnection m_Connection;
	public NetworkConnection m_BroadcastConnection;

	private bool m_IsHost = false;
	private string ipAddress;

	[SerializeField]
	private GameObject clientLobbyObj = null;

	[SerializeField]
	private GameObject clientGameObj = null;

	[SerializeField]
	private int connectTimeoutMs = 5000;

	[SerializeField]
	private int disconnectTimeoutMs = 5000;

	private PersistentPlayerInfo m_PlayerInfo;

	private void Start()
	{
		NetworkConfigParameter config = new NetworkConfigParameter();
		config.connectTimeoutMS = connectTimeoutMs;
		config.disconnectTimeoutMS = disconnectTimeoutMs;

		//Debug.Log("ClientConnectionsComponent::Start called");
		m_Driver = new UdpNetworkDriver(config);
		m_BroadcastDriver = new UdpNetworkDriver(config);
		m_Connection = default;
		m_BroadcastConnection = default;

		if (ipAddress == null)
		{
			ipAddress = "127.0.0.1";
		}

		//var endpoint = new IPEndPoint(ipAddress, 9000);
		NetworkEndPoint endpoint = new NetworkEndPoint();

		if(!NetworkEndPoint.TryParse(ipAddress, 9000, out endpoint))
		{
			Debug.LogError("ClientConnectionsComponent::Start Could not parse IP Address");
		}

		Debug.Log("ClientConnectionsComponent::Start endpoint = " + ipAddress + ":" + endpoint.Port);
		m_Connection = m_Driver.Connect(endpoint);

		Debug.Log("ClientConnectionsComponent::Start Connection state = " + m_Driver.GetConnectionState(m_Connection));
		Debug.Log("ClientConnectionsComponent::Start Remote Endpoint port = " + m_Driver.RemoteEndPoint(m_Connection).Port);
		Debug.Log("ClientConnectionsComponent::Start Local Endpoint port = " + m_Driver.LocalEndPoint().Port);

		/*
		var broadcastEndpoint = new IPEndPoint(IPAddress.Any, 6677);
		m_BroadcastConnection = m_BroadcastDriver.Connect(broadcastEndpoint);
		*/
	}

	private void OnDestroy()
	{
		m_Driver.Dispose();
	}

	public ref UdpNetworkDriver GetDriver()
	{
		return ref m_Driver;
	}

	public ref NetworkConnection GetConnection()
	{
		return ref m_Connection;
	}

	public bool IsHost()
	{
		return m_IsHost;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		PrepareClient(scene.name);
	}

	public void PrepareClient(string sceneName)
	{
		//Debug.Log("ClientConnectionsComponent::OnSceneLoaded called");
		if (sceneName == "LobbyScene")
		{
			Debug.Log("ClientConnectionsComponent::PrepareClient LobbyScene loading");
			GameObject client = Instantiate(clientLobbyObj);
			client.GetComponent<ClientLobbyReceiveComponent>().Init(this);
			client.GetComponent<ClientLobbyDataComponent>().Init(this);
		}
		else if (sceneName == "PlayScene")
		{
			Debug.Log("ClientConnectionsComponent::PrepareClient PlayScene loading");
			GameObject client = Instantiate(clientGameObj);
			client.GetComponent<ClientGameReceiveComponent>().Init(this);
			client.GetComponent<ClientGameDataComponent>().Init(this, m_PlayerInfo);
		}
		else
		{
			Debug.LogError("ClientConnectionsComponent::PrepareClient Wrong scene! sceneName = " + sceneName);
		}
	}

	public void Init(bool _isHost, string _ipAddress)
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
		m_IsHost = _isHost;

		ipAddress = _ipAddress;
	}

	public void SavePlayerInfo(PersistentPlayerInfo playerinfo)
	{
		m_PlayerInfo = playerinfo;
	}
}

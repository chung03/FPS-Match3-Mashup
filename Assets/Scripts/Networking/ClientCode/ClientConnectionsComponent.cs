using System.Net;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using UnityEngine.SceneManagement;

public class ClientConnectionsComponent : MonoBehaviour
{
	public UdpCNetworkDriver m_Driver;
	public NetworkConnection m_Connection;
	public NetworkConnection m_BroadcastConnection;

	private bool m_IsHost = false;
	private int m_PlayerID;
	private IPAddress ipAddress;

	[SerializeField]
	private GameObject clientLobbyObj;

	[SerializeField]
	private GameObject clientGameObj;

	[SerializeField]
	private int connectTimeoutMs = 5000;

	[SerializeField]
	private int disconnectTimeoutMs = 5000;

	private void Start()
	{
		NetworkConfigParameter config = new NetworkConfigParameter();
		config.connectTimeoutMS = connectTimeoutMs;
		config.disconnectTimeoutMS = disconnectTimeoutMs;

		//Debug.Log("ClientConnectionsComponent::Start called");
		m_Driver = new UdpCNetworkDriver(config);
		m_Connection = default;
		m_BroadcastConnection = default;

		//var endpoint = new IPEndPoint(IPAddress.Loopback, 9000);
		var endpoint = new IPEndPoint(ipAddress, 9000);
		m_Connection = m_Driver.Connect(endpoint);

		//var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, 6677);
		//m_BroadcastConnection = m_Driver.Connect(broadcastEndpoint);
	}

	private void OnDestroy()
	{
		m_Driver.Dispose();
	}

	public ref UdpCNetworkDriver GetDriver()
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
		//Debug.Log("ClientConnectionsComponent::OnSceneLoaded called");
		if (scene.name == "LobbyScene")
		{
			GameObject client = Instantiate(clientLobbyObj);
			client.GetComponent<ClientLobbyComponent>().Init(this);
		}
		else if (scene.name == "PlayScene")
		{
			GameObject client = Instantiate(clientGameObj);
			client.GetComponent<ClientGameComponent>().Init(this);
		}
	}

	public void Init(bool _isHost, IPAddress _ipAddress)
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
		m_IsHost = _isHost;

		ipAddress = _ipAddress;
	}

	public void SavePlayerID(int playerID)
	{
		m_PlayerID = playerID;
	}
}

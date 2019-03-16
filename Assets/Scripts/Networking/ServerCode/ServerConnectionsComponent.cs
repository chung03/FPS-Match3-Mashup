using System.Net;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using UnityEngine.SceneManagement;

public class ServerConnectionsComponent : MonoBehaviour
{
	public static readonly int MAX_NUM_PLAYERS = 6;

	public UdpCNetworkDriver m_Driver;
	public NativeList<NetworkConnection> m_Connections;

	[SerializeField]
	private GameObject serverLobbyObj;

	[SerializeField]
	private GameObject serverGameObj;

	private void Start()
	{
		//Debug.Log("ServerConnectionsComponent::Start called");

		m_Driver = new UdpCNetworkDriver(new INetworkParameter[0]);
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
		else if (scene.name == "GameScene")
		{
			Instantiate(serverGameObj);
		}
	}

	public void Init()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
	}
}

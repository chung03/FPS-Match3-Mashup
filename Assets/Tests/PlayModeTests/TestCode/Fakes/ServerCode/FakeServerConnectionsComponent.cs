using System.Net;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;
using System.Collections.Generic;



using UnityEngine.SceneManagement;

using LobbyUtils;

public class FakeServerConnectionsComponent : MonoBehaviour
{
	public static readonly int MAX_NUM_PLAYERS = 6;

	public UdpNetworkDriver m_Driver;
	public UdpNetworkDriver m_BroadcastDriver;
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
		//Debug.Log("FakeServerConnectionsComponent::Start called");

		// Set timeout to something larger
		NetworkConfigParameter config = new NetworkConfigParameter();
		config.connectTimeoutMS = connectTimeoutMs;
		config.disconnectTimeoutMS = disconnectTimeoutMs;

		NetworkEndPoint addr = NetworkEndPoint.AnyIpv4;
		addr.Port = 9000;

		m_Driver = new UdpNetworkDriver(config);
		if (m_Driver.Bind(addr) != 0)
		{
			Debug.Log("FakeServerConnectionsComponent::Start Failed to bind to port 9000");
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

	public ref UdpNetworkDriver GetDriver()
	{
		return ref m_Driver;
	}

	public ref NativeList<NetworkConnection> GetConnections()
	{
		return ref m_Connections;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		PrepareServer(scene.name);
	}

	public void Init()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	public void PrepareServer(string sceneName)
	{
		Debug.Log("FakeServerConnectionsComponent::OnSceneLoaded called");
		if (sceneName == "LobbyScene")
		{
			GameObject server = Instantiate(serverLobbyObj);
			server.GetComponent<FakeServerLobbyReceiveComponent>().Init(this);
			server.GetComponent<FakeServerLobbyDataComponent>().Init(this);
		}
		else if (sceneName == "PlayScene")
		{
			GameObject server = Instantiate(serverGameObj);
			server.GetComponent<FakeServerGameReceiveComponent>().Init(this);
			server.GetComponent<FakeServerGameDataComponent>().Init(this);
		}
	}

	public byte GetNextPlayerID()
	{
		return nextPlayerID++;
	}

	// Only supposed to be called from ServerLobby to set info for connections
	public void SaveGameInfo(List<PersistentPlayerInfo> playerList)
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

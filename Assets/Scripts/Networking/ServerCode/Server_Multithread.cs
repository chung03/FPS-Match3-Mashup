using System.Net;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;
using Unity.Jobs;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using ServerJobs;
using ServerUtils;
using Util;
using UnityEngine.SceneManagement;

public class Server_Multithread : MonoBehaviour
{
	public static readonly int MAX_NUM_PLAYERS = 6;

	public UdpCNetworkDriver m_Driver;

	public NativeList<NetworkConnection> m_Connections;
	public NativeList<PlayerInfo> m_PlayerList;
	private JobHandle ServerJobHandle;

	private SERVER_MODE m_CurrentMode;

	private void Start()
	{
		m_CurrentMode = SERVER_MODE.GAME_MODE;

		m_Driver = new UdpCNetworkDriver(new INetworkParameter[0]);
		if (m_Driver.Bind(new IPEndPoint(IPAddress.Any, 9000)) != 0)
			Debug.Log("Failed to bind to port 9000");
		else
			m_Driver.Listen();

		m_Connections = new NativeList<NetworkConnection>(MAX_NUM_PLAYERS, Allocator.Persistent);
		m_PlayerList = new NativeList<PlayerInfo>(MAX_NUM_PLAYERS, Allocator.Persistent);
	}

	public void Init()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		Debug.Log("Server_Multithread::OnSceneLoaded called");
		GetComponent<ServerUI>().SetUI(scene.name);
	}

	public void SetMode(SERVER_MODE newMode)
	{
		m_CurrentMode = newMode;
	}

	void OnDestroy()
	{
		ServerJobHandle.Complete();
		m_Driver.Dispose();
		m_Connections.Dispose();
		m_PlayerList.Dispose();
	}

	void Update()
	{
		ServerJobHandle.Complete();

		var connectionJob = new ServerUpdateConnectionsJob
		{
			driver = m_Driver,
			connections = m_Connections,
			playersList = m_PlayerList
		};

		ServerJobHandle = m_Driver.ScheduleUpdate();
		ServerJobHandle = connectionJob.Schedule(ServerJobHandle);

		// Must complete here because reading m_Connections before the job completes is wrong
		ServerJobHandle.Complete();
		ServerJobHandle = HandleData(m_CurrentMode, m_Driver, m_Connections, ServerJobHandle);
	}

	private JobHandle HandleData(SERVER_MODE currentMode, 
									UdpCNetworkDriver driver, 
									NativeList<NetworkConnection> connections,
									JobHandle dependencies)
	{
		if (currentMode == SERVER_MODE.GAME_MODE)
		{
			return new ServerGameJob
			{
				driver = driver.ToConcurrent(),
				connections = connections.ToDeferredJobArray()
			}.Schedule(connections.Length, 1, dependencies);
		}
		else
		{
			return new ServerLobbyJob
			{
				driver = driver.ToConcurrent(),
				connections = connections.ToDeferredJobArray()
			}.Schedule(connections.Length, 1, dependencies);
		}
	}
}

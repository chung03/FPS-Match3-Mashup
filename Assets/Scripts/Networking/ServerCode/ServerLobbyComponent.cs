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

public class ServerLobbyComponent : MonoBehaviour
{
	public static readonly int MAX_NUM_PLAYERS = 6;
	
	public NativeList<LobbyPlayerInfo> m_PlayerList;
	public NativeArray<byte> m_playerNames;

	ServerConnectionsComponent connectionsComponent;

	private void Start()
	{
		connectionsComponent = GetComponent<ServerConnectionsComponent>();
		m_PlayerList = new NativeList<LobbyPlayerInfo>(MAX_NUM_PLAYERS, Allocator.Persistent);
	}

	public void Init()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		Debug.Log("ServerLobbyComponent::OnSceneLoaded called");
		GetComponent<ServerUI>().SetUI(scene.name);
	}

	private void OnDestroy()
	{
		m_PlayerList.Dispose();
	}

	void Update()
	{
		ref UdpCNetworkDriver driver = ref connectionsComponent.GetDriver();
		ref NativeList<NetworkConnection> connections = ref connectionsComponent.GetConnections();
		ref JobHandle serverJobHandle = ref connectionsComponent.GetServerHandle();

		serverJobHandle.Complete();

		// ***** Handle Connections *****
		var connectionJob = new ServerUpdateConnectionsJob
		{
			driver = driver,
			connections = connections,
			playersList = m_PlayerList
		};
		serverJobHandle = driver.ScheduleUpdate();
		serverJobHandle = connectionJob.Schedule(serverJobHandle);

		// ***** Receive data *****
		// Must complete here because reading m_Connections before the job completes is wrong
		serverJobHandle.Complete();
		serverJobHandle = new ServerReceiveLobbyJob
		{
			driver = driver.ToConcurrent(),
			connections = connections.ToDeferredJobArray(),
			playerList = m_PlayerList.ToDeferredJobArray()
		}.Schedule(connections.Length, 1, serverJobHandle);

		// ***** Receive string data *****
		// This has to be done on the main thread because blittable types must be sent


		// ***** Process data *****


		// ***** Send data *****

		// ***** Send string data *****
	}
}

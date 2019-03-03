using System.Net;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;
using Unity.Jobs;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using ServerJobs;
using ServerUtils;

public class Server_Multithread : MonoBehaviour
{
	public UdpCNetworkDriver m_Driver;
	public NativeList<NetworkConnection> m_Connections;
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

		m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
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
	}

	void Update()
	{
		ServerJobHandle.Complete();

		var connectionJob = new ServerUpdateConnectionsJob
		{
			driver = m_Driver,
			connections = m_Connections
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

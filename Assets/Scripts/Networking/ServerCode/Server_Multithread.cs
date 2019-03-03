using System.Net;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Assertions;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using ServerJobs;
using ServerUtils;

public class Server_Multithread : MonoBehaviour
{
	public UdpCNetworkDriver m_Driver;
	public NativeList<NetworkConnection> m_Connections;
	private JobHandle ServerJobHandle;

	private SERVER_MODE currentMode;

	private void Start()
	{
		currentMode = SERVER_MODE.GAME_MODE;

		m_Driver = new UdpCNetworkDriver(new INetworkParameter[0]);
		if (m_Driver.Bind(new IPEndPoint(IPAddress.Any, 9000)) != 0)
			Debug.Log("Failed to bind to port 9000");
		else
			m_Driver.Listen();

		m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
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

		var serverUpdateJob = new ServerUpdateJob
		{
			driver = m_Driver.ToConcurrent(),
			connections = m_Connections.ToDeferredJobArray()
		};
		
		ServerJobHandle = m_Driver.ScheduleUpdate();
		ServerJobHandle = connectionJob.Schedule(ServerJobHandle);

		// Must complete here because reading m_Connections before the job completes is wrong
		ServerJobHandle.Complete();
		ServerJobHandle = serverUpdateJob.Schedule(m_Connections.Length, 1, ServerJobHandle);
	}

	struct ServerUpdateConnectionsJob : IJob
	{
		public UdpCNetworkDriver driver;
		public NativeList<NetworkConnection> connections;

		public void Execute()
		{
			// Clean up connections
			for (int i = 0; i < connections.Length; i++)
			{
				if (!connections[i].IsCreated)
				{
					connections.RemoveAtSwapBack(i);
					--i;
				}
			}
			// Accept new connections
			NetworkConnection c;
			while ((c = driver.Accept()) != default(NetworkConnection))
			{
				connections.Add(c);
				Debug.Log("Accepted a connection");
			}
		}
	}
}

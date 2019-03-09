using System.Net;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;
using Unity.Jobs;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using ClientJobs;

public class Client_Multithread : MonoBehaviour
{
	public UdpCNetworkDriver m_Driver;
	public NativeArray<NetworkConnection> m_Connection;
	public NativeArray<byte> m_Done;
	public JobHandle ClientJobHandle;

	public void OnDestroy()
	{
		ClientJobHandle.Complete();

		m_Connection.Dispose();
		m_Driver.Dispose();
		m_Done.Dispose();
	}

	private void Start()
	{
		m_Driver = new UdpCNetworkDriver(new INetworkParameter[0]);
		m_Connection = new NativeArray<NetworkConnection>(1, Allocator.Persistent);
		m_Done = new NativeArray<byte>(1, Allocator.Persistent);

		var endpoint = new IPEndPoint(IPAddress.Loopback, 9000);
		m_Connection[0] = m_Driver.Connect(endpoint);
	}

	public void Update() {
		ClientJobHandle.Complete();

		var readJob = new ClientReadLobbyJob
		{
			driver = m_Driver,
			connection = m_Connection,
			ready = m_Done
		};

		ClientJobHandle = m_Driver.ScheduleUpdate();
		ClientJobHandle = readJob.Schedule(ClientJobHandle);

		var sendJob = new ClientSendLobbyJob
		{
			driver = m_Driver,
			connection = m_Connection,
			ready = m_Done
		};

		ClientJobHandle = sendJob.Schedule(ClientJobHandle);
	}
}
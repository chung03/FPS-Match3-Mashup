using System.Net;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;
using Unity.Jobs;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

public class ServerConnectionsComponent : MonoBehaviour
{
	public static readonly int MAX_NUM_PLAYERS = 6;

	public UdpCNetworkDriver m_Driver;
	public NativeList<NetworkConnection> m_Connections;
	public JobHandle ServerJobHandle;

	private void Start()
	{
		m_Driver = new UdpCNetworkDriver(new INetworkParameter[0]);
		if (m_Driver.Bind(new IPEndPoint(IPAddress.Any, 9000)) != 0)
			Debug.Log("Failed to bind to port 9000");
		else
			m_Driver.Listen();

		m_Connections = new NativeList<NetworkConnection>(MAX_NUM_PLAYERS, Allocator.Persistent);
	}

	private void OnDestroy()
	{
		ServerJobHandle.Complete();
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

	public ref JobHandle GetServerHandle()
	{
		return ref ServerJobHandle;
	}
}

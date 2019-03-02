using System.Net;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;
using Unity.Jobs;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

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

		var job = new ClientUpdateJob
		{
			driver = m_Driver,
			connection = m_Connection,
			done = m_Done
		};

		ClientJobHandle = m_Driver.ScheduleUpdate();
		ClientJobHandle = job.Schedule(ClientJobHandle);
	}

	struct ClientUpdateJob : IJob
	{
		public UdpCNetworkDriver driver;
		public NativeArray<NetworkConnection> connection;
		public NativeArray<byte> done;

		public void Execute()
		{
			if (!connection[0].IsCreated)
			{
				// Remember that its not a bool anymore.
				if (done[0] != 1)
					Debug.Log("Something went wrong during connect");
				return;
			}
			DataStreamReader stream;
			NetworkEvent.Type cmd;

			while ((cmd = connection[0].PopEvent(driver, out stream)) !=
				   NetworkEvent.Type.Empty)
			{
				if (cmd == NetworkEvent.Type.Connect)
				{
					Debug.Log("We are now connected to the server");

					var value = 1;
					using (var writer = new DataStreamWriter(4, Allocator.Temp))
					{
						writer.Write(value);
						connection[0].Send(driver, writer);
					}
				}
				else if (cmd == NetworkEvent.Type.Data)
				{
					var readerCtx = default(DataStreamReader.Context);
					uint value = stream.ReadUInt(ref readerCtx);
					Debug.Log("Got the value = " + value + " back from the server");
					// And finally change the `done[0]` to `1`
					done[0] = 1;
					connection[0].Disconnect(driver);
					connection[0] = default(NetworkConnection);
				}
				else if (cmd == NetworkEvent.Type.Disconnect)
				{
					Debug.Log("Client got disconnected from server");
					connection[0] = default(NetworkConnection);
				}
			}
		}
	}
}
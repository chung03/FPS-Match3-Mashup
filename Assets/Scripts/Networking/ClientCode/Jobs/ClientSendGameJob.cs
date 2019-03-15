using System.Net;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;
using Unity.Jobs;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using Util;
namespace ClientJobs
{
	struct ClientSendGameJob : IJob
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

			using (var writer = new DataStreamWriter(4, Allocator.Temp))
			{
				writer.Write(1);
				connection[0].Send(driver, writer);
			}
		}
	}
}
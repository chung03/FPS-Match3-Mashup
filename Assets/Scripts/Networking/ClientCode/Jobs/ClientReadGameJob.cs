using System.Net;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;
using Unity.Jobs;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using Util;
namespace ClientJobs
{
	struct ClientReadGameJob : IJob
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
					byte serverCmd = stream.ReadByte(ref readerCtx);
					Debug.Log("Got the command = " + serverCmd + " back from the server");

					if (serverCmd == (byte)LOBBY_COMMANDS.GET_ID)
					{

					}

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
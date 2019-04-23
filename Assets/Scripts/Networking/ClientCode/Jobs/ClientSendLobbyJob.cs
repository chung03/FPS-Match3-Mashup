using System.Net;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;
using Unity.Jobs;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using LobbyUtils;
namespace ClientJobs
{
	struct ClientSendLobbyJob : IJob
	{
		public UdpCNetworkDriver driver;
		public NativeArray<NetworkConnection> connection;
		public NativeArray<byte> ready;

		public void Execute()
		{
			DataStreamReader stream;
			NetworkEvent.Type cmd;

			while ((cmd = connection[0].PopEvent(driver, out stream)) !=
				   NetworkEvent.Type.Empty)
			{
				if (cmd == NetworkEvent.Type.Connect)
				{
					Debug.Log("We are now connected to the server. Sending Ready");
					
					using (var writer = new DataStreamWriter(4, Allocator.Temp))
					{
						writer.Write((byte)LOBBY_SERVER_COMMANDS.READY);
						writer.Write(ready[0]);
						connection[0].Send(driver, writer);
					}
				}
				else if (cmd == NetworkEvent.Type.Data)
				{
					var readerCtx = default(DataStreamReader.Context);
					byte serverCmd = stream.ReadByte(ref readerCtx);
					Debug.Log("Got the command = " + serverCmd + " back from the server");

					if (serverCmd == (byte)LOBBY_SERVER_COMMANDS.SET_ID)
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
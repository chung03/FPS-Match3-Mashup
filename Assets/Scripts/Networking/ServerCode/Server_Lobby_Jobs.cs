using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Assertions;
using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

namespace ServerJobs
{
	public struct ServerLobbyJob : IJobParallelFor
	{
		public UdpCNetworkDriver.Concurrent driver;
		public NativeArray<NetworkConnection> connections;

		public void Execute(int index)
		{
			Debug.Log("ServerLobbyJob connections.Length = " + connections.Length + ", index = " + index);

			if (!connections[index].IsCreated)
			{
				Debug.Log("ServerLobbyJob connections[" + index + "] was not created");
				Assert.IsTrue(true);
			}

			NetworkEvent.Type cmd;
			DataStreamReader stream;
			while ((cmd = driver.PopEventForConnection(connections[index], out stream)) !=
					NetworkEvent.Type.Empty)
			{
				if (cmd == NetworkEvent.Type.Data)
				{
					var readerCtx = default(DataStreamReader.Context);
					uint number = stream.ReadUInt(ref readerCtx);

					Debug.Log("Got " + number + " from the Client adding + 2 to it.");
					number += 2;

					using (var writer = new DataStreamWriter(4, Allocator.Temp))
					{
						//writer.Write(System.Text.Encoding.UTF8.GetBytes("abcd"), 4);
						writer.Write(number);
						driver.Send(connections[index], writer);
					}
				}
				else if (cmd == NetworkEvent.Type.Disconnect)
				{
					Debug.Log("Client disconnected from server");
					connections[index] = default(NetworkConnection);
				}
				else
				{
					Debug.Log("Unhandled Network Event: " + cmd);
				}
			}

			Debug.Log("ServerLobbyJob Finished processing connection[" + index + "]");
		}
	}
}

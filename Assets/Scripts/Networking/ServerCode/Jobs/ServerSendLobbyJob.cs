using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Assertions;
using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using Util;

namespace ServerJobs
{
	/*
	public struct ServerSendLobbyJob : IJobParallelFor
	{
		public UdpCNetworkDriver.Concurrent driver;
		public NativeArray<NetworkConnection> connections;
		public NativeArray<LobbyPlayerInfo> playerList;

		public void Execute(int index)
		{
			Debug.Log("ServerSendLobbyJob connections.Length = " + connections.Length + ", index = " + index);

			if (!connections[index].IsCreated)
			{
				Debug.Log("ServerSendLobbyJob connections[" + index + "] was not created");
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
					byte clientCmd = stream.ReadByte(ref readerCtx);

					Debug.Log("Got " + clientCmd + " from the Client");

					if (clientCmd == (byte)LOBBY_COMMANDS.READY)
					{
						byte readyStatus = stream.ReadByte(ref readerCtx);
						LobbyPlayerInfo newInfo = playerList[index];
						newInfo.isReady = readyStatus;
						playerList[index] = newInfo;
					}

					using (var writer = new DataStreamWriter(4, Allocator.Temp))
					{
						//writer.Write(System.Text.Encoding.UTF8.GetBytes("abcd"), 4);
						writer.Write(clientCmd);
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

			Debug.Log("ServerSendLobbyJob Finished processing connection[" + index + "]");
		}
	}
	*/
}

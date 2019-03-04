using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;
using Unity.Jobs;
using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using Util;

namespace ServerUtils
{
	public enum SERVER_MODE
	{
		LOBBY_MODE,
		GAME_MODE
	}

	public struct ServerUpdateConnectionsJob : IJob
	{
		public UdpCNetworkDriver driver;
		public NativeList<NetworkConnection> connections;
		public NativeList<PlayerInfo> playersList;

		public void Execute()
		{
			// Clean up connections
			for (int i = 0; i < connections.Length; i++)
			{
				if (!connections[i].IsCreated)
				{
					connections.RemoveAtSwapBack(i);
					playersList.RemoveAtSwapBack(i);
					--i;
				}
			}
			// Accept new connections
			NetworkConnection c;
			while ((c = driver.Accept()) != default(NetworkConnection))
			{
				connections.Add(c);
				playersList.Add(new PlayerInfo());
				Debug.Log("Accepted a connection");
			}
		}
	}
}

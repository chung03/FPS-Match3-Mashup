﻿using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;
using Unity.Jobs;


using LobbyUtils;

namespace ServerUtils
{
	/*
	public enum SERVER_MODE
	{
		LOBBY_MODE,
		GAME_MODE
	}

	public struct ServerUpdateConnectionsJob : IJob
	{
		public UdpNetworkDriver driver;
		public NativeList<NetworkConnection> connections;
		public NativeList<LobbyPlayerInfo> playersList;

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
				playersList.Add(new LobbyPlayerInfo());
				Debug.Log("Accepted a connection");
			}
		}
	}
	*/
}

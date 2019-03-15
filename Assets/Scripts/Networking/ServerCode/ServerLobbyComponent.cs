using System.Net;
using System.Collections.Generic;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using UnityEngine.Assertions;
using Util;
using UnityEngine.SceneManagement;

public class ServerLobbyComponent : MonoBehaviour
{
	public static readonly int MAX_NUM_PLAYERS = 6;
	
	public List<LobbyPlayerInfo> m_PlayerList;

	ServerConnectionsComponent connectionsComponent;

	private void Start()
	{
		m_PlayerList = new List<LobbyPlayerInfo>(MAX_NUM_PLAYERS);
	}


	public void Init(ServerConnectionsComponent connHolder)
	{
		connectionsComponent = connHolder;
	}

	void Update()
	{
		ref UdpCNetworkDriver driver = ref connectionsComponent.GetDriver();
		ref NativeList<NetworkConnection> connections = ref connectionsComponent.GetConnections();

		// ***** Handle Connections *****
		HandleConnections(ref connections, ref driver, m_PlayerList);

		// ***** Receive data *****
		HandleReceiveData(ref connections, ref driver, m_PlayerList);

		// ***** Process data *****


		// ***** Send data *****
		HandleSendData(ref connections, ref driver, m_PlayerList);
	}

	private void HandleConnections(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver, List<LobbyPlayerInfo> playerList)
	{
		// Clean up connections
		for (int i = 0; i < connections.Length; i++)
		{
			if (!connections[i].IsCreated)
			{
				connections.RemoveAtSwapBack(i);
				playerList.RemoveAtSwapBack(i);
				--i;
			}
		}
		// Accept new connections
		NetworkConnection c;
		while ((c = driver.Accept()) != default(NetworkConnection))
		{
			connections.Add(c);
			playerList.Add(new LobbyPlayerInfo());
			Debug.Log("Accepted a connection");
		}
	}

	private void HandleReceiveData(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver, List<LobbyPlayerInfo> playerList)
	{
		for (int index = 0; index < connections.Length; ++index)
		{
			if (!connections.IsCreated)
			{
				Debug.Log("ServerLobbyComponent::HandleReceiveData connections[" + index + "] was not created");
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
						

						Debug.Log("A Client " + index + " ready state set to " + readyStatus);
					}
					else if (clientCmd == (byte)LOBBY_COMMANDS.CHANGE_TEAM)
					{
						byte newTeam = stream.ReadByte(ref readerCtx);

						LobbyPlayerInfo newInfo = playerList[index];
						newInfo.team = newTeam;
						playerList[index] = newInfo;


						Debug.Log("A Client " + index + " team was set to " + newTeam);
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

			Debug.Log("ServerLobbyComponent::HandleReceiveData Finished processing connection[" + index + "]");
		}
	}

	private void HandleSendData(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver, List<LobbyPlayerInfo> playerList)
	{
	}
}

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
		//Debug.Log("ServerLobbyComponent::Start Called");
		m_PlayerList = new List<LobbyPlayerInfo>(MAX_NUM_PLAYERS);
	}


	public void Init(ServerConnectionsComponent connHolder)
	{
		//Debug.Log("ServerLobbyComponent::Init Called");
		connectionsComponent = connHolder;
	}

	void Update()
	{
		ref UdpCNetworkDriver driver = ref connectionsComponent.GetDriver();
		ref NativeList<NetworkConnection> connections = ref connectionsComponent.GetConnections();

		driver.ScheduleUpdate().Complete();

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
		//Debug.Log("ServerLobbyComponent::HandleConnections Called");

		// Clean up connections
		for (int i = 0; i < connections.Length; i++)
		{
			if (!connections[i].IsCreated)
			{
				Debug.Log("ServerLobbyComponent::HandleConnections Removing a connection");

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
			Debug.Log("ServerLobbyComponent::HandleConnections Accepted a connection");
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
					ReadClientBytes(index, playerList, stream);
				}
				else if (cmd == NetworkEvent.Type.Disconnect)
				{
					Debug.Log("ServerLobbyComponent::HandleReceiveData Client disconnected from server");
					connections[index] = default(NetworkConnection);
				}
				else
				{
					Debug.Log("ServerLobbyComponent::HandleReceiveData Unhandled Network Event: " + cmd);
				}
			}

			//Debug.Log("ServerLobbyComponent::HandleReceiveData Finished processing connection[" + index + "]");
		}
	}

	private void ReadClientBytes(int index, List<LobbyPlayerInfo> playerList, DataStreamReader stream)
	{
		var readerCtx = default(DataStreamReader.Context);
		

		Debug.Log("ServerLobbyComponent::ReadClientBytes stream.Length = " + stream.Length);

		byte[] bytes = stream.ReadBytesAsArray(ref readerCtx, stream.Length);

		for (int i = 0; i < stream.Length; ++i)
		{
			byte clientCmd = bytes[i];
			++i;

			Debug.Log("ServerLobbyComponent::ReadClientBytes Got " + clientCmd + " from the Client");

			if (clientCmd == (byte)LOBBY_COMMANDS.READY)
			{
				byte readyStatus = bytes[i];

				LobbyPlayerInfo newInfo = playerList[index];
				newInfo.isReady = readyStatus;
				playerList[index] = newInfo;


				Debug.Log("ServerLobbyComponent::ReadClientBytes Client " + index + " ready state set to " + readyStatus);
			}
			else if (clientCmd == (byte)LOBBY_COMMANDS.CHANGE_TEAM)
			{
				byte newTeam = bytes[i];

				LobbyPlayerInfo newInfo = playerList[index];
				newInfo.team = newTeam;
				playerList[index] = newInfo;

				Debug.Log("ServerLobbyComponent::ReadClientBytes Client " + index + " team was set to " + newTeam);
			}
		}
	}

	private void HandleSendData(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver, List<LobbyPlayerInfo> playerList)
	{
	}
}

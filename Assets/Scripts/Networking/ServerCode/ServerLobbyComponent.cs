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

	//private Queue<KeyValuePair<byte, int>> sendQueue;

	ServerConnectionsComponent connectionsComponent;
	 
	private void Start()
	{
		//Debug.Log("ServerLobbyComponent::Start Called");
		m_PlayerList = new List<LobbyPlayerInfo>(MAX_NUM_PLAYERS);
		//sendQueue = new Queue<KeyValuePair<byte, int>>();
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
					var readerCtx = default(DataStreamReader.Context);
					byte[] bytes = stream.ReadBytesAsArray(ref readerCtx, stream.Length);
					
					ReadClientBytes(index, playerList, ref connections, ref driver, bytes);
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

	private void ReadClientBytes(int index, List<LobbyPlayerInfo> playerList, ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver, byte[] bytes)
	{
		Debug.Log("ServerLobbyComponent::ReadClientBytes bytes.Length = " + bytes.Length);

		for (int i = 0; i < bytes.Length;)
		{
			byte clientCmd = bytes[i];

			// Unsafely assuming that everything is working as expected and there are no attackers.
			++i;

			Debug.Log("ServerLobbyComponent::ReadClientBytes Got " + clientCmd + " from the Client");

			if (clientCmd == (byte)LOBBY_CLIENT_REQUESTS.READY)
			{
				byte readyStatus = bytes[i];
				++i;

				playerList[index].isReady = readyStatus;


				Debug.Log("ServerLobbyComponent::ReadClientBytes Client " + index + " ready state set to " + readyStatus);
			}
			else if (clientCmd == (byte)LOBBY_CLIENT_REQUESTS.CHANGE_TEAM)
			{
				byte newTeam = bytes[i];
				++i;

				playerList[index].team = newTeam;

				Debug.Log("ServerLobbyComponent::ReadClientBytes Client " + index + " team was set to " + newTeam);
			}
			else if (clientCmd == (byte)LOBBY_CLIENT_REQUESTS.GET_ID)
			{
				Debug.Log("ServerLobbyComponent::ReadClientBytes Client " + index + " sent request for its ID");
				using (var writer = new DataStreamWriter(16, Allocator.Temp))
				{
					writer.Write((byte)LOBBY_SERVER_COMMANDS.SET_ID);
					writer.Write((byte)index);

					connections[index].Send(driver, writer);
				}
			}
		}
	}

	// For now, send entire lobby state to all players
	private void HandleSendData(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver, List<LobbyPlayerInfo> playerList)
	{
		for (int index = 0; index < connections.Length; ++index)
		{
			if (!connections.IsCreated)
			{
				Debug.Log("ServerLobbyComponent::HandleReceiveData connections[" + index + "] was not created");
				Assert.IsTrue(true);
			}

			// Send state of all players
			using (var writer = new DataStreamWriter(16, Allocator.Temp))
			{
				writer.Write((byte)LOBBY_SERVER_COMMANDS.SET_ALL_PLAYER_STATES);

				// Send data for present players
				for ( int playerNum = 0; playerNum < playerList.Count; playerNum++)
				{
					// Tell Client this player is really there
					writer.Write((byte)1);

					// Write player info
					writer.Write((byte)playerList[playerNum].isReady);
					writer.Write((byte)playerList[playerNum].team);
				}

				// Send data saying that some player slots aren't filled
				for (int playerNum = playerList.Count; playerNum < MAX_NUM_PLAYERS; playerNum++)
				{
					// Tell Client this player is not there
					writer.Write((byte)0);
				}

				connections[index].Send(driver, writer);
			}
		}
	}
}

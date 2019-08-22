using System.Collections.Generic;
using UnityEngine;
using CommonNetworkingUtils;

using Unity.Networking.Transport;
using Unity.Collections;


using UnityEngine.Assertions;
using LobbyUtils;

public class ServerLobbyReceiveComponent : MonoBehaviour
{
	private enum LOBBY_SERVER_PROCESS
	{
		START_GAME,
		CHANGE_PLAYER_TYPE
	}

	private ServerConnectionsComponent connectionsComponent;
	private ServerLobbyDataComponent serverLobbyDataComponent;
	private ServerLobbySend serverLobbySend;


	public void Init(ServerConnectionsComponent connHolder)
	{
		//Debug.Log("ServerLobbyComponent::Init Called");
		connectionsComponent = connHolder;
		serverLobbySend = GetComponent<ServerLobbySend>();
		serverLobbyDataComponent = GetComponent<ServerLobbyDataComponent>();
	}

	void Update()
	{
		ref UdpNetworkDriver driver = ref connectionsComponent.GetDriver();
		ref NativeList<NetworkConnection> connections = ref connectionsComponent.GetConnections();

		driver.ScheduleUpdate().Complete();

		// ***** Handle Connections *****
		HandleConnections(ref connections, ref driver);

		// ***** Receive data *****
		HandleReceiveData(ref connections, ref driver);
	}

	private void HandleConnections(ref NativeList<NetworkConnection> connections, ref UdpNetworkDriver driver)
	{
		//Debug.Log("ServerLobbyComponent::HandleConnections Called");

		// Clean up connections
		for (int i = 0; i < connections.Length; i++)
		{
			if (!connections[i].IsCreated)
			{
				Debug.Log("ServerLobbyComponent::HandleConnections Removing a connection");

				serverLobbyDataComponent.RemovePlayerFromTeam(i);
				connections.RemoveAtSwapBack(i);
				--i;
			}
		}

		// Accept new connections
		NetworkConnection c;
		while ((c = driver.Accept()) != default)
		{
			if (connections.Length >= CONSTANTS.MAX_NUM_PLAYERS)
			{
				Debug.Log("ServerLobbyComponent::HandleConnections Too many connections, rejecting latest one");
				driver.Disconnect(c);
				continue;
			}

			Debug.Log("ServerLobbyComponent::HandleConnections Accepted a connection");

			connections.Add(c);
			serverLobbyDataComponent.AddPlayerToTeam();
		}
	}

	private void HandleReceiveData(ref NativeList<NetworkConnection> connections, ref UdpNetworkDriver driver)
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

					serverLobbyDataComponent.ProcessClientBytes(index, bytes);
				}
				else if (cmd == NetworkEvent.Type.Disconnect)
				{
					Debug.Log("ServerLobbyComponent::HandleReceiveData Client disconnected from server");
					connections[index] = default;
				}
				else
				{
					Debug.Log("ServerLobbyComponent::HandleReceiveData Unhandled Network Event: " + cmd);
				}
			}

			//Debug.Log("ServerLobbyComponent::HandleReceiveData Finished processing connection[" + index + "]");
		}
	}
}

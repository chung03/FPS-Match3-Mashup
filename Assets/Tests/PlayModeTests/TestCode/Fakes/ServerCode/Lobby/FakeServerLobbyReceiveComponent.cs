using System.Collections.Generic;
using UnityEngine;
using CommonNetworkingUtils;

using Unity.Networking.Transport;
using Unity.Collections;


using UnityEngine.Assertions;
using LobbyUtils;

public class FakeServerLobbyReceiveComponent : MonoBehaviour
{
	private enum LOBBY_SERVER_PROCESS
	{
		START_GAME,
		CHANGE_PLAYER_TYPE
	}

	private FakeServerConnectionsComponent connectionsComponent;
	private FakeServerLobbyDataComponent serverLobbyDataComponent;
	private FakeServerLobbySend serverLobbySend;


	public void Init(FakeServerConnectionsComponent connHolder)
	{
		//Debug.Log("ServerLobbyComponent::Init Called");
		connectionsComponent = connHolder;
		serverLobbySend = GetComponent<FakeServerLobbySend>();
		serverLobbyDataComponent = GetComponent<FakeServerLobbyDataComponent>();
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
				Debug.Log("FakeServerLobbyReceiveComponent::HandleConnections Removing a connection");

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
				Debug.Log("FakeServerLobbyReceiveComponent::HandleConnections Too many connections, rejecting latest one");
				driver.Disconnect(c);
				continue;
			}

			Debug.Log("FakeServerLobbyReceiveComponent::HandleConnections Accepted a connection");

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
				Debug.Log("FakeServerLobbyReceiveComponent::HandleReceiveData connections[" + index + "] was not created");
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
					Debug.Log("FakeServerLobbyReceiveComponent::HandleReceiveData Client disconnected from server");
					connections[index] = default;
				}
				else
				{
					Debug.Log("FakeServerLobbyReceiveComponent::HandleReceiveData Unhandled Network Event: " + cmd);
				}
			}

			//Debug.Log("FakeServerLobbyReceiveComponent::HandleReceiveData Finished processing connection[" + index + "]");
		}
	}
}

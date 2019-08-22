using System.Collections.Generic;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;



using UnityEngine.Assertions;

public class ServerGameReceiveComponent : MonoBehaviour
{
	private enum GAME_SERVER_PROCESS
	{
		START_GAME,
		CHANGE_PLAYER_TYPE
	}

	private ServerConnectionsComponent connectionsComponent;
	private ServerGameSend serverGameSend;
	private ServerGameDataComponent serverGameDataComponent;

	public void Init(ServerConnectionsComponent connHolder)
	{
		//Debug.Log("ServerGameReceiveComponent::Init Called");
		connectionsComponent = connHolder;
		serverGameSend = GetComponent<ServerGameSend>();
		serverGameDataComponent = GetComponent<ServerGameDataComponent>();
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
		//Debug.Log("ServerGameReceiveComponent::HandleConnections Called");

		// Clean up connections
		for (int i = 0; i < connections.Length; i++)
		{
			if (!connections[i].IsCreated)
			{
				Debug.Log("ServerGameReceiveComponent::HandleConnections Removing a connection");

				connections.RemoveAtSwapBack(i);
				serverGameDataComponent.RemovePlayer(i);
				--i;
			}
		}

		// Don't accept new connections
		NetworkConnection c;
		while ((c = driver.Accept()) != default)
		{
			Debug.Log("ServerGameReceiveComponent::HandleConnections Too many connections, rejecting latest one");
			driver.Disconnect(c);
		}
	}

	private void HandleReceiveData(ref NativeList<NetworkConnection> connections, ref UdpNetworkDriver driver)
	{
		for (int index = 0; index < connections.Length; ++index)
		{
			if (!connections.IsCreated)
			{
				Debug.Log("ServerGameReceiveComponent::HandleReceiveData connections[" + index + "] was not created");
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

					serverGameDataComponent.ProcessClientBytes(index, bytes);
				}
				else if (cmd == NetworkEvent.Type.Disconnect)
				{
					Debug.Log("ServerGameReceiveComponent::HandleReceiveData Client disconnected from server");
					connections[index] = default;
				}
				else
				{
					Debug.Log("ServerGameReceiveComponent::HandleReceiveData Unhandled Network Event: " + cmd);
				}
			}

			//Debug.Log("ServerGameReceiveComponent::HandleReceiveData Finished processing connection[" + index + "]");
		}
	}
}

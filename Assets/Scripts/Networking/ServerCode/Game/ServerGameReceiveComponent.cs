using System.Collections.Generic;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using UnityEngine.Assertions;
using GameUtils;
using CommonNetworkingUtils;

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
	
	private Dictionary<GAME_CLIENT_REQUESTS, ServerHandleIncomingBytes> CommandToFunctionDictionary;

	private void Start()
	{
		CommandToFunctionDictionary = new Dictionary<GAME_CLIENT_REQUESTS, ServerHandleIncomingBytes>();
		CommandToFunctionDictionary.Add(GAME_CLIENT_REQUESTS.CREATE_ENTITY_WITH_OWNERSHIP, serverGameDataComponent.HandleCreateEntityWithOwnership);
		CommandToFunctionDictionary.Add(GAME_CLIENT_REQUESTS.SET_ALL_OBJECT_STATES, serverGameDataComponent.HandleSetAllObjectStatesCommand);
		CommandToFunctionDictionary.Add(GAME_CLIENT_REQUESTS.HEARTBEAT, serverGameDataComponent.HeartBeat);
	}


	public void Init(ServerConnectionsComponent connHolder)
	{
		//Debug.Log("ServerGameReceiveComponent::Init Called");
		connectionsComponent = connHolder;
		serverGameSend = GetComponent<ServerGameSend>();
		serverGameDataComponent = GetComponent<ServerGameDataComponent>();
	}

	void Update()
	{
		ref UdpCNetworkDriver driver = ref connectionsComponent.GetDriver();
		ref NativeList<NetworkConnection> connections = ref connectionsComponent.GetConnections();

		driver.ScheduleUpdate().Complete();

		// ***** Handle Connections *****
		HandleConnections(ref connections, ref driver);

		// ***** Receive data *****
		HandleReceiveData(ref connections, ref driver);
	}

	private void HandleConnections(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver)
	{
		//Debug.Log("ServerGameReceiveComponent::HandleConnections Called");

		// Clean up connections
		bool connectionsChanged = false;
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

	private void HandleReceiveData(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver)
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

					ReadClientBytes(index, bytes);
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

	private void ReadClientBytes(int playerIndex, byte[] bytes)
	{
		Debug.Log("ServerGameReceiveComponent::ReadClientBytes bytes.Length = " + bytes.Length);

		for (int i = 0; i < bytes.Length;)
		{
			GAME_CLIENT_REQUESTS clientCmd = (GAME_CLIENT_REQUESTS)bytes[i];

			// Unsafely assuming that everything is working as expected and there are no attackers.
			++i;

			Debug.Log("ServerGameReceiveComponent::ReadClientBytes Got " + clientCmd + " from the Client");

			i += CommandToFunctionDictionary[clientCmd](i, bytes, playerIndex);
		}
	}
}

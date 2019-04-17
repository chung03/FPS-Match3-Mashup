using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using UnityEngine.Assertions;
using Util;

public class ServerLobbySend : MonoBehaviour
{
	// Byte to send and the player ID
	public List<Queue<byte>> individualSendQueues;
	private Queue<byte> allSendQueue;
	
	// Used for calculating deltas and ultimately save on network bandwidth
	public List<LobbyPlayerInfo> m_PreviousStatePlayerList;

	[SerializeField]
	private float sendFrequencyMs = 50;
	private float timeSinceLastSend = 0;

    // Start is called before the first frame update
    private void Start()
    {
		// Initialize send queues for all possible players
		individualSendQueues = new List<Queue<byte>>(ServerLobbyComponent.MAX_NUM_PLAYERS);
		for (int i = 0; i < ServerLobbyComponent.MAX_NUM_PLAYERS; ++i)
		{
			individualSendQueues.Add(new Queue<byte>());
		}

		allSendQueue = new Queue<byte>();

		m_PreviousStatePlayerList = new List<LobbyPlayerInfo>(ServerLobbyComponent.MAX_NUM_PLAYERS);
	}

    // Is kind of like an update, but is called by other code
	// This is writen so that the ServerLobbyComponent can ensure that all data is ready before trying to send
    public void SendDataIfReady(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver, List<LobbyPlayerInfo> playerList)
    {
		// Not time to send yet.
		if (timeSinceLastSend * 1000 + sendFrequencyMs > Time.time * 1000)
		{
			return;
		}

		timeSinceLastSend = Time.time;
		
		// Send player state to all players
		// Calculate and send Delta
		for (int index = 0; index < connections.Length; ++index)
		{
			if (!connections.IsCreated)
			{
				Debug.Log("ServerLobbyComponent::HandleReceiveData connections[" + index + "] was not created");
				Assert.IsTrue(true);
			}

			// Send state of all players
			using (var writer = new DataStreamWriter(ServerLobbyComponent.MAX_NUM_PLAYERS * 5 * sizeof(byte), Allocator.Temp))
			{
				writer.Write((byte)LOBBY_SERVER_COMMANDS.SET_ALL_PLAYER_STATES);
				writer.Write((byte)playerList.Count);

				// Send data for present players
				for (int playerNum = 0; playerNum < playerList.Count; playerNum++)
				{
					// Write player info
					writer.Write(playerList[playerNum].isReady);
					writer.Write(playerList[playerNum].team);
					writer.Write((byte)playerList[playerNum].playerType);
					writer.Write(playerList[playerNum].playerID);
				}

				connections[index].Send(driver, writer);
			}
		}
		
		// Save previous state so that we can create deltas later
		m_PreviousStatePlayerList = DeepClone(playerList);

		// Send messages meant for individual players
		for (int index = 0; index < connections.Length; ++index)
		{
			Queue<byte> playerQueue = individualSendQueues[index];

			if (playerQueue.Count > 0)
			{
				// Send eveyrthing in the queue
				using (var writer = new DataStreamWriter(playerQueue.Count, Allocator.Temp))
				{
					while (playerQueue.Count > 0)
					{
						byte data = playerQueue.Dequeue();

						if (!connections[index].IsCreated)
						{
							Debug.Log("ServerLobbyComponent::HandleReceiveData connections[" + index + "] was not created");
							Assert.IsTrue(true);
						}

						writer.Write(data);
					}

					connections[index].Send(driver, writer);
				}
			}
		}


		// Send things in the queue meant for all players
		if (allSendQueue.Count > 0)
		{
			// Send eveyrthing in the queue
			using (var writer = new DataStreamWriter(allSendQueue.Count, Allocator.Temp))
			{
				while (allSendQueue.Count > 0)
				{
					byte byteToSend = allSendQueue.Dequeue();

					for (int index = 0; index < connections.Length; ++index)
					{
						if (!connections.IsCreated)
						{
							Debug.Log("ServerLobbyComponent::HandleReceiveData connections[" + index + "] was not created");
							Assert.IsTrue(true);
						}

						writer.Write(byteToSend);
						connections[index].Send(driver, writer);
					}
				}
			}
		}
	}

	public void SendIndividualPlayerDataWhenReady(int connectionIndex, byte data)
	{
		individualSendQueues[connectionIndex].Enqueue(data);
	}

	public void ResetIndividualPlayerQueue(int index)
	{
		individualSendQueues[index] = new Queue<byte>();
	}

	public void SendDataToAllPlayersWhenReady(byte data)
	{
		allSendQueue.Enqueue(data);
	}

	private List<LobbyPlayerInfo> DeepClone(List<LobbyPlayerInfo> list)
	{
		List<LobbyPlayerInfo> ret = new List<LobbyPlayerInfo>();

		foreach (LobbyPlayerInfo player in list)
		{
			ret.Add(player.Clone());
		}

		return ret;
	}
}

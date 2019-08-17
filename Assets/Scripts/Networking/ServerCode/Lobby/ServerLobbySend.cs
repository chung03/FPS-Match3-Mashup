using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using UnityEngine.Assertions;
using LobbyUtils;

using System.Text;

public class ServerLobbySend : MonoBehaviour
{
	// Byte to send and the player ID
	public List<Queue<byte>> individualSendQueues;
	private Queue<byte> allSendQueue;

	private ServerLobbyDataComponent serverLobbyData;

	[SerializeField]
	private float sendFrequencyMs = 50;
	private float timeSinceLastSend = 0;

    // Start is called before the first frame update
    private void Start()
    {
		// Initialize send queues for all possible players
		individualSendQueues = new List<Queue<byte>>(CONSTANTS.MAX_NUM_PLAYERS);
		for (int i = 0; i < CONSTANTS.MAX_NUM_PLAYERS; ++i)
		{
			individualSendQueues.Add(new Queue<byte>());
		}

		allSendQueue = new Queue<byte>();

		serverLobbyData = GetComponent<ServerLobbyDataComponent>();
	}

    // Is kind of like an update, but is called by other code
	// This is writen so that the ServerLobbyComponent can ensure that all data is ready before trying to send
    public void SendDataIfReady(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver, List<PersistentPlayerInfo> playerList)
    {
		// Not time to send yet.
		if (timeSinceLastSend * 1000 + sendFrequencyMs > Time.time * 1000)
		{
			return;
		}

		timeSinceLastSend = Time.time;

		serverLobbyData.SendPlayerDiffs();

		HandleIndividualPlayerSend(ref connections, ref driver);

		HandleAllPlayerSend(ref connections, ref driver);
	}

	private void HandleIndividualPlayerSend(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver)
	{
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
							Debug.Log("ServerLobbySend::HandleIndividualPlayerSend connections[" + index + "] was not created");
							Assert.IsTrue(true);
						}

						writer.Write(data);
					}

					connections[index].Send(driver, writer);
				}
			}
		}
	}

	private void HandleAllPlayerSend(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver)
	{
		// If the queue is empty, then nothing to do
		if (allSendQueue.Count <= 0)
		{
			return;
		}

		byte[] byteArray = allSendQueue.ToArray();
		
		for (int connectionIndex = 0; connectionIndex < connections.Length; ++connectionIndex)
		{
			if (!connections.IsCreated)
			{
				Debug.Log("ServerLobbySend::HandleAllPlayerSend connection[" + connectionIndex + "] was not created");
				Assert.IsTrue(true);
			}
			// Send eveyrthing in the queue
			using (var writer = new DataStreamWriter(byteArray.Length, Allocator.Temp))
			{
				for (int byteIndex = 0; byteIndex < byteArray.Length; ++byteIndex)
				{
					writer.Write(byteArray[byteIndex]);
				}

				connections[connectionIndex].Send(driver, writer);
			}	
		}

		allSendQueue.Clear();
	}

	public void ResetIndividualPlayerQueue(int index)
	{
		individualSendQueues[index] = new Queue<byte>();
	}

	public void SendDataToPlayerWhenReady(byte data, int connectionIndex)
	{
		if (connectionIndex == CONSTANTS.SEND_ALL_PLAYERS)
		{
			allSendQueue.Enqueue(data);
		}
		else
		{
			individualSendQueues[connectionIndex].Enqueue(data);
		}
	}

	public void SendDataToPlayerWhenReady(List<byte> data, int connectionIndex)
	{
		foreach (byte _byte in data)
		{
			SendDataToPlayerWhenReady(_byte, connectionIndex);
		}
	}
}

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
				Debug.Log("ServerLobbySend::SendDataIfReady connections[" + index + "] was not created");
				Assert.IsTrue(true);
			}

			// Send state of all players
			using (var writer = new DataStreamWriter(ServerLobbyComponent.MAX_NUM_PLAYERS * 5 * sizeof(byte), Allocator.Temp))
			{
				writer.Write((byte)LOBBY_SERVER_COMMANDS.SET_ALL_PLAYER_STATES);
				writer.Write((byte)playerList.Count);

				// Figure out diffs for each player that was here before
				byte[] playerDiffFlags = new byte[Mathf.Min(playerList.Count, m_PreviousStatePlayerList.Count)];
				for (int playerNum = 0; playerNum < Mathf.Min(playerList.Count, m_PreviousStatePlayerList.Count); playerNum++)
				{
					playerDiffFlags[playerNum] = 0;
					if (playerList[playerNum].playerID != m_PreviousStatePlayerList[playerNum].playerID)
					{
						playerDiffFlags[playerNum] |= CONSTANTS.PLAYER_ID_MASK;
					}

					if (playerList[playerNum].playerType != m_PreviousStatePlayerList[playerNum].playerType)
					{
						playerDiffFlags[playerNum] |= CONSTANTS.PLAYER_TYPE_MASK;
					}

					if (playerList[playerNum].isReady != m_PreviousStatePlayerList[playerNum].isReady)
					{
						playerDiffFlags[playerNum] |= CONSTANTS.READY_MASK;
					}

					if (playerList[playerNum].team != m_PreviousStatePlayerList[playerNum].team)
					{
						playerDiffFlags[playerNum] |= CONSTANTS.TEAM_MASK;
					}
				}

				// Send data for players that were here before
				for (int playerNum = 0; playerNum < Mathf.Min(playerList.Count, m_PreviousStatePlayerList.Count); playerNum++)
				{
					// Tell Client what changed
					writer.Write(playerDiffFlags[playerNum]);

					// Send necessary data. Go from most significant bit to least
					if ((playerDiffFlags[playerNum] & CONSTANTS.PLAYER_ID_MASK) > 0)
					{
						writer.Write(playerList[playerNum].playerID);
					}

					if ((playerDiffFlags[playerNum] & CONSTANTS.PLAYER_TYPE_MASK) > 0)
					{
						writer.Write((byte)playerList[playerNum].playerType);
					}

					if ((playerDiffFlags[playerNum] & CONSTANTS.READY_MASK) > 0)
					{
						writer.Write(playerList[playerNum].isReady);
					}

					if ((playerDiffFlags[playerNum] & CONSTANTS.TEAM_MASK) > 0)
					{
						writer.Write(playerList[playerNum].team);
					}
				}

				// Send full data for new players
				for (int playerNum = m_PreviousStatePlayerList.Count; playerNum < playerList.Count; playerNum++)
				{
					// Write player info
					writer.Write(CONSTANTS.PLAYER_ID_MASK | CONSTANTS.PLAYER_TYPE_MASK | CONSTANTS.READY_MASK | CONSTANTS.TEAM_MASK);
					writer.Write(playerList[playerNum].playerID);
					writer.Write((byte)playerList[playerNum].playerType);
					writer.Write(playerList[playerNum].isReady);
					writer.Write(playerList[playerNum].team);
				}

				connections[index].Send(driver, writer);
			}
		}
		
		// Save previous state so that we can create deltas later
		m_PreviousStatePlayerList = DeepClone(playerList);

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
							Debug.Log("ServerLobbySend::HandleAllPlayerSend connections[" + index + "] was not created");
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

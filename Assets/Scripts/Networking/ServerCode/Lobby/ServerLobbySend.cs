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
	
	// Used for calculating deltas and ultimately save on network bandwidth
	public List<LobbyPlayerInfo> m_PreviousStatePlayerList;

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

		m_PreviousStatePlayerList = new List<LobbyPlayerInfo>(CONSTANTS.MAX_NUM_PLAYERS);
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

		HandlePlayerDiff(ref connections, ref driver, playerList);

		HandleIndividualPlayerSend(ref connections, ref driver);

		HandleAllPlayerSend(ref connections, ref driver);
	}

	private void HandlePlayerDiff(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver, List<LobbyPlayerInfo> playerList)
	{
		// No players, so nothing to do
		if (playerList.Count <= 0)
		{
			return;
		}

		// Send player state to all players
		// Calculate and send Delta

		// Figure out diffs for each player that was here before
		byte[] playerDiffFlags = new byte[Mathf.Min(playerList.Count, m_PreviousStatePlayerList.Count)];
		for (int playerNum = 0; playerNum < Mathf.Min(playerList.Count, m_PreviousStatePlayerList.Count); playerNum++)
		{
			playerDiffFlags[playerNum] = 0;

			if (playerList[playerNum].name.CompareTo(m_PreviousStatePlayerList[playerNum].name) != 0)
			{
				playerDiffFlags[playerNum] |= CONSTANTS.NAME_MASK;
			}
			
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

		SendDataToPlayerWhenReady((byte)LOBBY_SERVER_COMMANDS.SET_ALL_PLAYER_STATES, CONSTANTS.SEND_ALL_PLAYERS);
		SendDataToPlayerWhenReady((byte)playerList.Count, CONSTANTS.SEND_ALL_PLAYERS);

		// Send data for players that were here before
		for (int playerNum = 0; playerNum < Mathf.Min(playerList.Count, m_PreviousStatePlayerList.Count); playerNum++)
		{
			// Tell Client what changed
			SendDataToPlayerWhenReady(playerDiffFlags[playerNum], CONSTANTS.SEND_ALL_PLAYERS);

			// Send necessary data. Go from most significant bit to least
			if ((playerDiffFlags[playerNum] & CONSTANTS.NAME_MASK) > 0)
			{
				byte[] nameAsBytes = Encoding.UTF8.GetBytes(playerList[playerNum].name);

				// Send length of name, and then send name
				SendDataToPlayerWhenReady((byte)nameAsBytes.Length, CONSTANTS.SEND_ALL_PLAYERS);

				for (int byteIndex = 0; byteIndex < nameAsBytes.Length; ++byteIndex)
				{
					SendDataToPlayerWhenReady(nameAsBytes[byteIndex], CONSTANTS.SEND_ALL_PLAYERS);
				}
			}

			if ((playerDiffFlags[playerNum] & CONSTANTS.PLAYER_ID_MASK) > 0)
			{
				SendDataToPlayerWhenReady(playerList[playerNum].playerID, CONSTANTS.SEND_ALL_PLAYERS);
			}

			if ((playerDiffFlags[playerNum] & CONSTANTS.PLAYER_TYPE_MASK) > 0)
			{
				SendDataToPlayerWhenReady((byte)playerList[playerNum].playerType, CONSTANTS.SEND_ALL_PLAYERS);
			}

			if ((playerDiffFlags[playerNum] & CONSTANTS.READY_MASK) > 0)
			{
				SendDataToPlayerWhenReady(playerList[playerNum].isReady, CONSTANTS.SEND_ALL_PLAYERS);
			}

			if ((playerDiffFlags[playerNum] & CONSTANTS.TEAM_MASK) > 0)
			{
				SendDataToPlayerWhenReady(playerList[playerNum].team, CONSTANTS.SEND_ALL_PLAYERS);
			}
		}

		// Send full data for new players
		SendFullPlayerState(playerList, CONSTANTS.SEND_ALL_PLAYERS, m_PreviousStatePlayerList.Count, playerList.Count);

		// Save previous state so that we can create deltas later
		m_PreviousStatePlayerList = DeepClone(playerList);
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

	// Send current player states to a new connection.
	// This will bring the connection up to date and able to use the deltas in the next update
	public void SendCurrentPlayerStateDataToNewPlayerWhenReady(int connectionIndex)
	{
		// Nothing to send if no players
		if (m_PreviousStatePlayerList.Count <= 0)
		{
			return;
		}

		SendDataToPlayerWhenReady((byte)LOBBY_SERVER_COMMANDS.SET_ALL_PLAYER_STATES, connectionIndex);
		SendDataToPlayerWhenReady((byte)m_PreviousStatePlayerList.Count, connectionIndex);
		SendFullPlayerState(m_PreviousStatePlayerList, connectionIndex, 0, m_PreviousStatePlayerList.Count);
	}

	private void SendFullPlayerState(List<LobbyPlayerInfo> playerList, int connectionIndex, int beginningIndex, int endIndex)
	{
		// Send full data for new players
		for (int playerNum = beginningIndex; playerNum < endIndex; playerNum++)
		{
			// Write player info
			SendDataToPlayerWhenReady(CONSTANTS.NAME_MASK | CONSTANTS.PLAYER_ID_MASK | CONSTANTS.PLAYER_TYPE_MASK | CONSTANTS.READY_MASK | CONSTANTS.TEAM_MASK, connectionIndex);

			byte[] nameAsBytes = Encoding.UTF8.GetBytes(playerList[playerNum].name);

			// Send length of name, and then send name
			SendDataToPlayerWhenReady((byte)nameAsBytes.Length, connectionIndex);

			for (int byteIndex = 0; byteIndex < nameAsBytes.Length; ++byteIndex)
			{
				SendDataToPlayerWhenReady(nameAsBytes[byteIndex], connectionIndex);
			}

			SendDataToPlayerWhenReady(playerList[playerNum].playerID, connectionIndex);
			SendDataToPlayerWhenReady((byte)playerList[playerNum].playerType, connectionIndex);
			SendDataToPlayerWhenReady(playerList[playerNum].isReady, connectionIndex);
			SendDataToPlayerWhenReady(playerList[playerNum].team, connectionIndex);
		}
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using UnityEngine.Assertions;
using GameUtils;

using System.Text;

public class ServerGameSend : MonoBehaviour
{
	// Byte to send and the player ID
	public List<Queue<byte>> individualSendQueues;
	private Queue<byte> allSendQueue;
	
	// Used for calculating deltas and ultimately save on network bandwidth
	public List<PersistentPlayerInfo> m_PreviousStatePlayerList;

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

		m_PreviousStatePlayerList = new List<PersistentPlayerInfo>(CONSTANTS.MAX_NUM_PLAYERS);
	}

    // Is kind of like an update, but is called by other code
	// This is writen so that the ServerGameComponent can ensure that all data is ready before trying to send
    public void SendDataIfReady(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver, Dictionary<int, ObjectWithDelta> IdToObjectsDictionary)
    {
		// Not time to send yet.
		if (timeSinceLastSend * 1000 + sendFrequencyMs > Time.time * 1000)
		{
			return;
		}

		timeSinceLastSend = Time.time;

		HandleObjectDiff(ref connections, ref driver, IdToObjectsDictionary);

		HandleIndividualPlayerSend(ref connections, ref driver);

		HandleAllPlayerSend(ref connections, ref driver);
	}

	private void HandleObjectDiff(ref NativeList<NetworkConnection> connections, ref UdpCNetworkDriver driver, Dictionary<int, ObjectWithDelta> IdToObjectsDictionary)
	{
		// No Objects, so nothing to do
		if (IdToObjectsDictionary.Count <= 0)
		{
			return;
		}

		SendDataToPlayerWhenReady((byte)GAME_SERVER_COMMANDS.SET_ALL_OBJECT_STATES, CONSTANTS.SEND_ALL_PLAYERS);
		SendDataToPlayerWhenReady((byte)IdToObjectsDictionary.Count, CONSTANTS.SEND_ALL_PLAYERS);

		foreach (ObjectWithDelta data in IdToObjectsDictionary.Values)
		{
			SendDataToPlayerWhenReady((byte)data.GetObjectId(), CONSTANTS.SEND_ALL_PLAYERS);

			List<byte> delta = data.GetDeltaBytes(false);

			foreach(byte dataByte in delta)
			{
				SendDataToPlayerWhenReady(dataByte, CONSTANTS.SEND_ALL_PLAYERS);
			}
		}
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
							Debug.Log("ServerGameSend::HandleIndividualPlayerSend connections[" + index + "] was not created");
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
				Debug.Log("ServerGameSend::HandleAllPlayerSend connection[" + connectionIndex + "] was not created");
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

		SendDataToPlayerWhenReady((byte)GAME_SERVER_COMMANDS.SET_ALL_OBJECT_STATES, connectionIndex);
		SendDataToPlayerWhenReady((byte)m_PreviousStatePlayerList.Count, connectionIndex);
		SendFullPlayerState(m_PreviousStatePlayerList, connectionIndex, 0, m_PreviousStatePlayerList.Count);
	}

	private void SendFullPlayerState(List<PersistentPlayerInfo> playerList, int connectionIndex, int beginningIndex, int endIndex)
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

	private List<PersistentPlayerInfo> DeepClone(List<PersistentPlayerInfo> list)
	{
		List<PersistentPlayerInfo> ret = new List<PersistentPlayerInfo>();

		foreach (PersistentPlayerInfo player in list)
		{
			ret.Add(player.Clone());
		}

		return ret;
	}
}

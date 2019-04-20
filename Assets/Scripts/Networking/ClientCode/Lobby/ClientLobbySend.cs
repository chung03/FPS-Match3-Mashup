using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using UnityEngine.Assertions;
using Util;

public class ClientLobbySend : MonoBehaviour
{
	private Queue<byte> sendQueue;

	[SerializeField]
	private float sendFrequencyMs = 50;
	private float timeSinceLastSend = 0;

	[SerializeField]
	private float heartbeatFrequencyMs = 2000;
	private float timeSinceLastHeartBeat = 0;

	// Start is called before the first frame update
	private void Start()
	{
		sendQueue = new Queue<byte>();
	}

	public void SendDataIfReady(ref NetworkConnection connection, ref UdpCNetworkDriver driver, List<LobbyPlayerInfo> allPlayerInfo)
	{
		// Heart beat every once in a while to prevent disconnects for no reason
		if (timeSinceLastHeartBeat * 1000 + heartbeatFrequencyMs <= Time.time * 1000)
		{
			timeSinceLastHeartBeat = Time.time;
			sendQueue.Enqueue((byte)LOBBY_CLIENT_REQUESTS.HEARTBEAT);
		}

		// Not time to send yet.
		if (timeSinceLastSend * 1000 + sendFrequencyMs > Time.time * 1000)
		{
			return;
		}

		timeSinceLastSend = Time.time;


		if (sendQueue.Count <= 0)
		{
			return;
		}

		// Send eveyrthing in the queue
		using (var writer = new DataStreamWriter(sendQueue.Count, Allocator.Temp))
		{
			while (sendQueue.Count > 0)
			{
				writer.Write(sendQueue.Dequeue());
			}

			connection.Send(driver, writer);
		}
	}

	public void SendDataWhenReady(byte data)
	{
		sendQueue.Enqueue(data);
	}
}
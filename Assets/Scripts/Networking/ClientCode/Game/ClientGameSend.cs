﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;



using UnityEngine.Assertions;
using GameUtils;

public class ClientGameSend : MonoBehaviour
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
		CreateQueueIfNecessary();
	}

	private void CreateQueueIfNecessary()
	{
		if (sendQueue == null)
		{
			sendQueue = new Queue<byte>();
		}
	}

	public void SendDataIfReady(ref NetworkConnection connection, ref UdpNetworkDriver driver, Dictionary<int, ObjectWithDelta> IdToClientControlledObjectDictionary)
	{
		CreateQueueIfNecessary();

		// Heart beat every once in a while to prevent disconnects
		if (timeSinceLastHeartBeat * 1000 + heartbeatFrequencyMs <= Time.time * 1000)
		{
			timeSinceLastHeartBeat = Time.time;
			sendQueue.Enqueue((byte)GAME_CLIENT_REQUESTS.HEARTBEAT);
		}

		// Not time to send yet.
		if (timeSinceLastSend * 1000 + sendFrequencyMs > Time.time * 1000)
		{
			return;
		}

		timeSinceLastSend = Time.time;

		HandleObjectDiff(IdToClientControlledObjectDictionary);


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

	private void HandleObjectDiff(Dictionary<int, ObjectWithDelta> IdToObjectsDictionary)
	{
		// No Objects, so nothing to do
		if (IdToObjectsDictionary.Count <= 0)
		{
			return;
		}

		SendDataWhenReady((byte)GAME_CLIENT_REQUESTS.SET_ALL_OBJECT_STATES);

		// Figure out how many objects have changed and only send deltas for those objects
		byte numObjectsToSend = 0;
		foreach (ObjectWithDelta data in IdToObjectsDictionary.Values)
		{
			// Only send data if something changed
			if (data.HasChanged())
			{
				++numObjectsToSend;
			}
		}

		SendDataWhenReady(numObjectsToSend);

		foreach (ObjectWithDelta data in IdToObjectsDictionary.Values)
		{
			// Only send data if something changed
			if (data.HasChanged())
			{
				SendDataWhenReady((byte)data.GetObjectId());

				List<byte> request = data.GetDeltaBytes(false);
				data.SetDeltaToZero();

				foreach (byte dataByte in request)
				{
					SendDataWhenReady(dataByte);
				}
			}
		}
	}

	public void SendDataWhenReady(byte data)
	{
		CreateQueueIfNecessary();
		sendQueue.Enqueue(data);
	}
}
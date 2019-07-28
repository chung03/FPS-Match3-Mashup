using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// This class stores the data for all FPS Player Components and handles deltas (create and read)
public class FPSPlayerData: ObjectWithDelta
{
	bool isDirty = false;
	int objectId = 0;
	Vector3 desiredPlayerPosn;
	Quaternion desiredPlayerRotation;

	Vector3 currentPlayerPosn;
	Quaternion currentPlayerRotation;

	public FPSPlayerData()
	{
		currentPlayerPosn = Vector3.zero;
		desiredPlayerPosn = Vector3.zero;

		desiredPlayerRotation = Quaternion.identity;
		currentPlayerRotation = Quaternion.identity;
	}

	public void SetPlayerPosn(Vector3 playerPosn)
	{
		desiredPlayerPosn = playerPosn;

		// Debug.LogWarning("SetPlayerPosn called. New currentPlayerPosn = " + currentPlayerPosn + ", previousPlayerPosn = " + previousPlayerPosn + ", currentPlayerPosn.x != previousPlayerPosn.x = " + (currentPlayerPosn.x != previousPlayerPosn.x));

		isDirty = true;
	}

	public Vector3 GetPlayerPosn()
	{
		//return currentPlayerPosn;
		return currentPlayerPosn;
	}

	public void SetPlayerRotation(Quaternion playerRotation)
	{
		desiredPlayerRotation = playerRotation;
		isDirty = true;
	}

	private byte CalculateDiffMask(bool getFullState)
	{
		byte playerDiffFlags = 0;

		if (getFullState)
		{
			playerDiffFlags = (byte)(FPS_PLAYER_DATA_CONSTANTS.POSN_X_MASK
									| FPS_PLAYER_DATA_CONSTANTS.POSN_Y_MASK
									| FPS_PLAYER_DATA_CONSTANTS.POSN_Z_MASK
									| FPS_PLAYER_DATA_CONSTANTS.ROTATION_W_MASK
									| FPS_PLAYER_DATA_CONSTANTS.ROTATION_X_MASK
									| FPS_PLAYER_DATA_CONSTANTS.ROTATION_Y_MASK
									| FPS_PLAYER_DATA_CONSTANTS.ROTATION_Z_MASK);
		}
		else
		{
			// Debug.LogWarning("CalculateDiffMask called. currentPlayerPosn = " + currentPlayerPosn + ", previousPlayerPosn = " + previousPlayerPosn);


			if (desiredPlayerPosn.x != currentPlayerPosn.x)
			{
				playerDiffFlags |= FPS_PLAYER_DATA_CONSTANTS.POSN_X_MASK;
			}

			if (desiredPlayerPosn.y != currentPlayerPosn.y)
			{
				playerDiffFlags |= FPS_PLAYER_DATA_CONSTANTS.POSN_Y_MASK;
			}

			if (desiredPlayerPosn.z != currentPlayerPosn.z)
			{
				playerDiffFlags |= FPS_PLAYER_DATA_CONSTANTS.POSN_Z_MASK;
			}

			if (desiredPlayerRotation.w != currentPlayerRotation.w)
			{
				playerDiffFlags |= FPS_PLAYER_DATA_CONSTANTS.ROTATION_W_MASK;
			}

			if (desiredPlayerRotation.x != currentPlayerRotation.x)
			{
				playerDiffFlags |= FPS_PLAYER_DATA_CONSTANTS.ROTATION_X_MASK;
			}

			if (desiredPlayerRotation.y != currentPlayerRotation.y)
			{
				playerDiffFlags |= FPS_PLAYER_DATA_CONSTANTS.ROTATION_Y_MASK;
			}

			if (desiredPlayerRotation.z != currentPlayerRotation.z)
			{
				playerDiffFlags |= FPS_PLAYER_DATA_CONSTANTS.ROTATION_Z_MASK;
			}
		}

		return playerDiffFlags;
	}

	public List<byte> ServerGetDeltaBytes(bool getFullState)
	{
		List<byte> deltaBytes = new List<byte>();

		byte playerDiffFlags = CalculateDiffMask(getFullState);

		if (playerDiffFlags != 0)
		{
			Debug.LogWarning("GetDeltaBytes Generated a non-zero delta. playerDiffFlags = " + playerDiffFlags);
		}

		// Prepare to send data now
		deltaBytes.Add(playerDiffFlags);

		if ((playerDiffFlags & FPS_PLAYER_DATA_CONSTANTS.POSN_X_MASK) > 0)
		{
			deltaBytes.AddRange(BitConverter.GetBytes(desiredPlayerPosn.x));
		}

		if ((playerDiffFlags & FPS_PLAYER_DATA_CONSTANTS.POSN_Y_MASK) > 0)
		{
			deltaBytes.AddRange(BitConverter.GetBytes(desiredPlayerPosn.y));
		}

		if ((playerDiffFlags & FPS_PLAYER_DATA_CONSTANTS.POSN_Z_MASK) > 0)
		{
			deltaBytes.AddRange(BitConverter.GetBytes(desiredPlayerPosn.z));
		}

		if ((playerDiffFlags & FPS_PLAYER_DATA_CONSTANTS.ROTATION_W_MASK) > 0)
		{
			deltaBytes.AddRange(BitConverter.GetBytes(desiredPlayerRotation.w));
		}

		if ((playerDiffFlags & FPS_PLAYER_DATA_CONSTANTS.ROTATION_X_MASK) > 0)
		{
			deltaBytes.AddRange(BitConverter.GetBytes(desiredPlayerRotation.x));
		}

		if ((playerDiffFlags & FPS_PLAYER_DATA_CONSTANTS.ROTATION_Y_MASK) > 0)
		{
			deltaBytes.AddRange(BitConverter.GetBytes(desiredPlayerRotation.y));
		}

		if ((playerDiffFlags & FPS_PLAYER_DATA_CONSTANTS.ROTATION_Z_MASK) > 0)
		{
			deltaBytes.AddRange(BitConverter.GetBytes(desiredPlayerRotation.z));
		}
		
		deltaBytes.Insert(0, (byte)deltaBytes.Count);
		isDirty = false;

		return deltaBytes;
	}

	public void ApplyDelta(byte[] delta, bool isServer)
	{
		byte playerDiffMask = delta[0];
		int index = 1;

		if (playerDiffMask != 0)
		{
			Debug.LogWarning("ClientApplyDelta Got a non-zero delta. playerDiffMask = " + playerDiffMask);
		}
		else
		{
			// Nothing to change, so return
			return;
		}

		if ((playerDiffMask & FPS_PLAYER_DATA_CONSTANTS.POSN_X_MASK) > 0)
		{
			desiredPlayerPosn.x = BitConverter.ToSingle(delta, index);
			index += sizeof(float);
		}

		if ((playerDiffMask & FPS_PLAYER_DATA_CONSTANTS.POSN_Y_MASK) > 0)
		{
			desiredPlayerPosn.y = BitConverter.ToSingle(delta, index);
			index += sizeof(float);
		}

		if ((playerDiffMask & FPS_PLAYER_DATA_CONSTANTS.POSN_Z_MASK) > 0)
		{
			desiredPlayerPosn.z = BitConverter.ToSingle(delta, index);
			index += sizeof(float);
		}

		if ((playerDiffMask & FPS_PLAYER_DATA_CONSTANTS.ROTATION_W_MASK) > 0)
		{
			desiredPlayerRotation.w = BitConverter.ToSingle(delta, index);
			index += sizeof(float);
		}

		if ((playerDiffMask & FPS_PLAYER_DATA_CONSTANTS.ROTATION_X_MASK) > 0)
		{
			desiredPlayerRotation.x = BitConverter.ToSingle(delta, index);
			index += sizeof(float);
		}

		if ((playerDiffMask & FPS_PLAYER_DATA_CONSTANTS.ROTATION_Y_MASK) > 0)
		{
			desiredPlayerRotation.y = BitConverter.ToSingle(delta, index);
			index += sizeof(float);
		}

		if ((playerDiffMask & FPS_PLAYER_DATA_CONSTANTS.ROTATION_Z_MASK) > 0)
		{
			desiredPlayerRotation.z = BitConverter.ToSingle(delta, index);
			index += sizeof(float);
		}

		//currentPlayerPosn = desiredPlayerPosn;
		//currentPlayerRotation = desiredPlayerRotation;

		if (isServer)
		{
			ServerCheckDeltaResults();
		}
		else
		{
			currentPlayerPosn = desiredPlayerPosn;
			currentPlayerRotation = desiredPlayerRotation;
		}

		isDirty = false;
	}

	private void ServerCheckDeltaResults()
	{
		
	}

	public List<byte> ClientGetRequestBytes()
	{
		return null;
	}

	public bool IsDirty()
	{
		return isDirty;
	}

	public int GetObjectId()
	{
		return objectId;
	}

	public void SetObjectId(int newId)
	{
		objectId = newId;
	}

	private class FPS_PLAYER_DATA_CONSTANTS
	{
		public static byte POSN_X_MASK = 1; 
		public static byte POSN_Y_MASK = 2; 
		public static byte POSN_Z_MASK = 4;

		public static byte ROTATION_W_MASK = 8;
		public static byte ROTATION_X_MASK = 16;
		public static byte ROTATION_Y_MASK = 32;
		public static byte ROTATION_Z_MASK = 64;

		public static byte REQUEST_CHANGE_POSN_X_MASK = 1;
		public static byte REQUEST_CHANGE_POSN_Y_MASK = 2;
		public static byte REQUEST_CHANGE_POSN_Z_MASK = 4;

		public static byte REQUEST_CHANGE_ROTATION_W_MASK = 8;
		public static byte REQUEST_CHANGE_ROTATION_X_MASK = 16;
		public static byte REQUEST_CHANGE_ROTATION_Y_MASK = 32;
		public static byte REQUEST_CHANGE_ROTATION_Z_MASK = 64;
	}
}
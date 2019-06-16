using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// This class stores the data for all FPS Player Components and handles deltas (create and read)
public class FPSPlayerData: ObjectWithDelta
{
	bool isDirty = false;
	int objectId = 0;
	Vector3 currentPlayerPosn;
	Quaternion currentPlayerRotation;

	Vector3 previousPlayerPosn;
	Quaternion previousPlayerRotation;

	public FPSPlayerData()
	{
		previousPlayerPosn = Vector3.zero;
		currentPlayerPosn = Vector3.zero;

		currentPlayerRotation = Quaternion.identity;
		previousPlayerRotation = Quaternion.identity;
	}

	public void SetPlayerPosn(Vector3 playerPosn)
	{
		currentPlayerPosn = playerPosn;
		isDirty = true;
	}

	public void SetPlayerRotation(Quaternion playerRotation)
	{
		currentPlayerRotation = playerRotation;
		isDirty = true;
	}

	private byte CalculateDiffMask(bool getFullState)
	{
		if (getFullState)
		{
			return (byte)(FPS_PLAYER_DATA_CONSTANTS.POSN_X_MASK
									| FPS_PLAYER_DATA_CONSTANTS.POSN_Y_MASK
									| FPS_PLAYER_DATA_CONSTANTS.POSN_Z_MASK
									| FPS_PLAYER_DATA_CONSTANTS.ROTATION_W_MASK
									| FPS_PLAYER_DATA_CONSTANTS.ROTATION_X_MASK
									| FPS_PLAYER_DATA_CONSTANTS.ROTATION_Y_MASK
									| FPS_PLAYER_DATA_CONSTANTS.ROTATION_Z_MASK);
		}

		byte playerDiffFlags = 0;

		if (currentPlayerPosn.x != previousPlayerPosn.x)
		{
			playerDiffFlags |= FPS_PLAYER_DATA_CONSTANTS.POSN_X_MASK;
		}

		if (currentPlayerPosn.y != previousPlayerPosn.y)
		{
			playerDiffFlags |= FPS_PLAYER_DATA_CONSTANTS.POSN_Y_MASK;
		}

		if (currentPlayerPosn.z != previousPlayerPosn.z)
		{
			playerDiffFlags |= FPS_PLAYER_DATA_CONSTANTS.POSN_Z_MASK;
		}

		if (currentPlayerRotation.w != previousPlayerRotation.w)
		{
			playerDiffFlags |= FPS_PLAYER_DATA_CONSTANTS.ROTATION_W_MASK;
		}

		if (currentPlayerRotation.x != previousPlayerRotation.x)
		{
			playerDiffFlags |= FPS_PLAYER_DATA_CONSTANTS.ROTATION_X_MASK;
		}

		if (currentPlayerRotation.y != previousPlayerRotation.y)
		{
			playerDiffFlags |= FPS_PLAYER_DATA_CONSTANTS.ROTATION_Y_MASK;
		}

		if (currentPlayerRotation.z != previousPlayerRotation.z)
		{
			playerDiffFlags |= FPS_PLAYER_DATA_CONSTANTS.ROTATION_Z_MASK;
		}

		return playerDiffFlags;
	}

	public List<byte> GetDeltaBytes(bool getFullState)
	{
		List<byte> deltaBytes = new List<byte>();

		byte playerDiffFlags = CalculateDiffMask(getFullState);

		// Prepare to send data now
		deltaBytes.Add(playerDiffFlags);

		if ((playerDiffFlags & FPS_PLAYER_DATA_CONSTANTS.POSN_X_MASK) > 0)
		{
			deltaBytes.AddRange(BitConverter.GetBytes(currentPlayerPosn.x));
		}

		if ((playerDiffFlags & FPS_PLAYER_DATA_CONSTANTS.POSN_Y_MASK) > 0)
		{
			deltaBytes.AddRange(BitConverter.GetBytes(currentPlayerPosn.y));
		}

		if ((playerDiffFlags & FPS_PLAYER_DATA_CONSTANTS.POSN_Z_MASK) > 0)
		{
			deltaBytes.AddRange(BitConverter.GetBytes(currentPlayerPosn.z));
		}

		if ((playerDiffFlags & FPS_PLAYER_DATA_CONSTANTS.ROTATION_W_MASK) > 0)
		{
			deltaBytes.AddRange(BitConverter.GetBytes(currentPlayerRotation.w));
		}

		if ((playerDiffFlags & FPS_PLAYER_DATA_CONSTANTS.ROTATION_X_MASK) > 0)
		{
			deltaBytes.AddRange(BitConverter.GetBytes(currentPlayerRotation.x));
		}

		if ((playerDiffFlags & FPS_PLAYER_DATA_CONSTANTS.ROTATION_Y_MASK) > 0)
		{
			deltaBytes.AddRange(BitConverter.GetBytes(currentPlayerRotation.y));
		}

		if ((playerDiffFlags & FPS_PLAYER_DATA_CONSTANTS.ROTATION_Z_MASK) > 0)
		{
			deltaBytes.AddRange(BitConverter.GetBytes(currentPlayerRotation.z));
		}

		deltaBytes.Insert(0, (byte)deltaBytes.Count);
		isDirty = false;

		return deltaBytes;
	}

	public void ApplyDelta(byte[] delta)
	{
		byte playerDiffMask = delta[0];
		int index = 1;

		if ((playerDiffMask & FPS_PLAYER_DATA_CONSTANTS.POSN_X_MASK) > 0)
		{
			currentPlayerPosn.x = BitConverter.ToSingle(delta, index);
			index += sizeof(float);
		}

		if ((playerDiffMask & FPS_PLAYER_DATA_CONSTANTS.POSN_Y_MASK) > 0)
		{
			currentPlayerPosn.y = BitConverter.ToSingle(delta, index);
			index += sizeof(float);
		}

		if ((playerDiffMask & FPS_PLAYER_DATA_CONSTANTS.POSN_Z_MASK) > 0)
		{
			currentPlayerPosn.z = BitConverter.ToSingle(delta, index);
			index += sizeof(float);
		}

		if ((playerDiffMask & FPS_PLAYER_DATA_CONSTANTS.ROTATION_W_MASK) > 0)
		{
			currentPlayerRotation.w = BitConverter.ToSingle(delta, index);
			index += sizeof(float);
		}

		if ((playerDiffMask & FPS_PLAYER_DATA_CONSTANTS.ROTATION_X_MASK) > 0)
		{
			currentPlayerRotation.x = BitConverter.ToSingle(delta, index);
			index += sizeof(float);
		}

		if ((playerDiffMask & FPS_PLAYER_DATA_CONSTANTS.ROTATION_Y_MASK) > 0)
		{
			currentPlayerRotation.y = BitConverter.ToSingle(delta, index);
			index += sizeof(float);
		}

		if ((playerDiffMask & FPS_PLAYER_DATA_CONSTANTS.ROTATION_Z_MASK) > 0)
		{
			currentPlayerRotation.z = BitConverter.ToSingle(delta, index);
			index += sizeof(float);
		}

		previousPlayerPosn = currentPlayerPosn;
		previousPlayerRotation = currentPlayerRotation;
		isDirty = false;
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
	}
}
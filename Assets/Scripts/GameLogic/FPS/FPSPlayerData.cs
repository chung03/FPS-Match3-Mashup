using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// This class stores the data for all FPS Player Components and handles deltas (create and read)
public class FPSPlayerData: ObjectWithDelta
{
	int objectId = 0;
	Vector3 previousPlayerPosn;
	Quaternion previousPlayerRotation;

	Vector3 currentPlayerPosn;
	Quaternion currentPlayerRotation;

	FPSPlayer fpsPlayer = null;

	public FPSPlayerData()
	{
		currentPlayerPosn = Vector3.zero;
		previousPlayerPosn = Vector3.zero;

		previousPlayerRotation = Quaternion.identity;
		currentPlayerRotation = Quaternion.identity;
	}

	public void SetFpsPlayer(FPSPlayer _fpsPlayer)
	{
		fpsPlayer = _fpsPlayer;
	}

	public void SetPlayerPosn(Vector3 playerPosn)
	{
		currentPlayerPosn = playerPosn;

		// Debug.LogWarning("SetPlayerPosn called. New currentPlayerPosn = " + currentPlayerPosn + ", previousPlayerPosn = " + previousPlayerPosn + ", currentPlayerPosn.x != previousPlayerPosn.x = " + (currentPlayerPosn.x != previousPlayerPosn.x));
	}

	public Vector3 GetPlayerPosn()
	{
		return currentPlayerPosn;
	}

	public void SetPlayerRotation(Quaternion playerRotation)
	{
		currentPlayerRotation = playerRotation;
	}

	public Quaternion GetPlayerRotation()
	{
		return currentPlayerRotation;
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


			if (previousPlayerPosn.x != currentPlayerPosn.x)
			{
				playerDiffFlags |= FPS_PLAYER_DATA_CONSTANTS.POSN_X_MASK;
			}

			if (previousPlayerPosn.y != currentPlayerPosn.y)
			{
				playerDiffFlags |= FPS_PLAYER_DATA_CONSTANTS.POSN_Y_MASK;
			}

			if (previousPlayerPosn.z != currentPlayerPosn.z)
			{
				playerDiffFlags |= FPS_PLAYER_DATA_CONSTANTS.POSN_Z_MASK;
			}

			if (previousPlayerRotation.w != currentPlayerRotation.w)
			{
				playerDiffFlags |= FPS_PLAYER_DATA_CONSTANTS.ROTATION_W_MASK;
			}

			if (previousPlayerRotation.x != currentPlayerRotation.x)
			{
				playerDiffFlags |= FPS_PLAYER_DATA_CONSTANTS.ROTATION_X_MASK;
			}

			if (previousPlayerRotation.y != currentPlayerRotation.y)
			{
				playerDiffFlags |= FPS_PLAYER_DATA_CONSTANTS.ROTATION_Y_MASK;
			}

			if (previousPlayerRotation.z != currentPlayerRotation.z)
			{
				playerDiffFlags |= FPS_PLAYER_DATA_CONSTANTS.ROTATION_Z_MASK;
			}
		}

		return playerDiffFlags;
	}

	public List<byte> GetDeltaBytes(bool getFullState)
	{
		List<byte> deltaBytes = new List<byte>();

		byte playerDiffFlags = CalculateDiffMask(getFullState);

		if (playerDiffFlags != 0)
		{
			//Debug.LogWarning("GetDeltaBytes Generated a non-zero delta. playerDiffFlags = " + playerDiffFlags);
		}

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

		return deltaBytes;
	}

	public void ApplyDelta(byte[] delta, bool isServer)
	{
		byte playerDiffMask = delta[0];
		int index = 1;

		if (playerDiffMask != 0)
		{
			//Debug.LogWarning("ClientApplyDelta Got a non-zero delta. playerDiffMask = " + playerDiffMask);
		}
		else
		{
			// Nothing to change, so return
			return;
		}

		/*
		if (isServer)
		{
			previousPlayerPosn = currentPlayerPosn;
			previousPlayerRotation = currentPlayerRotation;
		}
		*/

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
	}

	private void ServerCheckDeltaResults()
	{
		
	}

	public int GetObjectId()
	{
		return objectId;
	}

	public void SetObjectId(int newId)
	{
		objectId = newId;
	}

	public void SetDeltaToZero()
	{
		previousPlayerPosn = currentPlayerPosn;
		previousPlayerRotation = currentPlayerRotation;
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
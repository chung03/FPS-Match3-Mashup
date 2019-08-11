using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LobbyUtils;
using System.Text;

public class PersistentPlayerInfo : ObjectWithDelta
{
	private PersistentPlayerInfo previousState;

	public byte team;
	public byte isReady;
	public string name;
	public PLAYER_TYPE playerType;
	public byte playerID;

	public PersistentPlayerInfo()
	{
		previousState = null;
	}

	public PersistentPlayerInfo Clone()
	{
		PersistentPlayerInfo other = new PersistentPlayerInfo();
		other.team = team;
		other.isReady = isReady;

		if (name != null)
		{
			other.name = System.String.Copy(name);
		}
		else
		{
			other.name = null;
		}

		other.playerType = playerType;
		other.playerID = playerID;

		return other;
	}

	public void SetDeltaToZero()
	{
		previousState = Clone();
	}

	private byte CalculateDiffMask()
	{
		byte playerDiffFlags = 0;

		if (name.CompareTo(previousState.name) != 0)
		{
			playerDiffFlags |= CONSTANTS.NAME_MASK;
		}

		if (playerID != previousState.playerID)
		{
			playerDiffFlags |= CONSTANTS.PLAYER_ID_MASK;
		}

		if (playerType != previousState.playerType)
		{
			playerDiffFlags |= CONSTANTS.PLAYER_TYPE_MASK;
		}

		if (isReady != previousState.isReady)
		{
			playerDiffFlags |= CONSTANTS.READY_MASK;
		}

		if (team != previousState.team)
		{
			playerDiffFlags |= CONSTANTS.TEAM_MASK;
		}

		return playerDiffFlags;
	}

	public List<byte> GetDeltaBytes(bool getFullState)
	{
		List<byte> deltaBytes = new List<byte>();

		// If no previous state to compare against or specifically requested, then send full state
		if (previousState == null || getFullState)
		{
			deltaBytes.Add(CONSTANTS.NAME_MASK | CONSTANTS.PLAYER_ID_MASK | CONSTANTS.PLAYER_TYPE_MASK | CONSTANTS.READY_MASK | CONSTANTS.TEAM_MASK);
			byte[] nameAsBytes = Encoding.UTF8.GetBytes(name);

			// Send length of name, and then send name
			deltaBytes.Add((byte)nameAsBytes.Length);

			for (int byteIndex = 0; byteIndex < nameAsBytes.Length; ++byteIndex)
			{
				deltaBytes.Add(nameAsBytes[byteIndex]);
			}

			deltaBytes.Add(playerID);
			deltaBytes.Add((byte)playerType);
			deltaBytes.Add(isReady);
			deltaBytes.Add(team);
		}
		else
		{
			// Get Differences Now
			byte playerDiffFlags = CalculateDiffMask();

			// Prepare to send data now
			deltaBytes.Add(playerDiffFlags);

			if ((playerDiffFlags & CONSTANTS.NAME_MASK) > 0)
			{
				byte[] nameAsBytes = Encoding.UTF8.GetBytes(name);

				// Send length of name, and then send name
				deltaBytes.Add((byte)nameAsBytes.Length);

				for (int byteIndex = 0; byteIndex < nameAsBytes.Length; ++byteIndex)
				{
					deltaBytes.Add(nameAsBytes[byteIndex]);
				}
			}

			if ((playerDiffFlags & CONSTANTS.PLAYER_ID_MASK) > 0)
			{
				deltaBytes.Add(playerID);
			}

			if ((playerDiffFlags & CONSTANTS.PLAYER_TYPE_MASK) > 0)
			{
				deltaBytes.Add((byte)playerType);
			}

			if ((playerDiffFlags & CONSTANTS.READY_MASK) > 0)
			{
				deltaBytes.Add(isReady);
			}

			if ((playerDiffFlags & CONSTANTS.TEAM_MASK) > 0)
			{
				deltaBytes.Add(team);
			}
		}

		deltaBytes.Insert(0, (byte)deltaBytes.Count);

		return deltaBytes;
	}

	public void ApplyDelta(byte[] delta)
	{
		byte playerDiffMask = delta[0];
		int index = 1;

		if ((playerDiffMask & CONSTANTS.NAME_MASK) > 0)
		{
			// Convert from bytes to string
			name = DataUtils.ReadString(ref index, delta);
		}

		if ((playerDiffMask & CONSTANTS.PLAYER_ID_MASK) > 0)
		{
			playerID = delta[index];
			++index;
		}

		if ((playerDiffMask & CONSTANTS.PLAYER_TYPE_MASK) > 0)
		{
			playerType = (PLAYER_TYPE)delta[index];
			++index;
		}

		if ((playerDiffMask & CONSTANTS.READY_MASK) > 0)
		{
			isReady = delta[index];
			++index;
		}

		if ((playerDiffMask & CONSTANTS.TEAM_MASK) > 0)
		{
			team = delta[index];
			++index;
		}
	}

	public bool HasChanged()
	{
		return CalculateDiffMask() != 0;
	}

	public int GetObjectId()
	{
		return playerID;
	}

	public void SetObjectId(int newId)
	{
		playerID = (byte)newId;
	}

	public GameUtils.CREATE_ENTITY_TYPES GetEntityType()
	{
		return GameUtils.CREATE_ENTITY_TYPES.NONE;
	}
}
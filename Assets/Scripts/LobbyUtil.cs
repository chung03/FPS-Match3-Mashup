using System.Text;
using System.Collections.Generic;

namespace LobbyUtils
{
	public enum NETWORK_DATA_TYPE
	{
		SEND_PLAYER_NAME
	}

	public class LobbyPlayerInfo : ObjectWithDelta
	{
		private LobbyPlayerInfo previousState;

		public byte team;
		public byte isReady;
		public string name;
		public PLAYER_TYPE playerType;
		public byte playerID;
		private bool isDirty;

		public LobbyPlayerInfo()
		{
			previousState = null;
			isDirty = true;
		}

		public LobbyPlayerInfo Clone()
		{
			LobbyPlayerInfo ret = new LobbyPlayerInfo();
			ret.team = team;
			ret.isReady = isReady;

			if (name != null)
			{
				ret.name = System.String.Copy(name);
			}
			else
			{
				ret.name = null;
			}

			ret.playerType = playerType;
			ret.playerID = playerID;

			return ret;
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

				return deltaBytes;
			}

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

			previousState = Clone();
			isDirty = false;

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

		public bool IsDirty()
		{
			return isDirty;
		}

		public int GetObjectId()
		{
			return playerID;
		}
	}

	public class PersistentPlayerInfo
	{
		public byte team;
		public string name;
		public PLAYER_TYPE playerType;
		public byte playerID;
	}

	public enum LOBBY_SERVER_COMMANDS
	{
		CHANGE_TEAM = 1,
		READY,
		SET_ID,
		SET_ALL_PLAYER_STATES,
		START_GAME,
		HEARTBEAT
	}

	public enum LOBBY_CLIENT_REQUESTS
	{
		CHANGE_TEAM = 10,
		READY,
		GET_ID,
		CHANGE_PLAYER_TYPE,
		CHANGE_NAME,
		START_GAME,
		HEARTBEAT
	}

	public enum PLAYER_TYPE
	{
		NONE = 0,
		SHOOTER,
		MATCH3,
		PLAYER_TYPES
	}

	public class CONSTANTS
	{
		// Masks for diffs in the lobby player state
		public const byte TEAM_MASK = 1;
		public const byte READY_MASK = 2;
		public const byte PLAYER_TYPE_MASK = 4;
		public const byte PLAYER_ID_MASK = 8;
		public const byte NAME_MASK = 16;

		public const int SEND_ALL_PLAYERS = -1;

		public const int MAX_NUM_PLAYERS = 6;
	}

	public class DataUtils
	{
		public static string ReadString(ref int index, byte[] bytes)
		{
			int bytesRead = 0;

			// Get length of name
			byte stringBytesLength = bytes[index + bytesRead];
			++bytesRead;

			// Extract name into byte array
			byte[] stringAsBytes = new byte[stringBytesLength];
			for (int stringIndex = 0; stringIndex < stringBytesLength; ++stringIndex)
			{
				stringAsBytes[stringIndex] = bytes[index + bytesRead];
				++bytesRead;
			}

			index += bytesRead;

			// Convert from bytes to string
			return Encoding.UTF8.GetString(stringAsBytes);
		}
	}
}

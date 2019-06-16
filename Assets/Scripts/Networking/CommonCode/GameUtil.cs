using System.Text;

namespace GameUtils
{
	public enum NETWORK_DATA_TYPE
	{
		SEND_PLAYER_NAME
	}

	public class GamePlayerInfo
	{
		public byte team;
		public byte isReady;
		public string name;
		public PLAYER_TYPE playerType;
		public byte playerID;

		public GamePlayerInfo Clone()
		{
			GamePlayerInfo ret = new GamePlayerInfo();
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
	}

	public enum GAME_SERVER_COMMANDS
	{
		CHANGE_TEAM = 1,
		READY,
		SET_ID,
		SET_ALL_OBJECT_STATES,
		START_GAME,
		CREATE_ENTITY,
		CREATE_ENTITY_WITH_OWNERSHIP,
		HEARTBEAT
	}

	public enum GAME_CLIENT_REQUESTS
	{
		CHANGE_TEAM = 10,
		READY,
		GET_ID,
		CHANGE_PLAYER_TYPE,
		SET_ALL_OBJECT_STATES,
		CHANGE_NAME,
		START_GAME,
		CREATE_ENTITY,
		CREATE_ENTITY_WITH_OWNERSHIP,
		HEARTBEAT
	}

	public enum CREATE_ENTITY_TYPES
	{
		FPS_PLAYER = 20,
		MATCH3_PLAYER
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

		public const int SERVER_OWNS = 0;
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

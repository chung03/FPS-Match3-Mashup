namespace LobbyUtils
{
	public enum NETWORK_DATA_TYPE
	{
		SEND_PLAYER_NAME
	}

	public class LobbyPlayerInfo
	{
		public byte team;
		public byte isReady;
		public string name;
		public PLAYER_TYPE playerType;
		public byte playerID;

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
}

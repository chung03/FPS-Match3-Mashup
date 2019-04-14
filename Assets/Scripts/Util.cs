using Unity.Collections;

namespace Util
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
	}

	public enum LOBBY_SERVER_COMMANDS
	{
		CHANGE_TEAM = 1,
		READY,
		SET_ID,
		SET_ALL_PLAYER_STATES,
		START_GAME
	}

	public enum LOBBY_CLIENT_REQUESTS
	{
		CHANGE_TEAM = 10,
		READY,
		GET_ID,
		CHANGE_PLAYER_TYPE,
		START_GAME
	}

	public enum PLAYER_TYPE
	{
		NONE = 0,
		SHOOTER,
		MATCH3,
		PLAYER_TYPES
	}
}

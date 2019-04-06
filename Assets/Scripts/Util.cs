using Unity.Collections;

namespace Util
{
	public enum PLAYER_TYPE
	{
		TYPE_1,
		TYPE_2
	}

	public enum NETWORK_DATA_TYPE
	{
		SEND_PLAYER_NAME
	}

	public class LobbyPlayerInfo
	{
		public byte team;
		public byte isReady;
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
		START_GAME
	}
}

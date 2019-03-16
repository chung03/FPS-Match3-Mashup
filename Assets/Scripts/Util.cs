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

	public struct LobbyPlayerInfo
	{
		public byte team;
		public byte isReady;
	}

	public enum LOBBY_COMMANDS
	{
		CHANGE_TEAM = 1,
		READY,
		GET_ID
	}
}

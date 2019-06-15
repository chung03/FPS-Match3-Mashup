using System.Collections.Generic;

namespace CommonNetworkingUtils
{
	public delegate void ServerHandleIncomingBytes(ref int index, byte[] bytes, List<PersistentPlayerInfo> playerInfo, int playerIndex);
	public delegate int ClientHandleIncomingBytes(int index, byte[] bytes, List<PersistentPlayerInfo> playerInfo);
}
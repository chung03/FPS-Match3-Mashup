using System.Collections.Generic;

namespace CommonNetworkingUtils
{
	public delegate int ServerHandleIncomingBytes(int index, byte[] bytes, List<PersistentPlayerInfo> playerInfo, int playerIndex);
	public delegate int ClientHandleIncomingBytes(int index, byte[] bytes);
}
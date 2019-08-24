using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonNetworkingUtils;
using Unity.Networking.Transport;
using Unity.Collections;

using UnityEngine.Assertions;
using LobbyUtils;
using TestUtils;

public class FakeServerLobbyDataComponent : MonoBehaviour
{
	private enum LOBBY_SERVER_PROCESS
	{
		START_GAME,
		CHANGE_PLAYER_TYPE
	}

	// Current Player States
	public List<PersistentPlayerInfo> m_PlayerList;

	// Used for calculating deltas and ultimately save on network bandwidth
	public List<PersistentPlayerInfo> m_PreviousStatePlayerList;

	// A Pair of Dictionaries to make it easier to map Index and PlayerID
	// ID -> Connection Index
	private Dictionary<byte, int> IdToIndexDictionary;
	// Connection Index -> ID
	private Dictionary<int, byte> IndexToIdDictionary;

	// Command and the player ID
	private Queue<KeyValuePair<LOBBY_SERVER_PROCESS, int>> commandProcessingQueue;

	private FakeServerConnectionsComponent connectionsComponent;
	private FakeServerLobbySend serverLobbySend;

	int numTeam1Players = 0;
	int numTeam2Players = 0;

	private Trie<byte, List<byte>> byteSequenceTrie;

	private void Start()
	{
		//Debug.Log("FakeServerLobbyDataComponent::Start Called");
		m_PlayerList = new List<PersistentPlayerInfo>(CONSTANTS.MAX_NUM_PLAYERS);
		m_PreviousStatePlayerList = new List<PersistentPlayerInfo>(CONSTANTS.MAX_NUM_PLAYERS);

		IdToIndexDictionary = new Dictionary<byte, int>();
		IndexToIdDictionary = new Dictionary<int, byte>();

		commandProcessingQueue = new Queue<KeyValuePair<LOBBY_SERVER_PROCESS, int>>();

		byteSequenceTrie = new Trie<byte, List<byte>>();
	}


	public void Init(FakeServerConnectionsComponent connHolder)
	{
		//Debug.Log("FakeServerLobbyDataComponent::Init Called");
		connectionsComponent = connHolder;
		serverLobbySend = GetComponent<FakeServerLobbySend>();
	}

	void Update()
	{
		ref UdpNetworkDriver driver = ref connectionsComponent.GetDriver();
		ref NativeList<NetworkConnection> connections = ref connectionsComponent.GetConnections();

		// ***** Process data *****
		ProcessData(ref connections, ref driver);

		// ***** Send data *****
		serverLobbySend.SendDataIfReady(ref connections, ref driver, m_PlayerList);
	}

	public void SetResponse(List<byte> receivedBytes, List<byte> bytesToShare)
	{
		byteSequenceTrie.AddSequence(receivedBytes, bytesToShare);
	}

	public void ProcessData(ref NativeList<NetworkConnection> connections, ref UdpNetworkDriver driver)
	{
		while (commandProcessingQueue.Count > 0)
		{
			KeyValuePair<LOBBY_SERVER_PROCESS, int> processCommand = commandProcessingQueue.Dequeue();

			if (processCommand.Value == CONSTANTS.SEND_ALL_PLAYERS)
			{
				if (processCommand.Key == LOBBY_SERVER_PROCESS.START_GAME)
				{
					bool isReadytoStart = true;

					for (int playerNum = 0; playerNum < connections.Length; ++playerNum)
					{
						if (m_PlayerList[playerNum].playerType == PLAYER_TYPE.NONE
							|| m_PlayerList[playerNum].isReady == 0)
						{
							isReadytoStart = false;
						}
					}

					if (isReadytoStart)
					{
						connectionsComponent.SaveGameInfo(m_PlayerList);
						serverLobbySend.SendDataToPlayerWhenReady((byte)LOBBY_SERVER_COMMANDS.START_GAME, CONSTANTS.SEND_ALL_PLAYERS);
					}
				}
			}
			else
			{
				// Make sure that changing player types doesn't result in some invalid team configuration
				if (processCommand.Key == LOBBY_SERVER_PROCESS.CHANGE_PLAYER_TYPE)
				{
					int numShootersFound = 0;
					int numMatch3Found = 0;

					int currPlayerIndex = processCommand.Value;

					PersistentPlayerInfo currPlayer = m_PlayerList[currPlayerIndex];

					PLAYER_TYPE newplayerType = (PLAYER_TYPE)((int)(currPlayer.playerType + 1) % (int)PLAYER_TYPE.PLAYER_TYPES);

					// Get number of types of players on this player's team
					for (int playerIndex = 0; playerIndex < m_PlayerList.Count; ++playerIndex)
					{
						// Skip over self
						if (currPlayerIndex == playerIndex)
						{
							continue;
						}

						if (m_PlayerList[playerIndex].team == currPlayer.team)
						{
							if (m_PlayerList[playerIndex].playerType == PLAYER_TYPE.SHOOTER)
							{
								++numShootersFound;
							}
							else if (m_PlayerList[playerIndex].playerType == PLAYER_TYPE.MATCH3)
							{
								++numMatch3Found;
							}
						}
					}

					// Debug.Log("FakeServerLobbyDataComponent::ProcessData numShootersFound = " + numShootersFound + ", numMatch3Found = " + numMatch3Found);

					// Now figure out what type the player can be
					if (newplayerType == PLAYER_TYPE.SHOOTER
						&& numShootersFound >= 2)
					{
						newplayerType = (PLAYER_TYPE)((int)(newplayerType + 1) % (int)PLAYER_TYPE.PLAYER_TYPES);
					}

					if (newplayerType == PLAYER_TYPE.MATCH3
						&& numMatch3Found >= 1)
					{
						newplayerType = (PLAYER_TYPE)((int)(newplayerType + 1) % (int)PLAYER_TYPE.PLAYER_TYPES);
					}

					currPlayer.playerType = newplayerType;

					Debug.Log("FakeServerLobbyDataComponent::ProcessData Client " + currPlayerIndex + " player type was set to " + currPlayer.playerType.ToString());
				}
			}

		}
	}

	public void ProcessClientBytes(int playerIndex, byte[] bytes)
	{
		Debug.Log("FakeServerLobbyDataComponent::ReadClientBytes bytes.Length = " + bytes.Length);

		List<byte> receivedByteSequence = new List<byte>();

		for (int i = 0; i < bytes.Length;)
		{
			// LOBBY_CLIENT_REQUESTS clientCmd = (LOBBY_CLIENT_REQUESTS)bytes[i];
			
			// Debug.Log("FakeServerLobbyDataComponent::ReadClientBytes Got " + clientCmd + " from the Client");

			receivedByteSequence.Add(bytes[i]);

			// Unsafely assuming that everything is working as expected and there are no attackers.
			++i;
		}

		serverLobbySend.SendDataToPlayerWhenReady(byteSequenceTrie.GetValueOfSequence(receivedByteSequence), playerIndex);
	}

	private List<PersistentPlayerInfo> DeepClone(List<PersistentPlayerInfo> list)
	{
		List<PersistentPlayerInfo> ret = new List<PersistentPlayerInfo>();

		foreach (PersistentPlayerInfo player in list)
		{
			ret.Add(player.Clone());
		}

		return ret;
	}
}

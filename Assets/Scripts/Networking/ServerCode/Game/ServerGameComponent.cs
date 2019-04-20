using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

public class ServerGameComponent : MonoBehaviour
{
	private ServerConnectionsComponent connectionsComponent;
	private Queue<byte> allSendQueue;

	// Start is called before the first frame update
	void Start()
    {
        
    }

	public void Init(ServerConnectionsComponent connHolder)
	{
		//Debug.Log("ServerLobbyComponent::Init Called");
		connectionsComponent = connHolder;
	}

	// Update is called once per frame
	void Update()
    {
		ref UdpCNetworkDriver driver = ref connectionsComponent.GetDriver();
		ref NativeList<NetworkConnection> connections = ref connectionsComponent.GetConnections();

		driver.ScheduleUpdate().Complete();
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

public class ClientGameComponent : MonoBehaviour
{
	private ClientConnectionsComponent connectionsComponent;
	private Queue<byte> sendQueue;

	// Start is called before the first frame update
	void Start()
    {
        
    }

	public void Init(ClientConnectionsComponent connHolder)
	{
		//Debug.Log("ServerLobbyComponent::Init Called");
		connectionsComponent = connHolder;
	}

	// Update is called once per frame
	void Update()
    {
		ref UdpCNetworkDriver driver = ref connectionsComponent.GetDriver();
		ref NetworkConnection connection = ref connectionsComponent.GetConnection();

		driver.ScheduleUpdate().Complete();
	}
}

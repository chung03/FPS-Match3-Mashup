﻿using System.Net;
using UnityEngine;

using Unity.Networking.Transport;
using Unity.Collections;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

using UnityEngine.SceneManagement;

public class ClientConnectionsComponent : MonoBehaviour
{
	public UdpCNetworkDriver m_Driver;
	public NetworkConnection m_Connection;

	[SerializeField]
	private GameObject clientLobbyObj;

	[SerializeField]
	private GameObject clientGameObj;

	private void Start()
	{
		m_Driver = new UdpCNetworkDriver(new INetworkParameter[0]);
		m_Connection = default(NetworkConnection);

		var endpoint = new IPEndPoint(IPAddress.Loopback, 9000);
		m_Connection = m_Driver.Connect(endpoint);
	}

	private void OnDestroy()
	{
		m_Driver.Dispose();
	}

	public ref UdpCNetworkDriver GetDriver()
	{
		return ref m_Driver;
	}

	public ref NetworkConnection GetConnection()
	{
		return ref m_Connection;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		Debug.Log("ClientConnectionsComponent::OnSceneLoaded called");
		if (scene.name == "LobbyScene")
		{
			GameObject client = Instantiate(clientLobbyObj);
			client.GetComponent<ClientLobbyComponent>().Init(this);
		}
		else if (scene.name == "GameScene")
		{
			GameObject client = Instantiate(clientGameObj);
			// client.GetComponent<ClientGameComponent>().Init(this);
		}
	}

	public void Init()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
	}
}
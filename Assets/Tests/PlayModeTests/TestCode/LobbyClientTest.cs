using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

using System.Net;
using TestUtils;
using LobbyUtils;

namespace Tests
{
	public class LobbyClientTest
	{
		private bool loadedAllAssetBundles = false;

		[SetUp]
		public void SetUp()
		{
			DestroyAllGameObjectsInScene();

			if (!loadedAllAssetBundles)
			{
				AssetBundle myLoadedAssetBundle;
				myLoadedAssetBundle = AssetBundle.LoadFromFile("Assets/AssetBundles/testscenes");
				loadedAllAssetBundles = true;
			}
		}

		[TearDown]
		public void TearDown(){
			DestroyAllGameObjectsInScene();
		}

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator LobbyClientCanConnectToServer()
        {
			// Assuming the path of the test scene
			SceneManager.LoadScene("Assets/Tests/PlayModeTests/TestScenes/TestLobbyClient.unity");

			// Wait one frame for the scene to load
			yield return null;

			ClientConnectionsComponent clientConn = GameObject.Find("ClientConnectionObject").GetComponent<ClientConnectionsComponent>();
			FakeServerConnectionsComponent fakeServer = GameObject.Find("FakeServerConnectionsObject").GetComponent<FakeServerConnectionsComponent>();

			//clientConn.Init(false, "127.0.0.1");
			clientConn.SetIP("127.0.0.1");
			clientConn.PrepareClient("LobbyScene");

			fakeServer.PrepareServer("LobbyScene");

			yield return null;

			// ListAllGameObjectsInScene();

			//Time.timeScale = 20.0f;
			Time.timeScale = 1.0f;

			float time = 0;
			while (time < 3)
			{
				time += Time.fixedDeltaTime;
				yield return new WaitForFixedUpdate();
			}
			
			Time.timeScale = 1.0f;

			Assert.AreEqual(1, fakeServer.GetConnections().Length);
		}

		[UnityTest]
		public IEnumerator LobbyClientSendsHeartBeatAndIDToServer()
		{
			// Assuming the path of the test scene
			SceneManager.LoadScene("Assets/Tests/PlayModeTests/TestScenes/TestLobbyClient.unity");

			// Wait one frame for the scene to load
			yield return null;

			ClientConnectionsComponent clientConn = GameObject.Find("ClientConnectionObject").GetComponent<ClientConnectionsComponent>();
			FakeServerConnectionsComponent fakeServer = GameObject.Find("FakeServerConnectionsObject").GetComponent<FakeServerConnectionsComponent>();

			//clientConn.Init(false, "127.0.0.1");
			clientConn.SetIP("127.0.0.1");
			clientConn.PrepareClient("LobbyScene");

			fakeServer.PrepareServer("LobbyScene");

			yield return null;

			// ListAllGameObjectsInScene();

			FakeServerLobbyDataComponent fakeServerData = GameObject.Find("FakeServerLobbyObject(Clone)").GetComponent<FakeServerLobbyDataComponent>();

			List<byte> heartbeatRequest = new List<byte>();
			heartbeatRequest.Add((byte)LOBBY_CLIENT_REQUESTS.HEARTBEAT);

			List<byte> heartbeatResponse = new List<byte>();
			heartbeatResponse.Add((byte)LOBBY_SERVER_COMMANDS.HEARTBEAT);

			List<byte> getIdRequest = new List<byte>();
			getIdRequest.Add((byte)LOBBY_CLIENT_REQUESTS.GET_ID);

			List<byte> getIdResponse = new List<byte>();
			getIdResponse.Add((byte)LOBBY_SERVER_COMMANDS.SET_ID);
			getIdResponse.Add((byte)1);

			fakeServerData.SetResponse(heartbeatRequest, heartbeatResponse);
			fakeServerData.SetResponse(getIdRequest, getIdResponse);

			//Time.timeScale = 20.0f;
			Time.timeScale = 1.0f;

			float time = 0;
			while (time < 7)
			{
				time += Time.fixedDeltaTime;
				yield return new WaitForFixedUpdate();
			}

			Time.timeScale = 1.0f;

			Assert.AreEqual(1, fakeServer.GetConnections().Length);
		}

		private void ListAllGameObjectsInScene()
		{
			GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
			foreach (GameObject go in allObjects)
			{
				Debug.Log("GO in Test Scene: " + go.name);
			}
		}

		private void DestroyAllGameObjectsInScene()
		{
			GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
			foreach (GameObject go in allObjects)
			{
				GameObject.Destroy(go);
			}
		}
	}
}

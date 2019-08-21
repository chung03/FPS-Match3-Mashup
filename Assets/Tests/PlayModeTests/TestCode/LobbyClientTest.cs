using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

using System.Net;

namespace Tests
{
    public class LobbyClientTest
    {
        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator LobbyClientCanConnectToServer()
        {
			AssetBundle myLoadedAssetBundle;
			myLoadedAssetBundle = AssetBundle.LoadFromFile("Assets/AssetBundles/testscenes");

			// Assuming the path of the test scene
			SceneManager.LoadScene("Assets/Tests/PlayModeTests/TestScenes/TestLobbyClient.unity");

			// Wait one frame for the scene to load
			yield return null;

			ClientConnectionsComponent clientConn = GameObject.Find("ClientConnectionObject").GetComponent<ClientConnectionsComponent>();
			FakeLobbyServer fakeServer = GameObject.Find("FakeLobbyServerObject").GetComponent<FakeLobbyServer>();

			clientConn.Init(false, IPAddress.Loopback);
			clientConn.PrepareClient("LobbyScene");


			//Time.timeScale = 20.0f;
			Time.timeScale = 1.0f;

			float time = 0;
			while (time < 6)
			{
				time += Time.fixedDeltaTime;
				yield return new WaitForFixedUpdate();
			}
			
			Time.timeScale = 1.0f;

			Assert.AreEqual(1, fakeServer.GetConnections().Length);
		}
    }
}

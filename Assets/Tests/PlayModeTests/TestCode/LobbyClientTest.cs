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
			/*
			GameObject fakeServerObj = new GameObject();
			fakeServerObj.AddComponent<FakeLobbyServer>();

			GameObject lobbyClientConnObj = new GameObject();
			lobbyClientConnObj.AddComponent<ClientConnectionsComponent>();

			ClientConnectionsComponent lobbyClientConn = lobbyClientConnObj.GetComponent<ClientConnectionsComponent>();
			lobbyClientConn.Init(false, IPAddress.Loopback);

			GameObject lobbyClientObj = new GameObject();
			lobbyClientObj.AddComponent<ClientLobbyReceiveComponent>();
			lobbyClientObj.AddComponent<ClientLobbyDataComponent>();
			lobbyClientObj.AddComponent<ClientLobbySend>();

			lobbyClientObj.GetComponent<ClientLobbyReceiveComponent>().Init(lobbyClientConn);
			lobbyClientObj.GetComponent<ClientLobbyDataComponent>().Init(lobbyClientConn);
			*/

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
			while (time < 10)
			{
				time += Time.fixedDeltaTime;
				yield return new WaitForFixedUpdate();
			}

			Time.timeScale = 1.0f;

			yield return null;


			//GameObject.Destroy(fakeServerObj);

			/*
			// Increase the timeScale so the test executes quickly
			Time.timeScale = 20.0f;

			// _setup is a member of the class TestSetup where I store the code for
			//setting up the test scene (so that I don’t have a lot of copy-pasted code)
			Camera cam = _setup.CreateCameraForTest();

			GameObject[] paddles = _setup.CreatePaddlesForTest();

			float time = 0;
			while (time < 5)
			{
				paddles[0].GetComponent<Paddle>().RenderPaddle();
				paddles[0].GetComponent<Paddle>().MoveUpY("Paddle1");
				time += Time.fixedDeltaTime;
				yield return new WaitForFixedUpdate();
			}

			// Reset timeScale
			Time.timeScale = 1.0f;

			// Edge of paddle should not leave edge of screen
			// (Camera.main.orthographicSize - paddle.transform.localScale.y /2) is where the edge
			//of the paddle touches the edge of the screen, and 0.15 is the margin of error I gave it
			//to wait for the next frame
			Assert.LessOrEqual(paddles[0].transform.position.y, (Camera.main.orthographicSize - paddles[1].transform.localScale.y / 2) + 0.15
			*/
		}
    }
}

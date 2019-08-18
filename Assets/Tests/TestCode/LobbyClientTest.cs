using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class LobbyClientTest
    {
        // A Test behaves as an ordinary method
        [Test]
        public void LobbyClientSimplePasses()
        {
            // Use the Assert class to test conditions
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator LobbyClientWithEnumeratorPasses()
        {
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
			return null;
		}
    }
}

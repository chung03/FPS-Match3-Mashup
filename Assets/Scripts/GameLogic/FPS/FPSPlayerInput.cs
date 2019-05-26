using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class FPSPlayerInput : MonoBehaviour{

    private FPSMove fpsMove;
    //private FPSShooting fpsShooting;

	// Use this for initialization
	private void Start () {
        fpsMove = GetComponent<FPSMove>();
        //fpsShooting = GetComponent<FPSShooting>();
    }
	
	// Update is called once per frame
	private void FixedUpdate () {

        if (Input.GetKey(KeyCode.W))
        {
            fpsMove.GoForward();
        }

        if (Input.GetKey(KeyCode.S))
        {
            fpsMove.GoBackwards();
        }

        if (Input.GetKey(KeyCode.A))
        {
            fpsMove.GoLeft();
        }

        if (Input.GetKey(KeyCode.D))
        {
            fpsMove.GoRight();
        }

        if (Input.GetKey(KeyCode.Space))
        {
            fpsMove.DoJump();
        }

		/*
        if (Input.GetKeyDown(KeyCode.Q))
        {
            fpsShooting.ShootRotateLeft();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            fpsShooting.ShootRotateRight();
        }

        if (Input.GetMouseButton(0))
        {
            fpsShooting.ShootGun();
        }

		if (Input.GetMouseButtonDown(1))
		{
			fpsShooting.ShootGrappleHook();
		}

		if (Input.GetMouseButtonUp(1))
		{
			fpsShooting.StopGrapple();
		}
		*/
	}
}

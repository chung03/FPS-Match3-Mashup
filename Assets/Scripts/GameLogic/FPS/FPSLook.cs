using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class FPSLook : MonoBehaviour
{

    [SerializeField]
    private float lookSpeed = 0;

    private float rotY = 0.0f; // rotation around the up/y axis
    private float rotX = 0.0f; // rotation around the right/x axis

	private Vector3 initialRotation;

	[SerializeField]
	private Camera fpsCamera = null;

	private FPSPlayer fpsPlayer;

	private void Start()
	{
		fpsPlayer = GetComponent<FPSPlayer>();
	}

	// Update is called once per frame
	private void Update () {

        rotX -= Input.GetAxis("Mouse Y") * lookSpeed;
        rotY += Input.GetAxis("Mouse X") * lookSpeed;

        rotX = Mathf.Clamp(rotX, -85, 85);

        Quaternion localRotation = Quaternion.Euler(rotX, rotY, 0.0f);
        transform.rotation = localRotation;

		fpsPlayer.SetPlayerRotation(transform.rotation);
	}

	public void SetInitialLook(Vector3 _initialRotation)
	{
		initialRotation = _initialRotation;
		rotX = initialRotation.x;
		rotY = initialRotation.y;
	}

	public void ResetLook()
	{
		rotX = initialRotation.x;
		rotY = initialRotation.y;
	}

    public void SetCameraActive()
    {
        fpsCamera.gameObject.SetActive(true);
    }

	public Camera GetCamera()
    {
        return fpsCamera;
    }
}

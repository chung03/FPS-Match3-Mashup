using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSPlayer : MonoBehaviour
{
	private FPSPlayerData data;

	// Start is called before the first frame update
	private void Start()
    {
		data = new FPSPlayerData();
	}

	public void SetPlayerPosn(Vector3 posn)
	{
		data.SetPlayerPosn(posn);
	}

	public void SetPlayerRotation(Quaternion rotation)
	{
		data.SetPlayerRotation(rotation);
	}
}

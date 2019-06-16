using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSPlayer : MonoBehaviour
{
	private FPSPlayerData data;
	private ClientGameComponent clientGameComponent;

	// Start is called before the first frame update
	private void Start()
    {
		clientGameComponent.AddObjectWithDeltaClient(data);
	}

	public void Init(ClientGameComponent _clientGameComponent, int objectId)
	{
		clientGameComponent = _clientGameComponent;
		data = new FPSPlayerData();
		data.SetObjectId(objectId);
	}

	public void SetPlayerPosn(Vector3 posn)
	{
		data.SetPlayerPosn(posn);
	}

	public void SetPlayerRotation(Quaternion rotation)
	{
		data.SetPlayerRotation(rotation);
	}

	public FPSPlayerData GetData()
	{
		return data;
	}
}

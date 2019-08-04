using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSPlayer : MonoBehaviour
{
	private FPSPlayerData data;
	private ClientGameDataComponent clientGameDataComponent;
	private bool isOwnedByThisClient = false;

	// Start is called before the first frame update
	private void Start()
    {
		clientGameDataComponent.AddObjectWithDeltaClient(data);
	}

	private void Update()
	{
		// If object is not owned by this client, let server control this object
		if (!isOwnedByThisClient)
		{
			transform.position = data.GetPlayerPosn();
			transform.rotation = data.GetPlayerRotation();
		}
	}

	public void Init(ClientGameDataComponent _clientGameDataComponent, int objectId, bool isOwner)
	{
		clientGameDataComponent = _clientGameDataComponent;
		data = new FPSPlayerData();
		data.SetObjectId(objectId);
		data.SetFpsPlayer(this);

		// Debugging code to get objects out of each other's way
		transform.position = new Vector3(objectId * 2, 0, objectId * 2);
		data.SetPlayerPosn(transform.position);

		if (!isOwner)
		{
			// Since the player doesn't control this, remove input. Also remove camera
			Destroy(GetComponent<FPSPlayerInput>());
			Destroy(GetComponent<FPSLook>());
			Destroy(GetComponent<FPSMove>());
			Destroy(transform.Find("Camera").gameObject);
		}

		isOwnedByThisClient = isOwner;

	}

	public bool IsOwnedByThisClient()
	{
		return isOwnedByThisClient;
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

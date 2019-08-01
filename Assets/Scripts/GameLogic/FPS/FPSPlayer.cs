using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSPlayer : MonoBehaviour
{
	private FPSPlayerData data;
	private ClientGameDataComponent clientGameDataComponent;

	// Start is called before the first frame update
	private void Start()
    {
		clientGameDataComponent.AddObjectWithDeltaClient(data);
	}

	private void Update()
	{
		//if (!data.IsDirty())
		//{
		// transform.position = data.GetPlayerPosn();
		//}
	}

	public void Init(ClientGameDataComponent _clientGameDataComponent, int objectId, bool isOwner)
	{
		clientGameDataComponent = _clientGameDataComponent;
		data = new FPSPlayerData();
		data.SetObjectId(objectId);

		if (!isOwner)
		{
			// Since the player doesn't control this, remove input. Also remove camera
			Destroy(GetComponent<FPSPlayerInput>());
			Destroy(GetComponent<FPSLook>());
			Destroy(transform.Find("Camera").gameObject);
		}
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

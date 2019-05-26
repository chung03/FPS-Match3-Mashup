using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSPlayer : MonoBehaviour, ObjectWithDelta
{
	private class FPSPlayerState
	{
		public Transform currLocation;
	}

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public List<byte> GetDeltaBytes(bool getFullState)
	{
		List<byte> deltaBytes = new List<byte>();

		

		return deltaBytes;
	}

	public void ApplyDelta(byte[] delta)
	{
		
	}

	public bool IsDirty()
	{
		return false;
	}

	public int GetObjectId()
	{
		return 0;
	}
}

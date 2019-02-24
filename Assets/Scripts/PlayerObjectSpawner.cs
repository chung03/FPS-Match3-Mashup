using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Util;

public class PlayerObjectSpawner : NetworkBehaviour
{
	[SyncVar]
	private int team = 0;

	[SyncVar]
	private PLAYER_TYPE playerType = PLAYER_TYPE.TYPE_1;

	// Start is called before the first frame update
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

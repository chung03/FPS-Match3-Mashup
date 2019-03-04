using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyUIBehaviour : MonoBehaviour
{
	[SerializeField]
	private GameObject startGameButton;

    // Start is called before the first frame update
    private void Start()
    {
        
    }

    // Update is called once per frame
    private void Update()
    {
        
    }

	public void SetUI(bool isServer)
	{
		if (isServer)
		{
			startGameButton.SetActive(true);
		}
	}
}

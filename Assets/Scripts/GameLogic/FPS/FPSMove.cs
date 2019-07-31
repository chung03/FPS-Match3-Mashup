using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class FPSMove : MonoBehaviour
{

	private enum MOVE_STATE
	{
		ON_GROUND,
		JUMPING,
		GRAPPLING_HOOK
	}

	[SerializeField]
    private float moveSpeed;

	[SerializeField]
	private float grapplingHookSpeed;

	[SerializeField]
    private float jumpHeight;

    [SerializeField]
    private float jumpDuration;

    private float timeSinceJumpStart = 0f;
    private MOVE_STATE moveState = MOVE_STATE.ON_GROUND;

    private CharacterController charController;

    private Vector3[] momentVectors = new Vector3[4];

	private Vector3 grappleHookTarget = Vector3.zero;

	private FPSPlayer fpsPlayer;

	// Use this for initialization
	private void Start () {
        charController = GetComponent<CharacterController>();
		fpsPlayer = GetComponent<FPSPlayer>();
    }
    
    // Update is called once per frame
    private void FixedUpdate()
    {
        Vector3 moveVector = Vector3.zero;

		if (moveState == MOVE_STATE.GRAPPLING_HOOK)
		{
			Vector3 moveDir = grappleHookTarget - transform.position;

			//Vector3 finalMoveDir = Vector3.ClampMagnitude(moveDir, grapplingHookSpeed);
			Vector3 finalMoveDir = moveDir.normalized * grapplingHookSpeed;

			moveVector += finalMoveDir;
		}
		else
		{
			if (moveState == MOVE_STATE.JUMPING)
			{
				if (timeSinceJumpStart < jumpDuration)
				{
					timeSinceJumpStart += Time.deltaTime;
					moveVector += Vector3.up * jumpHeight * (jumpDuration - timeSinceJumpStart);
				}
				else if (charController.isGrounded)
				{
					moveState = MOVE_STATE.ON_GROUND;
				}
			}


			Vector3 nonJumpMovement = Vector3.zero;

			for (int move = 0; move < momentVectors.Length; ++move)
			{
				nonJumpMovement += momentVectors[move] * moveSpeed;
			}

			nonJumpMovement.y = 0;

			moveVector += nonJumpMovement;

			moveVector += Physics.gravity;
		}


		MoveChar(moveVector);

		if (moveVector != Vector3.zero)
		{
			// Send data to the Server
			fpsPlayer.SetPlayerPosn(transform.position + moveVector);
		}

		for (int move = 0; move < momentVectors.Length; ++move)
        {
            momentVectors[move] = Vector3.zero;
        }
	}

	public void CheckCollision(ControllerColliderHit hit)
	{
		if (moveState == MOVE_STATE.GRAPPLING_HOOK)
		{
			StopGrapplingHook();
		}
	}


	private void MoveChar(Vector3 dir)
    {
        charController.Move(dir * Time.deltaTime);
    }

    public void GoForward()
    {
        momentVectors[0] = transform.forward;
    }

    public void GoBackwards()
    {
        momentVectors[1] = -transform.forward;
    }

    public void GoLeft()
    {
        momentVectors[2] = -transform.right;
    }

    public void GoRight()
    {
        momentVectors[3] = transform.right;
    }

    public void DoJump()
    {
        if (moveState == MOVE_STATE.ON_GROUND)
        {
            timeSinceJumpStart = 0f;
            moveState = MOVE_STATE.JUMPING;
        }
    }

	public void StartGrapplingHook(Vector3 grapplingPosn)
	{
		moveState = MOVE_STATE.GRAPPLING_HOOK;
		grappleHookTarget = grapplingPosn;
	}

	public void StopGrapplingHook()
	{
		if (moveState == MOVE_STATE.GRAPPLING_HOOK)
		{
			moveState = MOVE_STATE.ON_GROUND;
			DoJump();
		}
	}
}

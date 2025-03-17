using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerAnimator : NetworkBehaviour
{

    private Animator animator;

    [SerializeField] private PlayerController player;

    private const string JUMP = "jump";
    private const string JUMP_RUNNING = "jumpRunning";
    private const string VERTICAL_AXIS = "vertical";
    private const string HORIZONTAL_AXIS = "horizontal";

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
        {
            return;
        }

        CheckIfJumpingServerRpc(player.IsJumping(), player.IsWalking());
        UpdateMovementBlendTreeServerRpc(player.getVerticalAxis(), player.getHorizontalAxis());
    }

    [Rpc(SendTo.Server)]
    public void CheckIfJumpingServerRpc(bool isJumping, bool isWalking)
    {
        //animator.SetBool(JUMP, isJumping && !isWalking);
        //animator.SetBool(JUMP_RUNNING, isJumping && isWalking);
    }

    [Rpc(SendTo.Server)]
    public void UpdateMovementBlendTreeServerRpc(float verticalAxis, float horizontalAxis)
    {
        animator.SetFloat(VERTICAL_AXIS, verticalAxis);
        animator.SetFloat(HORIZONTAL_AXIS, horizontalAxis);
    }
}

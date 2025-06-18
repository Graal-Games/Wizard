using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerAnimator : NetworkBehaviour
{
    private Animator animator;
    
    [SerializeField] private CharacterControllerMovement characterControllerMovement;
    
    private const string JUMP = "jump";
    private const string JUMP_RUNNING = "jumpRunning";
    private const string VERTICAL_AXIS = "vertical";
    private const string HORIZONTAL_AXIS = "horizontal";
    private const string IS_WALKING = "isWalking";
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        
        // Try to find CharacterControllerMovement if not assigned
        if (characterControllerMovement == null)
        {
            characterControllerMovement = GetComponent<CharacterControllerMovement>();
            if (characterControllerMovement == null)
            {
                characterControllerMovement = GetComponentInParent<CharacterControllerMovement>();
            }
        }
    }
    
    private void FixedUpdate()
    {
        if (!IsOwner)
        {
            return;
        }
        
        // Check if components are available
        if (characterControllerMovement == null)
        {
            Debug.LogWarning("CharacterControllerMovement is not assigned in PlayerAnimator!");
            return;
        }
        
        // Get movement input - with null check for GameInput
        Vector2 inputVector = Vector2.zero;
        if (GameInput.Instance != null)
        {
            inputVector = GameInput.Instance.GetMovementVector();
        }
        else
        {
            // Fallback to direct input if GameInput is not available
            inputVector = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        }
        
        CheckIfJumpingServerRpc(characterControllerMovement.IsJumping, characterControllerMovement.IsWalking);
        UpdateMovementBlendTreeServerRpc(inputVector.y, inputVector.x, characterControllerMovement.IsWalking);
    }
    
    [Rpc(SendTo.Server)]
    public void CheckIfJumpingServerRpc(bool isJumping, bool isWalking)
    {
        if (animator != null)
        {
            animator.SetBool(JUMP, isJumping && !isWalking);
            animator.SetBool(JUMP_RUNNING, isJumping && isWalking);
        }
    }
    
    [Rpc(SendTo.Server)]
    public void UpdateMovementBlendTreeServerRpc(float verticalAxis, float horizontalAxis, bool isWalking)
    {
        if (animator != null)
        {
            animator.SetFloat(VERTICAL_AXIS, verticalAxis);
            animator.SetFloat(HORIZONTAL_AXIS, horizontalAxis);
            animator.SetBool(IS_WALKING, isWalking);
        }
    }
}

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
    
    // Cache parameter existence
    private bool hasJumpParam = false;
    private bool hasJumpRunningParam = false;
    private bool hasVerticalParam = false;
    private bool hasHorizontalParam = false;
    private bool hasIsWalkingParam = false;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        
        // Try to find CharacterControllerMovement if not assigned
        if (characterControllerMovement == null)
        {
            characterControllerMovement = GetComponent<CharacterControllerMovement>();
        }
        
        // Check which parameters exist in the animator
        if (animator != null)
        {
            CheckAnimatorParameters();
        }
    }
    
    private void CheckAnimatorParameters()
    {
        if (animator == null) return;
        
        // Check each parameter
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            switch (param.name)
            {
                case JUMP:
                    hasJumpParam = true;
                    break;
                case JUMP_RUNNING:
                    hasJumpRunningParam = true;
                    break;
                case VERTICAL_AXIS:
                    hasVerticalParam = true;
                    break;
                case HORIZONTAL_AXIS:
                    hasHorizontalParam = true;
                    break;
                case IS_WALKING:
                    hasIsWalkingParam = true;
                    break;
            }
        }
        
        // Log missing parameters for debugging
        if (!hasJumpParam) Debug.LogWarning($"Animator is missing parameter: {JUMP}");
        if (!hasJumpRunningParam) Debug.LogWarning($"Animator is missing parameter: {JUMP_RUNNING}");
        if (!hasVerticalParam) Debug.LogWarning($"Animator is missing parameter: {VERTICAL_AXIS}");
        if (!hasHorizontalParam) Debug.LogWarning($"Animator is missing parameter: {HORIZONTAL_AXIS}");
        if (!hasIsWalkingParam) Debug.LogWarning($"Animator is missing parameter: {IS_WALKING}");
    }
    
    private void FixedUpdate()
    {
        if (!IsOwner) return;
        
        // Skip if no animator or movement controller
        if (animator == null || characterControllerMovement == null) return;
        
        // Get movement input safely
        Vector2 inputVector = GetMovementInput();
        
        // Update animator parameters only if they exist
        if (hasVerticalParam)
            animator.SetFloat(VERTICAL_AXIS, inputVector.y);
            
        if (hasHorizontalParam)
            animator.SetFloat(HORIZONTAL_AXIS, inputVector.x);
        
        // Check if moving
        bool isWalking = inputVector.magnitude > 0.01f;
        if (hasIsWalkingParam)
        {
            CheckIfWalkingServerRpc(isWalking);
        }
        
        // Check if jumping - only if we have valid movement data
        if (characterControllerMovement.IsJumping && (hasJumpParam || hasJumpRunningParam))
        {
            CheckIfJumpingServerRpc(characterControllerMovement.IsJumping, isWalking);
        }
    }
    
    private Vector2 GetMovementInput()
    {
        // Try to get input from GameInput component first using reflection
        GameObject gameInputGO = GameObject.Find("GameInput");
        if (gameInputGO != null)
        {
            var gameInputComponent = gameInputGO.GetComponent("GameInput");
            if (gameInputComponent != null)
            {
                try
                {
                    var method = gameInputComponent.GetType().GetMethod("GetMovementVector");
                    if (method != null)
                    {
                        var result = method.Invoke(gameInputComponent, null);
                        if (result is Vector2)
                        {
                            return (Vector2)result;
                        }
                    }
                }
                catch
                {
                    // Fall through to direct input
                }
            }
        }
        
        // Fallback to direct input
        return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
    }
    
    [ServerRpc]
    private void CheckIfWalkingServerRpc(bool isWalking)
    {
        CheckIfWalkingClientRpc(isWalking);
    }
    
    [ClientRpc]
    private void CheckIfWalkingClientRpc(bool isWalking)
    {
        if (animator != null && hasIsWalkingParam)
        {
            animator.SetBool(IS_WALKING, isWalking);
        }
    }
    
    [ServerRpc]
    private void CheckIfJumpingServerRpc(bool isJumping, bool isWalking)
    {
        CheckIfJumpingClientRpc(isJumping, isWalking);
    }
    
    [ClientRpc]
    private void CheckIfJumpingClientRpc(bool isJumping, bool isWalking)
    {
        if (animator == null) return;
        
        if (isWalking && hasJumpRunningParam)
        {
            animator.SetBool(JUMP_RUNNING, isJumping);
        }
        else if (hasJumpParam)
        {
            animator.SetBool(JUMP, isJumping);
        }
    }
}


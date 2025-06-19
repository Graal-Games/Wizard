using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Alternative animator adapter that works with ANY existing animator setup
/// without requiring specific parameter names
/// </summary>
public class FlexibleAnimatorAdapter : MonoBehaviour
{
    [Header("Map Your Existing Animator Parameters")]
    [Tooltip("The name of your walking/running parameter (could be 'Speed', 'Walk', 'Moving', etc.)")]
    public string walkParameterName = "Speed";
    
    [Tooltip("Is your walk parameter a Bool, Float, or Trigger?")]
    public AnimatorControllerParameterType walkParameterType = AnimatorControllerParameterType.Float;
    
    [Tooltip("The name of your grounded parameter (could be 'OnGround', 'Grounded', etc.)")]
    public string groundedParameterName = "";
    
    [Tooltip("The name of your jump parameter (could be 'Jump', 'InAir', etc.)")]
    public string jumpParameterName = "";
    
    [Header("Float Parameter Settings")]
    [Tooltip("If using float for walk, what value represents walking?")]
    public float walkingSpeed = 1f;
    
    [Tooltip("Smoothing for float parameters")]
    public float smoothing = 0.1f;
    
    private Animator animator;
    private CharacterControllerMovement movement;
    private float currentSpeed = 0f;
    
    // Parameter hashes for performance
    private int walkParamHash;
    private int groundedParamHash;
    private int jumpParamHash;
    private bool hasWalkParam;
    private bool hasGroundedParam;
    private bool hasJumpParam;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        
        movement = GetComponent<CharacterControllerMovement>();
        
        if (animator == null || movement == null)
        {
            Debug.LogError($"FlexibleAnimatorAdapter requires both Animator and CharacterControllerMovement!");
            enabled = false;
            return;
        }
        
        // Discover and setup parameters
        DiscoverParameters();
    }
    
    void DiscoverParameters()
    {
        Debug.Log("=== Flexible Animator Adapter: Discovering Parameters ===");
        
        // List all parameters for user reference
        Debug.Log($"Found {animator.parameters.Length} parameters in Animator Controller:");
        foreach (var param in animator.parameters)
        {
            Debug.Log($"  - '{param.name}' ({param.type})");
        }
        
        // Setup parameter hashes
        if (!string.IsNullOrEmpty(walkParameterName))
        {
            walkParamHash = Animator.StringToHash(walkParameterName);
            hasWalkParam = HasParameter(walkParameterName);
            if (hasWalkParam)
            {
                Debug.Log($"✅ Using '{walkParameterName}' for walking animation");
            }
            else
            {
                Debug.LogWarning($"❌ Parameter '{walkParameterName}' not found! Walking animation won't work.");
                Debug.LogWarning("Please set the correct parameter name in the inspector.");
            }
        }
        
        if (!string.IsNullOrEmpty(groundedParameterName))
        {
            groundedParamHash = Animator.StringToHash(groundedParameterName);
            hasGroundedParam = HasParameter(groundedParameterName);
            if (hasGroundedParam)
            {
                Debug.Log($"✅ Using '{groundedParameterName}' for grounded state");
            }
        }
        
        if (!string.IsNullOrEmpty(jumpParameterName))
        {
            jumpParamHash = Animator.StringToHash(jumpParameterName);
            hasJumpParam = HasParameter(jumpParameterName);
            if (hasJumpParam)
            {
                Debug.Log($"✅ Using '{jumpParameterName}' for jumping");
            }
        }
        
        // Suggest common parameter names if nothing is set
        if (string.IsNullOrEmpty(walkParameterName))
        {
            SuggestWalkParameters();
        }
    }
    
    void SuggestWalkParameters()
    {
        string[] commonWalkParams = { "Speed", "Walk", "Walking", "IsWalking", "Move", "Movement", "Velocity", "WalkSpeed", "MoveSpeed" };
        
        foreach (string paramName in commonWalkParams)
        {
            if (HasParameter(paramName))
            {
                var paramType = GetParameterType(paramName);
                Debug.Log($"💡 Found possible walk parameter: '{paramName}' ({paramType}) - Try setting this in the inspector!");
            }
        }
    }
    
    bool HasParameter(string paramName)
    {
        foreach (var param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }
    
    AnimatorControllerParameterType GetParameterType(string paramName)
    {
        foreach (var param in animator.parameters)
        {
            if (param.name == paramName)
                return param.type;
        }
        return AnimatorControllerParameterType.Float;
    }
    
    void Update()
    {
        if (!animator || !movement) return;
        
        // Update walking/movement animation
        if (hasWalkParam)
        {
            bool isWalking = movement.IsWalking;
            
            switch (walkParameterType)
            {
                case AnimatorControllerParameterType.Float:
                    // Smooth float parameter
                    float targetSpeed = isWalking ? walkingSpeed : 0f;
                    currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime / smoothing);
                    animator.SetFloat(walkParamHash, currentSpeed);
                    break;
                    
                case AnimatorControllerParameterType.Bool:
                    animator.SetBool(walkParamHash, isWalking);
                    break;
                    
                case AnimatorControllerParameterType.Trigger:
                    if (isWalking && currentSpeed < 0.1f)
                    {
                        animator.SetTrigger(walkParamHash);
                    }
                    currentSpeed = isWalking ? 1f : 0f;
                    break;
            }
        }
        
        // Update grounded state
        if (hasGroundedParam)
        {
            animator.SetBool(groundedParamHash, movement.IsGrounded);
        }
        
        // Update jump state
        if (hasJumpParam)
        {
            bool isJumping = movement.IsJumping;
            
            var paramType = GetParameterType(jumpParameterName);
            switch (paramType)
            {
                case AnimatorControllerParameterType.Bool:
                    animator.SetBool(jumpParamHash, isJumping);
                    break;
                    
                case AnimatorControllerParameterType.Trigger:
                    if (isJumping)
                    {
                        animator.SetTrigger(jumpParamHash);
                    }
                    break;
            }
        }
    }
} 
using UnityEngine;

// Runtime script for checking and helping set up animator parameters
public class AnimatorParameterSetup : MonoBehaviour
{
    private Animator animator;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            CheckAndReportParameters();
        }
        else
        {
            Debug.LogError($"No Animator or Animator Controller found on {gameObject.name}!");
        }
    }
    
    void CheckAndReportParameters()
    {
        Debug.Log($"=== Checking Animator Controller: {animator.runtimeAnimatorController.name} ===");
        
        bool hasIsGrounded = false;
        bool hasIsWalking = false;
        bool hasIsJumping = false;
        
        // Check existing parameters
        Debug.Log($"Current parameters ({animator.parameters.Length} total):");
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            Debug.Log($"  - {param.name} ({param.type})");
            
            if (param.name == "IsGrounded" && param.type == AnimatorControllerParameterType.Bool)
                hasIsGrounded = true;
            if (param.name == "IsWalking" && param.type == AnimatorControllerParameterType.Bool)
                hasIsWalking = true;
            if (param.name == "IsJumping" && param.type == AnimatorControllerParameterType.Bool)
                hasIsJumping = true;
        }
        
        // Report missing parameters
        if (!hasIsGrounded || !hasIsWalking || !hasIsJumping)
        {
            Debug.LogError("=== MISSING REQUIRED ANIMATOR PARAMETERS ===");
            Debug.LogError($"Animator Controller '{animator.runtimeAnimatorController.name}' is missing:");
            if (!hasIsGrounded) Debug.LogError("  ❌ IsGrounded (Bool)");
            if (!hasIsWalking) Debug.LogError("  ❌ IsWalking (Bool)");
            if (!hasIsJumping) Debug.LogError("  ❌ IsJumping (Bool)");
            
            Debug.LogError("\n=== HOW TO FIX ===");
            Debug.LogError("1. In Project window, find your Animator Controller asset");
            Debug.LogError("2. Double-click to open the Animator window");
            Debug.LogError("3. Click the 'Parameters' tab (usually on the left)");
            Debug.LogError("4. Click the '+' button and select 'Bool'");
            Debug.LogError("5. Add these exact parameter names:");
            Debug.LogError("   - IsGrounded");
            Debug.LogError("   - IsWalking");
            Debug.LogError("   - IsJumping");
            Debug.LogError("\n=== SETTING UP TRANSITIONS ===");
            Debug.LogError("After adding parameters, set up transitions:");
            Debug.LogError("1. Right-click your Idle animation → Make Transition → Walk animation");
            Debug.LogError("2. Click the transition arrow, add condition: IsWalking = true");
            Debug.LogError("3. Right-click Walk animation → Make Transition → Idle");
            Debug.LogError("4. Click that transition, add condition: IsWalking = false");
            Debug.LogError("5. For jumping, use Any State → Jump with IsJumping = true");
            Debug.LogError("==========================================");
        }
        else
        {
            Debug.Log("✅ All required animator parameters are present!");
            Debug.Log("If animations still don't work, check your animation transitions.");
        }
    }
} 
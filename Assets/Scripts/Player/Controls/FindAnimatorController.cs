using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper script to find your Animator Controller
/// Add this to your player GameObject and it will show you where the controller is
/// </summary>
public class FindAnimatorController : MonoBehaviour
{
    void Start()
    {
        FindAndReportAnimatorController();
    }
    
    [ContextMenu("Find Animator Controller")]
    public void FindAndReportAnimatorController()
    {
        Debug.Log("=== SEARCHING FOR ANIMATOR CONTROLLER ===");
        
        // Find the Animator component
        Animator animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        
        if (animator == null)
        {
            Debug.LogError("❌ No Animator component found on this GameObject or its children!");
            Debug.LogError("Make sure your player character has an Animator component.");
            return;
        }
        
        Debug.Log($"✅ Found Animator component on: {animator.gameObject.name}");
        
        // Check if controller is assigned
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError("❌ No Animator Controller assigned to the Animator component!");
            Debug.LogError("To fix: Select the Animator component and assign a controller in the 'Controller' field.");
            return;
        }
        
        // Get the controller
        var controller = animator.runtimeAnimatorController;
        Debug.Log($"✅ Animator Controller Name: {controller.name}");
        
#if UNITY_EDITOR
        // In editor, we can find the asset path
        string assetPath = AssetDatabase.GetAssetPath(controller);
        if (!string.IsNullOrEmpty(assetPath))
        {
            Debug.Log($"📁 Location: {assetPath}");
            Debug.Log("=== TO OPEN THE ANIMATOR CONTROLLER ===");
            Debug.Log("1. In the Project window, navigate to: " + assetPath);
            Debug.Log("2. Double-click the file: " + controller.name);
            Debug.Log("3. The Animator window will open");
            Debug.Log("4. Click the 'Parameters' tab on the left");
            Debug.Log("=====================================");
            
            // Ping the asset in project window
            Object controllerAsset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (controllerAsset != null)
            {
                EditorGUIUtility.PingObject(controllerAsset);
                Selection.activeObject = controllerAsset;
                Debug.Log("🎯 The Animator Controller has been highlighted in the Project window!");
            }
        }
#else
        Debug.Log("Note: Run this in the Unity Editor to see the exact file location.");
#endif
        
        // List current parameters
        Debug.Log($"\n=== CURRENT PARAMETERS ({animator.parameters.Length}) ===");
        if (animator.parameters.Length == 0)
        {
            Debug.LogWarning("No parameters found in the Animator Controller!");
        }
        else
        {
            foreach (var param in animator.parameters)
            {
                Debug.Log($"  - {param.name} ({param.type})");
            }
        }
        
        // Check for required parameters
        bool hasIsGrounded = false;
        bool hasIsWalking = false;
        bool hasIsJumping = false;
        
        foreach (var param in animator.parameters)
        {
            if (param.name == "IsGrounded") hasIsGrounded = true;
            if (param.name == "IsWalking") hasIsWalking = true;
            if (param.name == "IsJumping") hasIsJumping = true;
        }
        
        Debug.Log("\n=== REQUIRED PARAMETERS CHECK ===");
        Debug.Log($"IsGrounded: {(hasIsGrounded ? "✅ Found" : "❌ Missing")}");
        Debug.Log($"IsWalking: {(hasIsWalking ? "✅ Found" : "❌ Missing")}");
        Debug.Log($"IsJumping: {(hasIsJumping ? "✅ Found" : "❌ Missing")}");
        
        if (!hasIsGrounded || !hasIsWalking || !hasIsJumping)
        {
            Debug.LogError("\n⚠️ Some required parameters are missing!");
            Debug.LogError("After opening the Animator Controller, add these Bool parameters in the Parameters tab.");
        }
    }
} 
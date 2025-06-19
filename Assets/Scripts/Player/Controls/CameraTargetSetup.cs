using UnityEngine;
using Cinemachine;

/// <summary>
/// Sets up a camera target at head/neck height for third-person view
/// </summary>
public class CameraTargetSetup : MonoBehaviour
{
    [Header("Camera Target Settings")]
    [Tooltip("Height offset from character's base position (1.5-1.8 for head level)")]
    [SerializeField] private float targetHeight = 1.6f;
    
    [Tooltip("Forward offset if you want camera to look slightly in front")]
    [SerializeField] private float forwardOffset = 0f;
    
    [Tooltip("The name for the camera target GameObject")]
    [SerializeField] private string targetName = "CameraLookTarget";
    
    [Header("Auto Setup")]
    [Tooltip("Automatically find and update Cinemachine cameras")]
    [SerializeField] private bool autoSetupCinemachine = true;
    
    private GameObject cameraTarget;
    
    void Start()
    {
        SetupCameraTarget();
    }
    
    [ContextMenu("Setup Camera Target")]
    public void SetupCameraTarget()
    {
        Debug.Log("=== Setting up Camera Target ===");
        
        // Check if target already exists
        Transform existingTarget = transform.Find(targetName);
        if (existingTarget != null)
        {
            cameraTarget = existingTarget.gameObject;
            Debug.Log($"Found existing camera target: {targetName}");
        }
        else
        {
            // Create new target
            cameraTarget = new GameObject(targetName);
            cameraTarget.transform.SetParent(transform);
            Debug.Log($"Created new camera target: {targetName}");
        }
        
        // Position the target at head height
        cameraTarget.transform.localPosition = new Vector3(0, targetHeight, forwardOffset);
        cameraTarget.transform.localRotation = Quaternion.identity;
        
        Debug.Log($"Camera target positioned at local height: {targetHeight}");
        
        // Add visual indicator in editor
        #if UNITY_EDITOR
        // Add an icon so it's visible in scene view
        var iconContent = UnityEditor.EditorGUIUtility.IconContent("sv_icon_dot0_pix16_gizmo");
        if (iconContent != null && iconContent.image != null)
        {
            UnityEditor.EditorGUIUtility.SetIconForObject(cameraTarget, (Texture2D)iconContent.image);
        }
        #endif
        
        if (autoSetupCinemachine)
        {
            UpdateCinemachineCameras();
        }
    }
    
    void UpdateCinemachineCameras()
    {
        Debug.Log("=== Updating Cinemachine Cameras ===");
        
        // Find all Cinemachine virtual cameras
        CinemachineVirtualCameraBase[] virtualCameras = FindObjectsOfType<CinemachineVirtualCameraBase>();
        
        int updatedCount = 0;
        foreach (var vcam in virtualCameras)
        {
            // Check if this camera is following our player
            if (vcam.Follow == transform)
            {
                // Update LookAt to our head target
                if (vcam.LookAt != cameraTarget.transform)
                {
                    vcam.LookAt = cameraTarget.transform;
                    Debug.Log($"✅ Updated {vcam.name} LookAt to {targetName}");
                    updatedCount++;
                }
                
                // For FreeLook cameras specifically
                if (vcam is CinemachineFreeLook freeLook)
                {
                    Debug.Log($"FreeLook camera '{freeLook.name}' settings:");
                    Debug.Log($"  - Follow: {freeLook.Follow?.name}");
                    Debug.Log($"  - LookAt: {freeLook.LookAt?.name}");
                    Debug.Log("  - Tip: You can also adjust 'Tracked Object Offset' in the Body settings");
                }
            }
        }
        
        if (updatedCount == 0)
        {
            Debug.LogWarning("No Cinemachine cameras were updated. Make sure:");
            Debug.LogWarning("1. You have a Cinemachine FreeLook camera in the scene");
            Debug.LogWarning("2. Its 'Follow' target is set to your player");
            Debug.LogWarning("3. Then run this script again");
            
            // Manual instructions
            Debug.Log("\n=== MANUAL SETUP ===");
            Debug.Log("1. Select your Cinemachine FreeLook camera");
            Debug.Log("2. In the inspector, set:");
            Debug.Log($"   - Follow: {gameObject.name} (your player)");
            Debug.Log($"   - LookAt: {gameObject.name}/{targetName}");
            Debug.Log("====================");
        }
        else
        {
            Debug.Log($"✅ Successfully updated {updatedCount} camera(s)!");
        }
    }
    
    // Allow height adjustment in real-time
    void OnValidate()
    {
        if (cameraTarget != null && Application.isPlaying)
        {
            cameraTarget.transform.localPosition = new Vector3(0, targetHeight, forwardOffset);
        }
    }
    
    void OnDrawGizmos()
    {
        // Draw a sphere at the look target position
        Vector3 targetPos = transform.position + Vector3.up * targetHeight + transform.forward * forwardOffset;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(targetPos, 0.1f);
        
        // Draw a line from feet to head
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, targetPos);
    }
} 
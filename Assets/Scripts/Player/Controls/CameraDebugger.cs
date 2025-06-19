using UnityEngine;
using Cinemachine;

/// <summary>
/// Debug script to find out why camera is looking at feet
/// </summary>
public class CameraDebugger : MonoBehaviour
{
    void Start()
    {
        DebugCameraSetup();
    }
    
    [ContextMenu("Debug Camera Setup")]
    public void DebugCameraSetup()
    {
        Debug.Log("=== CAMERA DEBUG REPORT ===");
        
        // Find main camera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("❌ No Main Camera found!");
            return;
        }
        Debug.Log($"✅ Main Camera: {mainCam.name}");
        
        // Check for Cinemachine Brain
        CinemachineBrain brain = mainCam.GetComponent<CinemachineBrain>();
        if (brain == null)
        {
            Debug.LogError("❌ No Cinemachine Brain on Main Camera! Cinemachine might not be set up.");
            return;
        }
        Debug.Log("✅ Cinemachine Brain found");
        
        // Find all virtual cameras
        CinemachineVirtualCameraBase[] vcams = FindObjectsOfType<CinemachineVirtualCameraBase>();
        Debug.Log($"\n=== FOUND {vcams.Length} VIRTUAL CAMERAS ===");
        
        CinemachineVirtualCameraBase activeVcam = null;
        
        foreach (var vcam in vcams)
        {
            bool isActive = CinemachineCore.Instance.IsLive(vcam);
            string status = isActive ? "🟢 ACTIVE" : "⚪ Inactive";
            
            Debug.Log($"\n{status} Camera: {vcam.name}");
            Debug.Log($"  Priority: {vcam.Priority}");
            Debug.Log($"  Follow: {(vcam.Follow ? vcam.Follow.name : "None")}");
            Debug.Log($"  LookAt: {(vcam.LookAt ? vcam.LookAt.name : "None")}");
            
            if (vcam.LookAt != null)
            {
                Debug.Log($"  LookAt World Position: {vcam.LookAt.position}");
                Debug.Log($"  LookAt Local Position: {vcam.LookAt.localPosition}");
            }
            
            // Check if this camera is looking at our player
            if (vcam.Follow == transform || (vcam.Follow && vcam.Follow.IsChildOf(transform)))
            {
                Debug.Log("  👁️ This camera is following the player!");
                
                if (isActive)
                {
                    activeVcam = vcam;
                }
                
                // For FreeLook cameras, check additional settings
                if (vcam is CinemachineFreeLook freeLook)
                {
                    Debug.Log("  📹 This is a FreeLook camera");
                    Debug.Log($"  Binding Mode: {freeLook.m_BindingMode}");
                    Debug.Log($"  Y Axis: {freeLook.m_YAxis.Value} (range: {freeLook.m_YAxis.m_MinValue} to {freeLook.m_YAxis.m_MaxValue})");
                    
                    // Check rig settings
                    Debug.Log("  === Rig Heights ===");
                    for (int i = 0; i < 3; i++)
                    {
                        var rig = freeLook.GetRig(i);
                        if (rig != null)
                        {
                            string rigName = i == 0 ? "Top" : i == 1 ? "Middle" : "Bottom";
                            var composer = rig.GetCinemachineComponent<CinemachineComposer>();
                            if (composer != null)
                            {
                                Debug.Log($"    {rigName} Rig - Screen Y: {composer.m_ScreenY}, Tracked Object Offset: {composer.m_TrackedObjectOffset}");
                            }
                        }
                    }
                }
            }
        }
        
        // Report on the active camera
        if (activeVcam != null)
        {
            Debug.Log($"\n=== ACTIVE CAMERA ANALYSIS: {activeVcam.name} ===");
            
            if (activeVcam.LookAt == null)
            {
                Debug.LogError("❌ LookAt target is NULL! This is why it's looking at feet (default to Follow target base)");
                Debug.Log("FIX: Set the LookAt target to a point at head height");
            }
            else if (activeVcam.LookAt == transform)
            {
                Debug.LogWarning("⚠️ LookAt is set to the player root transform (feet level)");
                Debug.Log("FIX: Set LookAt to a child object at head height");
            }
            else
            {
                Debug.Log($"LookAt target: {activeVcam.LookAt.name}");
                float lookAtHeight = activeVcam.LookAt.position.y - transform.position.y;
                Debug.Log($"LookAt height above player: {lookAtHeight:F2} units");
                
                if (lookAtHeight < 1.0f)
                {
                    Debug.LogWarning("⚠️ LookAt target is too low! Should be around 1.6 units above player base");
                }
            }
        }
        
        // Check for camera target child
        Transform cameraTarget = transform.Find("CameraLookTarget");
        if (cameraTarget != null)
        {
            Debug.Log($"\n✅ Found CameraLookTarget child at local position: {cameraTarget.localPosition}");
            Debug.Log("Make sure your active Cinemachine camera's LookAt is set to this target!");
        }
        else
        {
            Debug.LogWarning("\n❌ No CameraLookTarget child found. Did you add the CameraTargetSetup component?");
        }
        
        Debug.Log("\n=== HOW TO FIX ===");
        Debug.Log("1. Select your active Cinemachine camera in the Hierarchy");
        Debug.Log("2. In the Inspector, find the 'LookAt' field");
        Debug.Log("3. Drag your player's 'CameraLookTarget' child object into this field");
        Debug.Log("4. OR adjust 'Tracked Object Offset' Y value to 1.6 in the Composer settings");
        Debug.Log("==================");
    }
} 
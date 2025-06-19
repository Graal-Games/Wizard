using UnityEngine;
using Cinemachine;
using System.Collections;

/// <summary>
/// Forcefully fixes camera to look at head height
/// </summary>
public class ForceCameraFix : MonoBehaviour
{
    [Header("Head Target Settings")]
    [SerializeField] private float headHeight = 1.6f;
    [SerializeField] private bool createVisualMarker = true;
    
    [Header("Fix Options")]
    [SerializeField] private bool fixMethod1_CreateLookTarget = true;
    [SerializeField] private bool fixMethod2_AdjustComposer = true;
    [SerializeField] private bool fixMethod3_ForceUpdate = true;
    
    private GameObject headTarget;
    
    void Start()
    {
        StartCoroutine(DelayedFix());
    }
    
    IEnumerator DelayedFix()
    {
        // Wait a frame to ensure everything is initialized
        yield return null;
        
        Debug.Log("=== FORCE CAMERA FIX STARTING ===");
        ApplyAllFixes();
    }
    
    [ContextMenu("Apply All Camera Fixes")]
    public void ApplyAllFixes()
    {
        if (fixMethod1_CreateLookTarget)
        {
            CreateHeadTarget();
        }
        
        if (fixMethod2_AdjustComposer)
        {
            AdjustComposerSettings();
        }
        
        if (fixMethod3_ForceUpdate)
        {
            ForceUpdateAllCameras();
        }
        
        // Final check
        StartCoroutine(VerifyFix());
    }
    
    void CreateHeadTarget()
    {
        Debug.Log("\n=== METHOD 1: Creating Head Target ===");
        
        // Remove old targets
        foreach (Transform child in transform)
        {
            if (child.name.Contains("HeadTarget") || child.name.Contains("CameraLookTarget"))
            {
                DestroyImmediate(child.gameObject);
            }
        }
        
        // Create new target
        headTarget = new GameObject("HeadTarget");
        headTarget.transform.SetParent(transform);
        headTarget.transform.localPosition = new Vector3(0, headHeight, 0);
        headTarget.transform.localRotation = Quaternion.identity;
        
        Debug.Log($"✅ Created HeadTarget at height {headHeight}");
        
        // Add visual marker
        if (createVisualMarker && Application.isPlaying)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(headTarget.transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = Vector3.one * 0.2f;
            
            // Make it semi-transparent red
            var renderer = sphere.GetComponent<Renderer>();
            renderer.material.color = new Color(1, 0, 0, 0.5f);
            
            // Remove collider
            Destroy(sphere.GetComponent<Collider>());
            
            Debug.Log("✅ Added red sphere marker at head position");
        }
    }
    
    void AdjustComposerSettings()
    {
        Debug.Log("\n=== METHOD 2: Adjusting Composer Settings ===");
        
        CinemachineFreeLook[] freeLooks = FindObjectsOfType<CinemachineFreeLook>();
        
        foreach (var freeLook in freeLooks)
        {
            // Check if this camera is for our player
            if (freeLook.Follow == transform || freeLook.LookAt == transform || 
                (freeLook.Follow && freeLook.Follow.IsChildOf(transform)))
            {
                Debug.Log($"Adjusting FreeLook camera: {freeLook.name}");
                
                // Adjust each rig
                for (int i = 0; i < 3; i++)
                {
                    var rig = freeLook.GetRig(i);
                    if (rig != null)
                    {
                        var composer = rig.GetCinemachineComponent<CinemachineComposer>();
                        if (composer != null)
                        {
                            // Set tracked object offset to head height
                            composer.m_TrackedObjectOffset = new Vector3(0, headHeight, 0);
                            
                            string rigName = i == 0 ? "Top" : i == 1 ? "Middle" : "Bottom";
                            Debug.Log($"  ✅ {rigName} Rig: Set Tracked Object Offset Y to {headHeight}");
                        }
                    }
                }
            }
        }
        
        // Also check regular virtual cameras
        CinemachineVirtualCamera[] vcams = FindObjectsOfType<CinemachineVirtualCamera>();
        foreach (var vcam in vcams)
        {
            if (vcam.Follow == transform || (vcam.Follow && vcam.Follow.IsChildOf(transform)))
            {
                var composer = vcam.GetCinemachineComponent<CinemachineComposer>();
                if (composer != null)
                {
                    composer.m_TrackedObjectOffset = new Vector3(0, headHeight, 0);
                    Debug.Log($"  ✅ {vcam.name}: Set Tracked Object Offset Y to {headHeight}");
                }
            }
        }
    }
    
    void ForceUpdateAllCameras()
    {
        Debug.Log("\n=== METHOD 3: Force Update All Cameras ===");
        
        CinemachineVirtualCameraBase[] allVcams = FindObjectsOfType<CinemachineVirtualCameraBase>();
        
        foreach (var vcam in allVcams)
        {
            // Check if this camera relates to our player
            if (vcam.Follow == transform || (vcam.Follow && vcam.Follow.IsChildOf(transform)))
            {
                Debug.Log($"Updating camera: {vcam.name}");
                
                // Set LookAt to head target if we have one
                if (headTarget != null)
                {
                    vcam.LookAt = headTarget.transform;
                    Debug.Log($"  ✅ Set LookAt to HeadTarget");
                }
                
                // Force camera update
                vcam.PreviousStateIsValid = false;
                
                // If it's the active camera, give it priority
                if (CinemachineCore.Instance.IsLive(vcam))
                {
                    vcam.Priority = 100;
                    Debug.Log($"  ✅ Set as high priority (active camera)");
                }
            }
        }
        
        // Force Cinemachine to update
        var brain = Camera.main.GetComponent<CinemachineBrain>();
        if (brain != null)
        {
            brain.ManualUpdate();
            Debug.Log("✅ Forced Cinemachine Brain update");
        }
    }
    
    IEnumerator VerifyFix()
    {
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("\n=== VERIFYING FIX ===");
        
        // Find active camera
        CinemachineVirtualCameraBase activeVcam = null;
        CinemachineVirtualCameraBase[] vcams = FindObjectsOfType<CinemachineVirtualCameraBase>();
        
        foreach (var vcam in vcams)
        {
            if (CinemachineCore.Instance.IsLive(vcam) && 
                (vcam.Follow == transform || (vcam.Follow && vcam.Follow.IsChildOf(transform))))
            {
                activeVcam = vcam;
                break;
            }
        }
        
        if (activeVcam != null)
        {
            Debug.Log($"Active camera: {activeVcam.name}");
            
            if (activeVcam.LookAt != null)
            {
                float lookHeight = activeVcam.LookAt.position.y - transform.position.y;
                Debug.Log($"LookAt height: {lookHeight:F2} units above player base");
                
                if (lookHeight > 1.4f)
                {
                    Debug.Log("✅ SUCCESS! Camera should now be looking at head level!");
                }
                else
                {
                    Debug.LogError("❌ Camera is still too low. Try increasing headHeight value.");
                }
            }
            else
            {
                // Check composer offset
                if (activeVcam is CinemachineFreeLook freeLook)
                {
                    var composer = freeLook.GetRig(1).GetCinemachineComponent<CinemachineComposer>();
                    if (composer != null && composer.m_TrackedObjectOffset.y > 1.4f)
                    {
                        Debug.Log("✅ SUCCESS! Using Tracked Object Offset for head height!");
                    }
                }
            }
        }
    }
    
    void OnDrawGizmos()
    {
        // Draw head height indicator
        Gizmos.color = Color.green;
        Vector3 headPos = transform.position + Vector3.up * headHeight;
        Gizmos.DrawWireSphere(headPos, 0.15f);
        Gizmos.DrawLine(transform.position, headPos);
    }
} 
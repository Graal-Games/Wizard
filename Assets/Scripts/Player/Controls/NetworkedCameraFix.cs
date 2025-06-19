using UnityEngine;
using Cinemachine;
using System.Collections;
using Unity.Netcode;

/// <summary>
/// Camera fix designed for networked spawned players
/// </summary>
public class NetworkedCameraFix : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float headHeight = 1.6f;
    [SerializeField] private bool createRedDebugSphere = false;
    
    private Transform headTarget;
    private GameObject debugSphere;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsOwner || !NetworkManager.Singleton.IsClient)
        {
            StartCoroutine(DelayedSetup());
        }
    }
    
    IEnumerator DelayedSetup()
    {
        // Wait a frame for everything to initialize
        yield return new WaitForEndOfFrame();
        
        Debug.Log($"[NetworkedCameraFix] Setting up camera for {gameObject.name}");
        
        // Create head target
        CreateHeadTarget();
        
        // Wait another frame
        yield return new WaitForEndOfFrame();
        
        // Find and update cameras
        UpdateAllCameras();
        
        // Create visual debug sphere
        if (createRedDebugSphere && Application.isPlaying)
        {
            CreateDebugSphere();
        }
    }
    
    void CreateHeadTarget()
    {
        // Clean up any existing targets
        foreach (Transform child in transform)
        {
            if (child.name.Contains("HeadTarget") || child.name.Contains("CameraLookTarget"))
            {
                Destroy(child.gameObject);
            }
        }
        
        // Create new target
        GameObject target = new GameObject("HeadTarget_Fixed");
        target.transform.SetParent(transform);
        target.transform.localPosition = new Vector3(0, headHeight, 0);
        target.transform.localRotation = Quaternion.identity;
        headTarget = target.transform;
        
        Debug.Log($"[NetworkedCameraFix] Created HeadTarget_Fixed at height {headHeight}");
    }
    
    void UpdateAllCameras()
    {
        // Find ALL Cinemachine cameras in the scene
        CinemachineVirtualCameraBase[] allCameras = FindObjectsOfType<CinemachineVirtualCameraBase>();
        
        Debug.Log($"[NetworkedCameraFix] Found {allCameras.Length} Cinemachine cameras");
        
        foreach (var cam in allCameras)
        {
            // Check if this camera is following this player (or its parent transform)
            if (cam.Follow == transform || 
                (cam.Follow != null && cam.Follow.IsChildOf(transform)) ||
                cam.LookAt == transform ||
                (cam.LookAt != null && cam.LookAt.IsChildOf(transform)))
            {
                Debug.Log($"[NetworkedCameraFix] Updating camera: {cam.name}");
                
                // Set both Follow and LookAt
                cam.Follow = transform;
                cam.LookAt = headTarget;
                
                // For FreeLook cameras, also set the binding mode
                if (cam is CinemachineFreeLook freeLook)
                {
                    freeLook.m_BindingMode = CinemachineTransposer.BindingMode.WorldSpace;
                    
                    // Also update the composer offset for each rig
                    for (int i = 0; i < 3; i++)
                    {
                        var rig = freeLook.GetRig(i);
                        if (rig != null)
                        {
                            var composer = rig.GetCinemachineComponent<CinemachineComposer>();
                            if (composer != null)
                            {
                                // Reset composer offset since we're using LookAt target
                                composer.m_TrackedObjectOffset = Vector3.zero;
                            }
                        }
                    }
                    
                    Debug.Log($"[NetworkedCameraFix] ✅ Successfully updated FreeLook camera!");
                }
            }
        }
        
        // Force Cinemachine to update
        if (CinemachineCore.Instance != null)
        {
            CinemachineCore.Instance.OnTargetObjectWarped(transform, transform.position - transform.position);
        }
    }
    
    void CreateDebugSphere()
    {
        if (headTarget == null) return;
        
        // Create visible sphere at head position
        debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        debugSphere.name = "HEAD_TARGET_DEBUG_SPHERE";
        debugSphere.transform.SetParent(headTarget);
        debugSphere.transform.localPosition = Vector3.zero;
        debugSphere.transform.localScale = Vector3.one * 0.3f;
        
        // Make it red and visible
        var renderer = debugSphere.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.red;
            // Make sure it renders on top
            renderer.material.renderQueue = 5000;
        }
        
        // Remove collider
        Destroy(debugSphere.GetComponent<Collider>());
        
        Debug.Log("[NetworkedCameraFix] ✅ Created RED debug sphere at head position!");
    }
    
    void OnDrawGizmos()
    {
        // Draw cyan wireframe sphere at head position
        Gizmos.color = Color.cyan;
        Vector3 headPos = transform.position + Vector3.up * headHeight;
        Gizmos.DrawWireSphere(headPos, 0.2f);
        
        // Draw "LOOK HERE" text
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(headPos + Vector3.up * 0.3f, "CAMERA SHOULD LOOK HERE");
        #endif
    }
    
    void OnDestroy()
    {
        if (debugSphere != null)
        {
            Destroy(debugSphere);
        }
    }
} 
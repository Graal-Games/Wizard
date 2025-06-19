using UnityEngine;
using Cinemachine;

/// <summary>
/// Simplified camera fix - just creates a head target and updates cameras
/// </summary>
[ExecuteInEditMode]
public class QuickCameraFix : MonoBehaviour
{
    [Header("Settings")]
    public float headHeight = 1.6f;
    public bool showDebugSphere = true;
    
    private Transform headTarget;
    
    void OnEnable()
    {
        CreateHeadTarget();
        UpdateCameras();
    }
    
    void Start()
    {
        if (Application.isPlaying)
        {
            CreateHeadTarget();
            UpdateCameras();
        }
    }
    
    void CreateHeadTarget()
    {
        // Check if already exists
        headTarget = transform.Find("HeadLookTarget");
        
        if (headTarget == null)
        {
            GameObject target = new GameObject("HeadLookTarget");
            target.transform.SetParent(transform);
            target.transform.localPosition = new Vector3(0, headHeight, 0);
            target.transform.localRotation = Quaternion.identity;
            headTarget = target.transform;
            
            Debug.Log($"[QuickCameraFix] Created HeadLookTarget at height {headHeight}");
        }
        else
        {
            // Update position
            headTarget.localPosition = new Vector3(0, headHeight, 0);
            Debug.Log($"[QuickCameraFix] Updated HeadLookTarget position to height {headHeight}");
        }
    }
    
    void UpdateCameras()
    {
        // Find all Cinemachine cameras
        var allCameras = FindObjectsOfType<CinemachineVirtualCameraBase>();
        int updated = 0;
        
        foreach (var cam in allCameras)
        {
            // Check if this camera is following the player
            if (cam.Follow == transform)
            {
                // Update LookAt
                if (headTarget != null && cam.LookAt != headTarget)
                {
                    cam.LookAt = headTarget;
                    updated++;
                    Debug.Log($"[QuickCameraFix] Updated {cam.name} to look at HeadLookTarget");
                }
                
                // For FreeLook, also update composer
                if (cam is CinemachineFreeLook freeLook)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var rig = freeLook.GetRig(i);
                        var composer = rig?.GetCinemachineComponent<CinemachineComposer>();
                        if (composer != null)
                        {
                            composer.m_TrackedObjectOffset.y = headHeight;
                        }
                    }
                    Debug.Log($"[QuickCameraFix] Also updated FreeLook composer offsets");
                }
            }
        }
        
        if (updated > 0)
        {
            Debug.Log($"[QuickCameraFix] SUCCESS! Updated {updated} camera(s)");
        }
        else if (allCameras.Length == 0)
        {
            Debug.LogError("[QuickCameraFix] No Cinemachine cameras found in scene!");
        }
        else
        {
            Debug.LogWarning("[QuickCameraFix] No cameras were following this player. Make sure your Cinemachine camera's Follow is set to the player.");
        }
    }
    
    void OnDrawGizmos()
    {
        if (showDebugSphere)
        {
            // Draw sphere at head position
            Gizmos.color = new Color(0, 1, 1, 0.5f); // Cyan
            Vector3 headPos = transform.position + Vector3.up * headHeight;
            Gizmos.DrawWireSphere(headPos, 0.15f);
            
            // Draw line from feet to head
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, headPos);
            
            // Label
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(headPos + Vector3.up * 0.2f, "Camera Look Target");
            #endif
        }
    }
    
    void OnValidate()
    {
        // Update in editor when values change
        if (!Application.isPlaying)
        {
            CreateHeadTarget();
        }
    }
} 
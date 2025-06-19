using UnityEngine;
using System;

/// <summary>
/// Alternative zoom script that uses Field of View (FOV) instead of orbit radius.
/// This is a fallback if the orbit radius approach doesn't work.
/// </summary>
public class AlternativeZoom : MonoBehaviour
{
    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 3f;
    [SerializeField] private float minFOV = 20f; // Zoomed in
    [SerializeField] private float maxFOV = 60f; // Zoomed out
    [SerializeField] private bool enableDebugLogs = true;
    
    private Component virtualCamera;
    private Camera unityCamera;
    private float currentZoom = 0.5f;
    
    void Start()
    {
        if (enableDebugLogs)
            Debug.Log("[AlternativeZoom] Starting FOV-based zoom setup...");
            
        FindCameras();
    }
    
    void Update()
    {
        if (virtualCamera == null && unityCamera == null)
        {
            if (Time.frameCount % 60 == 0)
                FindCameras();
            return;
        }
        
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            // Update zoom level
            currentZoom = Mathf.Clamp01(currentZoom - scrollInput * zoomSpeed * 0.1f);
            
            // Apply zoom via FOV
            ApplyFOVZoom();
        }
    }
    
    private void FindCameras()
    {
        // Find the main camera
        unityCamera = Camera.main;
        if (unityCamera == null)
        {
            unityCamera = FindObjectOfType<Camera>();
        }
        
        // Try to find virtual camera
        string[] vcamTypes = {
            "Cinemachine.CinemachineVirtualCameraBase, Cinemachine",
            "Cinemachine.CinemachineFreeLook, Cinemachine",
            "Cinemachine.CinemachineVirtualCamera, Cinemachine"
        };
        
        foreach (string typeName in vcamTypes)
        {
            Type vcamType = Type.GetType(typeName);
            if (vcamType != null)
            {
                var vcams = FindObjectsOfType(vcamType);
                if (vcams.Length > 0)
                {
                    virtualCamera = vcams[0] as Component;
                    if (enableDebugLogs)
                        Debug.Log($"[AlternativeZoom] Found virtual camera: {virtualCamera.name}");
                    break;
                }
            }
        }
        
        if (unityCamera != null && enableDebugLogs)
            Debug.Log($"[AlternativeZoom] Found Unity camera: {unityCamera.name}");
    }
    
    private void ApplyFOVZoom()
    {
        float targetFOV = Mathf.Lerp(maxFOV, minFOV, currentZoom);
        
        // Try to set FOV on virtual camera first
        if (virtualCamera != null)
        {
            try
            {
                var fovProperty = virtualCamera.GetType().GetProperty("m_Lens");
                if (fovProperty != null)
                {
                    var lens = fovProperty.GetValue(virtualCamera);
                    if (lens != null)
                    {
                        var fovField = lens.GetType().GetField("FieldOfView");
                        if (fovField != null)
                        {
                            fovField.SetValue(lens, targetFOV);
                            fovProperty.SetValue(virtualCamera, lens);
                            
                            if (enableDebugLogs)
                                Debug.Log($"[AlternativeZoom] Set virtual camera FOV to: {targetFOV:F1}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (enableDebugLogs)
                    Debug.LogError($"[AlternativeZoom] Error setting virtual camera FOV: {e.Message}");
            }
        }
        
        // Also set on Unity camera as fallback
        if (unityCamera != null)
        {
            unityCamera.fieldOfView = targetFOV;
            if (enableDebugLogs)
                Debug.Log($"[AlternativeZoom] Set Unity camera FOV to: {targetFOV:F1}");
        }
    }
} 
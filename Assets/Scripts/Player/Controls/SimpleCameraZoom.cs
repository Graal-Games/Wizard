using UnityEngine;
using System;
using System.Reflection;

/// <summary>
/// Simple camera zoom script for Cinemachine FreeLook cameras.
/// Attach this to your player GameObject to enable mouse wheel zoom.
/// </summary>
public class SimpleCameraZoom : MonoBehaviour
{
    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 3f;
    [SerializeField] private float minZoom = 2f;
    [SerializeField] private float maxZoom = 10f;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private Component freeLookCamera;
    private float currentZoom = 0.5f; // 0 = min, 1 = max
    private bool hasLoggedNoCameraWarning = false;
    
    void Start()
    {
        if (enableDebugLogs)
            Debug.Log("[SimpleCameraZoom] Starting camera zoom setup...");
            
        FindFreeLookCamera();
        
        if (freeLookCamera != null && enableDebugLogs)
        {
            Debug.Log($"[SimpleCameraZoom] Successfully found FreeLook camera: {freeLookCamera.name}");
            LogCameraDetails();
        }
    }
    
    void Update()
    {
        // Always try to find camera if we don't have one
        if (freeLookCamera == null)
        {
            if (Time.frameCount % 60 == 0) // Try every second
            {
                FindFreeLookCamera();
            }
            
            if (!hasLoggedNoCameraWarning && enableDebugLogs)
            {
                Debug.LogWarning("[SimpleCameraZoom] No FreeLook camera found. Make sure you have a Cinemachine FreeLook camera in the scene.");
                hasLoggedNoCameraWarning = true;
            }
            return;
        }
        
        // Check for scroll input
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[SimpleCameraZoom] Scroll detected: {scrollInput}");
            }
            
            // Update zoom level (inverted so scroll up zooms in)
            currentZoom = Mathf.Clamp01(currentZoom - scrollInput * zoomSpeed * 0.1f);
            
            // Apply zoom
            ApplyZoom();
        }
        
        // Debug key to log current state
        if (enableDebugLogs && Input.GetKeyDown(KeyCode.Z))
        {
            LogCameraDetails();
        }
    }
    
    private void FindFreeLookCamera()
    {
        // Try multiple ways to find the FreeLook camera
        string[] possibleTypeNames = new string[]
        {
            "Cinemachine.CinemachineFreeLook, Cinemachine",
            "Cinemachine.CinemachineFreeLook, Unity.Cinemachine",
            "Cinemachine.CinemachineFreeLook, com.unity.cinemachine",
            "CinemachineFreeLook, Cinemachine",
            "CinemachineFreeLook, Assembly-CSharp"
        };
        
        foreach (string typeName in possibleTypeNames)
        {
            Type freeLookType = Type.GetType(typeName);
            if (freeLookType != null)
            {
                if (enableDebugLogs)
                    Debug.Log($"[SimpleCameraZoom] Found type with: {typeName}");
                
                var cameras = FindObjectsOfType(freeLookType);
                if (cameras.Length > 0)
                {
                    freeLookCamera = cameras[0] as Component;
                    if (enableDebugLogs)
                        Debug.Log($"[SimpleCameraZoom] Found {cameras.Length} FreeLook camera(s)");
                    return;
                }
            }
        }
        
        // If still not found, try a more generic approach
        Component[] allComponents = FindObjectsOfType<Component>();
        foreach (var comp in allComponents)
        {
            if (comp != null && comp.GetType().Name.Contains("FreeLook"))
            {
                freeLookCamera = comp;
                if (enableDebugLogs)
                    Debug.Log($"[SimpleCameraZoom] Found FreeLook camera by name search: {comp.name}");
                return;
            }
        }
        
        if (enableDebugLogs)
            Debug.LogWarning("[SimpleCameraZoom] Could not find any FreeLook camera with any method!");
    }
    
    private void ApplyZoom()
    {
        if (freeLookCamera == null) return;
        
        try
        {
            // First, let's find out what members this camera has
            if (enableDebugLogs && Input.GetKey(KeyCode.LeftShift))
            {
                LogAllMembers();
            }
            
            // Try to get orbits as a FIELD first (not property)
            FieldInfo orbitsField = freeLookCamera.GetType().GetField("m_Orbits", 
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            
            object orbits = null;
            
            if (orbitsField != null)
            {
                orbits = orbitsField.GetValue(freeLookCamera);
                if (enableDebugLogs)
                    Debug.Log("[SimpleCameraZoom] Found m_Orbits as a FIELD");
            }
            else
            {
                // Try as a property
                PropertyInfo orbitsProperty = freeLookCamera.GetType().GetProperty("m_Orbits",
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                
                if (orbitsProperty != null)
                {
                    orbits = orbitsProperty.GetValue(freeLookCamera);
                    if (enableDebugLogs)
                        Debug.Log("[SimpleCameraZoom] Found m_Orbits as a PROPERTY");
                }
                else
                {
                    // Try alternative names
                    string[] possibleNames = { "Orbits", "m_Orbits", "_orbits", "orbits" };
                    foreach (string name in possibleNames)
                    {
                        orbitsField = freeLookCamera.GetType().GetField(name,
                            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                        if (orbitsField != null)
                        {
                            orbits = orbitsField.GetValue(freeLookCamera);
                            if (enableDebugLogs)
                                Debug.Log($"[SimpleCameraZoom] Found orbits as field '{name}'");
                            break;
                        }
                        
                        orbitsProperty = freeLookCamera.GetType().GetProperty(name,
                            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                        if (orbitsProperty != null)
                        {
                            orbits = orbitsProperty.GetValue(freeLookCamera);
                            if (enableDebugLogs)
                                Debug.Log($"[SimpleCameraZoom] Found orbits as property '{name}'");
                            break;
                        }
                    }
                }
            }
            
            if (orbits == null)
            {
                if (enableDebugLogs)
                {
                    Debug.LogError("[SimpleCameraZoom] Could not find orbits field/property! Press Shift+Z to see all members.");
                }
                return;
            }
            
            if (!orbits.GetType().IsArray)
            {
                if (enableDebugLogs)
                    Debug.LogError($"[SimpleCameraZoom] Orbits is not an array! Type: {orbits.GetType()}");
                return;
            }
            
            Array orbitsArray = (Array)orbits;
            
            // Update each rig's radius
            for (int i = 0; i < orbitsArray.Length && i < 3; i++)
            {
                object orbit = orbitsArray.GetValue(i);
                if (orbit != null)
                {
                    // Try to find the radius field
                    FieldInfo radiusField = orbit.GetType().GetField("m_Radius",
                        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    
                    if (radiusField == null)
                    {
                        // Try alternative names
                        string[] radiusNames = { "Radius", "radius", "_radius" };
                        foreach (string name in radiusNames)
                        {
                            radiusField = orbit.GetType().GetField(name,
                                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                            if (radiusField != null) break;
                        }
                    }
                    
                    if (radiusField != null)
                    {
                        float newRadius = Mathf.Lerp(minZoom, maxZoom, currentZoom);
                        radiusField.SetValue(orbit, newRadius);
                        
                        if (enableDebugLogs)
                            Debug.Log($"[SimpleCameraZoom] Rig {i} radius set to: {newRadius:F2}");
                    }
                    else
                    {
                        if (enableDebugLogs)
                            Debug.LogError($"[SimpleCameraZoom] Could not find radius field in orbit {i}");
                    }
                }
            }
            
                                // Important: For structs in arrays, we need to set the entire orbit back
                    for (int i = 0; i < orbitsArray.Length && i < 3; i++)
                    {
                        var orbit = orbitsArray.GetValue(i);
                        if (orbit != null)
                        {
                            // Set the modified orbit back into the array
                            orbitsArray.SetValue(orbit, i);
                        }
                    }
                    
                    // Apply changes back if we got orbits from a field
                    if (orbitsField != null)
                    {
                        orbitsField.SetValue(freeLookCamera, orbitsArray);
                    }
                    else
                    {
                        // Must be a property
                        PropertyInfo orbitsProperty = freeLookCamera.GetType().GetProperty("m_Orbits",
                            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                        if (orbitsProperty != null && orbitsProperty.CanWrite)
                        {
                            orbitsProperty.SetValue(freeLookCamera, orbitsArray);
                        }
                    }
            
            if (enableDebugLogs)
                Debug.Log($"[SimpleCameraZoom] Zoom applied! Current level: {currentZoom:F2} (0=close, 1=far)");
                
            // Try to force the camera to update
            try
            {
                // Look for an InvalidateCache method
                var invalidateMethod = freeLookCamera.GetType().GetMethod("InvalidateCache",
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (invalidateMethod != null)
                {
                    invalidateMethod.Invoke(freeLookCamera, null);
                    if (enableDebugLogs) Debug.Log("[SimpleCameraZoom] Called InvalidateCache");
                }
                
                // Try OnValidate
                var onValidateMethod = freeLookCamera.GetType().GetMethod("OnValidate",
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (onValidateMethod != null)
                {
                    onValidateMethod.Invoke(freeLookCamera, null);
                    if (enableDebugLogs) Debug.Log("[SimpleCameraZoom] Called OnValidate");
                }
                
                // Force component update
                freeLookCamera.SendMessage("OnValidate", SendMessageOptions.DontRequireReceiver);
            }
            catch (Exception ex)
            {
                if (enableDebugLogs) Debug.LogWarning($"[SimpleCameraZoom] Could not call update methods: {ex.Message}");
            }
        }
        catch (Exception e)
        {
            if (enableDebugLogs)
                Debug.LogError($"[SimpleCameraZoom] Error applying zoom: {e.Message}\n{e.StackTrace}");
        }
    }
    
    private void LogCameraDetails()
    {
        if (freeLookCamera == null)
        {
            Debug.Log("[SimpleCameraZoom] Camera Details: No camera found");
            return;
        }
        
        Debug.Log($"[SimpleCameraZoom] === Camera Details ===");
        Debug.Log($"  Camera Name: {freeLookCamera.name}");
        Debug.Log($"  Camera Type: {freeLookCamera.GetType().FullName}");
        Debug.Log($"  Current Zoom: {currentZoom:F2} (0=close, 1=far)");
        Debug.Log($"  Zoom Range: {minZoom} to {maxZoom}");
        
        // Try to log current radius values
        try
        {
            PropertyInfo orbitsProperty = freeLookCamera.GetType().GetProperty("m_Orbits");
            if (orbitsProperty != null)
            {
                object orbits = orbitsProperty.GetValue(freeLookCamera);
                if (orbits != null && orbits.GetType().IsArray)
                {
                    Array orbitsArray = (Array)orbits;
                    for (int i = 0; i < orbitsArray.Length && i < 3; i++)
                    {
                        object orbit = orbitsArray.GetValue(i);
                        if (orbit != null)
                        {
                            FieldInfo radiusField = orbit.GetType().GetField("m_Radius");
                            if (radiusField != null)
                            {
                                float radius = (float)radiusField.GetValue(orbit);
                                Debug.Log($"  Rig {i} current radius: {radius:F2}");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SimpleCameraZoom] Error reading camera details: {e.Message}");
        }
        
        Debug.Log("[SimpleCameraZoom] =====================");
    }
    
    void OnDrawGizmosSelected()
    {
        // Visual indicator of zoom range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minZoom);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxZoom);
    }
    
    private void LogAllMembers()
    {
        if (freeLookCamera == null) return;
        
        Debug.Log("[SimpleCameraZoom] === ALL CAMERA MEMBERS ===");
        Debug.Log($"Camera Type: {freeLookCamera.GetType().FullName}");
        
        // Log all fields
        Debug.Log("-- FIELDS --");
        FieldInfo[] fields = freeLookCamera.GetType().GetFields(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (field.Name.ToLower().Contains("orbit") || field.Name.ToLower().Contains("rig") || 
                field.Name.ToLower().Contains("radius"))
            {
                try
                {
                    object value = field.GetValue(freeLookCamera);
                    Debug.Log($"  {field.Name} ({field.FieldType.Name}): {value}");
                }
                catch
                {
                    Debug.Log($"  {field.Name} ({field.FieldType.Name}): <unable to read>");
                }
            }
        }
        
        // Log all properties
        Debug.Log("-- PROPERTIES --");
        PropertyInfo[] properties = freeLookCamera.GetType().GetProperties(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            if (prop.Name.ToLower().Contains("orbit") || prop.Name.ToLower().Contains("rig") || 
                prop.Name.ToLower().Contains("radius"))
            {
                try
                {
                    if (prop.CanRead)
                    {
                        object value = prop.GetValue(freeLookCamera);
                        Debug.Log($"  {prop.Name} ({prop.PropertyType.Name}): {value}");
                    }
                    else
                    {
                        Debug.Log($"  {prop.Name} ({prop.PropertyType.Name}): <write-only>");
                    }
                }
                catch
                {
                    Debug.Log($"  {prop.Name} ({prop.PropertyType.Name}): <unable to read>");
                }
            }
        }
        
        Debug.Log("[SimpleCameraZoom] =========================");
    }
} 
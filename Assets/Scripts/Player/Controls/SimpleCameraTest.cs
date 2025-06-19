using UnityEngine;

public class SimpleCameraTest : MonoBehaviour
{
    void Awake()
    {
        Debug.Log("=== SimpleCameraTest AWAKE ===");
        Debug.LogError("This is a test ERROR message (red)");
        Debug.LogWarning("This is a test WARNING message (yellow)");
    }
    
    void Start()
    {
        Debug.Log("=== SimpleCameraTest START ===");
        Debug.Log($"GameObject name: {gameObject.name}");
        Debug.Log($"Position: {transform.position}");
        
        // Check for cameras
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Debug.Log($"Main Camera found: {mainCam.name}");
        }
        else
        {
            Debug.LogError("NO MAIN CAMERA FOUND!");
        }
        
        // Count Cinemachine cameras
        var vcams = FindObjectsOfType<Cinemachine.CinemachineVirtualCameraBase>();
        Debug.Log($"Found {vcams.Length} Cinemachine virtual cameras");
    }
    
    void Update()
    {
        // Log every 60 frames (about once per second)
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"SimpleCameraTest is running... Frame: {Time.frameCount}");
        }
    }
} 
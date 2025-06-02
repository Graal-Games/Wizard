using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

public class ReassignCustomPassCamera : MonoBehaviour
{
    public CustomPassVolume customPassVolume; // Reference to the Custom Pass Volume

    void OnEnable()
    {
        // Subscribe to the sceneLoaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        // Unsubscribe from the sceneLoaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Find the camera in the new scene
        Camera newCamera = Camera.main; // Use Camera.main or find by tag/layer if needed

        if (newCamera != null && customPassVolume != null)
        {
            // Reassign the Target Camera in the Custom Pass Volume
            customPassVolume.targetCamera = newCamera;
            Debug.Log("Custom Pass Volume camera reassigned to: " + newCamera.name);
        }
        else
        {
            Debug.LogError("Camera or Custom Pass Volume not found!");
        }
    }
}
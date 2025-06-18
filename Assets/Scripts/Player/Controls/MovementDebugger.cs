using UnityEngine;

public class MovementDebugger : MonoBehaviour
{
    void Update()
    {
        // Debug input
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        
        if (h != 0 || v != 0)
        {
            Debug.Log($"Direct Input - Horizontal: {h}, Vertical: {v}");
        }
        
        if (Input.GetKeyDown(KeyCode.W)) Debug.Log("W key pressed");
        if (Input.GetKeyDown(KeyCode.A)) Debug.Log("A key pressed");
        if (Input.GetKeyDown(KeyCode.S)) Debug.Log("S key pressed");
        if (Input.GetKeyDown(KeyCode.D)) Debug.Log("D key pressed");
        if (Input.GetKeyDown(KeyCode.Space)) Debug.Log("Space key pressed");
        
        // Check mouse
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        
        if (mouseX != 0 || mouseY != 0)
        {
            Debug.Log($"Mouse - X: {mouseX}, Y: {mouseY}");
        }
        
        // Check GameInput
        if (GameObject.Find("GameInput") != null)
        {
            Debug.Log("GameInput GameObject found in scene");
        }
        else
        {
            Debug.LogWarning("GameInput GameObject NOT found in scene - you need to add it!");
        }
    }
} 
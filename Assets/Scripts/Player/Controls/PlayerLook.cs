using UnityEngine;
using Unity.Netcode;
using Singletons;

public class PlayerLook : Singleton<PlayerLook>
{
    float sensitivity = 1f; // Mouse sensitivity

    private Transform player; // Reference to player transform
    private float xRotation = 0f; // Current rotation around the x-axis

    private float yRotation = 0f;

    //private Vector3 offset = new Vector3 (5f, 5f, 0f); // The offset between the camera and the target

    void Start()
    {
        //player = this.transform.parent; // Set player reference
        // >> Cursor.lockState = CursorLockMode.Locked; // Hide cursor and lock it to the center of the screen
        // if (player != null)
        // {
        //     transform.SetParent(player);
        // }
    }

    void Update()
    {
        //if (!IsOwner) return;

        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;

        xRotation -= mouseY;
        yRotation += mouseX; 

        xRotation = Mathf.Clamp(xRotation, -90f, 25f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        if (player != null)
        {   
            player.Rotate(Vector3.up * mouseX);
            //transform.position = player.position + offset; // Set the camera's position to match the target's position plus the offset

        }
        
    }

    // Assign the camera to a player
    // Set the camera position and starting rotation
    public void GetPlayer(Transform playerReference)
    {
        player = playerReference;

        if (player != null)
        {
            transform.SetParent(player, true); //Vector3(0,2.25,-6.63000011)
            transform.localPosition = new Vector3(0f, 2.6f,-8.98f);
            //transform.localPosition = new Vector3(0f, 1.74f,-9f); // altered
            transform.localRotation = Quaternion.Euler(9.7f, 0f, 0f);
        }
    }
}
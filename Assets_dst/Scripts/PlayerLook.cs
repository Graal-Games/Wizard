using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    public float mouseSensitivity = 200f;
    public Transform playerBody;
    float xRotation = 0f;

    void Start()
    {
        // Lock the cursor to the center of the screen and hide it
        //Cursor.lockState = CursorLockMode.Locked;

        //transform.rotation = Quaternion.Euler(10.513f,0,0);
    }

    void Update()
    {
        // Get the horizontal and vertical mouse inputs
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Rotate the player horizontally based on the horizontal mouse input
        playerBody.Rotate(Vector3.up * mouseX);

        // Rotate the camera vertically based on the vertical mouse input
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

    }
}
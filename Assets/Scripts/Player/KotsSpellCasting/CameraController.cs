using System;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public PlayerController player;
    private float sensitivity = 200f;
    private float clampAngel = 85f; 

    private float verticalRotation;
    private float horizontalRotation;

    private void Start()
    {
        this.verticalRotation = this.transform.localEulerAngles.x;
        this.horizontalRotation = this.transform.eulerAngles.y;
    }

    private void Update()
    {
        Look();
        Debug.DrawRay(this.transform.position, this.transform.forward * 2, Color.red);


    }

    private void Look()
    {

        float mouseVertical = Input.GetAxis("Mouse Y");
        float mouseHorizontal = Input.GetAxis("Mouse X");

        this.verticalRotation += mouseVertical * this.sensitivity * Time.deltaTime;
        this.horizontalRotation += mouseHorizontal * this.sensitivity * Time.deltaTime;

        this.verticalRotation = Mathf.Clamp(this.verticalRotation, -this.clampAngel, this.clampAngel);

        this.transform.localRotation = Quaternion.Euler(this.verticalRotation, 0f, 0f);
        //this.player.transform.rotation = Quaternion.Euler(0f, this.horizontalRotation, 0f);
    }
}

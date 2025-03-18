using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class BeamBehavior : NetworkBehaviour
{
    float sensitivity = 100f;
    private float xRotation = 0f; // Current rotation around the x-axis
    private float yRotation = 0f;
    Transform myObject;
    Beam beam;

    Transform beamMovementRef;

    public bool hasHitShield = false;

    public bool isHitPlayer = false;

    void Start()
    {
        Debug.Log("PARENT: " + this.gameObject.transform.parent);
        //CharacterJoint joint = this.gameObject.GetComponent<CharacterJoint>();
        //GameObject player = GameObject.Find("Player");
        //joint.connectedBody = this.gameObject.transform.parent.GetComponent<Rigidbody>();
    }

    void Update()
    {
        //if (!IsOwner) return;
        // this.gameObject.transform.position = beamMovementRef.transform.position;
        // this.gameObject.transform.rotation = beamMovementRef.transform.rotation;
        
        
        //MoveWithPlayerLook();
    }

    private void MoveWithPlayerLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        xRotation -= mouseY;
        yRotation += mouseX; 

        xRotation = Mathf.Clamp(xRotation, -90f, 25f);

        //This works properly now:
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

   

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrierPoint : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // This should snap to the floor at the position it is at
    void Update()
    {
        //// Shoot a ray from the object's position along its axis (e.g., the forward axis)
        //Vector3 rayDirection = transform.forward; // Change 'forward' to the desired axis
        //Ray ray = new Ray(transform.position, rayDirection);
        //RaycastHit hit;

        //// Check if the ray hits something
        //if (Physics.Raycast(ray, out hit))
        //{
        //    // Log the name of the object hit by the ray
        //    Debug.Log("Ray hit: " + hit.collider.gameObject.name);

        //    if (hit.collider.gameObject.name == "Floor")
        //    {
        //        transform.position = new Vector3(
        //        hit.point.x,
        //        //floor.transform.position.y,
        //        hit.point.z
        //        );
        //    }
        //    // You can perform actions based on what was hit here
        //    // For example, you can apply damage, instantiate effects, etc.
        //}
    }
}

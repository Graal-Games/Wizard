using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BarrierAir : NetworkBehaviour
{
    //public float growSpeed = 200f;   // Speed of the growth
    //public Vector3 maxSize = new Vector3(200, 200, 200);          // Maximum size of the collider

    //private BoxCollider boxCollider;
    //private bool stopGrowing = false;

    //const float xSize = 100.0f;
    //const float ySize = 100.0f;
    //const float zSize = 100.0f;

    //void Start()
    //{
    //    // Initialize the collider (assuming it's a BoxCollider)
    //    boxCollider = GetComponent<BoxCollider>();
    //    Debug.Log("Box collider FOUND: " + boxCollider);
    //}

    //void Update()
    //{
    //    // Increase the collider size if it hasn't stopped growing
    //    if (stopGrowing == false)
    //    {
    //        Debug.Log("Growing Growing Growing: " + boxCollider);

    //        //Vector3 currentSize = boxCollider.size;
    //        //currentSize += Vector3.one * growSpeed * Time.deltaTime;
    //        boxCollider.size = new Vector3(xSize, ySize, zSize);

    //        //boxCollider.size.

    //        // Limit the size to the maxSize
    //        //currentSize = Vector3.Min(currentSize, maxSize);
    //        //boxCollider.size = currentSize;
    //    }
    //}

    //public override void Fire()
    //{
    //    Debug.LogFormat($"<color=orange> >>> AIR SPELL PUSH <<< </color>");
    //}
}

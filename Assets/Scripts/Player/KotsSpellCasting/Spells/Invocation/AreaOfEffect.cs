using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaOfEffect : MonoBehaviour
{

    // Reference to the parent GameObject
    private GameObject parentObject;


    void Start()
    {
        // Set the parent GameObject
        parentObject = transform.parent.gameObject;
    }
   

    // Forward the OnTriggerEnter event to the parent
    void OnTriggerEnter(Collider other)
    {
        // Check if the parent has a script with OnTriggerEnter method
        parentObject.SendMessage("OnTriggerEnter", other, SendMessageOptions.DontRequireReceiver);
    }

    // You can also forward OnTriggerExit and OnTriggerStay if needed
    void OnTriggerExit(Collider other)
    {
        parentObject.SendMessage("OnTriggerExit", other, SendMessageOptions.DontRequireReceiver);
    }
}

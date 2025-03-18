using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DRFloating : MonoBehaviour
{
    float floatSpeed = 2.5f; // Adjust this value to control the speed of floating
    float floatHeight; // Adjust this value to control the height of floating

    float floatHeightMin = 0.0003f; // Minimum floating height
    float floatHeightMax = 0.0006f; // Maximum floating height




    private Vector3 startPosition;

    private void Start()
    {
        floatHeight = Random.Range(floatHeightMin, floatHeightMax);

        startPosition = transform.position;
    }

    private void Update()
    {

        // Calculate the new Y position using a sine wave to create the floating effect
        float newY = this.gameObject.transform.position.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;

        // Update the object's position
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}

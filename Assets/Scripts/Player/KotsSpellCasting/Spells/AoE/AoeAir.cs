using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AoeAir : K_Spell
{
    [SerializeField] GameObject center;
    Vector3 directionToCenter;
    int multiplier = 2; // used to give an initial jolt to the center
    float pullForce = 25;
    bool firstPull = true;

    public void OnTriggerEnter(Collider other)
    {
        firstPull = true;
        // Check if the object has a Rigidbody
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Start attracting the Rigidbody to the center of the trigger
            StartCoroutine(Attract(rb));
            StartCoroutine(FirstPullElapsedTime());
        }
    }



    private IEnumerator Attract(Rigidbody rb)
    {
        // directionToCenter = transform.position - rb.position;
        //rb.AddForce(directionToCenter * SpellDataScriptableObject.pushForce * 32, ForceMode.Force); // Pushforce max is around 12 units. That value would have to change if player speed is changed.
        //Vector3 directionToCenter = transform.position - rb.position;
        //directionToCenter.Normalize();
        //pullForce = multiplier * SpellDataScriptableObject.pushForce;
        //rb.AddForce(center.transform.position * SpellDataScriptableObject.pushForce, ForceMode.Force);
        while (rb != null)
        {
            // Calculate the direction to the center of the trigger (current object's position)
            //Normalize the direction vector

            Vector3 directionToCenter = transform.position - rb.position;
            directionToCenter.Normalize();

            if (firstPull)
            {
                // Apply force to the Rigidbody to attract it to the center
                rb.AddForce(directionToCenter * 50, ForceMode.Force); // Pushforce max is around 12 units. That value would have to change if player speed is changed.
            }
            else
            {
                rb.AddForce(directionToCenter * SpellDataScriptableObject.pullForce, ForceMode.Force); // Pushforce max is around 12 units. That value would have to change if player speed is changed.
            }

            //// Exit the loop if the Rigidbody is close enough to the center (optional)
            if (Vector3.Distance(directionToCenter, rb.position) < 0.1f)
            {
                firstPull = false;
            }
            //    //pullForce = SpellDataScriptableObject.pushForce; // no multiplier
            //    //break;

            yield return null; // Wait for the next frame
        }
    }

    IEnumerator FirstPullElapsedTime()
    {
        yield return new WaitForSeconds(1);
        firstPull = false;
    }

    private void OnTriggerExit(Collider other)
    {
        // Optionally, stop the attraction when the object exits the trigger
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null)
        {
            StopAllCoroutines(); // Stop attracting the object
        }
    }


    public override void Fire()
    {

    }
       
}

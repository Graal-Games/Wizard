using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ProjectileClass : SpellsClass
{
    private Vector3 lastPosition;

    private void Start()
    {
        lastPosition = transform.position;

        rb = GetComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = false;
    }

    // Update is called once per frame
    public virtual void FixedUpdate()
    {
        //Vector3 forceDirection = transform.forward * 200f;
        //rb.AddForce(forceDirection, ForceMode.Force); // or ForceMode.Acceleration
        MoveAndHitRegRpc();
    }


    [Rpc(SendTo.Server)]
    void MoveAndHitRegRpc()
    {
        Vector3 currentPosition = transform.position;

        Vector3 forceDirection = transform.forward * 200f;
        rb.AddForce(forceDirection, ForceMode.Force); // or ForceMode.Acceleration

        RaycastHit hit;

        if (Physics.SphereCast(currentPosition, 0.2f, lastPosition - currentPosition, out hit, Vector3.Distance(currentPosition, lastPosition)))
        {
            // Debug.Log($"SphereCast hit: {hit.collider.name}");

            // Example: check for player hit
            if (hit.collider.CompareTag("Player"))
            {
                //Debug.LogFormat($"<color=orange>PLAYER DETECTED</color>");

                PlayerIsHit(hit.collider.gameObject);
                // Optionally destroy or deactivate the projectile here
            }
        }

        lastPosition = currentPosition; // Update lastPosition to the current position after the movement
    }

}

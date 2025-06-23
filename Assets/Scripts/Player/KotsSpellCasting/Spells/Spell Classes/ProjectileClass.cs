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

    public override void OnNetworkSpawn()
    {         
        base.OnNetworkSpawn();
        rb = GetComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = false;
    }

    // Update is called once per frame
    public virtual void FixedUpdate()
    {
        MoveAndHitRegRpc();
    }


    [Rpc(SendTo.Server)]
    void MoveAndHitRegRpc()
    {
        Vector3 currentPosition = transform.position;

        Vector3 forceDirection = transform.forward * SpellDataScriptableObject.moveSpeed;
        rb.AddForce(forceDirection, ForceMode.Force); // or ForceMode.Acceleration

        RaycastHit hit;

        // For a unit sphere mesh (diameter 1, radius 0.5)
        // The following can be made only if the GameObject is a uniformly scaled sphere
        float radius = 0.5f * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);

        if (Physics.SphereCast(currentPosition, radius, lastPosition - currentPosition, out hit, Vector3.Distance(currentPosition, lastPosition)))
        {
            // If player has active shield, handle the shield interaction and don't process the player hit
            if (hit.collider.CompareTag("ActiveShield"))
            {
                if (HandleIfPlayerHasActiveShield(hit.collider.gameObject) == false)
                {
                    // Check for player hit
                    if (hit.collider.CompareTag("Player"))
                    {

                        // If player does not have active shield, handle the player hit
                        PlayerIsHit(hit.collider.gameObject);
                    }
                }
            } else
            {
                // Check for player hit
                if (hit.collider.CompareTag("Player"))
                {

                    // If player does not have active shield, handle the player hit
                    PlayerIsHit(hit.collider.gameObject);
                }
            }

            // Check if the target is a spell instead 
            if (hit.collider.CompareTag("Spell"))
            {
                //Handle the spell to spell interaction
                HandleSpellToSpellInteractions(hit.collider.gameObject);
            }

            if (hit.collider.gameObject.layer == 7)
            {
                if (IsSpawned)
                {
                    Debug.LogFormat("<color=orange> >>> PROJECTILE DESTROY BY >>> (" + hit.collider.name + ")</color>");
                    DestroySpellRpc();
                }
            }
        }

        lastPosition = currentPosition; // Update lastPosition to the current position after the movement
    }

    void HandleSpecificSpellToSpellInteractions()
    {
        // 
    }
}

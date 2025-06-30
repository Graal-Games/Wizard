using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ProjectileClass : SpellsClass
{
    private Vector3 lastPosition;

    List<Rigidbody> pullSpellsList = new List<Rigidbody>();
    List<Rigidbody> pushSpellsList = new List<Rigidbody>();

    Vector3 pushDirection; // Adjust the direction of the force

    bool canDestroy = false;

    public bool CanDestroy
    {
        get { return canDestroy; }
        set { canDestroy = value; }

    }



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

        pushDirection = transform.forward;
    }

    // Update is called once per frame
    public virtual void FixedUpdate()
    {
        MoveAndHitRegRpc();

        HandlePushback();

        if (CanDestroy)
        {
            StartCoroutine(DelayedDestruction());
        }
    }

    IEnumerator DelayedDestruction()
    {
        yield return new WaitForSeconds(0.3f); // The value here seems to be working for now. Might need to revise it later.
        DestroySpell(gameObject);
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
            HandleAllInteractions(hit.collider);
            //// If player has active shield, handle the shield interaction and don't process the player hit
            //if (hit.collider.CompareTag("ActiveShield"))
            //{
            //    if (HandleIfPlayerHasActiveShield(hit.collider.gameObject) == false)
            //    {
            //        // Check for player hit
            //        if (hit.collider.CompareTag("Player"))
            //        {

            //            // If player does not have active shield, handle the player hit
            //            PlayerIsHit(hit.collider.gameObject);
            //        }
            //    }
            //} else
            //{
            //    // Check for player hit
            //    if (hit.collider.CompareTag("Player"))
            //    {

            //        // If player does not have active shield, handle the player hit
            //        PlayerIsHit(hit.collider.gameObject);
            //    }
            //}

            //// Check if the target is a spell instead 
            //if (hit.collider.CompareTag("Spell"))
            //{
            //    //Handle the spell to spell interaction
            //    HandleSpellToSpellInteractions(hit.collider.gameObject);
            //}
            if (gameObject.GetComponent<ISpell>().SpellName.Contains("Projectile_Air"))
            {
                ApplyPushbackToTarget(hit.collider.gameObject);
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

    void HandlePushback()
    {
        if (pullSpellsList.Count > 0) // Need to add this to the player behaviour script because this will be destroyed too fast and cannot take into account defensive spells
        {
            // Apply force to all the rigidbodies
            foreach (Rigidbody rb in pullSpellsList)
            {
                rb.AddForce(SpellDataScriptableObject.pushForce * pushDirection.normalized, ForceMode.Impulse);
                canDestroy = true; // If there is a need to destroy the gameObject after it applies force, use this variable.
            }
        }

        if (pushSpellsList.Count > 0)
        {
            foreach (Rigidbody rb2 in pushSpellsList)
            {
                Debug.LogFormat($"<color=blue>4 Push spell - RB: {rb2}</color>");

                //AddForce(rb2);
                rb2.AddForce(SpellDataScriptableObject.pushForce * pushDirection.normalized, ForceMode.Impulse);

                canDestroy = true;
            }
        }
    }

    public void ApplyPushbackToTarget(GameObject other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            //    if (SpellDataScriptableObject.pushForce > 0)
            //    {
            //        Debug.LogFormat("<color=green>2 Push spell</color>");
            //        // Cache the player's Rigidbody locally
            //        Rigidbody rb = other.GetComponent<Rigidbody>();

            //        // Add the rigidbody to the list of rigidbodies to be pushed
            //        if (rb != null)
            //        {
            //            //pullSpellsList.Add(rb);
            //        }
            //    }
            //    else
            //    {
            if (SpellDataScriptableObject.pushForce > 0)
            {
                Debug.LogFormat("<color=blue>2 Push spell</color>");

                // Cache the player's Rigidbody locally
                Rigidbody rb2 = other.GetComponent<Rigidbody>();

                Debug.LogFormat($"<color=blue>3 Push spell RB: {rb2}</color>");

                // Add the rigidbody to the list of rigidbodies to be pushed
                if (rb2 != null)
                {
                    pushSpellsList.Add(rb2);
                }
            }
            //}
        }
    }
}

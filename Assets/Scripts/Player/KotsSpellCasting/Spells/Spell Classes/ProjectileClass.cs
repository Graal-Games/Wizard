using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ProjectileClass : SpellsClass
{
    private Vector3 lastPosition;

    List<Rigidbody> pullSpellsList = new List<Rigidbody>();
    List<Rigidbody> pushSpellsList = new List<Rigidbody>();

    NetworkVariable<bool> _isExplodeOnHit = new NetworkVariable<bool>(false);

    Vector3 pushDirection; // Adjust the direction of the force

    protected NetworkVariable<bool> isMovement = new NetworkVariable<bool>(true);

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
    public override void FixedUpdate()
    {
        base.FixedUpdate();

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
    public virtual void MoveAndHitRegRpc()
    {
        Vector3 currentPosition = transform.position;

        if (_isExplodeOnHit.Value == false)
        {
            Vector3 forceDirection = transform.forward * SpellDataScriptableObject.moveSpeed;
            rb.AddForce(forceDirection, ForceMode.Force); // or ForceMode.Acceleration

        }

        RaycastHit hit;

        // For a unit sphere mesh (diameter 1, radius 0.5)
        // The following can be made only if the GameObject is a uniformly scaled sphere
        float radius = 0.5f * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);

        if (Physics.SphereCast(currentPosition, radius, lastPosition - currentPosition, out hit, Vector3.Distance(currentPosition, lastPosition)))
        {
            _isExplodeOnHit.Value = true;

            HandleAllInteractions(hit.collider);

            if (gameObject.GetComponent<ISpell>().SpellName.Contains("Projectile_Air"))
            {
                ApplyPushbackToTarget(hit.collider.gameObject);
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

        HandlePushbackRpc();

        if (IsHost) return;
        // {
        //     HandlePushbackRpc();
        if (pushSpellsList.Count > 0)
        {
            foreach (Rigidbody rb2 in pushSpellsList)
            {
                Debug.LogFormat($"<color=blue>4 Push spell - RB: {rb2}</color>");
                Debug.LogFormat($"Force applied - PUSH DIRECTION: {pushDirection.normalized} PUSH FORCE: {SpellDataScriptableObject.pushForce}");
                rb2.AddForce(SpellDataScriptableObject.pushForce * pushDirection.normalized, ForceMode.Impulse);
                canDestroy = true;
            }
        }

    }

    
    [Rpc(SendTo.Server)]
    void HandlePushbackRpc()
    {
        // Remove any null or destroyed rigidbodies
        pushSpellsList.RemoveAll(rb => rb == null || rb.gameObject == null);

        if (pushSpellsList.Count > 0)
        {
            foreach (Rigidbody rb2 in pushSpellsList)
            {
                // Try to get the NetworkObject and its owner
                NetworkObject netObj = rb2.GetComponent<NetworkObject>();
                if (netObj != null && !netObj.IsOwnedByServer)
                {
                    ApplyPushbackClientRpc(netObj.OwnerClientId, SpellDataScriptableObject.pushForce, pushDirection);                }
                else
                {
                    // Server-owned, apply force directly
                    Debug.LogFormat($"<color=blue>[RPC] 4 Push spell - RB: {rb2}</color>");
                    Debug.LogFormat($"[RPC] Force applied - PUSH DIRECTION: {pushDirection.normalized} PUSH FORCE: {SpellDataScriptableObject.pushForce}");
                    rb2.AddForce(SpellDataScriptableObject.pushForce * pushDirection.normalized, ForceMode.Impulse);
                    canDestroy = true;
                }
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    void ApplyPushbackClientRpc(ulong targetClientId, float pushForce, Vector3 direction, RpcParams rpcParams = default)
    {
        Debug.LogFormat($"<color=blue>targetClientId: {targetClientId}</color>");

        // Only run on the intended client
        //if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        // Find the local player object (assumes this script is not on the player, so you must find the correct Rigidbody)
        // This example assumes the player object is the owner of the Rigidbody in pushSpellsList
        foreach (Rigidbody rb in pushSpellsList)
        {
            if (rb.GetComponent<NetworkObject>() != null && rb.GetComponent<NetworkObject>().OwnerClientId == targetClientId)
            {
                Debug.LogFormat($"<color=blue>[ClientRpc] Push spell - RB: {rb}</color>");
                rb.AddForce(pushForce * direction.normalized, ForceMode.Impulse);
                canDestroy = true;
            }
        }
    }

    public void ApplyPushbackToTarget(GameObject other)
    {
        // if (other.gameObject.CompareTag("Player"))
        // {
        //     //    if (SpellDataScriptableObject.pushForce > 0)
        //     //    {
        //     //        Debug.LogFormat("<color=green>2 Push spell</color>");
        //     //        // Cache the player's Rigidbody locally
        //     //        Rigidbody rb = other.GetComponent<Rigidbody>();

        //     //        // Add the rigidbody to the list of rigidbodies to be pushed
        //     //        if (rb != null)
        //     //        {
        //     //            //pullSpellsList.Add(rb);
        //     //        }
        //     //    }
        //     //    else
        //     //    {
        //     if (SpellDataScriptableObject.pushForce > 0)
        //     {
        //         Debug.LogFormat("<color=blue>2 Push spell</color>");

        //         // Cache the player's Rigidbody locally
        //         Rigidbody rb2 = other.GetComponent<Rigidbody>();

        //         Debug.LogFormat($"<color=blue>3 Push spell RB: {rb2}</color>");

        //         // Add the rigidbody to the list of rigidbodies to be pushed
        //         if (rb2 != null)
        //         {
        //             pushSpellsList.Add(rb2);
        //         }
        //     }
        //     //}
        // }

        if (other.gameObject.CompareTag("Player"))
        {
            if (SpellDataScriptableObject.pushForce > 0)
            {
                Debug.LogFormat("<color=green>2 Push spell</color>");
                // Cache the player's Rigidbody locally
                Rigidbody rb = other.GetComponent<Rigidbody>();

                // Add the rigidbody to the list of rigidbodies to be pushed
                if (rb != null)
                {
                    pushSpellsList.Add(rb);
                }
            }
            else
            {
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
            }
        }
    }
}

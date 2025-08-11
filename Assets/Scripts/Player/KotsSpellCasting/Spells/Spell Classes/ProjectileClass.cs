using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ProjectileClass : SpellsClass
{
    private Vector3 lastPosition;

    List<Rigidbody> pullSpellsList = new List<Rigidbody>();
    List<Rigidbody> pushSpellsList = new List<Rigidbody>();

    protected NetworkVariable<bool> _isExplodeOnHit = new NetworkVariable<bool>(false);
    NetworkVariable<bool> hasCollided = new NetworkVariable<bool>(false);

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

        //Debug.LogFormat($"<color=blue>Current Position: {currentPosition}</color>");


        //if (_isExplodeOnHit.Value == false)
        //{
            Vector3 forceDirection = transform.forward * SpellDataScriptableObject.moveSpeed;
            rb.AddForce(forceDirection, ForceMode.Force); // or ForceMode.Acceleration
        //} 
        
        //if (_isExplodeOnHit.Value == true && rb.isKinematic == true)
        //{
            rb.isKinematic = false; // Stop the rigidbody from moving
            rb.useGravity = false; // Enable gravity if needed
        //}

        RaycastHit hit;

        // For a unit sphere mesh (diameter 1, radius 0.5)
        // The following can be made only if the GameObject is a uniformly scaled sphere
        float radius = 0.5f * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);

        Vector3 direction = currentPosition - lastPosition;
        float distance = direction.magnitude;

        
        // Throw a sphere cast IN FRONT OF the projectile gO
        // previously: the sphere cast was being thrown behind the projectile causing issues with collisions
        // The hit was being registered on exiting a collider instead of upon entering it
        if (Physics.SphereCast(lastPosition, radius, direction.normalized, out hit, distance))
        {
            Vector3 hitPosition = hit.point;

            //Debug.LogFormat($"<color=blue>Hit position: {hitPosition}</color>");

            //Debug.LogFormat($"<color=blue>hit: {hit.collider.gameObject.name}</color>");

            
            // If the projectile produces a secondary effect on collision, handle the spawning and prevent the spell from doing so again 
            if (SpellDataScriptableObject.spawnsSecondaryEffectOnCollision == true && hasCollided.Value == false && !hit.collider.gameObject.name.Contains("Projectile"))
            {
                 Debug.LogFormat($"<color=green> COLLIDER HIT: {hit.collider.gameObject.name}</color>");
                 Debug.LogFormat($"<color=green> CHILD GO: {SpellDataScriptableObject.childPrefab}</color>");
                SpawnEffectAtTargetLocationRpc(hitPosition);
                hasCollided.Value = true;
            }

            // Method: Spawns something at the end
            // gO to spawn source: Where should the gO be gotten from?
            // Solution 1: Assigned in inspector 

            HandleAllInteractions(hit.collider);

            if (gameObject.GetComponent<ISpell>().SpellName.Contains("Projectile_Air"))
            {
                ApplyPushbackToTarget(hit.collider.gameObject);
            }

            // Gameobject destroys self after collision of this is ticked in its SO
            if (SpellDataScriptableObject.destroyOnCollision && !hit.collider.gameObject.name.Contains("Projectile"))
            {
                Debug.LogFormat($"<color=green> COLLISION DESTROY: {hit.collider.gameObject.name}</color>");

                DestroySpell(gameObject);
            }
        }

        lastPosition = currentPosition; // Update lastPosition to the current position after the movement
    }


    [Rpc(SendTo.Server)]
    void SpawnEffectAtTargetLocationRpc(Vector3 position)
    {
            Debug.Log("NetworkManager.LocalClientId (" + NetworkManager.LocalClient.ClientId + ")");

            GameObject spellInstance = Instantiate(SpellDataScriptableObject.childPrefab, position, Quaternion.identity);

            NetworkObject netObj = spellInstance.GetComponent<NetworkObject>();

            //if (isWithOwnership)
            //{
            //    netObj.SpawnWithOwnership(NetworkManager.LocalClientId);
            //    if (netObj.GetComponent<HealSelf>())
            //    {
            //        netObj.GetComponent<HealSelf>().HealTarget(OwnerClientId);
            //    }
            //}
            //else
            //{
                netObj.Spawn();
            //}

        
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

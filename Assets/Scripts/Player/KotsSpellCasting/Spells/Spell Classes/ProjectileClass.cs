using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.HID;

public class ProjectileClass : SpellsClass
{

    [SerializeField] private ProjectileParryHandler projectileParryHandler;

    [SerializeField] private TriggerListener projectileTrigger;
    [SerializeField] private Transform projectileSphere;

    private Vector3 lastPosition;

    List<Rigidbody> pullSpellsList = new List<Rigidbody>();
    List<Rigidbody> pushSpellsList = new List<Rigidbody>();

    protected NetworkVariable<bool> _isExplodeOnHit = new NetworkVariable<bool>(false);
    NetworkVariable<bool> hasCollided = new NetworkVariable<bool>(false);

    Vector3 pushDirection; // Adjust the direction of the force

    protected NetworkVariable<bool> isMovement = new NetworkVariable<bool>(true);

    bool canDestroy = false;

    Dictionary<ulong, bool> playerHitID = new Dictionary<ulong, bool>();

    NetworkVariable<bool> isHitPlayer = new NetworkVariable<bool>(false);

    public bool CanDestroy
    {
        get { return canDestroy; }
        set { canDestroy = value; }

    }

    public override void OnNetworkSpawn()
    {         
        base.OnNetworkSpawn();
        rb = GetComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = false;

        pushDirection = transform.forward;

        if (projectileTrigger != null)
        {
            projectileTrigger.OnEnteredTrigger -= ProjectileTrigger_OnEnteredTrigger;
        }

        if (IsParriable())
        {
            projectileParryHandler.OnAnyPlayerPerformedParry += ProjectileParryHandler_OnAnyPlayerPerformedParry;


            // todo uncommend this
            string parryLetter = parryLetters.Value.ToString();

            // todo remove -> just for testing auto spawn in the arena
            System.Random random = new System.Random();
            int res = random.Next(0, K_SpellKeys.spellTypes.Length);
            string parryLetterTesting = K_SpellKeys.spellTypes[res].ToString();

            if (System.Array.Exists(K_SpellKeys.spellTypes, element => element.ToString() == parryLetter))
            {
                projectileParryHandler.OnProjectileSpawned(parryLetter);
            }
            else
            {
                projectileParryHandler.OnProjectileSpawned(parryLetterTesting);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (projectileTrigger != null)
        {
            projectileTrigger.OnEnteredTrigger -= ProjectileTrigger_OnEnteredTrigger;
        }
        // ...unsubscribe from the event here!
        if (IsParriable() && projectileParryHandler != null)
        {
            projectileParryHandler.OnAnyPlayerPerformedParry -= ProjectileParryHandler_OnAnyPlayerPerformedParry;
        }
    }

   private void ProjectileTrigger_OnEnteredTrigger(Collider collider)
   {


       Debug.Log("ProjectileTrigger_OnEnteredTrigger (" + collider.gameObject.name + ")");

        if (!IsServer) return;

       if (collider.gameObject.CompareTag("Player") && SpellDataScriptableObject.moveSpeed < 40)
       {
           ulong hitPlayerOwnerID = collider.gameObject.GetComponent<NetworkBehaviour>().OwnerClientId;

           if (!playerHitID.ContainsKey(hitPlayerOwnerID) && isHitPlayer.Value == false)
           {
               isHitPlayer.Value = true;

               playerHitID.Add(hitPlayerOwnerID, true);

               Vector3 hitPosition = collider.ClosestPoint(transform.position);

               HandleCollision(collider, hitPosition);
           }
       }
   }

    // This method is triggered when a player successfully performs a parry
    private void ProjectileParryHandler_OnAnyPlayerPerformedParry(object sender, System.EventArgs e)
    {
        if (!IsServer) return;
        StartCoroutine(DelayedDestruction());
    }

    // Update is called once per frame
    public override void FixedUpdate()
    {
        if (!IsSpawned) return;

        base.FixedUpdate();

        MoveAndHitRegRpc();

        if (!IsSpawned) return;

        HandlePushback();

        if (CanDestroy) // I figured that if I added a delay to the destruction of the spell then then the apply pushback would have enough time to apply its effect
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
        Vector3 currentPosition = projectileSphere.position;

        //Debug.LogFormat($"<color=blue>Current Position: {currentPosition}</color>");

        Vector3 forceDirection = projectileSphere.forward; // RESET SPEED

        forceDirection = projectileSphere.forward * SpellDataScriptableObject.moveSpeed;
        // rb.AddForce(forceDirection, ForceMode.Force); // or ForceMode.Acceleration
        rb.velocity = projectileSphere.forward * SpellDataScriptableObject.moveSpeed;

        rb.isKinematic = false; // Stop the rigidbody from moving
        rb.useGravity = false; // Enable gravity if needed


        RaycastHit hit;

        // For a unit sphere mesh (diameter 1, radius 0.5)
        // The following can be made only if the GameObject is a uniformly scaled sphere
        float radius = 0.5f * Mathf.Max(projectileSphere.lossyScale.x, projectileSphere.lossyScale.y, projectileSphere.lossyScale.z);

        Vector3 direction = currentPosition - lastPosition;
        float distance = direction.magnitude;

        // If the object is moving faster than a specific speed = Use the below method
        // Otherwise, OnTriggerEnter handlles the collision
        if (SpellDataScriptableObject.moveSpeed < 39) return;

        // Throw a sphere cast IN FRONT OF the projectile gO
        // previously: the sphere cast was being thrown behind the projectile causing issues with collisions
        // The hit was being registered on exiting a collider instead of upon entering it
        if (Physics.SphereCast(lastPosition, radius, direction.normalized, out hit, distance))
        {
            Vector3 hitPosition = hit.point;

            Debug.LogFormat($"<color=blue>Hit position: {hitPosition}</color>");

            //Debug.LogFormat($"<color=blue>hit: {hit.collider.gameObject.name}</color>");

            //TODO fix this
            HandleCollision(hit.collider, hitPosition);

            //// If the projectile produces a secondary effect on collision, handle the spawning and prevent the spell from doing so again
            //if (SpellDataScriptableObject.spawnsSecondaryEffectOnCollision == true && hasCollided.Value == false && !hit.collider.gameObject.name.Contains("Projectile") && !hit.collider.gameObject.CompareTag("Spell"))
            //{
            //     Debug.LogFormat($"<color=green> COLLIDER HIT: {hit.collider.gameObject.name}</color>");
            //     Debug.LogFormat($"<color=green> CHILD GO: {SpellDataScriptableObject.childPrefab}</color>");
            //    SpawnEffectAtTargetLocationRpc(hitPosition);
            //    hasCollided.Value = true;
            //}

            //// Method: Spawns something at the end
            //// gO to spawn source: Where should the gO be gotten from?
            //// Solution 1: Assigned in inspector

            //HandleAllInteractions(hit.collider);

            //if (gameObject.GetComponent<ISpell>().SpellName.Contains("Projectile_Air"))
            //{
            //    ApplyPushbackToTarget(hit.collider.gameObject);
            //}

            //// Gameobject destroys self after collision if isDestroyOnCollision is ticked in its SO
            //if (SpellDataScriptableObject.destroyOnCollision && !hit.collider.gameObject.CompareTag("Spell") && !hit.collider.gameObject.name.Contains("Projectile"))
            //{
            //    Debug.LogFormat($"<color=green> COLLISION DESTROY: {hit.collider.gameObject.name}</color>");

            //    DestroySpell(gameObject);
            //}
        }

        lastPosition = currentPosition; // Update lastPosition to the current position after the movement
    }


    void HandleCollision(Collider colliderHit, Vector3 hitPosition = default)
    {

        // If the projectile produces a secondary effect on collision, handle the spawning and prevent the spell from doing so again
        if (SpellDataScriptableObject.spawnsSecondaryEffectOnCollision == true && 
            hasCollided.Value == false && !colliderHit.gameObject.name.Contains("Projectile") && 
            !colliderHit.gameObject.CompareTag("Spell"))
        {
            Debug.LogFormat($"<color=green> COLLIDER HIT: {colliderHit.gameObject.name}</color>");
            Debug.LogFormat($"<color=green> CHILD GO: {SpellDataScriptableObject.childPrefab}</color>");
            SpawnEffectAtTargetLocationRpc(hitPosition);
            hasCollided.Value = true;
        }

        // Method: Spawns something at the end
        // gO to spawn source: Where should the gO be gotten from?
        // Solution 1: Assigned in inspector
        //if (hasCollided.Value == false)
        //{
        HandleAllInteractions(colliderHit);
        //    hasCollided.Value = true;
        //}

        if (gameObject.GetComponent<ISpell>().SpellName.Contains("Projectile_Air"))
        {
            ApplyPushbackToTarget(colliderHit.gameObject);
        }

        // Gameobject destroys self after collision if isDestroyOnCollision is ticked in its SO
        if (SpellDataScriptableObject.destroyOnCollision && !colliderHit.gameObject.CompareTag("Spell") && !colliderHit.gameObject.name.Contains("Projectile"))
        {
            Debug.LogFormat($"<color=green> COLLISION DESTROY: {colliderHit.gameObject.name}</color>");

            DestroySpell(gameObject);
        }
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

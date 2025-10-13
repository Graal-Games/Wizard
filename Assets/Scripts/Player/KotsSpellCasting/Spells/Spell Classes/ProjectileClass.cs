using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.HID;

public class ProjectileClass : SpellsClass
{
    [Header("Parry Settings")]
    [SerializeField] private float anticipationDistance = 15f;
    [SerializeField] private float parryDistance = 5f;
    [SerializeField, Range(1, 180)] private float parryAngle = 90f;
    [SerializeField] private LayerMask playerLayer;
    private bool isParried = false;

    // A set to keep track of players who were in range last frame
    private HashSet<NetworkObject> playersInRangeLastFrame = new HashSet<NetworkObject>();


    [SerializeField] private ProjectileParryHandler projectileParryHandler;
    //[SerializeField] private Transform projectileSphere;

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

    bool hasFoundShield = false;




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
        //lastPosition = projectileSphere.position;

        if (IsParriable())
        {
            projectileParryHandler.OnAnyPlayerPerformedParry += ProjectileParryHandler_OnAnyPlayerPerformedParry;

            // The NetworkVariable is already set by the caster. We just need to wait
            // for it to be synchronized and then use it. We can use a callback for this.
            parryLetters.OnValueChanged += (previousValue, newValue) =>
            {
                // This code will run when the parry letter is set or changed.
                if (!string.IsNullOrEmpty(newValue.ToString()))
                {
                    Debug.Log($"[Projectile {NetworkObjectId}]: Parry letter initialized to '{newValue}'.");
                    projectileParryHandler.OnProjectileSpawned(newValue.ToString());
                }
            };

            // Also handle the case where the value might already be set when we spawn
            if (!string.IsNullOrEmpty(parryLetters.Value.ToString()))
            {
                Debug.Log($"[Projectile {NetworkObjectId}]: Parry letter was already '{parryLetters.Value}'. Initializing immediately.");
                projectileParryHandler.OnProjectileSpawned(parryLetters.Value.ToString());
            }
        }
    }






    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsParriable() && projectileParryHandler != null)
        {
            projectileParryHandler.OnAnyPlayerPerformedParry -= ProjectileParryHandler_OnAnyPlayerPerformedParry;
        }

        if (IsServer && IsParriable() && playersInRangeLastFrame.Count > 0)
        {
            foreach (var player in playersInRangeLastFrame)
            {
                if (player != null && player.TryGetComponent<PlayerSpellParryManager>(out var parryManager))
                {
                    projectileParryHandler.RemovePlayerFromRange(player.OwnerClientId, parryManager);
                }
            }
        }
    }






    // This method is triggered when a player successfully performs a parry
    private void ProjectileParryHandler_OnAnyPlayerPerformedParry(object sender, System.EventArgs e)
    {
        Debug.Log($"<color=cyan>[SERVER Projectile {NetworkObjectId}]:</color> OnAnyPlayerPerformedParry event received! Proceeding with neutralization logic.");

        if (!IsServer || isParried) return; // Also prevent this from running more than once

        Debug.Log($"<color=cyan>[Projectile {this.NetworkObjectId}]: Parry event received! Neutralizing projectile.</color>");

        isParried = true;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero; // Immediately stop all movement
        }

        StartCoroutine(DelayedDestruction());
    }






    // Update is called once per frame
    public override void FixedUpdate()
    {
        if (isParried) return; // If parried, do nothing
        if (!IsSpawned) return;

        base.FixedUpdate();

        // The parry logic should run before movement to give the player the most time to react.
        if (IsServer && IsParriable())
        {
            HandleParryProximityCheck();
        }

        MoveAndHitRegRpc();

        //CLIENT_SIDE_MoveAndHitReg();

        HandlePushback();

        if (!IsSpawned) return;

        if (CanDestroy) // I figured that if I added a delay to the destruction of the spell then then the apply pushback would have enough time to apply its effect
        {
            StartCoroutine(DelayedDestruction());
        }
    }






    /// <summary>
    /// This new method handles all proximity logic for the parry system.
    /// </summary>
    private void HandleParryProximityCheck()
    {
        // A temporary set to track players detected in the current frame
        HashSet<NetworkObject> playersInRangeThisFrame = new HashSet<NetworkObject>();

        // Find all colliders on the player layer within the maximum anticipation distance
        Collider[] playerColliders = Physics.OverlapSphere(transform.position, anticipationDistance, playerLayer);

        // Calculate the dot product threshold from our angle field once before the loop
        float parryAngleThreshold = Mathf.Cos(parryAngle * 0.5f * Mathf.Deg2Rad);

        foreach (var playerCollider in playerColliders)
        {
            // 1. Get the direction from the projectile to the player
            Vector3 directionToPlayer = (playerCollider.transform.position - transform.position).normalized;

            // 2. Calculate the dot product
            float dotProduct = Vector3.Dot(transform.forward, directionToPlayer);

            // 3. If the player is NOT in the forward cone, skip them and continue to the next one
            if (dotProduct < parryAngleThreshold)
            {
                continue;
            }

            // If the player is in the cone, proceed with state checks
            if (playerCollider.TryGetComponent<NetworkObject>(out var networkObject) &&
                playerCollider.TryGetComponent<PlayerSpellParryManager>(out var parryManager))
            {
                playersInRangeThisFrame.Add(networkObject);
                float distance = Vector3.Distance(transform.position, playerCollider.transform.position);

                // Determine the correct parry state based on distance
                var newState = (distance <= parryDistance)
                    ? ProjectileParryHandler.ParryState.PARRIABLE
                    : ProjectileParryHandler.ParryState.ANTICIPATION;

                // Update the player's state (this method should handle sending RPCs)
                projectileParryHandler.UpdatePlayerState(networkObject.OwnerClientId, newState, parryManager);
            }
        }

        // Now, check for players who have moved out of range
        // We make a copy to iterate over, to avoid modifying the collection while looping
        var playersToCheckForExit = new HashSet<NetworkObject>(playersInRangeLastFrame);
        foreach (var oldPlayer in playersToCheckForExit)
        {
            if (oldPlayer != null && !playersInRangeThisFrame.Contains(oldPlayer))
            {
                // This player was in range last frame, but not anymore. Remove them.
                if (oldPlayer.TryGetComponent<PlayerSpellParryManager>(out var parryManager))
                {
                    projectileParryHandler.RemovePlayerFromRange(oldPlayer.OwnerClientId, parryManager);
                }
            }
        }

        // Finally, update the "last frame" set for the next check
        playersInRangeLastFrame = playersInRangeThisFrame;
    }






    IEnumerator DelayedDestruction()
    {
        yield return new WaitForSeconds(0.3f); // The value here seems to be working for now for pushback effect. Might need to revise it later.
        DestroySpell(gameObject);
    }


    void CLIENT_SIDE_MoveAndHitReg()
    {
        Vector3 currentPosition = transform.position;

        float radius = 0.5f * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        RaycastHit hit;
        Vector3 direction = currentPosition - lastPosition; // Used to be inside the below conditional
        float distance = direction.magnitude * Time.fixedDeltaTime; 

        Vector3 forceDirection = transform.forward; // RESET SPEED

        forceDirection = transform.forward * SpellDataScriptableObject.moveSpeed;
        rb.linearVelocity = transform.forward * SpellDataScriptableObject.moveSpeed;

        rb.isKinematic = false; // Stop the rigidbody from moving
        rb.useGravity = false; // Enable gravity if needed

        if (Physics.SphereCast(lastPosition, radius, direction.normalized, out hit, distance))
        {
            //Debug.Log($"<color=lime>Projectile something hit: '{hit.collider.gameObject}'");
            Vector3 hitPosition = hit.point;
            Debug.LogFormat($"<color=yellow>>>>>>>>>>>>>>>>>>>SPHERE CAST {hit.collider.gameObject.name}<<<<<<<<<<<<<<<<<<<<</color>");

            if (hit.collider.gameObject.CompareTag("Player")) // Can be migrated?? //
            {
                Debug.LogFormat($"<color=orange>>>>>>>>>>>>>>>>>>>Player Hit On Client {hit.collider.gameObject.name}<<<<<<<<<<<<<<<<<<<<</color>");
            }
        }

        lastPosition = currentPosition;
    }




    [Rpc(SendTo.Server)]
    public virtual void MoveAndHitRegRpc()
    {
        //Debug.LogFormat($"<color=blue>MOVE AND HIT REG</color>");
        Vector3 currentPosition = transform.position;
        Vector3 forceDirection = transform.forward; // RESET SPEED

        forceDirection = transform.forward * SpellDataScriptableObject.moveSpeed;
        rb.linearVelocity = transform.forward * SpellDataScriptableObject.moveSpeed;

        rb.isKinematic = false; // Stop the rigidbody from moving
        rb.useGravity = false; // Enable gravity if needed



        RaycastHit hit;

        Vector3 direction = currentPosition - lastPosition; // Used to be inside the below conditional
        float distance = direction.magnitude;


        // For a unit sphere mesh (diameter 1, radius 0.5)
        // The following can be made only if the GameObject is a uniformly scaled sphere
        float radius = 1f * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);



        // Throw a sphere cast IN FRONT OF the projectile gO
        // previously: the sphere cast was being thrown behind the projectile causing issues with collisions
        // The hit was being registered on exiting a collider instead of upon entering it
        if (Physics.SphereCast(lastPosition, radius, direction.normalized, out hit, distance))
        {
            //Debug.Log($"<color=lime>Projectile something hit: '{hit.collider.gameObject}'");
            Vector3 hitPosition = hit.point;
            //Debug.LogFormat($"<color=yellow>>>>>>>>>>>>>>>>>>>SPHERE CAST {hit.collider.gameObject.name}<<<<<<<<<<<<<<<<<<<<</color>");

            if (hit.collider.CompareTag("ActiveShield") && hasFoundShield == false)
            {
                hasFoundShield = true;
                HandleCollision(hit.collider, hitPosition);
                hasFoundShield = false;
            }

            if (SpellDataScriptableObject.moveSpeed < 40) return;

            if (hit.collider.gameObject.tag == "Player") // Can be migrated?? //
            {
                string actualLayerName = LayerMask.LayerToName(hit.collider.gameObject.layer);
                Debug.Log($"<color=lime>!!! PLAYER HIT !!!</color> The player's actual runtime layer is: '{actualLayerName}'");

                ulong hitPlayerOwnerID = hit.collider.gameObject.GetComponent<NetworkBehaviour>().OwnerClientId;


                // <<< The below code could be simplified
                if (!playerHitID.ContainsKey(hitPlayerOwnerID) && !isHitPlayer.Value)
                {
                    isHitPlayer.Value = true;
                    playerHitID.Add(hitPlayerOwnerID, true);
                    HandleCollision(hit.collider, hit.point);

                }

            } else if (!hit.collider.gameObject.name.Contains(SpellName.ToString()))
            {
                Debug.LogFormat($"<color=blue>hit.collider.gameObject.name: {hit.collider.gameObject.name} && SpellName: {SpellName}</color>");
                HandleCollision(hit.collider, hitPosition);
            }

        }
        //Debug.LogFormat($"<color=red>lastPosition: {lastPosition} && currentPosition: {currentPosition}</color>");

        lastPosition = currentPosition; // Update lastPosition to the current position after the movement
        //}
    }







    void HandleCollision(Collider colliderHit, Vector3 hitPosition = default)
    {
        // If the projectile produces a secondary effect on collision, handle the spawning and prevent the spell from doing so again
        if (SpellDataScriptableObject.spawnsSecondaryEffectOnCollision 
            && !hasCollided.Value 
            && !colliderHit.gameObject.CompareTag("Spell") 
            && !colliderHit.gameObject.name.Contains("Projectile")
            && !colliderHit.gameObject.name.Contains("Area of Effect")
            && !colliderHit.gameObject.name.Contains("Shaders"))
        {
            Debug.LogFormat($"<color=green> COLLIDER HIT: {colliderHit.gameObject.name}</color>");
            Debug.LogFormat($"<color=green> CHILD GO: {SpellDataScriptableObject.childPrefab}</color>");
            SpawnEffectAtTargetLocationRpc(hitPosition);
            hasCollided.Value = true;
        }

        // Debug.LogFormat($"<color=green> COLLIDER HIT: {colliderHit.gameObject.name}</color>");

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

        int layer = LayerMask.NameToLayer("Solid Spell");

        // Gameobject destroys self after collision if isDestroyOnCollision is ticked in its SO
        if (SpellDataScriptableObject.destroyOnCollision 
            && !colliderHit.gameObject.name.Contains("Projectile") 
            && !colliderHit.gameObject.name.Contains("Area of Effect")  
            && !colliderHit.gameObject.name.Contains("Shaders")
            ) // Add bool that checks whether the other spell (colliderHit) should be considered a solid surface
        {
            Debug.LogFormat($"<color=green> COLLISION DESTROY: {colliderHit.gameObject.name}</color>");

            if (!colliderHit.gameObject.name.Contains("Aoe"))
            DestroySpell(gameObject);
        }
    }







    [Rpc(SendTo.Server)]
    void SpawnEffectAtTargetLocationRpc(Vector3 position)
    {
        Debug.Log("NetworkManager.LocalClientId (" + NetworkManager.LocalClient.ClientId + ")");
        GameObject spellInstance = Instantiate(SpellDataScriptableObject.childPrefab, position, Quaternion.identity);
        NetworkObject netObj = spellInstance.GetComponent<NetworkObject>();
        netObj.Spawn();
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
                    ApplyPushbackClientRpc(netObj.OwnerClientId, SpellDataScriptableObject.pushForce, pushDirection);                
                }
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






    private void OnTriggerEnter(Collider other)
    {
        //if (!IsServer) return;
        //return;
        if (other.gameObject.CompareTag("Player") && SpellDataScriptableObject.moveSpeed < 40)
        {
            Debug.LogFormat($"<color=blue>ONNNNNNNN TRIGGER ENTER</color>");

            ulong hitPlayerOwnerID = other.gameObject.GetComponent<NetworkBehaviour>().OwnerClientId;

            if (!playerHitID.ContainsKey(hitPlayerOwnerID) && isHitPlayer.Value == false)
            {
                Debug.LogFormat($"<color=blue>2222 ONNNNNNNN TRIGGER ENTER</color>");

                isHitPlayer.Value = true;

                playerHitID.Add(hitPlayerOwnerID, true);

                Vector3 hitPosition = other.ClosestPoint(transform.position);

                if (hasFoundShield == false)
                {
                    hasFoundShield = true;
                    HandleCollision(other, hitPosition);
                    hasFoundShield = false;
                }
            }
        }
        else
        {
            Vector3 hitPosition = other.ClosestPoint(transform.position);
            HandleCollision(other, hitPosition);
        }

    }

    //private void OnTriggerExit(Collider other)
    //{
    //    if (other.gameObject.CompareTag("Player") && SpellDataScriptableObject.moveSpeed < 40)
    //    {

    //    }
    //}
        
}

using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private Transform projectileSphere;

    private Vector3 lastPosition;

    List<Rigidbody> pullSpellsList = new List<Rigidbody>();

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
        lastPosition = projectileSphere.position;

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
            rb.velocity = Vector3.zero; // Immediately stop all movement
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

        MoveAndHitRegistration();

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


    public virtual void MoveAndHitRegistration()
    {
        // This logic now runs exclusively on the server, which is much more efficient.
        if (!IsServer) return;

        Vector3 currentPosition = projectileSphere.position;
        rb.velocity = projectileSphere.forward * SpellDataScriptableObject.moveSpeed;

        Vector3 direction = currentPosition - lastPosition;
        // Avoid SphereCast with zero distance/direction, which can cause issues.
        if (direction.sqrMagnitude > 0.001f)
        {
            float radius = 0.5f * Mathf.Max(projectileSphere.lossyScale.x, projectileSphere.lossyScale.y, projectileSphere.lossyScale.z);
            float distance = direction.magnitude;
            if (Physics.SphereCast(lastPosition, radius, direction.normalized, out RaycastHit hit, distance))
            {
                if (hit.collider.gameObject.CompareTag("Player"))
                {

                    string actualLayerName = LayerMask.LayerToName(hit.collider.gameObject.layer);
                    Debug.Log($"<color=lime>!!! PLAYER HIT !!!</color> The player's actual runtime layer is: '{actualLayerName}'");

                    ulong hitPlayerOwnerID = hit.collider.gameObject.GetComponent<NetworkBehaviour>().OwnerClientId;
                    if (!playerHitID.ContainsKey(hitPlayerOwnerID) && !isHitPlayer.Value)
                    {
                        isHitPlayer.Value = true;
                        playerHitID.Add(hitPlayerOwnerID, true);
                        HandleCollision(hit.collider, hit.point);
                    }
                }
                else
                {
                    HandleCollision(hit.collider, hit.point);
                }
            }
        }
        lastPosition = currentPosition;
    }


    void HandleCollision(Collider colliderHit, Vector3 hitPosition = default)
    {

        if (SpellDataScriptableObject.spawnsSecondaryEffectOnCollision && !hasCollided.Value &&
            !colliderHit.gameObject.CompareTag("Spell") && !colliderHit.gameObject.name.Contains("Projectile"))
        {
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
            //Debug.LogFormat($"<color=green> COLLISION DESTROY: {colliderHit.gameObject.name} {colliderHit.gameObject.tag}</color>");

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

    [Rpc(SendTo.ClientsAndHost)]
    void ApplyPushbackClientRpc(ulong targetClientId, float pushForce, Vector3 direction, RpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        if (NetworkManager.Singleton.LocalClient.PlayerObject.TryGetComponent<Rigidbody>(out Rigidbody playerRb))
        {
            playerRb.AddForce(pushForce * direction.normalized, ForceMode.Impulse);
        }
    }

    public void ApplyPushbackToTarget(GameObject other)
    {
        // This check still runs on the server because it's called from MoveAndHitRegistration
        if (other.gameObject.CompareTag("Player"))
        {
            if (SpellDataScriptableObject.pushForce > 0)
            {
                // Get the client ID of the player who was hit
                ulong targetClientId = other.GetComponent<NetworkObject>().OwnerClientId;

                // Immediately send the RPC to that specific client
                ApplyPushbackClientRpc(targetClientId, SpellDataScriptableObject.pushForce, pushDirection);

                // The server decides when to destroy the object
                StartCoroutine(DelayedDestruction());
            }
        }
    }
}

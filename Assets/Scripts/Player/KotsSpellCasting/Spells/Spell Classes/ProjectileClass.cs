using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.HID;

public class ProjectileClass : SpellsClass
{

    [SerializeField] private ProjectileParryHandler projectileParryHandler;

    [SerializeField] private TriggerListener projectileTrigger;

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

    private void Awake()
    {
        // First, get the component from this GameObject
        projectileTrigger = GetComponentInChildren<TriggerListener>();

        // Now, check if it was found before subscribing to the event
        if (projectileTrigger != null)
        {
            projectileTrigger.OnEnteredTrigger += ProjectileTrigger_OnEnteredTrigger;
        }
        else
        {
            Debug.LogError("TriggerListener component not found on this GameObject!", this);
        }
    }

    private void ProjectileTrigger_OnEnteredTrigger(Collider collider)
    {

        Debug.Log("ProjectileTrigger_OnEnteredTrigger (" + collider.gameObject.name + ")");

    }

    public override void OnNetworkSpawn()
    {         
        base.OnNetworkSpawn();
        rb = GetComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = false;

        pushDirection = transform.forward;

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

        // ...unsubscribe from the event here!
        if (IsParriable() && projectileParryHandler != null)
        {
            projectileParryHandler.OnAnyPlayerPerformedParry -= ProjectileParryHandler_OnAnyPlayerPerformedParry;
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


        // Only the server should calculate movement and check for hits.
        if (IsServer)
        {
            // This method might cause this object to be despawned.
            HandleMovementAndCollision();
            if (!IsSpawned) return;
            HandlePushback();
        }

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

    public virtual void HandleMovementAndCollision()
    {
        Debug.Log($"SERVER is running physics for projectile owned by ClientId: {OwnerClientId}");

        Vector3 currentPosition = transform.position;

        //Debug.LogFormat($"<color=blue>Current Position: {currentPosition}</color>");

        Vector3 forceDirection = transform.forward; // RESET SPEED

        forceDirection = transform.forward * SpellDataScriptableObject.moveSpeed;
        // rb.AddForce(forceDirection, ForceMode.Force); // or ForceMode.Acceleration
        rb.velocity = transform.forward * SpellDataScriptableObject.moveSpeed;

        rb.isKinematic = false; // Stop the rigidbody from moving
        rb.useGravity = false; // Enable gravity if needed


        RaycastHit hit;

        // For a unit sphere mesh (diameter 1, radius 0.5)
        // The following can be made only if the GameObject is a uniformly scaled sphere
        float radius = 0.5f * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);

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

            // Get the NetworkObject of the thing we hit
            NetworkObject hitNetObj = hit.collider.GetComponentInParent<NetworkObject>();

            // Check if we hit a valid networked object AND if its owner is the same as our owner
            if (hitNetObj != null && hitNetObj.OwnerClientId == this.OwnerClientId)
            {
                Debug.Log("SERVER HIT: Collision was with self, ignoring.");
                // We hit ourselves or a part of our own player, so ignore this collision and continue.
                lastPosition = currentPosition;
                return;
            }

            Vector3 hitPosition = hit.point;

            Debug.LogFormat($"<color=blue>Hit position: {hitPosition}</color>");

            //Debug.LogFormat($"<color=blue>hit: {hit.collider.gameObject.name}</color>");

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
        if (SpellDataScriptableObject.spawnsSecondaryEffectOnCollision == true && hasCollided.Value == false && !colliderHit.gameObject.name.Contains("Projectile") && !colliderHit.gameObject.CompareTag("Spell"))
        {
            Debug.LogFormat($"<color=green> COLLIDER HIT: {colliderHit.gameObject.name}</color>");
            Debug.LogFormat($"<color=green> CHILD GO: {SpellDataScriptableObject.childPrefab}</color>");
            SpawnEffectAtTargetLocation(hitPosition);
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


    private void SpawnEffectAtTargetLocation(Vector3 position)
    {
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
        // This method is now only called by the server from FixedUpdate.

        // Remove any null objects from the list
        pushSpellsList.RemoveAll(item => item == null);

        // Apply force to all valid rigidbodies
        foreach (Rigidbody rb in pushSpellsList)
        {
            rb.AddForce(SpellDataScriptableObject.pushForce * pushDirection.normalized, ForceMode.Impulse);
        }

        // Clear the list after applying the force so it doesn't happen again next frame.
        if (pushSpellsList.Count > 0)
        {
            canDestroy = true; // Set the spell to be destroyed after pushing.
            pushSpellsList.Clear();
        }
    }

    public void ApplyPushbackToTarget(GameObject other)
    {
        // ONLY the server should detect and add objects to the push list.
        if (!IsServer) return;

        if (other.gameObject.CompareTag("Player"))
        {
            if (SpellDataScriptableObject.pushForce > 0)
            {
                Rigidbody rb = other.GetComponent<Rigidbody>();
                if (rb != null && !pushSpellsList.Contains(rb)) // Avoid duplicates
                {
                    pushSpellsList.Add(rb);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.gameObject.CompareTag("Player") && SpellDataScriptableObject.moveSpeed < 40)
        {
            ulong hitPlayerOwnerID = other.gameObject.GetComponent<NetworkBehaviour>().OwnerClientId;

            if (!playerHitID.ContainsKey(hitPlayerOwnerID) && isHitPlayer.Value == false)
            {
                isHitPlayer.Value = true;

                playerHitID.Add(hitPlayerOwnerID, true);

                Vector3 hitPosition = other.ClosestPoint(transform.position);

                HandleCollision(other, hitPosition);
            }
        }

    }
}

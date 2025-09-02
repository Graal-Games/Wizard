using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public abstract class SpellsClass : NetworkBehaviour, ISpell
{
    [SerializeField]
    private K_SpellData spellDataScriptableObject;
    public K_SpellData SpellDataScriptableObject => spellDataScriptableObject;

    protected Rigidbody rb;

    public string SpellName => spellDataScriptableObject.name;
    public bool IsDispelResistant => spellDataScriptableObject.isDispelResistant;

    // --- NETWORKED STATE ---
    // All game state is now server-authoritative.
    private NetworkVariable<float> healthPoints = new NetworkVariable<float>(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> isSpellActive = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString32Bytes> parryLetters = new NetworkVariable<FixedString32Bytes>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // --- SERVER-ONLY VARIABLES ---
    private float spellLifetimeTimer = 0f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // It's good practice to get the Rigidbody reference here in the base class.
        rb = GetComponent<Rigidbody>();

        if (IsServer)
        {
            if (SpellDataScriptableObject != null)
            {
                // The server is in charge of the spell's lifetime.
                spellLifetimeTimer = SpellDataScriptableObject.spellDuration;

                if (SpellDataScriptableObject.health > 0)
                {
                    healthPoints.Value = SpellDataScriptableObject.health;
                }

                // The server controls when the spell becomes active.
                if (SpellDataScriptableObject.spellActivationDelay > 0)
                {
                    StartCoroutine(ActivationDelayRoutine());
                }
                else
                {
                    ActivateSpell();
                }
            }
            else
            {
                Debug.LogError("SpellDataScriptableObject is not assigned in " + gameObject.name, this);
            }
        }
    }

    public virtual void FixedUpdate()
    {
        // The server handles the lifetime timer AND the max-scale destruction
        if (IsServer)
        {
            if (!IsSpawned) return;

            // The server ticks down the lifetime and destroys the spell when it expires.
            if (spellLifetimeTimer > 0)
            {
                spellLifetimeTimer -= Time.fixedDeltaTime;
                if (spellLifetimeTimer <= 0)
                {
                    DestroySpell(gameObject);
                }
            }

            // Server checks if the spell has reached max scale
            if (spellDataScriptableObject.maxScale > 0 && transform.localScale.x >= spellDataScriptableObject.maxScale)
            {
                DestroySpell(gameObject);
            }
        }
    }

    public bool IsParriable()
    {
        return spellDataScriptableObject != null && spellDataScriptableObject.isParriable;
    }

    // Server is the only one to process collisions.
    public virtual void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        HandleCollision(other);
    }

    // This is now the single, authoritative entry point for all collision logic.
    protected virtual void HandleCollision(Collider colliderHit)
    {
        if (!IsServer) return;

        Debug.Log($"[Server] Spell owned by [{OwnerClientId}] collided with '{colliderHit.name}'.");

        // 1. Check for self-hit and ignore
        if (colliderHit.TryGetComponent<NetworkObject>(out var hitNetObj))
        {
            // It's a networked object. Let's check the owners.
            Debug.Log($"[Server] Hit object '{hitNetObj.name}' is owned by ClientId [{hitNetObj.OwnerClientId}]. This spell is owned by ClientId [{this.OwnerClientId}].");

            // If the object we hit is owned by the same person who owns this spell...
            if (hitNetObj != null && hitNetObj.OwnerClientId == this.OwnerClientId)
            {
                Debug.LogWarning($"[Server] SELF-HIT DETECTED. Ignoring collision.");
                return;
            }
        }
        else {
            Debug.Log($"[Server] Hit object '{colliderHit.name}' is not a networked object (it has no NetworkObject component).");
        }

        // 2. Check if we hit a player
        if (colliderHit.CompareTag("Player"))
        {
            // 3. Get their PlayerHealth component
            if (colliderHit.TryGetComponent<PlayerHealth>(out PlayerHealth playerHealth))
            {
                // Get the status effect component
                if (colliderHit.TryGetComponent<PlayerStatusEffects>(out var status) && status.HasEffect(EffectType.Shield))
                {
                    // Logic to damage the shield instead of the player would go here.
                    Debug.Log("Spell hit a shielded player.");
                    // For now, we'll just stop the projectile.
                    // You could add logic here to damage the shield object itself.
                }
                else
                {
                    // If there's no shield, deal damage directly to the player.
                    float damage = SpellDataScriptableObject.directDamageAmount;
                    playerHealth.TakeDamage(damage, this.OwnerClientId);
                }
            }
        }

        if (SpellDataScriptableObject.isExplosive)
        {
            // If it is, spawn the secondary effect (the explosion).
            // The 'childPrefab' from your K_SpellData should be the explosion effect.
            SpawnEffectAtTargetLocation(transform.position);
        }

        // 5. Destroy the spell if it's supposed to
        if (SpellDataScriptableObject.destroyOnCollision)
        {
            Debug.Log($"[Server] DESTROYING SPELL due to `destroyOnCollision` being true after hitting '{colliderHit.name}'.");
            DestroySpell(gameObject);
        }
    }

    // Server-side logic for activating the spell's collider after a delay
    private IEnumerator ActivationDelayRoutine()
    {
        GetComponentInChildren<Collider>().enabled = false;
        yield return new WaitForSeconds(SpellDataScriptableObject.spellActivationDelay);
        ActivateSpell();
    }

    private void ActivateSpell()
    {
        GetComponentInChildren<Collider>().enabled = true;
        isSpellActive.Value = true;
    }

    // This is the robust, safe method for destroying any spell.
    public void DestroySpell(GameObject spellObj)
    {
        if (spellObj == null) return;

        NetworkObject netObj = spellObj.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            // Only the server can request a despawn
            if (IsServer)
            {
                DestroySpellServerRpc(netObj);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)] // Allow clients to request destruction, but server executes it
    private void DestroySpellServerRpc(NetworkObjectReference netObjRef)
    {
        if (netObjRef.TryGet(out NetworkObject netObjToDestroy))
        {
            if (netObjToDestroy != null && netObjToDestroy.IsSpawned)
            {
                netObjToDestroy.Despawn();
            }
        }
    }

    protected void GradualScale(float scaleSpeed, float maxScale)
    {
        float deltaScale = scaleSpeed * Time.fixedDeltaTime;
        if (transform.localScale.x < maxScale)
        {
            float newScale = Mathf.Min(transform.localScale.x + deltaScale, maxScale);
            transform.localScale = new Vector3(newScale, newScale, newScale);
        }
    }

    private void SpawnEffectAtTargetLocation(Vector3 position)
    {
        // This logic should only run on the server.
        if (!IsServer) return;

        // Make sure there is a child prefab assigned in the SpellData.
        if (SpellDataScriptableObject.childPrefab == null)
        {
            Debug.LogWarning($"Trying to spawn a child effect for '{SpellDataScriptableObject.name}', but no child prefab is assigned.", this);
            return;
        }

        // Instantiate the effect locally on the server.
        GameObject effectInstance = Instantiate(SpellDataScriptableObject.childPrefab, position, Quaternion.identity);

        // Get its NetworkObject to spawn it on the network for all clients to see.
        if (effectInstance.TryGetComponent<NetworkObject>(out var netObj))
        {
            netObj.Spawn();
        }
        else
        {
            Debug.LogError($"The child prefab '{SpellDataScriptableObject.childPrefab.name}' is missing a NetworkObject component!", this);
        }
    }
}
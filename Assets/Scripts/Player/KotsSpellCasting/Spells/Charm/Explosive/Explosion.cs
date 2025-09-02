using System.Collections.Generic; // Needed for the List
using UnityEngine;
using Unity.Netcode;

public class Explosion : SpellsClass
{
    // This list will exist ONLY on the server to track who has already been damaged.
    private List<Collider> alreadyHitColliders = new List<Collider>();

    public override void FixedUpdate()
    {
        // The base FixedUpdate handles the lifetime timer on the server.
        base.FixedUpdate();

        // The visual scaling can run on all clients.
        // TODO: Replace these hard-coded numbers with values from your SpellDataScriptableObject.
        GradualScale(20, 2.5f);

        // --- All hit detection and game logic MUST run only on the server ---
        if (!IsServer) return;

        // Perform the hit detection sweep.
        float currentRadius = GetComponent<SphereCollider>().radius * transform.localScale.x;
        Collider[] overlappingColliders = Physics.OverlapSphere(transform.position, currentRadius);

        foreach (var collider in overlappingColliders)
        {
            // Check if we have already hit this object to prevent multi-hitting.
            if (alreadyHitColliders.Contains(collider))
            {
                continue; // Skip if we've already hit it.
            }

            // Add the collider to the list so we don't hit it again next frame.
            alreadyHitColliders.Add(collider);

            // Call the authoritative collision handler from the base class.
            HandleCollision(collider);
        }
    }

    // This is the explosion's specific logic for what to do when it hits something.
    protected override void HandleCollision(Collider other)
    {
        // Ignore self-hits (the caster shouldn't be hurt by their own explosion).
        if (other.TryGetComponent<NetworkObject>(out var hitNetObj) && hitNetObj.OwnerClientId == this.OwnerClientId)
        {
            return;
        }

        // If we hit a player, deal damage.
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent<PlayerHealth>(out PlayerHealth playerHealth))
            {
                float damage = SpellDataScriptableObject.directDamageAmount;
                playerHealth.TakeDamage(damage, this.OwnerClientId);
            }
        }

        // Explosions usually don't destroy themselves on collision, they just complete their duration.
        // The lifetime timer in the base SpellsClass will handle this automatically.
    }
}
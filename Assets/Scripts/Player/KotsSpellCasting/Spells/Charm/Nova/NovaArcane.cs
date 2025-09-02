using UnityEngine;
using Unity.Netcode;

public class NovaArcane : NovaClass
{
    public override void FixedUpdate()
    {
        // The base FixedUpdate handles the lifetime timer on the server.
        base.FixedUpdate();

        // The visual scaling can safely run on all clients.
        // TODO: Update GradualScale to use values from your K_SpellData instead of hard-coded numbers.
        GradualScale(3f, SpellDataScriptableObject.maxScale);
    }

    // This is the new, server-authoritative way to handle collisions.
    // The base SpellsClass automatically calls this method on the server.
    protected override void HandleCollision(Collider other)
    {
        // Check if we hit a player.
        if (other.CompareTag("Player"))
        {
            // Get the NetworkObject to check for ownership.
            if (other.TryGetComponent<NetworkObject>(out var hitNetObj))
            {
                // Ignore self-hits (the caster shouldn't be hurt by their own nova).
                if (hitNetObj.OwnerClientId == this.OwnerClientId) return;
            }

            // Get the player's health component to deal damage.
            if (other.TryGetComponent<PlayerHealth>(out PlayerHealth playerHealth))
            {
                // Apply damage using the server-authoritative method.
                float damage = SpellDataScriptableObject.directDamageAmount;
                playerHealth.TakeDamage(damage, this.OwnerClientId);
            }
        }
    }

    // This is now clean. The K_SpellLauncher is responsible for its own state.
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }
}
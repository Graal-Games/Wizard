using UnityEngine;
using Unity.Netcode;

public class BarrierFire : SpellsClass
{
    // The base SpellsClass has an OnTriggerEnter that automatically calls this
    // method on the server whenever something touches the barrier's collider.
    // We just need to provide the specific logic for this fire barrier.
    protected override void HandleCollision(Collider other)
    {
        // Check if the object that entered is a player
        if (other.CompareTag("Player"))
        {
            // Optional: Prevent the caster from being hurt by their own barrier
            if (other.GetComponent<NetworkObject>().OwnerClientId == this.OwnerClientId)
            {
                return;
            }

            // Get the player's authoritative health component
            if (other.TryGetComponent<PlayerHealth>(out PlayerHealth playerHealth))
            {
                // Apply damage using the correct server-authoritative method
                float damage = SpellDataScriptableObject.directDamageAmount;
                playerHealth.TakeDamage(damage, this.OwnerClientId);

                // --- FUTURE IMPROVEMENT ---
                // For a fire barrier, instead of direct damage, you would likely apply a "Burn"
                // status effect here using the PlayerStatusEffects script.
                // For example:
                // var statusEffects = other.GetComponent<PlayerStatusEffects>();
                // var burnEffect = new StatusEffect { Type = EffectType.Burn, ... };
                // statusEffects.ApplyEffect(burnEffect);
            }
        }
    }
}
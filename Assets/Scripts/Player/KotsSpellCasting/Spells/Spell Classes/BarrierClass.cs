using UnityEngine;
using Unity.Netcode;

public class BarrierClass : SpellsClass
{
    // The base SpellsClass automatically calls this method on the server
    // whenever something touches the barrier's collider.
    protected override void HandleCollision(Collider other)
    {
        // Here, we define what the barrier does. A great example is
        // destroying enemy projectiles that collide with it.

        // Check if the object that hit us is a spell.
        if (other.CompareTag("Spell"))
        {
            // Get the spell component from the object that hit the barrier.
            if (other.TryGetComponent<SpellsClass>(out SpellsClass otherSpell))
            {
                // Make sure it's not one of our own spells hitting our own barrier.
                if (otherSpell.OwnerClientId != this.OwnerClientId)
                {
                    Debug.Log($"Barrier owned by {OwnerClientId} is destroying projectile owned by {otherSpell.OwnerClientId}.");

                    // Use the safe destruction method from the base class to destroy the projectile.
                    otherSpell.DestroySpell(other.gameObject);
                }
            }
        }

        // When a player hits the barrier, we don't need to do anything in the code.
        // The physics engine will handle blocking them automatically.
    }
}
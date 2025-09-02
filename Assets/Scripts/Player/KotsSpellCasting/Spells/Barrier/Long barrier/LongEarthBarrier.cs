using UnityEngine;
using Unity.Netcode;

public class LongEarthBarrier : SpellsClass // Assuming this inherits from BarrierClass -> SpellsClass
{
    // The base SpellsClass automatically calls this method on the server
    // whenever something touches the barrier's collider.
    protected override void HandleCollision(Collider other)
    {
        // Here, we can define what the barrier does. For example,
        // let's make it destroy enemy projectiles that hit it.

        // Check if the object that hit us is a spell
        if (other.CompareTag("Spell"))
        {
            // Get the spell component from the object that hit the barrier
            if (other.TryGetComponent<SpellsClass>(out SpellsClass otherSpell))
            {
                // Make sure it's not one of our own spells hitting our own barrier
                if (otherSpell.OwnerClientId != this.OwnerClientId)
                {
                    Debug.Log($"Barrier owned by {OwnerClientId} is destroying projectile owned by {otherSpell.OwnerClientId}.");

                    // Destroy the other spell using the safe method from our base class.
                    otherSpell.DestroySpell(other.gameObject);
                }
            }
        }

        // A barrier usually doesn't do anything when a player touches it,
        // as the physics engine handles the collision. So we don't need player logic here.
    }
}
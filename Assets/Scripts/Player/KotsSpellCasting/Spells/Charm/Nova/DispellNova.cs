// DispellNova.cs (Refactored)
using UnityEngine;
using Unity.Netcode;

public class DispellNova : SpellsClass
{
    public override void FixedUpdate()
    {
        // The base FixedUpdate handles the lifetime on the server.
        base.FixedUpdate();

        // This visual scaling method can safely run on all clients.
        // TODO: Replace these "magic numbers" with values from your SpellDataScriptableObject
        GradualScale(5, 1.4f);
    }

    // This is the new, server-authoritative way to handle collisions.
    // The base SpellsClass automatically calls this method on the server.
    protected override void HandleCollision(Collider other)
    {
        // This is the new home for your "Dispel" logic.
        // Check if the object we hit is another spell.
        if (other.CompareTag("Spell"))
        {
            if (other.TryGetComponent<SpellsClass>(out SpellsClass otherSpell))
            {
                // Make sure we don't dispel ourselves or friendly spells from the same caster.
                if (otherSpell.OwnerClientId != this.OwnerClientId)
                {
                    // If the other spell is not dispel-resistant, destroy it.
                    if (!otherSpell.IsDispelResistant)
                    {
                        Debug.Log($"DispellNova is destroying spell: {other.name}");

                        // Use the safe destruction method from the base class.
                        DestroySpell(other.gameObject);
                    }
                }
            }
        }
    }

    // This is now clean. The K_SpellLauncher should be responsible for its own state.
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }
}
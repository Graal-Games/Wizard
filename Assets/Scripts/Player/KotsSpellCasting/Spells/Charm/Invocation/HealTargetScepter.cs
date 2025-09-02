using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HealTargetScepter : SpellsClass
{
    // These variables will now only be used on the server.
    private float timer = 0f;
    private float interval = 3f;
    private Dictionary<ulong, PlayerHealth> playersInTrigger = new Dictionary<ulong, PlayerHealth>();

    // --- All physics and game logic MUST run only on the server ---
    public override void FixedUpdate()
    {
        // This script's logic should only run on the server.
        if (!IsServer) return;

        // Use the base class FixedUpdate for the IsSpawned check and lifetime timer.
        base.FixedUpdate();

        timer += Time.fixedDeltaTime;

        if (timer >= interval)
        {
            timer = 0f; // Reset the timer

            // Heal every player currently in the trigger zone.
            foreach (var playerHealth in playersInTrigger.Values)
            {
                // Check if the player object still exists before healing.
                if (playerHealth != null)
                {
                    // Call the authoritative TakeDamage method with a negative value.
                    float healAmount = SpellDataScriptableObject.healAmount;
                    playerHealth.TakeDamage(-healAmount, this.OwnerClientId);
                }
            }
        }
    }

    // OnTriggerEnter is a Unity message, so we guard it to only run on the server.
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent<PlayerHealth>(out var playerHealth))
            {
                ulong playerOwnerId = playerHealth.OwnerClientId;

                // Add the PlayerHealth component to our server-side list if it's not already there.
                if (!playersInTrigger.ContainsKey(playerOwnerId))
                {
                    playersInTrigger.Add(playerOwnerId, playerHealth);
                    Debug.Log($"SERVER: Player {playerOwnerId} entered heal zone.");
                }
            }
        }
    }

    // OnTriggerExit is also a Unity message, so we guard it.
    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent<PlayerHealth>(out var playerHealth))
            {
                ulong playerOwnerId = playerHealth.OwnerClientId;

                // Remove the player from our list when they leave.
                if (playersInTrigger.ContainsKey(playerOwnerId))
                {
                    playersInTrigger.Remove(playerOwnerId);
                    Debug.Log($"SERVER: Player {playerOwnerId} exited heal zone.");
                }
            }
        }
    }
}
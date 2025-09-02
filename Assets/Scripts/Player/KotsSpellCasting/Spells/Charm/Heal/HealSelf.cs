using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HealSelf : SpellsClass
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // The server is the only one that should process the heal.
        if (IsServer)
        {
            // Heal the person who cast the spell (this object's owner).
            HealTarget(OwnerClientId);

            // Since the heal is instant, we can destroy the "heal effect" object right away.
            DestroySpell(gameObject);
        }
    }

    public void HealTarget(ulong targetClientId)
    {
        if (!IsServer) return;

        // Find the player object of the person we want to heal.
        NetworkObject targetNetObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(targetClientId);

        if (targetNetObj != null)
        {
            // Get their PlayerHealth component.
            if (targetNetObj.TryGetComponent<PlayerHealth>(out var playerHealth))
            {
                // Apply the healing directly on the server by calling TakeDamage with a negative value.
                float healAmount = SpellDataScriptableObject.healAmount;
                playerHealth.TakeDamage(-healAmount, this.OwnerClientId); // The "attackerId" is the healer's ID.
            }
            else
            {
                Debug.LogWarning($"HealTarget could not find PlayerHealth component on client {targetClientId}.");
            }
        }
        else
        {
            Debug.LogWarning($"Player NetworkObject for clientId {targetClientId} not found.");
        }
    }


    // Scenario 2: The spell is an Area of Effect (AoE) that heals players who touch it.
    private void OnTriggerEnter(Collider other)
    {
        // The server is the only one who should process the trigger.
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            // Get the ID of the player who entered the healing zone.
            ulong targetId = other.GetComponent<NetworkObject>().OwnerClientId;

            // You could add logic here to check if they are an ally before healing.

            // Heal them.
            HealTarget(targetId);
        }
    }
}

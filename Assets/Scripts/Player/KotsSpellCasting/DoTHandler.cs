// DoTHandler.cs (Refactored)
using DamageOverTimeEffect;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DoTHandler : NetworkBehaviour
{
    public List<DamageOverTime> currentDamageOverTimeList = new List<DamageOverTime>();

    private PlayerHealth playerHealth;

    public override void OnNetworkSpawn()
    {
        // Get the authoritative health component for this player
        playerHealth = GetComponent<PlayerHealth>();
    }

    private void FixedUpdate()
    {
        // Damage Over Time logic should ONLY run on the server.
        if (!IsServer) return;

        if (currentDamageOverTimeList.Count > 0)
        {
            ApplyDoTOnPlayer();
        }
    }

    void ApplyDoTOnPlayer()
    {
        for (int i = currentDamageOverTimeList.Count - 1; i >= 0; i--)
        {
            var dot = currentDamageOverTimeList[i];

            if (dot.TimeExpired)
            {
                currentDamageOverTimeList.RemoveAt(i);
            }
            else
            {
                if (dot.Timer())
                {
                    // --- THIS IS THE FIX ---
                    // Instead of talking to the UI, we tell the authoritative PlayerHealth script to take damage.
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(dot.DamagePerSecond, dot.AttackerId);
                    }
                }
            }
        }
    }
}
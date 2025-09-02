using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerStatusEffects : NetworkBehaviour
{
    // This list is the single source of truth for all active effects.
    // It is controlled by the server and synchronized to all clients.
    public NetworkList<StatusEffect> ActiveEffects;

    [SerializeField] private PlayerHealth playerHealth; // Reference to the health component

    private void Awake()
    {
        ActiveEffects = new NetworkList<StatusEffect>();
    }

    public override void OnNetworkSpawn()
    {
        // When the list changes on the server, the OnListChanged event fires on all clients.
        ActiveEffects.OnListChanged += OnEffectsListChanged;
    }

    private void FixedUpdate()
    {
        // Only the server should tick down timers and apply damage.
        if (!IsServer) return;

        // Iterate backwards because we might remove items
        for (int i = ActiveEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = ActiveEffects[i];
            effect.Duration -= Time.fixedDeltaTime;

            // Apply Damage-Over-Time if this is a DOT effect
            if (effect.Type == EffectType.Burn && playerHealth != null)
            {
                // This is a simplified tick timer. You can add your more complex timer logic here.
                playerHealth.TakeDamage(effect.DamagePerSecond * Time.fixedDeltaTime, effect.AttackerId);
            }

            if (effect.Duration <= 0)
            {
                // Remove the effect from the list when its timer runs out.
                ActiveEffects.RemoveAt(i);
            }
            else
            {
                // Update the list with the new duration
                ActiveEffects[i] = effect;
            }
        }
    }

    public bool HasEffect(EffectType type)
    {
        // Loop through the list of active effects
        foreach (var effect in ActiveEffects)
        {
            // If we find an effect of the matching type, return true
            if (effect.Type == type)
            {
                return true;
            }
        }
        // If we finish the loop without finding it, return false
        return false;
    }

    // This is the public method that spells will call on the server to apply an effect.
    public void ApplyEffect(StatusEffect newEffect)
    {
        if (!IsServer) return;

        // You can add logic here to check if an effect is already active, etc.
        ActiveEffects.Add(newEffect);
    }

    // This method runs on ALL CLIENTS whenever the list changes.
    // Use this to control visual effects and gameplay mechanics.
    private void OnEffectsListChanged(NetworkListEvent<StatusEffect> changeEvent)
    {
        Debug.Log($"Client {OwnerClientId}: Status effect list changed. Total effects: {ActiveEffects.Count}");

        // Example: Check if the player is slowed
        bool isSlowed = false;
        foreach (var effect in ActiveEffects)
        {
            if (effect.Type == EffectType.Slow)
            {
                isSlowed = true;
                break;
            }
        }

        // Tell the PlayerController to update its speed
        GetComponent<PlayerController>().SetSlowed(isSlowed);

        // Here you would also turn on/off particle effects, shaders, etc.
    }
}
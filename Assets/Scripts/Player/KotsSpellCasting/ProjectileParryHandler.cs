using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ProjectileParryHandler : NetworkBehaviour
{
    [SerializeField] private GameObject parryAnticipationCanvas;
    [SerializeField] private TextMeshProUGUI parryAnticipationLetterText;

    public event EventHandler OnAnyPlayerPerformedParry;

    public string ParryLetters { get; set; }

    public enum ParryState
    {
        NONE, // Added a default state
        ANTICIPATION,
        PARRIABLE
    }

    // Dictionary to track players and their current parry state for this projectile
    private Dictionary<ulong, ParryState> playerParryStates = new Dictionary<ulong, ParryState>();

    // No longer need trigger listeners, so Awake and the trigger methods are removed.

    internal void OnProjectileSpawned(string parryLetters)
    {
        parryAnticipationLetterText.text = parryLetters;
        this.ParryLetters = parryLetters;
        parryAnticipationCanvas.SetActive(true); // Assuming canvas is always visible when projectile is active
    }

    /// <summary>
    /// Called by the ProjectileClass to update a player's state based on distance.
    /// This is the new central method for state management.
    /// </summary>
    public void UpdatePlayerState(ulong playerId, ParryState newState, PlayerSpellParryManager playerManager)
    {
        // Get the current state, defaulting to NONE if the player isn't tracked yet
        playerParryStates.TryGetValue(playerId, out ParryState currentState);

        // If the state hasn't changed, do nothing. This is an important optimization.
        if (currentState == newState) return;

        // Update the state in our dictionary
        playerParryStates[playerId] = newState;

        // Notify the player's manager about the new state
        if (newState == ParryState.ANTICIPATION || newState == ParryState.PARRIABLE)
        {
            Debug.Log($"Player {playerId} state updated to {newState}");
            playerManager.AddOrUpdateParriableSpell(this, playerId, newState);

            // Optional: Update UI visual cues based on the new state
            parryAnticipationLetterText.color = newState == ParryState.PARRIABLE ? Color.yellow : Color.white;
        }
    }

    /// <summary>
    /// Called by the ProjectileClass when a player is no longer in range.
    /// </summary>
    public void RemovePlayerFromRange(ulong playerId, PlayerSpellParryManager playerManager)
    {
        if (playerParryStates.ContainsKey(playerId))
        {
            Debug.Log($"Player {playerId} exited range.");
            // Get the last known state to correctly remove it from the manager
            ParryState lastState = playerParryStates[playerId];
            playerManager.RemoveSpellState(this.NetworkObjectId, playerId, lastState);
            playerParryStates.Remove(playerId);

            // Optional: Reset UI
            parryAnticipationLetterText.color = Color.white;
        }
    }

    internal void Parry()
    {
        Debug.Log($"[ParryHandler {this.NetworkObjectId}]: Parry() called. Invoking OnAnyPlayerPerformedParry event.");
        OnAnyPlayerPerformedParry?.Invoke(this, EventArgs.Empty);
    }
}
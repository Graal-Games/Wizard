using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class ProjectileParryHandler : NetworkBehaviour
{
    [SerializeField] private GameObject parryAnticipationCanvas;
    [SerializeField] private TextMeshProUGUI parryAnticipationLetterText;

    // This event is now only invoked ON THE SERVER.
    public event EventHandler OnAnyPlayerPerformedParry;

    public NetworkVariable<FixedString32Bytes> ParryLetters = new NetworkVariable<FixedString32Bytes>();

    public enum ParryState { NONE, ANTICIPATION, PARRIABLE }

    private Dictionary<ulong, ParryState> playerParryStates = new Dictionary<ulong, ParryState>();





    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        ParryLetters.OnValueChanged += OnParryLetterChanged;
        OnParryLetterChanged(default, ParryLetters.Value);
    }





    // Change the displayed letter on top of the gameObject once set or changed
    private void OnParryLetterChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        if (!string.IsNullOrEmpty(newValue.ToString()))
        {
            parryAnticipationLetterText.text = newValue.ToString();
            parryAnticipationCanvas.SetActive(true);
        }
    }






    internal void OnProjectileSpawned(string parryLetters)
    {
        if (!IsServer) return;
        ParryLetters.Value = parryLetters;
    }





    public void UpdatePlayerState(ulong playerId, ParryState newState, PlayerSpellParryManager playerManager)
    {
        if (!IsServer) return;
        playerParryStates.TryGetValue(playerId, out ParryState currentState);
        if (currentState == newState) return;
        playerParryStates[playerId] = newState;

        if (newState == ParryState.ANTICIPATION || newState == ParryState.PARRIABLE)
        {
            playerManager.Server_UpdateParryStateForPlayer(new NetworkObjectReference(this.NetworkObject), playerId, newState);
            // Color change is a visual flair, can be client-side if needed, but fine here for now.
            parryAnticipationLetterText.color = newState == ParryState.PARRIABLE ? Color.yellow : Color.white;
        }
    }





    public void RemovePlayerFromRange(ulong playerId, PlayerSpellParryManager playerManager)
    {
        if (!IsServer) return;
        if (playerParryStates.ContainsKey(playerId))
        {
            playerManager.Server_RemoveParryStateForPlayer(this.NetworkObjectId, playerId);
            playerParryStates.Remove(playerId);
            parryAnticipationLetterText.color = Color.white;
        }
    }





    /// <summary>
    /// This is called by the CLIENT's PlayerSpellParryManager after a successful local check.
    /// Its only job is to tell the server about the parry.
    /// </summary>
    internal void Parry()
    {
        // This method now securely calls the server to execute the parry.
        ParryServerRpc();
    }





    /// <summary>
    /// This RPC is sent from a client to the server to report a successful parry.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void ParryServerRpc(ServerRpcParams rpcParams = default)
    {
        Debug.Log($"[SERVER ProjectileHandler {NetworkObjectId}]: Received parry request from Client {rpcParams.Receive.SenderClientId}.");

        // Now, on the server, invoke the event that the ProjectileClass is listening for.
        OnAnyPlayerPerformedParry?.Invoke(this, EventArgs.Empty);
    }
}
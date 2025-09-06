using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static K_SpellLauncher;

public class PlayerSpellParryManager : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private ParryLetterAnticipationElement parryLetterAnticipation;
    [UDictionary.Split(20, 80)] public DrUiKeys parryUiKeysDictionary;

    [Header("Parry Settings")]
    [SerializeField] private float parryCooldownDuration = 2f;

    private bool isParryOnCooldown = false;

    // Static dictionary for easy server-side lookup
    public static Dictionary<ulong, PlayerSpellParryManager> managers = new Dictionary<ulong, PlayerSpellParryManager>();

    // Dictionaries for local client state
    private Dictionary<ulong, ProjectileParryHandler> spellsParryHandlerDictionary = new Dictionary<ulong, ProjectileParryHandler>();
    private Dictionary<ulong, ProjectileParryHandler.ParryState> spellParryStateDictionary = new Dictionary<ulong, ProjectileParryHandler.ParryState>();


    #region Network Lifecycle
    public override void OnNetworkSpawn()
    {
        // Hide local UI on spawn for the owner
        if (IsOwner)
        {
            parryLetterAnticipation.hideParryLetter();
        }

        // Register this manager on the server so projectiles can find it
        if (IsServer)
        {
            managers[OwnerClientId] = this;
        }
    }

    public override void OnNetworkDespawn()
    {
        // Clean up the static dictionary when the player despawns
        if (IsServer)
        {
            managers.Remove(OwnerClientId);
        }
    }
    #endregion


    #region Caster Hint Logic (Server-Authoritative)

    /// <summary>
    /// SERVER-SIDE ENTRY POINT: Called by K_SpellLauncher to show the casting hint to opponents.
    /// </summary>
    public void Server_ShowCastingHint(string parryLetter)
    {
        if (!IsServer) return;

        // This RPC will go to all clients. The logic inside filters it to non-owners.
        Client_ShowCastingHintClientRpc(parryLetter);
    }

    /// <summary>
    /// SERVER-SIDE ENTRY POINT: Called by K_SpellLauncher to hide the casting hint from everyone.
    /// </summary>
    public void Server_HideCastingHint()
    {
        if (!IsServer) return;
        Client_HideCastingHintClientRpc();
    }

    [ClientRpc]
    private void Client_ShowCastingHintClientRpc(string parryLetter)
    {
        // Don't show the caster their own hint UI this way.
        if (IsOwner) return;

        parryLetterAnticipation.showParryLetter(parryLetter);
    }

    [ClientRpc]
    private void Client_HideCastingHintClientRpc()
    {
        // Everyone's hint UI for this player should be hidden.
        parryLetterAnticipation.hideParryLetter();
    }

    #endregion


    #region Server-to-Client Communication (The Correct Flow)

    /// <summary>
    /// SERVER-SIDE ENTRY POINT: Called by projectiles to update a player's parry state.
    /// </summary>
    public void Server_UpdateParryStateForPlayer(NetworkObjectReference spellHandlerRef, ulong targetPlayerId, ProjectileParryHandler.ParryState newState)
    {
        if (!IsServer) return;

        var clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { targetPlayerId } }
        };
        Client_UpdateParryStateClientRpc(spellHandlerRef, newState, clientRpcParams);
    }

    /// <summary>
    /// SERVER-SIDE ENTRY POINT: Called by projectiles when a player is no longer in range.
    /// </summary>
    public void Server_RemoveParryStateForPlayer(ulong spellId, ulong targetPlayerId)
    {
        if (!IsServer) return;

        var clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { targetPlayerId } }
        };
        Client_RemoveParryStateClientRpc(spellId, clientRpcParams);
    }

    [ClientRpc]
    private void Client_UpdateParryStateClientRpc(NetworkObjectReference spellHandlerRef, ProjectileParryHandler.ParryState newState, ClientRpcParams clientRpcParams = default)
    {
        if (spellHandlerRef.TryGet(out NetworkObject spellNetworkObject))
        {
            ProjectileParryHandler handler = spellNetworkObject.GetComponent<ProjectileParryHandler>();
            if (handler != null)
            {
                Local_AddOrUpdateParriableSpell(handler, newState);
            }
        }
    }

    [ClientRpc]
    private void Client_RemoveParryStateClientRpc(ulong spellId, ClientRpcParams clientRpcParams = default)
    {
        Local_RemoveParriableSpell(spellId);
    }

    #endregion


    #region Local Client Logic (UI and State)

    /// <summary>
    /// Runs on the client to update local dictionaries and refresh the UI.
    /// </summary>
    private void Local_AddOrUpdateParriableSpell(ProjectileParryHandler spell, ProjectileParryHandler.ParryState newState)
    {
        if (!IsOwner) return;

        ulong spellId = spell.NetworkObjectId;
        spellsParryHandlerDictionary[spellId] = spell;
        spellParryStateDictionary[spellId] = newState;

        // Refresh the UI. This will respect the cooldown state.
        UpdateAnticipationSpellParryKeys();
    }

    /// <summary>
    /// Runs on the client to remove a spell and refresh the UI.
    /// </summary>
    private void Local_RemoveParriableSpell(ulong spellNetworkObjectId)
    {
        if (!IsOwner) return;

        spellsParryHandlerDictionary.Remove(spellNetworkObjectId);
        spellParryStateDictionary.Remove(spellNetworkObjectId);

        UpdateAnticipationSpellParryKeys();
    }

    private void UpdateAnticipationSpellParryKeys()
    {
        // If on cooldown, ensure all keys are hidden and do nothing else.
        if (isParryOnCooldown)
        {
            DeactivateSpellParryKeys();
            return;
        }

        DeactivateSpellParryKeys();
        foreach (var kvp in spellParryStateDictionary)
        {
            if (!spellsParryHandlerDictionary.ContainsKey(kvp.Key)) continue;

            ProjectileParryHandler handler = spellsParryHandlerDictionary[kvp.Key];

            string parryLetter = handler.ParryLetters.Value.ToString();
            if (string.IsNullOrEmpty(parryLetter)) continue;

            if (kvp.Value == ProjectileParryHandler.ParryState.PARRIABLE)
            {
                ActivateSpellParryKey(parryLetter);
            }
            else // ANTICIPATION
            {
                AnticipationSpellParryKey(parryLetter);
            }
        }
    }
    #endregion


    #region UI and Input Handling

    public void TryToParry(string inputParryLetters)
    {
        if (!IsOwner || isParryOnCooldown) return;

        ProjectileParryHandler spellToParry = null;

        foreach (var kvp in spellParryStateDictionary)
        {
            if (kvp.Value == ProjectileParryHandler.ParryState.PARRIABLE)
            {
                ProjectileParryHandler spell = spellsParryHandlerDictionary[kvp.Key];
                if (spell.ParryLetters.Value.ToString() == inputParryLetters)
                {
                    spellToParry = spell;
                    break;
                }
            }
        }

        if (spellToParry != null)
        {
            Debug.Log($"<color=lime>[CLIENT {OwnerClientId}]:</color> Parry SUCCESSFUL!");
            spellToParry.Parry();
            Local_RemoveParriableSpell(spellToParry.NetworkObjectId);
        }
        else
        {
            Debug.Log($"[CLIENT {OwnerClientId}]: Parry FAILED. Starting cooldown.");
            StartCoroutine(ParryCooldownCoroutine());
        }
    }

    /// <summary>
    /// A coroutine to handle the 2-second parry cooldown on the client.
    /// </summary>
    private IEnumerator ParryCooldownCoroutine()
    {
        isParryOnCooldown = true;

        // Immediately hide all parry hints.
        DeactivateSpellParryKeys();

        // Wait for the specified duration.
        yield return new WaitForSeconds(parryCooldownDuration);

        isParryOnCooldown = false;

        // After the cooldown, refresh the UI. This will show any parry keys
        // for projectiles that are still in range.
        UpdateAnticipationSpellParryKeys();
    }

    private void SetupParryKey(string parryLetter, bool isActive)
    {
        if (parryUiKeysDictionary.TryGetValue(parryLetter, out var scKey))
        {
            scKey.gameObject.SetActive(true);
            scKey.SetActive(isActive);
        }
    }

    private void AnticipationSpellParryKey(string parryLetter) => SetupParryKey(parryLetter, false);
    private void ActivateSpellParryKey(string parryLetter) => SetupParryKey(parryLetter, true);

    private void DeactivateSpellParryKeys()
    {
        foreach (var scKey in parryUiKeysDictionary.Values)
        {
            if (scKey.gameObject.activeSelf)
            {
                scKey.gameObject.SetActive(false);
            }
        }
    }

    #endregion
}

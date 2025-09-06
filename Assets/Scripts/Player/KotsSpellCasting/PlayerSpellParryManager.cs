using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static K_SpellLauncher;

public class PlayerSpellParryManager : NetworkBehaviour
{
    [SerializeField] private ParryLetterAnticipationElement parryLetterAnticipation;
    
    [UDictionary.Split(20, 80)] public DrUiKeys parryUiKeysDictionary;

    private Dictionary<ulong, ProjectileParryHandler> spellsParryHandlerDictionary = new Dictionary<ulong, ProjectileParryHandler>();
    private Dictionary<ulong, ProjectileParryHandler.ParryState> spellParryStateDictionary = new Dictionary<ulong, ProjectileParryHandler.ParryState>();
    public override void OnNetworkSpawn()
    {
        if (IsOwner) // Ensure UI is initialized only for the owner
        {
            parryLetterAnticipation.hideParryLetter();
        }
    }

    #region Handle player generated parry letter anticipation

    public string GeneratePlayerParryAnticipation(string spellSequence)
    {
        if (IsOwner)
        {
            // Generate the parry letter locally for the owner
            System.Random random = new System.Random();
            int res = random.Next(0, K_SpellKeys.spellTypes.Length);
            string parryLetter = K_SpellKeys.spellTypes[res].ToString();

            Debug.Log($"Owner {OwnerClientId} generated parry letter: {parryLetter}");

            // Notify the server to update all other players
            NotifyParryLetterGeneratedServerRpc(parryLetter);

            return parryLetter; // Return the generated letter for the owner's use
        }

        return string.Empty; // Non-owners should not generate the parry letter
    }

    [ServerRpc(RequireOwnership = false)]
    private void NotifyParryLetterGeneratedServerRpc(string parryLetter)
    {
        Debug.Log($"Server received parry letter '{parryLetter}' from player {OwnerClientId}.");
        ShowParryLetterForOtherPlayersClientRpc(parryLetter);
    }

    [ClientRpc]
    private void ShowParryLetterForOtherPlayersClientRpc(string parryLetter)
    {
        if (IsOwner) return; // Skip showing the parry letter for the owner

        Debug.Log($"Showing parry letter '{parryLetter}' for other players.");
        parryLetterAnticipation.showParryLetter(parryLetter); // Show the letter for non-owners
    }


    public void HidePlayerParryAnticipation()
    {
        if (IsOwner)
        {
            Debug.Log($"Owner {OwnerClientId} requesting to hide parry letters for all players.");
            HideParryLetterServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void HideParryLetterServerRpc()
    {
        Debug.Log($"Server received request to hide parry letters from player {OwnerClientId}.");
        HideParryLetterForAllPlayersClientRpc();
    }

    [ClientRpc]
    private void HideParryLetterForAllPlayersClientRpc()
    {
        Debug.Log($"Hiding parry letter on player {OwnerClientId}'s client.");
        parryLetterAnticipation.hideParryLetter();
    }

    #endregion

    #region Handle spell parry and parry keys

    public void AddOrUpdateParriableSpell(ProjectileParryHandler spell, ulong triggeringPlayerId, ProjectileParryHandler.ParryState newState)
    {
        if (OwnerClientId != triggeringPlayerId || !IsOwner) return;

        ulong spellId = spell.NetworkObjectId;

        Debug.Log($"[Player {OwnerClientId} Manager]: State Received for Spell {spellId}. New State: {newState}");

        spellsParryHandlerDictionary[spellId] = spell;
        spellParryStateDictionary[spellId] = newState; // Just assign the new state directly

        UpdateAnticipationSpellParryKeys();
    }

    public void RemoveSpellState(ulong spellNetworkObjectId, ulong triggeringPlayerId, ProjectileParryHandler.ParryState parryState)
    {
        if (OwnerClientId != triggeringPlayerId || !IsOwner) return;

        // We only need to remove the spell completely now, as there are no sub-states to manage
        RemoveParriableSpell(spellNetworkObjectId);
    }

    private void RemoveParriableSpell(ulong spellNetworkObjectId)
    {
        spellsParryHandlerDictionary.Remove(spellNetworkObjectId);
        spellParryStateDictionary.Remove(spellNetworkObjectId);

        UpdateAnticipationSpellParryKeys();
    }

    private void UpdateAnticipationSpellParryKeys()
    {
        DeactivateSpellParryKeys();
        foreach (var kvp in spellParryStateDictionary)
        {
            ulong spellId = kvp.Key;
            ProjectileParryHandler.ParryState state = kvp.Value;

            if (!spellsParryHandlerDictionary.ContainsKey(spellId)) continue;

            ProjectileParryHandler handler = spellsParryHandlerDictionary[spellId];

            if (state == ProjectileParryHandler.ParryState.PARRIABLE)
            {
                ActivateSpellParryKey(handler.ParryLetters);
            }
            else // It must be ANTICIPATION
            {
                AnticipationSpellParryKey(handler.ParryLetters);
            }
        }
    }

    public void TryToParry(string inputParryLetters)
    {
        if (!IsOwner) return;

        Debug.Log($"[Player {OwnerClientId} Manager]: --- PARRY ATTEMPT with key '{inputParryLetters}' ---");

        List<ulong> spellIdsToParry = new List<ulong>();

        foreach (var kvp in spellParryStateDictionary)
        {
            if (kvp.Value == ProjectileParryHandler.ParryState.PARRIABLE)
            {
                ProjectileParryHandler spell = spellsParryHandlerDictionary[kvp.Key];
                if (spell.ParryLetters == inputParryLetters)
                {
                    Debug.Log($"Player successfully parried letter => {inputParryLetters}");
                    spell.Parry();
                    spellIdsToParry.Add(kvp.Key);
                }
            }
        }

        if (spellIdsToParry.Count > 0)
        {
            foreach (var spellId in spellIdsToParry)
            {
                RemoveParriableSpell(spellId);
            }
        }
        else
        {
            Debug.Log("Parry failed, no matching projectiles.");
        }
    }

    private void SetupParryKey(string parryLetter, bool isActive)
    {
        if (parryUiKeysDictionary.TryGetValue(parryLetter, out var scKey))
        {
            scKey.invisible = false;
            scKey.buffered = false;
            scKey.gameObject.SetActive(true);
            scKey.SetActive(isActive); // This is the only difference
        }
    }

    private void AnticipationSpellParryKey(string parryLetter)
    {
        SetupParryKey(parryLetter, false);
    }

    private void ActivateSpellParryKey(string parryLetter)
    {
        SetupParryKey(parryLetter, true);
    }

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

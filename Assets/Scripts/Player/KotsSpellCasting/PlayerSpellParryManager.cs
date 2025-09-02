using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static K_SpellLauncher;

internal class PlayerSpellParryManager : NetworkBehaviour
{
    [SerializeField] private ParryLetterAnticipationElement parryLetterAnticipation;
    
    [UDictionary.Split(20, 80)] public DrUiKeys parryUiKeysDictionary;

    private Dictionary<ulong, ProjectileParryHandler> spellsParryHandlerDictionary = new Dictionary<ulong, ProjectileParryHandler>();
    private Dictionary<ulong, HashSet<ProjectileParryHandler.ParryState>> spellParryStateDictionary = new Dictionary<ulong, HashSet<ProjectileParryHandler.ParryState>>();

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

    public void AddOrUpdateParriableSpell(ProjectileParryHandler spell, ulong triggeringPlayerId, ProjectileParryHandler.ParryState parryState)
    {
        if (OwnerClientId != triggeringPlayerId || !IsOwner) return;

        ulong spellId = spell.NetworkObjectId;

        // clear spellParryStateDictionary
        if (!spellsParryHandlerDictionary.ContainsKey(spellId) && spellParryStateDictionary.ContainsKey(spellId))
        {
            spellParryStateDictionary.Remove(spellId);
        }

        if (!spellsParryHandlerDictionary.ContainsKey(spellId))
        {
            spellsParryHandlerDictionary[spellId] = spell;
            spellParryStateDictionary[spellId] = new HashSet<ProjectileParryHandler.ParryState>();
        }

        spellParryStateDictionary[spellId].Add(parryState);

        UpdateAnticipationSpellParryKeys();
    }

    public void RemoveSpellState(ulong spellNetworkObjectId, ulong triggeringPlayerId, ProjectileParryHandler.ParryState parryState)
    {
        if (OwnerClientId != triggeringPlayerId || !IsOwner) return;

        if (!spellsParryHandlerDictionary.ContainsKey(spellNetworkObjectId)) {
            spellParryStateDictionary.Remove(spellNetworkObjectId);
        }

        if (spellParryStateDictionary.ContainsKey(spellNetworkObjectId)) {
            spellParryStateDictionary[spellNetworkObjectId].Remove(parryState);
        }

        if (spellParryStateDictionary[spellNetworkObjectId].Count == 0) {
            RemoveParriableSpell(spellNetworkObjectId);
            return;
        }

        UpdateAnticipationSpellParryKeys();
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
        foreach (var spellState in spellParryStateDictionary)
        {
            if (!spellsParryHandlerDictionary.ContainsKey(spellState.Key))
                continue;
               

            if (spellState.Value.Contains(ProjectileParryHandler.ParryState.PARRIABLE))
            {
                ActivateSpellParryKey(spellsParryHandlerDictionary[spellState.Key].ParryLetters);
            }
            else
            {
                AnticipationSpellParryKey(spellsParryHandlerDictionary[spellState.Key].ParryLetters);
            }
        }
    }

    public void TryToParry(string inputParryLetters)
    {
        if (string.IsNullOrEmpty(inputParryLetters))
        {
            Debug.LogWarning("parry letter is null or empty, we can't look it up.");
            // If the parry letter is null or empty, we can't look it up.
            // We'll just ignore it and stop the method here.
            return;
        }

        HashSet<ulong> parriedSpells = new HashSet<ulong>();

        
        foreach (var spellState in spellParryStateDictionary) {

            if (spellsParryHandlerDictionary.ContainsKey(spellState.Key) && spellState.Value.Contains(ProjectileParryHandler.ParryState.PARRIABLE)) {

                ProjectileParryHandler spell = spellsParryHandlerDictionary[spellState.Key];

                if (spell.ParryLetters == inputParryLetters)
                {
                    Debug.Log($"Player successfully parried letter => ${inputParryLetters}");
                    spell.Parry();
                    parriedSpells.Add(spellState.Key);
                }
            }
        }

        bool isSuccessfull = parriedSpells.Count != 0;

        if (isSuccessfull)
        {
            foreach (var spellId in parriedSpells)
            {
                RemoveParriableSpell(spellId);
            }
            return;
        }

        // TODO what do we want to do if not successfull
        Debug.Log("Parry failed, no matching projectiles.");
    }

    private void AnticipationSpellParryKey(string parryLetter)
    {
        if (string.IsNullOrEmpty(parryLetter))
        {
            Debug.LogWarning("parry letter is null or empty, we can't look it up.");
            // If the parry letter is null or empty, we can't look it up.
            // We'll just ignore it and stop the method here.
            return;
        }

        if (parryUiKeysDictionary.TryGetValue(parryLetter, out var scKey))
        {
            scKey.invisible = false;
            scKey.buffered = false;
            scKey.gameObject.SetActive(true);
            scKey.SetActive(false);
        }
    }

    private void ActivateSpellParryKey(string parryLetter)
    {
        if (string.IsNullOrEmpty(parryLetter))
        {
            Debug.LogWarning("parry letter is null or empty, we can't look it up.");
            // If the parry letter is null or empty, we can't look it up.
            // We'll just ignore it and stop the method here.
            return;
        }

        if (parryUiKeysDictionary.TryGetValue(parryLetter, out var scKey))
        {
            scKey.invisible = false;
            scKey.buffered = false;
            scKey.gameObject.SetActive(true);
            scKey.SetActive(true);
        }
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

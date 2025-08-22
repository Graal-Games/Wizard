using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ProjectileParryHandler : NetworkBehaviour
{
    [SerializeField] private GameObject parryAnticipationCanvas;
    [SerializeField] private TextMeshProUGUI parryAnticipationLetterText;

    [SerializeField] private TriggerListener anticipationTrigger;
    [SerializeField] private TriggerListener parryTrigger;

    public event EventHandler OnAnyPlayerPerformedParry;

    private string parryLetters;

    public string ParryLetters
    {
        get { return parryLetters; }
        set { parryLetters = value; }
    }

    public enum ParryState {
        ANTICIPATION, PARRIABLE
    }

    // Dictionary to track players and their states
    private Dictionary<ulong, string> playerStatesDictionary = new Dictionary<ulong, string>();

    void Awake()
    {
        anticipationTrigger.OnEnteredTrigger += OnAnticipationTriggerEntered;
        anticipationTrigger.OnExitedTrigger += OnAnticipationTriggerExited;
        parryTrigger.OnEnteredTrigger += OnParryTriggerEntered;
        parryTrigger.OnExitedTrigger += OnParryTriggerExited;
    }

    internal void OnProjectileSpawned(string parryLetters)
    {
        parryAnticipationLetterText.text = parryLetters;
        this.parryLetters = parryLetters;
        parryAnticipationCanvas.SetActive(true);
    }

    void OnAnticipationTriggerEntered(Collider collider)
    {
        if (!collider.gameObject.CompareTag("Player")) return;

        if (collider.TryGetComponent<NetworkObject>(out var networkObject))
        {
            ulong playerId = networkObject.OwnerClientId;

            // Add to player states as "anticipation"
            playerStatesDictionary[playerId] = "anticipation";

            Debug.Log($"Player {playerId} entered anticipation trigger.");

            //parryAnticipationCanvas.SetActive(true);

            if (collider.transform.TryGetComponent(out PlayerSpellParryManager playerSpellParryManager))
            {
                playerSpellParryManager.AddOrUpdateParriableSpell(this, playerId, ParryState.ANTICIPATION);
            }
        }
    }

    void OnAnticipationTriggerExited(Collider collider)
    {
        if (!collider.gameObject.CompareTag("Player")) return;

        if (collider.TryGetComponent<NetworkObject>(out var networkObject))
        {
            ulong playerId = networkObject.OwnerClientId;

            // Remove from player states
            playerStatesDictionary.Remove(playerId);

            Debug.Log($"Player {playerId} exited anticipation trigger.");

            //parryAnticipationCanvas.SetActive(false);

            if (collider.transform.TryGetComponent(out PlayerSpellParryManager playerSpellParryManager))
            {
                playerSpellParryManager.RemoveSpellState(this.NetworkObjectId, playerId, ParryState.ANTICIPATION);
            }
        }
    }

    void OnParryTriggerEntered(Collider collider)
    {
        if (!collider.gameObject.CompareTag("Player")) return;

        if (collider.TryGetComponent<NetworkObject>(out var networkObject))
        {
            ulong playerId = networkObject.OwnerClientId;

            // Update state to "parry"
            //playerStatesDictionary[playerId] = "parry";

            Debug.Log($"Player {playerId} entered parry trigger.");

            //
            //parryAnticipationLetterText.color = Color.yellow;
            //parryAnticipationCanvas.SetActive(true);

            if (collider.transform.TryGetComponent(out PlayerSpellParryManager playerSpellParryManager))
            {
                playerSpellParryManager.AddOrUpdateParriableSpell(this, playerId, ParryState.PARRIABLE);
            }
        }
    }

    void OnParryTriggerExited(Collider collider)
    {
        if (!collider.gameObject.CompareTag("Player")) return;

        if (collider.TryGetComponent<NetworkObject>(out var networkObject))
        {
            ulong playerId = networkObject.OwnerClientId;

            Debug.Log($"Player {playerId} exited parry trigger.");

            //parryAnticipationLetterText.color = Color.white;
            //parryAnticipationCanvas.SetActive(false);

            if (collider.transform.TryGetComponent(out PlayerSpellParryManager playerSpellParryManager))
            {
                playerSpellParryManager.RemoveSpellState(this.NetworkObjectId, playerId, ParryState.PARRIABLE);
            }
        }
    }

    internal void Parry()
    {
        OnAnyPlayerPerformedParry?.Invoke(this, EventArgs.Empty);
    }
}

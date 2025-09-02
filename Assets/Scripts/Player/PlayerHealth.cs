using UnityEngine;

using Unity.Netcode;
using System; // Needed for the Action event

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 500;

    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // This is a local event for this specific player's UI to listen to.
    public event Action<int> OnHealthChanged;

    private Scoreboard scoreboard;
    private ulong lastAttackerId;
    private bool isDead = false;

    public override void OnNetworkSpawn()
    {
        scoreboard = FindObjectOfType<Scoreboard>();

        CurrentHealth.OnValueChanged += HealthUpdated;

        if (IsServer)
        {
            CurrentHealth.Value = maxHealth;
        }

        HealthUpdated(0, CurrentHealth.Value);
    }

    public override void OnNetworkDespawn()
    {
        CurrentHealth.OnValueChanged -= HealthUpdated;
    }

    private void HealthUpdated(int previousValue, int newValue)
    {
        OnHealthChanged?.Invoke(newValue);
    }

    public void TakeDamage(float damage, ulong attackerId)
    {
        if (!IsServer) return;
        if (isDead) return;

        lastAttackerId = attackerId;
        CurrentHealth.Value -= (int)damage;

        // Prepare to send an RPC ONLY to the owner of this player object
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { OwnerClientId }
            }
        };
        // Call the ClientRpc on this player's NewPlayerBehavior script
        GetComponent<NewPlayerBehavior>().ShowDamageEffectClientRpc("Blood", 1f, clientRpcParams);

        if (CurrentHealth.Value <= 0)
        {
            CurrentHealth.Value = 0;
            isDead = true;

            if (scoreboard != null)
            {
                scoreboard.PlayerScoredServerRpc(lastAttackerId);
            }

            StartCoroutine(RespawnRoutine());
        }
    }

    private System.Collections.IEnumerator RespawnRoutine()
    {
        // Tell the client to play a death effect, etc.
        // ...
        HandleDeathClientRpc();

        yield return new WaitForSeconds(3.0f); // 3 second respawn timer

        Vector3 spawnPosition = SpawnManager.Instance.AssignSpawnPoint(OwnerClientId);
        Quaternion spawnRotation = SpawnManager.Instance.AssignSpawnRotation(OwnerClientId);

        RespawnClientRpc(spawnPosition, spawnRotation);

        isDead = false;
        CurrentHealth.Value = maxHealth;
    }

    [ClientRpc]
    private void HandleDeathClientRpc(ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"Player {OwnerClientId} has died.");
        // Example: disable player controller on the owner's client
        if (IsOwner) GetComponent<PlayerController>().enabled = false;
    }

    [ClientRpc]
    private void RespawnClientRpc(Vector3 spawnPosition, Quaternion spawnRotation, ClientRpcParams clientRpcParams = default)
    {
        // This runs on the specific client, telling them to move their character.
        // We set kinematic temporarily to prevent physics glitches during the move.
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;
            rb.isKinematic = false;
        }
        // Re-enable controls on the owner's client
        if (IsOwner) GetComponent<PlayerController>().enabled = true;
    }
}

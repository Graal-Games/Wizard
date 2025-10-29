using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static K_SpellBuilder;

// CURRENTLY HEALS ALL PLAYERS IN ITS TRIGGER. TO MAKE IT SO THAT ONLY THE FIRST PLAYER IN IT IS HEALED??? - on second thoughts maybe not
// NOT CURRENTLY A PRIORITY <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
public class HealTargetScepter : SpellsClass
{
    private float timer = 0f;
    private float interval = 3f;

    GameObject player; // Reference to the player GameObject

    Collider triggerZone;

    // TO CHANGE THIS TO A NETWORK LIST
    Dictionary<ulong, GameObject> playersInTrigger = new Dictionary<ulong, GameObject>();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        triggerZone = GetComponent<Collider>();

        CheckIfPlayerInsideTrigger();
    }





    void OnDrawGizmos()
    {
        if (triggerZone != null)
        {
            // Use the same center and radius as your OverlapSphere
            Vector3 center = triggerZone.bounds.center;
            float radius = triggerZone.bounds.extents.magnitude;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(center, radius);
        }
    }





    void CheckIfPlayerInsideTrigger()
    {
        Collider[] colliders = Physics.OverlapSphere(triggerZone.bounds.center, triggerZone.bounds.extents.magnitude);

        foreach (Collider collider in colliders) 
        {
            Debug.Log("colliders: " + collider);

            if (collider.gameObject.name.Contains("Player") && playersInTrigger.Count < 1)
            {
                ulong playerOwnerId = collider.GetComponent<NewPlayerBehavior>().OwnerClientId;

                if (!playersInTrigger.TryGetValue(playerOwnerId, out GameObject playerGo))
                {
                    Debug.Log("Player is already inside the trigger on spawn!");
                    playersInTrigger.Add(playerOwnerId, collider.gameObject);
                    return;
                }
            }
        }
        Debug.Log("Player is not inside the trigger on spawn.");
    }





    public override void FixedUpdate()
    {
        base.FixedUpdate();

        timer += Time.fixedDeltaTime;

        if (timer >= interval)
        {
            // Your action here
            Debug.Log("3 seconds have passed! Do something.");

            if (playersInTrigger.Count > 0)
            {
                //CheckIfPlayerInsideTrigger();

                if (playersInTrigger.Count == 0) return;

                foreach (var entry in playersInTrigger)
                {
                    ulong clientId = entry.Key;
                    GameObject player = entry.Value;

                    Debug.Log($"Player {clientId} in trigger: {player.name}");

                    if (player.TryGetComponent(out NewPlayerBehavior behavior))
                    {
                        behavior.Heal(SpellDataScriptableObject.healAmount);
                    }
                }
            }

            // Reset the timer
            timer = 0f;
        }
    }





    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"ENTERED trigger: {other.name}");

        ulong playerOwnerId = other.gameObject.GetComponent<NewPlayerBehavior>().OwnerClientId;

        if (!playersInTrigger.ContainsKey(playerOwnerId))
        {
            playersInTrigger.Add(playerOwnerId, other.gameObject);
        }
    }





    void OnTriggerExit(Collider other)
    {
        Debug.Log($"222222exited trigger: {other.name}");

        ulong playerOwnerId = other.gameObject.GetComponent<NewPlayerBehavior>().OwnerClientId;
        //Debug.Log($"111111Player {playerOwnerId} exited trigger: {player.name}");

        if (other.gameObject.name.Contains("Player"))
        {
            Debug.Log($"222222Player {playerOwnerId} exited trigger: {other.name}");

            playersInTrigger.Remove(playerOwnerId);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Mist : NetworkBehaviour
{
    public float damage = 10f;
    public float movementSlowAmount = 2.5f;
    public float slowTime = 2;
    public bool hasHitShield;

    public delegate void MistStatus(ulong clientId, NetworkObjectReference spellObj, NetworkBehaviour spellNetBehavior, bool status);
    public static event MistStatus mistStatus;



    public override void OnNetworkDespawn()
    {
        // Might not need this
        if (!IsOwner) return;

        //Debug.LogFormat($"<color=red>DESpawned</color>");

        if (mistStatus != null) mistStatus(OwnerClientId, this.NetworkObject, this.GetNetworkBehaviour(this.NetworkBehaviourId), false);

        base.OnNetworkDespawn();
    }



    // Gott make it so that the aoe can only affect the player once
    //public bool hasCollided;
    void Awake()
    {
        StartCoroutine(TimeUntilDestroyed());
        //StartCoroutine(ActivateEffect());

        //Debug.LogFormat($"<color=green>{this.gameObject.GetComponent<Collider>()}</color>");
    }

    IEnumerator TimeUntilDestroyed()
    {
        yield return new WaitForSeconds(20);
        DestroyThisServerRpc();  
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyThisServerRpc()
    {
        NetworkObject.Despawn(this.gameObject);
        return;
    }

    IEnumerator ActivateEffect()
    {
        yield return new WaitForSeconds(1);
        this.gameObject.GetComponent<Collider>().enabled = true;
        
        // Change color
    }

    void OnTriggerEnter(Collider other)
    {
        // This needs to be handled by the player and not here if
        //the effect is not to be global
        if (other.gameObject.name.Contains("SphereShield"))
        {
            hasHitShield = true;
        } 

        if (other.gameObject.name.Contains("Player"))
        {
            Debug.Log("Player");

        }
        // if the player comes in contact
        // Reduce the player's health and speed
    }
}

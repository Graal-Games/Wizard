using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class FireAoe : NetworkBehaviour
{
    public float fireAoeDamage = 6f;
    //public float movementSlowAmount = 2.5f;
    public bool hasHitShield;
    float timeUntilSpellIsActive = 1f;
    float timeUntilSpellHasEnded = 13f;

    float applyDamageAtInterval = 0.5f;

    int dotPersistanceTime = 4;


    public delegate void AoeLifeStatus(ulong clientId, NetworkObjectReference spellObj, NetworkBehaviour spellNetBehavior, bool status, NetworkObject netObj);
    public static event AoeLifeStatus fireAoeExists;



    public float FireAoeDamage
    {
        get { return fireAoeDamage; } 
    }

    public int DotPersistanceTime
    {
        get { return dotPersistanceTime; }
    }

    public float ApplyDamageAtInterval
    {
        get { return applyDamageAtInterval; }
    }


    // Gott make it so that the aoe can only affect the player once
    //public bool hasCollided;
    void Awake()
    {
        StartCoroutine(TimeUntilDestroyed());
        StartCoroutine(BufferTime());

        this.gameObject.transform.position = new Vector3(this.gameObject.transform.position.x, -1f, this.gameObject.transform.position.z);
        //Debug.LogFormat($"<color=green>{this.gameObject.GetComponent<Collider>()}</color>");
        // Set color to transparent
        // After 0.5-1 second make change opacity to inform that it is active
        // after 0.5 seconds destroy this object
    }

    IEnumerator TimeUntilDestroyed()
    {
        // Color is in full opacity here

        yield return new WaitForSeconds(timeUntilSpellHasEnded);
        DestroyAoeServerRpc();
    }


    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        //Debug.LogFormat($"<color=red>DESpawned</color>");

        if (fireAoeExists != null) fireAoeExists(OwnerClientId, this.NetworkObject, this.GetNetworkBehaviour(this.NetworkBehaviourId), false, this.gameObject.GetComponent<NetworkObject>());

        base.OnNetworkDespawn();
    }


    void ChangeColor()
    {
        Color fullOpacity = Color.red;
        fullOpacity.a = 1.0f; // Set the alpha channel to 1.0 (fully opaque)

        this.gameObject.transform.GetComponent<Renderer>().material.color = fullOpacity;
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyAoeServerRpc()
    {
        NetworkObject.Despawn(this.gameObject);
        return;
    }

    IEnumerator BufferTime()
    {

        yield return new WaitForSeconds(timeUntilSpellIsActive);

        // Color to full opacity once the spell is active
        ChangeColor();

        ActivateEffect();
    }

    void ActivateEffect()
    {
        this.gameObject.GetComponent<Collider>().enabled = true;
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

        if (!other.gameObject.name.Contains("Floor"))
        {
            // Here I was trying to make it so that if the AoE spawns above the floor
            //it moves back down to it
            //this.gameObject.transform.position = new Vector3(this.gameObject.transform.position.x, -1f, this.gameObject.transform.position.z);
        }
        // if the player comes in contact
        // Reduce the player's health and speed
    }
}

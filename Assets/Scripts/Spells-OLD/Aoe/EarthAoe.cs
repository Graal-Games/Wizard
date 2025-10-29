using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
//using Unity.Netcode;

public class EarthAoe : NetworkBehaviour
{
    public float damage = 10f;
    //public float movementSlowAmount = 2.5f;
    public float incapacitatedDuration = 1.5f;
    public bool hasHitShield;
    float timeUntilSpellIsActive = 1f;
    float timeUntilSpellHasEnded = 1.5f;

    

    public float IncapacitatedDuration
    {
        get { return incapacitatedDuration; }
        set { incapacitatedDuration = value; }
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


    void ChangeColor()
    {
        Color fullOpacity = Color.yellow;
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

    //void OnTriggerEnter(Collider other)
    //{
    //    // This needs to be handled by the player and not here if
    //    //the effect is not to be global
    //    if (other.gameObject.name.Contains("SphereShield"))
    //    {
    //        hasHitShield = true;
    //    }

    //    if (other.gameObject.name.Contains("Player"))
    //    {
    //        Debug.Log("Player");

    //    }

    //    if (!other.gameObject.name.Contains("Floor"))
    //    {
    //        // Here I was trying to make it so that if the AoE spawns above the floor
    //        //it moves back down to it
    //        //this.gameObject.transform.position = new Vector3(this.gameObject.transform.position.x, -1f, this.gameObject.transform.position.z);
    //    }
    //    // if the player comes in contact
    //    // Reduce the player's health and speed
    //}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ArcaneAoe : NetworkBehaviour
{
    public float damage = 10f;
    public float movementSlowAmount = 2.5f;
    public float slowTime = 4;
    public bool hasHitShield;
    float lifeTime = 5f;

    //[SerializeField]
    //private K_SpellData spellDataScriptableObject;

    // Gott make it so that the aoe can only affect the player once
    //public bool hasCollided;
    void Awake()
    {
        StartCoroutine(TimeUntilDestroyed());
        StartCoroutine(ActivateEffect());
        this.gameObject.transform.position = new Vector3(this.gameObject.transform.position.x, -1f, this.gameObject.transform.position.z);
        //Debug.LogFormat($"<color=green>{this.gameObject.GetComponent<Collider>()}</color>");
        // Set color to transparent
        // After 0.5-1 second make change opacity to inform that it is active
        // after 0.5 seconds destroy this object
    }

    // To turn this into a timer
    IEnumerator TimeUntilDestroyed()
    {
        yield return new WaitForSeconds(lifeTime);
        DestroyAoeServerRpc();  
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyAoeServerRpc()
    {
        NetworkObject.Despawn(this.gameObject);
        return;
    }

    IEnumerator ActivateEffect()
    {
        yield return new WaitForSeconds(0.5f);
        ChangeColor();
        this.gameObject.GetComponent<Collider>().enabled = true;
        
        // Change color
    }


    void ChangeColor()
    {
        Color fullOpacity = new Color(1.0f, 0.0f, 1.0f); ;
        fullOpacity.a = 1.0f; // Set the alpha channel to 1.0 (fully opaque)

        this.gameObject.transform.GetComponent<Renderer>().material.color = fullOpacity;
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

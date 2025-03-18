using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ArcaneAoe : K_Spell
{
    public float damage = 10f;
    public float movementSlowAmount = 2.5f;
    public float slowTime = 4;
    //public bool hasHitShield;
    float lifeTime = 500f;

    //[SerializeField]
    //private K_SpellData spellDataScriptableObject;

    PlayerHitPayload playerHitPayload;


    // Gott make it so that the aoe can only affect the player once
    //public bool hasCollided;
    void Awake()
    {
        //this.gameObject.GetComponent<Rigidbody>().useGravity = false;
        //this.gameObject.GetComponent<Rigidbody>().isKinematic = true;


        StartCoroutine(TimeUntilDestroyed());
        StartCoroutine(ActivateEffect());
        this.gameObject.transform.position = new Vector3(this.gameObject.transform.position.x, -1f, this.gameObject.transform.position.z); // This still needed?

    }

    //public override void Update()
    //{
    //    //this.gameObject.GetComponent<Collider>().isTrigger = true;
    //}

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
        // this.gameObject.GetComponent<Collider>().isTrigger = true;

        // Change color
    }


    void ChangeColor()
    {
        Color fullOpacity = new Color(1.0f, 0.0f, 1.0f); ;
        fullOpacity.a = 1.0f; // Set the alpha channel to 1.0 (fully opaque)

        this.gameObject.transform.GetComponent<Renderer>().material.color = fullOpacity;
    }

    public override void Fire()
    {
        // To change this?
    }


    //void OnTriggerEnter(Collider other)
    //{
    //    // This can be placed in the spell class
    //    if (other.CompareTag("ActiveShield") && hasHitShield == false)
    //    {
    //        other.gameObject.GetComponent<K_SphereSpell>().TakeDamage(spellDataScriptableObject.directDamageAmount);
    //        hasHitShield = true;
    //        Debug.LogFormat($"<color=orange>{other.gameObject}</color>");
    //        return;
    //    }

    //    if (other.gameObject.name.Contains("Player"))
    //    {
    //        Debug.Log("Player");

    //        // Can this be on the spell class instead?
    //        //SpellPayloadConstructor
    //        //    (
    //        //        this.gameObject.GetInstanceID(), 
    //        //        other.GetComponent<NetworkObject>().OwnerClientId,
    //        //        spellDataScriptableObject.element.ToString(),
    //        //        spellDataScriptableObject.directDamageAmount, 
    //        //        spellDataScriptableObject.damageOverTimeAmount, 
    //        //        spellDataScriptableObject.damageOverTimeDuration, 
    //        //        spellDataScriptableObject.spellAttribute
    //        //    );

    //        PlayerIsHit(); // This emits an event that applies damage to the target on the behavior and the GM script  >> NEED TO PASS ALL RELEVANT DATA HERE
    //    }
    //}

    //private void OnTriggerExit(Collider other)
    //{
    //    if (other.CompareTag("ActiveShield") && hasHitShield == true)
    //    {
    //        hasHitShield = false;
    //        Debug.LogFormat($"<color=orange>{other.gameObject}</color>");
    //        return;
    //    }
    //}
}

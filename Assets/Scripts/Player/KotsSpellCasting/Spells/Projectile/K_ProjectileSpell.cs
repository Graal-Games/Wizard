using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class K_ProjectileSpell : K_Spell
{
    //[SerializeField]
    //private K_SpellData spellDataScriptableObject;
    bool spellHasSpawned = false;
    //string spellType;
    //bool hasHitShield = false;
    //bool hasHitPlayer = false;

    private void Start()
    {
        //spellType = SpellDataScriptableObject.element.ToString();
        // Add logic to disable rb depending on which spell is used
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.useGravity = false;
        rb.isKinematic = false;

        spellHasSpawned = true;
        //Fire();


    }

    // !!!IMPORTANT!!! Side-note: When testing locally, and for some weird, if the server did not look around, the projectiles on the cast client side
    //do not fly correctly
    public void LateUpdate()
    {

        if (spellHasSpawned)
        {
            // Direction here has to match the direction that the wand tip gameobject is looking in
            transform.Translate(Vector3.forward * Time.deltaTime * SpellDataScriptableObject.moveSpeed);
        }

        // Destroy the GO after it applies force to player
        if (CanDestroy)
        {
            StartCoroutine(DelayedDestruction());

        }
    }


    public override void Fire()
    {
        //AddForceToProjectileRpc();
    }

    IEnumerator DelayedDestruction()
    {
        yield return new WaitForSeconds(0.3f); // The value here seems to be working for now. Might need to revise it later.
        DestroySpellRpc();
    }

    //[Rpc(SendTo.Server)]
    //void AddForceToProjectileRpc()
    //{
    //    //transform.SetPositionAndRotation(caster.position + caster.forward + new Vector3(0f, 1f, 0f), caster.rotation);

    //    transform.Translate(Vector3.forward);
    //}

    //[Rpc(SendTo.Server)]
    //public void DestroyProjectileRpc()
    //{
    //    //despawn
    //    //destroy
    //    Destroy(gameObject);
    //    gameObject.GetComponent<NetworkObject>().Despawn();
    //}


    //public void SpellTriggerEvent(Collider other)
    //{
    //    SpellPayload(other);
    //    DestroyProjectileRpc();
    //}

    public PlayerHitPayload FetchSpellPayload(Collider other)
    {
        return Payload(other);
    }


    public IEnumerator DestroySelf()
    {
        //this.gameObject.SetActive(false);
        yield return new WaitForSeconds(1);
        DestroySpellRpc();
    }


    public override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);

        Debug.Log("Xotherotherotherother (" + other.gameObject.name + ")");

        // Pushback(other.gameObject.GetComponent<Rigidbody>());
        //if (other.gameObject.name != "InvocationBounds")
        //{
        //    Debug.Log("otherotherotherother (" + other + ")");

        //    DestroySpellRpc();
        //    //StartCoroutine(DestroySelf());
        //}
    }
}

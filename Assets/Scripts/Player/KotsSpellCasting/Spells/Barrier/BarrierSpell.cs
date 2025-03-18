using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BarrierSpell : K_Spell
{
    // For health, get reference to the associated spellData
    float localHealth;

    void Start()
    {
        //gameObject.transform.position = new Vector3(transform.position.x, 0, transform.position.z);
        localHealth = SpellDataScriptableObject.health;

        if (SpellDataScriptableObject.spellDuration > 0)
        {
            StartCoroutine(LifeTime(SpellDataScriptableObject.spellDuration));
        }
    }

    public override void Fire()
    {

    }

    IEnumerator LifeTime(float duration)
    {
        yield return new WaitForSeconds(duration);
        DestroyBarrierRpc();
    }

    [Rpc(SendTo.Server)]
    void DestroyBarrierRpc()
    {
        Destroy(gameObject);
        gameObject.GetComponentInParent<NetworkObject>().Despawn();
    }

    // While the animation is active
    // The player that comes into contact with this spell
    // is stunned

    void ActivateBarrier()
    {
        //gameObject.transform.position = new Vector3(transform.position.x, -1, transform.position.z);
        Debug.LogFormat($"<color=orange>armorPoints: {localHealth}</color>");
    }

    // This code can be placed in the K_Spell script
    public void TakeDamage(float damage)
    {
        localHealth -= damage;
        Debug.LogFormat($"<color=orange>armorPoints: {localHealth}</color>");

        if (localHealth <= 0)
        {
            DestroyBarrierRpc();
        }
    }

    //public void DestroyBarrierRpc()
    //{
    //    DestroyBarrierRpcRpc();
    //}

    //[Rpc(SendTo.Server)]
    //void DestroyBarrierRpcRpc()
    //{
    //    Destroy(gameObject);
    //    gameObject.GetComponent<NetworkObject>().Despawn();
    //}
}

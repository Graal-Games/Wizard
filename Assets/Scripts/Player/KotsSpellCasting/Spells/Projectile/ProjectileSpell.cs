using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using static K_Spell;

public class ProjectileSpell : ProjectileClass
{
    //[SerializeField]
    //private K_SpellData spellDataScriptableObject;
    bool spellHasSpawned = false;
    //string spellType;
    //bool hasHitShield = false;
    //bool hasHitPlayer = false;
    private bool triggerEntered = false;

    public NetworkVariable<float> directDamageAmount = new NetworkVariable<float>();

    // This is the locally saved GUID
    public string localSpellId;

    // This NV saves the GUID on the NetworkedObject instance of the same spell and is used to destroy the local instance of the projectile
    public NetworkVariable<FixedString128Bytes> spellId = new NetworkVariable<FixedString128Bytes>();

    public NetworkVariable<float> spellMoveSpeed = new NetworkVariable<float>();

    public delegate void DestroyLocalProjectileInstance(FixedString128Bytes spellId);
    public static event DestroyLocalProjectileInstance projectileInstance;

    public delegate void PlayerHitEvent2(float damage);
    public static event PlayerHitEvent2 playerHitEvent2;


    //private void OnDrawGizmos()
    //{
    //    // Visualize the last movement segment and the sphere cast
    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawWireSphere(transform.position, this.gameObject.transform.localScale.x / 2); // 0.2f is your sphere cast radius
    //}

    public override void FixedUpdate()
    {
        base.FixedUpdate();
    }
}

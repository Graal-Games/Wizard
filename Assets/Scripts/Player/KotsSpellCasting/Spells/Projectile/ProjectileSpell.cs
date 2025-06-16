using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class ProjectileSpell : NetworkBehaviour
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

    Rigidbody rb;

    private void Start()
    {

    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        rb = GetComponent<Rigidbody>();

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.useGravity = false;
        rb.isKinematic = false;

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 forceDirection = transform.forward * 130;
        rb.AddForce(forceDirection, ForceMode.Force); // or ForceMode.Acceleration
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.name.Contains("Player"))
        {
            Debug.LogFormat($"<color=red>>>>>>>>>COLLISION<<<<<<</color>");
            Debug.Log(">>>>>>>>COLLISION<<<<<<");
        }
    }
}

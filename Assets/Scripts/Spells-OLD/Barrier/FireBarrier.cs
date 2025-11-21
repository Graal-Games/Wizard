using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class FireBarrier : NetworkBehaviour
{
    //public float fireBarrierDamage = 6f;
    //public float movementSlowAmount = 2.5f;
    public bool hasHitShield;
    float timeUntilSpellIsActive = 1f;
    float aliveTime = 13f;

    float applyDamageAtInterval = 0.5f;

    int dotPersistanceTime = 4;

    [SerializeField] FireBarrierCollision fireBarrierCollisionScript; //

    public delegate void BarrierLifeStatus(ulong clientId, NetworkObjectReference spellObj, NetworkBehaviour spellNetBehavior, bool status, NetworkObject netObj);
    public static event BarrierLifeStatus fireAoeExists;


    public NetworkVariable<float> fireBarrierHealth = new NetworkVariable<float>(default,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<float> fireBarrierDamage = new NetworkVariable<float>(default,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);



    //public float FireBarrierDamage
    //{
    //    get { return fireBarrierDamage; }
    //}

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

    private void Start()
    {
        // gameObject.transform.position = new Vector3(transform.position.x, -1, transform.position.z);

        fireBarrierCollisionScript.GetComponentInChildren<EarthBarrierCollision>();//

        fireBarrierHealth.Value = 45;

        fireBarrierDamage.Value = 15;
    }

    IEnumerator TimeUntilDestroyed()
    {
        yield return new WaitForSeconds(aliveTime); // 8 seconds
        // Debug.LogFormat($"<color=red>IE</color>");

        if (fireBarrierHealth.Value <= 0f)
        {
            yield return null;
        }

        DestroyBarrierServerRpc();
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

        this.gameObject.transform.GetComponentInChildren<Renderer>().material.color = fullOpacity;
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyBarrierServerRpc()
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
        this.gameObject.GetComponentInChildren<Collider>().enabled = true;
    }

    public void IsBarrierHealthNaught()
    {
        if (fireBarrierHealth.Value <= 0f)
        {
            // Debug.LogFormat($"<color=red>{earthBarrierHealth.Value}</color>");
            DestroyBarrierServerRpc();
        }
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

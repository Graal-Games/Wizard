using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
//using Codice.Client.BaseCommands;
using static Beam;

public class SphereShield : NetworkBehaviour, IChannelable
{
    public NetworkVariable<float> baseSphereShieldHealth = new NetworkVariable<float>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    //private float baseSphereShieldHealth = 20f;
    //private PlayerBehavior playerBehavior;
    Transform player;
    float lifetime = 1.5f;
    private float addToDuration;
    private float startTime;

    public delegate void ShieldLifeStatus(ulong clientId, NetworkObjectReference spellObj, NetworkBehaviour spellNetBehavior, bool status);
    public static event ShieldLifeStatus shieldExists;

    public NetworkVariable<float> timerDuration = new NetworkVariable<float>(default,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);



    void Awake()
    {
        // REINSTATE SOME OF THE FOLLOWING CODE TO MAKE THE SPHERE SHIELD TIMED
        //playerBehavior = GameObject.Find("Player(Clone)").GetComponent<PlayerBehavior>();
        // player = this.gameObject.transform;
        // Debug.Log(player);
        //Debug.Log(GameObject.Find("Player(Clone)"));
        //StartCoroutine(DestroyAfterSeconds());
        //timerDuration.Value = 2;
    }

    void Start()
    {
        startTime = Time.time;

        if (IsOwner)
        {
            baseSphereShieldHealth.Value = 20f;
            //playerBehavior = this.gameObject.transform.parent.GetComponent<PlayerBehavior>();
            //playerBehavior.shieldActive = true;
            //playerBehavior.ShieldIsActive(true);
            //playerBehavior.localSphereShieldActive.Value = true;
            
        }
    }

    void Update()
    {
        if (!IsOwner) return;
        AliveTime();
    }



    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        timerDuration.Value = 2;

        if (shieldExists != null) shieldExists(OwnerClientId, this.NetworkObject, this.GetNetworkBehaviour(this.NetworkBehaviourId), true);

        //Debug.LogFormat($"<color=green>Spawned</color>");

        //timerDuration.Value = 2;

        base.OnNetworkSpawn();
    }



    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        //Debug.LogFormat($"<color=red>DESpawned</color>");

        if (shieldExists != null) shieldExists(OwnerClientId, this.NetworkObject, this.GetNetworkBehaviour(this.NetworkBehaviourId), false);

        base.OnNetworkDespawn();
    }



    void IsShieldDestroyed()
    {
        if (baseSphereShieldHealth.Value <= 0f)
        {
            //playerBehavior.ShieldIsActive(false);
            //Debug.Log("Sphere shield is being DESTROYED");

            if (shieldExists != null) shieldExists(OwnerClientId, this.NetworkObject, this.GetNetworkBehaviour(this.NetworkBehaviourId), false);
            // >>> playerBehavior.localSphereShieldActive.Value = false;


            //StartCoroutine(DelayDestroy());
            //playerBehavior.shieldActive = false;
            DestroyShield();
        }
    }



    void OnTriggerEnter(Collider other)
    {   
        if (!other.gameObject.CompareTag("Spell")) return;
        // Getting the spell by name wasn't working
        
        if (IsOwner)
        {
            if (other.gameObject.GetComponent<Bolt>())
            {
                float boltDamage = other.GetComponent<Bolt>().damage;
                baseSphereShieldHealth.Value -= boltDamage;
                other.GetComponent<Bolt>().DestroyBolt();

                IsShieldDestroyed();
            }
            
            if (other.gameObject.GetComponent<ArcaneAoe>())
            {
                float aoeDamage = other.GetComponent<ArcaneAoe>().damage;
                baseSphereShieldHealth.Value -= aoeDamage;
                IsShieldDestroyed();
            }
            
            // To implement all other spells

            //Debug.Log("Sphere Shield: " + baseSphereShieldHealth.Value);
        }

        // if (other.gameObject.name.Contains("Bolt"))
        // {
        //     Debug.Log("DESTROY BOLT");
        //     //DestroyBoltServerRpc(other.GetComponent<NetworkObject>());
        // }
        
    }



    public void DestroyShield()
    {
        if (this.GetComponent<NetworkObject>().IsSpawned)
        {
            if (IsServer)
            {     
                DestroyShieldServerRpc();
                Debug.Log("IS SERVER");
                    
            } else {

                Debug.Log("NOT SERVER");
                DestroyShieldServerRpc();
            }
            
            return;

        } else {

            return;
        }
        
    }



    [ServerRpc(RequireOwnership = false)]
    private void DestroyShieldServerRpc()
    {
        NetworkObject.Despawn(this.gameObject);
        return;
    }



    //IEnumerator DestroyAfterSeconds()
    //{
    //    yield return new WaitForSeconds(lifetime);
    //    DestroyShield();
    //}

    public override void OnGainedOwnership()
    {
        timerDuration.Value = 2;
        Debug.Log("timerDuration.Value: " + timerDuration.Value);

        base.OnGainedOwnership();
    }



    void AliveTime(GameObject target = null)
    {
        float elapsedTime = Time.time - startTime;

        timerDuration.Value += addToDuration;

        //Debug.Log("Owner:  " + OwnerClientId + " timerDuration:  " + timerDuration.Value + "  elapsedTime: " + elapsedTime);

        if (elapsedTime >= timerDuration.Value)
        {
            Debug.Log("DESTROY");
            DestroyShield();

            return;
        }

        addToDuration = 0;
    }


    public void Upkeep(float upkeepDurationAmount)
    {
        addToDuration += upkeepDurationAmount;
    }

    internal interface IChannelable
    {
        public void Upkeep(float upkeepDurationAmount);
    }
}

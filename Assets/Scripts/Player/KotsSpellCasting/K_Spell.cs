using System;
using Unity.Netcode;
using UnityEngine;
using Events;
using Unity.Collections;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
// This is the spell class

[SelectionBase] // Select parent
//[RequireComponent(typeof(Rigidbody))]
public abstract class K_Spell : NetworkBehaviour, ISpell
{
    //[Tooltip("The collider for damage detection, it should be set as a Trigger.")]
    //public Collider hurtBox;

    [Tooltip("The material asociated to each spell element. It should contain one entry for every element.")]
    [UDictionary.Split(30, 70)] public MaterialDict materialDictionary;
    [Serializable] public class MaterialDict : UDictionary<Element, Material> { }

    // Scriptable Object containing the relevant data for the spell
    [HideInInspector] public K_SpellData spellData;

    // Timers
    [HideInInspector] public float aliveTimer;
    [HideInInspector] public float dotTickTimer;

    // Components
    [HideInInspector] public Rigidbody rb;

    GameObject hitPlayer;

    GameObject gameObjectToDestroy;

    private float speed;

    float? healthPoints = null; // Nullable

    public string SpellName => SpellDataScriptableObject.name;

    public float? HealthPoints
    {
        get { return healthPoints; }
        set { healthPoints = value; }
    }

    // Spell caster
    [HideInInspector] public Transform caster;

    PlayerHitPayload playerHitPayload;

    public delegate void PlayerHitEvent(PlayerHitPayload damageInfo);
    public static event PlayerHitEvent playerHitEvent;

    Vector3 pushDirection; // Adjust the direction of the force

    List<Rigidbody> pullSpellsList = new List<Rigidbody>();
    List<Rigidbody> pushSpellsList = new List<Rigidbody>();

    [SerializeField]
    private K_SpellData spellDataScriptableObject;

    bool canDestroy = false;

    public bool CanDestroy
    {
        get { return canDestroy;  }
        set {  canDestroy = value; }
    }

    public K_SpellData SpellDataScriptableObject
    {
        get { return spellDataScriptableObject; }
    }

    public float Speed { get { return speed; } set { speed = value; } }

    public Rigidbody RigidbodyCP
    {
        get { return rb; }
        set { rb = value; }
    }

    NetworkVariable <bool> hasHitShield = new NetworkVariable <bool>(false,
        NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);
    bool hasHitPlayer = false;

    public bool HasHitPlayer
    {
        get { return hasHitPlayer; }
        set { hasHitPlayer = value; }
    }



    private void Awake()
    {
        RigidbodyCP = this.GetComponent<Rigidbody>();     
    }




    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.LogFormat($"<color=pink>ggggggggggggggggggggo{gameObject}</color>");

        pushDirection = transform.forward;

    }





    private void Update()
    {
        if (pullSpellsList.Count > 0) // Need to add this to the player behaviour script because this will be destroyed too fast and cannot take into account defensive spells
        {
            // Apply force to all the rigidbodies
            foreach (Rigidbody rb in pullSpellsList)
            {
                rb.AddForce(spellDataScriptableObject.pushForce * pushDirection.normalized, ForceMode.Impulse);
                canDestroy = true; // If there is a need to destroy the gameObject after it applies force, use this variable.
            }
        }

        if (pushSpellsList.Count > 0)
        {
            foreach (Rigidbody rb2 in pushSpellsList)
            {
                //AddForce(rb2);
                rb2.AddForce(spellDataScriptableObject.pushForce * pushDirection.normalized, ForceMode.Impulse);

                canDestroy = true; 
            }
        }
    }




    public PlayerHitPayload Payload(Collider other)
    {
        SpellPayloadConstructor
        (
            this.gameObject.GetInstanceID(),
            other.GetComponent<NetworkObject>().OwnerClientId,
            spellDataScriptableObject.element.ToString(),
            spellDataScriptableObject.incapacitationName,
            spellDataScriptableObject.incapacitationDuration,
            spellDataScriptableObject.visionImpairmentType,
            spellDataScriptableObject.visionImpairmentDuration,
            spellDataScriptableObject.directDamageAmount,
            spellDataScriptableObject.damageOverTimeAmount,
            spellDataScriptableObject.damageOverTimeDuration,
            spellDataScriptableObject.spellAttribute,
            spellDataScriptableObject.pushback
        );

        return playerHitPayload;

    }




    // This method instantiates a new instance of PlayerHitPayload struct that was defined elsewhere
    // Thereafter, once the spell has come into contact with a player the information required to handle player damage, along with the inflicted target player's clientId
    // Once the infomation is set, it is sent via an event that is emitted, thereafter received in the PlayerBehaviour script and handled there accordingly
    // Note parameters order
    public void SpellPayloadConstructor(int netId, ulong pId, string element, IncapacitationName incapName, float incapDur, VisionImpairment visionImp, float visionImpDur, float ddAmount, float dotAmount, float dotDur, SpellAttribute type, bool pushback)
    {
        // This is a struct that is defined in its own script
        // It is used to send information about the spell that hit a player for damage and effects handling
        playerHitPayload = new PlayerHitPayload
        {
            // The left side values are defined in the PlayerHitPayload struct
            NetworkId = netId,
            PlayerId = pId,
            SpellElement = element,
            IncapacitationName = incapName,
            IncapacitationDuration = incapDur,
            VisionImpairment = visionImp,
            VisionImpairmentDuration = visionImpDur,
            DirectDamageAmount = ddAmount,
            DamageOverTimeAmount = dotAmount,
            SpellAttribute = type,
            DamageOverTimeDuration = dotDur,
            Pushback = pushback
        };
    }





    public IEnumerator LifeTime(float duration, GameObject spellObj)
    {
        gameObjectToDestroy = spellObj;

        yield return new WaitForSeconds(duration);
        DestroySpellRpc(gameObjectToDestroy);
    }

    public void DestroySpell(GameObject spellObj)
    {
        NetworkObject netObj;

        if (spellObj.GetComponent<NetworkObject>())
        {
            netObj = spellObj.GetComponent<NetworkObject>();
        }
        else
        {
            netObj = spellObj.GetComponentInParent<NetworkObject>();
        }



        if (netObj != null && netObj.IsSpawned)
        {
            DestroySpellRpc(netObj); // or new NetworkObjectReference(netObj)
        }
        else
        {
            // Fallback: just destroy locally if not spawned
            Destroy(spellObj);
        }


        //gameObjectToDestroy = spellObj;

        //Debug.LogFormat($"<color=purple>111gameObjectgameObjectgameObject {gameObjectToDestroy}</color>");


        //DestroySpellRpc(gameObjectToDestroy);
    }



    [Rpc(SendTo.Server)]
    public void DestroySpellRpc(NetworkObjectReference spellObjRef)
    {
        Debug.LogFormat($"<color=purple>222gameObjectgameObjectgameObject {gameObjectToDestroy}</color>");

        Destroy(gameObjectToDestroy);

        if (spellObjRef.TryGet(out NetworkObject netObj))
        {
            if (netObj != null)
            {
                netObj.Despawn();
            }
            else
            {
                netObj.Despawn();
            }
        }
    }





    public void Pushback(Rigidbody rb)
    {
        if (spellDataScriptableObject.pushForce > 0)
        {
            // Add the rigidbody to the list of rigidbodies to be pushed
            if (rb != null)
            {
                pushSpellsList.Add(rb);
            }
        }
            
    }




    //[ClientRpc]
    //void ApplyDamageToPlayerClientRpc(ClientRpcParams rpcParams = default)
    //{
        
    //    DoSomething();
    //    Debug.LogFormat($"<color=blue> 1 NEW DAMAGE APPLICATION / OWNER: {rpcParams} </color>");
    //}

    [Rpc(SendTo.SpecifiedInParams)]
    void ApplyDamageToPlayerClientRpc(ulong ownerId, RpcParams rpcParams = default)
    {
        SpellPayloadConstructor
        (
            this.gameObject.GetInstanceID(),
            ownerId,
            spellDataScriptableObject.element.ToString(),
            spellDataScriptableObject.incapacitationName,
            spellDataScriptableObject.incapacitationDuration,
            spellDataScriptableObject.visionImpairmentType,
            spellDataScriptableObject.visionImpairmentDuration,
            spellDataScriptableObject.directDamageAmount,
            spellDataScriptableObject.damageOverTimeAmount,
            spellDataScriptableObject.damageOverTimeDuration,
            spellDataScriptableObject.spellAttribute,
            spellDataScriptableObject.pushback
        );

        PlayerIsHit();

        DoSomething();
        Debug.LogFormat($"<color=blue> 1 NEW DAMAGE APPLICATION / OWNER: {rpcParams} </color>");
    }

    public void DoSomething()
    {
        //this.GetComponent<NewPlayerBehavior>().DoSomething();
        Debug.LogFormat($"<color=blue> 2 NEW DAMAGE APPLICATION / OWNER: {OwnerClientId} </color>");

    }


    public virtual void OnTriggerEnter(Collider other)
    {
        Debug.LogFormat("OnTriggerEnter: this={0} (tag={1}), other={2} (tag={3})",
        gameObject.name, gameObject.tag, other.gameObject.name, other.gameObject.tag);

        if (gameObject.name.Contains("Player")) return;

        //if (gameObject.GetComponent<ISpell>().SpellName.Contains("Barrier_Air") || gameObject.GetComponentInParent<ISpell>().SpellName.Contains("Barrier_Air"))
        if (gameObject.GetComponent<ISpell>().SpellName.Contains("Barrier_Air") || gameObject.GetComponentInParent<ISpell>().SpellName.Contains("Barrier_Air")
            || gameObject.GetComponent<ISpell>().SpellName.Contains("Projectile_Air") || gameObject.GetComponentInParent<ISpell>().SpellName.Contains("Projectile_Air"))
        {
            if (spellDataScriptableObject.pushForce > 0)
            {
                Debug.LogFormat("<color=green>2 Push spell</color>");
                // Cache the player's Rigidbody locally
                Rigidbody rb = other.GetComponent<Rigidbody>();

                // Add the rigidbody to the list of rigidbodies to be pushed
                if (rb != null)
                {
                    //pullSpellsList.Add(rb);
                }
            }
            else
            {
                if (spellDataScriptableObject.pushForce > 0)
                {
                    Debug.LogFormat("<color=blue>2 Push spell</color>");

                    // Cache the player's Rigidbody locally
                    Rigidbody rb2 = other.GetComponent<Rigidbody>();

                    Debug.LogFormat($"<color=blue>3 Push spell RB: {rb2}</color>");

                    // Add the rigidbody to the list of rigidbodies to be pushed
                    if (rb2 != null)
                    {
                        pushSpellsList.Add(rb2);
                    }
                }
            }
        }

        // If shield is detected redirect damage to it
        // And DO NOT proceed to apply damage to the related player
        if (other.CompareTag("ActiveShield"))
        {
            // This is being called incorrectly from somewhere. Haven't figured out where or what yet.
            other.gameObject.GetComponent<K_SphereSpell>().TakeDamage(spellDataScriptableObject.directDamageAmount);

            hasHitShield.Value = true;

            // What?
            if (spellDataScriptableObject.spellType.ToString() == "Projectile")
                DestroySpellRpc(other.gameObject);

            return;
        }

        if (other.gameObject.CompareTag("Player"))
        {

            // OPTIMIZE BELOW
            // Assign the values to the payload to be sent with the event emission upon hitting the player
            SpellPayloadConstructor
            (
                this.gameObject.GetInstanceID(),
                other.GetComponent<NetworkObject>().OwnerClientId,
                spellDataScriptableObject.element.ToString(),
                spellDataScriptableObject.incapacitationName,
                spellDataScriptableObject.incapacitationDuration,
                spellDataScriptableObject.visionImpairmentType,
                spellDataScriptableObject.visionImpairmentDuration,
                spellDataScriptableObject.directDamageAmount,
                spellDataScriptableObject.damageOverTimeAmount,
                spellDataScriptableObject.damageOverTimeDuration,
                spellDataScriptableObject.spellAttribute,
                spellDataScriptableObject.pushback
            );


            PlayerIsHit(); // This emits an event that applies damage to the target on the behavior and the GM script  >> NEED TO PASS ALL RELEVANT DATA HERE
            hasHitPlayer = true;

            return;
        }


        // The below code can nbe simplified
        if (other.gameObject.CompareTag("Spell") && !other.gameObject.CompareTag("Player")) 
        {

            if (other.GetComponent<ISpell>().SpellName.Contains("Barrier") && other.gameObject.GetComponent<BarrierSpell>() != null)
            {
                BarrierSpell barrierScript = other.gameObject.GetComponentInParent<BarrierSpell>();

                if (barrierScript.SpellDataScriptableObject.health > 1) // 1 is minimum ie. undamageable
                {
                    barrierScript.ApplyDamage(SpellDataScriptableObject.directDamageAmount); //This is causing an error. No idea why.
                    //DestroySpellRpc();
                }

                DestroySpellRpc(other.gameObject);
            }

            if (other.gameObject.name.Contains("Scepter"))
            {
                InvocationSpell invocationSpell = other.gameObject.GetComponentInParent<InvocationSpell>();

                if (invocationSpell.SpellDataScriptableObject.health > 1)
                {
                    invocationSpell.ApplyDamage(SpellDataScriptableObject.directDamageAmount);
                }

                DestroySpellRpc(other.gameObject);

            }
        }

        // This is an exception that handles AIR PROJECTILES or AIR AOE
        // If the spell collides with the player character, handle this interaction
        if (other.gameObject.CompareTag("Player") && !gameObject.GetComponent<ISpell>().SpellName.Contains("Barrier"))
        {
            // Exception handler - If when the player has a shield on when hit by a projectile, ignore damage application on the player
            if (spellDataScriptableObject.spellType.ToString() == "Projectile")
            {
                if (hasHitShield.Value == true) return;
            }


            // OPTIMIZE BELOW
            // Assign the values to the payload to be sent with the event emission upon hitting the player
            SpellPayloadConstructor
            (
                this.gameObject.GetInstanceID(),
                other.GetComponent<NetworkObject>().OwnerClientId,
                spellDataScriptableObject.element.ToString(),
                spellDataScriptableObject.incapacitationName,
                spellDataScriptableObject.incapacitationDuration,
                spellDataScriptableObject.visionImpairmentType,
                spellDataScriptableObject.visionImpairmentDuration,
                spellDataScriptableObject.directDamageAmount,
                spellDataScriptableObject.damageOverTimeAmount,
                spellDataScriptableObject.damageOverTimeDuration,
                spellDataScriptableObject.spellAttribute,
                spellDataScriptableObject.pushback
            );


            PlayerIsHit(); // This emits an event that applies damage to the target on the behavior and the GM script  >> NEED TO PASS ALL RELEVANT DATA HERE
            hasHitPlayer = true;

            Debug.LogFormat("<color=blue>YYYYYYYK_Spell (" + other.gameObject.name + ")</color>");
        }
    }






    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();

            // If it has a Rigidbody, apply force
            if (rb != null)
            {
                pullSpellsList.Remove(rb);
                pushSpellsList.Remove(rb);
            }
        }


        //if (other == null) return;
        if (!spellDataScriptableObject) return;

        // This is needed to turn off DoT damage
        if (spellDataScriptableObject.spellAttribute.ToString() == "PersistentDamageOverTime" && other.gameObject.CompareTag("Player"))
        {
            SpellPayloadConstructor
            (
                this.gameObject.GetInstanceID(),
                other.GetComponent<NetworkObject>().OwnerClientId,
                spellDataScriptableObject.element.ToString(),
                spellDataScriptableObject.incapacitationName,
                spellDataScriptableObject.incapacitationDuration,
                spellDataScriptableObject.visionImpairmentType,
                spellDataScriptableObject.visionImpairmentDuration,
                spellDataScriptableObject.directDamageAmount,
                spellDataScriptableObject.damageOverTimeAmount,
                spellDataScriptableObject.damageOverTimeDuration,
                spellDataScriptableObject.spellAttribute,
                spellDataScriptableObject.pushback
            );

            PlayerIsHit();
        }
    }







    // Method that initializes the event for when a player is hit using the info passed to it from the spell
    public void PlayerIsHit()
    {
        playerHitEvent?.Invoke(playerHitPayload);
        //playerHitEvent2?.Invoke(playerHitPayload2);
    }







    /// <summary>
    /// Destroys the spell once t seconds have passed.
    /// Override this function to add aditional logic to be called before destroying the spell.
    /// </summary>
    /// <param name="t">The amount of time in seconds before destroying the spell.</param>
    public virtual void Destroy(float t)
    {
        GetComponent<NetworkObject>().Despawn();
        Destroy(gameObject, t);
    }






    /// <summary>
    /// Fires the spell. Override to create the firing logic.
    /// </summary>
    public abstract void Fire();
}

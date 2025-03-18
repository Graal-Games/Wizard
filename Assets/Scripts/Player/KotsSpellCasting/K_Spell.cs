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
public abstract class K_Spell : NetworkBehaviour
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

    private float speed;

    float? healthPoints = null; // Nullable

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
    //float lifetimeDuration;

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


    private void Awake()
    {
        RigidbodyCP = this.GetComponent<Rigidbody>();
        ////spellData = this.GetComponent<K_SpellData>();
        //caster = this.GetComponentInParent<Transform>();
        //Debug.LogFormat($"<color=purple>caster: {caster}</color>");

        // Assign the values to the payload to be sent with the event emission upon hitting the player

      
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        //Debug.Log("This is the the spell's instance ID: " + this.GetInstanceID());
        //Debug.LogFormat($"<color=purple> spellDataScriptableObject.childPrefab: {spellDataScriptableObject.childPrefab.gameObject }</color>");
        //
        pushDirection = transform.forward;
        // Set duration to 0 for infinite duration
        //if (spellDataScriptableObject.duration > 0)
        //{
        //    StartCoroutine(LifeTime(spellDataScriptableObject.duration));
        //}

    }

    IEnumerator LifeTime(float duration)
    {
        yield return new WaitForSeconds(duration);
        DestroySpellRpc();
    }

    private void Update()
    {
        if (pullSpellsList.Count > 0) // Need to add this to the player behaviour script because this will be destroyed too fast and cannot take into account defensive spells
        {
            Debug.LogFormat($"<color=pink>rb count ------------- {gameObject}</color>");

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

    //async Task AddForce(Rigidbody rb2)
    //{
    //    rb2.AddForce(spellDataScriptableObject.pushForce * pushDirection.normalized, ForceMode.Impulse);
    //    await Task.Delay(500);
    //}

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


    [Rpc(SendTo.Server)]
    public void DestroySpellRpc()
    {
        Debug.LogFormat($"<color=pink>gameObjectgameObjectgameObject {gameObject}</color>");

        Destroy(gameObject);
        if (gameObject.GetComponent<NetworkObject>() != null)
        {
            gameObject.GetComponent<NetworkObject>().Despawn();
        } else
        {
            gameObject.GetComponentInParent<NetworkObject>().Despawn();
        }
    }

    //[Rpc(SendTo.Server)]
    //public void PushBackRpc()
    //{
    //    // Get the velocity of the triggering object
    //    Vector3 forceDirection = this.gameObject.GetComponent<Rigidbody>().velocity;
    //    // Apply the force to the other object's Rigidbody
    //    hitPlayer.GetComponent<Rigidbody>().AddForce(forceDirection * 1, ForceMode.Force);
    //}


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



    public virtual void OnTriggerEnter(Collider other)
    {
        Debug.LogFormat($"<color=pink>otherotherotherotherotherother:{other}</color>");

        //if (!IsOwner) { return; }
        // // If the (projectile) spell comes into contact with the environment, destroy it
        //if (other.gameObject.CompareTag("Environment"))
        //{
        //    if (spellDataScriptableObject.spellType.ToString() == "Projectile")
        //        DestroyProjectileRpc();
        //}

        //if (other.gameObject.CompareTag("Spell"));
        //Debug.LogFormat($"<color=purple>Trigger: {other.gameObject.name} - Tag: {other.gameObject.tag}</color>");
        //if (spellData.damageLayers == (spellData.damageLayers | (1 << other.gameObject.layer)))
        //{
        //    if (other.transform == caster)
        //    {
        //        if (spellData.friendlyFire)
        //        {
        //            // TODO: Apply damage to the caster (spellData.damage)
        //        }
        //    }
        //    else
        //    {
        //        // TODO: Apply damage to enemy player (spellData.damage)
        //    }

        //    // If true, the spell will be destroyed on contact with a player
        //    if (spellData.destroyOnPlayerCollision)
        //        Destroy(0f);

        //    return;
        //}

        // If shield is detected redirect damage to it
        // And DO NOT proceed to apply damage to the related player
        if (other.CompareTag("ActiveShield"))
        {
            //Debug.LogFormat($"<color=pink>OTEN Active Shield entry</color>");
            other.gameObject.GetComponent<K_SphereSpell>().TakeDamage(spellDataScriptableObject.directDamageAmount);


            hasHitShield.Value = true;
            //Debug.LogFormat($"<color=pink>hasHitShieldhasHitShield:{hasHitShield.Value}</color>");

            if (spellDataScriptableObject.spellType.ToString() == "Projectile")
                DestroySpellRpc();


            //Debug.LogFormat($"<color=orange>{other.gameObject}</color>");
            return;
        }

        // If the spell hits a barrier, apply the damage to the barrier
        // If the spell hits a shield > That is handled above
        if (other.gameObject.name.Contains("Barrier"))
        {
            Debug.LogFormat($"<color=pink>555555555other555555555:{other}</color>");

            if (this.gameObject.CompareTag("ActiveShield")) return; // Handled above, this is just an exception handler.

            BarrierSpell barrierScript = other.gameObject.GetComponentInParent<BarrierSpell>();

            if (spellDataScriptableObject.health > 1) // 1 is minimum ie. undamageable
            {
                // Apply damage to the barrier
                other.gameObject.GetComponentInParent<BarrierSpell>().TakeDamage(spellDataScriptableObject.directDamageAmount);
            }

            //BarrierSpell BarrierScript = other.gameObject.GetComponent<BarrierSpell>();

            //if (BarrierScript)
            //{
            //    other.gameObject.GetComponentInParent<BarrierSpell>().TakeDamage(spellDataScriptableObject.directDamageAmount);
            //} else
            //{
            //    Debug.LogFormat($"<color=pink>ERROR: Barrier does not have an assigned barrier script.</color>");
            //}

        }

        // If the spell collides with the player character, handle this interaction
        if (other.gameObject.CompareTag("Player"))
        {
            //Debug.LogFormat($"<color=pink>222222222other222222222:{other}</color>");

            if (gameObject.name == "Aoe_Air" && gameObject.name == "Area of effect" && gameObject.tag == "Spell")
            {
                if (spellDataScriptableObject.pushForce > 0)
                {
                    Debug.LogFormat($"<color=pink>89888888other8888888888888:{other}</color>");
                    // Cache the player's Rigidbody locally
                    Rigidbody rb = other.GetComponent<Rigidbody>();

                    // Add the rigidbody to the list of rigidbodies to be pushed
                    if (rb != null)
                    {
                        //pullSpellsList.Add(rb);
                    }
                }
            } else 
            {
                Debug.LogFormat($"<color=pink>4444444444other4444444444:{other}</color>");
                if (spellDataScriptableObject.pushForce > 0)
                {
                    // Cache the player's Rigidbody locally
                    Rigidbody rb2 = other.GetComponent<Rigidbody>();
                    Debug.LogFormat($"<color=pink>4141414141414other41414141414141:{rb2}</color>");
                    // Add the rigidbody to the list of rigidbodies to be pushed
                    if (rb2 != null)
                    {
                        Debug.LogFormat($"<color=pink>000000000000other000000000000000:{other}</color>");
                        pushSpellsList.Add(rb2);                    
                        Debug.LogFormat($"<color=pink>42424224242424other42424242424:{pushSpellsList.Count}</color>");

                    }
                }
            }


            //Debug.LogFormat($"<color=pink>3333333333other333333333:{other}</color>");


            // Exception handler - If when the player has a shield on when hit by a projectile, ignore damage application on the player
            if (spellDataScriptableObject.spellType.ToString() == "Projectile")
                if (hasHitShield.Value == true) return;

            // Debug.LogFormat($"<color=orange>PLAYER: player detected: {other.gameObject.GetComponent<NetworkObject>().GetInstanceID()}</color>");

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
        }
        //if (spellData.collisionLayers == (spellData.collisionLayers | (1 << other.gameObject.layer)))
        //{
        //    // If true, the spell will be destroyed after detecting ANY collision
        //    if (spellData.destroyOnCollision)
        //        Destroy(0f);
        //    else
        //        rb.isKinematic = true;

        //    return;
        ////}
        //if (other.gameObject.CompareTag("Player") && this.gameObject.name.Contains("Projectile"))
        //{
        //    this.gameObject.GetComponent<K_ProjectileSpell>().DestroySelf();
        //}
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

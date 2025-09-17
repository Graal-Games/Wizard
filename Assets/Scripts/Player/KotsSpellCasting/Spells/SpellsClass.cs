using DamageOverTimeEffect;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using static ProjectileSpell;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Net.Security;
using Unity.Collections;

public class SpellsClass : NetworkBehaviour, ISpell
{
    [SerializeField]
    private K_SpellData spellDataScriptableObject;

    GameObject gameObjectToDestroy;

    [SerializeField]
    protected GameObject secondaryGameObjectToSpawn;

    //private static GameObject spellsExplosionGO;

    //private static AssetReferenceGameObject spellsExplosionAR;

    NetworkVariable<float> armorPoints = new NetworkVariable<float>(0);
    public K_SpellData SpellDataScriptableObject
    {
        get { return spellDataScriptableObject; }
    }

    public delegate void PlayerHitEvent(PlayerHitPayload damageInfo);
    public static event PlayerHitEvent playerHitEvent;

    protected Rigidbody rb;


    // These are being defined in the scriptable object associated to each prefab
    public string SpellName => SpellDataScriptableObject.name;
    public Element Element => SpellDataScriptableObject.element;
    public bool IsDispelResistant => SpellDataScriptableObject.isDispelResistant;
    public float DirectDamage => SpellDataScriptableObject.directDamageAmount;
    public float DamageOverTimeAmount => SpellDataScriptableObject.damageOverTimeAmount;



    NetworkVariable<bool> hasHitShield = new NetworkVariable<bool>(false,
    NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);

    NetworkVariable<float> healthPoints = new NetworkVariable<float>(0,
    NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);

    protected NetworkVariable<bool> isSpellActive = new NetworkVariable<bool>(false,
    NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);


    public List<OnCollisionConstantDamageOverTime> currentOnCollisionDoTList = new List<OnCollisionConstantDamageOverTime>();



    private float spellLifetimeTimer = 0f;
    private float spellLifetimeDuration = 0f;
    private bool spellLifetimeActive = false;

    public NetworkVariable<FixedString32Bytes> parryLetters = new NetworkVariable<FixedString32Bytes>(default,
  NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<FixedString32Bytes> ParryLetters
    {
        get { return parryLetters; }
        set { parryLetters = value; }
    }


    protected float checkRadius = 2f;    // Match your trigger size
    protected LayerMask triggerLayer;    // Layer for the kill trigger
    private GameObject otherGO;



    /// <summary>
    ///  1- Lifetime ----------------------------- Time until the spell is destroyed
    ///  2- Activation Delay - Optional ---------- Time before the spell effect is activated after casting 
    ///  3- Deactivation Delay - Optional -------- Time before the spell effect is deactivated after casting
    ///  4- Player Hit --------------------------- Handles emitting the payload
    ///  5- Spell To Player Interaction handler -- Handles the player hit check if shield is active and handle damage application
    ///  6- Spell To Spell Interaction handler --- Handles the spell to spell interaction check and handles damage application
    ///  >> Currently utilizes the individual script for each spell, to use an IInterface instead <<<<<<<<<<<<<<<<<< TO DO 
    ///  7- Handle if hit player active shield --- Handles the interaction with the active shield and redirects damage to it
    ///  8- Handle destroy spell ----------------- Handles the spell destruction logic
    /// </summary>

    /// >>>> IMPORTANT <<<<
    /// TO DO: This class should only define methods that are used by spells but are thereafter used and implemented by class and spell scripts
    /// >>>> IMPORTANT <<<< 


    private void Start()
    {
        if (rb != null)
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = false;
        }

    }




    public virtual void FixedUpdate()
    {
        if (spellLifetimeActive)
        {
            spellLifetimeTimer += Time.fixedDeltaTime;

            if (spellLifetimeTimer >= spellLifetimeDuration)
            {
                spellLifetimeActive = false;
                DestroySpellRpc();
            }
        }


        if (currentOnCollisionDoTList.Count > 0)
        {
            // Iterate through the DoT spells the player character is currently affected by
            for (int i = currentOnCollisionDoTList.Count - 1; i >= 0; i--)
            {
                // Get the instance of each of the DoT effect
                var dot = currentOnCollisionDoTList[i];


                // If the spell duration has not yet expired (above)
                // The method returns 'true' at a specified (per second) time interval and applies damage
                if (dot.OnCollisionConstantDoTDamageTick())
                {
                    Debug.LogFormat($"<color=purple>SPSPSPSPSPHEREEEE DOT APPLY DAMAGE OTHERGOOOOOOO: {otherGO}</color>");

                    // If the GO is destroyed remove it from the list
                    if (otherGO == null && !GetComponent<IDamageable>().ToString().Contains("Sphere"))
                    {
                        Debug.LogFormat($"<color=purple>222222 SPSPSPSPSPHEREEEE DOT APPLY DAMAGE</color>");

                        currentOnCollisionDoTList.Remove(dot);
                        return;
                    }

                    // If the GO is destroyed remove it from the list
                    if (otherGO == null && !GetComponent<IDamageable>().ToString().Contains("Barrier"))
                    {
                        Debug.LogFormat($"<color=purple>99999 SPSPSPSPSPHEREEEE DOT APPLY DAMAGE</color>");

                        currentOnCollisionDoTList.Remove(dot);
                        return;
                    }

                    UnityEngine.Debug.LogFormat($"<color=purple>SPSPSPSPSPHEREEEE other name: {otherGO.name} other nb script: {otherGO.GetComponent<NetworkBehaviour>()}</color>");


                    // Apply damage to the player
                    otherGO.GetComponent<IDamageable>().TakeDamage(dot.DamagePerSecond);

                    // Activating the blood shader for AoE doesn't work the same way when it is to be fired in succession
                    //if (shaderActivation != null) shaderActivation(OwnerClientId, "Blood", 1);
                    // DebuffController.DebuffController cont = new DebuffController.DebuffController(_healthBar.ApplyDamage(dot.DamagePerSecond));
                }

            }
        }
    }


    public virtual void TakeDamage(float damage)
    {
        armorPoints.Value -= damage;
        Debug.LogFormat($"<color=orange>armorPoints: {armorPoints.Value}</color>");

        TakeDamageRpc(damage);

        //CheckStatus();
    }

    [Rpc(SendTo.Server)]
    void TakeDamageRpc(float damage)
    {
        armorPoints.Value -= damage;
        CheckStatus();
    }

    void CheckStatus()
    {
        if (armorPoints.Value <= 0)
        {
            Debug.LogFormat($"<color=orange>2!!!!armorPoints: {armorPoints.Value}</color>");
            DestroySpell(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        //StartCoroutine(LifeTime(SpellDataScriptableObject.spellDuration, this.gameObject));
        if (SpellDataScriptableObject)
        {
            StartLifeTime(SpellDataScriptableObject.spellDuration, this.gameObject);

        } else
        {
            Debug.LogError("SpellDataScriptableObject is not assigned in " + gameObject.name);
        }

        // If the spell has a health value greater than 0, set the healthPoints variable
        // This is used to apply damage to the spell itself and handle it's (delayed) destruction
        if (SpellDataScriptableObject.health > 0)
        {
            healthPoints.Value = SpellDataScriptableObject.health;
        }

        if (SpellDataScriptableObject.spellActivationDelay > 0)
        { 
            SpellActivationDelay(); 
        }



    }


    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        // Unsubscribe from the player hit event // To remember to do this where applicable 
        //playerHitEvent -= EmitPayload;
        Debug.LogFormat("<color=orange>Spell despawned</color>", gameObject.name);
    }


    protected void SpellActivationDelay()
    {
        if (SpellDataScriptableObject.spellActivationDelay > 0)
        {
            gameObject.GetComponent<Collider>().enabled = false;
            StartCoroutine(ActivationDelay());
        }
        else
        {
            ActivateSpell();
        }
    }



    IEnumerator ActivationDelay()
    {
        // Wait for the specified activation delay
        yield return new WaitForSeconds(SpellDataScriptableObject.spellActivationDelay);

        // Activate the spell
        ActivateSpell();
    }


    protected void DeactivateSpell()
    {
        // Logic to deactivate the spell
        if (gameObject.GetComponent<Collider>() != null)
        {
            gameObject.GetComponent<Collider>().enabled = false;
            isSpellActive.Value = false;
        }
    }


    protected void ActivateSpell()
    {
        // Logic to activate the spell
        if (gameObject.GetComponent<Collider>() != null)
        {
            gameObject.GetComponent<Collider>().enabled = true;
            isSpellActive.Value = true;
        }

    }



    public void SpellDeactivationDelay(Collider colliderToDeactivate = null)
    {
        //Debug.LogFormat("<color=orange>111SpellDeactivationDelay called with null collider</color>");

        if (colliderToDeactivate == null)
        {
            //Debug.LogFormat("<color=orange>SpellDeactivationDelay called with null collider</color>");
            colliderToDeactivate = gameObject.GetComponent<Collider>();
        }
        else
        {
            //Debug.LogFormat("<color=orange>SpellDeactivationDelay called with collider: {0}</color>", colliderToDeactivate.name);
            StartCoroutine(DeactivationDelay(colliderToDeactivate));
        }

        StartCoroutine(DeactivationDelay(colliderToDeactivate));
    }

    
    IEnumerator DeactivationDelay(Collider colliderToDeactivate)
    {
        // Wait for the specified deactivation delay
        yield return new WaitForSeconds(SpellDataScriptableObject.spellTimeBeforeDeactivation);

        // Deactivate the spell
        DeactivateSpell(colliderToDeactivate);
    }



    void DeactivateSpell(Collider colliderToDeactivate)
    {
        colliderToDeactivate.enabled = false;
        isSpellActive.Value = false;
    }





    public virtual void PlayerIsHit(GameObject other)
    {
        // Get the NetworkObject ID of the player that was hit
        ulong targetNetworkObjectId = other.GetComponent<NetworkObject>().NetworkObjectId;

        // Handle spell interaction with player
        ApplyDamageToPlayerClientRpc(targetNetworkObjectId);
    }



    // Emits a client rpc payload to all players that is then locally digested to invoke associated methods
    [Rpc(SendTo.Everyone)]
    void ApplyDamageToPlayerClientRpc(ulong targetNetworkObjectId)
    {
        // OPTIMIZE BELOW
        // Assign the values to the payload to be sent with the event emission upon hitting the player
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject netObj))
        {
            GameObject other = netObj.gameObject;

            PlayerHitPayload spellPayload = new PlayerHitPayload
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
                spellDataScriptableObject.healAmount,
                spellDataScriptableObject.healOverTimeAmount,
                spellDataScriptableObject.spellAttribute,
                spellDataScriptableObject.pushback
            );

            EmitPayload(spellPayload);
        }
    }



    // I believe that the EmitPayload is received by all players and processed by all
    //but only works on the target player through a conditional
    void EmitPayload(PlayerHitPayload spellPayloadParam)
    {
        playerHitEvent?.Invoke(spellPayloadParam);
    }


    // Dispel destorys the other gameObject no matter its health points
    protected void Dispel(Collider other)
    {
        if (!other.gameObject.name.Contains("Projectile)"))
        {
            //Debug.LogFormat($"<color=blue>!!!!!!!!!!!!!! DISPEL {other.gameObject.name}</color>");
            DestroyOtherSpell(other);
        }
    }


    void DestroyOtherSpell(Collider colliderHit)
    {
        if (colliderHit.GetComponent<K_Spell>())
        {
            //Debug.LogFormat($"<color=blue>11111111!!!!!!!!!!!!!! DISPEL {colliderHit.gameObject.name}</color>");

            colliderHit.GetComponent<K_Spell>().DestroySpell(colliderHit.gameObject);
        }
        else if (colliderHit.GetComponent<SpellsClass>())
        {
            //Debug.LogFormat($"<color=blue>22222222!!!!!!!!!!!!!! DISPEL {colliderHit.gameObject.name}</color>");

            DestroySpell(colliderHit.gameObject);
        }
    }



    public void HandleAllInteractions(Collider colliderHit)
    {
        HandleSpellToSpellInteractions(colliderHit);
        HandleSpellToPlayerInteractions(colliderHit);

        // DestroyOnLayerImpact(colliderHit); 
    }

    //serverRPC
    //get the local health of the player involved
    //validate that the player health is similar to what is saved on the server
    //player health = 80
    //clientRPC get health >> is local health == server health

    SpellsClass SpellsClassScript(GameObject other)
    {
        if (other.GetComponent<SpellsClass>() != null)
        {
            return other.GetComponent<SpellsClass>();
        }
        else if (other.GetComponentInParent<SpellsClass>() != null)
        {
            return other.GetComponentInParent<SpellsClass>();

        }
        else if (other.GetComponentInChildren<SpellsClass>() != null)
        {
            return other.GetComponentInParent<SpellsClass>();
        } else
        {
            Debug.LogFormat($"<color=orange> !!! SPELLS CLASS SCRIPT IS NULL !!! </color>");

            return null;
        }
    }

    public bool HandleIfPlayerHasActiveShield(GameObject other)
    {
        Debug.LogFormat($"<color=orange> 000 FFFFFFFFFFFFFFFFFFFFFFFFFFFIRE {SpellsClassScript(other)} </color>");

        //if (other.GetComponent<NewPlayerBehavior>().localSphereShieldActive.Value == true || other.CompareTag("ActiveShield"))
        if (other.CompareTag("ActiveShield"))
        {
            // If the spell has hit an active shield, change the following value
            //Debug.LogFormat("<color=orange> ACTIVESHIELD (" + other.name + ")</color>");  
            hasHitShield.Value = true;

           Debug.LogFormat($"<color=orange> 1111 FFFFFFFFFFFFFFFFFFFFFFFFFFFIRE other name: {other.name} current barrier spell script: {GetComponent<BarrierSpell>()} current spellsClass script: {SpellsClassScript(other)} current ISpell: {GetComponent<ISpell>()} </color>");


            if (DamageOverTimeAmount > 0) 
            {
                Debug.LogFormat($"<color=orange> XXX FFFFFFFFFFFFFFFFFFFFFFFFFFFIRE {GetComponent<ISpell>().SpellName} </color>");

                otherGO = other.gameObject;

                // Only add a new DoT entry if it hasn't been added before
                if (!currentOnCollisionDoTList.Any(i => i.NetworkId == GetComponent<NetworkBehaviour>().NetworkBehaviourId))
                {
                    if (GetComponent<ISpell>().SpellName.Contains("Projectile_Fire"))
                    {
                        Debug.LogFormat($"<color=orange> YYY FFFFFFFFFFFFFFFFFFFFFFFFFFFIRE {GetComponent<ISpell>().SpellName} </color>");

                        other.GetComponent<K_SphereSpell>().currentOnCollisionDoTList.Add(new OnCollisionConstantDamageOverTime(GetComponent<NetworkBehaviour>().NetworkBehaviourId, GetComponent<ISpell>().Element.ToString(), GetComponent<ISpell>().DamageOverTimeAmount));

                    } else
                    {
                        Debug.LogFormat($"<color=orange> NNN FFFFFFFFFFFFFFFFFFFFFFFFFFFIRE {GetComponent<ISpell>().SpellName} </color>");

                        currentOnCollisionDoTList.Add(new OnCollisionConstantDamageOverTime(GetComponent<NetworkBehaviour>().NetworkBehaviourId, GetComponent<ISpell>().Element.ToString(), GetComponent<ISpell>().DamageOverTimeAmount));

                    }
                }
            }

            // TO DO: If the spell is a DoT apply DoT on the spell
            if (SpellDataScriptableObject.directDamageAmount > 0 || SpellDataScriptableObject.damageOverTimeAmount > 0)
            {
                // This is being called incorrectly from somewhere. Haven't figured out where or what yet.
                other.gameObject.GetComponent<K_SphereSpell>().TakeDamage(spellDataScriptableObject.directDamageAmount);
            }

            //if (SpellsClassScript(other) != null)
            //{
            //    SpellsClass spellsClass = SpellsClassScript(other);

            //    Debug.LogFormat($"<color=orange> 222 FFFFFFFFFFFFFFFFFFFFFFFFFFFIRE {other.gameObject.GetComponent<ISpell>().Element} </color>");

            //    if (other.gameObject.GetComponent<ISpell>().Element == Element.Fire) 
            //    {
            //        Debug.LogFormat("<color=orange> 333 FFFFFFFFFFFFFFFFFFFFFFFFFFFIRE </color>");

            //    }

            //    // TO DO: If the spell is a DoT apply DoT on the spell
            //    if (spellsClass.SpellDataScriptableObject.directDamageAmount > 0 || spellsClass.SpellDataScriptableObject.damageOverTimeAmount > 0)
            //    {
            //        // This is being called incorrectly from somewhere. Haven't figured out where or what yet.
            //        other.gameObject.GetComponent<K_SphereSpell>().TakeDamage(spellDataScriptableObject.directDamageAmount);
            //    }
            //}



            // What?
            if (spellDataScriptableObject.spellType.ToString() == "Projectile")
                DestroySpell(gameObject);


            return true;
        }
  
        return false;
    }


    public bool IsParriable()
    {
        return spellDataScriptableObject != null && spellDataScriptableObject.isParriable;
    }


    void DestroyOnLayerImpact(Collider colliderHit)
    {
        if (colliderHit.gameObject.layer == 7)
        {
            if (IsSpawned)
            {
                Debug.LogFormat("<color=orange> >>> PROJECTILE DESTROY BY >>> (" + colliderHit.name + ")</color>");
                DestroySpell(gameObject);
            }
        }
    }



    void HandleSpellToPlayerInteractions(Collider colliderHit)
    {
        Debug.LogFormat($"<color=purple>SPELL TO PLAYER INTERACTIONS - collider tag: {colliderHit.tag} collider </color>");

        if (HandleIfPlayerHasActiveShield(colliderHit.gameObject) == true) return;

        // Check for player hit
        if (colliderHit.CompareTag("Player"))
        {
            Debug.LogFormat($"<color=purple>********************* Handle the player HIT *****************************</color>");

            // If player does not have active shield, handle the player hit
            PlayerIsHit(colliderHit.gameObject);

            //Debug.LogFormat($"<color=purple>1 SPELLS CLASS: ApplyForce</color>");
            if (SpellDataScriptableObject.pushForce > 0)
            {
                //Debug.LogFormat($"<color=purple>2 SPELLS CLASS: ApplyForce</color>");
                colliderHit.gameObject.GetComponent<Pushback>().ApplyForce(transform.forward, SpellDataScriptableObject.pushForce);
            }
        }



        // Check if the target is a spell instead 
        if (colliderHit.CompareTag("Spell"))
        {
            //Handle the spell to spell interaction
            HandleSpellToSpellInteractions(colliderHit);
        }



        // DO NOT DELETE
        // If the collider of the other gameObjecy belongs to layer 7 (layer of gameObjects that destroy a projectile)
        //>>destroy the projectile
        //if (colliderHit.gameObject.layer == 7)
        //{
        //    if (IsSpawned)
        //    {
        //        Debug.LogFormat("<color=orange> >>> PROJECTILE DESTROY BY >>> (" + colliderHit.name + ")</color>");
        //        DestroySpellRpc();
        //    }
        //}
    }

    // DO NOT DELETE
    // This is to be implemented as the mandatory method that handles spells interactions
    //implemented on each different spell type and further subdivided by element
    //public abstract void HandleSpellToSpellInteraction(Collider colliderHit);

    public void HandleSpellToSpellInteractions(Collider colliderHit)
    {
        var ISpellComponent = colliderHit.GetComponent<ISpell>();
        var ISpellComponentInParent = colliderHit.GetComponentInParent<ISpell>();

        //Debug.LogFormat("<color=brown> Handle Spell To Spell Interactions (" + colliderHit.name + ")</color>");


        // If the other object that this gameObject has interacted with is a spell
        //>>handle the behavior of the spell interaction
        if (!colliderHit.CompareTag("Spell")) return;
 
        //Debug.LogFormat("<color=orange> ()()()()()()() (" + colliderHit.name + ")</color>");

        //Debug.LogFormat($"<color=green> '''''''''' DISPEL vars: SpellDataScriptableObject.dispel {SpellDataScriptableObject.dispel} AAND IsDispelResistant: {IsDispelResistant} </color>");


        // If the spell dispels other spells and the other spell hit is dispellable (or not resistant to dispels) destroy it.
        if (SpellDataScriptableObject.dispel == true && IsDispelResistant == false && !colliderHit.gameObject.name.Contains("Projectile"))
        {
            //Debug.LogFormat("<color=blue> ][][][][] DISPEL TRUU (" + colliderHit.name + ")</color>");

            DestroyOtherSpell(colliderHit);
        }



        if (ISpellComponent != null && ISpellComponent.SpellName.Contains("Barrier"))
        {
            //Debug.LogFormat("<color=orange> Projectile hit barrier (" + colliderHit.name + ")</color>");

            // IF colliderHit.GetComponent<IDamageable>() != null
            if (colliderHit.gameObject.GetComponent<BarrierSpell>().SpellDataScriptableObject.health > 1) // 1 is minimum ie. undamageable
            {
                //Debug.LogFormat("<color=orange> Projectile hit barrier (" + colliderHit.name + ")</color>");

                // BarrierSpell barrierScript = colliderHit.GetComponentInParent<BarrierSpell>();

                // IF colliderHit.GetComponent<IDamageable>() != null
                if (colliderHit.gameObject.GetComponent<BarrierSpell>().SpellDataScriptableObject.health > 1) // 1 is minimum ie. undamageable
                {
                    //Debug.LogFormat("<color=orange> Projectile hit barrier (" + colliderHit.name + ")</color>");

                    colliderHit.gameObject.GetComponent<BarrierSpell>().ApplyDamage(SpellDataScriptableObject.directDamageAmount); //This is causing an error. No idea why.

                }
                if (!gameObject.name.Contains("Explosion"))
                {
                    DestroySpell(gameObject);
                }
            }
            else if (ISpellComponentInParent != null && ISpellComponentInParent.SpellName.Contains("Scepter"))
            {
                //Debug.LogFormat("<color=orange> Projectile hit SCEPTER (" + colliderHit.name + ")</color>");

                InvocationSpell invocationSpell = colliderHit.gameObject.GetComponentInParent<InvocationSpell>();

                if (invocationSpell.SpellDataScriptableObject.health > 1)
                {
                    invocationSpell.ApplyDamage(SpellDataScriptableObject.directDamageAmount);
                }

                if (!gameObject.name.Contains("Explosion"))
                {
                    DestroySpell(gameObject);
                }

                colliderHit.gameObject.GetComponent<BarrierSpell>().ApplyDamage(SpellDataScriptableObject.directDamageAmount); //This is causing an error. No idea why.

            }


            if (DamageOverTimeAmount > 0)
            {
                Debug.LogFormat($"<color=orange> ;;;;;; FFFFFFFFFFFFFFFFFFFFFFFFFFFIRE {GetComponent<ISpell>().SpellName} OTHER: {colliderHit.gameObject}</color>");

                otherGO = colliderHit.gameObject;

                // Only add a new DoT entry if it hasn't been added before
                if (!currentOnCollisionDoTList.Any(i => i.NetworkId == GetComponent<NetworkBehaviour>().NetworkBehaviourId))
                {
                    // If the spell with DoT effect is destroyed on contact make the spell self handle the DoT 
                    // else have the other spell handle it.
                    // TO DO: Could be written differently: if oncollisiondestroy is true
                    if (GetComponent<ISpell>().SpellName.Contains("Projectile_Fire"))
                    {
                        Debug.LogFormat($"<color=orange> YYY FFFFFFFFFFFFFFFFFFFFFFFFFFFIRE {GetComponent<ISpell>().SpellName} </color>");

                        colliderHit.GetComponent<BarrierSpell>().currentOnCollisionDoTList.Add(new OnCollisionConstantDamageOverTime(GetComponent<NetworkBehaviour>().NetworkBehaviourId, GetComponent<ISpell>().Element.ToString(), GetComponent<ISpell>().DamageOverTimeAmount));

                if (!gameObject.GetComponent<SpellsClass>().SpellDataScriptableObject.dispel && !gameObject.GetComponent<SpellsClass>().SpellDataScriptableObject.spawnsSecondaryEffectOnCollision)
                {
                    DestroySpell(gameObject);
                    }
                    else
                    {
                        Debug.LogFormat($"<color=orange> NNN FFFFFFFFFFFFFFFFFFFFFFFFFFFIRE {GetComponent<ISpell>().SpellName} </color>");

                        currentOnCollisionDoTList.Add(new OnCollisionConstantDamageOverTime(GetComponent<NetworkBehaviour>().NetworkBehaviourId, GetComponent<ISpell>().Element.ToString(), GetComponent<ISpell>().DamageOverTimeAmount));

                    }
                }
            }


    //public void ApplyDamage(float one)
    //{

    //}


    public virtual void FixedUpdate()
    {
        if (!IsSpawned) return;

        if (spellLifetimeActive)
        {
            spellLifetimeTimer += Time.fixedDeltaTime;

            if (spellLifetimeTimer >= spellLifetimeDuration)
            {
                spellLifetimeActive = false;
                DestroySpell(gameObject);
            if (!gameObject.name.Contains("Explosion"))
            {
                DestroySpellRpc();
            }
        }
        else if (ISpellComponentInParent != null && ISpellComponentInParent.SpellName.Contains("Scepter"))
        {
            //Debug.LogFormat("<color=orange> Projectile hit SCEPTER (" + colliderHit.name + ")</color>");

            InvocationSpell invocationSpell = colliderHit.gameObject.GetComponentInParent<InvocationSpell>();

            if (invocationSpell.SpellDataScriptableObject.health > 1)
            {
                invocationSpell.ApplyDamage(SpellDataScriptableObject.directDamageAmount);
            }

            if (!gameObject.name.Contains("Explosion"))
            {
                DestroySpellRpc();
            }


        }
        else if (ISpellComponentInParent != null && ISpellComponentInParent.SpellName.Contains("Aoe"))
        {
            //Debug.LogFormat("<color=orange> hit AOE (" + colliderHit.name + ")</color>");

            AoeSpell aoeSpell = colliderHit.gameObject.GetComponentInParent<AoeSpell>();

            if (aoeSpell.SpellDataScriptableObject.health > 1)
            {
                aoeSpell.ApplyDamage(SpellDataScriptableObject.directDamageAmount);
            }

            if (!gameObject.GetComponent<SpellsClass>().SpellDataScriptableObject.dispel && !gameObject.GetComponent<SpellsClass>().SpellDataScriptableObject.spawnsSecondaryEffectOnCollision)
            {
                DestroySpellRpc();
            }
        }
        
    }





    #region Spell Duration handling and destruction
    public IEnumerator LifeTime(float duration, GameObject spellObj)
    {
        gameObjectToDestroy = spellObj;

        //Debug.LogFormat($"<color=orange>Spell {gameObjectToDestroy} will be destroyed in {duration} seconds</color>", spellObj.name, duration);

        yield return new WaitForSeconds(duration);
        DestroySpell(gameObject);
    }


    public void StartLifeTime(float duration, GameObject spellObj)
    {
        gameObjectToDestroy = spellObj;
        spellLifetimeDuration = duration;
        spellLifetimeTimer = 0f;
        spellLifetimeActive = true;

        //Debug.LogFormat($"<color=orange>Spell {gameObjectToDestroy} will be destroyed in {duration} physics seconds</color>", spellObj.name, duration);
    }



    public void DestroySpell(GameObject spellObj)
    {
        NetworkObject netObj = spellObj.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            DestroySpellRpc(netObj);
        }
    }




    [Rpc(SendTo.Server)]
    public void DestroySpellRpc(NetworkObjectReference netObjRef)
    {
        // Try to get the NetworkObject from the reference passed as a parameter.
        if (netObjRef.TryGet(out NetworkObject netObjToDestroy))
        {
            // Check if the object still exists and is spawned before trying to despawn it.
            if (netObjToDestroy != null && netObjToDestroy.IsSpawned)
            {
                Debug.LogFormat($"<color=orange>Server is despawning {netObjToDestroy.name}</color>");
                netObjToDestroy.Despawn();
            }
            else
            {
                Debug.LogWarning($"Server received DestroySpellRpc, but the object was already destroyed or despawned.");
            }
        }
        else
        {
            Debug.LogWarning($"Server could not find the NetworkObject from the reference in DestroySpellRpc.");
        }
    }
    #endregion

    #region Only REFERENCABLE Charm Methods 
    protected void GradualScale(float scaleSpeed, float maxScale)
    {
        //float scaleSpeed = 5f;
        float deltaScale = scaleSpeed * Time.fixedDeltaTime;
        //float maxScale = 1.4f;

        // Only increase if current scale is less than maxScale
        if (transform.localScale.x < maxScale)
        {
            float newScale = Mathf.Min(transform.localScale.x + deltaScale, maxScale);
            transform.localScale = new Vector3(newScale, newScale, newScale);
        } else
        {
            // After the gameObject/ sphere has reached max size, destroy the gameObject
            DestroySpell(gameObject);
        }
    }
    #endregion
}

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using static ProjectileSpell;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Net.Security;

public class SpellsClass : NetworkBehaviour, ISpell
{
    [SerializeField]
    private K_SpellData spellDataScriptableObject;

    GameObject gameObjectToDestroy;

    [SerializeField]
    protected GameObject secondaryGameObjectToSpawn;

    //private static GameObject spellsExplosionGO;

    //private static AssetReferenceGameObject spellsExplosionAR;


    public K_SpellData SpellDataScriptableObject
    {
        get { return spellDataScriptableObject; }
    }

    public delegate void PlayerHitEvent(PlayerHitPayload damageInfo);
    public static event PlayerHitEvent playerHitEvent;

    protected Rigidbody rb;


    // These are being defined in the scriptable object associated to each prefab
    public string SpellName => SpellDataScriptableObject.name;
    public bool IsDispelResistant => SpellDataScriptableObject.isDispelResistant;
    public float DirectDamage => SpellDataScriptableObject.directDamageAmount;



    NetworkVariable<bool> hasHitShield = new NetworkVariable<bool>(false,
    NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);

    NetworkVariable<float> healthPoints = new NetworkVariable<float>(0,
    NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);

    protected NetworkVariable<bool> isSpellActive = new NetworkVariable<bool>(false,
    NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);


    private float spellLifetimeTimer = 0f;
    private float spellLifetimeDuration = 0f;
    private bool spellLifetimeActive = false;


    protected float checkRadius = 2f;    // Match your trigger size
    protected LayerMask triggerLayer;    // Layer for the kill trigger


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


        //if (spellsExplosionAR == null)
        //{
        //    // Replace the line causing the error with the following code:
        //    spellsExplosionAR.LoadAssetAsync("Assets/Scripts/Player/KotsSpellCasting/Spells/Charm/Explosive/Explosion.prefab").Completed += ;

        //    Debug.LogFormat($"<color=orange>Spells Explosion GameObject loaded: {spellsExplosionGO}</color>");
        //}


        // if (SpellDataScriptableObject.spellTimeBeforeDeactivation > 0)
        // {
        //     StartCoroutine(SpellDeactivation());
        // }
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

    //void ApplyDamageToSpell()
    //{
    //    healthPoints.Value -= damage;
    //    Debug.LogFormat($"<color=orange>armorPoints: {healthPoints}</color>");

    //    if (healthPoints.Value <= 0)
    //    {
    //        // DestroyBarrierRpc();
    //        DestroySpellRpc();
    //    }
    //}







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
            return null;
        }
    }

    public bool HandleIfPlayerHasActiveShield(GameObject other)
    {
        //if (other.gameObject.CompareTag("Player"))
        //{
        //if (!other.CompareTag("Player")) return false;

        if (other.GetComponent<NewPlayerBehavior>().localSphereShieldActive.Value == true)
        {
            // If the spell has hit an active shield, change the following value
            //Debug.LogFormat("<color=orange> ACTIVESHIELD (" + other.name + ")</color>");
            hasHitShield.Value = true;
            //other.gameObject.GetComponent<K_SphereSpell>().TakeDamage(spellDataScriptableObject.directDamageAmount);
            if (SpellsClassScript(other) != null)
            {
                SpellsClass spellsClass = SpellsClassScript(other);

                if (spellsClass.SpellDataScriptableObject.directDamageAmount > 0 || spellsClass.SpellDataScriptableObject.damageOverTimeAmount > 0)
                {
                    // This is being called incorrectly from somewhere. Haven't figured out where or what yet.
                    other.gameObject.GetComponent<K_SphereSpell>().TakeDamage(spellDataScriptableObject.directDamageAmount);
                }
            }



            // What?
            if (spellDataScriptableObject.spellType.ToString() == "Projectile")
                DestroySpellRpc();


            return true;
        }
        else
        {
            return false;
        }
    }



    void DestroyOnLayerImpact(Collider colliderHit)
    {
        if (colliderHit.gameObject.layer == 7)
        {
            if (IsSpawned)
            {
                Debug.LogFormat("<color=orange> >>> PROJECTILE DESTROY BY >>> (" + colliderHit.name + ")</color>");
                DestroySpellRpc();
            }
        }
    }



    void HandleSpellToPlayerInteractions(Collider colliderHit)
    {
        //.LogFormat($"<color=purple>SPELL TO PLAYER INTERACTIONS {colliderHit.tag}</color>");

        if (HandleIfPlayerHasActiveShield(colliderHit.gameObject) == true) return;
        
        // Check for player hit
        if (colliderHit.CompareTag("Player"))
        {
            //Debug.LogFormat($"<color=purple>HAS SHIELD {colliderHit.tag}</color>");

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
        //bool isBarrier = ISpellComponent.SpellName.Contains("Barrier");
        //bool isScepter = ISpellComponent.SpellName.Contains("Scepter");

        //Debug.LogFormat("<color=brown> Handle Spell To Spell Interactions (" + colliderHit.name + ")</color>");

        // Collider objectHit = colliderHit;
        // If the other object that this gameObject has interacted with is a spell
        //>>handle the behavior of the spell interaction
        if (colliderHit.CompareTag("Spell"))
        {
            //Debug.LogFormat("<color=orange> ()()()()()()() (" + colliderHit.name + ")</color>");

            // && colliderHit.GetComponent<ProjectileSpell>().Spell

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

                // BarrierSpell barrierScript = colliderHit.GetComponentInParent<BarrierSpell>();

                // IF colliderHit.GetComponent<IDamageable>() != null
                if (colliderHit.gameObject.GetComponent<BarrierSpell>().SpellDataScriptableObject.health > 1) // 1 is minimum ie. undamageable
                {
                    //Debug.LogFormat("<color=orange> Projectile hit barrier (" + colliderHit.name + ")</color>");

                    colliderHit.gameObject.GetComponent<BarrierSpell>().ApplyDamage(SpellDataScriptableObject.directDamageAmount); //This is causing an error. No idea why.

                }
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

                //if (!gameObject.name.Contains("Explosion"))
                //{
                //    DestroySpellRpc();
                //}

                if (!gameObject.GetComponent<SpellsClass>().SpellDataScriptableObject.dispel && !gameObject.GetComponent<SpellsClass>().SpellDataScriptableObject.spawnsSecondaryEffectOnCollision)
                {
                    DestroySpellRpc();
                }
            }
        }
    }


    //public void ApplyDamage(float one)
    //{

    //}


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


    }


    #region Spell Duration handling and destruction
    public IEnumerator LifeTime(float duration, GameObject spellObj)
    {
        gameObjectToDestroy = spellObj;

        //Debug.LogFormat($"<color=orange>Spell {gameObjectToDestroy} will be destroyed in {duration} seconds</color>", spellObj.name, duration);

        yield return new WaitForSeconds(duration);
        DestroySpellRpc();
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
        gameObjectToDestroy = spellObj;

        DestroySpellRpc();
    }




    [Rpc(SendTo.Server)]
    public void DestroySpellRpc()
    {

        //Debug.LogFormat($"<color=orange>Destroying spell {gameObjectToDestroy.name}</color>");
        
        //Destroy(gameObjectToDestroy);

        if (gameObjectToDestroy.GetComponent<NetworkObject>() != null)
        {
            //Debug.LogFormat($"<color=orange>Spell to destroy {gameObjectToDestroy.name} has a NetworkObject</color>");
            gameObjectToDestroy.GetComponent<NetworkObject>().Despawn();
        }
        else
        {
            //Debug.LogFormat($"<color=orange>Spell to destroy {gameObjectToDestroy.name} does NOT have a NetworkObject</color>");
            gameObjectToDestroy.GetComponentInParent<NetworkObject>().Despawn();
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

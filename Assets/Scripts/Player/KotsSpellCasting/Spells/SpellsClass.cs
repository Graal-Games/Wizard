using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using static ProjectileSpell;
 
public class SpellsClass : NetworkBehaviour, ISpell
{
    [SerializeField]
    private K_SpellData spellDataScriptableObject;

    GameObject gameObjectToDestroy;

    public K_SpellData SpellDataScriptableObject
    {
        get { return spellDataScriptableObject; }
    }

    public delegate void PlayerHitEvent(PlayerHitPayload damageInfo);
    public static event PlayerHitEvent playerHitEvent;

    protected Rigidbody rb;

    public string SpellName => SpellDataScriptableObject.name;

    NetworkVariable<bool> hasHitShield = new NetworkVariable<bool>(false,
    NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);

    NetworkVariable<float> healthPoints = new NetworkVariable<float>(0,
    NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);

    protected NetworkVariable<bool> isSpellActive = new NetworkVariable<bool>(false,
    NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);



    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = false;
    }




    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        StartCoroutine(LifeTime(SpellDataScriptableObject.spellDuration, this.gameObject));

        // If the spell has a health value greater than 0, set the healthPoints variable
        // This is used to apply damage to the spell itself and handle it's (delayed) destruction
        if (SpellDataScriptableObject.health > 0)
        {
            healthPoints.Value = SpellDataScriptableObject.health;
        }

        SpellActivationDelay();

        // if (SpellDataScriptableObject.spellTimeBeforeDeactivation > 0)
        // {
        //     StartCoroutine(SpellDeactivation());
        // }
    }



    void SpellActivationDelay()
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



    void ActivateSpell()
    {
        // Logic to activate the spell
        gameObject.GetComponent<Collider>().enabled = true;
        isSpellActive.Value = true;
    }



    public void SpellDeactivationDelay(Collider colliderToDeactivate = null)
    {
        Debug.LogFormat("<color=orange>111SpellDeactivationDelay called with null collider</color>");

        if (colliderToDeactivate == null)
        {
            Debug.LogFormat("<color=orange>SpellDeactivationDelay called with null collider</color>");
            colliderToDeactivate = gameObject.GetComponent<Collider>();
        }
        else
        {
            Debug.LogFormat("<color=orange>SpellDeactivationDelay called with collider: {0}</color>", colliderToDeactivate.name);
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




    public void HandleAllInteractions(Collider colliderHit)
    {
        HandleSpellToSpellInteractions(colliderHit);
        HandleSpellToPlayerInteractions(colliderHit);
    }

    //serverRPC
    //get the local health of the player involved
    //validate that the player health is similar to what is saved on the server
    //player health = 80
    //clientRPC get health >> is local health == server health

    public bool HandleIfPlayerHasActiveShield(GameObject other)
    {
        // If shield is detected redirect damage to it
        // And DO NOT proceed to apply damage to the related player
        if (other.CompareTag("ActiveShield"))
        {
            // This is being called incorrectly from somewhere. Haven't figured out where or what yet.
            other.gameObject.GetComponent<K_SphereSpell>().TakeDamage(spellDataScriptableObject.directDamageAmount);

            hasHitShield.Value = true;

            // What?
            if (spellDataScriptableObject.spellType.ToString() == "Projectile")
                DestroySpellRpc();


            return true;
        }

        return false;
    }






    void HandleSpellToPlayerInteractions(Collider colliderHit)
    {

        if (HandleIfPlayerHasActiveShield(colliderHit.gameObject) == false)
        {
            // Check for player hit
            if (colliderHit.CompareTag("Player"))
            {
                // If player does not have active shield, handle the player hit
                PlayerIsHit(colliderHit.gameObject);
            }
            return;
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
        // if (colliderHit.gameObject.layer == 7)
        // {
        //     if (IsSpawned)
        //     {
        //         Debug.LogFormat("<color=orange> >>> PROJECTILE DESTROY BY >>> (" + colliderHit.name + ")</color>");
        //         DestroySpellRpc();
        //     }
        // }
    }

    // DO NOT DELETE
    // This is to be implemented as the mandatory method that handles spells interactions
    //implemented on each different spell type and further subdivided by element
    //public abstract void HandleSpellToSpellInteraction(Collider colliderHit);

    public void HandleSpellToSpellInteractions(Collider colliderHit)
    {
        // Collider objectHit = colliderHit;
        // If the other object that this gameObject has interacted with is a spell
        //>>handle the behavior of the spell interaction
        if (colliderHit.CompareTag("Spell"))
        {
            var spellComponent = colliderHit.GetComponent<ISpell>();

            Debug.LogFormat("<color=orange> Spell hit (" + colliderHit.name + ")</color>");

            if (spellComponent != null && spellComponent.SpellName.Contains("Barrier"))
            {
                Debug.LogFormat("<color=orange> Projectile hit barrier (" + colliderHit.name + ")</color>");

                BarrierSpell barrierScript = colliderHit.GetComponentInParent<BarrierSpell>();

                if (barrierScript.SpellDataScriptableObject.health > 1) // 1 is minimum ie. undamageable
                {
                    Debug.LogFormat("<color=orange> Projectile hit barrier (" + colliderHit.name + ")</color>");

                    colliderHit.gameObject.GetComponent<BarrierSpell>().ApplyDamage(SpellDataScriptableObject.directDamageAmount); //This is causing an error. No idea why.

                }

                DestroySpellRpc();
            }
            else if (colliderHit.GetComponentInParent<ISpell>() != null && colliderHit.GetComponentInParent<ISpell>().SpellName.Contains("Scepter"))
            {
                InvocationSpell invocationSpell = colliderHit.gameObject.GetComponentInParent<InvocationSpell>();

                if (invocationSpell.SpellDataScriptableObject.health > 1)
                {
                    invocationSpell.ApplyDamage(SpellDataScriptableObject.directDamageAmount);
                }

                DestroySpellRpc();

            }
            // else if (colliderHit.GetComponentInParent<ISpell>() != null && colliderHit.GetComponentInParent<ISpell>().SpellName.Contains("Projectile"))
            // {

            // }
        }
    }




    #region Spell Duration handling and destruction
    public IEnumerator LifeTime(float duration, GameObject spellObj)
    {
        gameObjectToDestroy = spellObj;

        yield return new WaitForSeconds(duration);
        DestroySpellRpc();
    }




    public void DestroySpell(GameObject spellObj)
    {
        gameObjectToDestroy = spellObj;

        DestroySpellRpc();
    }




    [Rpc(SendTo.Server)]
    public void DestroySpellRpc()
    {
        Destroy(gameObjectToDestroy);

        if (gameObjectToDestroy.GetComponent<NetworkObject>() != null)
        {
            gameObjectToDestroy.GetComponent<NetworkObject>().Despawn();
        }
        else
        {
            gameObjectToDestroy.GetComponentInParent<NetworkObject>().Despawn();
        }
    }
    #endregion
}

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
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

    public Rigidbody rb;

    public string SpellName => SpellDataScriptableObject.name;

    NetworkVariable<bool> hasHitShield = new NetworkVariable<bool>(false,
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

        StartCoroutine(LifeTime(spellDataScriptableObject.spellDuration, this.gameObject));
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

    //serverRPC
    //get the local health of the player involved
    //validate that the player health is similar to what is saved on the server
    //player health = 80
    //clientRPC get health >> is local health == server health

    public bool HandleIfPlayerHasActiveShield(GameObject other)
    {
        Debug.LogFormat("<color=orange> 1 >>> PROJECTILE HIT SHIELD >>> (" + other.gameObject.name + ")</color>");

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

            Debug.LogFormat("<color=orange> 3 >>> PROJECTILE HIT SHIELD >>> (" + other.gameObject.name + ")</color>");


            return true;
        }
        Debug.LogFormat("<color=orange> 4 >>> PROJECTILE HIT SHIELD >>> (" + other.gameObject.name + ")</color>");
        return false;
    }


    public void HandleSpellToSpellInteractions(GameObject other)
    {
        // If the other object that this gameObject has interacted with is a spell
        //>>handle the behavior of the spell interaction
        if (other.CompareTag("Spell"))
        {
            if (other.GetComponent<ISpell>().SpellName.Contains("Barrier"))
            {
                BarrierSpell barrierScript = other.GetComponentInParent<BarrierSpell>();

                if (barrierScript.SpellDataScriptableObject.health > 1) // 1 is minimum ie. undamageable
                {
                    other.gameObject.GetComponent<BarrierSpell>().ApplyDamage(SpellDataScriptableObject.directDamageAmount); //This is causing an error. No idea why.
                    //DestroySpellRpc();
                }

                //Debug.LogFormat("<color=orange> 2222222 >>> PROJECTILE DESTROY BY >>> (" + other.gameObject.name + ")</color>");
                DestroySpellRpc();
            }

            if (other.gameObject.name.Contains("Scepter"))
            {
                InvocationSpell invocationSpell = other.gameObject.GetComponentInParent<InvocationSpell>();

                if (invocationSpell.SpellDataScriptableObject.health > 1)
                {
                    //Debug.LogFormat("<color=orange> SSSSSSSS >>> PROJECTILE DESTROY BY >>> (" + gameObject.name + ")</color>");

                    invocationSpell.ApplyDamage(SpellDataScriptableObject.directDamageAmount);
                }

                //Debug.LogFormat("<color=orange> SSSSSSSS >>> PROJECTILE DESTROY BY >>> (" + other.gameObject.name + ")</color>");
                DestroySpellRpc();

            }
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
        Debug.LogFormat($"<color=pink>gameObjectgameObjectgameObject {gameObjectToDestroy}</color>");

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

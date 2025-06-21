using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static ProjectileSpell;

public class SpellsClass : NetworkBehaviour
{
    [SerializeField]
    private K_SpellData spellDataScriptableObject;

    public K_SpellData SpellDataScriptableObject
    {
        get { return spellDataScriptableObject; }
    }

    public delegate void PlayerHitEvent(PlayerHitPayload damageInfo);
    public static event PlayerHitEvent playerHitEvent;

    //PlayerHitPayload spellPayload;

    public Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        //K_SphereSpell.shieldExists += ShieldAliveStatus;
    }


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

    //// This method instantiates a new instance of PlayerHitPayload struct that was defined elsewhere
    //// Thereafter, once the spell has come into contact with a player the information required to handle player damage, along with the inflicted target player's clientId
    //// Once the infomation is set, it is sent via an event that is emitted, thereafter received in the PlayerBehaviour script and handled there accordingly
    //// Note parameters order
    //public void SpellPayloadConstructor(int netId, ulong pId, string element, IncapacitationName incapName, float incapDur, VisionImpairment visionImp, float visionImpDur, float ddAmount, float dotAmount, float dotDur, SpellAttribute type, bool pushback)
    //{
    //    Debug.LogFormat($"<color=orange>PAYLOAD CONSTRUCTOR</color>");
    //    // This is a struct that is defined in its own script
    //    // It is used to send information about the spell that hit a player for damage and effects handling
    //    spellPayload = new PlayerHitPayload
    //    {
    //        // The left side values are defined in the PlayerHitPayload struct
    //        NetworkId = netId,
    //        PlayerId = pId,
    //        SpellElement = element,
    //        IncapacitationName = incapName,
    //        IncapacitationDuration = incapDur,
    //        VisionImpairment = visionImp,
    //        VisionImpairmentDuration = visionImpDur,
    //        DirectDamageAmount = ddAmount,
    //        DamageOverTimeAmount = dotAmount,
    //        SpellAttribute = type,
    //        DamageOverTimeDuration = dotDur,
    //        Pushback = pushback
    //    };
    //}

    //serverRPC
    //get the local health of the player involved
    //validate that the player health is similar to what is saved on the server
    //player health = 80
    //clientRPC get health >> is local health == server health

    public virtual void PlayerIsHit(GameObject other)
    {
        Debug.LogFormat($"<color=orange>PLAYERISHIT</color>");


        //// Get the NetworkObjectId of the other GameObject involved in the collision
        //ulong networkObjectId = other.gameObject.GetComponent<NetworkObject>().NetworkObjectId;

        //// Attempt to retrieve the NetworkObject from the SpawnManager using its ID retrieved above
        //if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
        //{
        //    // Get the client ID that owns this NetworkObject
        //    ulong ownerId = netObj.OwnerClientId;

        //    // Log the ownership information for debugging purposes
        //    Debug.Log($"Object {networkObjectId} is owned by client {ownerId}");

        //    // Send an RPC to apply damage only to the owning client of this object
        //    ApplyDamageToPlayerClientRpc
        //    (
        //        other.GetComponent<NetworkObject>().OwnerClientId,
        //        RpcTarget.Single(ownerId, RpcTargetUse.Temp)
        //    );

        //}

        //NetworkObject targetNetObj = other.gameObject.GetComponent<NetworkObject>();

        //if (targetNetObj != null)
        //{
        //    var clientRpcParams = new ClientRpcParams
        //    {
        //        Send = new ClientRpcSendParams
        //        {
        //            TargetClientIds = new[] { targetNetObj.OwnerClientId }
        //        }
        //    };
        //}

        // 'other' is the GameObject you want to reference
        ulong targetNetworkObjectId = other.GetComponent<NetworkObject>().NetworkObjectId;
        ApplyDamageToPlayerClientRpc(targetNetworkObjectId);

        // Apply damage HealthBarUi

    }


}

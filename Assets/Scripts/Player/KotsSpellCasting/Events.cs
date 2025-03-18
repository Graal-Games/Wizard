using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Events
{
    public class Events : NetworkBehaviour
    {
        ////public delegate void PlayerHitEvent(ulong playerHit, float damage, DamageType damageType);
        ////public static event PlayerHitEvent playerHitEvent;
        //PlayerHitPayload playerHitPayload;

        //NetworkVariable<bool> hasHitShield = new NetworkVariable<bool>(false,
        //    NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);

        //public void SpellPayloadConstructor(int netId, ulong pId, string element, string incap, float incapDur, float ddAmount, float dotAmount, float dotDur, SpellAttribute type)
        //{
        //    // This is a struct that is defined in its own script
        //    // It is used to send information about the spell that hit a player for damage and effects handling
        //    playerHitPayload = new PlayerHitPayload
        //    {
        //        NetworkId = netId,
        //        PlayerId = pId,
        //        SpellElement = element,
        //        IncapacitationType = incap,
        //        IncapacitationDuration = incapDur,
        //        DirectDamageAmount = ddAmount,
        //        DamageOverTimeAmount = dotAmount,
        //        SpellAttribute = type,
        //        DamageOverTimeDuration = dotDur
        //    };
        //}

        //public void SpellPayload(Collider other)
        //{
        //    Debug.LogFormat($"<color=orange>1 SPELL PAYLOAD 1: {other.gameObject.GetComponent<NetworkObject>().GetInstanceID()}</color>");

        //    // If the player has a shield on, ignore damage application
        //    if (other.gameObject.GetComponent<K_ProjectileSpell>().SpellDataScriptableObject.spellType.ToString() == "Projectile")
        //        if (hasHitShield.Value == true) return;

        //    Debug.LogFormat($"<color=orange>2 SPELL PAYLOAD 2: {other.gameObject.GetComponent<NetworkObject>().GetInstanceID()}</color>");

        //    SpellPayloadConstructor
        //    (
        //        this.gameObject.GetInstanceID(),
        //        other.GetComponent<NetworkObject>().OwnerClientId,
        //        spellDataScriptableObject.element.ToString(),
        //        spellDataScriptableObject.incapacitation.ToString(),
        //        spellDataScriptableObject.incapacitationDuration,
        //        spellDataScriptableObject.directDamageAmount,
        //        spellDataScriptableObject.damageOverTimeAmount,
        //        spellDataScriptableObject.damageOverTimeDuration,
        //        spellDataScriptableObject.spellAttribute
        //    );

        //    PlayerIsHit(); // This emits an event that applies damage to the target on the behavior and the GM script  >> NEED TO PASS ALL RELEVANT DATA HERE
        //    hasHitPlayer = true;
        //}
    }

}

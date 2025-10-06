using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BarrierSpell : BarrierClass // WARNING: TO RENAME TO BARRIERCLASS
{
    //// For health, get reference to the associated spellData
    //NetworkVariable<float> localHealth = new NetworkVariable<float>();

    //void Start()
    //{
    //    //localHealth.Value = SpellDataScriptableObject.health;
    //}

    //// This code can (can it?) be placed in the K_Spell script
    //// Revision the K_Spell script cannot get the damage to apply unless specifically sent through by the projectile script
    //public void ApplyDamage(float damage)
    //{
    //    localHealth.Value -= damage;
    //    Debug.LogFormat($"<color=orange>armorPoints: {localHealth.Value}</color>");
    //    Debug.LogFormat($"<color=orange>gameObject: {gameObject} parent: {gameObject.transform.parent }</color>");

    //    GameObject thisGameObject;

    //    if (gameObject.GetComponent<NetworkObject>())
    //    {
    //        thisGameObject = gameObject;
    //    }
    //    else
    //    {
    //        thisGameObject = gameObject.transform.parent.gameObject;
    //    }

    //    if (localHealth.Value <= 0)
    //    {
    //        DestroySpell(gameObject);
    //    }
    //}

}

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class StunAndDamageTrigger : MonoBehaviour
{
    //bool hasHitShield;
    //List <ulong> playersWithShieldActive = new List<ulong>();

    //public bool HasHitShield
    //{
    //    get { return hasHitShield; }
    //    set { hasHitShield = value; }
    //}

    //// If one player has a shield and the other does not
    ////then we need to have a list of the players that have
    ////their shield on and send them a bool value that would allow them
    ////to avoid taking damage or getting incapacitated
    //void CheckPlayerShield()
    //{

    //}

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.gameObject.name.Contains("SphereShield"))
    //    {
    //        //int playerId = other.GetComponentInParent<NetworkObject>().NetworkObjectId;
    //        ulong playerId = other.GetComponentInParent<NetworkObject>().OwnerClientId;

    //        if (playersWithShieldActive.Contains(playerId)) 
    //        {
    //            return; 
    //        } else
    //        {
    //            playersWithShieldActive.Add(playerId);
    //        }
            

    //        CheckPlayerShield();
    //        // Get the id of the player
    //        // if the player id has sphere shield on
    //        // mark hasHitShield as true for specified player
    //        hasHitShield = true;
    //    }
    //}
}

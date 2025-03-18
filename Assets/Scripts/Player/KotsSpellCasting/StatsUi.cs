using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Singletons;

// I created this because (possibly) I wasn't getting the correct reference to the player's healthbar ui
//this assisted in locating it correctly
public class StatsUi : Singleton<StatsUi>
{

    private Transform player; // Reference to player transform

    public void GetPlayer(Transform playerReference)
    {
        player = playerReference;

        if (player != null)
        {
            transform.SetParent(player, true); 
        }
    }
}

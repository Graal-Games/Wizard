using DotTimers;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using static Beam;

public class GameManager2 : NetworkBehaviour
{
    //HealthBar _healthBarUi;
    NetworkObject _networkObject;

    private Dictionary<ulong, PlayerClass> playersInfo = new Dictionary<ulong, PlayerClass>();

    private Dictionary<int, DefaultDotTimer> dotTimers = new Dictionary<int, DefaultDotTimer>();

    PlayerClass playerClass;
    //public class NetworkDictionary<ulong, PlayerClass> playerInfoNetDict = new NetworkDictionary<ulong, PlayerClass>();
    //bool firstTime = true;

    private void OnEnable()
    {
        Debug.Log($"****************************************OnEnable!");
        _networkObject = this.GetComponent<NetworkObject>();
        // Specifies the method to be used that will handle the player spawn event, which sends a payload upon player connection
        NewPlayerBehavior.playerHasSpawnedEvent += AccessTest;

        NewPlayerBehavior.playerHasSpawnedEvent2 += AccessTest2;

        K_Spell.playerHitEvent += HandleSpellInflicted;
            //.beamHitPlayer += BeamHitPlayer;
    }

    private void Start()
    {
        Debug.Log($"****************************************StartGameMAnager!");
        //_healthBarUi = StatsUi.Instance.GetComponent<StatsUi>().GetComponentInChildren<HealthBar>();
        //_healthBarUi.SetMaxHealth(100f);

    }


    void Update()
    {
        foreach (var kvp in dotTimers)
        {
            DefaultDotTimer dotTimer = kvp.Value;

            // These two variables are controlled through on trigger enter and exit
            //if (dotTimer.IsInteractingWithSpell || dotTimer.IsDotPersistent) // In the original code, the player carried with him both the direct dot damage as well as teh persistent dot damage.
            if (dotTimer.IsInteractingWithSpell || dotTimer.IsDotPersistent) // Is DoT persistent is important to have to apply damage only when the player is no longer in contact with the 
            {
                // player reference then apply damage: ApplyDotDamage();
            }
        }
    }

    // This event is emitted by spells (curently the projectile)
    void HandleSpellInflicted(PlayerHitPayload emittedPlayerHitPayload)
    {
        Debug.Log("Handling spell inflicted");
    }

    // SPLIT THE GAME MANAGER INTO TWO

    // 1 --- LOCAL CONTROLS AND FUNCTIONALITIES
    /* 
        Calculations are made locally (then sent to the server where they will be validated)
        Player infor and stats are saved immediately to a locally saved player class (then sent to the server where they will be validated)     
            - The Ui is updated immediately: Health bar
            - The DR is updated immediately

    */

    // 2 --- SERVER UPDATE AND (later) INFORMATION VALIDATION
    /*
        The server calls are made with functions that validate the change of information 

    */


    void AccessTest2(ulong id, PlayerClass playerClass)
    {
        Debug.Log($"****************************************OnAccessTest2!");
        SaveOnServerRpc(id);
    }


    // This is called whenever a player connects to the game
    void AccessTest(ulong clientId, NetworkObjectReference playerObjRef, NetworkBehaviour playerBehaviorNetScript, NetworkObject netObj)
    {
        Debug.Log($"****************************************OnAccessTest!");

        // Create a player class instance here with the information passed through from the player behavior script event

        /* ERROR WHEN CREATING PLAYER CLASS EHRE
         * You are trying to create a MonoBehaviour using the 'new' keyword.  This is not allowed.  
         * MonoBehaviours can only be added using AddComponent(). 
         * Alternatively, your script can inherit from ScriptableObject or no base class at all
         */

        // playerClass = new PlayerClass(playerObjRef, playerBehaviorNetScript, netObj);


        // Save it to a dictionary on the server
        SaveOnServerRpc(clientId);

    }

    [Rpc(SendTo.Server)]
    void SaveOnServerRpc(ulong clientId)
    {
        playersInfo.Add(clientId, playerClass);

        //ForLoopIt();
    }

    private void ForLoopIt()
    {
        foreach (var player in playersInfo.Keys)
        {
            Debug.LogFormat($"<color=orange>{player}</color>");
        }
    }
}

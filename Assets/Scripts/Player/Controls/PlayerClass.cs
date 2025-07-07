// Access modifier (e.g., public, private, internal)
// Other modifiers (e.g., static, abstract, sealed)
using DotTimers;
using System;
using System.Collections.Generic;
using Unity.IO.Archive;
using Unity.Netcode;
using UnityEngine;

public class PlayerClass : MonoBehaviour 
{
    // // Dictionary<playerID, PlayerClassInstance>

    // private bool _inactive = false;
    // private bool _active = true;

    // private string playerName;

    // public NetworkVariable<int> m_SomeValue = new NetworkVariable<int>();

    // // This information is emitted once, once the player character spawns
    // //and is thereafter used to make changes to the network object as needed
    // //from the game manager
    // [Header("Player character information")] 
    // private ulong playerId;
    // private NetworkObjectReference netObjRef;
    // private NetworkBehaviour script;
    // private NetworkObject netObj;

    // // All the below variables will become network variables that are used by the
    // //game manager script to modify the players' in game information
    // // Being network variables, they can also be used to retrieve information 
    // //after they have been altered and modified
    // [Header("Base player character stats")]
    // private NetworkVariable<float> health = new NetworkVariable<float>();
    // // private float health;

    // [Header("Resistance factor")] // This could alternatively be booleans
    // private float fireResistance;
    // private float waterResistance;
    // private float airResistance;
    // private float earthResistance;
    // private float arcaneResistance;

    // [Header("DR Status")]
    // private bool aoeDRStatus = false;
    // private bool projectileDRStatus = false;
    // private bool invokeDRStatus = false;
    // private bool barrierDRStatus = false;
    // private bool beamDRStatus = false;
    // private bool sphereDRStatus = false;

    // [Header("Player in-game stats")]
    // private float damageTaken;
    // private float damageInflicted;
    // private float healTaken;
    // private float healGiven;
    // private float distanceTravelled;

    // // ?? private int gamesPlayed;


    // //private List<DamageOverTimeEffect> currentDamageOverTimeDebuffList = new List<DamageOverTimeEffect>();


    // #region Player Character Information Initializer Methods

    // public string PlayerName
    // {
    //     get { return playerName; }
    //     set { playerName = value; }
    // }

    // public ulong PlayerId
    // {
    //     get { return playerId; }
    //     set { playerId = value; }
    // }

    // public NetworkObjectReference NetObjRef
    // {
    //     get { return netObjRef; }
    //     set { netObjRef = value; }
    // }
    
    // public NetworkBehaviour Script
    // {
    //     get { return script; }
    //     set { script = value; }
    // }

    // public NetworkObject NetObj
    // {
    //     get { return netObj; }
    //     set { netObj = value; }
    // }

    // #endregion

    // public float Health
    // {
    //     get { return health.Value; }
    //     set { health.Value = value; }
    // }

    // #region resistances initializer methods

    // public float FireResistance
    // {
    //     get { return fireResistance; }
    //     set { fireResistance = value; }
    // }

    // public float WaterResistance
    // {
    //     get { return waterResistance; }
    //     set { waterResistance = value; }
    // }

    // public float AirResistance
    // {
    //     get { return airResistance; }
    //     set { airResistance = value; }
    // }

    // public float EarthResistance
    // {
    //     get { return earthResistance; }
    //     set { earthResistance = value; }
    // }

    // public float ArcaneResistance
    // {
    //     get { return arcaneResistance; }
    //     set { arcaneResistance = value; }
    // }

    // #endregion


    // #region Spell Category DR Status Initializer methods

    // public bool AoeDRStatus
    // {
    //     get { return aoeDRStatus; }
    //     private set { aoeDRStatus = value; }
    // }

    // public bool ProjectileDRStatus
    // {
    //     get { return projectileDRStatus; }
    //     private set { projectileDRStatus = value; }
    // }

    // public bool InvokeDRStatus
    // {
    //     get { return invokeDRStatus; }
    //     private set { invokeDRStatus = value; }
    // }

    // public bool BarrierDRStatus
    // {
    //     get { return barrierDRStatus; }
    //     private set { barrierDRStatus = value; }
    // }

    // public bool BeamDRStatus
    // {
    //     get { return beamDRStatus; }
    //     private set { beamDRStatus = value; }
    // }

    // public bool SphereDRStatus
    // {
    //     get { return sphereDRStatus; }
    //     private set { sphereDRStatus = value; }
    // }

    // #endregion


    // #region Player In-game Stats

    // public float DamageTaken
    // {
    //     get { return damageTaken; }
    //     set { damageTaken = value; }
    // }

    // public float DamageInflicted
    // {
    //     get { return damageInflicted; }
    //     set { damageInflicted = value; }
    // }

    // public float HealTaken
    // {
    //     get { return healTaken; }
    //     set { healTaken = value; }
    // }

    // public float HealGiven
    // {
    //     get { return healGiven; }
    //     set { healGiven = value; }
    // }

    // public float DistanceTravelled
    // {
    //     get { return distanceTravelled; }
    //     set { distanceTravelled = value; }
    // }

    // #endregion



    // // Constructors (methods called when an instance is created) // removed player health and playerID
    // public PlayerClass(NetworkObjectReference netObjRef, NetworkBehaviour script, NetworkObject netObj)
    // {
    //     // Set initial values based on constructor parameters
    //     NetObjRef = netObjRef;
    //     NetObj = netObj;
    //     Script = script;

    //     //PlayerName = name; // // This will need to be reactivated at some point when player names start existing
    //     //playerScore = score;
    //     //PlayerId = playerId;
    //     Health = 100f;
    // }



    // // Apply damage to the player taking into consideration the spell element resistance
    // public void ApplyDamage(float damage, string element)
    // {
    //     health.Value -= damage;
    //     Debug.LogFormat($"<color=orange>Player [pass client id here] health {health.Value}</color>");
    // }


    // void ApplyDotDamage()
    // {
    //     // Apply dot damage to the player here

    //     //DamageOverTimeEffect dot = new DamageOverTimeEffect(3, 5);

    //     //currentDamageOverTimeDebuffList.Add(dot);
    // }


    // // The first character of a spell sequence is sent here to ACTIVATE THE DR for the specific category
    // public void SetDRActive(char spellCategory)
    // {
    //     Debug.LogFormat($"<color=yellow>spellCategory {spellCategory} </color>");
    //     DRCategoriesActivityStates(spellCategory, _active);
    // }


    // private void Update()
    // {
    //     // Something like that will apply damage to the player over time


    //     // Apply damage over time for each effect
    //     //foreach (var effectName in currentDamageOverTimeDebuffList)
    //     //{
    //     //    //var effect = damageOverTimeEffects[effectName];
    //     //    effectName.ApplyDamageOverTime();

    //     //    // Check if the effect's duration has expired, and remove it if needed
    //     //    if (effectName.ElapsedTime >= effectName.Duration)
    //     //    {
    //     //        currentDamageOverTimeDebuffList.Remove(effectName);
    //     //        Debug.Log($"{effectName} effect removed.");
    //     //    }
    //     //}
    // }





    // // The first character of a spell sequence is sent here to DEACTIVATE THE DR for the specific category
    // public void SetDRInactive(char spellCategory)
    // {
    //     bool currentSpell;

    //     DRCategoriesActivityStates(spellCategory, _inactive);

    //     Debug.LogFormat($"<color=red> Spell category: {spellCategory} DR activity is: {GetSpellCategoryActivityStatus(spellCategory)} </color>");

    //     currentSpell = GetSpellCategoryActivityStatus(spellCategory);

    //     currentSpell = _inactive;

    //     Debug.LogFormat($"<color=red> Spell category: {spellCategory} DR activity is: {GetSpellCategoryActivityStatus(spellCategory)} </color>");

    // }





    // public bool GetSpellCategoryActivityStatus(char spellCategory)
    // {
    //     switch (spellCategory)
    //     {
    //         case 'V':
    //             Debug.LogFormat($"<color=yellow>spellCategory {ProjectileDRStatus} </color>");
    //             return ProjectileDRStatus; 
    //         case 'F':
            
    //             return AoeDRStatus;
    //         case 'B':
            
    //             return BeamDRStatus;
    //         case 'Y':
            
    //             return BarrierDRStatus;
    //         case 'T':
            
    //             return SphereDRStatus;
    //         case 'N':
            
    //             return InvokeDRStatus;
                
    //         default: return false;
    //     }
    // }





    // // Both activation and deactivation of DR for a category are routed through this
    // private void DRCategoriesActivityStates(char spellCategory, bool activeStatus)
    // {
    //     switch (spellCategory)
    //     {
    //         case 'V':
    //             ProjectileDRStatus = activeStatus;
    //             Debug.LogFormat($"<color=yellow>spellCategory {ProjectileDRStatus} </color>");

    //             return;
    //         case 'F':
    //             AoeDRStatus = activeStatus;
    //                     Debug.LogFormat($"<color=yellow>spellCategory {spellCategory} </color>");

    //             return;
    //         case 'B':
    //             BeamDRStatus = activeStatus;
    //                     Debug.LogFormat($"<color=yellow>spellCategory {spellCategory} </color>");

    //             return;
    //         case 'Y':
    //             BarrierDRStatus = activeStatus;
    //                     Debug.LogFormat($"<color=yellow>spellCategory {spellCategory} </color>");

    //             return;
    //         case 'T':
    //             SphereDRStatus = activeStatus;
    //                     Debug.LogFormat($"<color=yellow>spellCategory {spellCategory} </color>");

    //             return;
    //         case 'N':
    //             InvokeDRStatus = activeStatus;
    //                     Debug.LogFormat($"<color=yellow>spellCategory {spellCategory} </color>");

    //             return;
    //         default:
    //             return;
    //     }
    // }





    // // Methods (functions or member functions)
    // public void DisplayPlayerInfo()
    // {
    //     Console.WriteLine($"Player Name: {PlayerName}, Score:");
    // }
}
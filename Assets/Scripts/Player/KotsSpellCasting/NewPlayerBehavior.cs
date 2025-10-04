using DamageOverTimeEffect;
using DebuffEffect;
using IncapacitationEffect;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class NewPlayerBehavior : NetworkBehaviour
{
    // Future implementation: Any player or player character specific information that is linked to the player steam id (or similar) is to also be sent through this event
    public delegate void PlayerHasSpawnedEventPayload(ulong clientId, NetworkObjectReference playerObjRef, NetworkBehaviour playerBehaviorNetScript, NetworkObject netObj);
    public static event PlayerHasSpawnedEventPayload playerHasSpawnedEvent;

    public delegate void PlayerHasSpawnedEventPayload2(ulong clientId, PlayerClass playerClassInstance);
    public static event PlayerHasSpawnedEventPayload2 playerHasSpawnedEvent2;

    public delegate void ShaderActivation(ulong clientId, string shader, float seconds);
    public static event ShaderActivation shaderActivation;

    public delegate void OnDeathScoreUpdate(ulong clientId, int deaths);
    public static event OnDeathScoreUpdate onDeathScoreUpdate;

    [SerializeField] GameObject scoreboardGO;


    private List<DamageOverTime> currentDamageOverTimeList = new List<DamageOverTime>();

    private Dictionary<int, DamageOverTime> activePersistentDamageOverTimeSpells = new Dictionary<int, DamageOverTime>();
    private List<int> toRemovePersistentDamageOverTimeEntry = new List<int>();

    private List<int> spellsTriggeredList = new List<int>();

    bool isRemovingPersistentDotEntry;
    int persistentSpellsCount = 0;

    private List<Incapacitation> currentDebuffList = new List<Incapacitation>();

    public NetworkVariable<bool> localSphereShieldActive = new NetworkVariable<bool>(default,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);    
    
    public NetworkVariable<bool> isSlowed = new NetworkVariable<bool>(false,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<bool> isStunned = new NetworkVariable<bool>(false,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<bool> isSilenced = new NetworkVariable<bool>(false,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    public NetworkVariable<bool> isImmobilized = new NetworkVariable<bool>(false,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<int> deathCount = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private List<int> spellsIds = new List<int>();
    private List<ulong> projectileIds = new List<ulong>();

    [Header("Scripts")]
    PlayerClass playerClass; // To continue implementation? This is to track the player stats on the server. To revise later.
    HealthBarUi _healthBar;
    Scoreboard scoreboardScript;
    PlayerController _playerController;
    K_SpellLauncher _spellLauncherScript;
    DoTHandler _doTHanndler;

    [SerializeField]
    GameObject healthBarGO;
    GameObject shaderGO;
    [SerializeField]
    GameObject parryLetterGO;

    //bool isShieldActive = false;

    bool isIncapacitated = false; // This is used to lock player movement and spellcasting if the player is stunned

    public bool IsIncapacitated
    {
        get { return isIncapacitated; }
        private set { isIncapacitated = value; }
    }

    public PlayerClass LocalPlayerClass
    {
        get { return playerClass; }
        set { playerClass = value; }
    }

    public HealthBarUi HealthBar
    {
        get { return _healthBar; }
        set { _healthBar = value; }
    }

    [SerializeField] GameObject wandTip2;





    // This correctly spawns and respawns the player at the spawn point's location
    void SpawnPlayerAtStartingLocation()
    {
        Vector3 spawnPosition = SpawnManager.Instance.AssignSpawnPoint(OwnerClientId);
        Quaternion spawnRotation = SpawnManager.Instance.AssignSpawnRotation(OwnerClientId);

        gameObject.GetComponent<Rigidbody>().MovePosition(spawnPosition);
        gameObject.GetComponent<Rigidbody>().MoveRotation(spawnRotation);     

        SpawnPlayerAtStartingLocationRpc(spawnPosition, spawnRotation);
    }

    


    // This translates the player's position on the server
    [Rpc(SendTo.Server)]
    void SpawnPlayerAtStartingLocationRpc(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        gameObject.GetComponent<Rigidbody>().MovePosition(spawnPosition);
        gameObject.GetComponent<Rigidbody>().MoveRotation(spawnRotation);

    }




    // ! Start is run after OnNetworkSpawn
    void Start()
    {
        if (!IsOwner || !IsLocalPlayer) return;
        // Debug.LogFormat($"<color=brown> Player Access {OwnerClientId} </color>");

        

        // playerClass = new PlayerClass(this.NetworkObject, this.GetNetworkBehaviour(this.NetworkBehaviourId), this.gameObject.GetComponent<NetworkObject>());

        // Get the health bar instance reference that was assigned to this player
        NewHealthBarSingleton.Instance.GetPlayer(gameObject.transform);
        _healthBar = NewHealthBarSingleton.Instance.GetComponent<NewHealthBarSingleton>().GetComponentInChildren<HealthBarUi>();

        // For external access - DoTHandler, .
        HealthBar = _healthBar;

        // Debug.Log("NetworkObject found12321321321321321312312: " + NewHealthBarSingleton.Instance.GetComponent<NewHealthBarSingleton>());


        // Emit an event of the player info to be ingested by the GameManager
        if (playerHasSpawnedEvent != null) playerHasSpawnedEvent(OwnerClientId, this.NetworkObject, this.GetNetworkBehaviour(this.NetworkBehaviourId), this.gameObject.GetComponent<NetworkObject>());

        _spellLauncherScript = gameObject.GetComponent<K_SpellLauncher>();

    }





    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        parryLetterGO.SetActive(false); // Note: If this were to be placed in the Start(), the object would be deactivated locally but persists over the network

        // Subscribe to the changes made to the player health
        SpellsClass.playerHitEvent += DamageHandler; // Event subscription to event emitted by spell upon interaction with player
        
        // To replace
        K_Spell.playerHitEvent += DamageHandler; // Event subscription to event emitted by spell upon interaction with player

        K_SphereSpell.shieldExists += ShieldAliveStatus;

        Incapacitation.playerIncapacitation += HandleIncapacitation;

        BeamSpell.beamStatus += HandleBeam;

        SpawnPlayerAtStartingLocation();

        _playerController = GetComponent<PlayerController>();

        // if (!IsOwner) return;
        //isSlowed.Value = false;

        //SpawnWandRpc();
    }




    public void AssignPlayerCenter(NetworkObject netObjParam)
    {
        UnityEngine.Debug.Log("SETTING SPAWN CENTER 222222");
        _spellLauncherScript.SetSpawnCenter(netObjParam);
    }



    public void DoSomething()
    {
        UnityEngine.Debug.LogFormat($"<color=blue> 2 NEW DAMAGE APPLICATION / OWNER: {OwnerClientId} </color>");

    }





    [Rpc(SendTo.Server)]
    void SpawnWandRpc()
    {
        GameObject wandInstance = Instantiate(wandTip2);
        NetworkObject netObj = wandInstance.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(OwnerClientId);
        netObj.TrySetParent(gameObject.transform);
    }





    void HandleBeam(ulong clientId, NetworkObjectReference spellObj, NetworkBehaviour spellNetBehaviorScript, bool status0)
    {
        if (spellObj.TryGet(out NetworkObject networkObject))
        {
            // Successfully got the NetworkObject from the reference
            UnityEngine.Debug.Log("NetworkObject found: " + networkObject);

            //networkObject.gameObject.transform.SetParent(gameObject.transform.GetChild(2).gameObject.transform);
            networkObject.TrySetParent(gameObject.transform.GetChild(2).gameObject.transform); // >>>>>>>>>>>>>>>>>>>>>>>  DANGER --- TO REVISE <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            UnityEngine.Debug.Log("If the beam is not spawning correctly check this ^");


            //spellNetBehaviorScript
        }
        else
        {
            // Handle the case where the NetworkObject could not be found
            UnityEngine.Debug.LogError("Failed to get the NetworkObject from the reference.");
        }
    }





    private void Update()
    {
        if (!IsOwner || !IsLocalPlayer) return;


        // If the player health has reached 0:
        // Respawn at the starting location
        // Reset health back to max
        if (_healthBar.HealthSlider.value <= 0)
        {
            // Send out an event here to update the scoreboard for all players
            // This event should be ingested by the scoreboard script handling the score for either player
            if (onDeathScoreUpdate != null) onDeathScoreUpdate(OwnerClientId, deathCount.Value);


            // This counts how many times the player has died
            // The scoreboard uses this value to attribute a score point to the opposing player
            deathCount.Value += 1;

            _healthBar.SetMaxHealth(500);
            SpawnPlayerAtStartingLocation(); // make this local <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            // make this an event to reset the health of all players
            // ++ This implicates the introduction of a Game Manager that is continuously keeping
            //track of the player stats, health accuracy and so on.

            // RemoveAny pertsisting dot
            // Remove any debuff effects
        }

        // Have the timer method here with the dictionary being iterated over handled by the class
        // Check if the player is inflicted with a DoT effect - Check if a DoT effect has been added to the DoT effects list
        //if a list entry exists then the player would take DoT damage
        if (currentDamageOverTimeList.Count > 0)
        {
            // Iterate through the DoT spells the player character is currently affected by
            for (int i = currentDamageOverTimeList.Count - 1; i >= 0; i--)
            {
                // Get the instance of each of the DoT effect
                var dot = currentDamageOverTimeList[i];

                // Check if the spell duration has expired
                if (dot.TimeExpired)
                {
                    UnityEngine.Debug.LogFormat($"<color=purple>DOT EXPIRED</color>");

                    // If spell duration has expired, remove the DoT effect instance
                    currentDamageOverTimeList.RemoveAt(i);
                    return;
                }
                else
                {
                    // If the spell duration has not yet expired (above)
                    // The method returns 'true' at a specified (per second) time interval and applies damage
                    if (dot.Timer() && !localSphereShieldActive.Value)
                    {
                        UnityEngine.Debug.LogFormat($"<color=purple>DOT APPLY DAMAGE</color>");

                        // Apply damage to the player
                        _healthBar.ApplyDamage(dot.DamagePerSecond);

                        // Activating the blood shader for AoE doesn't work the same way when it is to be fired in succession
                        //if (shaderActivation != null) shaderActivation(OwnerClientId, "Blood", 1);
                        // DebuffController.DebuffController cont = new DebuffController.DebuffController(_healthBar.ApplyDamage(dot.DamagePerSecond));
                    }
                }
            }
        } else if (currentDebuffList.Count > 0) // This is the TIMER ONLY for incapacitation effects / stuns, movement slow etc.. to double check // TO MOVE THIS TO ITS OWN SCRIPT
        { // !!! IMPORTANT !!! An event is used to handle the reduction and reset of the movement speed (PlayerController script)
            //Debug.LogFormat($"<color=brown> currentDebuffList.CountcurrentDebuffList.CountcurrentDebuffList.Count {currentDebuffList.Count}</color>");

            // TO DO: This should be assigned to true only IF it is a slow

            // TO DO: CHECK THE INCAPACITATION TYPE AND UPDATE THE RESPECTIVE NV (??)

            // Iterate through the DoT spells the player character is currently affected by
            for (int i = currentDebuffList.Count - 1; i >= 0; i--)
            {
                if (currentDebuffList[i].isActivated == false)
                {
                    currentDebuffList[i].ActivateIncapacitation(); // Activates the incapacitation effect
                    currentDebuffList[i].isActivated = true;
                }
                // Get the instance of each of the incapacitation effects
                var debuff = currentDebuffList[i];

                // Check if the spell duration has expired
                if (debuff.TimeExpired)
                {
                    // If spell duration has expired, remove the DoT effect instance
                    currentDebuffList[i].DeactivateIncapacitation(); // Deactivates the incapacitation effect
                    currentDebuffList.RemoveAt(i); // Removes it for the list being iterated over
                    //isSlowed.Value = false;
                    return;
                }
                else //I dont think the code here below is needed. To revise
                {
                    // Given that incapacitations do not directly apply damage
                    // This below is not needed - To confirm
                    if (debuff.Timer())
                    {
                        //currentDebuffList[i].DeactivateIncapacitation();

                        //isSlowed.Value = false;
                        // Apply damage to the player
                        return;
                        // DebuffController.DebuffController cont = new DebuffController.DebuffController(_healthBar.ApplyDamage(dot.DamagePerSecond));
                    }
                }
            }
        }



        // There is a delay upon exiting a fire AoE until the DoT tick damage begins to apply
        // Following my investigation, this is likely due to the fact that foreach is based upon an IEnumerator
        //and the delay follows the necessity to create an instance of it. (to investigate)
        if (activePersistentDamageOverTimeSpells.Count > 0 && isRemovingPersistentDotEntry == false)
        {
            foreach (KeyValuePair<int, DamageOverTime> kvp in activePersistentDamageOverTimeSpells)
            {
                if (toRemovePersistentDamageOverTimeEntry.Count > 0)
                {
                    //Debug.LogFormat($"<color=orange> toRemovePersistentDamageOverTimeEntry </color>");
                    isRemovingPersistentDotEntry = true;
                    RemovePersistentDotEntry();
                    return;
                }

                //Debug.Log("Key: " + kvp.Key + ", Value: " + kvp.Value);

                // If the spell duration has not yet expired (above)
                // The method returns true at a specified (per second) time interval and applies damage
                if (kvp.Value.Timer() && !localSphereShieldActive.Value)
                {
                    // Apply damage to the player
                    _healthBar.ApplyDamage(kvp.Value.DamagePerSecond);
                    // DebuffController.DebuffController cont = new DebuffController.DebuffController(_healthBar.ApplyDamage(dot.DamagePerSecond));
                } 

            }
        }

    }





    // This was initially to revise how damage application is handled 
    // with damage over time spells. To explore again later
    public void ApplyDamageToPlayer(float dmg)
    {
        _healthBar.ApplyDamage(dmg);
    }





    void HandleIncapacitation(ulong clientId, IncapacitationInfo incapacitation) // IncapacitationInfo is part of the spell payload emitted upon spell>player interaction
    {
        // Checks if the script instance belongs to the player the spell has interacted with
        if (clientId != OwnerClientId) return;

        UnityEngine.Debug.LogFormat($"<color=brown> incapacitationincapacitation {incapacitation.SlowsMovement}</color>");

        // Blocks spellcasting if the spell contains a true value for spell casting incapacitation
        //isSilenced.Value = incapacitation.AffectsSpellCasting;
        isSlowed.Value = incapacitation.SlowsMovement;
        //if (incapacitation.SlowsMovement && incapacitation.StopsMovement) { isStunned.Value = true; }
        //isImmobilized.Value = incapacitation.StopsMovement;
    }





    void RemovePersistentDotEntry()
    {
        //Debug.LogFormat($"<color=orange> RemovePersistentDotEntry </color>");

        for (int i = toRemovePersistentDamageOverTimeEntry.Count - 1; i >= 0; i--)
        {
            //Debug.LogFormat($"<color=orange> REMOVE ENTRY:: {toRemovePersistentDamageOverTimeEntry[i]} </color>");
            activePersistentDamageOverTimeSpells.Remove(toRemovePersistentDamageOverTimeEntry[i]);
            toRemovePersistentDamageOverTimeEntry.RemoveAt(i);
        }

        isRemovingPersistentDotEntry = false;
    }





    private void UpdateScore(int current, int previous)
    {
        UnityEngine.Debug.LogFormat($"<color=brown> WHAT IS BEING CALLED {OwnerClientId}</color>");
        if (IsServer)
        {
            return;
        }
        //scoreboardScript.UpdateScore(OwnerClientId);

        //_playerController.HandleMovement();
    }




    void ShieldAliveStatus(ulong p_clientId, NetworkObjectReference spellObj, NetworkBehaviour spellNetBehavior, bool isShieldAvailable)
    {
        if (p_clientId != OwnerClientId) return;

        UnityEngine.Debug.LogFormat($"<color=brown>ShieldAliveStatus {localSphereShieldActive.Value}  Owner ccc : {OwnerClientId} </color>");

        // Evaluates to true when a shield is spawned and is active on the player character
        //and is thereafter used to check whether or not to deal damage to the player
        // !! This method is further elaborated so that damage to the player is not made at the same time a projectile destroys it (the problem this code is solving)
        //ShieldIsActiveServerRpc();
        //isShieldActive = isShieldAvailable;
        localSphereShieldActive.Value = isShieldAvailable;

        UnityEngine.Debug.LogFormat($"<color=brown>ShieldAliveStatus {localSphereShieldActive.Value}  Owner ccc : {OwnerClientId} </color>");
 
    }



    void DamageHandler(PlayerHitPayload emittedPlayerHitPayload) 
    {

        UnityEngine.Debug.LogFormat($"<color=brown>XXX DAMAGE HANDLER 1 XXX: {emittedPlayerHitPayload.PlayerId} HAS HIT PLAYER: {OwnerClientId}  </color>");
        // if (!IsOwner)
        if (emittedPlayerHitPayload.PlayerId != GetComponent<NetworkObject>().OwnerClientId) return;

        //Debug.LogFormat($"<color=brown>XXX DAMAGE HANDLER 2 XXX: {emittedPlayerHitPayload.PlayerId} HAS HIT PLAYER: {OwnerClientId} </color>");

        // Make sure the the event is being processed by the respective script of the player that was hit
        // xx Lets say the projectile is owned by player 2 and emits that it hit player 1, if this script is indeed owned by player 1 then run the code otherwise skip it

        // Received from an event emitted by the spawed sphere
        // If the player has a shield on, don-t apply damage to the player
        // Damage applied to the shield is handled elsewhere

        //if (localSphereShieldActive.Value) return; <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

        // USE THIS TO ACTIVATE SHADERS ALTERIOR FROM BLOOD
        //if (shaderActivation != null) shaderActivation(OwnerClientId, emittedPlayerHitPayload.VisionImpairment.ToString(), emittedPlayerHitPayload.VisionImpairmentDuration);

        // If the projectile spell has not already hit the player, add it to the list for future reference
        spellsIds.Add(emittedPlayerHitPayload.NetworkId); // This was not useful so far - Queued for deletion

        //Debug.LogFormat($"<color=brown>XXX DAMAGE HANDLER 4 XXX: {emittedPlayerHitPayload.PlayerId} HAS HIT PLAYER: {OwnerClientId} </color>");
        

        /// Applies slow to player character
        // Check first if the player is already slowed to not stack slow
        // Check if the spell also has slow effect to be applied
        if (isSlowed.Value == false && emittedPlayerHitPayload.IncapacitationName.ToString() != "None")
        {
            // This adds an incapacitation to a dictionary that will incapacitate the player for the passed amount of time
            // This is controlled with a timer above in the script
            currentDebuffList.Add(new Incapacitation(emittedPlayerHitPayload.IncapacitationName, emittedPlayerHitPayload.IncapacitationDuration, emittedPlayerHitPayload.PlayerId));
        }


        //if (emittedPlayerHitPayload.Pushback == true)
        //{
        //    Debug.LogFormat($"<color=brown> PLAYER HIT PAYLOAADDD: {emittedPlayerHitPayload.Pushback} </color>");
        //}


        switch (emittedPlayerHitPayload.SpellAttribute.ToString())
        {
            case "DirectDamage":
                // Add calculations here - If shphere shield becomes a passive restistance addition
                DirectDamage(emittedPlayerHitPayload.DirectDamageAmount);
                return;

            // DoT while player is in contact with the spell
            case "DamageOverTime":
                // SingleApplicationDoT
                DamageOverTimeHandler(emittedPlayerHitPayload.NetworkId, emittedPlayerHitPayload.SpellElement, emittedPlayerHitPayload.DamageOverTimeAmount, emittedPlayerHitPayload.DamageOverTimeDuration);
                return;

            // DoT while the player is no longer (has exited) contact with the spell
            case "PersistentDamageOverTime":
                UnityEngine.Debug.LogFormat($"<color=orange> PP ************* PERSISTENT {emittedPlayerHitPayload.NetworkId} </color>");

                // Deal an initial damage amount to player upon spell entry only
                // Note: This is accessed also on player exit and therefore the below logic handles
                if (spellsTriggeredList.Contains(emittedPlayerHitPayload.NetworkId))
                {
                    UnityEngine.Debug.LogFormat($"<color=orange> > ************* REMOVE </color>");

                    spellsTriggeredList.Remove(emittedPlayerHitPayload.NetworkId);
                } else if (!spellsTriggeredList.Contains(emittedPlayerHitPayload.NetworkId))
                {
                    UnityEngine.Debug.LogFormat($"<color=orange> > ************* ADD </color>");

                    if (localSphereShieldActive.Value == false)
                    {
                        // Deal an initial amount of damage on spell entry
                        DirectDamage(emittedPlayerHitPayload.DamageOverTimeAmount);
                    }

                    spellsTriggeredList.Add(emittedPlayerHitPayload.NetworkId);

                }


                // Add the gameObject to a persistenSpellList and make sure the damage does not stack

                // Add calculations here - If shphere shield becomes a passive restistance addition
                PersistentDamageOverTimeHandler(emittedPlayerHitPayload.NetworkId, emittedPlayerHitPayload.SpellElement, emittedPlayerHitPayload.DamageOverTimeAmount, emittedPlayerHitPayload.DamageOverTimeDuration);
                return;

            case "HybridDamage":
                HybridDamage(emittedPlayerHitPayload.NetworkId, emittedPlayerHitPayload.SpellElement, emittedPlayerHitPayload.DirectDamageAmount, emittedPlayerHitPayload.DamageOverTimeAmount, emittedPlayerHitPayload.DamageOverTimeDuration);
                return;

            case "Heal":
                Heal(emittedPlayerHitPayload.HealAmount);
                return;

            default: break;
        }

        // ACTIVATE RESPECTIVE LOGIC
        // CHANGE HEALTH NV > EMIT EVENT OF RESULTS TO GM > SERVER
        // Debug.LogFormat($"<color=orange>NPB Damage Handler:\n Payload: \n Player ID: {emittedPlayerHitPayload.PlayerId} \n Damage Type: {emittedPlayerHitPayload.DamageType} \n Direct Damage Amount: {emittedPlayerHitPayload.DirectDamageAmount} \n DebugLog Source Player: {OwnerClientId} </color>");

    }




    public void Heal(float healAmount)
    {
        UnityEngine.Debug.LogFormat($"<color=orange> > 1 HEALING method - amount: {healAmount} </color>");

        if (!IsOwner) return;
        // This is used to heal the player
        _healthBar.Heal(healAmount);

        UnityEngine.Debug.LogFormat($"<color=orange> > 2 HEALING method - amount: {healAmount} </color>");

    }




    public void DirectDamage(float directDamageAmount)
    {
        UnityEngine.Debug.LogFormat($"<color=orange> >1Direct Damage method - Damage amount: {directDamageAmount} </color>");

        if (!IsOwner) return;    

        if (shaderActivation != null) shaderActivation(OwnerClientId, "Blood", 1);

        _healthBar.ApplyDamage(directDamageAmount);

        // edit health on local class
        // send event for game manager
        UnityEngine.Debug.LogFormat($"<color=orange> >2Direct Damage method - Damage amount: {directDamageAmount} </color>");
    }





    void DamageOverTimeHandler(int networkId, string element, float damageOverTimeAmount, float damageOverTimeDuration)
    {
        // Check the spell and spell type
        // Check the stack limit for each (if any)
        // Add a new entry to the dot list // that entry's elimination will be handled through the dot class itself OR through a dispell spell
        // use the DamageOverTime class to add a new instance of a spell that is dealing damage to the player

        // If the type of spell is aoe, repeat damage application until an exit event has been returned here

        // TD: Make this into a dictionary? bool persistant? > Persists until the player removes the dot spell off of himself


        /// NEW - THIS PREVENTS STACKING OF DOT SPELL
        if (currentDamageOverTimeList.Any(dot => dot.NetworkId == networkId)) return;

        // Work in progress
        //_doTHanndler.ApplyOnCollisionConstantDoTOnPlayer(networkId, element, damageOverTimeAmount);

        UnityEngine.Debug.LogFormat($"<color=purple>DOT ADD</color>");

        // Add damage on hit
        if (!localSphereShieldActive.Value)
            _healthBar.ApplyDamage(damageOverTimeAmount);

        currentDamageOverTimeList.Add(new DamageOverTime(networkId, element, damageOverTimeAmount, damageOverTimeDuration));

        UnityEngine.Debug.LogFormat($"<color=orange> >Damage Over Time Method - Damage amount: {damageOverTimeAmount} </color>");
    }





    void PersistentDamageOverTimeHandler(int networkId, string element, float damageOverTimeAmount, float damageOverTimeDuration)
    {
        //Debug.LogFormat($"<color=orange> PersistentDamageOverTimeHandler </color>");

        ///<summary>If the player already has a DoT applied on him from the same spell
        ///reset the timer by removing the DoT entry and adding it again</summary>
        // If the dictionary that deals dot to the player already contains an entry of the nework object
        // i.e: the object is already dealing damage to the player. Remove it, as the second time it is received is from the trigger exit
        if (activePersistentDamageOverTimeSpells.ContainsKey(networkId))
        {

            // TD: make it so that the residual dot entries are removed upon entry
            currentDamageOverTimeList.Add(new DamageOverTime(networkId, element, damageOverTimeAmount, damageOverTimeDuration));

            toRemovePersistentDamageOverTimeEntry.Add(networkId);
            return;
        }

        if (currentDamageOverTimeList.Count > 0)
        {
            foreach (var netObj in currentDamageOverTimeList)
            {
                if (netObj.NetworkId != networkId)
                {
                    // This will continuously deal damage to the player until a trigger exit event stops it (here above)
                    activePersistentDamageOverTimeSpells.Add(networkId, new DamageOverTime(networkId, element, damageOverTimeAmount, damageOverTimeDuration));

                    //exists = true;
                    break;
                }
                else
                {
                    UnityEngine.Debug.LogFormat($"<color=purple>DOT REMOVE</color>");

                    // If the player has exited the DoT spell trigger, stop dealing the DoT caused by direct contact with the DoT Spell >> the else code below follows, adding a timed DoT on the player
                    currentDamageOverTimeList.Remove(netObj);
                    activePersistentDamageOverTimeSpells.Add(networkId, new DamageOverTime(networkId, element, damageOverTimeAmount, damageOverTimeDuration));
                }
            }
        } else // This adds a DoT on the player when he exits a DoT spell's trigger
        {
            if (!localSphereShieldActive.Value)
            activePersistentDamageOverTimeSpells.Add(networkId, new DamageOverTime(networkId, element, damageOverTimeAmount, damageOverTimeDuration));
        }

    }




    void HybridDamage(int networkId, string spellElement, float directDamageAmount, float damageOverTimeAmount, float damageOverTimeDuration)
    {
        DirectDamage(directDamageAmount);
        DamageOverTimeHandler(networkId,spellElement, damageOverTimeAmount, damageOverTimeDuration);
    }





    private void OnTriggerEnter(Collider other)
    {
        // Debug.LogFormat($"<color=orange> this gameObject: {gameObject} other: {other.gameObject.name} </color>");

        if (other.gameObject.name.Contains("Targeting"))
        {
            // Activate parry letters above this player character
            //_spellLauncherScript.parryLetters;
            parryLetterGO.SetActive(true);
            // needs to be a server rpc method to send across the network
        }
    }



    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name.Contains("Targeting"))
        {
            // Activate parry letters above this player character
            //_spellLauncherScript.parryLetters;
            parryLetterGO.SetActive(false);
            // needs to be a server rpc method to send across the network
        }
    }
}

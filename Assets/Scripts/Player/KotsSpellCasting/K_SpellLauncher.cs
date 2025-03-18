using IncapacitationEffect;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using static PlayerSpellController;
using static System.Runtime.CompilerServices.RuntimeHelpers;

public class K_SpellLauncher : NetworkBehaviour
{
    [Header("UI Elements")]
    [Tooltip("The text field which shows spell related inputs.")]
    [SerializeField] private TMP_Text spellText;
    [Tooltip("The cast key at the center of the DR keys. Should have castKey = true.")]
    [SerializeField] private K_DRKey castKey;

    [UDictionary.Split(20, 80)] public DrUiKeys drUiKeysDictionary;
    [Serializable] public class DrUiKeys : UDictionary<string, K_DRKey> { }

    [Header("DR Settings")]
    [Tooltip("The maximum amount of keys to be activated at the start of a DR lock.")]
    [SerializeField, Min(1)] private int maxStartingDrKeys;
    [Tooltip("The number of DR keys to activate after solving a key.")]
    [SerializeField, Min(1)] private int drKeysPerSolve;

    private bool canCast = true;
    private bool inSpellCastModeOrWaitingSpellCategory;
    private bool isInDRLockMode;
    private bool castingIncapacitated;

    private Queue<K_DRKeyData> drLockKeysQueue;

    private string spellSequence = "";

    public string SpellSequence
    {
        get { return spellSequence; }
    }

    private KeyCode[] allSpellKeys;

    private KeyCode currentSpellType;
    private KeyCode lastSpellType = KeyCode.None;
    private bool ignoreDrLock;

    public delegate void OnBufferFailedEventHandler();

    //K_Spell spell;

    //[SerializeField] GameObject projectile_test;

    [SerializeField] K_SpellBuilder spellBuilder;

    [Header("Spells Spawn Points' gameObjects")]
    [SerializeField] GameObject wandTipGO;
    WandTip wandTipScript;
    Transform wandTip;

    [SerializeField] GameObject playerCenterGO;
    Transform playerCenter;

    [SerializeField] GameObject barrierPointGO;
    Transform barrierPoint;

    [SerializeField] GameObject invocationBoundsGO;
    InvocationSpawner invocationSpawnerScript;
    Transform invocationBounds;

    [SerializeField] GameObject theProjectile1;

   // public Dictionary<string, K_SpellData> spellDictionary = new Dictionary<string, K_SpellData>();
    public Dictionary<string, GameObject> prefabReferences = new Dictionary<string, GameObject>();
    
    // This will be injected with the DR status for each spell category
    public Dictionary<string, bool> ignoreSpellDRLock = new Dictionary<string, bool>();

    NewPlayerBehavior newPlayerBehaviorScript;

    public NetworkVariable<FixedString32Bytes> parryLetters = new NetworkVariable<FixedString32Bytes>(default,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);


    // 2 keycode properties that hold the values for spell type and elemental transmutation
    // Pressing G casts the last saved combination
    // 



    // SpellCastController 



    // SpellCastManager 



    // SpallUnlockManager




    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        //// PlayerBehavior itself????
        // subscribe to the event that handles incapacitating the player movement or casting when hit by an incapacitating effect spell
        Incapacitation.playerIncapacitation += HandleIncapacitation;

        K_DRKey.onPlayerFailedToSyncInputToBuffer += HandleBufferFailed;

        newPlayerBehaviorScript = this.GetComponent<NewPlayerBehavior>();

        //// START SpellCastController

        // Populate the spellKeys array with the values of spellTypesKeys and spellElements
        allSpellKeys = new KeyCode[K_SpellKeys.spellTypes.Length + K_SpellKeys.spellElements.Length + 1];
        K_SpellKeys.spellTypes.CopyTo(allSpellKeys, 0);
        K_SpellKeys.spellElements.CopyTo(allSpellKeys, K_SpellKeys.spellTypes.Length);
        //allSpellKeys[allSpellKeys.Count() - 1] = K_SpellKeys.cast;

        //// END SpellCastController

        //// START SpellCastManager 

        // Stop animation & Reset casting variables + Cancel AoE raycast if active
        castKey.OnBufferFailed += HandleBufferFailed;
        // location from which some spells spawn (from the tip of the wand)
        wandTipScript = wandTipGO.GetComponent<WandTip>();
        wandTip = wandTipGO.GetComponent<Transform>();
        // handles spaming
        invocationSpawnerScript = this.GetComponent<InvocationSpawner>();

        playerCenter = playerCenterGO.GetComponent<Transform>();
        barrierPoint = barrierPointGO.GetComponent<Transform>();
        invocationBounds = invocationBoundsGO.GetComponent<Transform>();
        invocationBoundsGO.SetActive(false);

        // The SpellDict cannot be saved to a Dictionary reference directly
        // This saves a copy of the spell key to prefab, and is used later to spawn the spell object
        foreach (var spellData in this.GetComponent<K_SpellBuilder>().spellDictionary)
        {
            prefabReferences.Add(spellData.Key.ToString(), spellData.Value.prefab);
        }

        // Checking whenever we are casting spell and shoud ignore 
        foreach (KeyCode key in K_SpellKeys.spellTypes)
        {
            ignoreSpellDRLock.Add(key.ToString(), false);
        }

        //// END SpellCastManager 

        //// START SpallUnlockManager

        // when try solve DR and fails is called
        foreach (K_DRKey drKey in drUiKeysDictionary.Values)
            drKey.OnBufferFailed += OnDrBufferFailed;

        //// END SpallUnlockManager

       
        Debug.LogFormat($"<color=red> IsLocalPlayer {IsLocalPlayer} OwnerClientId {OwnerClientId} </color>");
    }

    private void Update()
    {
        if (!IsLocalPlayer) return;

        if (castingIncapacitated == true) return;

        // if (_player) return; 

        // (not sure) This handles if the player can cast or is required
        // to enter a DR sequence for a specific spell category
        // this might be incorrect
        if (!canCast)
            return;


        // if CONDITIONAL ISSTUNNED: HERE

        // if (IsDrSolved() == false)
        if (isInDRLockMode)
        {
            // Debug.LogFormat($"<color=red> UPDATE !!!! IS DR LOCK !!!! UPDATE </color>");
            HandleDRLockInput();
            return;
        }

        ///<summary>
        ///This is the spell casting entry point where spell casting is handled
        ///following the player's G letter input.
        ///</summary>
        ///
        ///<defaultValue> false </defaultValue>
        ///
        if (inSpellCastModeOrWaitingSpellCategory) // This is when the player already is in cast mode
        {
            // SPELL FIRING
            if (Input.GetKeyUp(K_SpellKeys.cast))
            {
                // This fires the spell, and changes the bool value of the 'inSpellCastModeOrWaitingSpellCategory' parameter to false
                HandleSpellFiring();
            }
          
            // SPELL BUILDING // Handles for spell category inputs + exceptions
            HandleSpellCasting();
           
        }
        else // >>>>>>>>> This places the player in SPELLCAST MODE
        {
            // After casting a spell this should not immediately reenable the player's ability to enter cast mode
            if (Input.GetKeyUp(K_SpellKeys.cast))
            {
                inSpellCastModeOrWaitingSpellCategory = true;

                // This writes the sequence input on the top right corner of the screen
                // note: G is not saved here to the spell sequence array
                spellText.text = K_SpellKeys.cast.ToString();
            }
        }

    }

    // When a spell object interacts with a player, an event is emitted and then ingested in the script (global) 
    //where this function is called to ONLY handle spell casting incapacitation
    // If it is the case, the player is hereafter prevented from casting spells
    void HandleIncapacitation(ulong clientId, IncapacitationInfo incapacitation) // IncapacitationInfo is part of the spell payload emitted upon spell>player interaction
    {
        // Checks if the script instance belongs to the player the spell has interacted with
        if (clientId != OwnerClientId) return;

        // Blocks spellcasting if the spell contains a true value for spell casting incapacitation
        castingIncapacitated = incapacitation.AffectsSpellCasting;
    }

    //// START SpellCastController
    // Whenever the Cast Key (G) buffer expires before player input an event is emitted 
    //and ingested here to exit the player from Cast Mode
    void HandleBufferFailed()
    {
        // Takes the player out of DR mode
        isInDRLockMode = false;

        // Resets the spell sequence saved AND exits the player from Cast Mode (to idle)
        ResetSpellSequence();

        // Handles deactivating the AOE visuallizer 
        if (wandTipScript.IsAoeRaycastActive)
        {
            wandTipScript.DeactivateAoePlacementVisualizer();
        }
    }

    #region Spell Casting


    /// <summary>
    /// Listens for spell casting related key presses, updates the
    /// currentSpellType, updates the spellSequence based on the
    /// key presses and activates the "square" if the current
    /// spellSequence is a valid spell.
    /// </summary>
    /// NOTE: INSTANT SPELLS' LOGIC HAS NOT YET BEEN IMPLEMENTED. CHECK S.O 'BUFFER TYPE' SELECTION.
    private void HandleSpellCasting()
    {
        // Checks if any of the stored keys have been pressed
        // WARNING: Make sure to add letters in K_SpellKeys if adding spells with new letters
        KeyCode key = SpellKeyPressed();

        if (key != KeyCode.None)
        {
            // The nested method here checks if the DR lock sequence should be activated
            if (K_SpellKeys.spellTypes.Contains(key)) // Check if the button pressed is present in the pre defined list of spell categories
            {
                currentSpellType = key;

                // This method checks if DR should be activated and does so if yes
                HandleDrLockActivation();

                if (isInDRLockMode)
                {
                    /// Note: The following two lines were placed in this block to save
                    ///the spell sequence input BEFORE DR Lock is activated

                    // Add the button pressed to the spell sequence (the spell's existance is checked thereafter)
                    spellSequence += key.ToString();

                    // This writes the spell string sequence input on the top left corner of the screen
                    spellText.text = K_SpellKeys.cast.ToString() + spellSequence;


                    // If the DR Lock was activated, return
                    //making way for the DR letters sequence activation and player input

                    return;
                }
            }

            // handle parry letters generation here
            parryLetters.Value = "R";

            // This is for elements??
            // Add the button pressed to the spell sequence (the spell's existance is checked thereafter)
            spellSequence += key.ToString();

            // This writes the spell string sequence input on the top left corner of the screen
            spellText.text = K_SpellKeys.cast.ToString() + spellSequence;



            // Check if the spell (spell sequence) exists. If not,
            // reset the sequence, the spellText & the casting status
            if (!spellBuilder.SpellExists(spellSequence))
            {
                //Debug.LogFormat($"<color=red> Spell does not exist </color>");

                // Note: No need to cancel anim here since the anim is not active here yet
                StopCastBuffer();
                //StopCastBufferAnimationIfActive();

                return;
            }



            // If the animation is already active, deactivate it first
            if (castKey.Anim.GetCurrentAnimatorStateInfo(0).IsName("BufferOnce"))
            {
                Debug.LogFormat($"<color=red> Anim is playing > Stop playing </color>");

                // The animation is not currently playing, so start it ?
                castKey.StopCastBufferAnim();
            }

            InitCastProcudure();
        } 
    }

    void StopCastBufferAnimationIfActive()
    {
        // If the animation is already active, deactivate it first
        if (castKey.Anim.GetCurrentAnimatorStateInfo(0).IsName("BufferOnce"))
        {
            // The animation is not currently playing, so start it ?
            StopCastBuffer();
        }



    }

    void InitCastProcudure() 
    {
        //StopCastBufferAnimationIfActive();

        Debug.LogFormat($"<color=red> Anim is not playing > playing </color>");

        // Animate the cast buffer square
        InitiateCastProcedure(spellSequence);

        // Instead if the switch case above, the cast procedure could be gotten from the spell
        //or spell prefab itself
    }



    void InitiateCastProcedure(string spellSequence)
    {

        Debug.LogFormat($"<color=red> spellSequencespellSequence {spellSequence} </color>");

        string spellCastProcedure = spellBuilder.GetSpellCastProcedureType(spellSequence);


        switch (spellCastProcedure)
        {
            case "Instant":
                break;
            case "Buffered":
                castKey.StartCastBufferAnim();

                // Maybe we should add this into a conditional or something?
                invocationBoundsGO.SetActive(true);
                break;
            case "InstantPlaceable":
                //Activate Raycast
                // Send event to wand tip
                // Wand tip implements raycast using 'using'
                // 
                Debug.Log("ACTIVATE AOE RAYCAST HERE");
                break;
            case "BufferedPlaceable":
                //Activate Raycast
                // Send event to wand tip
                // Wand tip implements raycast using 'using'
                //
                wandTipScript.ActivateAoePlacementVisualizer();
                castKey.StartCastBufferAnim();
                //Activate Raycast
                break;
            default:
                Debug.Log("Exception Error: Spell not found");
                break;
        }


        // or 

        // Check cast type and handle accordingly
    }





    /// <summary>
    /// Checks if the square is inside the valid area and if the
    /// the current spellSequence is a valid spell. If both conditions
    /// are met, fires the spell.
    /// </summary>
    private void HandleSpellFiring() // This should act a router relatively to the type of spell
    {
        // Check if the key pressed is a spell category key &&
        // check if the spell in that category exists 
        if (castKey.TryCast() && spellBuilder.SpellExists(spellSequence)) // To move this somewhere else, include also a router
        {
            // TD: Instantiate a spell instance LOCALLY based on the spell name
            spellBuilder.UpdateDRTier(currentSpellType, spellSequence);

            newPlayerBehaviorScript.LocalPlayerClass.SetDRActive(spellSequence[0]);

            // This can be written better. Calling the Fire() method in each spell
            // The difficulty is making sure that the spells are casted the same way
            CastBySpellType(spellBuilder.GetSpellType(spellSequence));

            // This is to be replaced with the spellDRStatus dict
            //which will be used to track the DR status for each spell category
            lastSpellType = currentSpellType;
            StopCastBufferAnimationIfActive();
        } else
        {
            // if the G button is pressed repeatedly, take the player out of Cast Mode (CM)
            StopCastBufferAnimationIfActive();
        }
        inSpellCastModeOrWaitingSpellCategory = false;
        ResetSpellSequence();

    }




    void CastBySpellType(string spellType)
    {
        switch (spellType)
        {
            case "Projectile":
                ProjectileSpawnRpc(spellSequence, wandTip.transform.rotation, wandTip.transform.position);
                break;
            case "Sphere":
                SpellSpawnRpc(spellSequence, playerCenter.transform.rotation, playerCenter.transform.position);
                break;
            case "Beam":
                Debug.Log("NetworkManager.LocalClientId11111111111111 (" + NetworkManager.LocalClient.ClientId + ")");
                Debug.Log("NetworkManager.LocalClientId1111111111111 (" + OwnerClientId + ")");
                SpellSpawn2Rpc(spellSequence, Quaternion.LookRotation(new Vector3 (-wandTip.transform.rotation.x, wandTip.transform.rotation.y, wandTip.transform.rotation.z)), wandTip.transform.position);
                break;
            case "Aoe":
                /// TD: Instead of explicitely passing the position and rotatiom
                /// Shoot a projectile at the target location and spawn the aoe where the projectile hits the floor
                // Deconstructs the return value of the method to spawn the aoe at the specified location
                (Quaternion rotation, Vector3 position) = wandTipScript.GetAoeRotationAndPosition();

                //HandleAoeFiring(rotation, position);
                AoeSpawnRpc(spellSequence, rotation, position);
                break;
            case "Barrier":
                ProjectileSpawnRpc(spellSequence, barrierPoint.transform.rotation, barrierPoint.transform.position);
                break;
            case "Invocation":
                // Spawn 
                SpawnAtLocation(invocationSpawnerScript.SpawnOnce());
                break;

            default: 
                break;
        }
    }

    public void SpawnAtLocation(Vector3 spawnPosition)
    {
        SpawnAtLocationRpc(spawnPosition, spellSequence);
    }

    [Rpc(SendTo.Server)]
    private void SpawnAtLocationRpc(Vector3 spawnPosition, string spellSequenceParam)
    {
        //Debug.LogFormat($"<color=orange> Scepter SPAWN SPOT {spawnSpot.Value} </color>");

        GameObject spellInstance = Instantiate(prefabReferences[spellSequenceParam], spawnPosition, Quaternion.identity);

        NetworkObject netObj = spellInstance.GetComponent<NetworkObject>();
        // netObj.SpawnWithOwnership(NetworkManager.LocalClient.ClientId);
        netObj.SpawnWithOwnership(OwnerClientId);
    }


    [Rpc(SendTo.Server)]
    void SpellSpawn2Rpc(string spellSequenceParam, Quaternion rotation, Vector3 position)
    {
        Debug.Log("NetworkManager.LocalClientId (" + NetworkManager.LocalClient.ClientId + ")");
        Debug.Log("OwnerClientId (" + OwnerClientId + ")");

        GameObject spellInstance = Instantiate(prefabReferences[spellSequenceParam], position, rotation);

        NetworkObject netObj = spellInstance.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(OwnerClientId);

        // Set the parent locally first
        //spellInstance.transform.SetParent(gameObject.transform.GetChild(3).gameObject.transform);
        netObj.TrySetParent(gameObject.transform);

        // Call a custom RPC to handle parenting across the network
        //SetParentRpc(netObj.NetworkObjectId, gameObject.GetComponent<NetworkObject>().NetworkObjectId);

        inSpellCastModeOrWaitingSpellCategory = false;
        ignoreSpellDRLock[currentSpellType.ToString()] = false;

        ResetSpellSequence();
    }

    [Rpc(SendTo.Everyone)]
    void SetParentRpc(ulong childNetObjId, ulong parentNetObjId)
    {
        var childObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[childNetObjId].gameObject;
        var parentObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[parentNetObjId].gameObject;

        childObject.transform.SetParent(parentObject.transform);
    }

    [Rpc(SendTo.Server)]
    void SpellSpawnRpc(string spellSequenceParam, Quaternion rotation, Vector3 position)
    {
        Debug.Log("NetworkManager.LocalClientId (" + NetworkManager.LocalClient.ClientId + ")");
        Debug.Log("NetworkManager.LocalClientId (" + OwnerClientId + ")");

        GameObject spellInstance = Instantiate(prefabReferences[spellSequenceParam], position, rotation);

        NetworkObject netObj = spellInstance.GetComponent<NetworkObject>();
        // netObj.SpawnWithOwnership(NetworkManager.LocalClient.ClientId);
        netObj.SpawnWithOwnership(OwnerClientId);

        netObj.TrySetParent(gameObject.transform);


        inSpellCastModeOrWaitingSpellCategory = false;

        ignoreSpellDRLock[currentSpellType.ToString()] = false;

        ResetSpellSequence();
    }



    [Rpc(SendTo.Server)]
    void ProjectileSpawnRpc(string spellSequenceParam, Quaternion rotation, Vector3 position)
    {
        Debug.Log("NetworkManager.LocalClientId (" + NetworkManager.LocalClient.ClientId + ")");

        GameObject spellInstance = Instantiate(prefabReferences[spellSequenceParam], position, rotation);

        NetworkObject netObj = spellInstance.GetComponent<NetworkObject>();

        //netObj.SpawnWithOwnership(OwnerClientId);
        netObj.Spawn();

        //netObj.TrySetParent(gameObject.transform);
        inSpellCastModeOrWaitingSpellCategory = false;

        ignoreSpellDRLock[currentSpellType.ToString()] = false;

        ResetSpellSequence();
    }



    // Duplicated it because Sphere requires being parented and the AoE does not
    [Rpc(SendTo.Server)]
    void AoeSpawnRpc(string spellSequenceParam, Quaternion rotation, Vector3 position)
    {
        GameObject aoeInstance = Instantiate(prefabReferences[spellSequenceParam], position, rotation);

        NetworkObject netObj = aoeInstance.GetComponent<NetworkObject>();

        netObj.SpawnWithOwnership(NetworkManager.LocalClientId);

        inSpellCastModeOrWaitingSpellCategory = false;

        ignoreSpellDRLock[currentSpellType.ToString()] = false;

        ResetSpellSequence();
    }



    // Here we need to wait for a bit before setting the parent, as the lack of a pause would
    // cause an issue where the sphere would drag behind the CLIENT player character
    IEnumerator WaitForNextFrame(NetworkObject networkObject)
    {
        yield return new WaitForSeconds(.5f); // Wait for the end of the frame
        networkObject.TrySetParent(gameObject.transform);
        Debug.Log("Continuing code after waiting for a frame...");
        // Continue with the rest of your code here
    }



    // Stop animation & Reset casting variables + Cancel AoE raycast if active
    void StopCastBuffer()
    {
        castKey.StopCastBufferAnim();
        
        inSpellCastModeOrWaitingSpellCategory = false;
        ResetSpellSequence();

        if (wandTipScript.IsAoeRaycastActive)
        {
            wandTipScript.DeactivateAoePlacementVisualizer();
        }
    }






    /// <summary>
    /// Checks if a valid spell key was pressed this frame.
    /// </summary>
    /// <returns>The KeyCode of the pressed spell key. If no valid spell key
    /// was pressed, returns KeyCode.None</returns>
    private KeyCode SpellKeyPressed()
    {
        foreach (KeyCode key in allSpellKeys)
        {
            if (Input.GetKeyUp(key))
                return key;
        }

        return KeyCode.None;
    }

    #endregion








    #region Diminishing Returns (DR Lock)


    /// <summary>
    /// Checks if the user should be presented with a DR lock instance, and displays
    ///the DR lock instance on his UI if so.
    /// </summary>
    private void HandleDrLockActivation()
    {
        // DR has 1 Primary state and 1 Substate
        // The Primary state 

        // Handle showing the DR instance on the UI if the spell category's DR has not yet been solved
        if (!ignoreSpellDRLock[currentSpellType.ToString()])
        {
            // Gets the amount of DR keys to be discplayed to the player
            drLockKeysQueue = spellBuilder.GetDrKeys(currentSpellType); // Make sure that you have the spell category DR SO set in the spell builder on the player
            int keyCount = drLockKeysQueue.Count;

            if (keyCount > 0)
            {
                int activatedKeys = 0;

                for (int i = 0; i < keyCount; i++)
                {
                    K_DRKeyData drKeyData = drLockKeysQueue.ElementAt(i);
                    K_DRKey drKey = drUiKeysDictionary[drKeyData.keyCode.ToString()];

                    drKey.invisible = drKeyData.invisible;
                    drKey.buffered = drKeyData.buffered;
                    drKey.gameObject.SetActive(true);

                    if (i < maxStartingDrKeys)
                    {
                        drKey.SetActive(true);
                        activatedKeys++;
                    }
                }

                for (int i = 0; i < activatedKeys; i++)
                    drLockKeysQueue.Dequeue();

                isInDRLockMode = true;
                inSpellCastModeOrWaitingSpellCategory = false;

                ResetSpellSequence();

            }
        }
        //if (ignoreSpellDRLock[currentSpellType.ToString()] == false)
        // ignoreSpellDRLock[currentSpellType.ToString()] = false;
    }




    void ResetSpellSequence() 
    {

        if (castKey.Anim.GetCurrentAnimatorStateInfo(0).IsName("BufferOnce"))
        {
            castKey.StopCastBufferAnim();
        }

        inSpellCastModeOrWaitingSpellCategory = false;

        this.spellSequence = "";
        this.spellText.text = "";
    }





    // TO DO: The status for DR for each spell category
    //will be stored in a dictionary. This helps determine
    //if a spell is in DR.
    bool IsCurrentSpellCategoryInDR(string spellSequence)
    {

        // This will get its information from the NewPlayerBehavior script, which, throught the game manager, updates the playerClass information locally and on the server
        char firstChar = (char)currentSpellType;

        return newPlayerBehaviorScript.LocalPlayerClass.GetSpellCategoryActivityStatus(firstChar);
    }





    /// <summary>
    /// Reads player key presses, checks if the DR lock has been solved and
    /// if so, disables the DR lock. It also sets ignoreDrLock to true, so
    /// if the player tries to cast the spell it was casting before the
    /// DR lock, it dosn't get prompted with another DR lock.
    /// </summary>
    private void HandleDRLockInput()
    {
        KeyCode DRKeyPressed = DrKeyPressed();

        // if unlock interrupted
        if (DRKeyPressed != KeyCode.None || Input.GetKeyDown(K_SpellKeys.cast))
        {
            //Debug.LogFormat($"<color=red> 111 - HandleDrLockState - 111 </color>");

            // During DR: If the player presses the cast key or any other key that is not present
            //in the DR instance presented on his UI, deactivate all the DR letters and exit DR Mode 
            if (Input.GetKeyDown(K_SpellKeys.cast) || (drUiKeysDictionary.Keys.Contains(DRKeyPressed.ToString()) && !drUiKeysDictionary[DRKeyPressed.ToString()].isActiveAndEnabled))
            {
                // Deactivate all active DR keys
                foreach (var key in drUiKeysDictionary.Values)
                {
                    if (key.gameObject.activeSelf == true)
                    {
                        key.gameObject.SetActive(false);
                    }
                }

                // Reset the sequence and the cast animation
                ResetSpellSequence(); // To revise > Could be made more efficient

                // Exit DR Mode
                isInDRLockMode = false; 

                // Make sure that the spell category continues to be locked
                ignoreSpellDRLock[currentSpellType.ToString()] = false; // needed? <<<<<<<<<<<<<

                return;
            }

            // Check that the key pressed exists in the predefined DR key dictionary
            if (drUiKeysDictionary.Keys.Contains(DRKeyPressed.ToString()))
            {
                // Save the gameObject that is associated with the key pressed's class to this variable
                K_DRKey drKey = drUiKeysDictionary[DRKeyPressed.ToString()];

                // If the DR key's gameObject (letter on the UI) is active and the player is able to solve for it
                // deactivate that specific gameObject
                if (drKey.gameObject.activeSelf && drKey.TrySolve(DRKeyPressed))
                {
                    // The gameObject on the UI is deactivated
                    drKey.gameObject.SetActive(false);

                    // The key's ui gameObject is removed from the selected button/ gameObjects
                    //the player is solving for in the current DR instance
                    for (int i = 0; i < Mathf.Min(drLockKeysQueue.Count, drKeysPerSolve); i++)
                        drUiKeysDictionary[drLockKeysQueue.Dequeue().keyCode.ToString()].SetActive(true); // Activate the stored DR keys in fifo order and removes them from the list simultaneously
                    
                }
            }

            // Check if all the DR letters have been solved
            if (IsDrSolved())
            {
                // If all DR letters have been solved exit the player from DR mode
                isInDRLockMode = false;

                // Make sure that the spell category for which the DR lock was solved is skipped on the next cast attempt
                ignoreSpellDRLock[currentSpellType.ToString()] = true;

                // Place the player back in Spellcast Mode to allow him to cast the spell 
                //for whose spell category he just solved for
                inSpellCastModeOrWaitingSpellCategory = true;

                // Initiate the cast procedure 
                InitCastProcudure(); // TO RENAME
            }
        }
    }






    /// <summary>
    /// Checks if a key for DR lock has been pressed this frame and
    /// returns the corresponding KeyCode if so.
    /// </summary>
    /// <returns>The KeyCode of the DR lock key pressed this frame or
    /// KeyCode.None if no DR lock key was pressed.</returns>
    private KeyCode DrKeyPressed()
    {
        foreach (KeyCode key in K_SpellKeys.spellTypes)
        {
            if (Input.GetKeyUp(key))
                return key;
        }

        return KeyCode.None;
    }






    /// <summary>
    /// Checks if the DR lock has been solved by checking the active
    /// state of all the DR lock keys.
    /// </summary>
    /// <returns></returns>
    private bool IsDrSolved()
    {
        foreach (K_DRKey drKey in drUiKeysDictionary.Values)
        {
            // if there is still a DR key active on the UI (visible) keep the player in DR (unlock) mode
            if (drKey.gameObject.activeSelf) {
                Debug.LogFormat($"<color=orange> IsDrSolved + keyCode {drKey.gameObject} </color>");
                return false;
            }  
        }

        return true;
    }





    private void OnDrBufferFailed()
    {
        StartCoroutine(RestartDrCoroutine(0.2f));
    }





    /// <summary>
    /// Cancels de current DR lock after t seconds. Disables casting until the specified
    /// time has passed.
    /// </summary>
    /// <param name="t">The amount of time to wait before canceling the DR lock.</param>
    /// <returns></returns>
    private IEnumerator RestartDrCoroutine(float t)
    {
        canCast = false;

        yield return new WaitForSeconds(t);

        isInDRLockMode = false;
        canCast = true;
        drLockKeysQueue = null;

        foreach (K_DRKey drKey in drUiKeysDictionary.Values)
            drKey.gameObject.SetActive(false);

        spellBuilder.ResetCooldown();
    }

    #endregion
}

using IncapacitationEffect;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class K_SpellLauncher : NetworkBehaviour
{
    [Header("UI Elements")]
    [Tooltip("The text field which shows spell related inputs.")]
    [SerializeField] private TMP_Text spellText;

    public TMP_Text SpellText
    {
        get { return spellText; }
        set { spellText = value; }
    }

    [Tooltip("The cast key at the center of the DR keys. Should have castKey = true.")]
    [SerializeField] private K_DRKey castKey;

    public K_DRKey CastKey
    {
        get { return castKey; }
        set { castKey = value; }
    }

    [UDictionary.Split(20, 80)] public DrUiKeys drUiKeysDictionary;
    [UDictionary.Split(20, 80)] public DrUiKeys spellChargingUiKeysDictionary;
    [Serializable] public class DrUiKeys : UDictionary<string, K_DRKey> { }
    [UDictionary.Split(20, 80)] public DrUiKeys spellCastingUiKeysDictionary;

    [Header("DR Settings")]
    [Tooltip("The maximum amount of keys to be activated at the start of a DR lock.")]
    [SerializeField, Min(1)] private int maxStartingDrKeys;
    [Tooltip("The number of DR keys to activate after solving a key.")]
    [SerializeField, Min(1)] private int drKeysPerSolve;

    private bool canCast = true;
    private bool inSpellCastModeOrWaitingSpellCategory;

    public bool InSpellCastModeOrWaitingSpellCategory
    {
        get { return inSpellCastModeOrWaitingSpellCategory; }
        set { inSpellCastModeOrWaitingSpellCategory = value; }
    }

    private bool isInDRLockMode;

    public bool IsInDRLockMode
    {
        get { return isInDRLockMode; }
        set { isInDRLockMode = value; }
    }

    private bool isInSpellChargingMode;

    public bool IsInSpellChargingMode
    {
        get { return isInSpellChargingMode; }
        set { isInSpellChargingMode = value; }
    }

    private bool castingIncapacitated;

    private Queue<K_DRKeyData> drLockKeysQueue;
    public Queue<K_DRKeyData> spellChargingKeysQueue;

    public K_DRKeyData GetElementAtSpellChargingKeysQueue(int i)
    {
        return spellChargingKeysQueue.ElementAt(i);
    }

    private string spellSequence = "";

    public string SpellSequence
    {
        get { return spellSequence; }
        set { spellSequence = value; }
    }

    private KeyCode[] allSpellKeys;

    private KeyCode currentSpellType;

    public KeyCode CurrentSpellType
    {
        get { return currentSpellType; }
    }


    string g_currentSpellSequence = "";
    public String g_CurrentSpellSequence
    {
        get { return g_currentSpellSequence; }
        set { g_currentSpellSequence = value; }
    }
    

    private KeyCode lastSpellType = KeyCode.None;
    private bool ignoreDrLock;

    public delegate void OnBufferFailedEventHandler();

    //K_Spell spell;

    //[SerializeField] GameObject projectile_test;

    [SerializeField] public K_SpellBuilder spellBuilder;

    [Header("Spells Spawn Points' gameObjects")]
    [SerializeField] GameObject wandTipGO;
    WandTip wandTipScript;
    Transform wandTip;

    [SerializeField] GameObject playerCenterGO;
    Transform playerCenter;
    NetworkObject playerCenterNO;

    [SerializeField] GameObject dispellGO;
    Transform dispellTransform;


    [SerializeField] GameObject barrierPointGO;
    Transform barrierPoint;

    [SerializeField] GameObject invocationBoundsGO;
    InvocationSpawner invocationSpawnerScript;
    Transform invocationBounds;

    [SerializeField] GameObject theProjectile1;


    GameObject localSpellInstance;

    // public Dictionary<string, K_SpellData> spellDictionary = new Dictionary<string, K_SpellData>();
    public Dictionary<FixedString128Bytes, GameObject> localSpellInstances = new Dictionary<FixedString128Bytes, GameObject>();

    // public Dictionary<string, K_SpellData> spellDictionary = new Dictionary<string, K_SpellData>();

    // Holds all the prefabs for all the spells
    public Dictionary<string, GameObject> spellPrefabsReferences = new Dictionary<string, GameObject>(); // =================  SPELL PREFAB REFERENCES

    // This will be injected with the DR status for each spell category
    public Dictionary<string, bool> ignoreSpellDRLock = new Dictionary<string, bool>();

    NewPlayerBehavior newPlayerBehaviorScript;

    [SerializeField] private PlayerSpellParryManager playerSpellParryManager;

    public NetworkVariable<FixedString32Bytes> parryLetters = new NetworkVariable<FixedString32Bytes>(default,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString32Bytes>  ParryLetters {
        get { return parryLetters; }
        set { parryLetters = value; }
    }

    private SpellChargingManager spellChargingManager;

    public delegate void OnPlayerCast(bool isInCastMode);
    public static event OnPlayerCast CastModeSpeedChange;

    bool castModeMoveSpeedSlow = true;
    bool castModeMoveSpeedReset = false;


    // 2 keycode properties that hold the values for spell type and elemental transmutation
    // Pressing G casts the last saved combination
    // 



    // SpellCastController 



    // SpellCastManager 



    // SpallUnlockManager

    public void Start()
    {
        //// PlayerBehavior itself????
        // subscribe to the event that handles incapacitating the player movement or casting when hit by an incapacitating effect spell
        Incapacitation.playerIncapacitation += HandleIncapacitation;

        K_DRKey.onPlayerFailedToSyncInputToBuffer += HandleBufferFailed;


        K_ProjectileSpell.projectileInstance += HandleDestroyProjectile;

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
        dispellGO.SetActive(false);


        // The SpellDict cannot be saved to a Dictionary reference directly
        // This saves a copy of the spell key to prefab, and is used later to spawn the spell object
        foreach (var spellData in this.GetComponent<K_SpellBuilder>().spellDictionary)
        {
            spellPrefabsReferences.Add(spellData.Key.ToString(), spellData.Value.prefab);
        }

        // Checking whenever we are casting spell and shoud ignore 
        foreach (KeyCode key in K_SpellKeys.spellTypes)
        {
            ignoreSpellDRLock.Add(key.ToString(), false);
        }

        //// END SpellCastManager 

        //// START SpellUnlockManager

        // when try solve DR and fails is called
        foreach (K_DRKey drKey in drUiKeysDictionary.Values)
            drKey.OnBufferFailed += OnDrBufferFailed;

        //// END SpellUnlockManager
        ///

        spellChargingManager = new SpellChargingManager(this, spellBuilder);

        Debug.LogFormat($"<color=red> IsLocalPlayer {IsLocalPlayer} OwnerClientId {OwnerClientId} </color>");
    }


    public override void OnNetworkSpawn()
    {
        //if (!IsLocalPlayer) return;


        base.OnNetworkSpawn();

        invocationBoundsGO.SetActive(false);
        dispellGO.SetActive(false);
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
            //if (CastModeSpeedChange != null) CastModeSpeedChange(castModeMoveSpeedSlow);
            // Debug.LogFormat($"<color=red> UPDATE !!!! IS DR LOCK !!!! UPDATE </color>");65149835
            HandleDRLockInput();
            return;
        } else
        {
            //if (CastModeSpeedChange != null) CastModeSpeedChange(castModeMoveSpeedReset);
        }

        // >>>>>>>>>>>>>>>> CHARGING MODE ENTRY <<<<<<<<<<<<<<<<<
        // If the player is in spell charging mode, the spell charging manager handles the input
        if (isInSpellChargingMode)
        {
            spellChargingManager.HandleSpellChargingInput();
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
                // To do: Slow down player character by 50% - Here <<<<<<

                if (CastModeSpeedChange != null) CastModeSpeedChange(castModeMoveSpeedReset);

                // This fires the spell, and changes the bool value of the 'inSpellCastModeOrWaitingSpellCategory' parameter to false
                HandleSpellFiring();
                UpdateDynamicNextKeysUI();
            }
          
            // SPELL BUILDING // Handles for spell category inputs + exceptions
            HandleSpellCasting();
            if (!isInDRLockMode && !isInSpellChargingMode)
                UpdateDynamicNextKeysUI();

        }
        else // >>>>>>>>> This places the player in SPELLCAST MODE
        {
            // After casting a spell this should not immediately reenable the player's ability to enter cast mode
            if (Input.GetKeyUp(K_SpellKeys.cast))
            {
                inSpellCastModeOrWaitingSpellCategory = true;

                if (CastModeSpeedChange != null) CastModeSpeedChange(castModeMoveSpeedSlow);

                // This writes the sequence input on the top right corner of the screen
                // note: G is not saved here to the spell sequence array
                spellText.text = K_SpellKeys.cast.ToString();
                if (!isInDRLockMode && !isInSpellChargingMode)
                    ShowDynamicStartKeys();
                return;
            }

            // If the player is in idle mode, and can parry a spell handle the input
            AllowParryInput();
        }

    }

    private void AllowParryInput()
    {
        KeyCode key = SpellKeyPressed();

        // Currently Active Parry Key clould be any of the SpellTypes keys
        if (K_SpellKeys.spellTypes.Contains(key))
        {
            playerSpellParryManager.TryToParry(key.ToString());
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
        Debug.LogFormat($"<color=red> HandleBufferFailed </color>");
        // Takes the player out of DR mode
        isInDRLockMode = false;

        // Takes the player out of SpellCharging mode
        spellChargingManager.DeactivateSpellChargingMode();

        // Re-establish base player speed here <<<<<<<<<<<<

        // Resets the spell sequence saved AND exits the player from Cast Mode (to idle)
        ResetSpellSequence();

        if (CastModeSpeedChange != null) CastModeSpeedChange(castModeMoveSpeedReset);

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
        //Debug.LogFormat($"<color=red> 1 </color>");

        if (key != KeyCode.None)
        {
            //Debug.LogFormat($"<color=red> 2 </color>");
            // The nested method here checks if the DR lock sequence should be activated
            if (K_SpellKeys.spellTypes.Contains(key)) // Check if the button pressed is present in the pre defined list of spell categories
            {
                //Debug.LogFormat($"<color=red> 3 {spellSequence} </color>");
                currentSpellType = key;

                g_CurrentSpellSequence += key.ToString();
    
                // Add the new key to the sequence BEFORE checking
                string tempSequence = spellSequence + key.ToString();

                // This method checks if DR should be activated and does so if yes
                HandleDrLockActivation();

                if (isInDRLockMode)
                {
                    //Debug.LogFormat($"<color=red> 4 {spellSequence} </color>");

                    /// Note: The following two lines were placed in this block to save
                    ///the spell sequence input BEFORE DR Lock is activated

                    // Add the button pressed to the spell sequence (the spell's existance is checked thereafter)
                    spellSequence += key.ToString();

                    // This writes the spell string sequence input on the top left corner of the screen
                    spellText.text = K_SpellKeys.cast.ToString() + spellSequence;

                    //Debug.LogFormat($"<color=red> 4.5 {spellSequence} </color>");

                    // If the DR Lock was activated, return
                    //making way for the DR letters sequence activation and player input

                    return;
                }

                // >>>>>>>>>>>>>>>> POST DR INPUT - SPELL CHARGING MODE ENTRY <<<<<<<<<<<<<<
                // This method checks if SpellCharging should be activated
                // Checks if there is a spell already being casted
                // Checks if the spell type is a spell charging type
                // Activates Charging keys if so
                // if (SpellSequence.Length == 1)
                // {
                    //HandlePeriCastLockProcedure();
                //}
                // new - Check the type of peri cast lock and activate the correct associated spell
                // Tried modifying this with the HandlePeriCastProcedure(); option above but it didn't work as expected
                spellChargingManager.HandleSpellChargingActivation(tempSequence);

                

                if (isInSpellChargingMode)
                {
                    // If the spell charging was activated, return
                    //making way for the Spell Charging letters sequence activation and player input
                    return;
                }

            }

            spellText.text = K_SpellKeys.cast.ToString() + spellSequence;

            // This is for elements??
            // Add the button pressed to the spell sequence (the spell's existance is checked thereafter)
            spellSequence += key.ToString();

            // This writes the spell string sequence input on the top left corner of the screen
            spellText.text = K_SpellKeys.cast.ToString() + spellSequence;

            // if (SpellSequence.Length == 1 || spellBuilder.GetPeriCastLockProcedure(spellSequence) == "Charging")
            // {
            //     HandlePeriCastLockProcedure();
            // }
            spellChargingManager.HandleSpellChargingActivation(spellSequence);
            // Check if the spell (spell sequence) exists. If not,
            // reset the sequence, the spellText & the casting status
            if (!spellBuilder.SpellExists(spellSequence)) // TO DO - Make this able to detect a spell with an unfinished sequence
            {
                Debug.LogFormat($"<color=red> Spell does not exist {spellSequence} </color>");
                // Note: No need to cancel anim here since the anim is not active here yet
                StopCastBuffer();
                //StopCastBufferAnimationIfActive();

                return;
            }



            // If the animation is already active, deactivate it first
            if (castKey.Anim.GetCurrentAnimatorStateInfo(0).IsName("BufferOnce"))
            {
                // The animation is not currently playing, so start it ?
                castKey.StopCastBufferAnim();
            }

            InitCastProcedure(); // (exists in one other place in this script) This can be replaced by InitiateCastProcedure(spellSequence);
            UpdateDynamicNextKeysUI();
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




    public void InitCastProcedure()
    {
        //StopCastBufferAnimationIfActive();

        Debug.LogFormat($"<color=red> Anim is not playing > playing </color>");


        //HandlePeriCastLockProcedure(); // If this is resolved, init cast procedure

        // Animate the cast buffer square
        InitiateCastProcedure(spellSequence);

        // Instead if the switch case above, the cast procedure could be gotten from the spell
        //or spell prefab itself
        if (this.spellBuilder.GetIsSpellParriable(spellSequence))
        {
            SetParryLetterServerRpc(playerSpellParryManager.GeneratePlayerParryAnticipation(spellSequence));
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetParryLetterServerRpc(string parryLetter)
    {
        if (!IsServer) // Ensure this runs only on the server
        {
            Debug.LogError("SetParryLetterServerRpc called on a client! This should only run on the server.");
            return;
        }

        // This method will be executed on the server to set the value of the NetworkVariable
        this.parryLetters.Value = parryLetter;
    }

    // A spell is only cast here if the sequence contains a spellcast procedure
    void InitiateCastProcedure(string spellSequence)
    {

        Debug.LogFormat($"<color=red> spellSequencespellSequence {spellSequence} </color>");

        string spellCastProcedure = spellBuilder.GetSpellCastProcedureType(spellSequence);

        //spellChargingManager.HandleSpellChargingActivation(spellSequence);


        switch (spellCastProcedure)
        {
            case "Instant":
                break;
            case "Buffered":
                castKey.StartCastBufferAnim((isInSpellChargingMode) ? 0.7f : 1f);

                // Maybe we should add this into a conditional or something?
                //invocationBoundsGO.SetActive(true);
                break;
            case "InstantPlaceable":
                //Activate Raycast
                // Send event to wand tip
                // Wand tip implements raycast using 'using'
                Debug.Log("ACTIVATE AOE RAYCAST HERE");
                break;
            case "BufferedPlaceable":
                //Activate Raycast
                // Send event to wand tip
                // Wand tip implements raycast using 'using'
                wandTipScript.ActivateAoePlacementVisualizer();
                castKey.StartCastBufferAnim();
                //Activate Raycast
                break;
            default:
                Debug.Log("Exception Error: Spell not found");
                break;
        }
    }

    // This was to initially handle lock procedures preceding or proceeding a spell cast
    // Such as charging or channeling
    void HandlePeriCastLockProcedure()
    {
        // if (spellSequence.Contains("") 
        // || spellSequence.Length == 0
        // || spellBuilder.GetPeriCastLockProcedure(spellSequence).Contains("None"))
        // return;
        // Save the type of peri cast lock procedure it is
        string periCastLockProcedure = spellBuilder.GetPeriCastLockProcedure(spellSequence);

        Debug.LogFormat($"<color=orange>SpellLauncher > PeriCastLockProcedure: {periCastLockProcedure}</color>");

        switch (periCastLockProcedure)
        {
            case "Charging":
                // Charging locks a spell before it is casted
                spellChargingManager.HandleSpellChargingActivation();
                break;
            case "Channeling":
                // Channeling is a mid-spell lock whose resolution increases the lifetime of a spell
                break;
            default:
                Debug.Log("Exception Error: Spell type not found");
                return;
        }
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

            //newPlayerBehaviorScript.LocalPlayerClass.SetDRActive(spellSequence[0]);

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
                //localSpellInstance = Instantiate(spellPrefabsReferences[spellSequence], wandTip.transform.position, wandTip.transform.rotation);

                //var localSpellId = localSpellInstance.GetComponent<K_ProjectileSpell>().localSpellId;

                //localSpellInstances.Add(localSpellId, localSpellInstance);

                //ProjectileSpawnRpc(spellSequence, wandTip.transform.rotation, wandTip.transform.position, localSpellId);
                ProjectileSpawnRpc(spellSequence, wandTip.transform.rotation, wandTip.transform.position);
                break;
            case "Sphere":
                // A local instance of the sphere is created 
                // to have the sphere spawn correctly in the center of the player
                localSpellInstance = Instantiate(spellPrefabsReferences[spellSequence], playerCenter.transform.position, playerCenter.transform.rotation, gameObject.transform);

                // Disabling these on the local copy of the spell so no errors are thrown when shield is being destoyed
                // otherwise the functionality does not work
                if (localSpellInstance.GetComponent<K_SphereSpell>())
                {
                    localSpellInstance.GetComponent<K_SphereSpell>().enabled = false;
                }
                
                localSpellInstance.GetComponent<SphereCollider>().enabled = false;
                localSpellInstance.GetComponent<NetworkObject>().enabled = false;

                SpellSpawnRpc(spellSequence, playerCenter.transform.rotation, playerCenter.transform.position);
                break;
            case "Beam":
                // A modification to the rotation is added to the wandTip because the beam was first momentarily spawning with an incorrect rotation
                // Now the rotation of the beam spawns in the correct direction and correct rotation
                SpellSpawn2Rpc(spellSequence, wandTip.transform.rotation * Quaternion.Euler(0, -90, 0), wandTip.transform.position);

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
                BarrierSpawnRpc(spellSequence, barrierPoint.transform.rotation, barrierPoint.transform.position);
                break;
            case "Invocation":
                // Spawn 
                invocationBoundsGO.gameObject.SetActive(true);

                SpawnAtLocation(invocationSpawnerScript.SpawnOnce());

                invocationBoundsGO.gameObject.SetActive(false);
                break;
            case "Charm":
                // dispellGO.SetActive(true);
                ProjectileSpawnRpc(spellSequence, wandTip.transform.rotation, wandTip.transform.position, true);
                break;
            case "Conjured":
                break;
            default: 
                break;
        }
    }

    public void SpawnAtLocation(Vector3 spawnPosition)
    {
        SpawnAtLocationRpc(spawnPosition, spellSequence);
    }

    public void DestroyLocalShield()
    {
        Destroy(localSpellInstance);
    }    
    
    public void HandleDestroyProjectile(FixedString128Bytes spellId)
    {
        if (localSpellInstances.TryGetValue(spellId, out GameObject obj))
        {
            Destroy(obj);            // Destroys the GameObject
            localSpellInstances.Remove(spellId);   // Optionally remove the reference from the dictionary
        }
        else
        {
            Debug.Log("Object not found!");
        }
        //Destroy(localSpellInstance);
    }


    [Rpc(SendTo.Server)]
    private void SpawnAtLocationRpc(Vector3 spawnPosition, string spellSequenceParam)
    {

        GameObject spellInstance = Instantiate(spellPrefabsReferences[spellSequenceParam], spawnPosition, Quaternion.identity);

        NetworkObject netObj = spellInstance.GetComponent<NetworkObject>();

        netObj.SpawnWithOwnership(OwnerClientId);

        ResetPlayerCastStateAndDRRPC(currentSpellType.ToString());

        ResetSpellSequence();
    }


    [Rpc(SendTo.Server)]
    void SpellSpawn2Rpc(string spellSequenceParam, Quaternion rotation, Vector3 position)
    {
        Debug.Log("NetworkManager.LocalClientId (" + NetworkManager.LocalClient.ClientId + ")");
        Debug.Log("OwnerClientId (" + OwnerClientId + ")");

        GameObject spellInstance = Instantiate(spellPrefabsReferences[spellSequenceParam], position, rotation);

        NetworkObject netObj = spellInstance.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(OwnerClientId);

        // Set the parent locally first
        //spellInstance.transform.SetParent(gameObject.transform.GetChild(3).gameObject.transform);
        netObj.TrySetParent(gameObject.transform);

        // Call a custom RPC to handle parenting across the network
        //SetParentRpc(netObj.NetworkObjectId, gameObject.GetComponent<NetworkObject>().NetworkObjectId);

        ResetPlayerCastStateAndDRRPC(currentSpellType.ToString());

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
        GameObject spellInstance = Instantiate(spellPrefabsReferences[spellSequenceParam], playerCenterGO.transform.position, rotation);

        NetworkObject netObj = spellInstance.GetComponent<NetworkObject>();

        netObj.SpawnWithOwnership(OwnerClientId);

        netObj.TrySetParent(gameObject.GetComponent<NetworkObject>());

        if (netObj.GetComponent<K_SphereSpell>())
        {
            netObj.GetComponent<K_SphereSpell>().AssignParent(gameObject.transform);
            netObj.TrySetParent(gameObject.transform);
        }

        HideForOwnerRpc(netObj);

        ResetPlayerCastStateAndDRRPC(currentSpellType.ToString());

        ResetSpellSequence();
    }


    [Rpc(SendTo.Owner)]
    void HideForOwnerRpc(NetworkObjectReference spellRef)
    {
        if (spellRef.TryGet(out NetworkObject netObj) && netObj.gameObject.GetComponent<MeshRenderer>() != null)
        {
            //The network instance is spawning off center for the client. This is the workaround
            //The network instance is hidden to the local player and shown the local instance instead for a more accurate positioning
            netObj.gameObject.GetComponent<MeshRenderer>().enabled = false; // Hide the networked version only for the caster
        }
    }


    [Rpc(SendTo.Server)]
    void ProjectileSpawnRpc(string spellSequenceParam, Quaternion rotation, Vector3 position, bool isWithOwnership = false)
    {
        Debug.Log("NetworkManager.LocalClientId (" + NetworkManager.LocalClient.ClientId + ")");

        GameObject spellInstance = Instantiate(spellPrefabsReferences[spellSequenceParam], position, rotation);

        if (this.spellBuilder.GetIsSpellParriable(spellSequence))
        {
            SpellsClass projectile = spellInstance.GetComponent<SpellsClass>();
            projectile.parryLetters = this.parryLetters;
        }

        NetworkObject netObj = spellInstance.GetComponent<NetworkObject>();

        if (isWithOwnership)
        {
            netObj.SpawnWithOwnership(NetworkManager.LocalClientId);
            if (netObj.GetComponent<HealSelf>())
            {
                netObj.GetComponent<HealSelf>().HealTarget(OwnerClientId);
            }
        }
        else
        {
            netObj.Spawn();
        }

        ResetPlayerCastStateAndDRRPC(currentSpellType.ToString());

        ResetSpellSequence();
    }

    [Rpc(SendTo.Owner)]
    void ResetPlayerCastStateAndDRRPC(string spellCategory)
    {
        inSpellCastModeOrWaitingSpellCategory = false;

        ignoreSpellDRLock[spellCategory] = false;
    }
    
    
    [Rpc(SendTo.Server)]
    void BarrierSpawnRpc(string spellSequenceParam, Quaternion rotation, Vector3 position)
    {
        Debug.Log("NetworkManager.LocalClientId (" + NetworkManager.LocalClient.ClientId + ")");

        GameObject spellInstance = Instantiate(spellPrefabsReferences[spellSequenceParam], position, rotation);

        NetworkObject netObj = spellInstance.GetComponent<NetworkObject>();

        netObj.Spawn();

        ResetPlayerCastStateAndDRRPC(currentSpellType.ToString());

        ResetSpellSequence();
    }



    // Duplicated it because Sphere requires being parented and the AoE does not
    [Rpc(SendTo.Server)]
    void AoeSpawnRpc(string spellSequenceParam, Quaternion rotation, Vector3 position)
    {
        GameObject aoeInstance = Instantiate(spellPrefabsReferences[spellSequenceParam], position, rotation);

        NetworkObject netObj = aoeInstance.GetComponent<NetworkObject>();

        netObj.SpawnWithOwnership(NetworkManager.LocalClientId);

        ResetPlayerCastStateAndDRRPC(currentSpellType.ToString());

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
    public void StopCastBuffer()
    {
        castKey.StopCastBufferAnim();
        
        inSpellCastModeOrWaitingSpellCategory = false;
        ResetSpellSequence();
        HideAllDynamicNextKeys();

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
    public KeyCode SpellKeyPressed()
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
                // Hide all DR keys first
                foreach (var kv in drUiKeysDictionary.Values)
                {
                    if (kv.gameObject.activeSelf)
                        kv.gameObject.SetActive(false);
                }

                Debug.LogFormat($"<color=orange>DR> keys to activate: {keyCount}</color>");

                // Activate ALL keys provided by the ScriptableObject for this tier
                for (int i = 0; i < keyCount; i++)
                {
                    K_DRKeyData drKeyData = drLockKeysQueue.ElementAt(i);
                    string keyName = drKeyData.keyCode.ToString();
                    if (!drUiKeysDictionary.ContainsKey(keyName))
                    {
                        Debug.LogWarning($"DR> Missing UI key for '{keyName}' in drUiKeysDictionary");
                        continue;
                    }
                    K_DRKey drKey = drUiKeysDictionary[keyName];

                    drKey.invisible = drKeyData.invisible;
                    drKey.buffered = drKeyData.buffered;
                    drKey.gameObject.SetActive(true);
                    drKey.SetActive(true);
                    Debug.LogFormat($"DR> Activated key '{keyName}' (invisible={drKeyData.invisible}, buffered={drKeyData.buffered})");
                }

                // Do not dequeue here; we show all keys from the SO directly

                isInDRLockMode = true;
                inSpellCastModeOrWaitingSpellCategory = false;
                // Ensure dynamic hint keys are hidden during DR
                HideAllDynamicNextKeys();

                //ResetSpellSequence();

            }
        }
        //if (ignoreSpellDRLock[currentSpellType.ToString()] == false)
        // ignoreSpellDRLock[currentSpellType.ToString()] = false;
    }




    public void ResetSpellSequence()
    {
        playerSpellParryManager.HidePlayerParryAnticipation();

        if (castKey.Anim.GetCurrentAnimatorStateInfo(0).IsName("BufferOnce"))
        {
            castKey.StopCastBufferAnim();
        }

        inSpellCastModeOrWaitingSpellCategory = false;

        this.spellSequence = "";
        this.spellText.text = "";
        
        g_CurrentSpellSequence = "";
        HideAllDynamicNextKeys();
    }

    private void ShowDynamicStartKeys()
    {
        HideAllDynamicNextKeys();
        foreach (KeyCode key in K_SpellKeys.spellTypes)
        {
            string k = key.ToString();
            if (spellCastingUiKeysDictionary.ContainsKey(k))
            {
                var uiKey = spellCastingUiKeysDictionary[k];
                // Only skip if DR or SpellCharging is currently active
                if ((isInDRLockMode || isInSpellChargingMode) &&
                    ((drUiKeysDictionary != null && drUiKeysDictionary.Values.Contains(uiKey)) ||
                     (spellChargingUiKeysDictionary != null && spellChargingUiKeysDictionary.Values.Contains(uiKey))))
                    continue;
                uiKey.invisible = false;
                uiKey.buffered = false;
                uiKey.gameObject.SetActive(true);
                uiKey.SetActive(true);
            }
        }
    }

    private void UpdateDynamicNextKeysUI()
    {
        // Do not show dynamic spell-casting hints while in DR or SpellCharging modes
        if (isInDRLockMode || isInSpellChargingMode)
        {
            HideAllDynamicNextKeys();
            return;
        }

        if (!inSpellCastModeOrWaitingSpellCategory)
        {
            HideAllDynamicNextKeys();
            return;
        }

        var nextKeys = spellBuilder.GetNextValidKeys(spellSequence);

        // First hide all dynamic-only keys
        HideAllDynamicNextKeys();

        // Show only next valid keys
        foreach (var key in nextKeys)
        {
            string k = key.ToString();
            if (spellCastingUiKeysDictionary.ContainsKey(k))
            {
                var uiKey = spellCastingUiKeysDictionary[k];
                // Only skip if DR or SpellCharging is currently active
                if ((isInDRLockMode || isInSpellChargingMode) &&
                    ((drUiKeysDictionary != null && drUiKeysDictionary.Values.Contains(uiKey)) ||
                     (spellChargingUiKeysDictionary != null && spellChargingUiKeysDictionary.Values.Contains(uiKey))))
                    continue;
                uiKey.invisible = false;
                uiKey.buffered = false;
                uiKey.gameObject.SetActive(true);
                uiKey.SetActive(true);

                // Optional: set context-specific icon/label
                var context = spellBuilder.GetNextKeyContextTag(spellSequence, key);
                var uiHelper = uiKey.GetComponent<K_SpellKeyUI>();
                if (uiHelper != null)
                {
                    // You can map context->Sprite via a small ScriptableObject or a serialized dictionary
                    // Here we set just the label; icon mapping can be added later.
                    if (!string.IsNullOrEmpty(context))
                        uiHelper.SetLabel(context);
                }
            }
        }
    }

    private void HideAllDynamicNextKeys()
    {
        if (spellCastingUiKeysDictionary == null) return;
        foreach (var kv in spellCastingUiKeysDictionary.Values)
        {
            // Only preserve DR/Charging keys while those modes are active
            if ((isInDRLockMode || isInSpellChargingMode) &&
                ((drUiKeysDictionary != null && drUiKeysDictionary.Values.Contains(kv)) ||
                 (spellChargingUiKeysDictionary != null && spellChargingUiKeysDictionary.Values.Contains(kv))))
                continue;
            if (kv.gameObject.activeSelf)
            {
                kv.gameObject.SetActive(false);
            }
        }
    }

    public void RefreshDynamicSpellCastingHints()
    {
        UpdateDynamicNextKeysUI();
    }


    public void SetSpawnCenter(NetworkObject netObjParam)
    {
        Debug.Log("SETTING SPAWN CENTER 333333");
        playerCenterNO = netObjParam;
    }


    // TO DO: The status for DR for each spell category
    //will be stored in a dictionary. This helps determine
    //if a spell is in DR.
    // This has likely been discontinued
    // bool IsCurrentSpellCategoryInDR(string spellSequence)
    // {

    //     // This will get its information from the NewPlayerBehavior script, which, throught the game manager, updates the playerClass information locally and on the server
    //     char firstChar = (char)currentSpellType;

    //     return newPlayerBehaviorScript.LocalPlayerClass.GetSpellCategoryActivityStatus(firstChar);
    // }





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

                spellChargingManager.HandleSpellChargingActivation();

                if (!isInSpellChargingMode)
                {
                    inSpellCastModeOrWaitingSpellCategory = true;
                    RefreshDynamicSpellCastingHints();
                }
                    
                // Initiate the cast procedure 
                InitCastProcedure(); // TO RENAME
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

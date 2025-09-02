using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using DotTimers;
using System;
using UnityEngine.SocialPlatforms;


public class PlayerBehavior : NetworkBehaviour
{
    bool hasEntered = false;

    //public DotTimer dotTimer;

    [Header("Player Status")]
    PlayerMovement playerMovement;
    public bool isIncapacitated = false;
    private float incapacitatedDuration;


    [Header("Spawn")]
    // Default position of the player when he first enters the game
    private Vector2 defaultPositionRange = new Vector2(-4, 4);
    // [SerializeField] Transform P1Spawn;
    // [SerializeField] Transform P2Spawn;

    [SerializeField] private Transform playerTransform;

    [Header("Player")]
    ulong g_localId;
    public bool isOnFloor;

    [Header("Health")]
    [SerializeField] GameObject healthUi;
    public bool isAlive = true;
    private float _maxHealth = 100f;
    public NetworkVariable<float> health = new NetworkVariable<float>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    HealthBar _healthBar;

    //Canvas statsUi;


    [Header("Interactables")]
    List<int> spellObjects = new List<int>();
    Dictionary<int, bool> barrierObjects = new Dictionary<int, bool>(); // I created this in case there was an overlap of barriers - To implement (obj id & bool)

    [Header("Local Active Spells")]

    // there are two variables here a NV and a regular variable
    // The NV keeps track of the current sphere shielding status
    // The regular variable is there to check whether the shield had just been destroyed and 
    //prevent the player from taking damage from the spell that broke the shield
    public NetworkVariable<bool> localSphereShieldActive = new NetworkVariable<bool>(default,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    bool sphereShieldJustDestroyed = false;
    bool isShieldActive = false;
    GameObject currentSphereShield;
    List<int> earthBarriers = new List<int>();

    [Header("Spells")]
    GameObject enemyBeam;

    [Header("Other spells")]
    bool isDot = false;
    public NetworkVariable<bool> spellIsDot = new NetworkVariable<bool>(default,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // ############################################################################################
    // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> ORGANISE <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
    // ############################################################################################

    [Header("Dot spell damage")]
    float lastTime;
    float interval = 0.5f;
    float g_dotDamage = 0;

    float remainingDotTime = 0;
    bool isInteractingWithDotSpell = false;
    bool isDotPersistent = false;

    bool isRemovingDotSpellsFromList = false;

    List<int> keysToRemove = new List<int>();

    private Dictionary<int, DefaultDotTimer> dotTimers = new Dictionary<int, DefaultDotTimer>();


    PlayerInput playerInput;
    [SerializeField] AnimationStateController animationStateController;
    UiReferences uiReferences;

    [SerializeField] GameObject mistOverlayObject;

    


    public bool IsInteractingWithDotSpell
    {
        get { return isInteractingWithDotSpell; }
        set { isInteractingWithDotSpell = value; }
    }

    public bool IsDotPersistent
    {
        get { return isDotPersistent; }
        set { isDotPersistent = value; }
    }

    //MistOverlay mistOverlay;

    void Awake()
    {
        uiReferences = GetComponentInParent<UiReferences>();

        float lastTime = Time.time;
        g_localId = NetworkManager.Singleton.LocalClientId;

        localSphereShieldActive.OnValueChanged += DamageBehavior;
        
        // IMPORTANT
        //_healthBar = StatsUi.Instance.GetComponent<StatsUi>().GetComponentInChildren<HealthBar>();

        playerInput = GetComponentInChildren<PlayerInput>();

        playerMovement = GetComponent<PlayerMovement>();

        //mistOverlayObject.SetActive(false);

        //mistOverlay = overlaysObject.GetComponentInChildren<MistOverlay>();



    }

    public AnimationStateController AnimationStateController
    {
        get { return animationStateController; }
    }

    void OnEnable()
    {
        Beam.beamHitPlayer += BeamHitPlayer;
        SphereShield.shieldExists += ShieldStatus;
        Mist.mistStatus += MistStatus;
        FireAoe.fireAoeExists += FireAoeStatus;
    }



    void Start()
    {
        //isInteractingWithDotSpell || remainingDotTime 
        //if (!IsOwner) return;
        //DefaultDotTimer DotTimers = new DotTimers.DefaultDotTimer(1,1,0.5f, remainingDotTime, isInteractingWithDotSpell);
        //DotTimers.Update();
        //dotTimer.Update();

        if (IsOwner)
        {
            health.Value = 100f;
            isAlive = true;
        }

  
    }



    // Method that sets clients' health
    [ServerRpc(RequireOwnership = false)]
    void SetStartingHealthServerRpc(float maxHealth, ServerRpcParams serverRpcParams = default)
    {
        health.Value = maxHealth;
        _healthBar.SetMaxHealth(_maxHealth);
    }



    public void ShieldIsActive(bool active)
    {
        ShieldIsActiveServerRpc();
        Debug.Log("Sphere shield is active: " + localSphereShieldActive.Value);
    }



    // Here the shield is marked as active
    [ServerRpc(RequireOwnership = false)]
    void ShieldIsActiveServerRpc()
    {
        localSphereShieldActive.Value = true;
    }



    void Update()
    {
        // Debug.Log("TIME " + Time.time);
        if (IsClient && IsOwner)
        {
            if (isAlive && isIncapacitated == false)
            {
                playerMovement.Movement();
                playerMovement.Dodge();
                //playerMovement.WASDmovement();
            }
        }

        if (isDot && g_dotDamage != 0)
        {
            DotSpellDamage();
        }

        if (isIncapacitated)
        {
            SpellInflictedIncapacitation();
        }




        foreach (var kvp in dotTimers)
        {
            DefaultDotTimer dotTimer = kvp.Value;

            // These two variables are controlled through on trigger enter and exit
            if (dotTimer.IsInteractingWithSpell || dotTimer.IsDotPersistent)
            {
                AoeSpellDotDamage();
            }
        }

        if (isRemovingDotSpellsFromList)
        {
            RemoveDotSpellFromList();
        }

    }



    // The problem with this one is that the Beam owner is not correct
    // Need to debug
    // Update: It's been a while not sure if this is still an issue
    // Update 2: It doesn't look like this is being used
    void BeamHitPlayer(bool isPlayerHitByDot, ulong client, string location)
    {
        if (OwnerClientId == client) return;
        
        isDot = isPlayerHitByDot;
        
    }

    

    void ShieldStatus(ulong client, NetworkObjectReference obj, NetworkBehaviour spellNetBehavior, bool shieldStatus)
    {
        // This bool tells us that a new local sphere shield has spawned
        if (shieldStatus)
        {
            // If a shield already exists on the local player
            // destroy it and replace it with the new one.
            // Otherwise, just save the new shield to a variable for the mentioned logic
            if (currentSphereShield != null)
            {
                currentSphereShield.GetComponent<SphereShield>().DestroyShield();
                currentSphereShield = obj;

            }
            else
            {
                currentSphereShield = obj;
            }
        }
        else
        {
            // If the shield was destroyed or has expired,
            //clear the local variable saving its reference
            currentSphereShield = null;
        }

        // !* Not sure this needs to be a serverRPC - To check
        ShieldStatusServerRpc(shieldStatus);

        // This is only being used in tandem with handling regular barrier damage
        isShieldActive = shieldStatus;
    }

    [ServerRpc(RequireOwnership = false)]
    void ShieldStatusServerRpc(bool shieldStatus)
    {
        localSphereShieldActive.Value = shieldStatus;
    }


    // Event that handles turning the mist overlay off in case the player was OnTriggerEnter when the spell disappears
    void MistStatus(ulong client, NetworkObjectReference obj, NetworkBehaviour spellNetBehavior, bool beamStatus)
    {
        mistOverlayObject.SetActive(false);
    }




    /* <summary> 
    * 
    * <outline>
    * Aoe Dot Damage is applied using a dictionary of aoe spells which is aggregated dynamicaly, 
    * after the player has come into contact with said persistable dot spell,
    * using a foreach loop in Update(). (i.e) Foreach dot spell that the player has come in contact with, either apply direct
    * damage (if he is still overlapping with it), or indirect damage if the player is no longer in contact with the spell.
    * </outline>
    * 
    * When a fire aoe is destroyed or its lifetime ends, th following event queues the specific spell for deletion by adding it to 
    * a list 'keysToRemove' (to rename) which is thereafter deleted in the AoeDotSpellDamage() method 
    * which is activated through Update(). The deletion occurs only after the player is no longer 
    * receiving persistentDotDamage (indirect damage sustained from being affected from fire for example).
    * 
    * </summary> */
    void FireAoeStatus(ulong client, NetworkObjectReference obj, NetworkBehaviour spellNetBehavior, bool beamStatus, NetworkObject netObj)
    {
        int objectId = netObj.GetComponent<NetworkObject>().GetInstanceID();

        // (!!*) This is causing an error when the fire aoe spell is not interacted with, no idea why.
        // Save the DotTimer of the spell that was destroyed to a variable
        DefaultDotTimer dotTimerToModify = dotTimers[objectId];

        // Switch its boolean values that are used to apply damage in the method used in the update method in this script
        dotTimerToModify.IsInteractingWithSpell = false;
        dotTimerToModify.IsDotPersistent = true;

        // Queue it for removal - Is removed after the persistant damage timer is complete
        keysToRemove.Add(objectId);
    }



    // Apply Damage over time when enemy beam is connected to player
    //**To rename to BeamDotDamage (As there already is another method handling
    //the Dot of Aoe for example)
    void DotSpellDamage(float extraTime = 0)
    {
        float currentTime = Time.time;

        // Check if local player    
        if (OwnerClientId == g_localId)
        {
            // If the enemy beam that was/is in contact with the player no longer exists
            // stop applying damage
            if (!enemyBeam)
            {
                // This bool handles entry for beam dot damage application in Update()
                isDot = false;
            }

            // (!) Interval has to become a parameter passed value
            if (currentTime - lastTime >= interval)
            {
                //Debug.Log("DAMAGE APPLIED: " + g_dotDamage);
                health.Value -= g_dotDamage;
                //_healthBar.SetHealth(health.Value);
                lastTime = currentTime;
            }
        }    
    }



    void AoeSpellDotDamage(float extraTime = 0)
    {
        float currentTime = Time.time;

 
        if (dotTimers != null && dotTimers.Count != 0)
        {
            if (OwnerClientId == g_localId)
            {

                if (dotTimers != null)
                {
                    foreach (var kvp in dotTimers)
                    {
                        DefaultDotTimer dotTimer = kvp.Value;
                        //--Debug.LogFormat($"<color=orange>dotTimer: {kvp}</color>");

                        // The values of the condition used below is set through OnTriggerEnter/Exit
                        if (dotTimer.IsDotPersistent)
                        {
                            //Debug.LogFormat($"<color=orange>PersistentDotDamage</color>");
                            dotTimer.PersistentDotDamage();
                        }

                        // The values of the condition used below is set through OnTriggerEnter/Exit
                        if (dotTimer.IsInteractingWithSpell)
                        {
                            //--Debug.LogFormat($"<color=green>DirectDotDamage</color>");
                            dotTimer.DirectDotDamage();
                        }

                        // Only after the spell is no longer applying either direct or indirect damage
                        // to the player is the associated dot timer to it removed from the main Dictionary (dotTimers)
                        if (dotTimer.IsInteractingWithSpell == false && dotTimer.IsDotPersistent == false)
                        {
                            // (*) Below bools are no longer being used - To confirm and delete (*)
                            isInteractingWithDotSpell = false;
                            isDotPersistent = false;

                            // This activates removing the inactive persistent dot spells from the persistent dot spells list
                            isRemovingDotSpellsFromList = true;
    
                            //--Debug.LogFormat($"<color=green>timerComplete {dotTimer}</color>");

                        }
                    }
                }
            }
        }
    }


    /* <summary>
     * Before the dotTimers that are no longer active, keysToRemove list is first saved to a second list
     * The keysToRemove is used for iteration to remove the unused dotTimers, the other List<> is used
     * to remove the key-value-pairs that have been removed from the keysToRemove<>
     * the other list is then cleared.
     * </summary>
     */
    void RemoveDotSpellFromList()
    {
        List<int> removeKeyReferencesFromReferenceList = new List<int>();

        foreach (var key in keysToRemove)
        {
            removeKeyReferencesFromReferenceList.Add(key);
        }

        foreach (var key in keysToRemove)
        {
            //Debug.LogFormat($"<color=orange>{key}</color>");
            //Debug.LogFormat($"<color=orange>{dotTimers[key]}</color>");

            if (dotTimers.ContainsKey(key))
            {
                //Debug.LogFormat($"<color=orange>{dotTimers[key]}</color>");
                dotTimers.Remove(key); // Remove the key-value pair from the dictionary
                
            }
        }

        foreach (var item in removeKeyReferencesFromReferenceList)
        {
            if (keysToRemove.Contains(item))
            {
                keysToRemove.Remove(item); // Remove the key from the list
            }
        }

        removeKeyReferencesFromReferenceList.Clear();
        isRemovingDotSpellsFromList = false;
    }



    #region Parry - Letter Activation
    // This is for the old Parry system - Currently deprecated - DO NOT DELETE
    // Request the server to actiate letter on client's Ui
    [ServerRpc(RequireOwnership = false)]
    public void ActivateLetterServerRpc(string letterName, bool isActive, ServerRpcParams serverRpcParams = default)
    {
        if (!IsServer) return;

        // Get the Id of the Net.Obj. making the req.
        var clientId = serverRpcParams.Receive.SenderClientId;

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[]{clientId}
            }
        };

        ActivateLetterOnClientRpc(letterName, isActive, clientRpcParams);
    }


    // This is for the old Parry system - Currently deprecated - DO NOT DELETE
    // After getting approval from server: Activate that letter on the client
    [ClientRpc]
    void ActivateLetterOnClientRpc(string letterName, bool isActive, ClientRpcParams clientRpcParams = default)
    {
        if (IsOwner && !IsServer)
            {
                LetterActive(letterName, isActive);
            }
    }


    // This is for the old Parry system - Currently deprecated - DO NOT DELETE
    // If Host: Activate the letter on the Ui for the host
    [ClientRpc]
    public void ActivateLetterClientRpc(string letterName, bool isActive)
    {
            if (IsOwner)
            {
                LetterActive(letterName, isActive);
            }
    }


    // This is for the old Parry system - Currently deprecated - DO NOT DELETE
    void LetterActive(string letter, bool isActive)
    {
        switch (letter) {
            case "R":
                uiReferences.AllKeys["R"].SetActive(isActive);
                break;

            case "V":
                uiReferences.AllKeys["V"].SetActive(isActive);      
                break;

            case "Y":
                uiReferences.AllKeys["Y"].SetActive(isActive);      
                break;

            case "N":
                uiReferences.AllKeys["N"].SetActive(isActive);      
                break;
                
            default:
                break;
        }
    }

    #endregion



    // Every time the sphere is either activated or deactivated
    //check whether the shield has just been destroyed
    //so that the damage taken by the shield is not also 
    //applied to the player (I suppose it's a race issue)
    //**Should be renamed to SphereDamageTakenBehavior
    //**To migrate?
    void DamageBehavior(bool previous, bool current)
    {
        //Debug.Log("SSNV: " + localSphereShieldActive.Value);
        //Debug.Log("DB sphereShieldJustDestroyed: " + sphereShieldJustDestroyed);
        if (previous == true && current == false)
        {
            sphereShieldJustDestroyed = true;
            return;
        } 
    }



    void HandleDamageTaken(int objectId, float objectDamage, ulong localId, bool dot = false)
    {
        // This check is made so that the bolt damage is not compounded (computed twice)
        if (!spellObjects.Contains(objectId))
        {
            // Would need to remove the id after the collision occurs
            // The spell object's ID is used to check if the spell
            //object has or has not yet collided with the player
            spellObjects.Add(objectId);

            // (!!) I don't think this is being used - To check
            for (int i = 0; i < spellObjects.Count; i++)
            {
                int item = spellObjects[i];
                //Debug.Log(item);
            }

            //Debug.Log("Owner: " + OwnerClientId + " - Local: " + localId);

            if (OwnerClientId == localId && dot == false)
            {
                // Debug.Log("Sphere Active: " + localSphereShieldActive.Value);
                // Debug.Log("DAMAGE APPLIED: " + objectDamage);

                health.Value -= objectDamage;
                //_healthBar.SetHealth(health.Value);
            }

        } 
    }



    // This method was necessary due to the fact that the player was triggering
    // damage application multiple times. 
    // <summary> This method makes it so damage is applied only once
    void HandleBarrierDamageTaken(int objectId, float objectDamage, ulong localId, bool dot = false)
    {
        // If player has entered the barrier spell add the it to a dict
        // and set its value pair to true (Player Can Be Damaged)
        if (!barrierObjects.ContainsKey(objectId))
        {
            barrierObjects.Add(objectId, true);
        } 

           
        // <summary> Ensures that only the player that entered
        // the barrier is taking damage
        if (OwnerClientId == localId && dot == false)
        {
            // If player can be damaged: Apply damage
            // else do nothing
            if (barrierObjects[objectId] == true)
            {
                // Apply damage to player
                health.Value -= objectDamage;
                //_healthBar.SetHealth(health.Value);

                // Prevents player from taking damage again
                // reactivated when player exits the trigger
                barrierObjects[objectId] = false;
            }
            else
            {
                return;
            }
        } else if (OwnerClientId == localId && dot == true)
        {
            // Handle barrier fire dot damage
        }
        

        
    }



    void CheckIfSpellHitShield(bool hasHitShield)
    {

    }



    public void SpellInflictedIncapacitation()
    {
        // movementSlowTime = slowTimeAmount;

        if (incapacitatedDuration > 0f && isIncapacitated == true)
        {
            // Debug.Log("SLOWING: " + movementSlowTime);
            incapacitatedDuration -= Time.deltaTime;
            Debug.Log("Time left: " + incapacitatedDuration.ToString("F1"));
            
        }
        else
        {
            Debug.Log("incapacitation Over");
            incapacitatedDuration = 0;
            isIncapacitated = false;
        }
    }



    void OnTriggerEnter(Collider other)
    {
        Debug.LogFormat($"Message: <color=blue>{other.name} </color>");
        if (other != null && IsOwner)
        {
            if (other.name == "Floor")
            {
                return;
            }

            if (other.name.Contains("Mist"))
            {
                mistOverlayObject.SetActive(true);
            }

            if (other.name.Contains("Bolt"))
            {
                //Debug.Log("IS BOLT");
                int objectId = other.GetComponent<NetworkObject>().GetInstanceID();
                ulong localId = NetworkManager.Singleton.LocalClientId;
                float objDamage = other.GetComponent<Bolt>().damage;

                bool hasHitShield = other.GetComponent<Bolt>().hasHitShield;

                if (hasHitShield)
                {
                    return;
                } else {
                    HandleDamageTaken(objectId, objDamage, localId);
                    other.GetComponent<Bolt>().DestroyBolt();
                }
            } 
            else if (other.name.Contains("Aoe"))
            {
                int objectId = other.GetComponent<NetworkObject>().GetInstanceID();
                ulong localId = NetworkManager.Singleton.LocalClientId;

                string spell = other.name.Replace("(Clone)", "");

                // ** The name of the Aoe will be different depending on the Aoe
                // **and will therefore require to check what type of aoe it is. 
                // ** One way would be yet another switch case maybe.

                switch (spell)
                {
                    case "Arcane Aoe":
                        float arcaneAoeDamage = other.GetComponent<ArcaneAoe>().damage;
                        // bool hasHitShield = other.GetComponent<ArcaneAoe>().hasHitShield;

                        // CheckIfSpellHitShield(hasHitShield);

                        if (other.GetComponent<ArcaneAoe>().hasHitShield)
                        {
                            return;
                        }
                        else
                        {
                            playerMovement.MovementSlowAmount = other.GetComponent<ArcaneAoe>().movementSlowAmount;
                            playerMovement.MovementSlowTime = other.GetComponent<ArcaneAoe>().slowTime;

                            playerMovement.IsSlowed = true;
                            HandleDamageTaken(objectId, arcaneAoeDamage, localId);
                        }

                        return;

                    case "Earth Aoe":
                        float earthAoeDamage = other.GetComponent<EarthAoe>().damage;
                        //bool hasHitShield = other.GetComponent<ArcaneAoe>().hasHitShield;

                        //CheckIfSpellHitShield(hasHitShield);

                        

                        if (other.GetComponent<EarthAoe>().hasHitShield)
                        {
                            return;
                        }
                        else
                        {
                            incapacitatedDuration = other.GetComponent<EarthAoe>().IncapacitatedDuration;

                            isIncapacitated = true;

                            // (**) Handle incapacitation animation here

                            // SpellInflictedIncapacitation();
                            HandleDamageTaken(objectId, earthAoeDamage, localId);
                        }

                        return;

                    case "Fire Aoe":
                        float applyDamageAtInterval = other.GetComponent<FireAoe>().ApplyDamageAtInterval;
                        float fireAoeDamage = other.GetComponent<FireAoe>().ApplyDamageAtInterval;
                        float startTime = Time.time;
                        remainingDotTime = other.GetComponent<FireAoe>().DotPersistanceTime;
                        PlayerBehavior playerBehaviorScript = this.gameObject.GetComponent<PlayerBehavior>();


                        // dotTimers[objectId].IsInteractingWithSpell
                        //if (dotTimers[objectId].IsInteractingWithSpell == false)
                        //{
                        //    Debug.LogFormat($"<color=purple>FALSE</color>");
                        //}
                        // Debug.LogFormat($"<color=purple>DOT TIMER BOOL {dotTimers[objectId].IsInteractingWithSpell}</color>");

                        if (other.GetComponent<FireAoe>().hasHitShield)
                        {
                            return;
                        }
                        else
                        {
                            //Debug.LogFormat($"<color=purple>DOT TIMER BOOL {dotTimers[objectId].IsInteractingWithSpell}</color>");

                            if (isInteractingWithDotSpell == false || dotTimers[objectId].IsInteractingWithSpell == false)
                            {
                                //Debug.LogFormat($"<color=blue>FIRE AOE</color>");
                                isInteractingWithDotSpell = true;

                                dotTimers[objectId] = new DefaultDotTimer(applyDamageAtInterval, fireAoeDamage, 5.0f, remainingDotTime, isInteractingWithDotSpell, playerBehaviorScript, _healthBar, startTime);
                                
                                DefaultDotTimer dotTimerToModify = dotTimers[objectId];

                                // The following bools are used for dot damage application
                                // interaction is when the player is interacting with the spell
                                // dot persistence is the damage taken when the effects persists after interaction is broken
                                dotTimerToModify.IsInteractingWithSpell = true;
                                dotTimerToModify.IsDotPersistent = false;

                                // (*) Don't think these two are used anymore. Can probably delete - To confirm
                                isInteractingWithDotSpell = true;
                                isDotPersistent = false;
                            } else
                            {
                                return;
                            }

                        }

                        return;

                    default:
                        return;

                }

                
            } 
            else if (other.name.Contains("BeamObject"))
            {

                float objDamage = other.GetComponent<Beam>().Damage();
                GameObject beamObj = other.gameObject;
                enemyBeam = beamObj;

                bool hasHitShield = other.GetComponent<Beam>().hasHitShield;

                // Still need to implement this
                // (**) Use local isShieldActive bool to make this first check
                if (hasHitShield)
                {
                    Debug.LogFormat($"Message: <color=blue>BEAM HIT SHIELD </color>");
                }

                isDot = true;

                g_dotDamage = objDamage;

            } else if (other.name.Contains("Barrier") || other.name.Contains("Long Forward Arcane Barrier"))
            {
                // Get player and object Ids
                int objectId = other.GetComponentInParent<NetworkObject>().GetInstanceID();
                ulong localId = NetworkManager.Singleton.LocalClientId;

                // The spells GameObject's 3D model contains the word Model in its name - This removes it
                string spell = other.name.Replace(" Model", "");

                //Debug.LogFormat($"Message: <color=blue>BARRIER HIT</color>");

                // (**) Implement logic when player has shield active
                // (**) Use local isShieldActive bool to make this first check

                switch (spell)
                {
                    case "Arcane Barrier":

                        // (**) If needed: Must implement removing the objectId after the associated gameObject is despawned (use event?)
                        
                        float arcaneBarrierDamage = other.GetComponentInParent<ArcaneBarrier>().arcaneBarrierDamage.Value;

                        HandleBarrierDamageTaken(objectId, arcaneBarrierDamage, localId);

                        return;

                    case "Long Forward Arcane Barrier":
                        //Debug.LogFormat($"Message: <color=blue>BEAM HIT SHIELD </color>");
                        float arcaneLongBarrierDamage = other.GetComponentInParent<ArcaneBarrier>().arcaneBarrierDamage.Value;

                        HandleBarrierDamageTaken(objectId, arcaneLongBarrierDamage, localId);

                        return;

                    case "Fire Barrier":
                        float applyDamageAtInterval = other.GetComponentInParent<FireBarrier>().ApplyDamageAtInterval;
                        float fireAoeDamage = other.GetComponentInParent<FireBarrier>().ApplyDamageAtInterval;
                        float startTime = Time.time;
                        remainingDotTime = other.GetComponentInParent<FireBarrier>().DotPersistanceTime;
                        PlayerBehavior playerBehaviorScript = this.gameObject.GetComponent<PlayerBehavior>();


                        // dotTimers[objectId].IsInteractingWithSpell
                        //if (dotTimers[objectId].IsInteractingWithSpell == false)
                        //{
                        //    Debug.LogFormat($"<color=purple>FALSE</color>");
                        //}
                        // Debug.LogFormat($"<color=purple>DOT TIMER BOOL {dotTimers[objectId].IsInteractingWithSpell}</color>");

                        // (**) Use local isShieldActive bool to make this first check
                        if (other.GetComponentInParent<FireBarrier>().hasHitShield)
                        {
                            // Apply damage to shield
                            return;
                        }
                        else
                        {
                            //Debug.LogFormat($"<color=purple>DOT TIMER BOOL {dotTimers[objectId].IsInteractingWithSpell}</color>");

                            if (isInteractingWithDotSpell == false || dotTimers[objectId].IsInteractingWithSpell == false)
                            {
                                //Debug.LogFormat($"<color=blue>FIRE AOE</color>");
                                isInteractingWithDotSpell = true;

                                dotTimers[objectId] = new DefaultDotTimer(applyDamageAtInterval, fireAoeDamage, 5.0f, remainingDotTime, isInteractingWithDotSpell, playerBehaviorScript, _healthBar, startTime);

                                DefaultDotTimer dotTimerToModify = dotTimers[objectId];

                                // The following bools are used for dot damage application
                                // interaction is when the player is interacting with the spell
                                // dot persistence is the damage taken when the effects persists after interaction is broken
                                dotTimerToModify.IsInteractingWithSpell = true;
                                dotTimerToModify.IsDotPersistent = false;

                                // (*) Don't think these two are used anymore. Can probably delete - To confirm
                                isInteractingWithDotSpell = true;
                                isDotPersistent = false;
                            }
                            else
                            {
                                return;
                            }

                        }
                        return;

                    case "Earth Barrier":
                        return;

                    case "Water Barrier":
                        // This doesn't make sense
                        return;

                    case "Air Barrier":
                        // Air barrier
                        return;

                }
            }

            if (other.name.Contains("Stun and Damage Trigger") )
            {
                ulong localId = NetworkManager.Singleton.LocalClientId;

                int objectId = other.GetComponentInParent<NetworkObject>().GetInstanceID();

                float earthBarrierDamage = other.GetComponentInParent<EarthBarrier>().earthBarrierDamage.Value;

                
                // Not all spells are using this logic
                // For now it clearly is only the Long Earth Barrier
                if (isShieldActive)
                {
                    // This was supposed to handle applying damage to sphere shield
                    // Is not instead being handled inside the sphere shield script
                    Debug.LogFormat($"<color=purple>shield active</color>");
                    return;
                    
                }
                else
                {
                    Debug.LogFormat($"<color=purple>shield not active</color>");

                    HandleBarrierDamageTaken(objectId, earthBarrierDamage, localId);

                    incapacitatedDuration = other.GetComponentInParent<EarthBarrier>().IncapacitatedDuration;

                    // Stuns the player for the duration of the spell
                    //disabling movement
                    // ** TO IMPLEMENT: CASTING DISABLE (Prevents player from casting when stunned)
                    isIncapacitated = true;

                    //if (earthBarriers.Contains(objectId))
                    //{
                    //    return;
                    //} else
                    //{
                    //    earthBarriers.Add(objectId);
                    //    // Apply damage to player

                    //    // Stun player
                    //}

                }

            }

            // ############################################################################
            // ############################## DO NOT DELETE ###############################
            // ############################################################################
            // // REACTIVATE THE CODE BELOW FOR PARRY SYSTEM
            // //If the bolt's LOS warning interacts with the player
            // //get the letter associated with the object
            // //activate the respective letter on the player's ui
            // //Which is done in preperation for the parry window/ action
            // if (other.name.Contains("LOS Warning"))
            // {
            //     string theLetter = other.GetComponentInChildren<LOSletterGen>().associatedLetter;

            //     if (IsServer)
            //     {   
            //         // This is what SHOULD activate the letter on the HOST
            //         ActivateLetterClientRpc(theLetter, true);
            //         Debug.Log("OTEn: IS SERVER");

            //     } else {

            //         // This is what activates the letter on CLIENTs
            //         ActivateLetterServerRpc(theLetter, true);
            //         Debug.Log("OTEn: NOT SERVER");
            //     }

            // } else {

            //     return;
            // }
            // ############################################################################
            // ############################## DO NOT DELETE ###############################
            // ############################################################################
        }
    }



    // Check if the player/ target has moved out of
    //the line-of-fire of a spell to deactivate the 
    //respective letter on the ui 
    void OnTriggerExit(Collider other)
    {
        if (other != null && IsOwner)
        {
            // // REACTIVATE THE CODE BELOW FOR PARRY SYSTEM
            // if (other.name.Contains("LOS Warning"))
            // {
            //     string theLetter = other.GetComponentInChildren<LOSletterGen>().associatedLetter;

            //     if (IsServer)
            //     {   // This is what activates the letter on the HOST
            //         ActivateLetterClientRpc(theLetter, false);
            //         Debug.Log("OTEx: IS SERVER");
                    
            //     } else {
            //         // This is what activates the letter on the CLIENT
            //         ActivateLetterServerRpc(theLetter, false);
            //         Debug.Log("OTEx: NOT SERVER");
            //     }
            // }

            

            // When the beam is connecting with the player the isDot bool 
            // allows Update() entry into the dot damage application method
            if (other.name.Contains("BeamObject"))
            {
                isDot = false;
                g_dotDamage = 0;
            }

            if (other.name.Contains("Mist"))
            {
                // Deactivate screen overlay from mist spell effect
                mistOverlayObject.SetActive(false);
            }

            if (other.name.Contains("Aoe"))
            {
                int objectId = other.GetComponent<NetworkObject>().GetInstanceID();
                ulong localId = NetworkManager.Singleton.LocalClientId;

                string spell = other.name.Replace("(Clone)", "");

                switch (spell)
                {

                    case "Fire Aoe":
                        //Debug.LogFormat($"<color=orange>Fire Aoe Exited</color>");
                        //Debug.LogFormat($"<color=red>IsInteractingWithSpell: {isInteractingWithDotSpell}</color>");

                        if (other.GetComponent<FireAoe>().hasHitShield)
                        {
                            return;
                        }
                        else
                        {
                            DefaultDotTimer dotTimerToModify = dotTimers[objectId];

                            dotTimerToModify.IsInteractingWithSpell = false;

                            dotTimerToModify.IsDotPersistent = true;

                            isDotPersistent = true;

                            // (**) Here we need to make a check if there are any other spells the player
                            // is in contact with before setting the isInteracting to false
                            foreach (var existingTimers in dotTimers)
                            {
                                DefaultDotTimer dotTimer = existingTimers.Value;

                                if (dotTimer.IsInteractingWithSpell)
                                {
                                    return;
                                    
                                } else
                                {
                                    isInteractingWithDotSpell = false;
                                }
                            }
                            
                            
                        }

                        return;

                    default:
                        return;

                }
            }



            if (other.name.Contains("Barrier"))
            {
                hasEntered = true;

                string spell = other.name.Replace(" Model", "");

                //int objectId = other.GetComponent<NetworkObject>().GetInstanceID();
                //ulong localId = NetworkManager.Singleton.LocalClientId;

                switch (spell)
                {
                    

                    case "Arcane Barrier":

                        //Debug.LogFormat($"<color=green>{entryCount}</color>");

                        int objectId = other.GetComponentInParent<NetworkObject>().GetInstanceID();
                        ulong localId = NetworkManager.Singleton.LocalClientId;

                        // (**) If needed: Must implement removing the objectId after the associated gameObject is despawned (use event?)

                        if (barrierObjects.ContainsKey(objectId))
                        {
                            if (barrierObjects[objectId] == false)
                            {
                                barrierObjects[objectId] = true;
                            }
                            else
                            {
                                return;
                            }
                        }

                        float arcaneBarrierDamage = other.GetComponentInParent<ArcaneBarrier>().arcaneBarrierDamage.Value;

                        //HandleBarrierDamageTaken(objectId, arcaneBarrierDamage, localId);

                        return;


                    case "Long Forward Arcane Barrier":

                        //Debug.LogFormat($"<color=green>{entryCount}</color>");

                        int longBarrierObjectId = other.GetComponentInParent<NetworkObject>().GetInstanceID();
                        // ulong localId = NetworkManager.Singleton.LocalClientId;

                        // (**) If needed: Must implement removing the objectId after the associated gameObject is despawned (use event?)

                        if (barrierObjects.ContainsKey(longBarrierObjectId))
                        {
                            if (barrierObjects[longBarrierObjectId] == false)
                            {
                                barrierObjects[longBarrierObjectId] = true;
                            }
                            else
                            {
                                return;
                            }
                        }

                        // float arcaneBarrierDamage = other.GetComponentInParent<ArcaneBarrier>().arcaneBarrierDamage.Value;

                        //HandleBarrierDamageTaken(objectId, arcaneBarrierDamage, localId);

                        return;

                    case "Fire Barrier":

                        int fireBarrierObjectId = other.GetComponentInParent<NetworkObject>().GetInstanceID();
                        //ulong localId = NetworkManager.Singleton.LocalClientId;

                        if (other.GetComponentInParent<FireBarrier>().hasHitShield)
                        {
                            return;
                        }
                        else
                        {
                            DefaultDotTimer dotTimerToModify = dotTimers[fireBarrierObjectId];

                            dotTimerToModify.IsInteractingWithSpell = false;

                            dotTimerToModify.IsDotPersistent = true;

                            isDotPersistent = true;

                            // (**) Here we need to make a check if there are any other spells the player
                            // is in contact with before setting the isInteracting to false
                            foreach (var existingTimers in dotTimers)
                            {
                                DefaultDotTimer dotTimer = existingTimers.Value;

                                if (dotTimer.IsInteractingWithSpell)
                                {
                                    return;

                                }
                                else
                                {
                                    isInteractingWithDotSpell = false;
                                }
                            }


                        }
                        return;

                    case "Earth Barrier":
                        return;

                    case "Water Barrier":
                        // Water barrier doesn't make sense - figure out?
                        return;

                    case "Air Barrier":
                        // Air barrier
                        return;

                }
            }

        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name.Contains("Floor"))
        {
            isOnFloor = true;
            //Debug.LogFormat($"<color=purple>Floor collision</color>");
        }
    }


    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.name.Contains("Floor"))
        {
            isOnFloor = false;
            // Debug.LogFormat($"<color=purple>Floor collision exit</color>");
        }
    }
} 
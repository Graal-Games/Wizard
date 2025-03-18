using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;
using System.Linq;
using Unity.IO.LowLevel.Unsafe;
using static ActionSquareState;

// struct MyComplexStruct : INetworkSerializable
// {
//     public NetworkVariable<NetworkObject> beamObj = new NetworkVariable<NetworkObject>(default,
//         NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

//     // INetworkSerializable
//     public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
//     {
//         serializer.SerializeValue(ref beamObj);
//         serializer.SerializeValue(ref Rotation);
//     }
//     // ~INetworkSerializable
// }
public class PlayerInput : NetworkBehaviour
{
    // Should this be a network behavior?
    [SerializeField]  GameObject dischargeSpellsCastPoint;
    [SerializeField]  GameObject localSpellsCastPoint;
    private DischargedSpellcast dischargedSpellcast;
    [SerializeField] DRTierObjectColorManager dRTierObjectColorManager;


    //public TextMeshProUGUI playerInputText;

    // Handles letters input
    private int[] values;
    private bool[] keys;

    public GameObject gBackground;
    //PlayerBehavior playerBehavior;
    RawImage castingSquare;

    //private float startTime;
    string keyPressed;


    public bool actionBufferActiveGate = false;
    //bool spellHasBeenCast;
    [SerializeField] KeyUi mainActionSquare; 

    // Cast window values
    public float canCastEntry = 1.2f;
    public float canCastExit = 1.6f;

    public float actionBufferCastWindowBaseTimerTime = 0f;

    public List<string> letters = new List<string>();

     public NetworkVariable<bool> spellSequenceValidator = new NetworkVariable<bool>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    bool isCastPrevented = false;


    [Header("Diminishing Returns Variables")]
    public bool isBeamDRActive;
    public bool isAoeDRActive;
    public bool isShieldDRActive;
    public bool isCharmDRActive;
    public bool isCastingDRSequence;
    public bool isBarrierDRActive;


    [Header("Beam")]
    public bool channeledCastingActive = false;

    [Header("Scripts")]
    DiminishingReturn diminishingReturns;
    Spellcasting spellcasting;
    DRRouter dRRouter;
    DRActivationLogic dRActivationLogic;
    Beam beamScript;
    SphereShield sphereShieldScript;
    NetworkBehaviour spellToBeChanneled;
    LocalSpells localSpells;
    PlayerBehavior playerBehavior;
    ChannelingSequencer channelingSequencer;
    [SerializeField] ChanneledCasting channeledCastingScript;
    UiReferences uiReferencesScript;
    SpellKeys spellKeys;
    ActiveTimer activeTimer;
    PlayerMovement playerMovement;



    public bool isBeamSpellAlive = false;

    [Header("Audio")]
    [SerializeField] private AudioClip castSuccessful_SFX;
    [SerializeField] private AudioClip castUnsuccessful_SFX;
    [SerializeField] private AudioClip castMode_SFX;

    //public delegate void ScriptToBeChanneled(ulong clientId, NetworkObjectReference spellObj, NetworkBehaviour spellNetBehavior, bool status);
    //public static event ScriptToBeChanneled beamExists;

    public bool IsCastPrevented { get => isCastPrevented; set => isCastPrevented = value; }

    bool isActionSquareAnimationIdle;
    // Get the values of all the keycodes and save them to an array.
    // If values.Length is not 0 add them to the keys array
    void Awake() 
    {

        values = (int[])System.Enum.GetValues(typeof(KeyCode));
        keys = new bool[values.Length];

        playerBehavior = GetComponentInParent<PlayerBehavior>();
        uiReferencesScript = GetComponentInParent<UiReferences>();
        spellcasting = GetComponentInParent<Spellcasting>();

        dischargedSpellcast = dischargeSpellsCastPoint.GetComponent<DischargedSpellcast>();
        localSpells = localSpellsCastPoint.GetComponent<LocalSpells>();
        
        mainActionSquare = GetComponentInChildren<KeyUi>();
        spellKeys = GetComponent<SpellKeys>();
        diminishingReturns = GetComponentInParent<DiminishingReturn>();

        dRRouter = GetComponentInParent<DRRouter>();

        dRActivationLogic = GetComponentInParent<DRActivationLogic>();

        activeTimer = GetComponentInChildren<ActiveTimer>();

        playerMovement = GetComponentInParent<PlayerMovement>();

        ActionSquareState.actionSquareAnimationState += ActionSquareAnimationState;
    }

    void ActionSquareAnimationState(bool state)
    {
        isActionSquareAnimationIdle = state;

        Debug.LogFormat($"<color=green>isActionSquareAnimationIdle: {isActionSquareAnimationIdle}</color>");

        return;
    }

    void OnEnable()
    {
        // Subscribe to the spawn beam spell event
        // whenever its value(s) change call the BeamStatus method
        Beam.beamExists += BeamStatus;
        SphereShield.shieldExists += ShieldStatus;

    }



    void Start()
    {
        castingSquare = gBackground.GetComponent<RawImage>();
        castingSquare.gameObject.SetActive(false);

        channelingSequencer = GetComponent<ChannelingSequencer>();

    }



    void Update() {

        if (!IsLocalPlayer) return;

        if (playerBehavior.isAlive)
        {

            //// Displays the spell icons on the Ui
            //if (Input.GetKeyDown(KeyCode.LeftAlt))
            //{
            //    uiReferencesScript.ActivateSpellIcons();

            //} else if (Input.GetKeyUp(KeyCode.LeftAlt))
            //{
            //    uiReferencesScript.DeactivateSpellIcons();

            //}


            // I think: This validates --which-- button is pressed
            //by its respective keycode
            // Note: This doesn't look like it's being used
            if (Input.anyKeyDown)
            {
                for(int i = 0; i < values.Length; i++) 
                {
                    keys[i] = Input.GetKey((KeyCode)values[i]);
                }
            }


            if (actionBufferActiveGate == true)
            {
                InstantSpellsBufferBehavior();

                // This migration needs some work
                //activeTimer.InstantSpellsBufferBehavior2();

                // activeTimer.InstantSpellsBufferBehavior2(preCastWindowWarning);


            }


            
            if (Input.anyKeyDown && Input.inputString.Length == 1)
            {
                keyPressed = Input.inputString;

                // If the player presses w, a, s, d ignore them, before storing the cast sequence letters
                if (keyPressed != "w" && keyPressed != "a" && keyPressed != "s" && keyPressed != "d")
                {
                    if (!isCastPrevented)
                    {
                        letters.Add(keyPressed.ToUpper());
                        spellSequenceValidator.Value = !spellSequenceValidator.Value;
                        //letters2.Value.Add(keyPressed.ToUpper());
                        //DO NOT DELETE - Track player input in the main square on the Ui
                        //playerInputText.text = keyPressed.ToString().ToUpper();

                        // This filters the player input and handles the logic and casting of casting spells
                        //>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> This is the old spellcasting logic method call
                        //spellcasting.SpellComboRouter(letters.Count, letters);
                        spellcasting.Spellcast(letters);
                    }
                    
                }
            }

            // handles input when DR sequence is active
            if (isCastingDRSequence)
            {
                diminishingReturns.DRLock();

            }
            
            // Handles entry into the input functionality for channeled spell(s)
            if (channeledCastingActive && !isBeamSpellAlive)
            {
                // This is an extra screening layer ?
                if ( spellToBeChanneled.name.Contains("SphereShield") )
                {
                    // the script of the spell being cast is saved to a global variable
                    // and is used in a HandleInput method call, where the life of the spell is
                    // extended through player's input
                    sphereShieldScript = spellToBeChanneled.GetComponent<SphereShield>();
                    ChanneledCasting();
                } else
                {
                    beamScript = spellToBeChanneled.GetComponent<Beam>();
                    ChanneledCasting();
                }
                
                
            } else if (!channeledCastingActive && isBeamSpellAlive) 
            {
                // Debug.LogFormat($"<color=purple>DeActivated</color>");
                StopChanneledCasting();
            
            }
        }
    }


    // This is the base method that handles Channeled spell-casting 
    //(currently only handles the beam spell)
    // Script Thread: this > ChanneledCasting > Beam
    // ** Make this method also handle the channeles casting for
    //other spells
    // >> To make the regular beam a timed cast, and the charm beam channeled
    // >> To add Sphere shield to the same spell category and allow it to function in a similar fashion
    void ChanneledCasting()
    {
        string keyPressed = Input.inputString.ToUpper();

        if (keyPressed != null 
         && keyPressed != "")
        {
            if (beamScript != null)
            {
                channeledCastingScript.HandleInput(keyPressed, beamScript);

            } else if (sphereShieldScript != null)
            {
                channeledCastingScript.HandleInput(keyPressed, null, sphereShieldScript);
            }
            
        }
    }



    public void StopChanneledCasting()
    {
        // SetActive all go to false
        channelingSequencer.StopChanneledSequence();

        // Debug.LogFormat($"<color=green>DeActivated</color>");

        channeledCastingScript.DeactivateTierLetters();
        isBeamSpellAlive = false;

    }


    // ** This is to be migrated to its own script
    // ** Clean-up
    void InstantSpellsBufferBehavior()
    {
        // Once the cast square is shown, check
        // if the ability to cast has reached the time limit
        // and reset the timer and deactivate the square.

        actionBufferCastWindowBaseTimerTime += Time.deltaTime;

        if (actionBufferCastWindowBaseTimerTime >= canCastEntry 
         && actionBufferCastWindowBaseTimerTime <= canCastExit 
         && actionBufferActiveGate == true)
        {
            castingSquare.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2 (110, 110);
            dischargedSpellcast.StopAoePlacement();

        } else if (actionBufferCastWindowBaseTimerTime > 1.6f){

            dischargedSpellcast.StopAoePlacement();

            spellcasting.ExitCastMode();

            // This prevents the player from having to reintroduce DR sequence to unlock the AoE spell cat.
            //isAoeDRActive = false;
            SoundManager.Instance.PlayCastUnsuccessful(castUnsuccessful_SFX);
            ResetTimerSquareAndLetterList();
        } else {
            return;
        }
    }



    public void ResetTimerSquareAndLetterList()
    {
        Debug.LogFormat($"<color=green>5555 ResetTimerSquareAndLetterList Func Beg</color>");
        actionBufferActiveGate = false;
        actionBufferCastWindowBaseTimerTime = 0;
        //keyUi.castSquareBuffer.SetTrigger("hasEnded");

        if (!isActionSquareAnimationIdle)
        {
            mainActionSquare.StopActionBufferSquareAnimation();
        }

        //castingSquare.gameObject.SetActive(false);
        //letters.Clear();

        // Is this correct?
        castingSquare.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2 (100, 100);
        Debug.LogFormat($"<color=red>5555 ResetTimerSquareAndLetterList Func end</color>");
    }


    // ** To migrate to spellcasting
    void CastCharmSpell( string charmSpellToBeCast)
    {
        if (letters[2] == "P" && letters[3] == "I")
        {
            Debug.LogFormat($"<color=orange>Charm cast</color>");
            dischargedSpellcast.ConfirmPlacement(charmSpellToBeCast);
            dRRouter.CharmCount++;

            dRTierObjectColorManager.SetCharmDRObjectTierColor("tier" + dRRouter.CharmCount.ToString());

            return;
        }
    }

    public void CastLongBarrier(string spell)
    {
        switch (spell)
        {
            case "U": //water
                dischargedSpellcast.ConfirmPlacement("Water Long Barrier");
                SoundManager.Instance.PlayCastSuccessful(castSuccessful_SFX);
                break;
            case "I": //earth
                dischargedSpellcast.ConfirmPlacement("Earth Long Barrier");
                SoundManager.Instance.PlayCastSuccessful(castSuccessful_SFX);
                break;
            case "O": //air
                dischargedSpellcast.ConfirmPlacement("Air Long Barrier");
                SoundManager.Instance.PlayCastSuccessful(castSuccessful_SFX);
                break;
            case "P": //fire
                dischargedSpellcast.ConfirmPlacement("Fire Long Barrier");
                SoundManager.Instance.PlayCastSuccessful(castSuccessful_SFX);
                break;

        }

    }

    // TO MIGRATE THIS TO: Spellcasting
    public void CastSpell(string spell)
    {
        switch (spell){
            case "R":

                if (letters[1] == spellKeys.Charm
                && letters[2] == spellKeys.BaseSpells("F")
                && letters[3] == spellKeys.BaseSpells("Y"))
                {
                    string element = letters[4];

                    switch(element)
                    {
                        case "U":
                            dischargedSpellcast.ConfirmPlacement("earth barrier");
                            SoundManager.Instance.PlayCastSuccessful(castSuccessful_SFX);

                            Debug.LogFormat($"<color=green>CHARM ELEMENT: EARTH</color>");
                            break;

                        case "I":
                            SoundManager.Instance.PlayCastUnsuccessful(castUnsuccessful_SFX);
                            Debug.LogFormat($"<color=green>No spell</color>");
                            break;

                        case "O":
                            SoundManager.Instance.PlayCastUnsuccessful(castUnsuccessful_SFX);
                            Debug.LogFormat($"<color=green>no spell</color>");
                            break;

                        case "P":
                            dischargedSpellcast.ConfirmPlacement("fire barrier");
                            SoundManager.Instance.PlayCastSuccessful(castSuccessful_SFX);
                            Debug.LogFormat($"<color=green>CHARM ELEMENT: FIRE</color>");
                            break;
                    }
                }
                return;
            case "Arcane Long Barrier":
                // This should increment charm count for DR
                dischargedSpellcast.ConfirmPlacement("Arcane Long Barrier");
                SoundManager.Instance.PlayCastSuccessful(castSuccessful_SFX);
                return;

            case "mist":
                CastCharmSpell("mist");

                SoundManager.Instance.PlayCastSuccessful(castSuccessful_SFX);

                return;

            case "T":
                // THIS IS NOT BEING ACCESSED
                //localSpells.CastShield();
                //SoundManager.Instance.PlaySound(castMode_SFX);
                ////dRActivationLogic.DRActivationGate("Shield");
                //dRActivationLogic.IsSphereCastInterrupted = false;
                //dRRouter.ShieldCount++;
                //Debug.LogFormat($"<color=blue>SHIELD SHIELD{dRRouter.ShieldCount}</color>");
                break;

            case "Y":

                if (letters[2] == spellKeys.Elements("fire"))
                {
                    dRActivationLogic.DRActivationGate("Barrier", "Fire");

                } else if (letters[2] == spellKeys.Elements("earth"))
                {
                    dRActivationLogic.DRActivationGate("Barrier", "Earth");
                }
                else
                {
                    dRActivationLogic.DRActivationGate("Barrier", "Arcane");
                }
                

                return;

            case "Air Aoe":
            case "Earth Aoe":
            case "Water Aoe":
            case "Fire Aoe":
            case "Arcane Aoe":
                dischargedSpellcast.ConfirmPlacement(spell);
                SoundManager.Instance.PlayCastSuccessful(castSuccessful_SFX);

                dRTierObjectColorManager.SetAoeDRObjectTierColor("tier" + dRRouter.AoeCount.ToString());

                dRRouter.AoeCount++;
                break;

            case "F":
                
                if (letters[2] == spellKeys.BeginCast)
                {
                    //Debug.LogFormat($"<color=red>Casting ARCANE AOE</color>");
                    dischargedSpellcast.ConfirmPlacement("arcane");
                    SoundManager.Instance.PlayCastSuccessful(castSuccessful_SFX);

                    dRTierObjectColorManager.SetAoeDRObjectTierColor("tier" + dRRouter.AoeCount.ToString());

                } else if (letters[2] == spellKeys.Elements("earth") )
                {
                    //Debug.LogFormat($"<color=red>Casting earth AOE</color>");
                    dischargedSpellcast.ConfirmPlacement("earth");
                    SoundManager.Instance.PlayCastSuccessful(castSuccessful_SFX);

                    dRTierObjectColorManager.SetAoeDRObjectTierColor("tier" + dRRouter.AoeCount.ToString());
                } else if (letters[2] == spellKeys.Elements("fire"))
                {
                    dischargedSpellcast.ConfirmPlacement("fire");
                    SoundManager.Instance.PlayCastSuccessful(castSuccessful_SFX);

                    dRTierObjectColorManager.SetAoeDRObjectTierColor("tier" + dRRouter.AoeCount.ToString());
                }
                
                dRRouter.AoeCount++;
                break;

            case "H":
                break;

            case "Air Projectile":
            case "Fire Projectile":
            case "Water Projectile":
            case "Earth Projectile":
            case "Arcane Projectile":
                //SoundManager.Instance.PlayCastSuccessful(castSuccessful_SFX);
                dischargedSpellcast.CastProjectile(spell);
                break;

            //case "V":
            //    // !! The code is not accessing the first condition block
            //    //when it should be
            //    if (letters[2] == null)
            //    {
            //        SoundManager.Instance.PlayCastSuccessful(castSuccessful_SFX);
            //        dischargedSpellcast.CastBolt("none");
            //    } else {
            //        SoundManager.Instance.PlayCastSuccessful(castSuccessful_SFX);
            //        dischargedSpellcast.CastBolt(spellKeys.Elements(letters[2]));
            //    }
            //    break;

            case "B":
                break;

            case "N":
                break;

            default:
                break;
        }
    }



    // Event triggered when a beam exists/ is being cast
    // When it does, begin the channeled sequence
    //--
    // !! ** This has to be also applied to other channeled spell objects
    //like sphere shield
    void BeamStatus(ulong client, NetworkObjectReference obj, NetworkBehaviour spellNetBehavior, bool beamStatus)
    {
        if (!IsOwner) return;

        Debug.LogFormat($"<color=green>BEAM STATUS: {beamStatus}</color>");

        if (OwnerClientId == client)
        {
            // This funnels the logic of the channeling sequence's keys' to add
            //seconds to the the relative spell being cast, using the spell's script
            spellToBeChanneled = spellNetBehavior;

            //beamScript = spellNetBehavior.GetComponent<Beam>();

            // If the beam no longer exists: Stop channeled casting sequence
            channeledCastingActive = beamStatus;

            if (beamStatus)
            {
                // ** To rename the bool variable below
                isBeamSpellAlive = false;
            } else {
                isBeamSpellAlive = true;
            }

            // Takes in and converts all the possible string letters that can appear during a channeled cast
            //to gameObjects. Those gameObjects are the letters that appear on the screen during the channeled cast.
            GameObject[] ChanneledCastingKeys_Tier_1 = uiReferencesScript.ChanneledCastingKeys_Tier_1.Values.ToArray();

            channelingSequencer.StartChanneledSequence(ChanneledCastingKeys_Tier_1);
        }
    }

    void ShieldStatus(ulong client, NetworkObjectReference obj, NetworkBehaviour spellNetBehavior, bool sphereShieldStatus)
    {
        //Debug.LogFormat($"<color=red>Shield Alive</color> {sphereShieldStatus}");
        if (!IsOwner) return;

        if (OwnerClientId == client)
        {
            // This funnels the logic of the channeling sequence's keys' to add
            //seconds to the the relative spell being cast, using the spell's script
            spellToBeChanneled = spellNetBehavior;

            //sphereShieldScript = spellNetBehavior.GetComponent<SphereShield>();

            
            
            channeledCastingActive = sphereShieldStatus;

            if (sphereShieldStatus)
            {
                // ** To rename the bool variable below
                isBeamSpellAlive = false;
            }
            else
            {
                isBeamSpellAlive = true;
            }

            // This activates channeled casting on the Sphere Shield
            // ** To make this only activate when it is a conjured spell.
            // Takes in and converts all the possible string letters that can appear during a channeled cast
            //to gameObjects. Those gameObjects are the letters that appear on the screen during the channeled cast.
            //GameObject[] ChanneledCastingKeys_Tier_1 = uiReferencesScript.ChanneledCastingKeys_Tier_1.Values.ToArray();

            // Activate the chenelling sequence using the provided keys in the tier list
            // channelingSequencer.StartChanneledSequence(ChanneledCastingKeys_Tier_1);
        }
    }
}

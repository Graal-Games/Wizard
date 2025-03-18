using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using static SpellsDRSpecifications;
using TMPro;
using System;

// Handles input and converts it to spellcasting actions
public class Spellcasting : NetworkBehaviour
{
    [Header("Scripts")]
    SpellKeys spellKeys;
    PlayerInput playerInput;
    DiminishingReturn diminishingReturn;
    RawImage castingIcon;
    DRRouter dRRouter;
    DRActivationLogic dRActivationLogic;
    DischargedSpellcast dischargedSpellcast;
    LocalSpells localSpells;
    PlayerMovement playerMovement;
    PlayerBehavior playerBehavior;
    BarrierSpellCast barrierSpellCast;
    AoeCast aoeCast;

    [Header("Ui")]
    [SerializeField] KeyUi mainActionSquare;
    [SerializeField] GameObject gBackground;
    [SerializeField] GameObject localSpellsCastPoint;
    [SerializeField] TextMeshProUGUI buttonsPressed;

    [Header("Audio")]
    [SerializeField] private AudioClip castMode_SFX;
    [SerializeField] private AudioClip castBufferSquare_SFX;
    [SerializeField] private AudioClip castUnsuccessful_SFX;
    [SerializeField] private AudioClip castSuccessful_SFX;

    List<string> lettersCache;

    SpellManager spellManager;

    bool isPlayerCasting;
    bool isActionSquareAnimationIdle;

    Dictionary<string, string> SpellBook;

    bool isInCastMode = false;
    bool isCastingAoe = false;
    // bool isBeamDRActive;
    // bool isAoeActive;

    //bool actionSquareAnimationState;
    public NetworkVariable<bool> actionSquareAnimationState = new NetworkVariable<bool>(default,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    void Awake()
    {
        playerInput = GetComponentInChildren<PlayerInput>();
        diminishingReturn = GetComponent<DiminishingReturn>();
        dischargedSpellcast = GetComponentInChildren<DischargedSpellcast>();
        spellKeys = GetComponentInChildren<SpellKeys>();
        dRRouter = GetComponent<DRRouter>();
        dRActivationLogic = GetComponent<DRActivationLogic>();
        playerMovement = GetComponent<PlayerMovement>();
        playerBehavior = GetComponent<PlayerBehavior>();
        barrierSpellCast = GetComponentInChildren<BarrierSpellCast>();
        localSpells = localSpellsCastPoint.GetComponent<LocalSpells>();
        aoeCast = dischargedSpellcast.GetComponent<AoeCast>();
        spellManager = GetComponent<SpellManager>();
    }



    void Start()
    {
        castingIcon = gBackground.GetComponent<RawImage>();
        castingIcon.gameObject.SetActive(false);

        mainActionSquare = GetComponentInChildren<KeyUi>();

        // Save the spellbook in a global variable
        SpellBook = spellKeys.SpellBook;

        //actionSquareAnimationState.OnValueChanged += ActionSquareAnimationState;

        ActionSquareState.actionSquareAnimationState += ActionSquareAnimationState;

        // Debug.LogFormat($"<color=orange>{spellManager} script </color>");
    }


    void ActionSquareAnimationState(bool state)
    {
        isActionSquareAnimationIdle = state;

        Debug.LogFormat($"<color=green>isActionSquareAnimationIdle: {isActionSquareAnimationIdle}</color>");

        return;
    }


    // *? Migrate this to a Ui script?
    public void InstantSpellBuffer() 
    {
        //Debug.LogFormat($"<color=green>2222 Instant Spell Buffer Func Beg</color>");
        // This starts the timer for the action buffer
        playerInput.actionBufferActiveGate = true;
        //playerInput.timer = 0; // Not needed?

        castingIcon.gameObject.SetActive(true);
        mainActionSquare.StartAnimation();

        SoundManager.Instance.PlayCastBuffer(castBufferSquare_SFX);
        //Debug.LogFormat($"<color=red>2222 Instant Spell Buffer Func Beg</color>");
    }



    private void ActionBufferQuickReset()
    {
        //if (!isInCastMode) return;
        //isInCastMode = false;
        Debug.LogFormat($"<color=green>1111 ActionBufferQuickReset Func Beg</color>");
        //playerInput.actionBufferActiveGate = false;
        playerInput.actionBufferCastWindowBaseTimerTime = 0;
        //castingIcon.gameObject.SetActive(false);
        //if (!isActionSquareAnimationIdle)
        //{
        mainActionSquare.InterruptActionBufferSquareAnimation();
        //}
        SoundManager.Instance.StopCastBuffer();
        Debug.LogFormat($"<color=red>1111 ActionBufferQuickReset Func Beg</color>");
    }



    public void ClearLettersAndDeactivateCastIcon()
    {
        SoundManager.Instance.StopCastModeSound();
        castingIcon.gameObject.SetActive(false);
        playerInput.letters.Clear();
    }

    // Play cast mode sound and alter movement/ animation speed
    void EnterCastMode()
    {
        if (isInCastMode) return;
        mainActionSquare.isInterrupted = false;
        SoundManager.Instance.PlaySound(castMode_SFX);


        castingIcon.gameObject.SetActive(true);

        // To remove parameters
        playerMovement.EnterCastMovementSlow(2, 0.3f);
        isInCastMode = true;

        return;
    }


    void CastSuccessful()
    {
        SoundManager.Instance.PlayCastSuccessful(castSuccessful_SFX);
    }

    public void ExitCastMode()
    {
        if (!isInCastMode) return;
        playerInput.letters.Clear();
        //Debug.LogFormat($"<color=green>4444 ExitCastMode Func Beg</color>");
        
        castingIcon.gameObject.SetActive(false);

        if (!isActionSquareAnimationIdle)
        {
            mainActionSquare.InterruptActionBufferSquareAnimation();
            //mainActionSquare.isInterrupted = false;
        }
        
        playerMovement.ExitCastMovementSlow();

        SoundManager.Instance.StopCastModeSound();
        //if (castSuccessfull)
        //{
        //    SoundManager.Instance.PlayCastUnsuccessful(castSuccessful_SFX);
        //} else
        //{
        //    SoundManager.Instance.PlayCastUnsuccessful(castUnsuccessful_SFX);
        //}
        //SoundManager.Instance.PlayCastUnsuccessful(castUnsuccessful_SFX);
        isInCastMode = false;
            
        Debug.LogFormat($"<color=red>4444 ExitCastMode Func end</color>");
        return;
        
        
    }    

    public string CheckSpellExistence(List<string> letters, Dictionary<string, string> SpellBook)
    {
        // Convert the list of letters to a string - This is the Key used to access the spell value in the SpellBook Dictionary
        string sequence = string.Join("", letters);

        // Check if the input sequence matches a spell in the spellbook, if not, clear the letters and return null
        if (!SpellBook.ContainsKey(sequence))
        {
            playerInput.letters.Clear();
            return null;
        }

        // Otherwise, return the spell value using the key
        return SpellBook[sequence];
    }

    // If the player had a DR sequence active when he pressed G (Cast)
    // Exit the DR sequence and deactivate the currently active DR keys
    void DeactivateDRCastIfActive()
    {
        if (playerInput.isCastingDRSequence)
        {
            playerInput.isCastingDRSequence = false;

            //playerInput.isAoeDRActive = false;

            diminishingReturn.DeactivateCurrentDRKeys();
        }
    }

    // If the player presses the BeginCast button again while mid casting a beam
    //disable the channeling sequencer, despawn the beam and close all gates
    // ** Do the same thing for Sphere Shield
    void CheckIfChanneledCastingIsActive()
    {
        if (playerInput.channeledCastingActive && !playerInput.isBeamSpellAlive)
        {
            playerInput.StopChanneledCasting();
            playerInput.channeledCastingActive = false;

            SpellsManager.Beam.CancelBeam();
        }
    }

    bool IsInputWithinActionBufferTimerCastableWindow()
    {
        if (playerInput.actionBufferCastWindowBaseTimerTime >= playerInput.canCastEntry && playerInput.actionBufferCastWindowBaseTimerTime <= playerInput.canCastExit)
        {
            return true;
        } else
        {
            
            return false;
        }
    }

    void CastSpell(string passedSpell, bool isSkipActionBuffer = false)
    {
        Debug.LogFormat($"<color=green>3333 CastProjectile Func Beg</color>");

        // This checks if the cast is made within the Action Buffer Cast window
        if (!IsInputWithinActionBufferTimerCastableWindow())
        {
            // if (isCastingAoe == true) isAoeCastInterrupted = true;
            //mainActionSquare.isInterrupted = true;
            dRActivationLogic.IsAoeCastInterrupted = true;
            mainActionSquare.isInterrupted = true;
            ExitCastMode();
            playerInput.ResetTimerSquareAndLetterList();
            Debug.LogFormat($"<color=purple>3333 CastProjectile Func ELSE</color>");
            
            return;
        }

        if (IsInputWithinActionBufferTimerCastableWindow() || isSkipActionBuffer)
        {
            // playerInput.preCastWindowWarning = false; // IF SPELLCASTING BREAKS - I DEACTIVATED THIS
            //isCastingAoe = false;
            // CastSuccessful();
            // InstantSpellBuffer();
            // The aoe placement is an object that is rapidly spawning and despawning
            // If the player was casting aoe this stops the function that visualizes placement.
            dischargedSpellcast.StopAoePlacement();
            Debug.LogFormat($"<color=orange>Arcane Projectile</color>");

            // This casts "V", "F", "Y" as they share the same base cast type
            playerInput.CastSpell(passedSpell);

            playerInput.ResetTimerSquareAndLetterList();
            playerMovement.EnterCastMovementSlow(5, 1);
            Debug.LogFormat($"<color=red>3333 CastProjectile Func End</color>");
            return;
        }
    }

    bool CheckIfDRIsActive()
    {
        if (playerInput.isAoeDRActive == false)
        {
            return true;
        }

        return false;
    }

    bool CheckIfToActivateSpellCategoryDR(string spell)
    {
        if ( playerInput.isAoeDRActive == false || dRActivationLogic.IsAoeCastInterrupted )
        {
            
            dRActivationLogic.CheckIfToActivateAoeDR();

            return false;
        } else
        {
            dRActivationLogic.ActivateAoeDRSequence();
            //ExitCastMode();
            //playerInput.ResetTimerSquareAndLetterList();
            Debug.LogFormat($"<color=purple>22222222222222222222222</color>");
            return true;
        }
            //dRActivationLogic.DRActivationGate("Aoe");
            //return;
        
    }

    void PlaySound()
    {
        if (!mainActionSquare.isInterrupted || !dRActivationLogic.IsAoeCastInterrupted)
        {
            SoundManager.Instance.PlayCastUnsuccessful(castSuccessful_SFX);
        }
        else
        {
            SoundManager.Instance.PlayCastUnsuccessful(castUnsuccessful_SFX);
            
        }
    }

    public void Spellcast(List<string> letters)
    {

        // Check if the spell exists in the spellbook dictionary, if it does, return the spell key's value from the dictionary
        string spell = CheckSpellExistence(letters, SpellBook);
        //bool isCastSuccessful = true;
        


        switch (spell)
        {
            default:
                Debug.Log("Spell Does Not Exist");

                ExitCastMode();
                playerInput.ResetTimerSquareAndLetterList();

                if (!playerInput.isCastingDRSequence)
                {
                    //mainActionSquare.isInterrupted = false;
                    SoundManager.Instance.PlayCastUnsuccessful(castUnsuccessful_SFX);
                    //isCastingAoe = false;
                }
                break;

            //###########
            //  Cast
            //###########

            case "Cast":
                
                EnterCastMode();
                DeactivateDRCastIfActive();
                CheckIfChanneledCastingIsActive();
                break;

            // ########### Projectiles ###########
            case "Arcane Projectile -1 [T]":

                InstantSpellBuffer();
                break;

            case "Air Projectile -1":
            case "Earth Projectile -1":
            case "Water Projectile -1":
            case "Fire Projectile -1":
            case "Air Aoe-1":
            case "Earth Aoe-1":
            case "Water Aoe-1":
            case "Fire Aoe-1":
                // Action buffer reset before a new Action Buffer is activated
                ActionBufferQuickReset();
                InstantSpellBuffer();
                break;

            case "Air Projectile":
            case "Earth Projectile":
            case "Water Projectile":
            case "Fire Projectile":
            case "Arcane Projectile":

                CastSpell(spell);
                ExitCastMode();
                PlaySound();
                    
                playerInput.ResetTimerSquareAndLetterList();
                break;


            // ########### AOEs ###########
            case "ArcaneAoe -1 [T]":

                if (!CheckIfToActivateSpellCategoryDR(spell))
                {
                    InstantSpellBuffer();
                    dischargedSpellcast.StartCastAoe(spell);
                } else
                {
                    ExitCastMode();
                    playerInput.ResetTimerSquareAndLetterList();
                    return;
                }
                break;

            //case "Air Aoe": (Needs implementation)
            case "Earth Aoe":
            //case "Water Aoe": (Needs implementation)
            case "Fire Aoe":
            case "Arcane Aoe":
                CastSpell(spell);
                ExitCastMode();
                PlaySound();
                playerInput.ResetTimerSquareAndLetterList();

                break;

            // ########### Spheres ###########


            case "Arcane Shield -1 [T]":
                // In order for the shield to be cast instantly, and to maintain the option
                //for the player to transmute it, this will be skipped and a following "G" is required.

                // Additionally, channeled sphere shield will be switched to become casted through
                //the conjuration category.

                // This is where DR is to be added in case implementation is decided
                break;

            case "Air Shield -1":
            case "Earth Shield -1":
            case "Water Shield -1":
            case "Fire Shield -1":
                InstantSpellBuffer();
                break;

            case "Arcane Shield":
            case "Air Shield":
            case "Earth Shield":
            case "Water Shield":
            case "Fire Shield":
                localSpells.CastShield();
                dRRouter.SphereCount++;
                //CastSpell(spell, true);
                ExitCastMode();
                PlaySound();
                playerInput.ResetTimerSquareAndLetterList();

                break;
                
            // ########### Beams ###########


            // ########### Barriers ###########


            // ########### Conjurations ###########


            // ########### Invocations ###########


            // ########### Summons ########### 


            

        }
    }

    // The DR liable spells are currently being cast from
    //within the DRActivationLogic script
    // ?? Should cast command code be unified somewhere instead? 
    public void SpellComboRouter(int count, List<string> letters)
    {
        //Debug.LogFormat($"<color=orange>LETTERS VARIABLE: {letters}</color>");
        lettersCache = letters;

        string listContents = string.Join(" ", lettersCache);

        // Shows the current spell combo/ buttons pressed
        buttonsPressed.text = listContents;
        

        switch (count)
        {
            /// >>>>>>>>>>>>>>>>>>>>>>>>>>>>> 1 <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            case 1:
                
                //playerMovement.MainCharSpeed = 2;
                // AccessBaseSpells();
                if (lettersCache[0] == spellKeys.BeginCast)
                {
                    SoundManager.Instance.PlaySound(castMode_SFX);
                    
                    playerMovement.EnterCastMovementSlow(2, 0.3f);


                    // If the player presses the BeginCast button again mid DR lock
                    //deactivate the letters presented on the screen
                    if (playerInput.isCastingDRSequence)
                    {
                        // Cancel the the DRSeq lock if the DR is active but was cancelled 
                        //by pressing G 
                        playerInput.isCastingDRSequence = false;
                        //playerInput.isAoeDRActive = false;
                        diminishingReturn.DeactivateCurrentDRKeys();
                    }
                    
                    // If the player presses the BeginCast button again while mid casting a beam
                    //disable the channeling sequencer, despawn the beam and close all gates
                    // ** Do the same thing for Sphere Shield
                    if (playerInput.channeledCastingActive && !playerInput.isBeamSpellAlive)
                    {
                        playerInput.StopChanneledCasting();
                        playerInput.channeledCastingActive = false;

                        SpellsManager.Beam.CancelBeam();
                    }

                    castingIcon.gameObject.SetActive(true);
                    //Debug.LogFormat($"<Color=black>Casting Square</color>");

                } else {

                    castingIcon.gameObject.SetActive(false);
                    lettersCache.Clear();
                    return;
                }
            break;

            /// ##################################################################
            /// >>>>>>>>>>>>>>>>>>>>>>>>>>>>> 2 <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            /// ##################################################################
            case 2:
            // When firing DRs, casting needs to occur within the DR logic
            //for the spellcasting to work as intended. Bad code?
                if (lettersCache[1] == spellKeys.BaseSpells(lettersCache[1]))
                {
                    // Design: For the DR activations below, we are presented with the option
                    //to either keep the character slowed when he is DR locked
                    //or to keep his regular speed.
                    //For the time being the slow is kept while in DR lock.
                    if (lettersCache[1] == "F") // Aoe
                    {
                        //The below line doesn't work here
                        //dischargedSpellcast.StartCastAoe();

                        // This is incorrect, DR needs to be activated after the cast is
                        //confirmed, not when the player is still casting.
                        // ** To migrate DR entry to case 3 and do the proper changes.

                        dRActivationLogic.DRActivationGate("Aoe");

                        return;
                    }

                    if (lettersCache[1] == "B") // Beam
                    {
                        // If the DR lock is not active, cast the beam
                        //else if the spell had been cast up to the point of
                        //activating DR activate DR logic.

                        // ** A DR counter should be activated here immediately.
                        // to be checked using a value counter
                        //the value of the case inside DRRouter 

                        // Had to make two references to the charm DRActivation logic
                        // due to the fact that it initially needs to be activated once the spell is cast,
                        // but once it is activated DR sequence and logic needs to be accessed as soon as the
                        // charm category is accessed
                        if (!playerInput.channeledCastingActive)
                        {
                            dRActivationLogic.DRActivationGate("Beam");
                        } else
                        {
                            return;
                        }

                        ClearLettersAndDeactivateCastIcon();
                        
                        //SoundManager.Instance.StopPlayCastMode();
                        //castingIcon.gameObject.SetActive(false);
                        //lettersCache.Clear();

                        return;
                    }
                    
                    // The shield works differently from the other two spells
                    //in that it requires aditionally a cast buffer window
                    //which means the thread needs should be:
                    //G>T>Check for DR>if yes, present DR>if not, present buffer>then G, casts spell
                    if (lettersCache[1] == "T") // Shield
                    {
                        //Debug.LogFormat($"<Color=orange>Shield</color>");
                        // If the DR lock is not active, cast the beam
                        //else if the spell had been cast up to the point of
                        //activating DR activate DR logic.

                        // ** A DR counter should be activated here immediately.
                        // to be checked using a value counter
                        //the value of the case inside DRRouter 

                        // !! The code below is required to prevent the shield from 
                        //stacking when cast twice simultaneously

                        // ** Add a condition here (Maybe not here maybe in DRActivationGate script): 
                        //When the last DR lock had been correctly input
                        //do not make the player have to input a new one in case the cast was interrupted.
                        // I guess there would have to be a bool placed inside PlayerInput script
                        //that makes that check?
                        
                        dRActivationLogic.DRActivationGate("Shield");

                        // !!! IN SOME INSTANCES - AT THE TIME OF WRITING THIS, I AM NOT SURE OF THE DETAILS
                        //OR WHY - BUT THE LINE BELOW IS WHAT IS CAUSING THE BUFFERING INSTANT 
                        //SPELL BUFFER BUG. !!!
                        // >> playerInput.ResetTimerSquareAndLetterList(); << "This guy, that's him ":*("

                        ClearLettersAndDeactivateCastIcon();

                        //SoundManager.Instance.StopPlayCastMode();
                        //castingIcon.gameObject.SetActive(false);
                        //lettersCache.Clear();

                        // ** Change this so that there is a single method for each new speed
                        playerMovement.EnterCastMovementSlow(5, 1);

                        return;

                        
                    }

                    if (letters[1] == "R") // Charm
                    {
                        // (?) should there be a base charm spell

                       
                        // This checks if the Charm spells category is currently in DR
                        if (playerInput.isCharmDRActive && dRActivationLogic.IsCharmCastInterrupted == false)
                        {
                            
                            dRActivationLogic.DRActivationGate("Charm", "Mist");

                            ClearLettersAndDeactivateCastIcon();

                            //SoundManager.Instance.StopPlayCastMode();
                            //castingIcon.gameObject.SetActive(false);
                            //lettersCache.Clear();

                            playerMovement.EnterCastMovementSlow(5, 1);
                        } else
                        {
                            return;
                        }
                        
                        return;
                    }

                    if (letters[1] == "V") // Bolt
                    {
                        InstantSpellBuffer();
                        SoundManager.Instance.PlayCastBuffer(castBufferSquare_SFX);

                        //Debug.LogFormat($"<color=orange>BOLT</color>");

                        return;
                    }
                    
                    if (letters[1] == "Y") // Barrier
                    {
                        SoundManager.Instance.StopCastModeSound();

                        if (playerInput.isBarrierDRActive)
                        {
                            dRActivationLogic.DRActivationGate("Barrier", "Arcane");

                        } else
                        {
                            SoundManager.Instance.PlayCastBuffer(castBufferSquare_SFX);
                            InstantSpellBuffer();
                            // dRActivationLogic.DRActivationGate("Barrier");
                        }


                        return;
                    }

                    if (letters[1] == "H" || letters[1] == "V")
                    {

                        ClearLettersAndDeactivateCastIcon();
                        // This stops the cast mode sound
                        //SoundManager.Instance.StopPlayCastMode();
                        //castingIcon.gameObject.SetActive(false);
                        //lettersCache.Clear();

                        playerMovement.EnterCastMovementSlow(5, 1);
                        //SoundManager.Instance.PlayCastBuffer(castBufferSquare_SFX);
                        SoundManager.Instance.PlayCastUnsuccessful(castUnsuccessful_SFX);
                    }

                } else {
                    ClearLettersAndDeactivateCastIcon();
                    //// This stops the cast mode sound
                    //SoundManager.Instance.StopPlayCastMode();
                    //castingIcon.gameObject.SetActive(false);
                    //lettersCache.Clear();
                    playerMovement.EnterCastMovementSlow(5, 1);

                    
                    //SoundManager.Instance.PlayCastBuffer(castBufferSquare_SFX);
                    SoundManager.Instance.PlayCastUnsuccessful(castUnsuccessful_SFX);

                }
            break;

            /// ##################################################################
            /// >>>>>>>>>>>>>>>>>>>>>>>>>>>>> 3 <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            /// ##################################################################
            case 3:
                // This casts the bolt - Only if the cast button is pressed within the buffer window
                // The Base spell in this condition is to be changed to "BufferSpells" instead
                //and in continuation have the else statement for "F" and "T" by default i.e without having
                //to explicitly state it
                if (playerInput.actionBufferCastWindowBaseTimerTime >= playerInput.canCastEntry && playerInput.actionBufferCastWindowBaseTimerTime <= playerInput.canCastExit
                    && (lettersCache[1] == spellKeys.BaseSpells(letters[1]))
                    && (lettersCache[2] == spellKeys.BeginCast))
                {
                    // playerInput.preCastWindowWarning = false; // IF SPELLCASTING BREAKS - I DEACTIVATED THIS

                    // The aoe placement is an object that is rapidly spawning and despawning
                    // this stops it.
                    dischargedSpellcast.StopAoePlacement();
                    Debug.LogFormat($"<color=orange>First</color>");

                    // This is what casts "T" (Sphere shield) - in case you were wondering
                    //And "V" (Bolt) 
                    //And "F" (AoE) as they share the same base cast type
                    playerInput.CastSpell(lettersCache[1]);

                    playerInput.ResetTimerSquareAndLetterList();
                    playerMovement.EnterCastMovementSlow(5, 1);
                    return;

                }
                else 
                {

                    // If the second letter of the 3 letter combination is F (AOE) stopthecast?
                    /* <Summary>
                    * This here bad boy ensures that if the player was casting an aoe but
                    * either didn't make the cast in time or input an incorrect [2]nd letter (TO COMPLETE)
                    </Summary> */
                    Debug.LogFormat($"<color=orange>Third</color>");

                    if (lettersCache[1] == "F"
                        && (letters[2] == spellKeys.Elements("earth")
                        || letters[2] == spellKeys.Elements("fire")))
                    {
                        // (**) To rename 'timer' variable.
                        // <summary> It handles the timer that restricts the player
                        // to cast when the Active Action feature permits </summary>
                        playerInput.actionBufferCastWindowBaseTimerTime = 0;
                        Debug.LogFormat($"<color=orange>Fifth</color>");
                        mainActionSquare.StopActionBufferSquareAnimation();
                        SoundManager.Instance.StopCastBuffer();
                        // Debug.LogFormat($"<color=orange>Water Bolt</color>");
                        InstantSpellBuffer();
                        SoundManager.Instance.PlayCastBuffer(castBufferSquare_SFX);
                        return;
                    }
                    else if (lettersCache[1] == "F")
                    {
                        dischargedSpellcast.StopAoePlacement();
                        dRActivationLogic.IsAoeCastInterrupted = true;
                        playerMovement.EnterCastMovementSlow(5, 1);
                        Debug.LogFormat($"<color=orange>Sixth point five</color>");
                    }

                    // If the spell was interrupted mid-cast activate the gate that exempts
                    // the player from retyping the DR sequence
                    // Does this need to be here? 
                    if (lettersCache[1] == "T")
                    {
                        dRActivationLogic.IsSphereCastInterrupted = true;
                        // playerInput.ResetTimerSquareAndLetterList();
                        playerMovement.EnterCastMovementSlow(5, 1);
                        Debug.LogFormat($"<color=orange>Sixth</color>");
                        return;
                    }





                    if (lettersCache[1] == "Y" 
                        && (lettersCache[2] == spellKeys.Elements("fire") 
                        || lettersCache[2] == spellKeys.Elements("earth"))) // Y & U
                    {
                        playerInput.actionBufferCastWindowBaseTimerTime = 0;

                        mainActionSquare.StopActionBufferSquareAnimation();

                        InstantSpellBuffer();
                        Debug.LogFormat($"<color=orange>Sixth Second</color>");
                        SoundManager.Instance.StopCastBuffer();
                        SoundManager.Instance.PlayCastBuffer(castBufferSquare_SFX);
                        return;
                        // Debug.LogFormat($"<color=orange>FIRE BARRIER</color>");
                    }


                    //if (lettersCache[1] == "R" && lettersCache[2] == "P")
                    //{
                    //    playerInput.timer = 0;

                    //    keyUi.StopAnimation();

                    //    SoundManager.Instance.StopCastBuffer();
                    //    Debug.LogFormat($"<color=cyan>Second</color>");
                    //    InstantSpellBuffer();
                    //    SoundManager.Instance.PlayCastBuffer(castBufferSquare_SFX);

                    //    return;
                }

                if (lettersCache[1] == "V"
                    && (lettersCache[2] == "U" || lettersCache[2] == "I" || lettersCache[2] == "O" || lettersCache[2] == "P"))
                {
                    playerInput.actionBufferCastWindowBaseTimerTime = 0;
                    mainActionSquare.StopActionBufferSquareAnimation();
                    SoundManager.Instance.StopCastBuffer();
                    InstantSpellBuffer();
                    SoundManager.Instance.PlayCastBuffer(castBufferSquare_SFX);
                    Debug.LogFormat($"<color=orange>Seventh</color>");
                    return;
                }

                // THIS IS TO BE CHANGED TO G:R:F:P:I:G ??? - MIST <<
                // (**!!) The Charm DR found here should actually be placed in Case 2 (**!!) 
                if ( (lettersCache[1] == spellKeys.Charm) 
                    && (letters[2] == spellKeys.Elements("fire")) ) 
                {
                    
                    // (!) This might have to be removed
                    dRActivationLogic.DRActivationGate("Charm", "Mist");



                    // (!!) Reactivate the commented lines below if DR activation logic
                    // is moved back to the previous case
                    // >> playerInput.timer = 0;

                    // >> keyUi.StopAnimation();
                    // >> SoundManager.Instance.StopCastBuffer();
                    Debug.LogFormat($"<color=orange>Second</color>");
                    // >> InstantSpellBuffer();
                    // >> SoundManager.Instance.PlayCastBuffer(castBufferSquare_SFX);

                    return;
                // >>> CHANNELABLE BARRIER <<<
                } else if ((lettersCache[1] == spellKeys.Charm)
                    && (letters[2] == spellKeys.BaseSpells("Y")))
                {
                    dRActivationLogic.DRActivationGate("Charm", "Barrier");
                    // Upkeepable barreir here - Channelable
                    // Save the Long barrier prefab as a barrier placement
                    Debug.LogFormat($"<color=orange>Upkeepable barreir - TO IMPLEMENT</color>");

                    return;
                //                 >> AOE <<     >> Long Barrier <<     >> G:R:F <<
                } else if ((lettersCache[1] == spellKeys.Charm)
                    && (letters[2] == spellKeys.BaseSpells("F")))
                {

                    dRActivationLogic.DRActivationGate("Charm", "Mist");

                    //if (playerInput.isCharmDRActive)
                    //{
                    //    // This no longer needs to hold the type of spell
                    //    // Spell type will be directly injected into AoeCast
                    //    dRActivationLogic.DRActivationGate("Charm", "Mist");
                    //} else
                    //{
                    //    dischargedSpellcast.StartCastAoe(); //
                    //}
                     
                    Debug.LogFormat($"<color=orange>G:R:F</color>");
                    return;
                }
                // The below could probably be deleted
                else if ((lettersCache[1] == spellKeys.Charm)
                            && (letters[2] != spellKeys.Elements("fire")))
                {
                    // !! Important note: Incorrect input of casting charm in this case
                    // has got to be handled in such manner and not using ResetTimerSquareAndList()
                    // otherwise, resetting the variables and deactivating them before they are even
                    // activated, causes the active action square to bug out and stop working

                    lettersCache.Clear();

                    SoundManager.Instance.StopCastBuffer(castBufferSquare_SFX);
                    SoundManager.Instance.PlayCastUnsuccessful(castUnsuccessful_SFX);

                    castingIcon.gameObject.SetActive(false);

                    playerMovement.EnterCastMovementSlow(5, 1);

                    Debug.LogFormat($"<color=orange>Second 2</color>");

                    return;

                }

                playerInput.ResetTimerSquareAndLetterList();
                playerMovement.EnterCastMovementSlow(5, 1);
                SoundManager.Instance.StopCastBuffer();
                //SoundManager.Instance.PlayCastBuffer(castBufferSquare_SFX);

                //>>castingSquare.gameObject.SetActive(false);
                //>>lettersCache.Clear();

                SoundManager.Instance.PlayCastUnsuccessful(castUnsuccessful_SFX);

                Debug.LogFormat($"<color=red>Reset</color>");

                //}
                
                
            break;

            /// ##################################################################
            /// >>>>>>>>>>>>>>>>>>>>>>>>>>>>> 4 <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            /// ##################################################################

            // Modifications of base spells. Includes:
            // Size - Quantity - Element and potency
            case 4:
                // Cast modified base spell - Tier 1 Mod
                if (playerInput.actionBufferCastWindowBaseTimerTime >= playerInput.canCastEntry
                    && playerInput.actionBufferCastWindowBaseTimerTime <= playerInput.canCastExit
                    && letters[3] == spellKeys.BeginCast)
                {
                    //playerInput.preCastWindowWarning = false; // IF SPELLCASTING BREAKS - I DEACTIVATED THIS

                    castingIcon.gameObject.SetActive(false);

                    //if (letters.Contains())
                    dischargedSpellcast.StopAoePlacement();
                    playerInput.CastSpell(lettersCache[1]);
                    playerInput.ResetTimerSquareAndLetterList();
                    Debug.LogFormat($"<color=green>CASE 4: base cast</color>");

                    playerMovement.EnterCastMovementSlow(5, 1);

                    return;

                }
                else
                {
                    if (lettersCache[1] == "F")
                    {
                        dRActivationLogic.IsAoeCastInterrupted = true;
                        playerMovement.EnterCastMovementSlow(5, 1);
                        Debug.LogFormat($"<color=orange>Sixth point five</color>");

                        // If player was casting charm aoe, this stops it
                        dischargedSpellcast.StopAoePlacement();
                        castingIcon.gameObject.SetActive(false);
                        playerInput.ResetTimerSquareAndLetterList();

                        SoundManager.Instance.StopCastModeSound();
                        SoundManager.Instance.PlayCastUnsuccessful(castUnsuccessful_SFX);
                        return;
                    }





                    if ((lettersCache[1] == "R")
                            && ((lettersCache[2] == spellKeys.Elements("fire"))
                            && (lettersCache[3] == spellKeys.Elements("water"))))
                    {
                        ActionBufferQuickReset();
                        InstantSpellBuffer();

                        Debug.LogFormat($"<color=yellow>CASE 4: mist cast - transitory</color>");

                        //SoundManager.Instance.PlayCastBuffer(castBufferSquare_SFX);
                        return;
                        // >> CURRENTLY ONLY TRANSITORY <<
                    }

                    if (lettersCache[1] == spellKeys.Charm
                        && letters[2] == spellKeys.BaseSpells("F")
                        && letters[3] == spellKeys.BaseSpells("Y"))
                    {
                        // Change the Aoe placement type here
                        aoeCast.ChangeAoeSpell("Arcane Barrier");

                        InstantSpellBuffer();

                        Debug.LogFormat($"<color=orange>CASE 4: barrier pre-cast</color>");
                        return;
                    }
                    // I had to add this last else if because an else would otherwise not permit the execution of the proceeding code
                    else if (lettersCache[1] == spellKeys.Charm
                        && letters[2] == spellKeys.BaseSpells("F")
                        && letters[3] != spellKeys.BaseSpells("Y"))
                    {
                        dischargedSpellcast.StopAoePlacement();
                        //dRActivationLogic.IsCharmCastInterrupted = true;
                        playerMovement.EnterCastMovementSlow(5, 1);
                        Debug.LogFormat($"<color=orange>Sixth point five</color>");
                        // playerInput.ResetTimerSquareAndLetterList();
                        letters.Clear();

                        SoundManager.Instance.StopCastModeSound();
                        SoundManager.Instance.PlayCastUnsuccessful(castUnsuccessful_SFX);
                        castingIcon.gameObject.SetActive(false);
                        return;
                    }

                    playerInput.actionBufferActiveGate = true;
                    dischargedSpellcast.StopAoePlacement();
                    castingIcon.gameObject.SetActive(false);

                    //ClearLettersAndDeactivateCastIcon();
                    playerInput.ResetTimerSquareAndLetterList();
                    // >> reset player speed here
                    playerMovement.EnterCastMovementSlow(5, 1);

                    Debug.LogFormat($"<color=brown>CASE 4: default</color>");
                    SoundManager.Instance.StopCastModeSound();
                    SoundManager.Instance.PlayCastUnsuccessful(castUnsuccessful_SFX);
                    return;
                }
                //if ((lettersCache[1] == "R")
                //   && ((lettersCache[2] == spellKeys.Elements("fire"))
                //   || (lettersCache[3] != spellKeys.Elements("water"))))
                //{

                //    // ! This line might be causing casting issues
                //    // (**)
                //    playerInput.ResetTimerSquareAndLetterList();
                //    SoundManager.Instance.StopCastBuffer();
                //    dischargedSpellcast.StopAoePlacement();
                //    //>> playerInput.timer = 0;


                //    //>> mainActionSquare.StopAnimation();
                //    // reset player speed here
                //    playerMovement.CastingMovementSlow(5, 1);

                //    //SoundManager.Instance.PlayCastBuffer(castBufferSquare_SFX);
                //    SoundManager.Instance.PlayCastUnsuccessful(castUnsuccessful_SFX);
                //    Debug.LogFormat($"<color=brown>CASE 4: Charm fail</color>");
                //    return;
                //} 

                // REMOVED THIS CUZ IT DID NOT MAKE SENSE, REACTIVATE IF SOMETHING STOPS WORKING
                //else if (lettersCache[1] == "F")
                //{
                //    dRActivationLogic.IsAoeCastInterrupted = true;
                //    playerMovement.CastingMovementSlow(5, 1);
                //    Debug.LogFormat($"<color=orange>Sixth point five</color>");
                //}

                //// This is a regular long barrier
                //if ((lettersCache[1] == spellKeys.Charm)
                //    && (letters[2] != spellKeys.BaseSpells("Y"))
                //    && (letters[3] != spellKeys.BaseSpells("B")))
                //{
                //    // (Optional) Show where it would be placed for player feedback/ reference
                //}


                // <to-do>
                // Handle when the incorrect button is pressed
                //or if cast timing was off
                //</to-do>
                //playerInput.timer = 0;
                //dischargedSpellcast.StopAoePlacement();
                //playerInput.ResetTimerSquareAndLetterList();
                ////>>keyUi.StopAnimation();
                ////castingSquare.gameObject.SetActive(false);
                ////>>lettersCache.Clear();
                //playerMovement.CastingMovementSlow(5, 1);
                //Debug.LogFormat($"<color=brown>CASE 4: last else</color>");
                //// This stops the cast mode sound
                //SoundManager.Instance.StopPlayCastMode();
                ////SoundManager.Instance.PlayCastBuffer(castBufferSquare_SFX);
                //SoundManager.Instance.PlayCastUnsuccessful(castUnsuccessful_SFX);

                playerInput.actionBufferActiveGate = true;
                dischargedSpellcast.StopAoePlacement();
                castingIcon.gameObject.SetActive(false);

                //ClearLettersAndDeactivateCastIcon();
                playerInput.ResetTimerSquareAndLetterList();
                // >> reset player speed here
                playerMovement.EnterCastMovementSlow(5, 1);

                Debug.LogFormat($"<color=brown>CASE 4: default</color>");
                SoundManager.Instance.StopCastModeSound();
                SoundManager.Instance.PlayCastUnsuccessful(castUnsuccessful_SFX);


                break;


            /// ##################################################################
            /// >>>>>>>>>>>>>>>>>>>>>>>>>>>>> 5 <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            /// ##################################################################
            case 5:

                // Here the order of mod 1 and mod 2 are interchangeable
                //thus the check should be made for both [2] and [3]
                //[4] is the cast.

                // if (letters.Contains(spellKeys.ElementsList()))
                // {

                // }

                if ( playerInput.actionBufferCastWindowBaseTimerTime >= playerInput.canCastEntry && playerInput.actionBufferCastWindowBaseTimerTime <= playerInput.canCastExit 
                    && (lettersCache[1] == spellKeys.Charm) 
                    && (letters[2] == spellKeys.Elements("fire"))  // "P" 
                    && (letters[3] == spellKeys.Elements("water")) // "I" 
                    && (letters[4] == spellKeys.BeginCast) ) {

                    playerInput.actionBufferActiveGate = true;
                    dischargedSpellcast.StopAoePlacement();
                    castingIcon.gameObject.SetActive(false);

                    playerInput.CastSpell("mist");
                    //ClearLettersAndDeactivateCastIcon();
                    playerInput.ResetTimerSquareAndLetterList();
                    // >> reset player speed here
                    playerMovement.EnterCastMovementSlow(5, 1);

                    Debug.LogFormat($"<color=brown>CASE 5: Charm</color>");
                    return;

                }
                // >>>>>>>>>>>> Arcane Aoe Mode Barrier <<<<<<<<<<<<
                if (playerInput.actionBufferCastWindowBaseTimerTime >= playerInput.canCastEntry && playerInput.actionBufferCastWindowBaseTimerTime <= playerInput.canCastExit
                    && (lettersCache[1] == spellKeys.Charm
                    && letters[2] == spellKeys.BaseSpells("F")
                    && letters[3] == spellKeys.BaseSpells("Y")
                    && letters[4] == spellKeys.BeginCast) )
                {
                    // ****************
                    // Aoe mode Barrier
                    // ****************
                    // >> dRActivationLogic.DRActivationGate("Barrier", "Arcane");
                    barrierSpellCast.CastBarrier("Aoe Arcane", aoeCast.AoeSpawnPosition);
                    SoundManager.Instance.PlayCastUnsuccessful(castSuccessful_SFX); // amend

                    dRRouter.CharmCount++;

                    dischargedSpellcast.StopAoePlacement();
                    playerInput.ResetTimerSquareAndLetterList();
                    // >> reset player speed here
                    playerMovement.EnterCastMovementSlow(5, 1);
                    return;
                } 
                else if (lettersCache[1] == spellKeys.Charm
                && letters[2] == spellKeys.BaseSpells("F")
                && letters[3] == spellKeys.BaseSpells("Y")
                && letters[4] == spellKeys.BaseSpells("B"))
                {
                    ActionBufferQuickReset();

                    aoeCast.ChangeAoeSpell("Long Arcane Barrier");

                    InstantSpellBuffer();

                    SoundManager.Instance.PlayCastBuffer(castBufferSquare_SFX);

                    Debug.LogFormat($"<color=brown>CASE 5: LONG BARRIER Charm - Instant spell buffer</color>");

                    return;
                }
                //|| letters[4] == spellKeys.Elements("water")
                //|| letters[4] == spellKeys.Elements("air")
                else if (lettersCache[1] == spellKeys.Charm
                && letters[2] == spellKeys.BaseSpells("F")
                && letters[3] == spellKeys.BaseSpells("Y")
                && (letters[4] == spellKeys.Elements("earth")
                
                || letters[4] == spellKeys.Elements("fire")) )
                {
                    ActionBufferQuickReset();

                    // change spell element
                    aoeCast.ChangeAoeBarrierElement(letters[4]);

                    InstantSpellBuffer();

                    SoundManager.Instance.PlayCastBuffer(castBufferSquare_SFX);

                    Debug.LogFormat($"<color=brown>CASE 5: LONG BARRIER Charm - Instant spell buffer</color>");

                    return;
                }
                //|| letters[4] != spellKeys.Elements("I")  
                //|| letters[4] != spellKeys.Elements("O")
                
                else if (  lettersCache[1] == spellKeys.Charm
                && letters[2] == spellKeys.BaseSpells("F")
                && letters[3] == spellKeys.BaseSpells("Y")
                && ( letters[4] != spellKeys.Elements("U")  
                  
                || letters[4] != spellKeys.Elements("P")
                || letters[4] != spellKeys.BaseSpells("B"))  )
                {
                    Debug.LogFormat($"<color=brown>CASE 5: BARRIER Charm - INTERRUPTED {letters[4]}</color>");

                    playerMovement.EnterCastMovementSlow(5, 1);

                    // If player was casting charm aoe, this stops it
                    dischargedSpellcast.StopAoePlacement();
                    castingIcon.gameObject.SetActive(false);
                    playerInput.ResetTimerSquareAndLetterList();

                    dRActivationLogic.IsCharmCastInterrupted = true;

                    SoundManager.Instance.StopCastModeSound();
                    SoundManager.Instance.PlayCastUnsuccessful(castUnsuccessful_SFX);
                    return;
                }


                    Debug.LogFormat($"<color=red>Weather charm Cancelled</color>");

                    //castingSquare.gameObject.SetActive(false);
                    //lettersCache.Clear();
                    playerMovement.EnterCastMovementSlow(5, 1);

                    // If player was casting charm aoe, this stops it
                    dischargedSpellcast.StopAoePlacement();
                    castingIcon.gameObject.SetActive(false);
                    playerInput.ResetTimerSquareAndLetterList();

                    SoundManager.Instance.StopCastModeSound();
                    SoundManager.Instance.PlayCastUnsuccessful(castUnsuccessful_SFX);
                

                break;


            /// ##################################################################
            /// >>>>>>>>>>>>>>>>>>>>>>>>>>>>> 6 <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            /// ##################################################################
            case 6:
                // Removed air and water here
                //|| letters[4] == spellKeys.Elements("water")
                //|| letters[4] == spellKeys.Elements("air")
                if (playerInput.actionBufferCastWindowBaseTimerTime >= playerInput.canCastEntry && playerInput.actionBufferCastWindowBaseTimerTime <= playerInput.canCastExit
                    && lettersCache[1] == spellKeys.Charm
                    && letters[2] == spellKeys.BaseSpells("F")
                    && letters[3] == spellKeys.BaseSpells("Y")
                    && (letters[4] == spellKeys.Elements("earth")
                    
                    || letters[4] == spellKeys.Elements("fire"))
                    && letters[5] == spellKeys.BeginCast)
                {
                    dischargedSpellcast.StopAoePlacement();
                    castingIcon.gameObject.SetActive(false);

                    dRRouter.CharmCount++;

                    playerInput.CastSpell(letters[1]);

                    playerInput.ResetTimerSquareAndLetterList();

                    playerMovement.EnterCastMovementSlow(5, 1);
                    Debug.LogFormat($"<color=red>ELEMENTAL BARRIER</color>");
                    return;
                }

                if (playerInput.actionBufferCastWindowBaseTimerTime >= playerInput.canCastEntry && playerInput.actionBufferCastWindowBaseTimerTime <= playerInput.canCastExit
                    && (lettersCache[1] == spellKeys.Charm
                    && letters[2] == spellKeys.BaseSpells("F")
                    && letters[3] == spellKeys.BaseSpells("Y")
                    && letters[4] == spellKeys.BaseSpells("B")
                    && (letters[5] == spellKeys.BeginCast)))
                {
                    dischargedSpellcast.StopAoePlacement();
                    castingIcon.gameObject.SetActive(false);

                    dRRouter.CharmCount++;

                    playerInput.CastSpell("Arcane Long Barrier");

                    playerInput.ResetTimerSquareAndLetterList();

                    playerMovement.EnterCastMovementSlow(5, 1);

                    return;
                }
                else if (lettersCache[1] == spellKeys.Charm
                    && letters[2] == spellKeys.BaseSpells("F")
                    && letters[3] == spellKeys.BaseSpells("Y")
                    && letters[4] == spellKeys.BaseSpells("B")

                    && (letters[5] == spellKeys.Elements("fire")
                    || letters[5] == spellKeys.Elements("earth")
                    || letters[5] == spellKeys.Elements("air")
                    || letters[5] == spellKeys.Elements("water")))
                {
                    ActionBufferQuickReset();

                    aoeCast.ChangeAoeLongBarrierElement(letters[5]);

                    InstantSpellBuffer();

                    SoundManager.Instance.PlayCastBuffer(castBufferSquare_SFX);

                    return;

                } else
                {
                    Debug.LogFormat($"<color=red>CASE 6: LONG BARRIER Charm Cancelled</color>");

                    playerMovement.EnterCastMovementSlow(5, 1);

                    // If player was casting charm aoe, this stops it
                    dischargedSpellcast.StopAoePlacement();

                    playerInput.ResetTimerSquareAndLetterList();

                    SoundManager.Instance.StopCastModeSound();
                    SoundManager.Instance.PlayCastUnsuccessful(castUnsuccessful_SFX);

                }



                break;

            case 7:

                if (lettersCache[1] == spellKeys.Charm
                    && letters[2] == spellKeys.BaseSpells("F")
                    && letters[3] == spellKeys.BaseSpells("Y")
                    && letters[4] == spellKeys.BaseSpells("B")

                    && (letters[5] == spellKeys.Elements("fire")
                    || letters[5] == spellKeys.Elements("earth")
                    || letters[5] == spellKeys.Elements("air")
                    || letters[5] == spellKeys.Elements("water"))
                    && (letters[6] == spellKeys.BeginCast))
                {
                    dischargedSpellcast.StopAoePlacement();
                    castingIcon.gameObject.SetActive(false);

                    playerInput.CastLongBarrier(letters[5]);

                    dRRouter.CharmCount++;

                    playerInput.ResetTimerSquareAndLetterList();

                    playerMovement.EnterCastMovementSlow(5, 1);

                    return;
                }
                else
                {
                    Debug.LogFormat($"<color=red>CASE 6: LONG BARRIER Charm Cancelled</color>");

                    playerMovement.EnterCastMovementSlow(5, 1);

                    // If player was casting charm aoe, this stops it
                    dischargedSpellcast.StopAoePlacement();

                    playerInput.ResetTimerSquareAndLetterList();

                    SoundManager.Instance.StopCastModeSound();
                    SoundManager.Instance.PlayCastUnsuccessful(castUnsuccessful_SFX);

                }

            break;

            case 8:
                Debug.LogFormat($"<color=red>CASE 8?</color>");
                if (playerInput.actionBufferCastWindowBaseTimerTime >= playerInput.canCastEntry && playerInput.actionBufferCastWindowBaseTimerTime <= playerInput.canCastExit
                    && (lettersCache[1] == spellKeys.Charm
                    && letters[2] == spellKeys.BaseSpells("F")
                    && letters[3] == spellKeys.BaseSpells("Y")
                    && letters[4] == spellKeys.BaseSpells("B")

                    && (letters[5] == spellKeys.Elements("fire")
                    || letters[5] == spellKeys.Elements("earth")
                    || letters[5] == spellKeys.Elements("air")
                    || letters[5] == spellKeys.Elements("water"))

                    && (letters[6] == spellKeys.BeginCast)))
                {
                    dischargedSpellcast.StopAoePlacement();
                    castingIcon.gameObject.SetActive(false);

                    // Figure out how to route the spellcasting
                    playerInput.CastSpell("Arcane Long Barrier");

                    playerInput.ResetTimerSquareAndLetterList();

                    playerMovement.EnterCastMovementSlow(5, 1);

                    return;
                }
                else
                {
                    Debug.LogFormat($"<color=red>CASE 6: LONG BARRIER Charm Cancelled</color>");

                    playerMovement.EnterCastMovementSlow(5, 1);

                    // If player was casting charm aoe, this stops it
                    dischargedSpellcast.StopAoePlacement();

                    playerInput.ResetTimerSquareAndLetterList();

                    SoundManager.Instance.StopCastModeSound();
                    SoundManager.Instance.PlayCastUnsuccessful(castUnsuccessful_SFX);

                }
            break;
        }
    }

    
}

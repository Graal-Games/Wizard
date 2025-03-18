using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DRActivationLogic : MonoBehaviour
{
    SpellKeys spellKeys;
    PlayerInput playerInput;
    DRRouter dRRouter;
    DischargedSpellcast dischargedSpellcast;
    DiminishingReturn diminishingReturn;
    Spellcasting spellcasting;
    LocalSpells localSpells;
    [SerializeField] GameObject localSpellsCastPoint;
    [SerializeField] DRTierObjectColorManager dRTierObjectColorManager;
    BarrierSpellCast barrierSpellCast;

    bool isSphereCastInterrupted;
    bool isAoeCastInterrupted = false;
    bool isCharmCastInterrupted = false;

    [SerializeField] private AudioClip castBufferSquare_SFX;
    [SerializeField] private AudioClip castMode_SFX;
    [SerializeField] private AudioClip castSuccessful_SFX;


    delegate DRActivationLogic.MethodDelegate MethodDelegate();

    void Awake()
    {
        playerInput = GetComponentInChildren<PlayerInput>();
        diminishingReturn = GetComponent<DiminishingReturn>();
        dischargedSpellcast = GetComponentInChildren<DischargedSpellcast>();
        spellKeys = GetComponentInChildren<SpellKeys>();
        dRRouter = GetComponent<DRRouter>();
        spellcasting = GetComponentInParent<Spellcasting>();
        localSpells = localSpellsCastPoint.GetComponent<LocalSpells>();

        barrierSpellCast = GetComponentInChildren<BarrierSpellCast>();

        Debug.LogFormat($"<color=black>barrierSpellCast: {barrierSpellCast}</color>");
    }

    

    //public class SpellVariablesPayload
    //{
    //    public bool isDRActive { get; set; }
    //    public int keysAmount { get; set; }
    //    public bool isLettersHidden { get; set; }
    //    public int DRactivationBeginsAt { get; set; }
    //}



    //// Not being used, can delete
    //SpellVariablesPayload BeamVariables(string spell)
    //{
    //    SpellVariablesPayload payload = new SpellVariablesPayload();
        
    //    payload.isDRActive = playerInput.isBeamDRActive;
    //    payload.keysAmount = 0;

    //    return payload;
    //}



    public bool IsSphereCastInterrupted
    {
        set { isSphereCastInterrupted = value; }
        get { return isSphereCastInterrupted; }
    }
    
    public bool IsAoeCastInterrupted
    {
        set { isAoeCastInterrupted = value; }
        get { return isAoeCastInterrupted; }
    }

    public bool IsCharmCastInterrupted
    {
        set { isCharmCastInterrupted = value; }
        get { return isCharmCastInterrupted; }
    }



    // I made this a switch because it might be used in the future
    //to route to a single method that handles the DR logic
    //with the appropriate variable relative to the spell used 
    public void DRActivationGate(string spellCategory, string spell = null, Vector3 aoeBarrier = default) //
    {
        //bool isDrActive = dRRouter.SpellCategoryDRCounter("Beam").drActive;
        // Check if the amount of times the beam was cast is equal to the amount
        //of times it is required to be cast to activate DR.
        // If they are equal, activate DR
        //** It is deactivated/ reset through timer [TO BE IMPLEMENTED]
        //** Additionally, currently we are checking for the first tier entry point
        //**but it should be eventually that each tier has its own entry point also (what??)


        // Turn this into a switch case where the method takes in a parameter with the name of the 
        //spell and handles all routing and gates accordingly

        switch(spellCategory)
        {
            case "Beam":
                BeamDRLogic();
                break;

            case "Aoe":
                //AoeDRLogic();
                break;

            case "Shield":
                ShieldDRLogic();
                break;

            case "Charm":
                CharmDRLogic(spell);
                break;

            case "Barrier":
                BarrierDRLogic(spell);
                break;

            // To add: Invoke, Summon

            default:
            break;
        }
        
        
    }


    void BeamDRLogic()
    {
        if ( playerInput.isBeamDRActive == false )
        {
            // Check here if the beamCount is equal to the next beam DR tier
            //if it is > activate the DR
            
            dischargedSpellcast.CastBeam();

            // bool that increments the count
            //for the specific spell

            dRRouter.BeamCount++;

            if (dRRouter.BeamCount >= dRRouter.BeamDRActiveCount)
            {
                playerInput.isBeamDRActive = true;

                dRTierObjectColorManager.SetBeamDRObjectTierColor("tier" + dRRouter.BeamCount.ToString());

                Debug.LogFormat($"<color=orange>Beam DR Accessed</color>");

                // ** Check if this line below is necessary
                return;
            } 

        } else {

            if (playerInput.isBeamDRActive && !playerInput.isCastingDRSequence)
            {
                // This method call generates the letter sequence for DR
                //and displays the letters on the ui. The nested method in the parameter
                //determines how many letters there should be, which varies
                // depending on how many times the spell has been cast.
                diminishingReturn.DRLockSequence(
                    dRRouter.SpellCategoryDRCounter("Beam").keysAmount);
            }
            //Debug.LogFormat($"<color=orange>{dRRouter.BeamCount} {dRRouter.BeamDRActiveCount}</color>");
        }
    }


    private void ShieldDRLogic()
    {
        // if ( playerInput.isShieldDRActive == false || isSphereCastInterrupted == true) 
        if ( playerInput.isShieldDRActive == false ) 
        {
            // Check if the beamCount is equal to the next beam DR tier
            //if it is > activate the DR

            // REACTIVATE THIS IF RE-IMPLEMENTING BUFFER
            //spellcasting.InstantSpellBuffer();
            localSpells.CastShield();
            dRRouter.SphereCount++;
            //playerInput.CastSpell("T");
            
            // if (localSpells.CastShield() == true)
            // {
            
            // SoundManager.Instance.PlaySound(castMode_SFX);
            //SoundManager.Instance.StopPlayCastMode();



            // if (dRRouter.ShieldCount >= dRRouter.ShieldDRActiveCount 
            // && isSphereCastInterrupted == false)
            if (dRRouter.SphereCount >= dRRouter.SphereDRActiveCount)
            {
                dRTierObjectColorManager.SetSphereDRObjectTierColor("tier" + dRRouter.SphereCount.ToString());
                //Debug.LogFormat("<color=red>isShieldDRActive</color>");
                playerInput.isShieldDRActive = true;
                IsSphereCastInterrupted = false; 
                // ** Check if this line below is necessary
                return;
            }

            IsSphereCastInterrupted = false;

        } else {
            
            if (playerInput.isShieldDRActive && !playerInput.isCastingDRSequence)
            {
                // This method call generates the letter sequence for DR
                //- and displays the letters on the ui - the nested method in the parameter
                //determines how many letters there should be depending on how many
                //times the spell has been cast.
                // >> spellcasting.ClearLettersAndDeactivateCastIcon();

                // diminishingReturn takes in a script for a parameter that handles
                //serving the correct DR tier, wherein the number
                //of letters that are presented to the player is specified
                diminishingReturn.DRLockSequence(
                    dRRouter.SpellCategoryDRCounter("Shield").keysAmount);
            }
            Debug.LogFormat($"<color=orange>{dRRouter.SphereCount} {dRRouter.SphereDRActiveCount}</color>");
            //Debug.LogFormat($"<color=orange>{isDrActive}</color>");
            
        }
    }


    public void CheckIfToActivateAoeDR()
    {
        if (dRRouter.AoeCount >= dRRouter.AoeDRActiveCount)
        {
            Debug.LogFormat($"<color=purple> >>aoe dr activated << {dRRouter.AoeCount} : {dRRouter.AoeDRActiveCount} </color>");
            playerInput.isAoeDRActive = true;
            //IsAoeCastInterrupted = false;
            //return;
        }
        IsAoeCastInterrupted = false;
        return;
    }

    public void ActivateAoeDRSequence()
    {
        if (playerInput.isAoeDRActive && !playerInput.isCastingDRSequence)
        {
            // This method call generates the letter sequence for DR
            //- and displays the letters on the ui - the nested method in the parameter
            //determines how many letters there should be depending on how many
            //times the spell has been cast.

            //spellcasting.ClearLettersAndDeactivateCastIcon();

            diminishingReturn.DRLockSequence(dRRouter.SpellCategoryDRCounter("Aoe").keysAmount);
        }
    }


//ClearLettersAndDeactivateCastIcon

// NEED TO FIX THIS SO THAT ONLY WHEN THE SPELL IS CAST DOES IT ACTIVATE
//AND ADD A COUNT TO HOW MANY AOES WERE CAST
    //private void AoeDRLogic()
    //{
    //    if ( playerInput.isAoeDRActive == false || IsAoeCastInterrupted)
    //    {
    //        //** Check here if the beamCount is equal to the next beam DR tier
    //        //** In fact, there should be a seperate script that checks for that
    //        //**so that all the spells can be routed through it
    //        // >> spellcasting.InstantSpellBuffer();
    //        // >> dischargedSpellcast.StartCastAoe("Arcane");
    //        // >> SoundManager.Instance.PlayCastBuffer(castBufferSquare_SFX);

    //        // DR Tier Object for this is in Player Input as the final cast is handled there
    //        // >> Debug.LogFormat($"<color=purple>dRRouter.AoeCount: {dRRouter.AoeCount} - dRRouter.AoeDRActiveCount: {dRRouter.AoeDRActiveCount}</color>");
    //        // >> if (dRRouter.AoeCount >= dRRouter.AoeDRActiveCount)
    //        // >> {
    //        // >>     Debug.LogFormat($"<color=purple> >>aoe dr activated << {dRRouter.AoeCount} : {dRRouter.AoeDRActiveCount} </color>");
    //        // >>     playerInput.isAoeDRActive = true;
    //        // >>     IsAoeCastInterrupted = false;
    //        // >>     return;
    //        // >> }
    //        // >> IsAoeCastInterrupted = false;

    //    } else {
            
    //        // Activate DR Lock sequence
    //       // >>if (playerInput.isAoeDRActive && !playerInput.isCastingDRSequence)
    //       // >>{
    //       //     // This method call generates the letter sequence for DR
    //       //     //- and displays the letters on the ui - the nested method in the parameter
    //       //     //determines how many letters there should be depending on how many
    //       //     //times the spell has been cast.
    //       // >>    spellcasting.ClearLettersAndDeactivateCastIcon();

    //       // >>    diminishingReturn.DRLockSequence( dRRouter.SpellCategoryDRCounter("Aoe").keysAmount );
    //       // >>}
    //       //>>Debug.LogFormat($"<color=orange>{dRRouter.AoeCount} {dRRouter.AoeDRActiveCount}</color>");
            
    //    }
    //}



    // This will need to be split later. The logic works but not when the actions are supposed to happen
    // Instead it will have to go like this:
    // a - on "R" Activate DR
    // b - on "P" Activate Buffer && StartCastAoe
    void CharmDRLogic(string spell = null)
    {
        if ( playerInput.isCharmDRActive == false || IsCharmCastInterrupted )
        {
            IsCharmCastInterrupted = false;
            //spellcasting.InstantSpellBuffer();

            // (**) Right now the logic is handling the charm as though
            // there is always a spell associated with the base spell
            /////// however, the base charm as it stands should have none
            /// >> dischargedSpellcast.StartCastAoe(spell);
            /// 
            dischargedSpellcast.StartCastAoe();

            //SoundManager.Instance.PlayCastBuffer(castBufferSquare_SFX);

            //Check first if the amount of casts required for the first tier has been reached
            if (dRRouter.CharmCount >= dRRouter.CharmDRActiveCount)
            {
                playerInput.isCharmDRActive = true;
                //Debug.LogFormat($"<color=black>playerInput.isAoeDRActiveplayerInput.isAoeDRActiveplayerInput.isAoeDRActive{playerInput.isAoeDRActive}</color>");
                //return;
            } 
             

        } else {
            

            if (playerInput.isCharmDRActive && !playerInput.isCastingDRSequence)
            {
                // This method call generates the letter sequence for DR
                //- and displays the letters on the ui - the nested method in the parameter
                //determines how many letters there should be depending on how many
                //times the spell has been cast.
                spellcasting.ClearLettersAndDeactivateCastIcon();
                diminishingReturn.DRLockSequence(
                    dRRouter.SpellCategoryDRCounter("Charm").keysAmount);
            }
            //Debug.LogFormat($"<color=orange>{dRRouter.AoeCount} {dRRouter.AoeDRActiveCount}</color>");
            
        }
    }



    void BarrierDRLogic(string spell = null)
    {
        if (playerInput.isBarrierDRActive == false)
        {
            barrierSpellCast.CastBarrier(spell);
           
            // Keep track of how many times the spell was cast for DR
            dRRouter.BarrierCount++;

            SoundManager.Instance.PlayCastSuccessful(castSuccessful_SFX);

            //Check first if the amount of casts required for the first tier has been reached
            if (dRRouter.BarrierCount >= dRRouter.BarrierDRActiveCount)
            {

                dRTierObjectColorManager.SetBarrierDRObjectTierColor("tier" + dRRouter.BarrierCount.ToString());

                playerInput.isBarrierDRActive = true;
            }


        }
        else
        {
            if (playerInput.isBarrierDRActive && !playerInput.isCastingDRSequence)
            {
                // This method call generates the letter sequence for DR
                //- and displays the letters on the ui - the nested method in the parameter
                //determines how many letters there should be depending on how many
                //times the spell has been cast.
                spellcasting.ClearLettersAndDeactivateCastIcon();
                diminishingReturn.DRLockSequence(
                    dRRouter.SpellCategoryDRCounter("Barrier").keysAmount);
            }
        }
    }
}

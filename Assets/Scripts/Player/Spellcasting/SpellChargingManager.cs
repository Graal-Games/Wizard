using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellChargingManager
{
    private K_SpellLauncher spellLauncher;

    private K_SpellBuilder spellBuilder;
    public SpellChargingManager(K_SpellLauncher spellLauncher, K_SpellBuilder spellBuilder)
    {
        this.spellLauncher = spellLauncher;
        this.spellBuilder = spellBuilder;
    }


    // This 
    // private bool isSpellChargingType(KeyCode spellType)
    // {
    //     if (spellLauncher.CurrentSpellType.ToString() == "B" ||
    //         spellLauncher.CurrentSpellType.ToString() == "R")
    //     {
    //         // If the spell type is bean spell, then it is a Spell Charging type
    //         return true;
    //     }
    //     // Currently only bean spell is ChargingType
    //     return false;
    // }

    private bool isSpellChargingType(string spellType)
    {
        string periCastLockProcedure = spellBuilder.GetPeriCastLockProcedure(spellLauncher.g_CurrentSpellSequence);

        switch (periCastLockProcedure)
        {
            case "Charging":
                return true;
            case "None":
                return false;
            default:
                Debug.LogError($"<color=red>SpellChargingManager > Unknown periCastLockProcedure: {periCastLockProcedure}</color>");
                return false;
        }

    }


    /// <summary>
    /// Checks if the user should be presented with a Spell Charging instance, and displays
    /// the Spell Charging instance on his UI if so.
    /// </summary>
    public void HandleSpellChargingActivation()
    {
        /// <summary>
        /// spellLauncher.SpellSequence.Length != 0

        /// Checks if the spell sequence is not empty (i.e., the player has already started entering a spell sequence).
        /// If true, the function exits early (return;), so spell charging will not be activated if a spell 
        /// is already being built.
        /// 
        /// !isSpellChargingType(spellLauncher.CurrentSpellType)
        /// Calls a helper function to check if the current spell type is a "charging" type 
        /// (for example, a spell that requires holding or charging up).
        /// If the current spell type is not a charging type, the function exits early.
        /// </summary>
        if (spellLauncher.SpellSequence.Length != 0 ||
            !isSpellChargingType(spellLauncher.g_CurrentSpellSequence))
        {
            return;
        }

        Debug.LogFormat($"<color=orange>SpellChargingManager > Activate SpellCharging! </color>");

        ActivateSpellChargingKeys(2); // To manually pass a parameter for a variable number of keys

        spellLauncher.IsInSpellChargingMode = true;
        spellLauncher.InSpellCastModeOrWaitingSpellCategory = false;

        // handle parry letters generation here
        spellLauncher.ParryLetters.Value = "R";

        // Add the button pressed to the spell sequence (the spell's existance is checked thereafter)
        spellLauncher.SpellSequence += spellLauncher.CurrentSpellType.ToString();

        // This writes the spell string sequence input on the top left corner of the screen
        spellLauncher.SpellText.text = K_SpellKeys.cast.ToString() + spellLauncher.SpellSequence;


        // Check if the spell (spell sequence) exists. If not,
        // reset the sequence, the spellText & the casting status
        if (!spellLauncher.spellBuilder.SpellExists(spellLauncher.SpellSequence))
        {
            //Debug.LogFormat($"<color=red> Spell does not exist </color>");
            Debug.LogFormat($"<color=red> SPELL DOES NOT EXIST </color>");

            // Note: No need to cancel anim here since the anim is not active here yet
            spellLauncher.StopCastBuffer();
            //StopCastBufferAnimationIfActive();

            return;
        }

        // If the animation is already active, deactivate it first
        if (spellLauncher.CastKey.Anim.GetCurrentAnimatorStateInfo(0).IsName("BufferOnce"))
        {
            Debug.LogFormat($"<color=red> Anim is playing > Stop playing </color>");

            // The animation is not currently playing, so start it ?
            spellLauncher.CastKey.StopCastBufferAnim();
        }

        spellLauncher.InitCastProcedure();
    }

    private void ActivateSpellChargingKeys(int number)
    {
        Debug.LogFormat($"<color=orange>SpellChargingManager > Start loading UI keyData </color>");

        spellLauncher.spellChargingKeysQueue = spellLauncher.spellBuilder.GetSpellChargingKeys(number);
        int keyCount = spellLauncher.spellChargingKeysQueue.Count;

        int activatedKeys = 0;

        for (int i = 0; i < keyCount; i++)
        {
            K_DRKeyData spellChargingKeyData = spellLauncher.GetElementAtSpellChargingKeysQueue(i);
            K_DRKey scKey = spellLauncher.spellChargingUiKeysDictionary[spellChargingKeyData.keyCode.ToString()];

            scKey.invisible = spellChargingKeyData.invisible;
            scKey.buffered = spellChargingKeyData.buffered;
            scKey.gameObject.SetActive(true);

            scKey.SetActive(true);
            activatedKeys++;
        }

        for (int i = 0; i < activatedKeys; i++)
            spellLauncher.spellChargingKeysQueue.Dequeue();

    }

    public void DeactivateSpellChargingMode()
    {
        // Deactivate all active SpellCharging keys
        foreach (var sckey in spellLauncher.spellChargingUiKeysDictionary.Values)
        {
            if (sckey.gameObject.activeSelf == true)
            {
                sckey.gameObject.SetActive(false);
            }
        }

        // Exit SpellCharging Mode
        spellLauncher.IsInSpellChargingMode = false;
    }

    /// <summary>
    /// Reads player key presses, checks if the SpellCharging has been solved and
    /// if so, completes the SpellCharging. Then it goes diretly to fire the spell.
    /// </summary>
    public void HandleSpellChargingInput()
    {
        KeyCode spellChargingKeyPressed = SpellChargingKeyPressed();


        // if spell charging interrupted
        if (spellChargingKeyPressed != KeyCode.None || Input.GetKeyDown(K_SpellKeys.cast))
        {

            // During SpellCharging : If the player presses the cast key or any other key that is not present
            //in the SpellCharging instance presented on his UI, deactivate all the SpellCharging letters and exit SpellCharging Mode 
            if (Input.GetKeyDown(K_SpellKeys.cast) || (spellLauncher.spellChargingUiKeysDictionary.Keys.Contains(spellChargingKeyPressed.ToString()) && !spellLauncher.spellChargingUiKeysDictionary[spellChargingKeyPressed.ToString()].isActiveAndEnabled))
            {
                Debug.LogFormat($"<color=orange>SpellChargingManager > SpellCharging interrupted & failed </color>");

                DeactivateSpellChargingMode();

                // Reset the sequence and the cast animation
                spellLauncher.ResetSpellSequence(); // To revise > Could be made more efficient

                return;
            }

            // Check that the key pressed exists in the predefined SpellCharging key dictionary
            if (spellLauncher.spellChargingUiKeysDictionary.Keys.Contains(spellChargingKeyPressed.ToString()))
            {
              
                Debug.LogFormat($"<color=orange>SpellChargingManager > SpellCharging Try to Solve {spellChargingKeyPressed.ToString()} </color>");
                // Save the gameObject that is associated with the key pressed's class to this variable
                K_DRKey spellChargingKey = spellLauncher.spellChargingUiKeysDictionary[spellChargingKeyPressed.ToString()];

                // If the SpellCharging key's gameObject (letter on the UI) is active and the player is able to solve for it
                // deactivate that specific gameObject
                if (spellChargingKey.gameObject.activeSelf && spellChargingKey.TrySolve(spellChargingKeyPressed))
                {
                    // The gameObject on the UI is deactivated
                    spellChargingKey.gameObject.SetActive(false);
                }
            }

            // Check if all the SpellCharging letters have been solved
            if (IsSpellChargingSolved())
            {
                Debug.LogFormat($"<color=orange>SpellChargingManager > SpellCharging Solved! </color>");
                // If all SpellCharging letters have been solved exit the player from SpellCharging mode
                spellLauncher.IsInSpellChargingMode = false;
                spellLauncher.InSpellCastModeOrWaitingSpellCategory = true;
            }
        }
        else
        {

            // If another spell sequence is triyng to be cast will enter here
            KeyCode key = spellLauncher.SpellKeyPressed();
            if (key != KeyCode.None)
            {

                // handle parry letters generation here
                spellLauncher.ParryLetters.Value = "R";

                // This is for elements??
                // Add the button pressed to the spell sequence (the spell's existance is checked thereafter)
                spellLauncher.SpellSequence += key.ToString();

                // This writes the spell string sequence input on the top left corner of the screen
                spellLauncher.SpellText.text = K_SpellKeys.cast.ToString() + spellLauncher.SpellSequence;

                DeactivateSpellChargingMode();

                // Check if the spell (spell sequence) exists. If not,
                // reset the sequence, the spellText & the casting status
                if (!spellLauncher.spellBuilder.SpellExists(spellLauncher.SpellSequence))
                {
                    Debug.LogFormat($"<color=orange>SpellChargingManager > Stop SpellCharging spell sequence do not exists </color>");
                    spellLauncher.StopCastBuffer();
                    return;
                }

                Debug.LogFormat($"<color=orange>SpellChargingManager > Reset SpellCharging new spell sequence </color>");
                ActivateSpellChargingKeys(3);

                // If the animation is already active, deactivate it first
                if (spellLauncher.CastKey.Anim.GetCurrentAnimatorStateInfo(0).IsName("BufferOnce"))
                {
                    // The animation is not currently playing, so start it ?
                    spellLauncher.CastKey.StopCastBufferAnim();
                }

                spellLauncher.InitCastProcedure();
            }
        }
    }

    /// <summary>
    /// Checks if a key for SpellCharging has been pressed this frame and
    /// returns the corresponding KeyCode if so.
    /// </summary>
    /// <returns>The KeyCode of the SpellCharging key pressed this frame or
    /// KeyCode.None if no SpellCharging key was pressed.</returns>
    private KeyCode SpellChargingKeyPressed()
    {
        foreach (KeyCode key in K_SpellKeys.spellTypes)
        {
            if (Input.GetKeyUp(key))
                return key;
        }

        return KeyCode.None;
    }

    /// <summary>
    /// Checks if the SpellCharging has been solved by checking the active
    /// state of all the SpellCharging keys.
    /// </summary>
    /// <returns></returns>
    private bool IsSpellChargingSolved()
    {

        foreach (K_DRKey spellChargingKey in spellLauncher.spellChargingUiKeysDictionary.Values)
        {
            // if there is still a SpellCharging key active on the UI (visible) keep the player in SpellCharging (unlock) mode
            if (spellChargingKey.gameObject.activeSelf)
            {
                Debug.LogFormat($"<color=orange>SpellChargingManager > IsSpellChargingKeySolved + keyCode {spellChargingKey.gameObject} </color>");
                return false;
            }
        }

        return true;
    }

}

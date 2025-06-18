using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

public class DiminishingReturn : NetworkBehaviour
{
    private List<GameObject> currentDRKeys;
    private List<GameObject> baseSpellsAuxiliaryKeys;
    GameObject randomKey1;
    GameObject randomKey2;
    bool isCastingDRSequence;
    UiReferences uiReferences;
    bool isBeamDRActive;
    PlayerInput playerInput;
    GameObject[] ChanneledCastingKeys_Tier_1;
    int correctInput = 0;
    bool isAnyActive = true;
    HashSet<int> usedIndexes;
    HashSet<int> usedIndexesCopy = new HashSet<int>();
    IMovementEffects movementEffects;
    bool gateOmitFirstInputString = true;

    DRActivationLogic dRActivationLogic;


    void Awake()
    {
        playerInput = GetComponentInChildren<PlayerInput>();
        uiReferences = GetComponent<UiReferences>();
        movementEffects = GetComponent<IMovementEffects>();
        dRActivationLogic = GetComponent<DRActivationLogic>();
    }



    void Start()
    {
        ChanneledCastingKeys_Tier_1 = uiReferences.ChanneledCastingKeys_Tier_1.Values.ToArray();
    }



    // This handles cancelling the spell if its DR sequence active
    public void DeactivateCurrentDRKeys()
    {
        foreach (int index in usedIndexes)
        {
            GameObject gameObject = ChanneledCastingKeys_Tier_1[index];
            usedIndexes = null;
            gameObject.SetActive(false);
        }
    }



#region timer
        // Edit the variables
    // void DiminishingReturn()
    // {

    //     timer += Time.deltaTime;

    //     if (timer >= canCastEntry && timer <= canCastExit)
    //     {
    //         //print(timer);
    //         //print("CAST NOW!");
    //     } else if (timer > 1.6f){
    //         print("Timer reset!");
    //         timer = 0;
    //         letters.Clear();
    //         preCastWindowWarning = false;
    //         castingSquare.gameObject.SetActive(false);
    //     }
    // }
#endregion



    // This one handles input when DR sequence is active
    public void DRLock()
    {
        // Store the key pressed
        string keyPressed = Input.inputString.ToUpper();

        // Ignore the first letter pressed
        if (gateOmitFirstInputString)
        {
            keyPressed = string.Empty;
            gateOmitFirstInputString = false;
            return;
        }

        // If any of the letters (gameobjects) presented during DR
        //are NOT active. Stop the DR sequence.
        if (!isAnyActive)
        {
            // ?? Do the gates reset here need to be specific only to the currently active one?
            // These booleans are used primarily in the DRactivationLogic script to check if
            //the player can cast or should be given a DR lock sequence
            playerInput.isBeamDRActive = false; // This equals to false in 2 places in this script - Needed?
            playerInput.isAoeDRActive = false; // This equals to false in 2 places in this script - Needed?
            playerInput.isShieldDRActive = false; 
            playerInput.isCastingDRSequence = false;

            dRActivationLogic.IsAoeCastInterrupted = false;

            gateOmitFirstInputString = true;

            correctInput = 0;

        }

        // When a key is pressed, iterate through the stored indexes of each letter gameobject
        foreach (int index in usedIndexes)
        {
            // Assuming you have a list of game objects named "gameObjectsList"
            if (index >= 0 && index < ChanneledCastingKeys_Tier_1.Length)
            {
                if (ChanneledCastingKeys_Tier_1[index].activeSelf == true && keyPressed == ChanneledCastingKeys_Tier_1[index].name)
                {
                    // Store the associate letter's Gameobject to deactivate it after
                    //the check for if all the letter gos are deactivated
                    GameObject gameObject = ChanneledCastingKeys_Tier_1[index];

                    // This is used to check if the amount of correct inputs is equal to the amount of
                    //letters initaially activated

                    correctInput++;
                    
                    // If all the letters have been input correctly
                    //deactive the DR sequence
                    if (correctInput == usedIndexes.Count)
                    {
                        isAnyActive = false;

                        movementEffects.EnterCastMovementSlow(5, 1);

                        // Do the gates reset here need to be specific to the currently active one?
                        playerInput.isBeamDRActive = false;
                        //playerInput.isShieldDRActive = false;
                        playerInput.isAoeDRActive = false;
                        playerInput.isCharmDRActive = false;
                        playerInput.isBarrierDRActive = false;
                    }

                    gameObject.SetActive(false);
                }
            }
        }
    }



    void RemoveLetterHashIndex(int index)
    {
        usedIndexes.Remove(index);
    }



    // Handles which letters to show on Ui during a DR lock dequence
    // Takes a number for the amount of letters that are to be shown on the Ui
    // This is called first, the method above it is called second
    public void DRLockSequence(int number)
    {
        gateOmitFirstInputString = true;
        correctInput = 0;
        //if (usedIndexes != null || usedIndexes.Count != 0)
        //{
        //    usedIndexes.Clear();
        //} 
        //Debug.LogFormat($"<color=green>DRLockSequence ENTRY</color>");
        // I don't think this first check is doing anything
        if (currentDRKeys == null || currentDRKeys.Count == 0)
        {
            //Debug.LogFormat($"<color=green>DRLockSequence FIRST IF</color>");
            usedIndexes = new HashSet<int>();

            //Debug.LogFormat($"<color=red>usedIndexes: {usedIndexes.Count()}</color>");

            for (int i = 0; i < number; i++)
            {
                //Debug.LogFormat($"<color=green>DRLockSequence FOR LOOP ENTRY</color>");
                int index;
                do
                {
                    //Debug.LogFormat($"<color=green>DRLockSequence DO LOOP ENTRY</color>");
                    index = Random.Range(0, ChanneledCastingKeys_Tier_1.Length);
                } while (usedIndexes.Contains(index));

                //Debug.LogFormat($"<color=green>DRLockSequence DO LOOP EXIT</color>");
                usedIndexes.Add(index);
            }
        
            foreach (int index in usedIndexes)
            {
                //Debug.LogFormat($"<color=green>DRLockSequence ENTRY</color>");
                // Assuming you have a list of game objects named "gameObjectsList"
                if (index >= 0 && index < ChanneledCastingKeys_Tier_1.Length)
                {
                    GameObject gameObject = ChanneledCastingKeys_Tier_1[index];
                    gameObject.SetActive(true);
                }
            }

            // (!) This does not look like it's being used
            usedIndexesCopy = usedIndexes;

            // After the letters are generated. Check if the DRSequence had been activated
            //if not. Activate it.
            if (isCastingDRSequence == false )
            {
                playerInput.isCastingDRSequence = true;
                isAnyActive = true;
                return;
            }

            return;
        }
    }
}

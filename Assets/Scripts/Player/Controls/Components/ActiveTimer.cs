using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;


// Can rename this later
// ActiveTimerController
// ActiveTimerComponent
// ActiveTimerModule

public class ActiveTimer : MonoBehaviour
{
    [Header("Timer Variables")]
    // Cast window values
    // ** To encapsulate
    public float canCastEntryTime = 1.4f; // Correct-timing-for-input window opens
    public float canCastExitTime = 1.6f; // Correct-timing-for-input window closes
    public float mainTimerTime = 0f; // Timer's main time value

    [Header("Imported Scripts")]
    PlayerInput playerInput;
    DischargedSpellcast dischargedSpellcast;


    [SerializeField] GameObject dischargeSpellsCastPoint;


    [Header("Referenced Game Objects")]
    [SerializeField] GameObject gBackground;

    [Header("Other Variables")]
    RawImage castingSquare;


    private AudioClip castUnsuccessful_SFX;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        dischargedSpellcast = dischargeSpellsCastPoint.GetComponent<DischargedSpellcast>();
    }

    private void Start()
    {
        castingSquare = gBackground.GetComponent<RawImage>();
        castingSquare.gameObject.SetActive(false);
    }

    //bool ReturnAoe(bool isAoeDRActiveParam)
    //{

    //    return isAoeDRActive = false;
    //}

    public void InstantSpellsBufferBehavior2()
    {
        // Once the cast square is shown, check
        // if the ability to cast has reached the time limit
        // and reset the timer and deactivate the square.

        mainTimerTime += Time.deltaTime;

        if (mainTimerTime >= canCastEntryTime
         && mainTimerTime <= canCastExitTime
         && playerInput.actionBufferActiveGate == true) // ** To rename preCastWindowWarning to something else, perhaps isActiveTimerModuleStart? Localize?
        {
            // To migrate this to KeyUi ?
            castingSquare.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(110, 110);

        }
        else if (mainTimerTime > 1.6f)
        {
            dischargedSpellcast.StopAoePlacement();

            SoundManager.Instance.PlayCastSuccessful(castUnsuccessful_SFX);


            playerInput.actionBufferActiveGate = false;
            mainTimerTime = 0;
            //ReturnAoe(isAoeDRActive);

            // This prevents the player from having to reintroduce DR sequence to unlock the AoE spell cat.
            playerInput.isAoeDRActive = false;

            // Check if this is fine - Last seemed like it was the cause of some bugs
            playerInput.ResetTimerSquareAndLetterList();

        }
        else // !? What the hell is this for?
        {
            return;
        }
    }
}

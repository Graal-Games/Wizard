// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class InstantSpellsBuffer : MonoBehaviour
// {
//     PlayerInput playerInput;

//     float timer = 0f;
//     float canCastEntry = 1.4f;
//     float canCastExit = 1.6f;

//     void Awake()
//     {
//         playerInput = GetComponent<PlayerInput>();
//     }

//     void CastTimer()
//     {
//         // Once the cast square is shown, check
//         // if the ability to cast has reached the time limit
//         // and reset the timer and deactivate the square.

//         timer += Time.deltaTime;

//         if (timer >= canCastEntry 
//          && timer <= canCastExit 
//          && playerInput.preCastWindowWarning == true)
//         {
//             castingSquare.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2 (110, 110);

//         } else if (timer > 1.6f){
//             dischargedSpellcast.StopAoePlacement();
//             ResetTimerSquareAndLetterList();
//         } else {
//             return;
//         }

//     }
// }

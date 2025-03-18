using IncapacitationEffect;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using static GameInput;

public class PlayerSpellController : NetworkBehaviour
{
    [SerializeField] private SpellUnlockManager spellUnlockManager;
    [SerializeField] private SpellCastManager spellCastManager;

    private enum State
    {
        Idle,
        WaitingSpellCategory,
        SpellUnlockState,
        SpellCastState,
        SpellFiringState,
    }

    public enum SpellCategory
    {
        Sphere,
        Barrier,
        Invoke,
        Beam,
        Projectile,
        Aoe,
        Conjure,
        Charm
    }

    public enum TransmutationCategory
    {
        Arcane,
        Water,
        Earth,
        Fire,
        Air
    }

    private State state = State.Idle;
    private SpellCategory? currentSpellCategory = null;
    private TransmutationCategory currentTransmutationCategory = TransmutationCategory.Arcane;

    /*public override void OnNetworkSpawn()
    {
        GameInput.Instance.OnSpellKeyAction += GameInput_OnSpellKeyAction;
    }*/

    /*private void GameInput_OnSpellKeyAction(object sender, GameInput.OnSpellKeyEventArgs onSpellKeyEventArgs)
    {
        HandleSpellManagerState(onSpellKeyEventArgs.binding);
    }*/

    /*private SpellCategory? GetSpellCategoryBySpellInputKey(GameInput.Binding spellKey)
    {
        switch (spellKey)
        {
            case Binding.Sphere:
                return SpellCategory.Sphere;
            case Binding.Barrier:
                return SpellCategory.Barrier;
            case Binding.Invoke:
                return SpellCategory.Invoke;
            case Binding.Beam:
                return SpellCategory.Beam;
            case Binding.Projectile:
                return SpellCategory.Projectile;
            case Binding.Aoe:
                return SpellCategory.Aoe;
            case Binding.Conjure:
                return SpellCategory.Conjure;
            case Binding.Charm:
                return SpellCategory.Charm;
            default:
                return null;
        }
    }*/

    private void HandleSpellManagerState() {
        String FUNC = "HandleSpellManagerState";

        //bool isCastKeyPressed = spellInputKey.Equals(Binding.Cast);
        bool isCastKeyPressed = false;


        Debug.Log($"{FUNC}, Begins - state: {state}, isCastKeyPressed: {isCastKeyPressed}");

        switch (state)
        {
            case State.Idle:
                if (isCastKeyPressed) {
                    // start cast, wait for spell category selection.   
                    state = State.WaitingSpellCategory;
                    break;
                }

                // mapped key press but in Idle
                break;

            case State.WaitingSpellCategory:

                // Check if Spell Category was selected correctly.
                //this.currentSpellCategory = GetSpellCategoryBySpellInputKey(spellInputKey);
                if (this.currentSpellCategory == null)
                {
                    // Already waiting for spell category selection, go back to Idle.
                    state = State.Idle;
                    break;
                }

                Debug.Log($"{FUNC}, Spell Category Selected: {this.currentSpellCategory}");

                if (isCurrentSpellCategoryLocked())
                {
                    state = State.SpellUnlockState;
                    spellUnlockManager.StartUnlock();
                }
                else
                {
                    state = State.SpellCastState;
                    // Cast animation starts
                    spellCastManager.StartCast();
                }
                break;

            case State.SpellUnlockState:
                /* Check if Spell Category was selected correctly.
                UnlockKey? unlockKey = GetUnlockKeyBySpellInputKey(spellInputKey);
                if (unlockKey == null)
                {
                    // Spell Unlock is active, dimiss and go back to idle.  
                    spellUnlockManager.Dismiss();
                    state = State.Idle;
                    break;
                }

                // Spell Unlock is active, try to solve unlock key.  
                Debug.LogFormat($"{FUNC}, TrySolveUnlockKey()");
                spellUnlockManager.TrySolveUnlockKey(spellInputKey);*/
                break;

            case State.SpellCastState:

                /*if (isUnmappedKeyPressed) {
                    state = State.Idle;
                    break;
                }*/

                if (isCastKeyPressed)
                {
                    // Cast in progress, try to fire spell.
                    if (spellCastManager.TryFireSpell())
                    {
                        state = State.SpellFiringState;
                    }
                    else
                    {
                        state = State.Idle;
                    }
                    break;
                }

                // chose Element
                // Cast in progress try to use spellKey to transmute Spell.
                Debug.LogFormat($"{FUNC}, TryTransmuteSpell()");

                // Refresh cast animation 
                spellCastManager.TryTransmuteSpell();
                break;

            case State.SpellFiringState:
                if (isCastKeyPressed)
                {
                    // might be stop firing spell or interrup? 
                }

                // in case we want to add behaviour during spell firing 
                break;
        }

        Debug.Log($"{FUNC}, ens state: {state}");
    }

    private bool isCurrentSpellCategoryLocked()
    {
        // todo check if locked 
        return true;
    }

    private void Update()
    {
        switch (state)
        {
            case State.Idle:
                // do nothing with Unmapped keys 
                break;

            case State.WaitingSpellCategory:

                // handle Unmapped keys 

                break;

            case State.SpellUnlockState:

                // 1- Check if it is unlock key, so try to unlock 

                // 2- Check if it is mapped key, if unmmaped dismiss

                // 3- Check if mapped key is unrelated to unlock system but can be used async for  example movement, if not dissmiss

                // 4- if unrelated but valid do nothing

              
                /*if (unlockKey == null)
                {
                    // Spell Unlock is active, dimiss and go back to idle.  
                    spellUnlockManager.Dismiss();
                    state = State.Idle;
                    break;
                }*/

                // Spell Unlock is active, try to solve unlock key.  
                //Debug.LogFormat($"{FUNC}, TrySolveUnlockKey()");
                //spellUnlockManager.TrySolveUnlockKey(spellInputKey);
                break;

            case State.SpellCastState:

                // handle Unmapped keys 

                /*if (isUnmappedKeyPressed) {
                    state = State.Idle;
                    break;
                }*/
                break;

            case State.SpellFiringState:
                // Do nothing
                break;
        }
    }
}

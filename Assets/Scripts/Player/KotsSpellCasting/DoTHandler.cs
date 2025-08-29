using DamageOverTimeEffect;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DoTHandler : NetworkBehaviour
{
    public List<DamageOverTime> currentDamageOverTimeList = new List<DamageOverTime>();

    private void FixedUpdate()
    {
        if (currentDamageOverTimeList.Count > 0)
        {
            ApplyDoTOnPlayer();
        }
    }

    void ApplyDoTOnPlayer()
    {
        // Have the timer method here with the dictionary being iterated over handled by the class
        // Check if the player is inflicted with a DoT effect - Check if a DoT effect has been added to the DoT effects list
        //if a list entry exists then the player would take DoT damage

            // Iterate through the DoT spells the player character is currently affected by
            for (int i = currentDamageOverTimeList.Count - 1; i >= 0; i--)
            {
                // Get the instance of each of the DoT effect
                var dot = currentDamageOverTimeList[i];

                // Check if the spell duration has expired
                if (dot.TimeExpired)
                {
                    UnityEngine.Debug.LogFormat($"<color=purple>DOT EXPIRED</color>");

                    // If spell duration has expired, remove the DoT effect instance
                    currentDamageOverTimeList.RemoveAt(i);
                    return;
                }
                else
                {
                    // If the spell duration has not yet expired (above)
                    // The method returns 'true' at a specified (per second) time interval and applies damage
                    if (dot.Timer())
                    {
                        UnityEngine.Debug.LogFormat($"<color=purple>DOT APPLY DAMAGE</color>");

                        // Apply damage to the player
                        gameObject.GetComponent<NewPlayerBehavior>().HealthBar.ApplyDamage(dot.DamagePerSecond);

                        // Activating the blood shader for AoE doesn't work the same way when it is to be fired in succession
                        //if (shaderActivation != null) shaderActivation(OwnerClientId, "Blood", 1);
                        // DebuffController.DebuffController cont = new DebuffController.DebuffController(_healthBar.ApplyDamage(dot.DamagePerSecond));
                    }
                }
            }
        }
    }

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DotTimers
{
    public class DefaultDotTimer
    {
        float startTime = Time.time;
        float applyDamageAtInterval;
        float dotDamage;
        float dotPersistanceDuration;
        float defaultDotPersistanceDuration;
        float playerHealth;
        bool isDotPersistent = true;
        float durationEnds;
        PlayerBehavior playerBehaviorScript;
        HealthBar healthBar;
        public bool isInteractingWithSpell;
        float postInteractionPersistenceTime;
        bool isSpellDestroyed;


        private float persistentDotTimer = 0.0f;
        private float persistentDotDuration = 5.0f; // 5 seconds

        private bool hasNewSavedTime = false;

        public bool IsSpellDestroyed
        {
            get { return isSpellDestroyed; }
            set { isSpellDestroyed = value; }
        }

        public bool IsInteractingWithSpell
        {
            get { return isInteractingWithSpell; }
            set { isInteractingWithSpell = value; }
        }

        public bool IsDotPersistent
        {
            get { return isDotPersistent; }
            set { isDotPersistent = value; }
        }



        public DefaultDotTimer(float applyDamageAtInterval, float dotDamage, float dotPersistanceDuration, float postInteractionPersistenceTime, bool isInteractingWithSpell, PlayerBehavior playerBehaviorScript, HealthBar healthBar, float startTime)
        {
            this.applyDamageAtInterval = applyDamageAtInterval;
            this.dotDamage = dotDamage;
            this.dotPersistanceDuration = dotPersistanceDuration;
            this.playerHealth = playerBehaviorScript.health.Value;
            this.healthBar = healthBar;
            this.postInteractionPersistenceTime = postInteractionPersistenceTime;
            this.isInteractingWithSpell = isInteractingWithSpell;
            this.startTime = startTime;
            defaultDotPersistanceDuration = this.dotPersistanceDuration;
            this.playerBehaviorScript = playerBehaviorScript;
        }



        public bool DirectDotDamage()
        {
            float currentTime = Time.time;

            hasNewSavedTime = false;

            // not sure this needs to be in the conditional
            if (isInteractingWithSpell)
            {
                dotPersistanceDuration -= currentTime;
                // Check if the duration has elapsed
                if (currentTime - startTime >= applyDamageAtInterval)
                {
                    //Debug.LogFormat($"<color=red>damage applied</color>");
                    //Debug.LogFormat($"<color=green>currentTime: {currentTime}  -  startTime:{startTime} - applyDamageAtInterval: {applyDamageAtInterval}</color>");
                    //Debug.LogFormat($"<color=green>playerHealth: {playerHealth}  -  dotDamage:{dotDamage} - applyDamageAtInterval: {applyDamageAtInterval}</color>");
                    //Debug.LogFormat($"<color=green>playerBehaviorScript: {playerBehaviorScript.health.Value}</color>");

                    playerBehaviorScript.health.Value -= dotDamage;
                    //healthBar.SetHealth(playerBehaviorScript.health.Value);
                    startTime = currentTime;
                return true; // Timer has completed
                }
            } else
            {
                isDotPersistent = false;
            }

            return false; // Timer still active
        }


        public void PersistentDotDamage()
        {
            float currentTime = Time.time;

            persistentDotTimer = currentTime;

            // >> Debug.LogFormat($"<color=red>currentTime {currentTime} - startTime { startTime} - persistentDotTimer {persistentDotTimer} - persistentDotDuration {persistentDotDuration}</color>");

            if (hasNewSavedTime == false)
            {
                // Save the current time
                //startTime = currentTime;
                //persistentDotDuration += currentTime;
                // Set the flag to indicate that time has been saved
                persistentDotDuration = currentTime + 5;
                hasNewSavedTime = true;

                // You can now use savedTime for any further calculations or comparisons
                // >> Debug.Log($"hasNewSavedTime {hasNewSavedTime} + Time saved:  + startTime");
            }

            if (currentTime - startTime >= applyDamageAtInterval)
            {
                playerBehaviorScript.health.Value -= dotDamage;
                //healthBar.SetHealth(playerBehaviorScript.health.Value);
                startTime = currentTime;
            }

            // Check if the timer has reached its desired duration (5 seconds)
            if (persistentDotTimer >= persistentDotDuration)
            {
                // The timer has reached 5 seconds
                // >> Debug.Log("Timer has reached 5 seconds.");

                //playerBehaviorScript.IsInteractingWithDotSpell = false;
                // You can perform any actions you need to do when the timer is done here.
                IsDotPersistent = false;
                playerBehaviorScript.IsDotPersistent = false;
                //dotPersistanceDuration = 0;
                //applyDamageAtInterval = 0;
                // Optionally, you can reset the timer if needed
                // timer = 0.0f;
                return;
            }
        }
    }
}


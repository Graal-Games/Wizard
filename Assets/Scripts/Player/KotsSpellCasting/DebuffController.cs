using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DamageOverTimeEffect;
using System;

namespace DebuffEffect
{
    public class DebuffController
    {

        string debuff;
        float damagePerSecond;
        float spellDuration;

        float elapsedTime = 0f;
        bool timeExpired = false;
        bool dealDamage = true;
        int seconds;
        float timer; // Timer to accumulate time
        float incapacitationDuration;

        public string Debuff
        {
            get { return debuff; }
            private set { debuff = value; }
        }

        public float IncapacitationDuration
        {
            get { return incapacitationDuration; }
            set { incapacitationDuration = value; }
        }

        public float DamagePerSecond
        {
            get { return damagePerSecond; }
            private set { damagePerSecond = value; }
        }

        public float SpellDuration
        {
            get { return spellDuration; }
            private set { spellDuration = value; }
        }

        public float ElapsedTime
        {
            get { return elapsedTime; }
            private set { elapsedTime = value; }
        }

        public bool TimeExpired
        {
            get { return timeExpired; }
            private set { timeExpired = value; }
        }


        //// CONSTRUCTOR
        //public DebuffController(float duration)
        //{
        //    Debuff = debuff;
        //    SpellDuration = duration;
        //}

        public virtual bool Timer() // should be activated using parameters that determine its lifetime and the item in the list which it should delete
        {
            // chatgpt did this
            timer += Time.deltaTime; // output is 100+ each second

            // Check if 1 second has elapsed
            if (timer >= 1f)
            {
                seconds += 1;

                // Debug.Log("Elapsed: " + seconds + " Action performed at: " + Time.time);

                // Reset the timer
                timer = 0f;

                return dealDamage;
            }

            if (seconds >= incapacitationDuration)
            {
                TimeExpired = true;
                Debug.Log(seconds + " seconds Elapsed: ");
            }

            return !dealDamage;
        }

    }
}

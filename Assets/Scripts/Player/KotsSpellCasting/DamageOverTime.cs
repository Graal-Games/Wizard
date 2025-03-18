using System.Collections.Generic;
using UnityEngine;

// GPT generated. To clean up
namespace DamageOverTimeEffect {

    public class DamageOverTime
    {
        string element;
        float damagePerSecond;
        float spellDuration;

        float elapsedTime = 0f;
        bool timeExpired = false;
        bool dealDamage = true;
        int seconds;
        float timer;

        public string Element
        {
            get { return element; }
            private set { element = value; }
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


        // CONSTRUCTOR
        public DamageOverTime(int networkId, string spellElement, float dps, float duration)
        {
            this.Element = spellElement;
            DamagePerSecond = dps;
            SpellDuration = duration;
        }

        public bool Timer() // should be activated using parameters that determine its lifetime and the item in the list which it should delete
        {
            // chatgpt did this
            timer += Time.deltaTime; // output is 100+ each second

            // Check if 1 second has elapsed
            // Each second return a 'true' bool value. Following this return, the PlayerBehaviour script applies damage to the player.
            if (timer >= 1f)
            {
                seconds += 1;

                // Debug.Log("Elapsed: " + seconds + " Action performed at: " + Time.time);

                // Reset the timer
                timer = 0f;

                return dealDamage;
            }

            // Once the timer duration in seconds reaches the total spellDuration
            // Returns a bool that instructs the method in the PlayerBehavior script
            //to remove the entry of the instance in the Dictionary
            if (seconds >= spellDuration)
            {
                TimeExpired = true;
                //Debug.Log(spellDuration + " seconds Elapsed: ");
            }

            return !dealDamage;
        }

    }
}
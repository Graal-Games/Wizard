using System.Collections.Generic;
using UnityEngine;

// GPT generated. To clean up
namespace DamageOverTimeEffect 
{

    public class DamageOverTime
    {
        public int NetworkId { get; private set; }
        public string Element { get; private set; }
        public float DamagePerSecond { get; private set; }
        public float SpellDuration { get; private set; }

        private float elapsedTime = 0f;
        private bool dealDamage = true;
        private int seconds = 0;
        private float timer = 0f;
        public bool TimeExpired { get; private set; } = false;


        // CONSTRUCTOR
        public DamageOverTime(int networkId, string spellElement, float dps, float duration)
        {
            NetworkId = networkId;  
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
            if (timer == 0 || timer >= 1f)
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
            if (seconds >= SpellDuration)
            {
                TimeExpired = true;
                //Debug.Log(spellDuration + " seconds Elapsed: ");
            }

            return !dealDamage;
        }

    }

    public class OnCollisionConstantDamageOverTime
    {
        public int NetworkId { get; private set; }
        public string Element { get; private set; }
        public float DamagePerSecond { get; private set; }
        public float SpellDuration { get; private set; }

        private float elapsedTime = 0f;
        private bool dealDamage = true;
        private int seconds = 0;
        private float timer = 0f;
        public bool TimeExpired { get; private set; } = false;


        public OnCollisionConstantDamageOverTime(int networkId, string spellElement, float dps)
        {
            NetworkId = networkId;
            this.Element = spellElement;
            DamagePerSecond = dps;
        }

        public bool OnCollisionConstantDoTDamageTick() // should be activated using parameters that determine its lifetime and the item in the list which it should delete
        {
            // chatgpt did this
            timer += Time.deltaTime; // output is 100+ each second

            // Check if 1 second has elapsed
            // Each second return a 'true' bool value. Following this return, the PlayerBehaviour script applies damage to the player.
            if (timer == 0 || timer >= 1f)
            {
                seconds += 1;

                // Debug.Log("Elapsed: " + seconds + " Action performed at: " + Time.time);

                // Reset the timer
                timer = 0f;

                return dealDamage;
            }

            return !dealDamage;
        }
    }

}
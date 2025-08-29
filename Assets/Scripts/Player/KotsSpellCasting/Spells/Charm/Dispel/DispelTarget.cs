using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DispelTarget : ProjectileClass
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Spell"))
        {
            if (other.GetComponent<K_Spell>())
            {
                other.GetComponent<K_Spell>().DestroySpell(other.gameObject);
                DestroySpell(gameObject);
            }
            else if (other.GetComponent<SpellsClass>())
            {
                DestroySpell(other.gameObject);
                DestroySpell(gameObject);
            }
            // Handle dispel effect on player
            //DestroySpell(other.gameObject); // Destroy the spell object
        }
    }
}

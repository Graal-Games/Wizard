using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DispellNova : SpellsClass
{

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        // The variables used below to be passed from the Scriptable Object itself
        // Which are properties to be defined under or within an 'Explosive' or 'Nova' field
        GradualScale(5, 1.4f);

    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Spell"))
        {
            if (other.GetComponent<K_Spell>())
            {
                other.GetComponent<K_Spell>().DestroySpell(other.gameObject);
            } else if (other.GetComponent<SpellsClass>())
            {
                DestroySpell(other.gameObject);
            }
            // Handle dispel effect on player
            //DestroySpell(other.gameObject); // Destroy the spell object
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        //Debug.LogFormat("<color=orange>DispellNova despawned</color>", gameObject.name);

        // This destroys the local instance of the shield
        gameObject.transform.parent.GetComponent<K_SpellLauncher>().DestroyLocalShield();

    }
}

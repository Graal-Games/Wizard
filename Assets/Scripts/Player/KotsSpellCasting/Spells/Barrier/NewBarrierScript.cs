using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBarrierScript : K_Spell
{
    public override void Fire()
    {

    }

    public override void OnTriggerEnter(Collider other)
    {
        Debug.Log("NewBarrierScript OnTriggerEnter called with: " + other.name);

        if (other.GetComponent<ISpell>().SpellName.Contains("Barrier"))
        {
            Debug.Log("NewBarrierScript OnTriggerEnter called with: " + other.name);
            Debug.Log("SpellDataScriptableObject: " + SpellDataScriptableObject.directDamageAmount);
        }
    }
}

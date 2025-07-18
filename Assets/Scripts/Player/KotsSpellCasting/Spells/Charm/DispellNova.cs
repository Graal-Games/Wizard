using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DispellNova : SpellsClass
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Spell"))
        {
            // Handle dispel effect on player
            DestroySpell(other.gameObject); // Destroy the spell object
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        Debug.LogFormat("<color=orange>DispellNova despawned</color>", gameObject.name);

        // This destroys the local instance of the shield
        gameObject.transform.parent.GetComponent<K_SpellLauncher>().DestroyLocalShield();

    }
}

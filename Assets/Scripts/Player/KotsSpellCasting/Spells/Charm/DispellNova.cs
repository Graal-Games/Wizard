using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DispellNova : SpellsClass
{



    IEnumerator DispelGO()
    {
        yield return new WaitForSeconds(0.2f); // Wait for 0.1 seconds before destroying the spell object
        Debug.LogFormat("<color=orange>DispellNova despawned</color>", gameObject.name);

    }


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

        StartCoroutine(DispelGO());

        // This destroys the local instance of the shield
        gameObject.transform.parent.GetComponent<K_SpellLauncher>().DestroyLocalShield();

    }
}

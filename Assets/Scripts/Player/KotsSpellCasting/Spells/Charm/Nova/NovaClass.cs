using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NovaClass : SpellsClass
{ 

    // Increase the size of the spell gradually over time.
    public void GradualScale(float maxScale)
    {
        float scaleSpeed = 5f;
        float deltaScale = scaleSpeed * Time.fixedDeltaTime;

        // Only increase if current scale is less than maxScale
        if (transform.localScale.x < maxScale)
        {
            float newScale = Mathf.Min(transform.localScale.x + deltaScale, maxScale);
            transform.localScale = new Vector3(newScale, newScale, newScale);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Spell"))
        {
            if (other.GetComponent<K_Spell>())
            {
                other.GetComponent<K_Spell>().DestroySpell(other.gameObject);
            }
            else if (other.GetComponent<SpellsClass>())
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

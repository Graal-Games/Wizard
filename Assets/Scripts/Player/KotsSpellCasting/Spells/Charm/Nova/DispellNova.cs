using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DispellNova : SpellsClass
{
    // This script as well as Explosion are very similar (MERGE?)
    public override void FixedUpdate()
    {
        base.FixedUpdate();

        // The variables used below to be passed from the Scriptable Object itself
        // Which are properties to be defined under or within an 'Explosive' or 'Nova' field
        GradualScale(5, 1.4f);

    }

    void OnTriggerEnter(Collider other)
    {
        Dispel(other);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        //Debug.LogFormat("<color=orange>DispellNova despawned</color>", gameObject.name);

        // This destroys the local instance of the shield
        gameObject.transform.parent.GetComponent<K_SpellLauncher>().DestroyLocalShield();

    }
}

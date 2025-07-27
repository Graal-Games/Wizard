using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NovaArcane : NovaClass
{
    public override void FixedUpdate()
    {
        base.FixedUpdate();

        GradualScale(3f);

    }

    private void OnTriggerEnter(Collider other)
    {
        HandleAllInteractions(other);
        
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        //Debug.LogFormat("<color=orange>DispellNova despawned</color>", gameObject.name);

        // This destroys the local instance of the shield
        gameObject.transform.parent.GetComponent<K_SpellLauncher>().DestroyLocalShield();

    }

}

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ProjectileExplosive : ProjectileClass
{
    NetworkVariable<bool> isHit = new NetworkVariable<bool>(false);


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _isExplodeOnHit.Value = true;
        DeactivateSpell(); // Deactivate the spell initially
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        // Expand the projectile's collider over time // Explosive effect
        if (isHit.Value == true)
        {
            // The variables used below to be passed from the Scriptable Object itself
            // Which are properties to be defined under or within an 'Explosive' or 'Nova' field
            GradualScale(5, 1.2f);
        }
    }



    //public void OnTriggerEnter(Collider other)
    //{
        
    //}
}

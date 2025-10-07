using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DispelExplosion : SpellsClass
{

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.LogFormat($"<color=yellow>Explosion spell has spawned on the network.</color>");
    }

    public override void FixedUpdate()
    {
        if (!IsSpawned) return;

        base.FixedUpdate();
        GradualScale(20, 3f);


        // Handle all overlapping colliders at once
        Collider[] overlappingColliders = Physics.OverlapSphere(transform.position, GetComponent<SphereCollider>().radius * transform.localScale.x);

        foreach (var collider in overlappingColliders)
        {
            Debug.LogFormat("o9o9o99o9o collider gameObject hit IS:" + collider.gameObject);
            // Optionally filter out self or already handled colliders
            if (collider != null && collider != GetComponent<Collider>() && collider.name != gameObject.name)
            {
                Debug.LogFormat("1q1q1q1q1q1q collider gameObject hit IS:" + collider.gameObject);
               
                DestroySpell(collider.gameObject);
            
            }
        }
    }

    ////Optionally, you can remove OnTriggerEnter if you only want to handle all at once in FixedUpdate
    ////Or keep it if you want both behaviors
    //private void OnTriggerEnter(Collider other)
    //{
    //    // Dispel is handled by a bool variable Scriptable object
    //    HandleAllInteractions(other);
    //}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LongEarthBarrier : BarrierSpell
{
    [SerializeField] Collider nonTriggerCollider;


    private void OnDrawGizmos()
    {
       // Visualize the last movement segment and the sphere cast
       Gizmos.color = Color.yellow;
       Gizmos.DrawWireSphere(transform.position, this.gameObject.transform.localScale.x / 2); // 0.2f is your sphere cast radius
    }

    void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        
    }
}

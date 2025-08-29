using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using static K_Spell;

public class ProjectileSpell : ProjectileClass
{

    private void OnDrawGizmos()
    {
        // Visualize the last movement segment and the sphere cast
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, this.gameObject.transform.localScale.x / 2); // 0.2f is your sphere cast radius
    }

    public override void FixedUpdate()
    {
        if (!IsSpawned) return;

        base.FixedUpdate();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileAir : ProjectileClass
{
    public override void FixedUpdate()
    {
        if (!IsSpawned) return;

        base.FixedUpdate();
    }

}

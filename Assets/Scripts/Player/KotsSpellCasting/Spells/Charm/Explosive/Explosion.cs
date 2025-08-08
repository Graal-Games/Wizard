using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : SpellsClass
{

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.LogFormat($"<color=yellow>Explosion spell has spawned on the network.</color>");    
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        GradualScale(18, 1.8f);
    }

}

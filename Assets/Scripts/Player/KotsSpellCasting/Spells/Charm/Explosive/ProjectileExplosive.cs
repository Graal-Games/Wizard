using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ProjectileExplosive : ProjectileClass
{
    // This class is now just a "marker" component.
    // All of its unique explosive behavior is defined in its K_SpellData asset
    // and handled by the base ProjectileClass.
    // No extra code is needed here.
}

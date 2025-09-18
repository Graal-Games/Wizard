using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISpell
{
    // require each spell to save its name to a varialble
    // This is used to identify the spell and then use it to handle spell overlaps/ interactions in SpellsClass.cs
    string SpellName { get; }
    Element Element {  get; }
    bool IsDispelResistant { get; }

    float DirectDamage { get; }
    float DamageOverTimeAmount { get; }
}

public interface IDeactivatable
{
    // require associated spells to implement a deactivation method
    // This is used to handle the deactivation of spells in SpellsClass.cs
    void DeactivateSpell();
}

//public interface ICharacteristics
//{
//    // require associated spells to implement a deactivation method
//    // This is used to handle the deactivation of spells in SpellsClass.cs
    
//}

public interface IDamageable
{
    float Health { get; }
    void TakeDamage(float dmg);
}

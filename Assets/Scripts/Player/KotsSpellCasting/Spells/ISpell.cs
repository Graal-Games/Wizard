using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISpell
{
    // require each spell to save its name to a varialble
    // This is used to identify the spell and then use it to handle spell overlaps/ interactions in SpellsClass.cs
    string SpellName { get; }
}

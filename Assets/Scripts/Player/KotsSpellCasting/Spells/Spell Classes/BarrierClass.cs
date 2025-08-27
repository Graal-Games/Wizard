using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrierClass : SpellsClass
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name.Contains("Player"))
        {
            HandleAllInteractions(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name.Contains("Player"))
        {
            HandleAllInteractions(other);
        }
    }
}

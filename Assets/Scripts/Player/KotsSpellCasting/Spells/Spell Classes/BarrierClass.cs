using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrierClass : SpellsClass
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.LogFormat($"<color=purple>BARRIER OTEN</color>");

        if (other.gameObject.CompareTag("Player"))
        {
            HandleAllInteractions(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            HandleAllInteractions(other);
        }
    }
}

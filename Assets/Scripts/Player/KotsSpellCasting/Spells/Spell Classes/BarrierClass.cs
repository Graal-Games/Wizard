using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrierClass : SpellsClass, IDamageable
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.LogFormat($"<color=purple>BARRIER OTEN</color>");


        if (other.gameObject.CompareTag("ActiveShield")) 
        {
            HandleIfPlayerHasActiveShield(other.gameObject);
        }   
        else if (other.gameObject.CompareTag("Player"))
        {
            HandleAllInteractions(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.LogFormat($"<color=purple>BARRIER OTEX</color>");

        if (other.gameObject.CompareTag("Player"))
        {
            HandleAllInteractions(other);
        }
    }

    public override void TakeDamage(float dmg)
    {
        base.TakeDamage(dmg);
    }
}

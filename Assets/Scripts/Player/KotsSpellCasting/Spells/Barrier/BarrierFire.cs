using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrierFire : BarrierClass
{
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            HandleAllInteractions(other);
        }
    }
}

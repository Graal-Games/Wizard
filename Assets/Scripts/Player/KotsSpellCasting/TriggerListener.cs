using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerListener : MonoBehaviour
{

    public event System.Action<Collider> OnEnteredTrigger;

    public event System.Action<Collider> OnExitedTrigger;

    private void OnTriggerEnter(Collider collider)
    {
        OnEnteredTrigger?.Invoke(collider);
    }

    private void OnTriggerExit(Collider collider)
    {
        OnExitedTrigger?.Invoke(collider);
    }
}

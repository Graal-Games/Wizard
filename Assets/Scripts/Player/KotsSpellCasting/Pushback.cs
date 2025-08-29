using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Pushback : NetworkBehaviour
{
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        Debug.LogFormat($"<color=purple>Player RB: {rb}</color>");

    }

    [Rpc(SendTo.Server)]
    public void ApplyForceRpc(Vector3 forceDirection, float pushForce)
    {
        Debug.LogFormat($"<color=purple>ApplyForce</color>");
        rb.AddForce(pushForce * forceDirection.normalized, ForceMode.Impulse);
    }

    // If the call only originates server-side, you can skip the RPC and directly apply.
    public void ApplyForce(Vector3 forceDirection, float pushForce)
    {
        //if (!IsServer) return; // ensure only server
        rb.AddForce(pushForce * forceDirection.normalized, ForceMode.Impulse);
        ApplyForceRpc(forceDirection, pushForce);
    }
}

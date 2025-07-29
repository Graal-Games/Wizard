using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AimAtFirstPlayer : NetworkBehaviour
{
    Transform targetTransform;
    ulong clientId;
    Vector3 targetPositionWithOffset;
    Collider gOCollider;
    float radius;
    bool targetFound;

    public bool TargetFound
    {
        get { return targetFound; }
        set { targetFound = value; }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.LogFormat($"<color=blue>{OwnerClientId}</color>");

        gOCollider = GetComponent<Collider>();

        if (OwnerClientId == 0)
        {
            clientId = 1;
        }
        else
        {
            clientId = 0;
        }

        //CheckForRigidbodiesInArea();
        GetOpposingPlayerObjectRpc();
        //GetColliderRadius();
    }


    void FixedUpdate()
    {

        if (targetTransform)
        {
            // Add an offset of 5 units on the x-axis
            targetPositionWithOffset = targetTransform.position + new Vector3(0.20f, 0.5f, 0);

        }

        if (targetPositionWithOffset != null)
            gameObject.transform.LookAt(targetPositionWithOffset);

    }

    [Rpc(SendTo.Server)]
    void GetOpposingPlayerObjectRpc()
    {
        if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject.transform.GetChild(1))
        {
            targetTransform = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject.transform.GetChild(1);
        }
        else
        {
            return;
        }

        TargetFound = true;
    }


    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("other.nameother.nameother.nameother.name: " + other.name);

        if (other.name.Contains("Player"))
        {
            Debug.Log("other.nameother.nameother.nameother.name: " + other.name);

            clientId = other.gameObject.GetComponent<NetworkObject>().OwnerClientId;

            if (OwnerClientId != clientId)
            {
                Debug.Log("OwnerClientIdOwnerClientIdOwnerClientId: " + OwnerClientId);

                TargetFound = true;
                targetTransform = other.gameObject.transform;

            }

        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("other.nameother.nameother.nameother.name: " + other.name);


        if (other.name.Contains("Player"))
        {
            clientId = other.gameObject.GetComponent<NetworkObject>().OwnerClientId;

            if (OwnerClientId != clientId)
            {
                TargetFound = false;
                targetTransform = null;

            }

        }
    }
}



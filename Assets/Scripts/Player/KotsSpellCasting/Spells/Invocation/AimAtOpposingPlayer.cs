using Singletons;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AimAtOpposingPlayer : NetworkBehaviour
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
        } else
        {
            clientId = 0;
        }

        //CheckForRigidbodiesInArea();
        GetOpposingPlayerObjectRpc();
        //GetColliderRadius();
    }


    //void GetColliderRadius()
    //{
    //    if (gOCollider is CapsuleCollider capsuleCollider)
    //    {
    //        // CapsuleCollider has a radius property
    //        radius = capsuleCollider.radius;
    //        Debug.Log("CapsuleCollider radius: " + radius);
    //    }
    //}

    // Update is called once per frame
    void Update()
    {

        // Get the target transform (second child of PlayerObject)
        //Transform targetTransform = NetworkManager.Singleton.ConnectedClients[0].PlayerObject.gameObject.transform.GetChild(1);
        //CheckForRigidbodiesInArea();

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
        targetTransform = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject.transform.GetChild(1);
        TargetFound = true;
    }

    //void CheckForRigidbodiesInArea()
    //{
    //    // Define a search radius and position (center of the collider)
    //    //float searchRadius = 5f;

    //    Vector3 centerPosition = transform.position;

    //    // Find all colliders within the sphere
    //    Collider[] colliders = Physics.OverlapSphere(centerPosition, radius);

    //    foreach (Collider collider in colliders)
    //    {
    //        // Check if the object has a Rigidbody
    //        Rigidbody rb = collider.GetComponent<Rigidbody>();


    //        if (rb != null)
    //        {
    //            Debug.Log("Rigidbody found inside the area: " + rb.name);
    //            clientId = rb.gameObject.GetComponent<NetworkObject>().OwnerClientId;

    //            if (OwnerClientId != clientId)
    //            {
    //                TargetFound = true;
    //                targetTransform = rb.gameObject.transform;
    //            }
    //        }
    //    }
    //}

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

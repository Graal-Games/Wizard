using NUnit.Framework.Constraints;
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

    // Instead of a variable, it now simply checks if the targetTransform has been successfully assigned.
    // This is much more reliable.
    public bool TargetFound => targetTransform != null;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.LogFormat($"<color=blue>{OwnerClientId}</color>");

        if (!IsOwner) return;

        // Start a coroutine to ask for the target.
        // This is better than a direct call in case the other player takes a moment to connect.
        StartCoroutine(RequestTargetWithDelay());

        StartCoroutine(CheckForRigidbodiesInArea());
    }

    private IEnumerator RequestTargetWithDelay()
    {
        // Wait a very short moment to allow the network to settle.
        yield return new WaitForSeconds(0.5f);
        // Ask the server to find our target.
        AskForTargetServerRpc();
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

        if (targetTransform != null)
        {
            // Your aiming logic remains the same.
            targetPositionWithOffset = targetTransform.position + new Vector3(0.20f, 0.5f, 0);
            transform.LookAt(targetPositionWithOffset);

            Debug.LogFormat($"<color=purple>TAREGETING {targetPositionWithOffset}</color>");

        }

    }

    [Rpc(SendTo.Server)]
    private void AskForTargetServerRpc(RpcParams rpcParams = default)
    {
        ulong requestingClientId = rpcParams.Receive.SenderClientId;
        ulong targetClientId = ulong.MaxValue;

        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (clientId != requestingClientId)
            {
                targetClientId = clientId;
                break;
            }
        }

        if (targetClientId != ulong.MaxValue)
        {
            NetworkObject targetNetObject = NetworkManager.Singleton.ConnectedClients[targetClientId].PlayerObject;

            if (targetNetObject != null)
            {
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { requestingClientId }
                        

                    }
                };

                Debug.LogFormat($"<color=purple>INVOCATION TAREGET {requestingClientId}</color>");
                SetTargetClientRpc(targetNetObject.NetworkObjectId, clientRpcParams);
            }
        }
        else
        {
            Debug.LogWarning($"Server could not find a target for client {requestingClientId}");
        }
    }

    [ClientRpc]
    private void SetTargetClientRpc(ulong targetId, ClientRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out NetworkObject targetNetObject))
        {
            if (targetNetObject.transform.childCount > 1)
            {
                targetTransform = targetNetObject.transform.GetChild(1);
                Debug.Log($"Target set to: {targetTransform.name}");
            }
        }
    }

    IEnumerator CheckForRigidbodiesInArea()
    {
        yield return new WaitForSeconds(0.5f);
        // Define a search radius and position (center of the collider)
        //float searchRadius = 5f;

        Vector3 centerPosition = transform.position;

        // Find all colliders within the sphere
        Collider[] colliders = Physics.OverlapSphere(centerPosition, radius);

        foreach (Collider collider in colliders)
        {
            // Check if the object has a Rigidbody
            Rigidbody rb = collider.GetComponent<Rigidbody>();


            if (rb != null)
            {
                Debug.Log("Rigidbody found inside the area: " + rb.name);
                clientId = rb.gameObject.GetComponent<NetworkObject>().OwnerClientId;

                if (OwnerClientId != clientId)
                {
                    //TargetFound = true;
                    targetTransform = rb.gameObject.transform;
                }
            }
        }
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
                targetTransform = null;
            }
        }
    }
}

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

    Collider triggerZone;

    InvocationSpell invocationSpellScript;


    public Transform shootOrigin;  
    public float range = 100f;   
    public LayerMask hitMask;      


    // Instead of a variable, it now simply checks if the targetTransform has been successfully assigned.
    // This is much more reliable.
    public NetworkVariable<bool> TargetFound = new NetworkVariable<bool>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner); //=> targetTransform != null;

    public NetworkVariable<ulong> friendlyPlayerId = new NetworkVariable<ulong>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner); //=> targetTransform != null;

    public NetworkVariable<bool> isEnemyPlayerBehindCover = new NetworkVariable<bool>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner); //=> targetTransform != null;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.LogFormat($"<color=blue>{OwnerClientId}</color>");

        invocationSpellScript = GetComponentInParent<InvocationSpell>();

        CheckIfPlayerInsideTrigger();
    }





    // Update is called once per frame
    void Update()
    {
        if (targetTransform != null)
        {
            targetPositionWithOffset = targetTransform.position + new Vector3(0f, 1.5f, 0); // adding offset to hit the player center exactly.
            transform.LookAt(targetPositionWithOffset);

            //Debug.LogFormat($"<color=purple>TAREGETING {targetPositionWithOffset}</color>");
        }

        // If a target is found, shoot a ray at it to check whether or not he is hiding behind a wall or not
        if (TargetFound.Value)
            CheckIfTargetBehindWallOrBarrier();
    }


    // OPTIONAL: Make the scepter shoot the player only if he is visible and not behind cover
    void CheckIfTargetBehindWallOrBarrier()
    {
        Ray ray = new Ray(targetTransform.position, targetTransform.forward);

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, range, hitMask))
        {
            Debug.Log($"Hit {hit.collider.name} at {hit.point}");
            
        }
        else
        {
            Debug.Log("Missed");
        }

        // Optional: draw ray in scene view for debugging
        Debug.DrawRay(shootOrigin.position, shootOrigin.forward * range, Color.red, 1f);
    }



    [Rpc(SendTo.Server)]
    void TargetFoundRpc(bool value)
    {
        TargetFound.Value = value;
    }




    [Rpc(SendTo.Server)]
    void IsEnemyPlayerBehindCover(bool value)
    {
        isEnemyPlayerBehindCover.Value = value;
    }






    void CheckIfPlayerInsideTrigger()
    {
        //Debug.LogFormat($"<color>11colliders</color>");

        Collider[] colliders = Physics.OverlapSphere(triggerZone.bounds.center, triggerZone.bounds.extents.magnitude);

        foreach (Collider collider in colliders)
        {
            //Debug.LogFormat($"<color>22colliders: {collider}</color>");

            if (collider.gameObject.name.Contains("Player"))
            {
                ulong playerOwnerId = collider.GetComponent<NewPlayerBehavior>().OwnerClientId;

                // Check if the ID of the player character that was found is different from the id of the player character that casted the spell
                if (friendlyPlayerId.Value != playerOwnerId)
                {
                    //Debug.Log("Player is already inside the trigger on spawn!");
                    targetTransform = collider.GetComponent<Rigidbody>().transform;

                    // To not duplicate the shoot call make a check if a target was found first
                    if (TargetFound.Value == false)
                    {
                        TargetFoundRpc(true);

                        StartCoroutine(invocationSpellScript.Shoot());
                    }
                    return;
                }
            }
        }
        //Debug.Log("Player is not inside the trigger on spawn.");
    }






    private void OnTriggerEnter(Collider other)
    {
        if (other.name.Contains("Player"))
        {
            //Debug.Log("SCEPTER 1- AIM AT PLAYER: " + other.name);

            clientId = other.gameObject.GetComponent<NetworkObject>().OwnerClientId;

            if (friendlyPlayerId.Value != clientId)
            {
                //Debug.Log("SCEPTER 2- AIM AT PLAYER: " + OwnerClientId);

                targetTransform = other.gameObject.transform;

                if (TargetFound.Value == false)
                {
                    TargetFoundRpc(true);

                    StartCoroutine(invocationSpellScript.Shoot());
                }
            }
        }
    }






    private void OnTriggerExit(Collider other)
    {
        //Debug.Log("SCEPTER 4- RELEASE AIM AT PLAYER: " + other.name);

        if (other.name.Contains("Player"))
        {
            clientId = other.gameObject.GetComponent<NetworkObject>().OwnerClientId;

            if (friendlyPlayerId.Value != clientId)
            {
                targetTransform = null;
                TargetFoundRpc(false);
                //Debug.Log("SCEPTER 4- RELEASE AIM AT PLAYER: " + other.name);
            }
        }
    }
}

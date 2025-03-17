using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EarthBarrier : NetworkBehaviour
{
    public NetworkVariable<float> earthBarrierHealth = new (default,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<float> earthBarrierDamage = new (default,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [SerializeField] EarthBarrierCollision earthBarrierCollisionScript;

    int earthBarrierLifetime = 25;

    Animator animator;

    float incapacitatedDuration = 4;

    // Collider earthBarrierCollider;

    public delegate void BarrierLifeStatus(ulong clientId, NetworkObjectReference spellObj, NetworkBehaviour spellNetBehavior, bool status, NetworkObject netObj);
    public static event BarrierLifeStatus earthBarrierExists;

    public float IncapacitatedDuration
    {
        get { return incapacitatedDuration; }
    }


    private void Start()
    {
        gameObject.transform.position = new Vector3(transform.position.x, -1, transform.position.z);

        //RevertAnimatorPositionChanges();
        SetBarrierHealthAndDamageValues();

        earthBarrierCollisionScript.GetComponentInChildren<EarthBarrierCollision>();//

        //Debug.LogFormat($"<color=red>{arcaneBarrierCollisionScript}</color>");

        StartCoroutine(SpellExpirationTimer());
    }


    // This is used to remove the objectId from the earthBarrier list in playerBehavior script
    // That list makes sure the player is only affected once by the earth barrier
    //and that is a solution for multiple trigger enters being recorded by the earth barrier Stun and Damage trigger collider
    // (**) To implement
    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        //Debug.LogFormat($"<color=red>DESpawned</color>");

        if (earthBarrierExists != null) earthBarrierExists(OwnerClientId, this.NetworkObject, this.GetNetworkBehaviour(this.NetworkBehaviourId), false, this.gameObject.GetComponent<NetworkObject>());

        base.OnNetworkDespawn();
    }


    void SetBarrierHealthAndDamageValues()
    {
        // I don't think this needs to be a Network Variable
        // Perhaps unless these values are relative to each player perks (to be introduced later)
        earthBarrierHealth.Value = 45;

        earthBarrierDamage.Value = 10;
    }


    IEnumerator SpellExpirationTimer()
    {
        yield return new WaitForSeconds(earthBarrierLifetime); // 8 seconds
        // Debug.LogFormat($"<color=red>IE</color>");

        if (earthBarrierHealth.Value <= 0f)
        {
            yield return null;
        }

        DestroyBarrierServerRpc();
    }



    [ServerRpc(RequireOwnership = false)]
    void DestroyBarrierServerRpc()
    {
        NetworkObject.Despawn(this.gameObject); 
        return;
    }


    public void IsBarrierDestroyed()
    {
        if (earthBarrierHealth.Value <= 0f)
        {
            // Debug.LogFormat($"<color=red>{earthBarrierHealth.Value}</color>");
            DestroyBarrierServerRpc();
        }
    }
}

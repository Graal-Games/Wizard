using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ArcaneBarrier : NetworkBehaviour
{
    public NetworkVariable<float> arcaneBarrierHealth = new NetworkVariable<float>(default,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<float> arcaneBarrierDamage = new NetworkVariable<float>(default,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [SerializeField] ArcaneBarrierCollision arcaneBarrierCollisionScript;

    int acraneBarrierLifetime = 80;


    private void Start()
    {
        gameObject.transform.position = new Vector3( transform.position.x, -1, transform.position.z );

        arcaneBarrierCollisionScript.GetComponentInChildren<ArcaneBarrierCollision>();

        arcaneBarrierHealth.Value = 20;

        arcaneBarrierDamage.Value = 15;

        //Debug.LogFormat($"<color=red>{arcaneBarrierCollisionScript}</color>");

        StartCoroutine(SpellExpirationTimer());
    }


    

    IEnumerator SpellExpirationTimer()
    {
        yield return new WaitForSeconds(acraneBarrierLifetime); // 8 seconds
        Debug.LogFormat($"<color=red>IE</color>");
        DestroyArcaneBarrierServerRpc();
    }



    [ServerRpc(RequireOwnership = false)]
    void DestroyArcaneBarrierServerRpc()
    {
        NetworkObject.Despawn(this.gameObject);
        return;
    }


    public void IsBarrierDestroyed()
    {
        if (arcaneBarrierHealth.Value <= 0f)
        {
           Debug.LogFormat($"<color=red>{arcaneBarrierHealth.Value}</color>");
           DestroyArcaneBarrierServerRpc();
        }
    }


}

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerCenter : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log("SETTING SPAWN CENTER 1111111");
        gameObject.GetComponentInParent<NewPlayerBehavior>().AssignPlayerCenter(this.NetworkObject);
    }
}

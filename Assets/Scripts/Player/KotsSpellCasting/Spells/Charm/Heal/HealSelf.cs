using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HealSelf : SpellsClass
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.LogFormat($"1111HealSelfServerRpc called on: {OwnerClientId} - Parent:  {gameObject.transform.parent}");

        //HealSelfServerRpc(OwnerClientId);
    } 

    [Rpc(SendTo.SpecifiedInParams)]
    void HealSelfServerRpc(NetworkObjectReference playerRef, RpcParams rpcParams = default)
    {
        if (playerRef.TryGet(out NetworkObject netObj))
        {
            var player = netObj.GetComponent<NewPlayerBehavior>();

            if (player != null)
            {
                // Call your heal method here
                player.Heal(SpellDataScriptableObject.healAmount); // Example: negative damage = heal

                Debug.LogFormat($"Healed player: {rpcParams} for {SpellDataScriptableObject.healAmount}");
            }
            else
            {
                Debug.LogWarning("NewPlayerBehavior not found on targeted player.");
            }
        }
        else
        {
            Debug.LogWarning("Could not resolve NetworkObjectReference.");
        }
    }




    public void HealTarget(ulong targetClientId)
    {
        NetworkObject netObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(targetClientId);

        if (netObj != null)
        {
            NetworkObjectReference objRef = new NetworkObjectReference(netObj);
            Debug.LogWarning($"Player NetworkObjectReference found: {objRef}");

            // Now you can use objRef in your RPCs, for example:
            HealSelfServerRpc(objRef, RpcTarget.Single(targetClientId, RpcTargetUse.Persistent));
        }
        else
        {
            Debug.LogWarning($"Player NetworkObject for clientId {targetClientId} not found.");
        }
    }
}

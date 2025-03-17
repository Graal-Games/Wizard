using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class BarrierSpellCast : NetworkBehaviour
{

    GameObject barrierToSpawn;
    [SerializeField] GameObject arcaneBarrierObject;
    [SerializeField] GameObject fireBarrierObject;
    [SerializeField] GameObject earthBarrierObject;

    public void CastBarrier(string barrierType, Vector3 newPosition = default)
    {
        
        switch(barrierType)
        {
            case "Aoe Arcane":
                CastAoeArcaneBarrierServerRpc(newPosition);
                return;

            case "Arcane":
                CastArcaneBarrierServerRpc();
                return;

            case "Fire":
                CastFireBarrierServerRpc();
                return;

            case "Earth":
                CastEarthBarrierServerRpc();
                return;

        }
    }

    [ServerRpc]
    private void CastArcaneBarrierServerRpc()
    {
        GameObject barrierInstance = Instantiate(arcaneBarrierObject, transform.position, this.transform.rotation);
        barrierInstance.GetComponent<NetworkObject>().Spawn();
    }
    
    [ServerRpc]
    private void CastFireBarrierServerRpc()
    {
        GameObject barrierInstance = Instantiate(fireBarrierObject, transform.position, this.transform.rotation);
        barrierInstance.GetComponent<NetworkObject>().Spawn();
    }
    
    [ServerRpc]
    private void CastEarthBarrierServerRpc()
    {
        GameObject barrierInstance = Instantiate(earthBarrierObject, transform.position, this.transform.rotation);
        barrierInstance.GetComponent<NetworkObject>().Spawn();
    }

    [ServerRpc]
    void CastAoeArcaneBarrierServerRpc(Vector3 aoeBarrierPosition)
    {
        GameObject aoeBarrierInstance = Instantiate(arcaneBarrierObject, aoeBarrierPosition, this.transform.rotation);
        aoeBarrierInstance.GetComponent<NetworkObject>().Spawn();
    }
}

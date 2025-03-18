using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Singletons;
using System;
using Unity.Networking;
using Unity.Netcode.Components;
using SpellsManager;



// RENAME TO DISCHARGED SPELLS
public class DischargedSpellcast : NetworkBehaviour
{   
    [Header("Spell Objects")]
    [SerializeField] GameObject arcaneProjectile;
    [SerializeField] GameObject waterProjectile;
    [SerializeField] GameObject earthProjectile;
    [SerializeField] GameObject airProjectile;
    [SerializeField] GameObject fireProjectile;
    [SerializeField] GameObject beam;


    GameObject spawnedBeamInstance;

    [Header("Associated Scripts")]
    AoeCast aoeCast;
    [SerializeField] GameObject playerInput;

    [Header("Castpoint movement")]
    private float sensitivity = 1f; // Mouse sensitivity
    private float xRotation = 0f; // Current rotation around the x-axis
    private float yRotation = 0f;


    public NetworkObject beamMain;
    NetworkObject beamNetObj;
    // >> public NetworkVariable<NetworkObjectReference> beamNetVar = new NetworkVariable<NetworkObjectReference>(default,
    //NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    
    void Awake()
    {
        aoeCast = this.gameObject.GetComponent<AoeCast>();
    }

    void Update()
    {
        if (!IsLocalPlayer) return;
        MoveWithPlayerLook();
    }

    // This Moves the object from which discharge spells are cast
    //and aoes Through a raycast inside aoeCast.cs
    private void MoveWithPlayerLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        xRotation += mouseY;
        yRotation += mouseX; 

        xRotation = Mathf.Clamp(xRotation, -90f, 25f);

        // transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.localRotation = Quaternion.Euler(0f, yRotation, xRotation);
    }



#region Beam
    public void CastBeam()
    {
        CastBeamServerRpc();
    }


    [ServerRpc(RequireOwnership = true)]
    void CastBeamServerRpc(ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;

        spawnedBeamInstance = Instantiate(beam, transform.position, this.transform.rotation, this.gameObject.transform.parent);

        beamNetObj =  spawnedBeamInstance.GetComponent<NetworkObject>();
        
        beamNetObj.SpawnWithOwnership(clientId);
        //Debug.Log("//////////////////////////////clientId: " + clientId + "OwnerClientId: " + OwnerClientId + " Parent: " + this.gameObject.transform.parent.GetComponent<NetworkObject>());

        SaveBeamRefOnClientRpc(beamNetObj);

        beamNetObj.TrySetParent(this.gameObject.transform.parent.GetComponent<NetworkObject>());

    }

    // Not sure if this is being used
    [ClientRpc]
    void SaveBeamRefOnClientRpc (NetworkObjectReference beamNetObjx, ClientRpcParams clientRpcParams = default)
    {

        beamNetObjx.TryGet(out NetworkObject beamNetObjv);
        beamMain = beamNetObjv;

        return;
    }

#endregion

#region Aoe
    public void StartCastAoe(string aoeToBeCast = null)
    {
        // Debug.LogFormat($"<color=green>{aoeToBeCast}</color>");
        aoeCast.StartCastAoe(aoeToBeCast);
    }

    public void StopCastAoe()
    {
        aoeCast.StopCastAoe();
    }

    public void StopAoePlacement()
    {
        aoeCast.StopAoePlacement();
    }

    public void ConfirmPlacement(string spellToBePlaced)
    {
        StopCastAoe();
        aoeCast.ConfirmPlacement(spellToBePlaced);
    }
#endregion

#region Bolt
    public void CastProjectile(string projectile)
    {  
        SpawnBoltServerRpc(projectile);
    }

    void BoltToSpawn(GameObject BoltOfType)
    {
        GameObject boltInstance = Instantiate(BoltOfType, transform.position, transform.rotation);
        boltInstance.GetComponent<NetworkObject>().Spawn();
    }

    [ServerRpc]
    public void SpawnBoltServerRpc(string projectile)
    {
        switch (projectile)
        {
            case "Earth Projectile":
                BoltToSpawn(earthProjectile);
                break;
            
            case "Water Projectile":
                BoltToSpawn(waterProjectile);
                break;

            case "Air Projectile":
                BoltToSpawn(airProjectile);
                break;
            
            case "Fire Projectile":
                BoltToSpawn(fireProjectile);
                break;

            case "Arcane Projectile":
                BoltToSpawn(arcaneProjectile);
                break;

            default:
                //BoltToSpawn(arcaneBolt);
                break;
        } 
    }
#endregion
}

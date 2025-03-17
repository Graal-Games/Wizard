using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class LocalSpells : NetworkBehaviour
{
    public GameObject sphereShield;
    GameObject sphereShieldInstance;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool CastShield()
    {   
        //Debug.LogFormat($"<color=cyan>prefab is: {sphereShield} - Instance: {sphereShieldInstance}</color>");

        // Is there already a shield on the player?
        //ShieldIsExist();

        
        // Here make it so it selects the color relatively to the type
        
        Cast();
        
        return true;
    }

    public bool ShieldIsExist()
    {
        // If there's already a shield on the player when another is cast > Destroy the previous one
        if (sphereShieldInstance)
        {
            DestroyPreviousSphereShieldServerRpc();
            return true;
        }
        
        return false;
    }

    public void Cast()
    {
        SpawnShieldServerRpc();
    }

    [ServerRpc]
    void DestroyPreviousSphereShieldServerRpc()
    {
        sphereShieldInstance.GetComponent<NetworkObject>().Despawn();
        Destroy(sphereShieldInstance);
    }

    [ServerRpc]
    public void SpawnShieldServerRpc(ServerRpcParams serverRpcParams = default)
    {
        //
        var clientId = serverRpcParams.Receive.SenderClientId;
        //Debug.Log(this.gameObject.transform);
        //Debug.Log(clientId);
        sphereShieldInstance = Instantiate(sphereShield, transform.position, transform.rotation);
        sphereShieldInstance.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);

        sphereShieldInstance.GetComponent<NetworkObject>().TrySetParent(this.gameObject.transform.parent);

        return;
    }


}

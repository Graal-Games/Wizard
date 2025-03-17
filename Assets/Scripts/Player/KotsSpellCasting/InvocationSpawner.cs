using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class InvocationSpawner : K_Spell
{
    [SerializeField] GameObject spawnLocation;
    [SerializeField] GameObject invocationRadius;
    [SerializeField] GameObject invokedObjectGO;
    K_SpellLauncher spellLauncher;
    //[SerializeField] GameObject[] spawnableGameObjects;

    Transform playerGO;

    bool hasFoundLocation = false;

    // If the radius is to be variable in distance or size in the future
    //these values will need to be gotten from elsewhere
    float x;
    float y;
    float z;

    float randomX;
    float randomZ;

    NetworkVariable<Vector3> spawnSpot = new NetworkVariable<Vector3>(default,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);


    // Start is called before the first frame update
    void Start()
    {
        
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        playerGO = gameObject.transform;
        Debug.Log("This is the player GO: " + playerGO);
        //objectToSpawn = SpellDataScriptableObject.childPrefab;
        // InitializeObjectToSpawnPrefab();
        //spawnLocation = GetComponent<GameObject>();
    }



    // Update is called once per frame
    void Update()
    {
        
    }

    public Vector3 SpawnOnce()
    {
        Vector3 centerMass = new Vector3 (playerGO.position.x, playerGO.position.y, playerGO.position.z);

        randomX = Random.Range(playerGO.position.x, playerGO.position.x + 4);
        //float randomY = Random.Range(playerGO.position.y, playerGO.position.y + 4); // To implement this later -- Will require ground detection system
        randomZ = Random.Range(playerGO.position.z, playerGO.position.z + 4);

        Debug.LogFormat($"<color=orange> Scepter X Position: {randomX} - Scepter Z Position: {randomZ} </color>");

        spawnSpot.Value = new Vector3(randomX, playerGO.position.y, randomZ);
        
        return spawnSpot.Value;
        //SpawnAtLocationRpc(spawnSpot.Value);
    }    

    public void ChooseLocation()
    {
        //while (hasFoundLocation == false)
        //{

        //}
    }

    //public void SpawnAtLocation(Vector3 spawnPosition)
    //{
    //    SpawnAtLocationRpc(spawnPosition);
    //}

    //[Rpc(SendTo.Server)]
    //private void SpawnAtLocationRpc(Vector3 spawnPosition)
    //{
    //    Debug.LogFormat($"<color=orange> Scepter SPAWN SPOT {spawnSpot.Value} </color>");

    //    GameObject spellInstance = Instantiate(spellLauncher.prefabReferences[spellLauncher.SpellSequence], spawnPosition, Quaternion.identity);

    //    NetworkObject netObj = spellInstance.GetComponent<NetworkObject>();
    //    // netObj.SpawnWithOwnership(NetworkManager.LocalClient.ClientId);
    //    netObj.SpawnWithOwnership(OwnerClientId);
    //}

    public override void Fire()
    {
    }
}

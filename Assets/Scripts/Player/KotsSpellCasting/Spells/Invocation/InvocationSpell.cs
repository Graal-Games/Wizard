using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class InvocationSpell : K_Spell
{
    [SerializeField] GameObject spawnLocation;
    Transform spawnPosition;
    AimAtOpposingPlayer aimAtOpposingPlayerScript;
    GameObject objectToSpawn;
    [SerializeField] GameObject[] spawnableGameObjects;

    // Start is called before the first frame update
    void Start()
    {
        if (!IsOwner) { return; }



    }

    private void Update()
    {
        spawnPosition = spawnLocation.gameObject.transform;

        //Debug.Log("spawnPositionspawnPositionspawnPositionspawnPosition " + spawnPosition.position.x + spawnPosition.position.y + spawnPosition.position.z);



        //AimAtOpposingPlayer(); spellLauncher.prefabReferences[spellSequenceParam]
    }


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        aimAtOpposingPlayerScript = GetComponentInChildren<AimAtOpposingPlayer>();
        // InitializeObjectToSpawnPrefab();
        StartCoroutine(Shoot());
    }

    GameObject InitializeObjectToSpawnPrefab()
    {
        Debug.Log("SpellDataScriptableObject.element.ToString() " + SpellDataScriptableObject.element.ToString());



        switch (SpellDataScriptableObject.element.ToString())
        {
            case "Arcane":
                Debug.Log("SpellDataScriptableObject.childPrefab " + objectToSpawn);

                objectToSpawn = spawnableGameObjects[0];
                return objectToSpawn;
            case "Water":
                objectToSpawn = spawnableGameObjects[1];
                Debug.Log("Water.WaterWaterWaterWater " + objectToSpawn);

                return objectToSpawn;

            case "Earth":
                objectToSpawn = spawnableGameObjects[2];
                Debug.Log("EarthEarthEarthEarth " + objectToSpawn);

                return objectToSpawn;

            case "Fire":
                objectToSpawn = spawnableGameObjects[3];
                Debug.Log("FireFireFireFire " + objectToSpawn);

                return objectToSpawn;

            case "Air":
                objectToSpawn = spawnableGameObjects[4];
                Debug.Log("AirAirAirAirAir" + objectToSpawn);

                return objectToSpawn;

            default:
                return null;
        }
    }

    IEnumerator Shoot()
    {
        yield return new WaitForSeconds(5);
        Debug.Log("SpellDataScriptableObject.childPrefab " + SpellDataScriptableObject.childPrefab);
        //SpawnProjectileRpc(spawnPosition.position.x, spawnPosition.position.y, spawnPosition.position.z);

        // If a target is available
        if (aimAtOpposingPlayerScript.TargetFound)
            SpawnProjectileRpc(spawnPosition.position.x, spawnPosition.position.y, spawnPosition.position.z);
        
    }

    [Rpc(SendTo.Server)]
    //void SpawnProjectileRpc(float xPos, float yPos, float zPos)
    void SpawnProjectileRpc(float xPos, float yPos, float zPos)
    {
        //Debug.Log("NetworkManager.LocalClientId (" + NetworkManager.LocalClient.ClientId + ")");
        Debug.Log("SpellDataScriptableObject.childPrefab " + SpellDataScriptableObject.childPrefab);
        Debug.Log("SpellDataScriptableObject.childPrefab " + xPos + yPos + zPos);

        // spawnPosition = spawnLocation.gameObject.transform;
        //spawnPosition = aimAtOpposingPlayerScript.GetPlayerGameObject();


        GameObject spellInstance = Instantiate(InitializeObjectToSpawnPrefab(), new Vector3(xPos, yPos, zPos), spawnPosition.rotation);

        NetworkObject netObj = spellInstance.GetComponent<NetworkObject>();
        // netObj.SpawnWithOwnership(NetworkManager.LocalClient.ClientId);
        netObj.Spawn();

        StartCoroutine(Shoot());
        //netObj.TrySetParent(gameObject.transform);
    }

    public override void Fire()
    {
        // Vector3 upRayOrigin = objectToShootFrom.transform.position + objectToShootFrom.transform.up * objectToShootFrom.transform.localScale.y / 2;
        // Vector3 downRayOrigin = objectToShootFrom.transform.position - objectToShootFrom.transform.up * objectToShootFrom.transform.localScale.y / 2;

    }
}

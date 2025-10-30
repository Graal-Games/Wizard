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
    NetworkVariable<float> localHealth = new NetworkVariable<float>();


    private void Update() // To Do: This should be changed to FixedUpdate for correct performance
    {
        spawnPosition = spawnLocation.gameObject.transform;
    }


    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        base.OnNetworkSpawn();
        aimAtOpposingPlayerScript = GetComponentInChildren<AimAtOpposingPlayer>();

        if (aimAtOpposingPlayerScript.TargetFound.Value)
            StartCoroutine(Shoot());

        StartCoroutine(LifeTime(SpellDataScriptableObject.spellDuration, gameObject) );
    }

    GameObject InitializeObjectToSpawnPrefab()
    {
        Debug.Log("SpellDataScriptableObject.element.ToString() " + SpellDataScriptableObject.element.ToString());



        switch (SpellDataScriptableObject.element.ToString())
        {
            case "Arcane":
                //Debug.Log("SpellDataScriptableObject.childPrefab " + objectToSpawn);

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



    public IEnumerator Shoot()
    {
        yield return new WaitForSeconds(5);
        Debug.Log("SSSSSSSSSSSSSSSSSSHOOOT");

        // If a target is available
        if (aimAtOpposingPlayerScript.TargetFound.Value)
        {
            SpawnProjectileRpc(spawnPosition.position.x, spawnPosition.position.y - 0.2f, spawnPosition.position.z);
            yield return null;
        }
        else 
        {             
            Debug.Log("No target found, cannot spawn projectile.");
            //StartCoroutine(Shoot());
        }
    }



    [Rpc(SendTo.Server)]
    void SpawnProjectileRpc(float xPos, float yPos, float zPos)
    {

        GameObject spellInstance = Instantiate(InitializeObjectToSpawnPrefab(), new Vector3(xPos, yPos, zPos), spawnPosition.rotation);
        NetworkObject netObj = spellInstance.GetComponent<NetworkObject>();

        netObj.Spawn();

        StartCoroutine(Shoot());
    }

    public override void Fire()
    {
        // Vector3 upRayOrigin = objectToShootFrom.transform.position + objectToShootFrom.transform.up * objectToShootFrom.transform.localScale.y / 2;
        // Vector3 downRayOrigin = objectToShootFrom.transform.position - objectToShootFrom.transform.up * objectToShootFrom.transform.localScale.y / 2;

    }
}

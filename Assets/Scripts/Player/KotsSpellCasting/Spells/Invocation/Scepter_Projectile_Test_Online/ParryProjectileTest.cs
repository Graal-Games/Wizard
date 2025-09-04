using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class ParryProjectileTest : NetworkBehaviour
{
    [SerializeField] GameObject spawnLocation;
    Transform spawnPosition;
    AimAtOpposingPlayer aimAtOpposingPlayerScript;
    GameObject objectToSpawn;
    [SerializeField] GameObject[] spawnableGameObjects;


    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Shoot());
    }

    public override void OnNetworkSpawn()
    {
        spawnPosition = spawnLocation.gameObject.transform;
    }


    IEnumerator Shoot()
    {
        yield return new WaitForSeconds(5);
        Debug.Log("SpellDataScriptableObject.childPrefab " + spawnableGameObjects[0]);

        GameObject spellInstance = Instantiate(spawnableGameObjects[0], spawnPosition.position, Quaternion.identity);

        NetworkObject netObj = spellInstance.GetComponent<NetworkObject>();

        netObj.Spawn();

        StartCoroutine(Shoot());
        //// If a target is available
        //if (aimAtOpposingPlayerScript.TargetFound)
        //{
        //    //SpawnProjectileRpc(spawnPosition.position.x, spawnPosition.position.y - 0.2f, spawnPosition.position.z);
        //    Instantiate(spawnableGameObjects[0], spawnPosition.position, Quaternion.identity);
        //}
        //else 
        //{             
        //    Debug.Log("No target found, cannot spawn projectile."); 
        //}
    }
}

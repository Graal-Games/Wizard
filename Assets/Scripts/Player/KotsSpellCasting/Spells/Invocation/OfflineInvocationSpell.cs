using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OfflineInvocationSpell : MonoBehaviour
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

    private void Update()
    {
        spawnPosition = spawnLocation.gameObject.transform;
    }


    IEnumerator Shoot()
    {
        yield return new WaitForSeconds(5);
        Debug.Log("SpellDataScriptableObject.childPrefab " + spawnableGameObjects[0]);

        Instantiate(spawnableGameObjects[0], spawnPosition.position, Quaternion.identity);

        StartCoroutine(Shoot());

    }
}

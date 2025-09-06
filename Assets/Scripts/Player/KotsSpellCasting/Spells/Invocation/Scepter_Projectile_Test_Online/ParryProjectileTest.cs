using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class ParryProjectileTest : NetworkBehaviour
{
    // Enum to create a dropdown in the Inspector for the spawn behavior
    public enum SpawnMode { Sequential, Random }

    [Header("Spawning Configuration")]
    [SerializeField] private GameObject[] spawnableGameObjects; // The array of prefabs to spawn from
    [SerializeField] private SpawnMode spawnMode = SpawnMode.Sequential; // The selected spawn mode
    [SerializeField] private float spawnInterval = 5f; // Configurable delay

    [Header("References")]
    [SerializeField] private GameObject spawnLocation;

    private Transform spawnPosition;
    private int currentIndex = 0; // Used to track the next spell in Sequential mode

    public override void OnNetworkSpawn()
    {
        if (spawnLocation != null)
        {
            spawnPosition = spawnLocation.transform;
        }

        // IMPORTANT: Only the server should run the spawning logic.
        if (!IsServer)
        {
            return;
        }

        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // Make sure the array isn't empty before trying to spawn
            if (spawnableGameObjects == null || spawnableGameObjects.Length == 0)
            {
                continue; // Skip this loop iteration if there's nothing to spawn
            }

            GameObject prefabToSpawn = null;

            // Select the prefab based on the chosen spawn mode
            switch (spawnMode)
            {
                case SpawnMode.Sequential:
                    prefabToSpawn = spawnableGameObjects[currentIndex];
                    // Move to the next index, looping back to 0 if we reach the end
                    currentIndex = (currentIndex + 1) % spawnableGameObjects.Length;
                    break;

                case SpawnMode.Random:
                    int randomIndex = Random.Range(0, spawnableGameObjects.Length);
                    prefabToSpawn = spawnableGameObjects[randomIndex];
                    break;
            }

            // Spawn the selected prefab if it's not null
            if (prefabToSpawn != null && spawnPosition != null)
            {
                GameObject spellInstance = Instantiate(prefabToSpawn, spawnPosition.position, Quaternion.identity);
                NetworkObject netObj = spellInstance.GetComponent<NetworkObject>();
                netObj.Spawn();
            }
        }
    }
}

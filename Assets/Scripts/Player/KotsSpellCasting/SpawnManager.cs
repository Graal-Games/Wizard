using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Singletons;

public class SpawnManager : Singleton<SpawnManager>
{
    [SerializeField] GameObject P1Spawn;
    [SerializeField] GameObject P2Spawn;

    private void Awake()
    {
        if (P1Spawn == null)
        {
            Debug.LogError("SpawnManager Error: P1Spawn has NOT been assigned in the Inspector!", this.gameObject);
        }

        if (P2Spawn == null)
        {
            Debug.LogError("SpawnManager Error: P2Spawn has NOT been assigned in the Inspector!", this.gameObject);
        }
    }

    // We subscribe when the object is enabled
    private void OnEnable()
    {
        GameEvents.OnRequestSpawnPoint += AssignSpawnPoint;
        GameEvents.OnRequestSpawnRotation += AssignSpawnRotation;
    }

    // It's crucial to unsubscribe when the object is disabled or destroyed
    private void OnDisable()
    {
        GameEvents.OnRequestSpawnPoint -= AssignSpawnPoint;
        GameEvents.OnRequestSpawnRotation -= AssignSpawnRotation;
    }

    public Vector3 AssignSpawnPoint(ulong clientId)
    {
        if (clientId == 0)
        {
            // Added extra leeway for him to spawn above the ground
            return new Vector3(P1Spawn.transform.position.x, 0.4f, P1Spawn.transform.position.z);

        } else {

            return new Vector3(P2Spawn.transform.position.x, 0.4f, P2Spawn.transform.position.z);

        }
    }

    public Quaternion AssignSpawnRotation(ulong clientId)
    {
        if (clientId == 0)
        {
            return P1Spawn.transform.rotation;

        } else {

            return P2Spawn.transform.rotation;

        }
    }

}

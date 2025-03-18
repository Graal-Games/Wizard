using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Singletons;

public class SpawnManager : Singleton<SpawnManager>
{
    [SerializeField] GameObject P1Spawn;
    [SerializeField] GameObject P2Spawn;

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

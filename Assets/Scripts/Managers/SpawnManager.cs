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
            return P1Spawn.transform.position + new Vector3(0, 0.4f, 0);

        } else {

            return P2Spawn.transform.position + new Vector3(0, 0.4f, 0);

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

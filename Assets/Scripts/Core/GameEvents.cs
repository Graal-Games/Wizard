using System;
using UnityEngine;

// This is a static class, meaning we don't need to create an instance of it.
// It will hold events that any part of our game can access.
public static class GameEvents
{
    // A "Func" is an event that can return a value.
    // This event asks for a client ID (ulong) and expects a spawn position (Vector3) back.
    public static event Func<ulong, Vector3> OnRequestSpawnPoint;
    public static event Func<ulong, Quaternion> OnRequestSpawnRotation;

    // We invoke the event and return the result.
    public static Vector3 RequestSpawnPoint(ulong clientId)
    {
        return OnRequestSpawnPoint?.Invoke(clientId) ?? Vector3.zero;
    }

    public static Quaternion RequestSpawnRotation(ulong clientId)
    {
        return OnRequestSpawnRotation?.Invoke(clientId) ?? Quaternion.identity;
    }
}

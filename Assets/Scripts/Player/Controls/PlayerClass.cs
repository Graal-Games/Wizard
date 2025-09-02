// Access modifier (e.g., public, private, internal)
// Other modifiers (e.g., static, abstract, sealed)
using DotTimers;
using System;
using System.Collections.Generic;
using Unity.IO.Archive;
using Unity.Netcode;
using UnityEngine;

public class PlayerClass
{
    // You'll likely want a constructor to pass in the initial data
    public ulong ClientId;
    public string PlayerName;
    // ... other stats like strength, intelligence, etc.

    public PlayerClass(ulong id, string name)
    {
        ClientId = id;
        PlayerName = name;
    }
}
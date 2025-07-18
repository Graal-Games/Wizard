using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using Unity.Netcode;

public class K_SpellBuilder : NetworkBehaviour
{
    [UDictionary.Split(30, 70)] public SpellDict spellDictionary;
    [Serializable] public class SpellDict : UDictionary<string, K_SpellData> { }

    [UDictionary.Split(30, 70)] public DrDict drDictionary;
    [Serializable] public class DrDict : UDictionary<string, K_DRData> { }

    private Dictionary<KeyCode, bool> isIgnoreDRLock = new Dictionary<KeyCode, bool>();

    // The minimum number of casts before triggering a DR Lock
    private static readonly int minCasts = 2;

    // Holds the current DR tier for each spell family
    private Dictionary<string, int> drTiers;

    // Holds the current DR cooldown for each spell family
    private Dictionary<string, float> drCooldowns;

    // The last spell type that triggered a DR
    private string lastDrSpellType;

    private static Random rng = new Random();
    //public static K_SpellBuilder Instance { get; private set; }

    K_Spell spellComponent;
    GameObject spellGO;
    K_SpellData spellData;

    GameObject baseSpell;

    private void Awake()
    {
        //if (Instance != null && Instance != this)
        //    Destroy(this);
        //else
        //    Instance = this;

        K_SpellData spellData = spellDictionary["V"];
        baseSpell = spellData.prefab;

        drTiers = new Dictionary<string, int>();
        drCooldowns = new Dictionary<string, float>();
        foreach (string spellType in drDictionary.Keys)
        {
            drTiers.Add(spellType, 0);
            drCooldowns.Add(spellType, 0f);
        }

        // Add all spell keys to a dictionary with a boolean as the associated value
        // then use that boolean value to check whether or not that specific category 
        // is in DR
        // NOTE: Alternatively, the system currently determines if a spell category is in DR
        // by whether or not the DR tier is active and/or higher than 0
        foreach (KeyCode keyCode in K_SpellKeys.spellTypes)
        {
            isIgnoreDRLock.Add(keyCode, false);
            Debug.LogFormat($"<color=red> isIgnoreDRLock::  {keyCode} </color>");
        }
    }

    private void Update()
    {
        // Below is a timer that expires the DR lock after a certain period of time
        // That period of time is set in the DR SO in the inspector
        foreach (string spellType in drCooldowns.Keys.ToList())
        {
            if (drCooldowns[spellType] > 0f)
            {
                drCooldowns[spellType] -= Time.deltaTime;

                if (drCooldowns[spellType] <= 0)
                {
                    drTiers[spellType] = 0;

                    // What are these two lines below doing?
                    // The below lines are supposed to deactivate the DR lock for the specified category
                    KeyCode keyCodes = (KeyCode)Enum.Parse(typeof(KeyCode), spellType, false);

                    isIgnoreDRLock[keyCodes] = false;
                }
            }
        }
    }

    public bool GetIgnoreDRLockStatus(KeyCode spellKey)
    {
        return isIgnoreDRLock[spellKey];
    }

    public void SetSpellCategoryDRIgnore(KeyCode spellKey, bool isIgnore)
    {
        isIgnoreDRLock[spellKey] = isIgnore; 
    }

    /// <summary>
    /// Retrieves a spell from the spellDictionary given a spellName and
    /// creates an instance of said spell. The spell names do NOT contain the cast key (G) in their names.
    /// </summary>
    /// <param name="spellName">The name of the spell, acts as a key for spellDictionary.</param>
    /// <returns>The spell component from the instanced spell.</returns>
    public void StringToSpell(string spellName)
    {

        if (!spellDictionary.ContainsKey(spellName))
            return;

        SpellNetworkSpawnRpc();
    }

    [Rpc(SendTo.Server)]
    void SpellNetworkSpawnRpc()
    {
        float yRotation = transform.rotation.eulerAngles.y;
        Quaternion newRotation = Quaternion.Euler(0, yRotation, 0);
        // Change position here to a launch point gameObject
        GameObject spellInstance = Instantiate(baseSpell, (this.transform.position = new Vector3 (1,2,3)), newRotation);

        K_Spell spellComponent = spellInstance.GetComponent<K_Spell>();
        spellComponent.spellData = spellData;

        NetworkObject netObj = spellInstance.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(NetworkManager.LocalClientId);

    }

    /// <summary>
    /// Checks if a given spell exists in the spellDictionary.
    /// </summary>
    /// <param name="spellName">The spell name to be checked.</param>
    /// <returns>A bool representing whether the spell exists or not.</returns>
    public bool SpellExists(string spellName)
    {
        return spellDictionary.ContainsKey(spellName);
    }

    public string GetSpellType(string spellStringSequence)
    {
        return spellDictionary[spellStringSequence].spellType.ToString();
    }

    public string GetSpellCastProcedureType(string spellStringSequence)
    {
        return spellDictionary[spellStringSequence].castProcedure.ToString();
    }

    // Peri includes pre and post "around the time" of the cast
    public string GetPeriCastLockProcedure(string spellStringSequence)
    {
        Debug.LogFormat($"<color=orange> GetPeriCastLockProcedure: {spellStringSequence} </color>");
        return spellDictionary[spellStringSequence].periCastLockProcedure.ToString();
    }

    /// <summary>
    /// Generates a queue of unique random DRKeyData. Applying the
    /// invisible and / or buffered properties to random keys within
    /// the array. The queue length depends on the current DR tier for
    /// the specified spell type.
    /// </summary>
    /// <param name="spellType">The KeyCode representing a specific spell family.</param>
    /// <returns>A queue of unique random DRKeyData objects.</returns>
    public Queue<K_DRKeyData> GetDrKeys(KeyCode spellType)
    {
        string strSpellType = spellType.ToString();
        int drTier = drTiers[strSpellType] - minCasts;

        if (!drDictionary.ContainsKey(strSpellType) || drTier < 0)
            return new Queue<K_DRKeyData>();

        KeyCode[] keys = FisherYatesShuffle(K_SpellKeys.spellTypes);

        drTier = Math.Clamp(drTier, 0, drDictionary[strSpellType].drKeys.Length - 1);
        int keyCount = drDictionary[strSpellType].drKeys[drTier];

        K_DRKeyData[] drKeyData = new K_DRKeyData[keyCount];

        for (int i = 0; i < keyCount; i++)
            drKeyData[i] = new K_DRKeyData(keys[i]);

        int[] drKeysIdx = Enumerable.Range(0, keyCount).ToArray();

        int invisibleKeys = drDictionary[strSpellType].drInvisibleKeys[drTier];
        int[] invisibleKeysIdx = FisherYatesShuffle(drKeysIdx);

        for (int i = 0; i < invisibleKeys; i++)
            drKeyData[invisibleKeysIdx[i]].invisible = true;

        int bufferedKeys = drDictionary[strSpellType].drBufferedKeys[drTier];
        int[] bufferedKeysIdx = FisherYatesShuffle(drKeysIdx);

        for (int i = 0; i < bufferedKeys; i++)
            drKeyData[bufferedKeysIdx[i]].buffered = true;

        lastDrSpellType = strSpellType;

        return new Queue<K_DRKeyData>(drKeyData);
    }

    /// <summary>
    /// Generates a queue of Spell Charging Key Data. 
    /// </summary>
    /// <param name="number">The number of keys to be solved, max 4.</param>
    /// <returns>A queue of unique random DRKeyData objects.</returns>
    public Queue<K_DRKeyData> GetSpellChargingKeys(int number)
    {
        KeyCode[] keys = FisherYatesShuffle(K_SpellKeys.spellTypes);

        int numberOfKeys = Math.Min(number, 4);

        K_DRKeyData[] drKeyData = new K_DRKeyData[numberOfKeys];

        for (int i = 0; i < 2; i++)
            drKeyData[i] = new K_DRKeyData(keys[i]);

        return new Queue<K_DRKeyData>(drKeyData);
    }

    /// <summary>
    /// Updates the DR dictionary entry for a given spell type, increasing
    /// the tier by an amount equal to the cast multiplier value of the
    /// spell being casted. Also sets the cooldown for the spell type.
    /// </summary>
    /// <param name="spellType">The spell family.</param>
    /// <param name="spellName">The spell that's being casted.</param>
    public void UpdateDRTier(KeyCode spellType, string spellName)
    {
        string strSpellType = spellType.ToString();

        if (!drTiers.ContainsKey(strSpellType))
        {
            return;
        }

        int castMultiplier = spellDictionary[spellName].castMultiplier;

        if (drTiers[strSpellType] + castMultiplier < drDictionary[strSpellType].drTimer.Length) {

            drTiers[strSpellType] += castMultiplier;

        }

        SetCooldown(strSpellType);
    }

    /// <summary>
    /// Resets the current cooldown timer for the last spell
    /// type that triggered a DR lock.
    /// </summary>
    public void ResetCooldown()
    {
        Debug.LogFormat($"<color=red> ResetCooldown begins </color>");
        SetCooldown(lastDrSpellType);
    }

    /// <summary>
    /// Sets the corresponding CD to the specified spell type based
    /// on the current DR tier for said spell type.
    /// </summary>
    /// <param name="spellType">The spell type or family.</param>
    public void SetCooldown(string spellType)
    {
        int drTier = drTiers[spellType] - minCasts - 1;

        if (drTier >= 0)
        {
            float[] coolDowns = drDictionary[spellType].drTimer;
            float cd = coolDowns[Mathf.Min(drTier, coolDowns.Length - 1)];
            drCooldowns[spellType] = cd;
            
        }
    }

    /// <summary>
    /// Implementation of the Fisher-Yates shuffling algorithm. It generates
    /// good enough entropy for small arrays while running in O(n) time. It
    /// also makes the shuffle in place, so it has little impact on memory.
    /// </summary>
    /// <param name="arr">The array to be shuffled, accepts any type.</param>
    /// <returns>The shuffled array.</returns>
    public T[] FisherYatesShuffle<T>(T[] arr)
    {
        int n = arr.Length;
        while (n > 1)
        {
            int k = rng.Next(n--);
            T temp = arr[n];
            arr[n] = arr[k];
            arr[k] = temp;
        }

        return arr;
    }
}
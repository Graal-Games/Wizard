using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Singletons;

// Sequencer
public class ChannelingSequencer : NetworkBehaviour
{
    public NetworkVariable<bool> isChanneling = new NetworkVariable<bool>(default,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("Timer")]
    float lastTime;
    // !! A 'what' interval? - time between each letter activation?
    float interval = 0.50f; // was 0.47f
    List<object> keys = new List<object>();

    void Awake()
    {
        isChanneling.Value = false;
    }

    void Update()
    {
        if (!IsLocalPlayer) return;

        if (isChanneling.Value)
        {
            // -- Set this gate's value from the outside
            ChanneledSequencer();
            
        }
    }

    GameObject getRandomKey() 
    {
        int randomIndex = Random.Range(0, keys.Count);

        GameObject randomKey = (GameObject)keys[randomIndex];

        return randomKey;
    }

    void ChanneledSequencer()
    {
        float currentTime = Time.time;

        if (currentTime - lastTime >= interval)
        {
            lastTime = currentTime;

            GameObject randomKey = getRandomKey();

            // Debug.LogFormat($"<color=orange>{}</color>");
            
            if (randomKey.activeSelf == true) 
            {
                getRandomKey();
            } else {
                randomKey.SetActive(true);
            }
           
        }
    }

    public void StartChanneledSequence(params GameObject[] values)
    {
        foreach (GameObject value in values)
        {
            keys.Add(value);
        }

        isChanneling.Value = true;

    }

    public void StopChanneledSequence()
    {
        isChanneling.Value = false;
    }
}

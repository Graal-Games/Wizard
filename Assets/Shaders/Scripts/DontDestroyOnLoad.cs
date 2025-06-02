using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DontDestroyOnLoad : MonoBehaviour
{    void Awake()
    {
        // Check if another instance of this GameObject already exists
        if (GameObject.Find(gameObject.name) != null && GameObject.Find(gameObject.name) != gameObject)
        {
            Destroy(gameObject); // Destroy this instance if a duplicate exists
        }
        else
        {
            DontDestroyOnLoad(gameObject); // Mark this GameObject to persist
        }
    }
}

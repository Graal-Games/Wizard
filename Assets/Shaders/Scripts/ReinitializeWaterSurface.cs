using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReinitializeWaterSurface : MonoBehaviour
{
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reinitialize the water surface
        if (scene.name == "Arena")
        {
            gameObject.SetActive(false);
            gameObject.SetActive(true);
            Debug.Log("Water surface reinitialized.");
        }
    }
}

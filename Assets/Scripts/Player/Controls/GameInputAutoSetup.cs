using UnityEngine;
using System;
using System.Linq;

[DefaultExecutionOrder(-100)] // Execute before other scripts
public class GameInputAutoSetup : MonoBehaviour
{
    void Awake()
    {
        // Check if GameInput exists using reflection
        bool gameInputFound = false;
        
        // Search for GameInput by name
        GameObject gameInputGO = GameObject.Find("GameInput");
        if (gameInputGO != null)
        {
            // Check if it has the GameInput component
            var components = gameInputGO.GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                if (component.GetType().Name == "GameInput")
                {
                    gameInputFound = true;
                    break;
                }
            }
        }
        
        // If not found by GameObject name, search all objects
        if (!gameInputFound)
        {
            var allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
            foreach (var mb in allMonoBehaviours)
            {
                if (mb.GetType().Name == "GameInput")
                {
                    gameInputFound = true;
                    break;
                }
            }
        }
        
        if (!gameInputFound)
        {
            Debug.LogWarning("GameInput not found in scene! Creating one automatically...");
            
            // Create GameInput GameObject
            GameObject newGameInputGO = new GameObject("GameInput");
            
            // Find and add GameInput component using reflection
            var allTypes = System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
                .Concat(System.AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => 
                    {
                        try
                        {
                            return assembly.GetTypes();
                        }
                        catch
                        {
                            return new Type[0];
                        }
                    }));
            
            Type gameInputType = allTypes.FirstOrDefault(t => t.Name == "GameInput" && t.IsSubclassOf(typeof(MonoBehaviour)));
            
            if (gameInputType != null)
            {
                newGameInputGO.AddComponent(gameInputType);
                
                // Make it persistent across scenes if needed
                DontDestroyOnLoad(newGameInputGO);
                
                Debug.Log("GameInput created successfully!");
            }
            else
            {
                Debug.LogError("Could not find GameInput type! Make sure GameInput.cs exists in the project.");
                Destroy(newGameInputGO);
            }
        }
        else
        {
            Debug.Log("GameInput found in scene.");
        }
    }
} 
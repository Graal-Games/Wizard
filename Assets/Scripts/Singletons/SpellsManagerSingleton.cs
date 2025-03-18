using UnityEngine;
using Unity.Netcode;

namespace Singletons
{
    public class SpellsManagerSingleton : MonoBehaviour
    {
        private static SpellsManagerSingleton _instance;
        private SpellsManagerSingleton()
        {
            // Private constructor to prevent instantiation outside of this class
        }

        public static SpellsManagerSingleton Instance
        {
            get
            {
                if (_instance == null)
                {
                    var objs = FindObjectsOfType(typeof(SpellsManagerSingleton)) as SpellsManagerSingleton[];
                    if (objs.Length > 0)
                        _instance = objs[0];
                    if (objs.Length > 1)
                    {
                        Debug.LogError("There is more than one SpellsManager in the scene.");
                    }
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject();
                        obj.name = "_SpellsManager";
                        _instance = obj.AddComponent<SpellsManagerSingleton>();
                    }
                }
                return _instance;
            }
        }

        public class Beam : NetworkBehaviour
        {
            float upkeepTimeSum;
            
            bool isBeamAlive = false;
            
            public float BeamUpkeep(float amount = 0)
            {
                upkeepTimeSum += amount;
                //Debug.Log("BEAM UPKEEP AMOUNT: " + sumAmount);
                return upkeepTimeSum; 
            }

            public void ResetUpkeepAmount()
            {
                upkeepTimeSum = 0;
            }

            public bool IsBeamAlive
            {
                get
                {
                    return isBeamAlive;
                }
                set
                {
                    isBeamAlive = value;
                }
            }
        }
    }
        
}
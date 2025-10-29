using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace SpellsManager
{
    public class Beam : NetworkBehaviour
    {
        static GameObject beam;

        static float upkeepTimeSum;
        static bool isBeamAlive = false;
        static bool isBeamCancelled = false;

        public static float BeamUpkeep(float amount = 0)
        {
            upkeepTimeSum += amount;
            //Debug.Log("BEAM UPKEEP AMOUNT: " + upkeepTimeSum);
            return upkeepTimeSum; 
        }

        public static void ResetUpkeepAmount()
        {
            upkeepTimeSum = 0;
        }

        public static bool IsBeamAlive
        {
            get
            {
                return isBeamAlive;
            }
            set
            {
                isBeamAlive = value;
                //Debug.Log("BEAM LIFE: " + isBeamAlive);
            }
        }

        public static void CancelBeam()
        {
            isBeamCancelled = true;
        }

        public static bool BeamCancelled()
        {
            if (isBeamCancelled == false)
            {
                return false;
            } else {
                isBeamCancelled = false;
                return true;
            }
            
        }

        public static bool BeamDestroyed(ulong beamOwnerId = default, bool isDestroyed = false)
        {
            bool newValue = isDestroyed;
            //Debug.Log("BEAM DESTROYED");
            return newValue;
        }

        public static GameObject CurrentBeamInstance(GameObject beamInstance = null)
        {
            beam = beamInstance;
            return beam;
        }
    }
}

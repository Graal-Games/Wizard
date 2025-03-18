using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Handles channeled sequence input resolution
public class ChanneledCasting : MonoBehaviour
{
    UiReferences uiReferences;



    void Awake()
    {
        uiReferences = GetComponentInParent<UiReferences>();
    }



    // Add tier in params in the future
    public void DeactivateTierLetters()
    {
        foreach (KeyValuePair<string, GameObject> entry 
                 in uiReferences.ChanneledCastingKeys_Tier_1)
        {
            entry.Value.SetActive(false);
        }
    }



    public void HandleInput(string keyPressed, Beam beamScript = default, SphereShield sphereShieldScript = default)
    {
        // If the key pressed is part of the collection of letters associated to the 
        //specific channeled casting tier
        if (uiReferences.ChanneledCastingKeys_Tier_1.ContainsKey(keyPressed))
        {
            // If the key pressed was active, in other words seen on the screen
            if (uiReferences.ChanneledCastingKeys_Tier_1[keyPressed].activeSelf == true)
            {
                // Deactivate that letter
                uiReferences.DeactivateChanneledCastingLetter_Tier1(keyPressed);

                // Add lifetime to the channeled spell
                if (beamScript)
                {
                    beamScript.Upkeep(0.4f);
                } else
                {
                    sphereShieldScript.Upkeep(0.4f);
                }

            } else {

                if (beamScript)
                {
                    beamScript.Upkeep(-0.4f);
                }
                else
                {
                    sphereShieldScript.Upkeep(-0.4f);
                }
            }
        }
    }
}

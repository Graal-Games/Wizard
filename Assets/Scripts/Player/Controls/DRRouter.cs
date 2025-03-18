using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static SpellsDRSpecifications;

public class DRRouter : NetworkBehaviour
{
    SpellsDRSpecifications spellsDRSpecifications;
    [SerializeField] DRTierObjectColorManager dRTierObjectColorManager;
    
    // How many times the spell has been cast
    int beamCount = 0;
    // How many times does the spell need to be cast before DR is activated
    int beamDRActiveCount = 2;

    int aoeCount = 0;
    int aoeDRActiveCount = 1;
    
    int sphereCount = 0;
    int sphereDRActiveCount = 1;

    int charmCount = 0;
    int charmDRActiveCount = 1;

    int barrierCount = 0;
    int barrierDRActiveCount = 1;  


    void Awake()
    {
        spellsDRSpecifications = GetComponentInParent<SpellsDRSpecifications>();

    }

    // void BeamLogic()
    // {
    //     //Debug.LogFormat($"<color=red>CAN DO</color>");
    // }


    #region Beam count and activity variables
    public int BeamCount
    {
        set { beamCount = value; }
        get { return beamCount; }
    }

    public int BeamDRActiveCount
    {
        get { return beamDRActiveCount; }
    }

    #endregion

    #region Aoe count and activity variables
    public int AoeCount
    {
        set { aoeCount = value; }
        get { return aoeCount; }
    }

    public int AoeDRActiveCount
    {
        get { return aoeDRActiveCount; }
    }

    #endregion

    #region Shield count and activity variables
    public int SphereCount
    {
        set { sphereCount = value; }
        get { return sphereCount; }
    }

    public int SphereDRActiveCount
    {
        get { return sphereDRActiveCount; }
    }

    #endregion

    #region Charm cast count and activity variables

    public int CharmDRActiveCount
    {
        get { return charmDRActiveCount; }
    }

    public int BarrierDRActiveCount
    {
        get { return barrierDRActiveCount; }
    }

    #endregion

    #region Barrier cast count and activity variables

    public int CharmCount
    {
        set { charmCount = value; }
        get { return charmCount; }
    }

    public int BarrierCount
    {
        set { barrierCount = value; }
        get { return barrierCount; }
    }

    #endregion

    public DR_TierModel SpellCategoryDRCounter(string value)
    {
        switch(value)
        {
            case "Beam":
                //StartTimerHere + timer resets the variable above
                Debug.LogFormat($"<color=red>{BeamCount}</color>");
                return BeamDRHandler(BeamCount);

            case "Aoe":
                Debug.LogFormat($"<color=red>{AoeCount}</color>");
                return AoeDRHandler(AoeCount);
            
            case "Shield":
                Debug.LogFormat($"<color=red>{SphereCount}</color>");
                return ShieldDRHandler(SphereCount);
            
            case "Charm":
                Debug.LogFormat($"<color=red>{CharmCount}</color>");
                return CharmDRHandler(CharmCount);

            case "Barrier":
                Debug.LogFormat($"<color=red>{BarrierCount}</color>");
                return BarrierDRHandler(BarrierCount);

            default:
            return null;
        }
        
    }



    DR_TierModel BarrierDRHandler(int timesCast)
    {
        // BeamLogic();

        switch (timesCast)
        {
            // Case is the number of casts required to activate the associated tier (as specified for each spell)
            // Under the case is the tier level. They are ascending in order.
            case 2:

                DR_TierModel tier1 = spellsDRSpecifications.Tier1();

                return tier1;

            case 3:

                DR_TierModel tier2 = spellsDRSpecifications.Tier2();

                return tier2;

            case 4:

                DR_TierModel tier3 = spellsDRSpecifications.Tier3();

                return tier3;

            case 5:

                DR_TierModel tier4 = spellsDRSpecifications.Tier4();

                return tier4;

            case 6:

                DR_TierModel tier5 = spellsDRSpecifications.Tier5();

                return tier5;

            case 7:

                DR_TierModel tier6 = spellsDRSpecifications.Tier6();

                return tier6;

            case int n when n >= 8:
                // Handle case for 6 or above
                DR_TierModel tier7 = spellsDRSpecifications.Tier7();

                //Debug.LogFormat($"<color=yellow>tier{n}</color>");
                return tier7;

            default:
                DR_TierModel tier0 = spellsDRSpecifications.Tier0();
                return tier0;
        }
    }



    DR_TierModel BeamDRHandler(int timesCast)
    {
        // BeamLogic();

        switch (timesCast)
        {
            // Case is the number of casts required to activate the associated tier (as specified for each spell)
            // Under the case is the tier level. They are ascending in order.
            case 2:

                DR_TierModel tier1 = spellsDRSpecifications.Tier1();

            return tier1;

            case 3:

                DR_TierModel tier2 = spellsDRSpecifications.Tier2();

                return tier2;

            case 4:

                DR_TierModel tier3 = spellsDRSpecifications.Tier3();

                return tier3;

            case 5:

                DR_TierModel tier4 = spellsDRSpecifications.Tier4();

                return tier4;

            case 6:

                DR_TierModel tier5 = spellsDRSpecifications.Tier5();

                return tier5;

            case 7:

                DR_TierModel tier6 = spellsDRSpecifications.Tier6();

                return tier6;

            case int n when n >= 8:
                // Handle case for 6 or above
                DR_TierModel tier7 = spellsDRSpecifications.Tier7();

                //Debug.LogFormat($"<color=yellow>tier{n}</color>");
                return tier7;

            default:
                DR_TierModel tier0 = spellsDRSpecifications.Tier0();
                return tier0;
        }   
    }

    // This is currently instant inputs
    // We could either replace or mix this with buffer-slown inputs

    // DR_TierModel AoeDRHandler(int timesCast, string v)
    DR_TierModel AoeDRHandler(int timesCast)
    {
        switch (timesCast)
        {
            case 2:

                DR_TierModel tier1 = spellsDRSpecifications.Tier1();

                return tier1;

            case 3:

                DR_TierModel tier2 = spellsDRSpecifications.Tier2();

                return tier2;

            case 4:

                DR_TierModel tier3 = spellsDRSpecifications.Tier3();

                return tier3;

            case 5:

                DR_TierModel tier4 = spellsDRSpecifications.Tier4();

                return tier4;

            case 6:

                DR_TierModel tier5 = spellsDRSpecifications.Tier5();

                return tier5;

            case 7:

                DR_TierModel tier6 = spellsDRSpecifications.Tier6();

                return tier6;

            case int n when n >= 8:

                // Handle case for 7 or above
                DR_TierModel tier7 = spellsDRSpecifications.Tier7();

                return tier7;

            // default:
            // DR_TierModel defaultTier = spellsDRSpecifications.Tier0();
            // return defaultTier;
            default:
                return null;
        }
    }

    DR_TierModel ShieldDRHandler(int timesCast)
    {
        switch (timesCast)
        {
            case 1:
            DR_TierModel tier0 = spellsDRSpecifications.Tier0();
            return tier0;

            case 2:
            DR_TierModel tier1 = spellsDRSpecifications.Tier1();
            return tier1;

            case 3:
            DR_TierModel tier2 = spellsDRSpecifications.Tier2();
            return tier2;

            case 4:
            DR_TierModel tier3 = spellsDRSpecifications.Tier3();
            return tier3;

            case 5:
            DR_TierModel tier4 = spellsDRSpecifications.Tier4();
            return tier4;

            case 6:
            DR_TierModel tier5 = spellsDRSpecifications.Tier5();
            return tier5;

            case int n when n >= 7:
            // Handle case for 7 or above
            DR_TierModel tier6 = spellsDRSpecifications.Tier6();
            return tier6;

            default:
            return null;
        }
    }

    DR_TierModel CharmDRHandler(int timesCast)
    {
        switch (timesCast)
        {
            case 2:
            DR_TierModel tier1 = spellsDRSpecifications.Tier1();
            return tier1;

            case 3:
            DR_TierModel tier2 = spellsDRSpecifications.Tier2();
            return tier2;

            case 4:
            DR_TierModel tier3 = spellsDRSpecifications.Tier3();
            return tier3;

            case 5:
            DR_TierModel tier4 = spellsDRSpecifications.Tier4();
            return tier4;

            case 6:
            DR_TierModel tier5 = spellsDRSpecifications.Tier5();
            return tier5;

            case int n when n >= 7:
            // Handle case for 7 or above
            DR_TierModel tier6 = spellsDRSpecifications.Tier6();
            return tier6;

            default:
            DR_TierModel tier0 = spellsDRSpecifications.Tier0();
            return tier0;
        }
    }

    [ServerRpc]
    void DRObjectTierColorChangerServerRpc()
    {

    }
}

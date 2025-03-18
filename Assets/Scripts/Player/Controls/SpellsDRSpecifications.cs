using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class SpellsDRSpecifications : MonoBehaviour
{
    // rename to DR_TierModel
    public class DR_TierModel
    {
        public bool drActive { get; set; }
        public int keysAmount { get; set; }
        public bool isLettersHidden { get; set; }
        public int DRactivationBeginsAt { get; set; }
    }

    // Issue with returning keysAmount 0 (In case the spell cast is interrupted)
    // >> Spell can no longer be cast
    public DR_TierModel Tier0()
    {
        DR_TierModel Tier0_payload = new DR_TierModel();

        Tier0_payload.keysAmount = 1;
        Tier0_payload.isLettersHidden = false;
        Tier0_payload.drActive = false;
        
        return Tier0_payload;
    }

    // Which tier is triggered first is determined in the router
    public DR_TierModel Tier1()
    {
        DR_TierModel Tier1_payload = new DR_TierModel();

        Tier1_payload.keysAmount = 2;
        Tier1_payload.isLettersHidden = false;
        Tier1_payload.DRactivationBeginsAt = 1;

        return Tier1_payload;
    }

    public DR_TierModel Tier2()
    {
        DR_TierModel Tier2_payload = new DR_TierModel();

        Tier2_payload.keysAmount = 3;
        Tier2_payload.isLettersHidden = false;

        return Tier2_payload;
    }

    public DR_TierModel Tier3()
    {
        DR_TierModel Tier3_payload = new DR_TierModel();

        Tier3_payload.keysAmount = 4;
        Tier3_payload.isLettersHidden = false;

        return Tier3_payload;
    }

    public DR_TierModel Tier4()
    {
        DR_TierModel Tier4_payload = new DR_TierModel();

        Tier4_payload.keysAmount = 5;
        Tier4_payload.isLettersHidden = false;

        return Tier4_payload;
    }

    public DR_TierModel Tier5()
    {
        DR_TierModel Tier5_payload = new DR_TierModel();

        Tier5_payload.keysAmount = 5;
        Tier5_payload.isLettersHidden = true;

        return Tier5_payload;
    }

    public DR_TierModel Tier6()
    {
        DR_TierModel Tier6_payload = new DR_TierModel();

        Tier6_payload.keysAmount = 6;
        Tier6_payload.isLettersHidden = true;

        return Tier6_payload;
    }

    public DR_TierModel Tier7()
    {
        DR_TierModel Tier7_payload = new DR_TierModel();

        Tier7_payload.keysAmount = 7;
        Tier7_payload.isLettersHidden = true;

        return Tier7_payload;
    }

}

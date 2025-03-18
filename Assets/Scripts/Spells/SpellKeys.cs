using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellKeys : MonoBehaviour
{

    [Header("Primary Actions")]
    public string BeginCast = "G";
    public string modify = "S"; // Deprecated?

    [Header("Base spells")]
    public string Bolt = "V"; 
    public string Sphere = "T"; 
    public string Beam = "B";
    public string Aoe = "F";
    public string Barrier = "Y";
    public string Invocation = "N";
    public string Charm = "R"; 
    public string Conjuration = "H"; 

    [Header("Elements")]
    string Fire = "P";
    string Water = "U";
    string Air = "O";
    string Earth = "I";

    [Header("Base Projectiles")]
    List<string> arcane_Projectile = new List<string>  { "G", "V", "G"}; // Arcane

    List<string> fireProjectile = new List<string>  { "G", "V", "K", "G"}; // Fire
    List<string> waterProjectile = new List<string>  { "G", "V", "M", "G"}; // Water
    List<string> airProjectile = new List<string>  { "G", "V", "U", "G"}; // Air
    List<string> earthProjectile = new List<string>  { "G", "V", "J", "G"}; // Earth

    List<string> placeableArcaneAoe = new List<string> {"G", "R", "F", "G"};
    

    List<string> StarFall = new List<string> {"G", "F", "V", "G"};
    List<string> MeteorShower = new List<string> {"G", "F", "V", "U", "P", "G"};

    List<string[]> spells = new List<string[]>();

    public Dictionary<string, string> SpellBook = new Dictionary<string, string>
    {
        // Legend: (under construction)
        // -1: The name of the spell is proceeded by the amount of letters is the sequence left before it is/can be cast 
        // [T]: Means that the spell is Transmutable
        // [SM]: Means the spell is shape modifiable
        { "G", "Cast" }, 

        // #############################
        // Base + Transmuted Projectiles - V
        // #############################

        { "GV", "Arcane Projectile -1 [T]" },

        { "GVG", "Arcane Projectile" },

        // In Progress
        { "GVU", "Air Projectile -1" },
        { "GVJ", "Earth Projectile -1" },
        { "GVM", "Water Projectile -1" },
        { "GVK", "Fire Projectile -1" },

        // Full Sequence

        { "GVUG", "Air Projectile" },
        { "GVJG", "Earth Projectile" },
        { "GVMG", "Water Projectile" },
        { "GVKG", "Fire Projectile" },

        // ######################
        // Base + Transmuted AoEs - F
        // ######################
        { "GF", "ArcaneAoe -1 [T]" },

        { "GFG", "Arcane Aoe" },

        // In Progress
        { "GFU", "Air Aoe-1" },
        { "GFJ", "Earth Aoe-1" },
        { "GFM", "Water Aoe-1" },
        { "GFK", "Fire Aoe-1" },

        // Full Sequence
        { "GFUG", "Air Aoe" },
        { "GFJG", "Earth Aoe" },
        { "GFMG", "Water Aoe" },
        { "GFKG", "Fire Aoe" },

        // #############################
        // Base + Transmuted Beams - B
        // #############################

        { "GB", "Arcane Beam -1 [T]" },
        
        // Arcane Beam - Full Sequence
        { "GBG", "Arcane Beam" },

        { "GBU", "Air Beam -1" },
        { "GBJ", "Earth Beam -1" },
        { "GBM", "Water Beam -1" },
        { "GBK", "Fire Beam -1" },

        
        { "GBUG", "Air Beam" },
        { "GBJG", "Earth Beam" },
        { "GBMG", "Water Beam" },
        { "GBKG", "Fire Beam" },
        
        // #############################
        // Base + Transmuted Barriers - Y
        // #############################

        { "GY", "Arcane Barrier -1 [T]" },

        // Arcane Barrier - Full Sequence
        { "GYG", "Arcane Barrier" },

        // Arcane Barrier - Transmuted - In Progress
        { "GYU", "Air Barrier -1" },
        { "GYJ", "Earth Barrier -1" },
        { "GYM", "Water Barrier -1" },
        { "GYK", "Fire Barrier -1" },

        // Arcane Barrier - Transmuted - Full Sequence
        { "GYUG", "Air Barrier" },
        { "GYJG", "Earth Barrier" },
        { "GYMG", "Water Barrier" },
        { "GYKG", "Fire Barrier" },
        
        // #############################
        // Base + Transmuted Shields - T
        // #############################

        { "GT", "Arcane Shield -1 [T]" }, // Could be named sphere depending on functionality

        // Arcane Shield - Full Sequence
        { "GTG", "Arcane Shield" },

        // Transmuted Shield - In Progress
        { "GTU", "Air Shield -1" },
        { "GTJ", "Earth Shield -1" },
        { "GTM", "Water Shield -1" },
        { "GTK", "Fire Shield -1" },

        // Transmuted Shield - Full Sequence
        { "GTUG", "Air Shield" },
        { "GTJG", "Earth Shield" },
        { "GTMG", "Water Shield" },
        { "GTKG", "Fire Shield" },  
        
        // #############################
        // Base - CHARMS - R
        // #############################

        { "GH", "Conjuration -2 -5 -6" }, // Could be sphere depending on functionality

        // =================
        //       MIST
        // =================

        // Mist - In Progress
        { "GHF", "GRF: Placeable Conjuration Spell" },
        { "GHFK", "GHFK: Mist -2" },
        { "GHFKM", "GHFKM: Mist -1" },
        // Full Sequence
        { "GRFKMG", "GHFKMG: Mist" },

        // =================
        //    Aoe Barrier
        // ================= 
        // Aoe Barrier - In Progress
        { "GHFY", "GHFY: Arcane Aoe Barrier -1 [T][SM]" },
        // Aoe Barrier - Full Sequence
        { "GHFYG", "GHFYG: Arcane Aoe Barrier" },

        // Aoe Barrier - Transmutation - In Progress
        { "GHFYU", "GHFYU: Aoe Long AIR Barrier -1" },
        { "GHFYJ", "GHFYJ: Aoe Long EARTH Barrier -1" },
        { "GHFYM", "GHFYM: Aoe Long WATER Barrier -1" },
        { "GHFYK", "GHFYK: Aoe Long FIRE Barrier -1" },

        // Aoe Barrier - Transmuted - Full Sequence
        { "GHFYUG", "GHFYUG: Aoe Long AIR Barrier" },
        { "GHFYJG", "GHFYJG: Aoe Long EARTH Barrier" },
        { "GHFYMG", "GHFYMG: Aoe Long WATER Barrier" },
        { "GHFYKG", "GHFYKG: Aoe Long FIRE Barrier" },


        // =====================
        //    Aoe Long Barrier - (Aoe Barrier SM)
        // =====================

        // Aoe Long Barrier - In Progress
        { "GHFYB", "GHFYB: Aoe Long Barrier -1 [T]" },
        // Aoe Long Barrier - Full Sequence
        { "GHFYBG", "GHFYBG: Aoe Long Barrier" },

        // Aoe Long Transmuted Barrier - In Progress - T
        { "GHFYBU", "GHFYBU: Aoe Long AIR Barrier -1" },
        { "GHFYBJ", "GHFYBJ: Aoe Long EARTH Barrier -1" },
        { "GHFYBM", "GHFYBM: Aoe Long WATER Barrier -1" },
        { "GHFYBK", "GHFYBK: Aoe Long FIRE Barrier -1" },

        // Aoe Long Transmuted Barrier - Full sequence
        { "GHFYBUG", "GHFYBUG: Aoe Long AIR Barrier" },
        { "GHFYBJG", "GHFYBJG: Aoe Long EARTH Barrier" },
        { "GHFYBMG", "GHFYBMG: Aoe Long WATER Barrier" },
        { "GHFYBKG", "GHFYBKG: Aoe Long FIRE Barrier" }
  
    };

    private void Start()
    {
        // Optional: Make the spellbook keys a concatenated string in order to make it easier to edit the spells' key values more easily
        // Debug.Log("Spellbook entries: " + SpellBook.Count);

    }

    // (**) The case conditions need to be changed to the names of the spells
    public string BaseSpells(string key)
    {
        switch (key){
            case "R":
                return "R";
            case "T":
                return "T";
            case "Y":
                return "Y";

            case "F":
                return "F";
            case "H":
                return "H";
            
            case "V":
                return "V";
            case "B":
                return "B";
            case "N":
                return "N";

            default:
                return null;
        }
    }
    
    public string Elements(string element)
    {
        // Adds an element to compatible spells
        //Was looking into changing the elements to the following keys:
        //Fire: J, Air: K, Water: U, Earth: M

        if (element == "U") element = "earth";
        if (element == "I") element = "water";
        if (element == "O") element = "air";
        if (element == "P") element = "fire";

        switch (element){
            // Earth
            case "earth":
                return "U";
            // Water
            case "water":
                return "I";
            // Air
            case "air":
                return "O";
            // Fire
            case "fire":
                return "P";

            //case "arcane":
            //    return "arcane";

            default:
                return "arcane";
        }
        
    }
    
    // Concept revision - Not sure it will be on left or right side
    public string ModifySpell(string mod)
    {
        switch (mod){
            // Dispell ??
            case "Q":
                return "Q";
            // Duration
            case "W":
                return "W";
            // Speed
            case "E":
                return "E";

            // Move
            case "A":
                return "A";
            // Size
            case "D":
                return "D";

            // Quantity
            case "Z":
                return "Z";
            // Augment
            case "X":
                return "X";  
            case "C":
                return "C";

            default:
                return null;
        }
    }

}

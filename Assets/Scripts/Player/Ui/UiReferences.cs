using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

// In this script will be handled all the references to the keys
//seen on the screen when casting spells and 

public class UiReferences : NetworkBehaviour
{
    // Left
    [SerializeField] GameObject Q;
    [SerializeField] GameObject W;
    [SerializeField] GameObject E;
    [SerializeField] GameObject A;
    [SerializeField] GameObject S;
    [SerializeField] GameObject D;
    [SerializeField] GameObject Z;
    [SerializeField] GameObject X;
    [SerializeField] GameObject C;

    // Middle
    [SerializeField] GameObject G;
    [SerializeField] GameObject G_Icon_Cast;
    [SerializeField] GameObject R;
    [SerializeField] GameObject R_Icon_Charm;
    [SerializeField] GameObject T;
    [SerializeField] GameObject T_Icon_Shield;
    [SerializeField] GameObject Y;
    [SerializeField] GameObject Y_Icon_Barrier;

    [SerializeField] GameObject F;
    [SerializeField] GameObject F_Icon_Aoe;
    [SerializeField] GameObject H;
    [SerializeField] GameObject H_Icon_Invoke;

    [SerializeField] GameObject V;
    [SerializeField] GameObject V_Icon_Bolt;
    [SerializeField] GameObject N;
    [SerializeField] GameObject N_Icon_Summon;
    [SerializeField] GameObject B;
    [SerializeField] GameObject B_Icon_Beam;

    // Right
    [SerializeField] GameObject U;
    [SerializeField] GameObject I;
    [SerializeField] GameObject O;
    [SerializeField] GameObject P;
    [SerializeField] GameObject J;
    [SerializeField] GameObject K;
    [SerializeField] GameObject L;
    [SerializeField] GameObject M;

    private Dictionary<string, GameObject> allKeys;

    private Dictionary<string, GameObject> channeledCastingKeys_Tier_1;

    void Awake()
    {
        

        SetActiveBaseSpellsIcons(false);

        allKeys = new Dictionary<string, GameObject>
        {
            { "A", A },
            { "B", B },
            { "C", C },
            { "D", D },
            { "E", E },
            { "F", F },
            { "G", G },
            { "H", H },
            { "I", I },
            { "J", J },
            { "K", K },
            { "L", L },
            { "M", M },
            { "N", N },
            { "O", O },
            { "P", P },
            { "Q", Q },
            { "R", R },
            { "S", S },
            { "T", T },
            { "U", U },
            { "V", V },
            { "W", W },
            { "X", X },
            { "Y", Y },
            { "Z", Z }
        };

        // These should probably be defined in a different script
        channeledCastingKeys_Tier_1 = new Dictionary<string, GameObject>
        {
            { "R", R },
            { "T", T },
            { "Y", Y },

            { "F", F },
            { "H", H },

            { "V", V },
            { "B", B },
            { "N", N }
        };

        DeactivateKeysOnAwake();
    }

    //Deactivate all letters except for beginCast and its icon
    void DeactivateKeysOnAwake()
    {
        foreach (var element in allKeys)
        {
            element.Value.SetActive(false);
        }

        G_Icon_Cast.SetActive(true);
        G.SetActive(true);

    }

    // When implementing tier 2 and 3, DR timer will also have to be implemented
    //currently not implemented
    public Dictionary<string, GameObject> AllKeys
    {
        get { return allKeys; }
    }


    public Dictionary<string, GameObject> ChanneledCastingKeys_Tier_1
    {
        get { return channeledCastingKeys_Tier_1; }
    }

    public void DeactivateChanneledCastingLetter_Tier1(string keyLetter)
    {
        if (channeledCastingKeys_Tier_1.ContainsKey(keyLetter))
        {
            channeledCastingKeys_Tier_1[keyLetter].SetActive(false);
        }
    }


    void ActivateLetter()
    {

    }

    void DeactivateLetter()
    {

    }

    // GameObject GetUiGameObject(string reference)
    // {
        
    // }


    void SetActiveBaseSpellsIcons(bool value)
    {
        R_Icon_Charm.SetActive(value);
        T_Icon_Shield.SetActive(value);
        Y_Icon_Barrier.SetActive(value);
        F_Icon_Aoe.SetActive(value);
        H_Icon_Invoke.SetActive(value);
        V_Icon_Bolt.SetActive(value);
        N_Icon_Summon.SetActive(value);
        B_Icon_Beam.SetActive(value);
    }

    public void ActivateSpellIcons()
    {
        SetActiveBaseSpellsIcons(true);
    }

    public void DeactivateSpellIcons()
    {
        SetActiveBaseSpellsIcons(false);
    }
}

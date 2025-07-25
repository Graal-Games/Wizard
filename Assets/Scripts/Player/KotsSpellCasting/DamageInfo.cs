// This struct is used to send information (through an event) about the spell that hit a player for damage and effects handling
using UnityEngine;

public struct PlayerHitPayload
{
    int networkId;
    ulong playerId;
    string spellElement;
    IncapacitationName incapacitationName;
    float incapacitationDuration;
    VisionImpairment visionImpairment;
    float visionImpairmentDuration;
    float directDamageAmount;
    float damageOverTimeAmount;
    float damageOverTimeDuration;
    float healAmount;
    float healOverTimeAmount;
    SpellAttribute spellAttribute;
    bool pushback;

    public int NetworkId
    { 
        get { return networkId; } 
        set { networkId = value; }
    }

    public ulong PlayerId
    { 
        get { return playerId; } 
        set { playerId = value; }
    }

    public string SpellElement
    { 
        get { return spellElement; } 
        set { spellElement = value; }
    }

    public IncapacitationName IncapacitationName
    {
        get { return incapacitationName; }
        set { incapacitationName = value; }
    }
    
    public float IncapacitationDuration
    {
        get { return incapacitationDuration; }
        set { incapacitationDuration = value; }
    }

    public VisionImpairment VisionImpairment
    {
        get { return visionImpairment; }
        set { visionImpairment = value; }
    }

    public float VisionImpairmentDuration
    {
        get { return visionImpairmentDuration; }
        set { visionImpairmentDuration = value; }
    }

    public float DirectDamageAmount
    { 
        get { return directDamageAmount; } 
        set { directDamageAmount = value; }
    }
    public float DamageOverTimeAmount
    { 
        get { return damageOverTimeAmount; } 
        set { damageOverTimeAmount = value; }
    }
    public float DamageOverTimeDuration
    { 
        get { return damageOverTimeDuration; } 
        set { damageOverTimeDuration = value; }
    }
    public SpellAttribute SpellAttribute
    { 
        get { return spellAttribute; } 
        set { spellAttribute = value; }
    }

    public string SpellCategory
    { 
        get { return spellElement; } 
        set { spellElement = value; }
    }

    public bool Pushback
    { 
        get { return pushback; } 
        set { pushback = value; }
    }

    public float HealAmount 
    {
        get { return healAmount; }
        set { healAmount = value; }
    }

    //public float DotDuration { get; internal set; }

    // Constructor to initialize the fields
    public PlayerHitPayload(
        int netId, 
        ulong pId, 
        string element, 
        IncapacitationName incapName, 
        float incapDur, VisionImpairment 
        visionImp, 
        float visionImpDur, 
        float ddAmount, 
        float dotAmount, 
        float dotDuration,
        float hlAmount,
        float hOTAmount,
        SpellAttribute attribute, 
        bool pushbackp)
    {
        networkId = netId;
        playerId = pId;
        spellElement = element;
        incapacitationName = incapName;
        incapacitationDuration = incapDur;
        visionImpairment = visionImp;
        visionImpairmentDuration = visionImpDur;
        directDamageAmount = ddAmount; 
        damageOverTimeAmount = dotAmount;
        damageOverTimeDuration = dotDuration;
        healAmount = hlAmount;
        healOverTimeAmount = hOTAmount;
        spellAttribute = attribute;
        pushback = pushbackp;
    }
}

//public struct PlayerHitPayload2
//{
//    int networkId;
//    ulong playerId;
//    ScriptableObject spellScriptableObject;

//    public int NetworkId
//    { 
//        get { return networkId; } 
//        set { networkId = value; }
//    }

//    public ulong PlayerId
//    { 
//        get { return playerId; } 
//        set { playerId = value; }
//    }

//    public ScriptableObject SpellScriptableObject
//    { 
//        get { return spellScriptableObject; } 
//        set { spellScriptableObject = value; }
//    }

//    // Constructor to initialize the fields
//    public PlayerHitPayload2(int netId, ulong pId, ScriptableObject spellSO)
//    {
//        networkId = netId;
//        playerId = pId;
//        spellScriptableObject = spellSO;
//    }
//}
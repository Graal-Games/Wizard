using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Spell")]
public class K_SpellData : ScriptableObject
{
    [Header("Type & Element")]
    [Tooltip("The spell type (Projectile, AoE, Shield, etc...)")]
    public SpellType spellType;
    [Tooltip("The spell element (Water, Fire, Air, etc...)")]
    public Element element;    
    [Tooltip("damage type(Damage-over-time, Direct Damage, Hybrid")]
    public SpellAttribute spellAttribute;
    [Tooltip("The spell's cast type or procedure")]
    public CastType castProcedure;
    //[Tooltip("The type of incapacitation")]
    //public IncapacitationType incapacitationType; 
    [Tooltip("The name of incapacitation")]
    public IncapacitationName incapacitationName; 
    [Tooltip("The name of incapacitation")]
    public VisionImpairment visionImpairmentType; 

    [Header("Spell Parameters")]
    [Tooltip("The amount of \"casts\" this spell costs regarding the DR logic.")]
    [Min(1)] public int castMultiplier = 1;

    [Tooltip("The speed at wich the spell changes position or size")]
    [Min(0f)] public float moveSpeed;

    [Tooltip("The duration in seconds of the spell. Set to 0 for infinite duration")]
    [Min(0f)] public float spellDuration;

    [Tooltip("The amount of time the player is incapacitated for")]
    [Min(0f)] public float spellActivationDelay;

    [Tooltip("The amount of time the player is incapacitated for")]
    [Min(0f)] public float incapacitationDuration; 
    [Tooltip("The amount of time the inflicted player will be vision impaired")]
    [Min(0f)] public float visionImpairmentDuration; 
    [Tooltip("The amount of damage on contact with the spell")]
    [Min(0f)] public float directDamageAmount;    
    [Tooltip("The amount of  over time on contact with the spell")]
    [Min(0f)] public float damageOverTimeAmount;
    [Tooltip("The duration of the damage over time on contact with the spell")]
    [Min(0f)] public float damageOverTimeDuration;
    [Tooltip("If the spell has pushback the value of this should be > 0 to apply the effect")]
    [Min(0f)] public float pushForce; // A value between 100 and 150 is noticable
    [Tooltip("If the spell has pull the value of this should be > 0 to apply the effect")]
    [Min(0f)] public float pullForce; // A value between 100 and 150 is noticable
    // debuff (slow, lower/increase resistance)
    //[Tooltip("DoT tick duration in seconds. Set to 0 to NOT cause DoT")]
    //[Min(0f)] public float dotTickDuration;
    [Tooltip("The \"health\" of the spell. Set to 0 for infinite health")]
    [Min(0f)] public float health; 


    [Header("Collision Settings")]
    [Tooltip("The layers that can recive damage from this spell")]
    public LayerMask damageLayers;
    [Tooltip("The layers that the spell can collide with")]
    public LayerMask collisionLayers;
    [Tooltip("If true, the spell will be destroyed after detecting ANY collision")]
    public bool destroyOnCollision;
    [Tooltip("If true, the spell will be destroyed on contact with a player")]
    public bool destroyOnPlayerCollision;
    [Tooltip("If true, the spell can cause damage to it's own caster")]
    public bool friendlyFire;
    [Tooltip("If true, the spell causes pushback")]
    public bool pushback;

    [Header("Spell Body")]
    [Tooltip("The prefab to be created for this Spell")]
    public GameObject prefab;
    [Tooltip("The child prefab that this main spell body spawns")]
    public GameObject childPrefab;
    //[Tooltip("The placement prefab that gives visual feedback for placeable spells")]
    //public GameObject placementPrefab;
}

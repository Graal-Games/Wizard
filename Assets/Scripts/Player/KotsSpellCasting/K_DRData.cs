using UnityEngine;
using System.Linq;
using System;

/// <summary>
/// A few notes here:
/// - The index of any of the previously mentioned arrays, represents the DR tier, so the
///   values of the arrays are related when pointing to the same index. For example: For
///   the index 0, drKeys[0], drBufferedKeys[0] and drTimer[0] represent the settings for
///   the first DR tier.  
/// 
/// - The values in drBufferedKeys should be equal or smaller than their counterpart on
///   the drKeys array.
/// </summary>
[CreateAssetMenu(fileName = "New DR Data", menuName = "DR Data")]
public class K_DRData : ScriptableObject
{
    [Tooltip("The spell family (Projectile, Beam, AoE, ...)")]
    public SpellType spellType;
    [Tooltip("The amount of keys to prompt for every DR tier.")]
    public int[] drKeys;
    [Tooltip("The amount of keys to show as invisible keys.")]
    public int[] drInvisibleKeys;
    [Tooltip("The amout of keys that should be buffered for every DR tier.")]
    public int[] drBufferedKeys;
    [Tooltip("The cool down time in seconds for every DR tier.")]
    public float[] drTimer;

    [SerializeField, HideInInspector] private int drKeysCheck;
    [SerializeField, HideInInspector] private int drInvisibleKeysCheck;
    [SerializeField, HideInInspector] private int drBufferedKeysCheck;
    [SerializeField, HideInInspector] private int drTimerCheck;

    private void OnValidate()
    {
        int drTiers;

        for (int i = 0; i < drKeys.Length; i++)
        {
            if (drInvisibleKeys[i] > drKeys[i])
            {
                drInvisibleKeys[i] = drKeys[i];
            }

            if (drBufferedKeys[i] > drKeys[i])
            {
                drBufferedKeys[i] = drKeys[i];
            }
        }

        if (drKeys.Length != drKeysCheck)
            drTiers = drKeys.Length;
        else if (drInvisibleKeys.Length != drInvisibleKeysCheck)
            drTiers = drInvisibleKeys.Length;
        else if (drBufferedKeys.Length != drBufferedKeysCheck)
            drTiers = drBufferedKeys.Length;
        else if (drTimer.Length != drTimerCheck)
            drTiers = drTimer.Length;
        else
            return;

        if (drKeys.Length < drTiers)
            drKeys = drKeys.Concat(new int[drTiers - drKeys.Length]).ToArray();
        else if (drKeys.Length > drTiers)
        {
            int[] trucatedArr = new int[drTiers];
            Array.Copy(drKeys, trucatedArr, drTiers);
            drKeys = trucatedArr;
        }

        if (drInvisibleKeys.Length < drTiers)
            drInvisibleKeys = drInvisibleKeys.Concat(new int[drTiers - drInvisibleKeys.Length]).ToArray();
        else if (drInvisibleKeys.Length > drTiers)
        {
            int[] trucatedArr = new int[drTiers];
            Array.Copy(drInvisibleKeys, trucatedArr, drTiers);
            drInvisibleKeys = trucatedArr;
        }

        if (drBufferedKeys.Length < drTiers)
            drBufferedKeys = drBufferedKeys.Concat(new int[drTiers - drBufferedKeys.Length]).ToArray();
        else if (drBufferedKeys.Length > drTiers)
        {
            int[] trucatedArr = new int[drTiers];
            Array.Copy(drBufferedKeys, trucatedArr, drTiers);
            drBufferedKeys = trucatedArr;
        }

        if (drTimer.Length < drTiers)
            drTimer = drTimer.Concat(new float[drTiers - drTimer.Length]).ToArray();
        else if (drTimer.Length > drTiers)
        {
            float[] trucatedArr = new float[drTiers];
            Array.Copy(drTimer, trucatedArr, drTiers);
            drTimer = trucatedArr;
        }

        drKeysCheck = drKeys.Length;
        drInvisibleKeysCheck = drInvisibleKeys.Length;
        drBufferedKeysCheck = drBufferedKeys.Length;
        drTimerCheck = drTimer.Length;
    }
}

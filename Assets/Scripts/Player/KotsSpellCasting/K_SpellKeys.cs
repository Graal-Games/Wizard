using UnityEngine;

public class K_SpellKeys
{
    public static readonly KeyCode cast = KeyCode.Mouse0;
    public static readonly KeyCode[] spellTypes = new KeyCode[] { KeyCode.LeftControl, KeyCode.Z, KeyCode.X, KeyCode.E, KeyCode.C, KeyCode.Q, KeyCode.R, KeyCode.LeftShift };
    public static readonly KeyCode[] spellElements = new KeyCode[] { KeyCode.Alpha1, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha2 };
}

public class K_SpellKeys_Legacy
{
    public static readonly KeyCode cast = KeyCode.G;
    public static readonly KeyCode[] spellTypes = new KeyCode[] { KeyCode.V, KeyCode.B, KeyCode.N, KeyCode.F, KeyCode.H, KeyCode.R, KeyCode.T, KeyCode.Y };
    public static readonly KeyCode[] spellElements = new KeyCode[] { KeyCode.U, KeyCode.J, KeyCode.K, KeyCode.M };
}
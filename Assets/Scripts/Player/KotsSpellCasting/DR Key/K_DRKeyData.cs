using UnityEngine;

/// <summary>
/// This class just holds some values for the DR Keys. It's meant
/// to be used by the SpellBuilder to send the DR Keys to the
/// SpellLauncher.
/// </summary>
public class K_DRKeyData
{
    public KeyCode keyCode;
    public bool invisible;
    public bool buffered;

    public K_DRKeyData(KeyCode keyCode)
    {
        this.keyCode = keyCode;
    }
}
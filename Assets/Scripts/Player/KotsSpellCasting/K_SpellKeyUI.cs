using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Lightweight UI helper to change icon/label for a spell key at runtime
/// without touching DR-specific behavior.
/// Attach this to the same GameObject that has K_DRKey.
/// </summary>
public class K_SpellKeyUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text label;

    public void SetIcon(Sprite sprite)
    {
        if (icon == null) return;
        icon.sprite = sprite;
        icon.enabled = sprite != null;
    }

    public void SetLabel(string text)
    {
        if (label == null) return;
        label.text = text;
    }
}



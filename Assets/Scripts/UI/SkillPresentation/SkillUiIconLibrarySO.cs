using System;
using UnityEngine;

public enum CombatUiStatusIconKind
{
    Guard,
    Burn,
    Bleed,
    Mark,
    Freeze,
    Chilled,
    Ailment
}

[CreateAssetMenu(menuName = "Game/UI/Combat UI Icon Library", fileName = "CombatUiIconLibrary")]
public class SkillUiIconLibrarySO : ScriptableObject
{
    [Serializable]
    public struct ElementIconEntry
    {
        public ElementType element;
        public Sprite icon;
        public Color slotBackgroundColor;
        public Color badgeBackgroundColor;
        public Color iconTint;
    }

    [Serializable]
    public struct StatusIconEntry
    {
        public CombatUiStatusIconKind kind;
        public Sprite icon;
        public Color backgroundColor;
        public Color iconTint;
    }

    [Serializable]
    public struct DiceFaceEnchantIconEntry
    {
        public DiceFaceEnchantKind enchant;
        public Sprite icon;
        public Color iconTint;
    }

    [Header("Element Visuals")]
    [SerializeField] private ElementIconEntry[] elementIcons = Array.Empty<ElementIconEntry>();

    [Header("Dice Cost Icons")]
    [SerializeField] private Sprite oneSlotDiceIcon;
    [SerializeField] private Sprite twoSlotDiceIcon;
    [SerializeField] private Sprite threeSlotDiceIcon;

    [Header("Status Icons")]
    [SerializeField] private StatusIconEntry[] statusIcons = Array.Empty<StatusIconEntry>();

    [Header("Dice Face Enchant Icons")]
    [SerializeField] private DiceFaceEnchantIconEntry[] diceFaceEnchantIcons = Array.Empty<DiceFaceEnchantIconEntry>();
    [SerializeField] private Sprite brokenFaceIcon;
    [SerializeField] private Color brokenFaceIconTint = Color.white;

    public bool TryGetElementIcon(ElementType element, out Sprite icon, out Color badgeBackgroundColor, out Color iconTint)
    {
        return TryGetElementVisual(element, out _, out icon, out badgeBackgroundColor, out iconTint);
    }

    public bool TryGetElementVisual(ElementType element, out Color slotBackgroundColor, out Sprite icon, out Color badgeBackgroundColor, out Color iconTint)
    {
        for (int i = 0; i < elementIcons.Length; i++)
        {
            if (elementIcons[i].element != element)
                continue;

            slotBackgroundColor = elementIcons[i].slotBackgroundColor;
            icon = elementIcons[i].icon;
            badgeBackgroundColor = elementIcons[i].badgeBackgroundColor;
            iconTint = elementIcons[i].iconTint == default ? Color.white : elementIcons[i].iconTint;
            return true;
        }

        slotBackgroundColor = Color.white;
        icon = null;
        badgeBackgroundColor = Color.white;
        iconTint = Color.white;
        return false;
    }

    public bool TryGetStatusIcon(CombatUiStatusIconKind kind, out Sprite icon, out Color backgroundColor, out Color iconTint)
    {
        for (int i = 0; i < statusIcons.Length; i++)
        {
            if (statusIcons[i].kind != kind)
                continue;

            icon = statusIcons[i].icon;
            backgroundColor = statusIcons[i].backgroundColor;
            iconTint = statusIcons[i].iconTint == default ? Color.white : statusIcons[i].iconTint;
            return true;
        }

        icon = null;
        backgroundColor = Color.white;
        iconTint = Color.white;
        return false;
    }

    public Sprite GetDiceCostIcon(int slotsRequired)
    {
        switch (Mathf.Clamp(slotsRequired, 1, 3))
        {
            case 1:
                return oneSlotDiceIcon;
            case 2:
                return twoSlotDiceIcon;
            case 3:
                return threeSlotDiceIcon;
            default:
                return null;
        }
    }

    public bool TryGetDiceFaceEnchantIcon(DiceFaceEnchantKind enchant, out Sprite icon, out Color iconTint)
    {
        for (int i = 0; i < diceFaceEnchantIcons.Length; i++)
        {
            if (diceFaceEnchantIcons[i].enchant != enchant)
                continue;

            icon = diceFaceEnchantIcons[i].icon;
            iconTint = diceFaceEnchantIcons[i].iconTint == default ? Color.white : diceFaceEnchantIcons[i].iconTint;
            return true;
        }

        icon = null;
        iconTint = Color.white;
        return false;
    }

    public bool TryGetBrokenFaceIcon(out Sprite icon, out Color iconTint)
    {
        icon = brokenFaceIcon;
        iconTint = brokenFaceIconTint == default ? Color.white : brokenFaceIconTint;
        return icon != null;
    }
}

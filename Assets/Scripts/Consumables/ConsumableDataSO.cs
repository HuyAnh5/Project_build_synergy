using UnityEngine;
using Sirenix.OdinInspector;

public enum ConsumableFamily
{
    Zodiac,
    Seal,
    Rune
}

public enum ConsumableUseContext
{
    OutOfCombat,
    Combat,
    Any
}

public enum ConsumableTargetKind
{
    None,
    Self,
    Dice,
    DiceFace,
    Enemy,
    Ally
}

public enum ConsumableEffectId
{
    None,
    AdjustBaseValue,
    ApplyFaceEnchant,
    FinalVerdictDamage,
    IgniteSpread,
    Cryostasis,
    ExploitMark,
    Exsanguinate,
    RestoreFocus,
    Heal,
    CheatDeath,
    DiceReroll,
    DoubleGold,
    CreateLastUsed,
    Cleanse,
    CopyPasteFace,
    DoubleValue
}

[CreateAssetMenu(menuName = "Game/Consumable/Data", fileName = "Consumable_")]
public class ConsumableDataSO : ScriptableObject
{
    [HorizontalGroup("Top", Width = 70)]
    [HideLabel, PreviewField(70, ObjectFieldAlignment.Left)]
    public Sprite icon;

    [VerticalGroup("Top/Info")]
    [LabelText("Display Name")]
    public string displayName;

    [VerticalGroup("Top/Info")]
    [TextArea(2, 4)]
    [LabelText("Description")]
    public string description;

    [BoxGroup("Classification")]
    [LabelText("Family")]
    [EnumToggleButtons]
    public ConsumableFamily family = ConsumableFamily.Zodiac;

    [BoxGroup("Classification")]
    [LabelText("Use Context")]
    [EnumToggleButtons]
    public ConsumableUseContext useContext = ConsumableUseContext.OutOfCombat;

    [BoxGroup("Classification")]
    [LabelText("Target Kind")]
    [EnumToggleButtons]
    public ConsumableTargetKind targetKind = ConsumableTargetKind.DiceFace;

    [BoxGroup("Effect")]
    [LabelText("Effect")]
    [EnumToggleButtons]
    public ConsumableEffectId effectId = ConsumableEffectId.AdjustBaseValue;

    [BoxGroup("Effect")]
    [MinValue(1)]
    [LabelText("Charges")]
    public int charges = 1;

    [BoxGroup("Effect")]
    [LabelText("Value A")]
    [Tooltip("Example: +1/-1 for AdjustBaseValue, or heal/focus amount for utility effects.")]
    public int valueA = 1;

    [BoxGroup("Effect")]
    [LabelText("Face Enchant")]
    [ShowIf(nameof(UsesFaceEnchant))]
    public DiceFaceEnchantKind faceEnchant = DiceFaceEnchantKind.None;

    [BoxGroup("Runtime Notes")]
    [ReadOnly]
    [ShowInInspector]
    [LabelText("Summary")]
    private string RuntimeSummary => BuildRuntimeSummary();

    public int GetStartingCharges()
    {
        return Mathf.Max(1, charges);
    }

    private bool UsesFaceEnchant()
    {
        return effectId == ConsumableEffectId.ApplyFaceEnchant;
    }

    private string BuildRuntimeSummary()
    {
        switch (effectId)
        {
            case ConsumableEffectId.AdjustBaseValue:
                return $"Each use changes Base Value by {valueA:+#;-#;0} on 1 face.";
            case ConsumableEffectId.ApplyFaceEnchant:
                return $"Each use applies '{DiceFaceEnchantUtility.GetDisplayName(faceEnchant)}' to 1 face.";
            case ConsumableEffectId.FinalVerdictDamage:
                return $"Each use deals {Mathf.Max(0, valueA)} direct damage to one target.";
            case ConsumableEffectId.IgniteSpread:
                return "Each use spreads Burn from one target to other enemies.";
            case ConsumableEffectId.Cryostasis:
                return "Each use grants the Cryostasis reactive shield effect.";
            case ConsumableEffectId.ExploitMark:
                return "Each use converts Marks on enemies into consumables.";
            case ConsumableEffectId.Exsanguinate:
                return "Each use consumes Bleed on one target to heal the user.";
            case ConsumableEffectId.RestoreFocus:
                return $"Each use restores {Mathf.Max(0, valueA)} Focus.";
            case ConsumableEffectId.Heal:
                return $"Each use heals {Mathf.Max(0, valueA)} HP.";
            case ConsumableEffectId.CheatDeath:
                return "Each use grants the Cheat Death safety effect.";
            case ConsumableEffectId.DiceReroll:
                return "Each use rerolls one selected die.";
            case ConsumableEffectId.DoubleGold:
                return "Each use doubles current Gold with a gain cap.";
            case ConsumableEffectId.CreateLastUsed:
                return "Each use recreates the most recently used consumable type.";
            case ConsumableEffectId.Cleanse:
                return "Each use removes all current negative effects.";
            case ConsumableEffectId.CopyPasteFace:
                return "Each use copies Base Value and face enchant from one face to another face.";
            case ConsumableEffectId.DoubleValue:
                return "Each use doubles all face values of the chosen die for this turn.";
            default:
                return "Runtime not configured.";
        }
    }
}

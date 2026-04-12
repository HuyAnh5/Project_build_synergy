public static class DiceFaceEnchantUtility
{
    public const int ValuePlusNAddedValue = 3;
    public const int IceAddedValue = 5;
    public const int GuardBoostAmount = 3;
    public const int GoldProcAmount = 5;
    public const int FireBurnStacks = 2;
    public const int FireBurnTurns = 3;
    public const int BleedStacks = 2;

    public static bool HasEnchant(DiceFaceEnchantKind enchant)
    {
        return enchant != DiceFaceEnchantKind.None;
    }

    public static int GetFlatAddedValue(DiceFaceEnchantKind enchant)
    {
        switch (enchant)
        {
            case DiceFaceEnchantKind.ValuePlusN:
                return ValuePlusNAddedValue;
            case DiceFaceEnchantKind.Ice:
                return IceAddedValue;
            default:
                return 0;
        }
    }

    public static bool IsNumericFace(DiceFaceEnchantKind enchant)
    {
        return enchant != DiceFaceEnchantKind.Ice;
    }

    public static bool CountsAsCritForConditions(DiceFaceEnchantKind enchant)
    {
        return enchant == DiceFaceEnchantKind.Lightning;
    }

    public static bool CountsAsFailForConditions(DiceFaceEnchantKind enchant)
    {
        return enchant == DiceFaceEnchantKind.Lightning;
    }

    public static bool SuppressesCritBonus(DiceFaceEnchantKind enchant)
    {
        return enchant == DiceFaceEnchantKind.Lightning;
    }

    public static bool SuppressesFailPenalty(DiceFaceEnchantKind enchant)
    {
        return enchant == DiceFaceEnchantKind.Lightning;
    }

    public static bool HasOnRollSideEffect(DiceFaceEnchantKind enchant)
    {
        switch (enchant)
        {
            case DiceFaceEnchantKind.GuardBoost:
            case DiceFaceEnchantKind.GoldProc:
            case DiceFaceEnchantKind.Fire:
            case DiceFaceEnchantKind.Bleed:
                return true;
            default:
                return false;
        }
    }

    public static string GetDisplayName(DiceFaceEnchantKind enchant)
    {
        switch (enchant)
        {
            case DiceFaceEnchantKind.ValuePlusN: return "Value +3";
            case DiceFaceEnchantKind.GuardBoost: return "Guard Boost";
            case DiceFaceEnchantKind.GoldProc: return "Gold Proc";
            case DiceFaceEnchantKind.Fire: return "Fire";
            case DiceFaceEnchantKind.Bleed: return "Bleed";
            case DiceFaceEnchantKind.Lightning: return "Lightning";
            case DiceFaceEnchantKind.Ice: return "Ice";
            default: return "None";
        }
    }

    public static string GetRulesText(DiceFaceEnchantKind enchant)
    {
        switch (enchant)
        {
            case DiceFaceEnchantKind.ValuePlusN:
                return "This face gains +3 Added Value.";
            case DiceFaceEnchantKind.GuardBoost:
                return "When this face rolls, gain +3 Guard.";
            case DiceFaceEnchantKind.GoldProc:
                return "When this face rolls, gain +5 Gold.";
            case DiceFaceEnchantKind.Fire:
                return "When this face rolls, apply 2 Burn to 1 random living enemy.";
            case DiceFaceEnchantKind.Bleed:
                return "When this face rolls, apply 2 Bleed to 1 random living enemy.";
            case DiceFaceEnchantKind.Lightning:
                return "Counts as both Crit and Fail for skill/passive conditions, but gets no Crit bonus and no Fail penalty.";
            case DiceFaceEnchantKind.Ice:
                return "Stone-like face: always grants +5 Added Value and is no longer read as a normal number face.";
            default:
                return string.Empty;
        }
    }
}

public static class DiceFaceEnchantUtility
{
    public const int PowerAddedValue = 2;
    public const int StoneAddedValue = 5;
    public const int RelayValueModifier = 2;
    public const int GuardGoldAmount = 5;
    public const int HeavyPaymentContribution = 2;

    public static bool HasEnchant(DiceFaceEnchantKind enchant)
    {
        return enchant != DiceFaceEnchantKind.None;
    }

    public static int GetFlatAddedValue(DiceFaceEnchantKind enchant)
    {
        return 0;
    }

    public static int GetOnUseAddedValue(DiceFaceEnchantKind enchant)
    {
        switch (enchant)
        {
            case DiceFaceEnchantKind.Power:
                return PowerAddedValue;
            case DiceFaceEnchantKind.Stone:
                return StoneAddedValue;
            default:
                return 0;
        }
    }

    public static bool IsNumericFace(DiceFaceEnchantKind enchant)
    {
        return enchant != DiceFaceEnchantKind.Stone;
    }

    public static bool CountsAsCritForConditions(DiceFaceEnchantKind enchant)
    {
        return false;
    }

    public static bool CountsAsFailForConditions(DiceFaceEnchantKind enchant)
    {
        return false;
    }

    public static bool SuppressesCritBonus(DiceFaceEnchantKind enchant)
    {
        return !IsNumericFace(enchant);
    }

    public static bool SuppressesFailPenalty(DiceFaceEnchantKind enchant)
    {
        return !IsNumericFace(enchant);
    }

    public static bool HasOnRollSideEffect(DiceFaceEnchantKind enchant)
    {
        return false;
    }

    public static bool BreaksAfterCommittedUse(DiceFaceEnchantKind enchant)
    {
        switch (enchant)
        {
            case DiceFaceEnchantKind.Double:
            case DiceFaceEnchantKind.Repeat:
            case DiceFaceEnchantKind.Reload:
            case DiceFaceEnchantKind.Heavy:
            case DiceFaceEnchantKind.Echo:
                return true;
            default:
                return false;
        }
    }

    public static bool IsEchoCopyable(DiceFaceEnchantKind enchant)
    {
        switch (enchant)
        {
            case DiceFaceEnchantKind.Power:
            case DiceFaceEnchantKind.Guard:
            case DiceFaceEnchantKind.Charge:
            case DiceFaceEnchantKind.Gold:
            case DiceFaceEnchantKind.Relay:
            case DiceFaceEnchantKind.Double:
            case DiceFaceEnchantKind.Repeat:
            case DiceFaceEnchantKind.Reload:
            case DiceFaceEnchantKind.Heavy:
            case DiceFaceEnchantKind.Stone:
                return true;
            default:
                return false;
        }
    }

    public static string GetDisplayName(DiceFaceEnchantKind enchant)
    {
        switch (enchant)
        {
            case DiceFaceEnchantKind.Power: return "Power";
            case DiceFaceEnchantKind.Guard: return "Guard";
            case DiceFaceEnchantKind.Charge: return "Charge";
            case DiceFaceEnchantKind.Gold: return "Gold";
            case DiceFaceEnchantKind.Gum: return "Gum";
            case DiceFaceEnchantKind.Relay: return "Relay";
            case DiceFaceEnchantKind.Double: return "Double";
            case DiceFaceEnchantKind.Repeat: return "Repeat";
            case DiceFaceEnchantKind.Reload: return "Reload";
            case DiceFaceEnchantKind.Heavy: return "Heavy";
            case DiceFaceEnchantKind.Echo: return "Echo";
            case DiceFaceEnchantKind.Stone: return "Stone";
            default: return "None";
        }
    }

    public static string GetRulesText(DiceFaceEnchantKind enchant)
    {
        switch (enchant)
        {
            case DiceFaceEnchantKind.Power:
                return "On Use: skill/action gains +2 Added Value.";
            case DiceFaceEnchantKind.Guard:
                return "On Use: gain Guard equal to this face's resolved Value.";
            case DiceFaceEnchantKind.Charge:
                return "On Use: gain +1 AP after this action is committed.";
            case DiceFaceEnchantKind.Gold:
                return "On Use: mark this face for bonus Gold after combat victory.";
            case DiceFaceEnchantKind.Gum:
                return "Passive: the opposite logical face is easier to roll.";
            case DiceFaceEnchantKind.Relay:
                return "On Use: the die immediately to the right gains +2 Value this Player Phase.";
            case DiceFaceEnchantKind.Double:
                return "On Use: this face's Value is doubled for this action, then the face becomes Broken.";
            case DiceFaceEnchantKind.Repeat:
                return "Post-Skill: repeat the skill payload once without paying cost again, then Broken.";
            case DiceFaceEnchantKind.Reload:
                return "Post-Skill: reroll this die and return it to available use if the new face is usable, then Broken.";
            case DiceFaceEnchantKind.Heavy:
                return "Pay Cost: this face contributes 2 dice toward dice-slot cost, then Broken.";
            case DiceFaceEnchantKind.Echo:
                return "Copies the valid enchant on the die to the left for this committed use, then Broken.";
            case DiceFaceEnchantKind.Stone:
                return "Static: non-numeric face. On Use: skill/action gains +5 Added Value.";
            default:
                return string.Empty;
        }
    }
}

using UnityEngine;

public enum ConsumableUseFailure
{
    None,
    MissingConsumable,
    UnsupportedContext,
    MissingTarget,
    MissingUser,
    MissingInventory,
    InvalidTarget,
    UnsupportedEffect
}

public readonly struct ConsumableUseResult
{
    public readonly bool success;
    public readonly ConsumableUseFailure failure;
    public readonly string message;

    public ConsumableUseResult(bool success, ConsumableUseFailure failure, string message)
    {
        this.success = success;
        this.failure = failure;
        this.message = message;
    }

    public static ConsumableUseResult Ok(string message)
    {
        return new ConsumableUseResult(true, ConsumableUseFailure.None, message);
    }

    public static ConsumableUseResult Fail(ConsumableUseFailure failure, string message)
    {
        return new ConsumableUseResult(false, failure, message);
    }
}

public static class ConsumableRuntimeUtility
{
    public static bool CanUseInSandbox(ConsumableDataSO data, DiceSpinnerGeneric die, int faceIndex)
    {
        if (data == null)
            return false;
        if (data.useContext == ConsumableUseContext.Combat)
            return false;

        switch (data.targetKind)
        {
            case ConsumableTargetKind.None:
            case ConsumableTargetKind.Self:
                return true;
            case ConsumableTargetKind.Dice:
                return die != null;
            case ConsumableTargetKind.DiceFace:
                return die != null && faceIndex >= 0;
            default:
                return false;
        }
    }

    public static ConsumableUseResult TryUseInSandbox(ConsumableDataSO data, DiceSpinnerGeneric die, int faceIndex)
    {
        if (data == null)
            return ConsumableUseResult.Fail(ConsumableUseFailure.MissingConsumable, "Consumable is empty.");
        if (data.useContext == ConsumableUseContext.Combat)
            return ConsumableUseResult.Fail(ConsumableUseFailure.UnsupportedContext, "This consumable is combat-only.");

        switch (data.effectId)
        {
            case ConsumableEffectId.AdjustBaseValue:
                return TryAdjustBaseValue(data, die, faceIndex);
            case ConsumableEffectId.ApplyFaceEnchant:
                return TryApplyFaceEnchant(data, die, faceIndex);
            case ConsumableEffectId.CopyPasteFace:
                return ConsumableUseResult.Fail(ConsumableUseFailure.UnsupportedEffect, "Copy / Paste Face requires source and target faces.");
            default:
                return ConsumableUseResult.Fail(ConsumableUseFailure.UnsupportedEffect, "This effect is not implemented in sandbox runtime.");
        }
    }

    public static ConsumableUseResult TryCopyPasteFace(DiceSpinnerGeneric sourceDie, int sourceFaceIndex, DiceSpinnerGeneric targetDie, int targetFaceIndex)
    {
        if (sourceDie == null || sourceFaceIndex < 0)
            return ConsumableUseResult.Fail(ConsumableUseFailure.MissingTarget, "Select a source face first.");
        if (targetDie == null || targetFaceIndex < 0)
            return ConsumableUseResult.Fail(ConsumableUseFailure.MissingTarget, "Select a target face next.");

        DiceFace sourceFace = sourceDie.GetFace(sourceFaceIndex);
        if (!targetDie.SetFaceValue(targetFaceIndex, sourceFace.value))
            return ConsumableUseResult.Fail(ConsumableUseFailure.InvalidTarget, "Could not paste the copied value to the target face.");
        if (!targetDie.SetFaceEnchant(targetFaceIndex, sourceFace.enchant))
            return ConsumableUseResult.Fail(ConsumableUseFailure.InvalidTarget, "Could not paste the copied enchant to the target face.");

        return ConsumableUseResult.Ok(
            $"Copied face {sourceFaceIndex} from {sourceDie.name} to face {targetFaceIndex} on {targetDie.name}.");
    }

    public static bool CanUseInCombat(ConsumableDataSO data, CombatActor user, CombatActor target, RunInventoryManager inventory, DiceSpinnerGeneric targetDie = null)
    {
        if (data == null)
            return false;
        if (data.useContext == ConsumableUseContext.OutOfCombat)
            return false;

        switch (data.effectId)
        {
            case ConsumableEffectId.Cryostasis:
                return user != null;
            case ConsumableEffectId.Exsanguinate:
                return user != null && target != null && target.status != null && target.status.bleedStacks > 0;
            case ConsumableEffectId.Heal:
            case ConsumableEffectId.RestoreFocus:
            case ConsumableEffectId.Cleanse:
                return user != null;

            case ConsumableEffectId.FinalVerdictDamage:
                return user != null && target != null && target != user;

            case ConsumableEffectId.DoubleGold:
                return inventory != null;
            case ConsumableEffectId.DoubleValue:
                return targetDie != null;

            default:
                return false;
        }
    }

    public static ConsumableUseResult TryUseInCombat(ConsumableDataSO data, CombatActor user, CombatActor target, RunInventoryManager inventory, DiceSpinnerGeneric targetDie = null)
    {
        if (data == null)
            return ConsumableUseResult.Fail(ConsumableUseFailure.MissingConsumable, "Consumable is empty.");
        if (data.useContext == ConsumableUseContext.OutOfCombat)
            return ConsumableUseResult.Fail(ConsumableUseFailure.UnsupportedContext, "This consumable is out-of-combat only.");

        switch (data.effectId)
        {
            case ConsumableEffectId.Cryostasis:
                return TryCryostasis(user);
            case ConsumableEffectId.Exsanguinate:
                return TryExsanguinate(user, target);
            case ConsumableEffectId.Heal:
                return TryHeal(data, user);
            case ConsumableEffectId.RestoreFocus:
                return TryRestoreFocus(data, user);
            case ConsumableEffectId.Cleanse:
                return TryCleanse(user);
            case ConsumableEffectId.FinalVerdictDamage:
                return TryFinalVerdict(data, user, target);
            case ConsumableEffectId.DoubleGold:
                return TryDoubleGold(data, inventory);
            case ConsumableEffectId.DoubleValue:
                return TryDoubleValue(targetDie);
            default:
                return ConsumableUseResult.Fail(ConsumableUseFailure.UnsupportedEffect, "This combat consumable is not implemented yet.");
        }
    }

    private static ConsumableUseResult TryAdjustBaseValue(ConsumableDataSO data, DiceSpinnerGeneric die, int faceIndex)
    {
        if (die == null || faceIndex < 0)
            return ConsumableUseResult.Fail(ConsumableUseFailure.MissingTarget, "Select a dice face first.");

        DiceFace face = die.GetFace(faceIndex);
        int nextValue = DiceSpinnerGeneric.ClampFaceValue(face.value + data.valueA);
        if (!die.SetFaceValue(faceIndex, nextValue))
            return ConsumableUseResult.Fail(ConsumableUseFailure.MissingTarget, "Could not change the dice face value.");

        return ConsumableUseResult.Ok($"{die.name} face {faceIndex} changed to {nextValue}.");
    }

    private static ConsumableUseResult TryApplyFaceEnchant(ConsumableDataSO data, DiceSpinnerGeneric die, int faceIndex)
    {
        if (die == null || faceIndex < 0)
            return ConsumableUseResult.Fail(ConsumableUseFailure.MissingTarget, "Select a dice face first.");

        if (!die.SetFaceEnchant(faceIndex, data.faceEnchant))
            return ConsumableUseResult.Fail(ConsumableUseFailure.MissingTarget, "Could not apply enchant to the dice face.");

        return ConsumableUseResult.Ok($"{die.name} face {faceIndex} gained {DiceFaceEnchantUtility.GetDisplayName(data.faceEnchant)}.");
    }

    private static ConsumableUseResult TryHeal(ConsumableDataSO data, CombatActor user)
    {
        if (user == null)
            return ConsumableUseResult.Fail(ConsumableUseFailure.MissingUser, "Missing user actor.");

        int healed = user.Heal(Mathf.Max(0, data.valueA));
        return ConsumableUseResult.Ok($"Healed {healed} HP.");
    }

    private static ConsumableUseResult TryRestoreFocus(ConsumableDataSO data, CombatActor user)
    {
        if (user == null)
            return ConsumableUseResult.Fail(ConsumableUseFailure.MissingUser, "Missing user actor.");

        int before = user.focus;
        user.GainFocus(Mathf.Max(0, data.valueA));
        int gained = Mathf.Max(0, user.focus - before);
        return ConsumableUseResult.Ok($"Restored {gained} Focus.");
    }

    private static ConsumableUseResult TryCleanse(CombatActor user)
    {
        if (user == null)
            return ConsumableUseResult.Fail(ConsumableUseFailure.MissingUser, "Missing user actor.");

        if (user.status != null)
            user.status.ClearAll();

        return ConsumableUseResult.Ok("Cleared current status state.");
    }

    private static ConsumableUseResult TryCryostasis(CombatActor user)
    {
        if (user == null)
            return ConsumableUseResult.Fail(ConsumableUseFailure.MissingUser, "Missing user actor.");

        user.AddGuard(999);
        return ConsumableUseResult.Ok("Cryostasis armed as a temporary prototype shield.");
    }

    private static ConsumableUseResult TryFinalVerdict(ConsumableDataSO data, CombatActor user, CombatActor target)
    {
        if (user == null)
            return ConsumableUseResult.Fail(ConsumableUseFailure.MissingUser, "Missing user actor.");
        if (target == null)
            return ConsumableUseResult.Fail(ConsumableUseFailure.MissingTarget, "Select a target first.");
        if (target == user)
            return ConsumableUseResult.Fail(ConsumableUseFailure.InvalidTarget, "Target must be an enemy or another actor.");

        CombatActor.DamageResult damage = target.TakeDamageDetailed(Mathf.Max(0, data.valueA), bypassGuard: false);
        return ConsumableUseResult.Ok($"Final Verdict dealt {damage.blocked + damage.hpLost} total damage.");
    }

    private static ConsumableUseResult TryExsanguinate(CombatActor user, CombatActor target)
    {
        if (user == null)
            return ConsumableUseResult.Fail(ConsumableUseFailure.MissingUser, "Missing user actor.");
        if (target == null)
            return ConsumableUseResult.Fail(ConsumableUseFailure.MissingTarget, "Select a target first.");
        if (target.status == null || target.status.bleedStacks <= 0)
            return ConsumableUseResult.Fail(ConsumableUseFailure.InvalidTarget, "Target has no Bleed to consume.");

        int consumed = Mathf.Max(0, target.status.bleedStacks);
        target.status.bleedStacks = 0;
        int healed = user.Heal(consumed);
        return ConsumableUseResult.Ok($"Consumed {consumed} Bleed and healed {healed} HP.");
    }

    private static ConsumableUseResult TryDoubleGold(ConsumableDataSO data, RunInventoryManager inventory)
    {
        if (inventory == null)
            return ConsumableUseResult.Fail(ConsumableUseFailure.MissingInventory, "Missing inventory.");

        int currentGold = Mathf.Max(0, inventory.Gold);
        int bonus = Mathf.Min(currentGold, Mathf.Max(0, data.valueA));
        inventory.AddGold(bonus);
        return ConsumableUseResult.Ok($"Gained {bonus} Gold.");
    }

    private static ConsumableUseResult TryDoubleValue(DiceSpinnerGeneric targetDie)
    {
        if (targetDie == null)
            return ConsumableUseResult.Fail(ConsumableUseFailure.MissingTarget, "Select a die first.");

        targetDie.EnableDoubleValueForTurn();
        targetDie.RefreshDisplayedState();
        return ConsumableUseResult.Ok($"{targetDie.name} face values are doubled for this turn.");
    }
}

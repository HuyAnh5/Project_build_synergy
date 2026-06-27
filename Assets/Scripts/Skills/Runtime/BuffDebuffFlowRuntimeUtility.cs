using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BuffDebuffFlowRuntimeUtility
{
    private const float DiceFlowRollTimeoutSeconds = 2.5f;

    public static bool CheckRequirements(
        SkillBuffDebuffSO skill,
        SkillRuntime runtime,
        CombatActor caster,
        CombatActor target,
        out string failureReason)
    {
        failureReason = null;
        if (skill == null || skill.gameplay == null || skill.gameplay.requirements == null)
            return true;

        SkillConditionContext context = SkillGameplayResolver.BuildConditionContext(runtime, caster, target);
        for (int i = 0; i < skill.gameplay.requirements.Count; i++)
        {
            SkillRequirementData requirement = skill.gameplay.requirements[i];
            if (requirement == null || requirement.condition == null)
                continue;

            if (requirement.condition.Evaluate(context))
                continue;

            failureReason = string.IsNullOrWhiteSpace(requirement.failureText)
                ? "Requirement not met."
                : requirement.failureText;
            return false;
        }

        return true;
    }

    public static List<BuffDebuffFlowEffectData> ResolveEffects(
        SkillBuffDebuffSO skill,
        SkillRuntime runtime,
        CombatActor caster,
        CombatActor target)
    {
        List<BuffDebuffFlowEffectData> resolved = new List<BuffDebuffFlowEffectData>();
        if (skill == null || skill.gameplay == null)
            return resolved;

        AddEffects(skill.gameplay.baseEffects, resolved);

        if (skill.gameplay.conditionalOutcomes == null || skill.gameplay.conditionalOutcomes.Count == 0)
            return resolved;

        SkillConditionContext context = SkillGameplayResolver.BuildConditionContext(runtime, caster, target);
        for (int i = 0; i < skill.gameplay.conditionalOutcomes.Count; i++)
        {
            BuffDebuffFlowConditionalOutcomeData branch = skill.gameplay.conditionalOutcomes[i];
            if (branch == null || branch.condition == null || !branch.condition.Evaluate(context))
                continue;

            AddEffects(branch.effects, resolved);
        }

        return resolved;
    }

    public static void ApplyActorEffects(
        SkillBuffDebuffSO skill,
        SkillRuntime runtime,
        CombatActor caster,
        CombatActor target)
    {
        List<BuffDebuffFlowEffectData> effects = ResolveEffects(skill, runtime, caster, target);
        for (int i = 0; i < effects.Count; i++)
            BuffDebuffFlowActorEffectApplier.Apply(effects[i], caster, target);
    }

    public static bool HasPostCastDiceEffects(SkillBuffDebuffSO skill, SkillRuntime runtime, CombatActor caster, CombatActor target)
    {
        List<BuffDebuffFlowEffectData> effects = ResolveEffects(skill, runtime, caster, target);
        for (int i = 0; i < effects.Count; i++)
        {
            if (IsPostCastDiceEffect(effects[i]))
                return true;
        }

        return false;
    }

    public static bool ShouldSkipCastAnimation(SkillBuffDebuffSO skill, SkillRuntime runtime, CombatActor caster, CombatActor target)
        => HasPostCastDiceEffects(skill, runtime, caster, target);

    public static IEnumerator ApplyPostCastDiceEffects(
        SkillBuffDebuffSO skill,
        SkillRuntime runtime,
        CombatActor caster,
        CombatActor target,
        DiceSlotRig diceRig,
        int paymentMask,
        TurnManager turnManager)
    {
        if (diceRig == null || paymentMask <= 0)
            yield break;

        List<BuffDebuffFlowEffectData> effects = ResolveEffects(skill, runtime, caster, target);
        for (int i = 0; i < effects.Count; i++)
        {
            BuffDebuffFlowEffectData effect = effects[i];
            if (!IsPostCastDiceEffect(effect))
                continue;

            yield return ApplyDiceFlowEffect(effect, diceRig, paymentMask, turnManager);
        }
    }

    private static void AddEffects(List<BuffDebuffFlowEffectData> source, List<BuffDebuffFlowEffectData> destination)
    {
        if (source == null || destination == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            if (source[i] != null)
                destination.Add(source[i]);
        }
    }

    private static bool IsPostCastDiceEffect(BuffDebuffFlowEffectData effect)
    {
        if (effect == null)
            return false;

        return effect.type == BuffDebuffFlowEffectType.RerollUsedDice ||
               effect.type == BuffDebuffFlowEffectType.TransformUsedDiceHigh ||
               effect.type == BuffDebuffFlowEffectType.TransformUsedDiceLow;
    }

    private static IEnumerator ApplyDiceFlowEffect(
        BuffDebuffFlowEffectData effect,
        DiceSlotRig diceRig,
        int paymentMask,
        TurnManager turnManager)
    {
        List<DiceSpinnerGeneric> rolling = new List<DiceSpinnerGeneric>(3);
        for (int slot0 = 0; slot0 < 3; slot0++)
        {
            if ((paymentMask & (1 << slot0)) == 0)
                continue;

            DiceSpinnerGeneric die = diceRig.GetDice(slot0);
            if (die == null || die.LastFaceIndex < 0)
                continue;

            switch (effect.type)
            {
                case BuffDebuffFlowEffectType.RerollUsedDice:
                    StartReload(die, turnManager, rolling);
                    break;

                case BuffDebuffFlowEffectType.TransformUsedDiceHigh:
                    StartTransform(die, turnManager, rolling, high: true);
                    break;

                case BuffDebuffFlowEffectType.TransformUsedDiceLow:
                    StartTransform(die, turnManager, rolling, high: false);
                    break;
            }
        }

        if (rolling.Count <= 0)
            yield break;

        float elapsed = 0f;
        while (elapsed < DiceFlowRollTimeoutSeconds)
        {
            bool anyRolling = false;
            for (int i = 0; i < rolling.Count; i++)
            {
                if (rolling[i] != null && rolling[i].IsRolling)
                {
                    anyRolling = true;
                    break;
                }
            }

            if (!anyRolling)
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        diceRig.RefreshRollInfoCache();
        if (turnManager != null)
            turnManager.RefreshPlanningAfterDiceValueReorder(rolling);
    }

    private static void StartReload(DiceSpinnerGeneric die, TurnManager turnManager, List<DiceSpinnerGeneric> rolling)
    {
        RestoreDie(turnManager, die);
        rolling.Add(die);
        die.RollRandomFace();
    }

    private static void StartTransform(DiceSpinnerGeneric die, TurnManager turnManager, List<DiceSpinnerGeneric> rolling, bool high)
    {
        int currentValue = die.GetDisplayedRolledValue();
        int targetFace = FindTransformFaceIndex(die, currentValue, high);
        if (targetFace < 0)
            return;

        RestoreDie(turnManager, die);
        rolling.Add(die);
        die.RollToFaceIndex(targetFace);
    }

    private static int FindTransformFaceIndex(DiceSpinnerGeneric die, int currentValue, bool high)
    {
        if (die == null || die.faces == null || die.faces.Length <= 0)
            return -1;

        List<int> candidates = new List<int>(die.faces.Length);
        for (int i = 0; i < die.faces.Length; i++)
        {
            if (die.IsFaceBroken(i) && i != die.LastFaceIndex)
                continue;
            if (!DiceFaceEnchantUtility.IsNumericFace(die.GetFaceEnchant(i)))
                continue;

            int faceValue = die.GetFace(i).value;
            if (high && faceValue >= currentValue)
                candidates.Add(i);
            else if (!high && faceValue <= currentValue)
                candidates.Add(i);
        }

        if (candidates.Count <= 0)
            return die.LastFaceIndex;

        return candidates[Random.Range(0, candidates.Count)];
    }

    private static void RestoreDie(TurnManager turnManager, DiceSpinnerGeneric die)
    {
        if (turnManager != null)
            turnManager.RestoreDieToAvailableThisTurn(die);
    }
}

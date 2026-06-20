using System.Collections.Generic;
using UnityEngine;

// Contains legacy attack side effects that are still authored directly on SkillRuntime.
// New data-driven effects should live in SkillAttackResolutionUtility.ResolvedGameplay.cs.
internal static partial class SkillAttackResolutionUtility
{
    /// <summary>Applies legacy status flags and conditional outcomes after direct damage resolves.</summary>
    public static void ApplyStatusesAfterHit(SkillRuntime rt, CombatActor caster, CombatActor target, int dieValue, int finalDamage, bool targetHadBurnBeforeHit)
    {
        if (target == null || target.status == null || rt == null)
            return;

        PassiveSystem ps = caster != null ? caster.GetComponent<PassiveSystem>() : null;

        if (rt.applyBurn)
        {
            int burnStacks = rt.burnAddStacks;
            if (SkillBehaviorRuntimeUtility.IsBehavior(rt, FireDamageBehaviorId.FireSlash))
            {
                burnStacks = rt.conditionMet ? Mathf.Max(0, rt.burnAddStacks) : 0;
            }
            else
            {
                burnStacks = SkillOutputValueUtility.ResolveStatusStacks(
                    rt.burnAddStacks,
                    rt,
                    rt.baseBurnValueMode,
                    dieValue,
                    rt.element == ElementType.Fire && rt.fireApplyBurnFromResolvedValue);

                if (rt.fireGrantBonusBurnOnOddBase &&
                    SkillBehaviorRuntimeUtility.TryGetSingleBaseValue(rt, out int fireBaseValue) &&
                    (fireBaseValue % 2) != 0)
                {
                    burnStacks += Mathf.Max(0, rt.fireOddBaseBonusBurn);
                }
            }

            burnStacks += ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Burn) : 0;
            if (burnStacks > 0)
                target.status.ApplyBurn(burnStacks, rt.burnRefreshTurns);
        }

        if (IsBasicStrike(rt) && caster != null && caster.status != null && caster.status.emberWeaponTurns > 0)
        {
            bool canApplyEmberBurn = caster.status.emberWeaponBurnEqualsDamage;
            if (canApplyEmberBurn && caster.status.emberWeaponBurnOnCritOnly)
                canApplyEmberBurn = rt.localCritAny;

            if (canApplyEmberBurn)
            {
                int emberBurn = Mathf.Max(0, finalDamage);
                emberBurn += ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Burn) : 0;
                if (emberBurn > 0)
                    target.status.ApplyBurn(emberBurn, Mathf.Max(1, caster.status.emberWeaponBurnTurns));
            }
        }

        if (rt.element == ElementType.Fire && rt.fireReapplyBurnPerExactBase)
        {
            bool canReapply = !rt.fireRequireBurnBeforeHitForReapply || targetHadBurnBeforeHit;
            if (canReapply)
            {
                int matchCount = SkillBehaviorRuntimeUtility.CountBaseValuesEqual(rt, rt.fireExactBaseForReapply);
                if (matchCount > 0)
                {
                    int reapplyBurn = (Mathf.Max(0, rt.fireBurnPerExactMatch) * matchCount) +
                                      (ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Burn) : 0);
                    target.status.ApplyBurn(reapplyBurn, Mathf.Max(rt.burnRefreshTurns, 3));
                }
            }
        }
        else if (SkillBehaviorRuntimeUtility.IsBehavior(rt, FireDamageBehaviorId.Hellfire) && targetHadBurnBeforeHit)
        {
            int sevensRolled = SkillBehaviorRuntimeUtility.CountBaseValuesEqual(rt, 7);
            if (sevensRolled > 0)
            {
                int reapplyBurn = (7 * sevensRolled) + (ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Burn) : 0);
                target.status.ApplyBurn(reapplyBurn, Mathf.Max(rt.burnRefreshTurns, 3));
            }
        }

        if (rt.applyMark) target.status.ApplyMark();
        if (rt.applyBleed)
        {
            int bleedStacks = SkillOutputValueUtility.ResolveFlatStatusStacks(rt.bleedTurns);
            bleedStacks += ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Bleed) : 0;
            if (bleedStacks > 0)
                target.status.ApplyBleed(bleedStacks);
        }
        if (rt.applyFreeze) target.status.TryApplyFreeze(rt.freezeChance);

        if (rt.conditionMet && rt.conditionalOutcomeEnabled && rt.conditionalOutcomeType == ConditionalOutcomeType.ApplyBurn)
        {
            int conditionalBurn = GetConditionalBurnStacks(rt, dieValue);
            conditionalBurn += ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Burn) : 0;
            if (conditionalBurn > 0)
                target.status.ApplyBurn(conditionalBurn, Mathf.Max(1, rt.conditionalOutcomeBurnTurns));
        }

        if (rt.conditionMet && rt.conditionalOutcomeEnabled && rt.conditionalOutcomeType == ConditionalOutcomeType.GainGuard && caster != null)
        {
            int conditionalGuard = GetConditionalGuardValue(rt, dieValue);
            if (conditionalGuard > 0)
            {
                caster.AddGuard(conditionalGuard);
                CombatHitFeedback.Play(caster, CombatHitFeedback.FeedbackKind.Guard);
            }
        }
    }

    private static void ApplyBehaviorAfterHit(
        SkillRuntime rt,
        CombatActor caster,
        CombatActor target,
        int dieValue,
        int finalDamage,
        bool targetHadMarkBeforeHit,
        int targetBurnStacksBeforeHit,
        SkillExecutor.AttackPreview preview,
        CombatActor.DamageResult dmgResult)
    {
        if (rt == null || caster == null)
            return;

        PassiveSystem ps = caster.GetComponent<PassiveSystem>();

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, PhysicalDamageBehaviorId.Execution))
        {
            if (target != null && target.IsDead)
            {
                int overkill = Mathf.Max(0, preview.finalDamage - dmgResult.blocked - dmgResult.hpLost);
                SkillCombatState state = caster.GetComponent<SkillCombatState>();
                if (state != null && overkill > 0)
                    state.QueueExecutionCarry(overkill);
            }
            return;
        }

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, FireDamageBehaviorId.Cauterize))
        {
            if (target != null && target.status != null)
            {
                int burnIndex = SkillBehaviorRuntimeUtility.GetLowestBaseValueIndex(rt);
                int burn = SkillBehaviorRuntimeUtility.GetPerDieResolvedOutput(rt, burnIndex);
                burn += ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Burn) : 0;
                if (burn > 0)
                    target.status.ApplyBurn(burn, 3);
            }

            int guardIndex = SkillBehaviorRuntimeUtility.GetHighestBaseValueIndex(rt);
            int guard = SkillBehaviorRuntimeUtility.GetPerDieResolvedOutput(rt, guardIndex);
            if (guard > 0)
                caster.AddGuard(guard);
            return;
        }

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, FireDamageBehaviorId.BiteTheDust))
        {
            int heal = Mathf.FloorToInt(Mathf.Max(0, targetBurnStacksBeforeHit) / 5f) * 3;
            if (heal > 0)
            {
                caster.Heal(heal);
                CombatHitFeedback.Play(caster, CombatHitFeedback.FeedbackKind.Heal);
            }
            return;
        }

        if (rt.splitRoleEnabled)
        {
            ApplySplitRole(rt, caster, target, ps);
            return;
        }

        if (rt.element == ElementType.Fire && rt.fireApplyBurnFromLowestBase)
        {
            if (target != null && target.status != null)
            {
                int burnIndex = SkillBehaviorRuntimeUtility.GetLowestBaseValueIndex(rt);
                int burn = SkillBehaviorRuntimeUtility.GetPerDieResolvedOutput(rt, burnIndex);
                burn += ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Burn) : 0;
                if (burn > 0)
                    target.status.ApplyBurn(burn, 3);
            }

            if (rt.fireGainGuardFromHighestBase)
            {
                int guardIndex = SkillBehaviorRuntimeUtility.GetHighestBaseValueIndex(rt);
                int guard = SkillBehaviorRuntimeUtility.GetPerDieResolvedOutput(rt, guardIndex);
                if (guard > 0)
                {
                    caster.AddGuard(guard);
                    CombatHitFeedback.Play(caster, CombatHitFeedback.FeedbackKind.Guard);
                }
            }
            return;
        }

        if (rt.element == ElementType.Fire &&
            rt.fireApplyConsumeBonusDebuff &&
            (!SkillBehaviorRuntimeUtility.IsBehavior(rt, FireDamageBehaviorId.Ignite) || rt.conditionMet))
        {
            if (target != null && target.status != null)
            {
                target.status.cinderbrandTurns = Mathf.Max(target.status.cinderbrandTurns, Mathf.Max(1, rt.fireConsumeBonusDebuffTurns));
                target.status.cinderbrandBonusPerBurn = Mathf.Max(target.status.cinderbrandBonusPerBurn, Mathf.Max(0, rt.fireConsumeBonusPerBurn));
            }
            return;
        }

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, IceDamageBehaviorId.WintersBite))
        {
            if (target != null && target.status != null && target.status.chilledTurns > 0)
                target.status.chilledTurns += 1;
            return;
        }

    }

    private static void ConsumeExecutionCarryIfNeeded(SkillRuntime rt, CombatActor caster)
    {
        if (rt == null || caster == null)
            return;

        if (rt.group != DamageGroup.Strike && rt.group != DamageGroup.Sunder)
            return;

        SkillCombatState state = caster.GetComponent<SkillCombatState>();
        if (state == null || state.ExecutionCarryActive <= 0)
            return;

        state.ConsumeExecutionCarry();
    }

    private static void ApplyBounceIfNeeded(SkillRuntime rt, CombatActor caster, CombatActor originalTarget, int dieValue, int finalDamage, DamagePopupSystem popups)
    {
        if (!SkillBehaviorRuntimeUtility.IsBehavior(rt, LightningDamageBehaviorId.SparkBarrage))
            return;

        if (!SkillBehaviorRuntimeUtility.TryGetSingleBaseValue(rt, out int baseValue) || (baseValue % 2) != 0)
            return;

        List<CombatActor> others = SkillBehaviorRuntimeUtility.GetOtherEnemies(caster, originalTarget);
        if (others.Count <= 0)
            return;

        CombatActor bounceTarget = others[0];
        if (bounceTarget == null)
            return;

        CombatActor.DamageResult bounce = bounceTarget.TakeDamageDetailed(finalDamage, bypassGuard: false);
        if (popups != null)
            popups.SpawnDamageSplit(caster, bounceTarget, bounce.blocked, bounce.hpLost);
    }

    private static bool IsBasicStrike(SkillRuntime rt)
    {
        if (rt == null)
            return false;
        return rt.coreAction == CoreAction.BasicStrike;
    }

    private static int GetConditionalBurnStacks(SkillRuntime rt, int dieValue)
    {
        if (rt == null || !rt.conditionalOutcomeEnabled || rt.conditionalOutcomeType != ConditionalOutcomeType.ApplyBurn)
            return 0;

        return SkillOutputValueUtility.ResolveConditionalStatusStacks(
            rt.conditionalOutcomeFlatValue,
            rt,
            rt.conditionalOutcomeValueMode,
            dieValue);
    }

    private static int GetConditionalGuardValue(SkillRuntime rt, int dieValue)
    {
        if (rt == null || !rt.conditionalOutcomeEnabled || rt.conditionalOutcomeType != ConditionalOutcomeType.GainGuard)
            return 0;

        switch (rt.conditionalOutcomeValueMode)
        {
            case ConditionalOutcomeValueMode.X:
                return SkillOutputValueUtility.ResolveXValue(dieValue, rt);

            case ConditionalOutcomeValueMode.Flat:
            default:
                return SkillOutputValueUtility.AddActionAddedValue(rt.conditionalOutcomeFlatValue, rt);
        }
    }

    private static void ApplySplitRole(SkillRuntime rt, CombatActor caster, CombatActor target, PassiveSystem ps)
    {
        if (rt == null || caster == null)
            return;

        int lowestIndex = SkillBehaviorRuntimeUtility.GetLowestBaseValueIndex(rt);
        int highestIndex = SkillBehaviorRuntimeUtility.GetHighestBaseValueIndex(rt);

        ApplySplitRoleBranch(rt, caster, target, ps, lowestIndex, rt.splitRoleLowestOutcome);
        if (highestIndex != lowestIndex || rt.splitRoleHighestOutcome != rt.splitRoleLowestOutcome)
            ApplySplitRoleBranch(rt, caster, target, ps, highestIndex, rt.splitRoleHighestOutcome);
    }

    private static void ApplySplitRoleBranch(SkillRuntime rt, CombatActor caster, CombatActor target, PassiveSystem ps, int dieIndex, SplitRoleBranchOutcome outcome)
    {
        if (rt == null || dieIndex < 0 || outcome == SplitRoleBranchOutcome.None)
            return;

        int value = SkillBehaviorRuntimeUtility.GetPerDieResolvedOutput(rt, dieIndex);
        if (value <= 0)
            return;

        switch (outcome)
        {
            case SplitRoleBranchOutcome.Burn:
                if (target != null && target.status != null)
                {
                    int burn = value + (ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Burn) : 0);
                    if (burn > 0)
                        target.status.ApplyBurn(burn, Mathf.Max(1, rt.splitRoleBurnTurns));
                }
                break;

            case SplitRoleBranchOutcome.Guard:
                caster.AddGuard(value);
                CombatHitFeedback.Play(caster, CombatHitFeedback.FeedbackKind.Guard);
                break;
        }
    }
}

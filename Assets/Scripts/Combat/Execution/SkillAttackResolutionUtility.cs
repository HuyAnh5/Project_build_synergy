using System.Collections.Generic;
using UnityEngine;

internal static class SkillAttackResolutionUtility
{
    public static int ApplyAttackToTargets(
        SkillRuntime rt,
        CombatActor caster,
        IReadOnlyList<CombatActor> targets,
        int dieValue,
        DamagePopupSystem popups,
        MonoBehaviour context,
        System.Func<SkillRuntime, CombatActor, CombatActor, int, SkillExecutor.AttackPreview> buildAttackPreview,
        out int lightningShockDamage)
    {
        int totalShockProcCount = 0;
        lightningShockDamage = 0;

        if (targets == null)
            return 0;

        for (int i = 0; i < targets.Count; i++)
        {
            CombatActor t = targets[i];
            if (t == null || t.IsDead)
                continue;

            SkillExecutor.AttackApplyResult result = ApplyAttack(rt, caster, t, dieValue, popups, context, buildAttackPreview);
            totalShockProcCount += result.lightningShockProcCount;
            if (result.lightningShockDamage > 0)
                lightningShockDamage = result.lightningShockDamage;
        }

        return totalShockProcCount;
    }

    public static SkillExecutor.AttackApplyResult ApplyAttack(
        SkillRuntime rt,
        CombatActor caster,
        CombatActor target,
        int dieValue,
        DamagePopupSystem popups,
        MonoBehaviour context,
        System.Func<SkillRuntime, CombatActor, CombatActor, int, SkillExecutor.AttackPreview> buildAttackPreview)
    {
        if (rt == null || target == null || buildAttackPreview == null)
            return default;

        PassiveSystem ps = caster != null ? caster.GetComponent<PassiveSystem>() : null;
        bool hadGuardBeforeHit = target.guardPool > 0;
        bool targetHadBurnBeforeHit = target.status != null && target.status.burnStacks > 0;
        bool targetHadMarkBeforeHit = target.status != null && target.status.marked;
        bool targetWasChilledBeforeHit = target.status != null && target.status.chilledTurns > 0;
        SkillExecutor.AttackPreview preview = buildAttackPreview(rt, caster, target, dieValue);

        var info = new DamageInfo
        {
            group = rt.group,
            element = rt.element,
            bypassGuard = rt.bypassGuard,
            clearsGuard = rt.clearsGuard,
            canUseMarkMultiplier = rt.canUseMarkMultiplier,
            isDamage = preview.finalDamage > 0
        };

        bool triggerMarkPayoff = AttackPreviewCalculator.CanUseMarkPayoff(rt, target);
        int lightningShockDamage = 0;
        if (triggerMarkPayoff && rt.element == ElementType.Lightning)
        {
            float shockMul = 1f;
            if (ps != null)
                shockMul += Mathf.Max(0f, ps.GetLightningVsMarkMultiplierAdd());
            lightningShockDamage = Mathf.FloorToInt(4 * shockMul);
        }

        if (rt.element == ElementType.Fire && rt.consumesBurn && target.status != null && target.status.burnStacks > 0)
        {
            target.status.ConsumeAllBurn();
        }

        if (Debug.isDebugBuild)
        {
            bool hasPS = ps != null;
            Debug.Log($"[EXEC] ApplyAttack rt={rt.kind}/{rt.group}/{rt.element} die={dieValue} base={preview.baseDamage} bonus={preview.bonusDamage} finalDmg={preview.finalDamage} hadGuard={hadGuardBeforeHit} hasPassiveSystem={hasPS}", context);
        }

        CombatActor.DamageResult dmgResult = target.TakeDamageDetailed(preview.finalDamage, bypassGuard: info.bypassGuard);

        if (info.isDamage && rt.element == ElementType.Ice && target.status != null && caster != null)
        {
            bool isFrozen = target.status.frozen;
            bool isChilled = target.status.chilledTurns > 0;
            if (isFrozen || isChilled)
            {
                caster.AddGuard(3);
                caster.GainFocus(1);
            }
        }

        if (info.clearsGuard)
            target.guardPool = 0;

        if (target.status != null && caster != null)
        {
            int reward = target.status.OnHitByDamageReturnFocusReward(ref info);
            if (reward > 0 && ps != null)
                reward += ps.GetFreezeBreakFocusBonusAdd();
            if (reward != 0)
                caster.GainFocus(reward);
        }

        if (triggerMarkPayoff && target.status != null)
            target.status.marked = false;

        if (preview.consumesStagger && target.status != null)
            target.status.ClearStagger();

        if (dmgResult.guardBroken && target.status != null)
            target.status.ApplyStagger();

        ApplyStatusesAfterHit(rt, caster, target, dieValue, preview.finalDamage, targetHadBurnBeforeHit);

        ApplyBehaviorAfterHit(rt, caster, target, dieValue, preview.finalDamage, targetHadMarkBeforeHit, targetWasChilledBeforeHit, preview, dmgResult);
        ApplyBounceIfNeeded(rt, caster, target, dieValue, preview.finalDamage, popups);

        ConsumeExecutionCarryIfNeeded(rt, caster);

        if (popups != null)
            popups.SpawnDamageSplit(caster, target, dmgResult.blocked, dmgResult.hpLost);

        return new SkillExecutor.AttackApplyResult
        {
            damageResult = dmgResult,
            lightningShockProcCount = lightningShockDamage > 0 ? 1 : 0,
            lightningShockDamage = lightningShockDamage,
            consumedStagger = preview.consumesStagger
        };
    }

    public static void ApplyStatusesAfterHit(SkillRuntime rt, CombatActor caster, CombatActor target, int dieValue, int finalDamage, bool targetHadBurnBeforeHit)
    {
        if (target == null || target.status == null || rt == null)
            return;

        PassiveSystem ps = caster != null ? caster.GetComponent<PassiveSystem>() : null;

        if (rt.applyBurn)
        {
            int burnStacks = rt.burnAddStacks;
            if (rt.baseBurnValueMode == BaseEffectValueMode.X || (rt.element == ElementType.Fire && rt.fireApplyBurnFromResolvedValue))
            {
                burnStacks = SkillOutputValueUtility.ResolveXValue(dieValue, rt);
                if (rt.fireGrantBonusBurnOnOddBase &&
                    SkillBehaviorRuntimeUtility.TryGetSingleBaseValue(rt, out int fireBaseValue) &&
                    (fireBaseValue % 2) != 0)
                {
                    burnStacks += Mathf.Max(0, rt.fireOddBaseBonusBurn);
                }
            }
            else if (SkillBehaviorRuntimeUtility.IsBehavior(rt, FireDamageBehaviorId.Ignite))
            {
                burnStacks = SkillOutputValueUtility.ResolveXValue(dieValue, rt);
                if (SkillBehaviorRuntimeUtility.TryGetSingleBaseValue(rt, out int baseValue) && (baseValue % 2) != 0)
                    burnStacks += 2;
            }
            else
            {
                burnStacks = SkillOutputValueUtility.AddActionAddedValue(burnStacks, rt);
            }

            burnStacks += ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Burn) : 0;
            target.status.ApplyBurn(burnStacks, rt.burnRefreshTurns);
        }

        if (IsBasicStrike(rt) && caster != null && caster.status != null && caster.status.emberWeaponTurns > 0)
        {
            if (caster.status.emberWeaponBurnEqualsDamage)
            {
                int emberBurn = Mathf.Max(0, finalDamage);
                emberBurn += ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Burn) : 0;
                if (emberBurn > 0)
                    target.status.ApplyBurn(emberBurn, 3);
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
            int bleedStacks = SkillOutputValueUtility.AddActionAddedValue(rt.bleedTurns, rt);
            bleedStacks += ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Bleed) : 0;
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
                caster.AddGuard(conditionalGuard);
        }
    }

    private static void ApplyBehaviorAfterHit(
        SkillRuntime rt,
        CombatActor caster,
        CombatActor target,
        int dieValue,
        int finalDamage,
        bool targetHadMarkBeforeHit,
        bool targetWasChilledBeforeHit,
        SkillExecutor.AttackPreview preview,
        CombatActor.DamageResult dmgResult)
    {
        if (rt == null || caster == null)
            return;

        PassiveSystem ps = caster.GetComponent<PassiveSystem>();

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, PhysicalDamageBehaviorId.BrutalSmash))
        {
            if (targetHadMarkBeforeHit)
                caster.GainFocus(1);
            return;
        }

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

        if (rt.splitRoleEnabled)
        {
            ApplySplitRole(rt, caster, target, ps);
            return;
        }

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, IceDamageBehaviorId.Shatter))
        {
            if (targetWasChilledBeforeHit)
            {
                int addGuard = Mathf.Min(20, Mathf.FloorToInt(caster.guardPool * 0.5f));
                if (addGuard > 0)
                    caster.AddGuard(addGuard);
            }
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
                    caster.AddGuard(guard);
            }
            return;
        }

        if (rt.element == ElementType.Fire && rt.fireApplyConsumeBonusDebuff)
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

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, IceDamageBehaviorId.ColdSnap))
        {
            int guard = SkillBehaviorRuntimeUtility.GetHighestBaseValue(rt);
            guard = SkillOutputValueUtility.AddActionAddedValue(guard, rt);
            if (guard > 0)
                caster.AddGuard(guard);
            return;
        }

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, LightningDamageBehaviorId.StaticConduit))
        {
            if (targetHadMarkBeforeHit)
            {
                List<CombatActor> others = SkillBehaviorRuntimeUtility.GetOtherEnemies(caster, target);
                for (int i = 0; i < others.Count; i++)
                {
                    if (others[i] != null && others[i].status != null)
                        others[i].status.ApplyMark();
                }
            }
            return;
        }

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, BleedDamageBehaviorId.Lacerate))
        {
            if (target != null && target.status != null)
            {
                int bleed = SkillOutputValueUtility.ResolveXValue(dieValue, rt);
                if (rt.localCritAny)
                    bleed += SkillOutputValueUtility.ResolveXValue(dieValue, rt);
                bleed += ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Bleed) : 0;
                if (bleed > 0)
                    target.status.ApplyBleed(bleed);
            }
            return;
        }

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, BleedDamageBehaviorId.Hemorrhage))
        {
            SkillCombatState state = caster.GetComponent<SkillCombatState>();
            if (target != null && target.status != null && state != null)
            {
                int bleed = SkillOutputValueUtility.AddActionAddedValue(Mathf.Max(0, state.LastEnemyTurnHpLost), rt);
                bleed += ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Bleed) : 0;
                if (bleed > 0)
                    target.status.ApplyBleed(bleed);
            }
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

        switch (rt.conditionalOutcomeValueMode)
        {
            case ConditionalOutcomeValueMode.X:
                return SkillOutputValueUtility.ResolveXValue(dieValue, rt);

            case ConditionalOutcomeValueMode.Flat:
            default:
                return SkillOutputValueUtility.AddActionAddedValue(rt.conditionalOutcomeFlatValue, rt);
        }
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
                break;
        }
    }
}

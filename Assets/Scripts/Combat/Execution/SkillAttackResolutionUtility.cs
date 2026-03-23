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
            target.status.burnStacks = 0;
            target.status.burnTurns = 0;
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
            if (SkillBehaviorRuntimeUtility.IsBehavior(rt, FireDamageBehaviorId.Ignite))
            {
                burnStacks = Mathf.Max(0, dieValue);
                if (SkillBehaviorRuntimeUtility.TryGetSingleBaseValue(rt, out int baseValue) && (baseValue % 2) != 0)
                    burnStacks += 2;
            }

            burnStacks += ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Burn) : 0;
            target.status.ApplyBurn(burnStacks, rt.burnRefreshTurns);
        }

        if (IsBasicStrike(rt) && caster != null && caster.status != null && caster.status.emberWeaponTurns > 0)
        {
            int emberBurn = Mathf.Max(0, finalDamage);
            emberBurn += ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Burn) : 0;
            if (emberBurn > 0)
                target.status.ApplyBurn(emberBurn, 3);
        }

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, FireDamageBehaviorId.Hellfire) && targetHadBurnBeforeHit)
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
            int bleedStacks = rt.bleedTurns + (ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Bleed) : 0);
            target.status.ApplyBleed(bleedStacks);
        }
        if (rt.applyFreeze) target.status.TryApplyFreeze(rt.freezeChance);
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
                int burn = SkillBehaviorRuntimeUtility.GetLowestBaseValue(rt);
                burn += ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Burn) : 0;
                if (burn > 0)
                    target.status.ApplyBurn(burn, 3);
            }

            int guard = SkillBehaviorRuntimeUtility.GetHighestBaseValue(rt);
            if (guard > 0)
                caster.AddGuard(guard);
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

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, IceDamageBehaviorId.WintersBite))
        {
            if (target != null && target.status != null && target.status.chilledTurns > 0)
                target.status.chilledTurns += 1;
            return;
        }

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, IceDamageBehaviorId.ColdSnap))
        {
            int guard = SkillBehaviorRuntimeUtility.GetHighestBaseValue(rt);
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
                int bleed = Mathf.Max(0, dieValue);
                if (rt.localCritAny)
                    bleed += Mathf.Max(0, dieValue);
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
                int bleed = Mathf.Max(0, state.LastEnemyTurnHpLost);
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
}

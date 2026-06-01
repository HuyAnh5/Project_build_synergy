using System.Collections.Generic;
using UnityEngine;

// Entry points for applying skill attack damage. Detailed pipelines live in partial files
// so legacy combat rules and new data-driven gameplay rules stay easier to audit.
internal static partial class SkillAttackResolutionUtility
{
    /// <summary>Applies one attack runtime to every valid target and aggregates combat side effects.</summary>
    public static SkillExecutor.AttackApplyResult ApplyAttackToTargets(
        SkillRuntime rt,
        CombatActor caster,
        IReadOnlyList<CombatActor> targets,
        int dieValue,
        DamagePopupSystem popups,
        MonoBehaviour context,
        System.Func<SkillRuntime, CombatActor, CombatActor, int, SkillExecutor.AttackPreview> buildAttackPreview)
    {
        SkillExecutor.AttackApplyResult aggregate = default;

        if (targets == null)
            return default;

        SkillDamageSO sourceSkill = SkillGameplayResolver.GetSourceSkill(rt);
        if (SkillGameplayResolver.CanResolveWithNewPipeline(sourceSkill))
        {
            CombatActor primaryTarget = null;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null && !targets[i].IsDead)
                {
                    primaryTarget = targets[i];
                    break;
                }
            }

            if (primaryTarget == null)
                return default;

            return ApplyAttack(rt, caster, primaryTarget, dieValue, popups, context, buildAttackPreview);
        }

        for (int i = 0; i < targets.Count; i++)
        {
            CombatActor t = targets[i];
            if (t == null || t.IsDead)
                continue;

            SkillExecutor.AttackApplyResult result = ApplyAttack(rt, caster, t, dieValue, popups, context, buildAttackPreview);
            aggregate.damageResult.blocked += result.damageResult.blocked;
            aggregate.damageResult.hpLost += result.damageResult.hpLost;
            aggregate.damageResult.guardBroken |= result.damageResult.guardBroken;
            aggregate.lightningShockProcCount += result.lightningShockProcCount;
            aggregate.lightningShockDamage = Mathf.Max(aggregate.lightningShockDamage, result.lightningShockDamage);
            aggregate.consumedStagger |= result.consumedStagger;
            aggregate.hadPrimaryDamageStep |= result.hadPrimaryDamageStep;
            aggregate.delayedBurnConsumeDamage += result.delayedBurnConsumeDamage;
            if (result.delayedFollowUpEffects != null && result.delayedFollowUpEffects.Count > 0)
            {
                if (aggregate.delayedFollowUpEffects == null)
                    aggregate.delayedFollowUpEffects = new List<ResolvedEffect>();
                aggregate.delayedFollowUpEffects.AddRange(result.delayedFollowUpEffects);
            }
        }

        return aggregate;
    }

    /// <summary>Applies a single attack to one target, routing to the new gameplay pipeline when enabled.</summary>
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

        SkillDamageSO sourceSkill = SkillGameplayResolver.GetSourceSkill(rt);
        if (SkillGameplayResolver.CanResolveWithNewPipeline(sourceSkill))
            return ApplyResolvedGameplayAttack(rt, caster, target, popups, context);

        PassiveSystem ps = caster != null ? caster.GetComponent<PassiveSystem>() : null;
        bool hadGuardBeforeHit = target.guardPool > 0;
        bool targetHadBurnBeforeHit = target.status != null && target.status.burnStacks > 0;
        int targetBurnStacksBeforeHit = target.status != null ? Mathf.Max(0, target.status.burnStacks) : 0;
        bool targetHadMarkBeforeHit = target.status != null && target.status.marked;
        SkillExecutor.AttackPreview preview = buildAttackPreview(rt, caster, target, dieValue);
        int delayedBurnConsumeDamage = 0;
        bool splitBurnConsumeStep = rt.element == ElementType.Fire &&
                                    rt.consumesBurn &&
                                    preview.primaryDamage > 0 &&
                                    preview.burnConsumeDamage > 0;

        var info = new DamageInfo
        {
            group = rt.group,
            element = rt.element,
            bypassGuard = rt.bypassGuard,
            clearsGuard = rt.clearsGuard,
            canUseMarkMultiplier = rt.canUseMarkMultiplier,
            isDamage = (splitBurnConsumeStep ? preview.primaryDamage : preview.finalDamage) > 0
        };

        bool triggerMarkPayoff = AttackPreviewCalculator.CanUseMarkPayoff(rt, target);
        int lightningShockDamage = 0;
        if (triggerMarkPayoff && rt.element == ElementType.Lightning && rt.triggerLightningMarkShock)
        {
            float shockMul = 1f;
            if (ps != null)
                shockMul += Mathf.Max(0f, ps.GetLightningVsMarkMultiplierAdd());
            int baseShockDamage = Mathf.Max(0, rt.lightningMarkShockDamage);
            lightningShockDamage = Mathf.FloorToInt(baseShockDamage * shockMul);
        }

        if (rt.element == ElementType.Fire && rt.consumesBurn && target.status != null && target.status.burnStacks > 0)
        {
            target.status.ConsumeAllBurn();
            if (splitBurnConsumeStep)
                delayedBurnConsumeDamage = preview.burnConsumeDamage;
        }

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, PhysicalDamageBehaviorId.FatedSunder) && rt.conditionMet)
            target.guardPool = 0;

        if (Debug.isDebugBuild)
        {
            bool hasPS = ps != null;
            Debug.Log($"[EXEC] ApplyAttack rt={rt.kind}/{rt.group}/{rt.element} die={dieValue} base={preview.baseDamage} bonus={preview.bonusDamage} finalDmg={preview.finalDamage} hadGuard={hadGuardBeforeHit} hasPassiveSystem={hasPS}", context);
        }

        int immediateDamage = splitBurnConsumeStep ? preview.primaryDamage : preview.finalDamage;
        CombatActor.DamageResult dmgResult = target.TakeDamageDetailed(immediateDamage, bypassGuard: info.bypassGuard);
        if (immediateDamage > 0)
            CombatHitFeedback.Play(target, CombatHitFeedback.FeedbackKind.Hit);
        if (delayedBurnConsumeDamage > 0)
            CombatHitFeedback.Play(target, CombatHitFeedback.FeedbackKind.BurnConsume);

        if (info.isDamage &&
            rt.element == ElementType.Ice &&
            rt.gainIceRewardOnFrozenOrChilledHit &&
            target.status != null &&
            caster != null)
        {
            bool isFrozen = target.status.frozen;
            bool isChilled = target.status.chilledTurns > 0;
            if (isFrozen || isChilled)
            {
                int guardReward = Mathf.Max(0, rt.iceRewardGuard);
                if (guardReward > 0)
                {
                    caster.AddGuard(guardReward);
                    CombatHitFeedback.Play(caster, CombatHitFeedback.FeedbackKind.Guard);
                }

                int focusReward = Mathf.Max(0, rt.iceRewardFocus);
                if (focusReward > 0)
                {
                    if (ps != null)
                        focusReward += ps.GetFreezeBreakFocusBonusAdd();
                    caster.GainFocus(focusReward);
                    CombatHitFeedback.Play(caster, CombatHitFeedback.FeedbackKind.Focus);
                }
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
            {
                caster.GainFocus(reward);
                CombatHitFeedback.Play(caster, CombatHitFeedback.FeedbackKind.Focus);
            }
        }

        if (triggerMarkPayoff && target.status != null)
        {
            CombatHitFeedback.Play(target, CombatHitFeedback.FeedbackKind.MarkPayoff);
            target.status.marked = false;
        }

        if (preview.consumesStagger && target.status != null)
            target.status.ClearStagger();

        if (dmgResult.guardBroken && target.status != null)
            target.status.ApplyStagger();

        ApplyStatusesAfterHit(rt, caster, target, dieValue, preview.finalDamage, targetHadBurnBeforeHit);

        ApplyBehaviorAfterHit(rt, caster, target, dieValue, preview.finalDamage, targetHadMarkBeforeHit, targetBurnStacksBeforeHit, preview, dmgResult);
        ApplyBounceIfNeeded(rt, caster, target, dieValue, preview.finalDamage, popups);

        ConsumeExecutionCarryIfNeeded(rt, caster);

        if (popups != null)
            popups.SpawnDamageSplit(caster, target, dmgResult.blocked, dmgResult.hpLost);

        return new SkillExecutor.AttackApplyResult
        {
            damageResult = dmgResult,
            lightningShockProcCount = lightningShockDamage > 0 ? 1 : 0,
            lightningShockDamage = lightningShockDamage,
            consumedStagger = preview.consumesStagger,
            hadPrimaryDamageStep = preview.primaryDamage > 0,
            delayedBurnConsumeDamage = delayedBurnConsumeDamage
        };
    }
}

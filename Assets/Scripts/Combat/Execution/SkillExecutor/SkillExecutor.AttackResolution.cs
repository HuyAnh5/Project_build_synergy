using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class SkillExecutor
{
    internal struct AttackApplyResult
    {
        public CombatActor.DamageResult damageResult;
        public int lightningShockProcCount;
        public int lightningShockDamage;
        public bool consumedStagger;
        public bool hadPrimaryDamageStep;
        public int delayedBurnConsumeDamage;
        public List<ResolvedEffect> delayedFollowUpEffects;
        public List<ResolvedEffect> delayedPassiveMeleeFollowUpEffects;

        public bool HasDelayedFollowUpEffects =>
            delayedFollowUpEffects != null && delayedFollowUpEffects.Count > 0;

        public bool HasDelayedPassiveMeleeFollowUpEffects =>
            delayedPassiveMeleeFollowUpEffects != null && delayedPassiveMeleeFollowUpEffects.Count > 0;
    }

    [Serializable]
    public struct AttackPreview
    {
        public int effectiveDieValue;
        public int baseDamage;
        public int bonusDamage;
        public int finalDamage;
        public int primaryDamage;
        public int burnConsumeDamage;
        public bool canDealDamage;
        public bool consumesStagger;
    }

    public int PreviewDamage(SkillRuntime rt, CombatActor caster, CombatActor target, int dieValue)
    {
        return BuildAttackPreview(rt, caster, target, dieValue).finalDamage;
    }

    public int PreviewDamage(SkillDamageSO skill, CombatActor caster, CombatActor target, int dieValue)
    {
        if (skill == null)
        {
            return 0;
        }

        return PreviewDamage(SkillRuntime.FromDamage(skill), caster, target, dieValue);
    }

    public AttackPreview BuildAttackPreview(SkillRuntime rt, CombatActor caster, CombatActor target, int dieValue)
    {
        return AttackPreviewCalculator.BuildAttackPreview(rt, caster, target, dieValue);
    }

    private AttackApplyResult ApplyAttackToTargets(SkillRuntime rt, CombatActor caster, IReadOnlyList<CombatActor> targets, int dieValue)
    {
        return SkillAttackResolutionUtility.ApplyAttackToTargets(rt, caster, targets, dieValue, GetPopups(), this, BuildAttackPreview);
    }

    private AttackApplyResult ApplyAttack(SkillRuntime rt, CombatActor caster, CombatActor target, int dieValue)
    {
        return SkillAttackResolutionUtility.ApplyAttack(rt, caster, target, dieValue, GetPopups(), this, BuildAttackPreview);
    }

    private IEnumerator ResolveDelayedAttackFollowUpsAtImpact(
        SkillRuntime rt,
        CombatActor caster,
        CombatActor primaryTarget,
        AttackApplyResult result,
        Action onComplete)
    {
        yield return ResolveDelayedAttackFollowUps(rt, caster, primaryTarget, result);
        onComplete?.Invoke();
    }

    private IEnumerator ResolveDelayedAttackFollowUps(SkillRuntime rt, CombatActor caster, CombatActor primaryTarget, AttackApplyResult result)
    {
        if (result.hadPrimaryDamageStep && result.HasDelayedFollowUpEffects && GlobalDelayedSecondaryStep > 0f)
        {
            yield return new WaitForSeconds(GlobalDelayedSecondaryStep);
        }

        if (ShouldStopEnemyActionForRevive(caster))
            yield break;

        if (result.HasDelayedFollowUpEffects)
        {
            SkillAttackResolutionUtility.ApplyResolvedGameplayFollowUpEffects(rt, caster, primaryTarget, result.delayedFollowUpEffects, GetPopups());
        }

        if (result.hadPrimaryDamageStep && result.HasDelayedPassiveMeleeFollowUpEffects && GlobalDelayedSecondaryStep > 0f)
        {
            yield return new WaitForSeconds(GlobalDelayedSecondaryStep);
        }

        if (ShouldStopEnemyActionForRevive(caster))
            yield break;

        if (result.HasDelayedPassiveMeleeFollowUpEffects)
        {
            SkillAttackResolutionUtility.ApplyPassiveMeleeFollowUpEffects(rt, caster, primaryTarget, result.delayedPassiveMeleeFollowUpEffects, GetPopups());
        }

        if (result.hadPrimaryDamageStep && result.delayedBurnConsumeDamage > 0 && GlobalDelayedSecondaryStep > 0f)
        {
            yield return new WaitForSeconds(GlobalDelayedSecondaryStep);
        }

        if (ShouldStopEnemyActionForRevive(caster))
            yield break;

        if (result.delayedBurnConsumeDamage > 0)
        {
            ApplyDelayedBurnConsumeDamage(caster, primaryTarget, result.delayedBurnConsumeDamage);
        }

        if (result.lightningShockProcCount > 0 && result.lightningShockDamage > 0)
        {
            if (result.hadPrimaryDamageStep && GlobalDelayedSecondaryStep > 0f)
            {
                yield return new WaitForSeconds(GlobalDelayedSecondaryStep);
            }

            if (ShouldStopEnemyActionForRevive(caster))
                yield break;

            yield return ApplyLightningMarkShockSequence(caster, result.lightningShockDamage, result.lightningShockProcCount);
        }
    }

    private IEnumerator ApplyLightningMarkShockSequence(CombatActor caster, int damage, int procCount)
    {
        if (caster == null || damage <= 0 || procCount <= 0)
        {
            yield break;
        }

        for (int i = 0; i < procCount; i++)
        {
            if (ShouldStopEnemyActionForRevive(caster))
                yield break;

            ApplyLightningMarkShock(caster, damage);
            if (i < procCount - 1 && GlobalDelayedSecondaryStep > 0f)
            {
                yield return new WaitForSeconds(GlobalDelayedSecondaryStep);
            }
        }
    }

    private static bool ShouldStopEnemyActionForRevive(CombatActor caster)
    {
        if (caster == null || caster.team != CombatActor.TeamSide.Enemy)
            return false;

        PassiveSystem playerPassiveSystem = PassiveSystemRegistry.GetPlayer();
        return playerPassiveSystem != null && playerPassiveSystem.IsEnemyTurnEndRequestedByRevive;
    }

    private void ApplyLightningMarkShock(CombatActor caster, int damage)
    {
        if (caster == null || damage <= 0)
        {
            return;
        }

        DamagePopupSystem popups = GetPopups();
        BattlePartyManager2D party = GetParty();
        if (party == null)
        {
            return;
        }

        IReadOnlyList<CombatActor> targets = caster.team == CombatActor.TeamSide.Enemy
            ? party.GetAliveAllies(includePlayer: true)
            : party.Enemies;

        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            CombatActor target = targets[i];
            if (target == null || target.IsDead || target == caster || target.team == caster.team)
            {
                continue;
            }

            CombatActor.DamageResult result = target.TakeDamageDetailed(damage, bypassGuard: false, attacker: caster);
            CombatHitFeedback.Play(target, CombatHitFeedback.FeedbackKind.Hit);
            if (popups != null)
            {
                popups.SpawnDamageSplit(caster, target, result.blocked, result.hpLost);
            }
        }
    }

    private void ApplyDelayedBurnConsumeDamage(CombatActor caster, CombatActor target, int damage)
    {
        if (caster == null || target == null || target.IsDead || damage <= 0)
        {
            return;
        }

        DamagePopupSystem popups = GetPopups();
        CombatActor.DamageResult result = target.TakeDamageDetailed(damage, bypassGuard: false, attacker: caster);
        CombatHitFeedback.Play(target, CombatHitFeedback.FeedbackKind.BurnConsume);
        if (popups != null)
        {
            popups.SpawnDamageSplit(caster, target, result.blocked, result.hpLost);
        }
    }

    private int ApplyCustomGuardBehavior(SkillRuntime rt, CombatActor caster, int baseGuard)
    {
        if (rt == null || caster == null)
        {
            return baseGuard;
        }

        return baseGuard;
    }
}

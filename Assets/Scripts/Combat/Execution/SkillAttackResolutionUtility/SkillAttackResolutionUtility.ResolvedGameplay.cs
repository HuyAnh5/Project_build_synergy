using System.Collections.Generic;
using UnityEngine;

// Resolves the new SkillGameplayData pipeline into damage, status, guard, heal and follow-up effects.
internal static partial class SkillAttackResolutionUtility
{
    /// <summary>Applies a data-driven attack result and returns aggregate damage/shock/follow-up data.</summary>
    private static SkillExecutor.AttackApplyResult ApplyResolvedGameplayAttack(
        SkillRuntime rt,
        CombatActor caster,
        CombatActor target,
        DamagePopupSystem popups,
        MonoBehaviour context)
    {
        SkillResolvedResult resolved = SkillGameplayResolver.Resolve(rt, caster, target);
        if (resolved == null || !resolved.canCast || target == null)
            return default;

        PassiveSystem passiveSystem = caster != null ? caster.GetComponent<PassiveSystem>() : null;
        var info = new DamageInfo
        {
            group = rt.group,
            element = rt.element,
            bypassGuard = rt.bypassGuard,
            clearsGuard = rt.clearsGuard,
            canUseMarkMultiplier = rt.canUseMarkMultiplier,
            isDamage = false
        };

        if (Debug.isDebugBuild)
            Debug.Log($"[EXEC] ApplyResolvedGameplayAttack rt={rt.kind}/{rt.group}/{rt.element} effects={resolved.effects.Count}", context);

        CombatActor.DamageResult aggregateDamageResult = default;
        int totalDamage = 0;
        bool consumedAnyStagger = false;
        int totalShockProcCount = 0;
        int shockDamagePerProc = 0;
        var markPayoffAppliedTargets = new HashSet<CombatActor>();
        var iceRewardAppliedTargets = new HashSet<CombatActor>();
        var primaryDamageAppliedTargets = new HashSet<CombatActor>();
        List<ResolvedEffect> delayedFollowUpEffects = null;

        for (int i = 0; i < resolved.effects.Count; i++)
        {
            ResolvedEffect effect = resolved.effects[i];
            if (effect == null)
                continue;
            if (effect.sameActionFollowUp)
            {
                if (delayedFollowUpEffects == null)
                    delayedFollowUpEffects = new List<ResolvedEffect>();
                delayedFollowUpEffects.Add(effect);
                continue;
            }
            if (effect.type != SkillEffectType.DealDamage && effect.type != SkillEffectType.DealSecondaryDamage)
                continue;

            CombatActor effectTarget = effect.targetActor != null ? effect.targetActor : target;
            if (effectTarget == null || effectTarget.IsDead)
                continue;

            bool isPrimaryDamage = effect.type == SkillEffectType.DealDamage;
            bool isFirstPrimaryDamageForTarget = isPrimaryDamage && primaryDamageAppliedTargets.Add(effectTarget);
            bool targetHadMarkBeforeHit = isFirstPrimaryDamageForTarget && AttackPreviewCalculator.CanUseMarkPayoff(rt, effectTarget);
            bool targetWasFrozenOrChilled = isFirstPrimaryDamageForTarget &&
                                            effectTarget.status != null &&
                                            (effectTarget.status.frozen || effectTarget.status.chilledTurns > 0);

            bool applyMarkPayoffNow = targetHadMarkBeforeHit && markPayoffAppliedTargets.Add(effectTarget);

            int damage = Mathf.Max(0, effect.value);
            if (applyMarkPayoffNow && rt.element != ElementType.Lightning)
                damage += 3;

            if (damage <= 0)
                continue;

            bool consumesStagger = isFirstPrimaryDamageForTarget && AttackPreviewCalculator.CanConsumeStagger(rt, effectTarget);
            if (consumesStagger)
            {
                damage = Mathf.FloorToInt(damage * 1.2f);
                if (damage < 1)
                    damage = 1;
            }

            info.isDamage = true;
            totalDamage += damage;
            CombatActor.DamageResult damageResult = effectTarget.TakeDamageDetailed(damage, bypassGuard: info.bypassGuard);
            PlayFeedback(effectTarget, CombatHitFeedback.FeedbackKind.Hit);
            aggregateDamageResult.blocked += damageResult.blocked;
            aggregateDamageResult.hpLost += damageResult.hpLost;
            aggregateDamageResult.guardBroken |= damageResult.guardBroken;

            if (isFirstPrimaryDamageForTarget)
                ApplyNewPipelineEmberWeaponBurn(rt, caster, effectTarget, damage);

            if (info.clearsGuard)
                effectTarget.guardPool = 0;

            if (isFirstPrimaryDamageForTarget && effectTarget.status != null && caster != null)
            {
                int reward = effectTarget.status.OnHitByDamageReturnFocusReward(ref info);
                if (reward != 0)
                {
                    caster.GainFocus(reward);
                        PlayFeedback(caster, CombatHitFeedback.FeedbackKind.Focus);
                }
            }

            if (isPrimaryDamage && damageResult.guardBroken && effectTarget.status != null)
                effectTarget.status.ApplyStagger();
            else if (isFirstPrimaryDamageForTarget && consumesStagger && effectTarget.status != null)
            {
                effectTarget.status.ClearStagger();
                consumedAnyStagger = true;
            }

            if (isFirstPrimaryDamageForTarget &&
                rt.element == ElementType.Ice &&
                rt.gainIceRewardOnFrozenOrChilledHit &&
                caster != null &&
                targetWasFrozenOrChilled &&
                iceRewardAppliedTargets.Add(effectTarget))
            {
                int guardReward = Mathf.Max(0, rt.iceRewardGuard);
                if (guardReward > 0)
                {
                    caster.AddGuard(guardReward);
                    PlayFeedback(caster, CombatHitFeedback.FeedbackKind.Guard);
                }

                int focusReward = Mathf.Max(0, rt.iceRewardFocus);
                if (focusReward > 0)
                {
                    focusReward += passiveSystem != null ? passiveSystem.GetFreezeBreakFocusBonusAdd() : 0;
                    caster.GainFocus(focusReward);
                    PlayFeedback(caster, CombatHitFeedback.FeedbackKind.Focus);
                }
            }

            if (isFirstPrimaryDamageForTarget && applyMarkPayoffNow && effectTarget.status != null)
            {
                if (rt.element == ElementType.Lightning && rt.triggerLightningMarkShock)
                {
                    float shockMul = 1f;
                    if (passiveSystem != null)
                        shockMul += Mathf.Max(0f, passiveSystem.GetLightningVsMarkMultiplierAdd());
                    int baseShockDamage = Mathf.Max(0, rt.lightningMarkShockDamage);
                    shockDamagePerProc = Mathf.Max(shockDamagePerProc, Mathf.FloorToInt(baseShockDamage * shockMul));
                    totalShockProcCount++;
                }

                PlayFeedback(effectTarget, CombatHitFeedback.FeedbackKind.MarkPayoff);
                effectTarget.status.marked = false;
            }

            if (popups != null)
                popups.SpawnDamageSplit(caster, effectTarget, damageResult.blocked, damageResult.hpLost);
        }

        ApplyResolvedGameplayEffects(resolved.effects, caster, target, includeFollowUpEffects: false);

        return new SkillExecutor.AttackApplyResult
        {
            damageResult = aggregateDamageResult,
            lightningShockProcCount = totalShockProcCount,
            lightningShockDamage = shockDamagePerProc,
            consumedStagger = consumedAnyStagger,
            hadPrimaryDamageStep = totalDamage > 0,
            delayedBurnConsumeDamage = 0,
            delayedFollowUpEffects = delayedFollowUpEffects
        };
    }

    /// <summary>Applies non-attack gameplay effects such as guard, heal, focus and status.</summary>
    public static void ApplyResolvedGameplayNonAttack(SkillRuntime rt, CombatActor caster, CombatActor selectedTarget)
    {
        SkillResolvedResult resolved = SkillGameplayResolver.Resolve(rt, caster, selectedTarget);
        if (resolved == null || !resolved.canCast)
            return;

        ApplyResolvedGameplayEffects(resolved.effects, caster, selectedTarget, includeFollowUpEffects: false);
    }

    private static void ApplyNewPipelineEmberWeaponBurn(SkillRuntime rt, CombatActor caster, CombatActor target, int finalDamage)
    {
        if (!IsBasicStrike(rt) || caster == null || caster.status == null || target == null || target.status == null)
            return;
        if (caster.status.emberWeaponTurns <= 0 || !caster.status.emberWeaponBurnEqualsDamage)
            return;
        if (caster.status.emberWeaponBurnOnCritOnly && !rt.localCritAny)
            return;

        int emberBurn = Mathf.Max(0, finalDamage);
        PassiveSystem passiveSystem = caster.GetComponent<PassiveSystem>();
        emberBurn += passiveSystem != null ? passiveSystem.GetBonusStatusStacksApplied(StatusKind.Burn) : 0;
        if (emberBurn > 0)
            target.status.ApplyBurn(emberBurn, Mathf.Max(1, caster.status.emberWeaponBurnTurns));
    }

    private static void ApplyResolvedGameplayEffects(IReadOnlyList<ResolvedEffect> effects, CombatActor caster, CombatActor selectedTarget, bool includeFollowUpEffects)
    {
        if (effects == null)
            return;

        PassiveSystem passiveSystem = caster != null ? caster.GetComponent<PassiveSystem>() : null;
        for (int i = 0; i < effects.Count; i++)
        {
            ResolvedEffect effect = effects[i];
            if (effect == null || effect.type == SkillEffectType.DealDamage || effect.type == SkillEffectType.DealSecondaryDamage)
                continue;
            if (effect.sameActionFollowUp != includeFollowUpEffects)
                continue;

            CombatActor target = effect.targetActor != null
                ? effect.targetActor
                : ResolveEffectTarget(effect.target, caster, selectedTarget);
            if (target == null)
                continue;

            int value = Mathf.Max(0, effect.value);
            switch (effect.type)
            {
                case SkillEffectType.ApplyStatus:
                    ApplyResolvedStatus(effect.status, target, value, passiveSystem);
                    break;
                case SkillEffectType.GainGuard:
                    target.AddGuard(value);
                    if (value > 0)
                        PlayFeedback(target, CombatHitFeedback.FeedbackKind.Guard);
                    break;
                case SkillEffectType.Heal:
                    if (target.Heal(value) > 0)
                        PlayFeedback(target, CombatHitFeedback.FeedbackKind.Heal);
                    break;
                case SkillEffectType.GainAP:
                    target.GainFocus(value);
                    if (value > 0)
                        PlayFeedback(target, CombatHitFeedback.FeedbackKind.Focus);
                    break;
                case SkillEffectType.ConsumeStatus:
                    ConsumeResolvedStatus(effect.status, target, value);
                    break;
                case SkillEffectType.ClearGuard:
                    ClearResolvedGuard(target);
                    break;
            }
        }
    }

    /// <summary>Resolves same-action delayed follow-up effects after the primary hit animation step.</summary>
    public static void ApplyResolvedGameplayFollowUpEffects(
        SkillRuntime rt,
        CombatActor caster,
        CombatActor selectedTarget,
        IReadOnlyList<ResolvedEffect> effects,
        DamagePopupSystem popups)
    {
        if (effects == null || rt == null)
            return;

        for (int i = 0; i < effects.Count; i++)
        {
            ResolvedEffect effect = effects[i];
            if (effect == null || !effect.sameActionFollowUp)
                continue;

            CombatActor effectTarget = effect.targetActor != null
                ? effect.targetActor
                : ResolveEffectTarget(effect.target, caster, selectedTarget);
            if (effectTarget == null || effectTarget.IsDead)
                continue;

            if (effect.type == SkillEffectType.DealDamage || effect.type == SkillEffectType.DealSecondaryDamage)
            {
                int damage = Mathf.Max(0, effect.value);
                if (damage <= 0)
                    continue;

                bool consumesStagger = AttackPreviewCalculator.CanConsumeStagger(rt, effectTarget);
                if (consumesStagger)
                {
                    damage = Mathf.FloorToInt(damage * 1.2f);
                    if (damage < 1)
                        damage = 1;
                }

                CombatActor.DamageResult damageResult = effectTarget.TakeDamageDetailed(damage, bypassGuard: rt.bypassGuard);
                PlayFeedback(effectTarget, CombatHitFeedback.FeedbackKind.Hit);
                if (damageResult.guardBroken && effectTarget.status != null)
                    effectTarget.status.ApplyStagger();
                else if (consumesStagger && effectTarget.status != null)
                    effectTarget.status.ClearStagger();
                if (popups != null)
                    popups.SpawnDamageSplit(caster, effectTarget, damageResult.blocked, damageResult.hpLost);
            }
        }

        ApplyResolvedGameplayEffects(effects, caster, selectedTarget, includeFollowUpEffects: true);
    }

    private static void PlayFeedback(CombatActor actor, CombatHitFeedback.FeedbackKind kind)
    {
        if (CombatSimulationContext.SuppressPresentation)
            return;

        CombatHitFeedback.Play(actor, kind);
    }

    private static bool ClearResolvedGuard(CombatActor target)
    {
        if (target == null)
            return false;

        int guardBefore = Mathf.Max(0, target.guardPool);
        if (guardBefore <= 0)
            return false;

        target.guardPool = 0;
        if (target.status != null)
            target.status.ApplyStagger();

        return true;
    }

    private static CombatActor ResolveEffectTarget(SkillEffectTarget target, CombatActor caster, CombatActor selectedTarget)
    {
        switch (target)
        {
            case SkillEffectTarget.Self:
                return caster;
            case SkillEffectTarget.SelectedEnemy:
            case SkillEffectTarget.RowEnemies:
            case SkillEffectTarget.AllEnemies:
            default:
                return selectedTarget;
        }
    }

    private static void ApplyResolvedStatus(StatusKind status, CombatActor target, int value, PassiveSystem passiveSystem)
    {
        if (target == null || target.status == null || value <= 0)
            return;

        switch (status)
        {
            case StatusKind.Burn:
                target.status.ApplyBurn(value + GetBonusStatusStacks(passiveSystem, StatusKind.Burn), 3);
                break;
            case StatusKind.Mark:
                target.status.ApplyMark();
                break;
            case StatusKind.Bleed:
                target.status.ApplyBleed(value + GetBonusStatusStacks(passiveSystem, StatusKind.Bleed));
                break;
            case StatusKind.Freeze:
                target.status.ApplyFreeze();
                break;
        }
    }

    private static int GetBonusStatusStacks(PassiveSystem passiveSystem, StatusKind status)
        => passiveSystem != null ? passiveSystem.GetBonusStatusStacksApplied(status) : 0;

    private static void ConsumeResolvedStatus(StatusKind status, CombatActor target, int expectedStacks)
    {
        if (target == null || target.status == null)
            return;

        if (status == StatusKind.Burn && expectedStacks > 0)
        {
            target.status.ConsumeAllBurn();
            CombatHitFeedback.Play(target, CombatHitFeedback.FeedbackKind.BurnConsume);
            return;
        }

        if (status == StatusKind.Mark && target.status.marked)
        {
            CombatHitFeedback.Play(target, CombatHitFeedback.FeedbackKind.MarkPayoff);
            target.status.marked = false;
        }
    }
}

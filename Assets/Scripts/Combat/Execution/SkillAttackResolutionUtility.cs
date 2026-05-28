using System.Collections.Generic;
using UnityEngine;

internal static class SkillAttackResolutionUtility
{
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
            CombatHitFeedback.Play(effectTarget, CombatHitFeedback.FeedbackKind.Hit);
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
                    CombatHitFeedback.Play(caster, CombatHitFeedback.FeedbackKind.Focus);
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
                    CombatHitFeedback.Play(caster, CombatHitFeedback.FeedbackKind.Guard);
                }

                int focusReward = Mathf.Max(0, rt.iceRewardFocus);
                if (focusReward > 0)
                {
                    focusReward += passiveSystem != null ? passiveSystem.GetFreezeBreakFocusBonusAdd() : 0;
                    caster.GainFocus(focusReward);
                    CombatHitFeedback.Play(caster, CombatHitFeedback.FeedbackKind.Focus);
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

                CombatHitFeedback.Play(effectTarget, CombatHitFeedback.FeedbackKind.MarkPayoff);
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
            target.status.ApplyBurn(emberBurn, 3);
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
                        CombatHitFeedback.Play(target, CombatHitFeedback.FeedbackKind.Guard);
                    break;
                case SkillEffectType.Heal:
                    if (target.Heal(value) > 0)
                        CombatHitFeedback.Play(target, CombatHitFeedback.FeedbackKind.Heal);
                    break;
                case SkillEffectType.GainAP:
                    target.GainFocus(value);
                    if (value > 0)
                        CombatHitFeedback.Play(target, CombatHitFeedback.FeedbackKind.Focus);
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

                CombatActor.DamageResult damageResult = effectTarget.TakeDamageDetailed(damage, bypassGuard: rt.bypassGuard);
                CombatHitFeedback.Play(effectTarget, CombatHitFeedback.FeedbackKind.Hit);
                if (damageResult.guardBroken && effectTarget.status != null)
                    effectTarget.status.ApplyStagger();
                if (popups != null)
                    popups.SpawnDamageSplit(caster, effectTarget, damageResult.blocked, damageResult.hpLost);
            }
        }

        ApplyResolvedGameplayEffects(effects, caster, selectedTarget, includeFollowUpEffects: true);
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

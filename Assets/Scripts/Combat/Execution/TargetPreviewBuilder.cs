using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds TargetPreviewData snapshots from a SkillRuntime + caster + targets.
/// Simulates combat outcome WITHOUT mutating any real combat state.
/// </summary>
public static class TargetPreviewBuilder
{
    public struct ActionPreviewBundle
    {
        public Dictionary<CombatActor, TargetPreviewData> targetPreviews;
        public int totalSelfFocusGain;
        public int totalSelfGuardGain;
        public int totalSelfHealGain;
        public bool valid;
    }

    /// <summary>
    /// Build preview for one specific target.
    /// </summary>
    public static TargetPreviewData Build(SkillRuntime rt, CombatActor caster, CombatActor target, int dieValue)
    {
        TargetPreviewData data = CreateBaselineData(caster, target);
        if (!data.valid || rt == null)
            return data;

        bool isSelf = caster == target;

        if (rt.kind == SkillKind.Guard && isSelf)
        {
            int baseGuard = rt.CalculateGuard(dieValue);
            if (rt.guardValueMode == BaseEffectValueMode.Flat && rt.guardFlat > 0)
                baseGuard = SkillOutputValueUtility.AddActionAddedValue(rt.guardFlat, rt);

            float pct = 0f;
            PassiveSystem ps = caster != null ? caster.GetComponent<PassiveSystem>() : null;
            if (ps != null)
                pct = ps.GetGuardGainPercent();

            float mult = 1f + Mathf.Max(-0.99f, pct);
            int scaledGuard = Mathf.FloorToInt(baseGuard * mult);

            data.selfGuardGain = scaledGuard;
            data.previewGuardAfter = target.guardPool + scaledGuard;
            return data;
        }

        if (rt.kind == SkillKind.Attack)
        {
            BuildAttackPreview(rt, caster, target, dieValue, ref data);
            return data;
        }

        if (rt.kind == SkillKind.Utility)
        {
            BuildUtilityPreview(rt, dieValue, ref data);
            return data;
        }

        return data;
    }

    /// <summary>
    /// Build the full action preview for the current cast, including AoE targets,
    /// lightning mark shock propagation, and self rewards on the caster.
    /// </summary>
    public static ActionPreviewBundle BuildActionBundle(
        SkillRuntime rt,
        CombatActor caster,
        CombatActor clickedTarget,
        int dieValue,
        BattlePartyManager2D party,
        CombatActor fallbackEnemy)
    {
        ActionPreviewBundle bundle = new ActionPreviewBundle
        {
            targetPreviews = new Dictionary<CombatActor, TargetPreviewData>(),
            totalSelfFocusGain = rt != null ? Mathf.Max(0, rt.focusGainOnCast) : 0,
            totalSelfGuardGain = 0,
            totalSelfHealGain = 0,
            valid = false
        };

        if (rt == null || caster == null)
            return bundle;

        SkillDamageSO sourceSkill = SkillGameplayResolver.GetSourceSkill(rt);
        if (SkillGameplayResolver.CanResolveWithNewPipeline(sourceSkill))
            return BuildResolvedGameplayBundle(rt, caster, clickedTarget, bundle);

        List<CombatActor> actionTargets = ResolveActionTargets(rt, caster, clickedTarget, party, fallbackEnemy);
        if (actionTargets.Count <= 0)
            return bundle;

        int lightningShockProcCount = 0;
        int lightningShockDamagePerProc = 0;

        for (int i = 0; i < actionTargets.Count; i++)
        {
            CombatActor target = actionTargets[i];
            if (target == null)
                continue;

            TargetPreviewData data = Build(rt, caster, target, dieValue);
            bundle.targetPreviews[target] = data;
            bundle.totalSelfFocusGain += Mathf.Max(0, data.selfFocusGain);
            bool previewAlreadyOnCaster = target == caster && (rt.kind == SkillKind.Guard || rt.kind == SkillKind.Utility);
            if (!previewAlreadyOnCaster)
                bundle.totalSelfGuardGain += Mathf.Max(0, data.selfGuardGain);
            bundle.totalSelfHealGain += Mathf.Max(0, data.selfHealGain);
            bundle.valid |= data.valid;

            if (ShouldTriggerLightningShock(rt, target))
            {
                lightningShockProcCount++;
                lightningShockDamagePerProc = Mathf.Max(lightningShockDamagePerProc, GetLightningShockDamagePerProc(rt, caster));
                data.willTriggerMarkShock = true;
                bundle.targetPreviews[target] = data;
            }

        }

        if (bundle.totalSelfGuardGain > 0)
            AddCasterGuardPreview(caster, bundle.totalSelfGuardGain, ref bundle);
        if (bundle.totalSelfHealGain > 0)
            AddCasterHealPreview(caster, bundle.totalSelfHealGain, ref bundle);

        if (lightningShockProcCount > 0 && lightningShockDamagePerProc > 0)
            ApplyLightningShockBoardPreview(rt, caster, party, fallbackEnemy, lightningShockDamagePerProc, lightningShockProcCount, ref bundle);

        return bundle;
    }

    private static ActionPreviewBundle BuildResolvedGameplayBundle(
        SkillRuntime rt,
        CombatActor caster,
        CombatActor clickedTarget,
        ActionPreviewBundle bundle)
    {
        SkillResolvedResult resolved = SkillGameplayResolver.Resolve(rt, caster, clickedTarget);
        if (resolved == null || !resolved.canCast || resolved.effects == null)
            return bundle;

        PassiveSystem passiveSystem = caster != null ? caster.GetComponent<PassiveSystem>() : null;
        int totalShockProcCount = 0;
        int shockDamagePerProc = 0;
        var markPayoffAppliedTargets = new HashSet<CombatActor>();
        var iceRewardAppliedTargets = new HashSet<CombatActor>();

        var primaryDamagePreviewTargets = new HashSet<CombatActor>();
        for (int i = 0; i < resolved.effects.Count; i++)
        {
            ResolvedEffect effect = resolved.effects[i];
            if (effect == null || !effect.previewable)
                continue;

            CombatActor effectTarget = effect.targetActor != null ? effect.targetActor : clickedTarget;
            if (effectTarget == null)
                continue;

            if (!bundle.targetPreviews.TryGetValue(effectTarget, out TargetPreviewData data))
                data = CreateBaselineData(caster, effectTarget);

            int value = Mathf.Max(0, effect.value);
            switch (effect.type)
            {
                case SkillEffectType.DealDamage:
                case SkillEffectType.DealSecondaryDamage:
                    bool isPrimaryDamage = effect.type == SkillEffectType.DealDamage;
                    bool isFirstPrimaryDamageForTarget = isPrimaryDamage && primaryDamagePreviewTargets.Add(effectTarget);
                    bool canConsumeStagger = isFirstPrimaryDamageForTarget &&
                                             !effect.sameActionFollowUp &&
                                             (AttackPreviewCalculator.CanConsumeStagger(rt, effectTarget) || data.willBreakGuard);
                    bool targetHadMarkBeforeHit = isFirstPrimaryDamageForTarget &&
                                                  !effect.sameActionFollowUp &&
                                                  AttackPreviewCalculator.CanUseMarkPayoff(rt, effectTarget);
                    bool targetWasFrozenOrChilled = isFirstPrimaryDamageForTarget &&
                                                    effectTarget.status != null &&
                                                    (effectTarget.status.frozen || effectTarget.status.chilledTurns > 0);
                    if (canConsumeStagger)
                    {
                        value = Mathf.FloorToInt(value * 1.2f);
                        if (value < 1)
                            value = 1;
                    }

                    bool applyMarkPayoffNow = targetHadMarkBeforeHit && !markPayoffAppliedTargets.Contains(effectTarget);
                    if (applyMarkPayoffNow && rt.element != ElementType.Lightning)
                    {
                        value += 3;
                        markPayoffAppliedTargets.Add(effectTarget);
                        data.previewMarkedAfter = false;
                    }

                    ApplyDamageToData(effectTarget, ref data, value, rt.bypassGuard, rt.clearsGuard, canBreakGuard: true, canConsumeStagger: canConsumeStagger);
                    if (isFirstPrimaryDamageForTarget)
                        ApplyEmberWeaponBurnPreview(rt, caster, value, ref data);

                    if (isFirstPrimaryDamageForTarget && applyMarkPayoffNow && rt.element == ElementType.Lightning && rt.triggerLightningMarkShock)
                    {
                        markPayoffAppliedTargets.Add(effectTarget);
                        shockDamagePerProc = Mathf.Max(shockDamagePerProc, GetLightningShockDamagePerProc(rt, caster));
                        totalShockProcCount++;
                    }

                    if (isFirstPrimaryDamageForTarget &&
                        rt.element == ElementType.Ice &&
                        rt.gainIceRewardOnFrozenOrChilledHit &&
                        targetWasFrozenOrChilled &&
                        iceRewardAppliedTargets.Add(effectTarget))
                    {
                        data.selfGuardGain += Mathf.Max(0, rt.iceRewardGuard);
                        data.selfFocusGain += Mathf.Max(0, rt.iceRewardFocus) +
                                              (passiveSystem != null ? passiveSystem.GetFreezeBreakFocusBonusAdd() : 0);
                    }
                    break;
                case SkillEffectType.ApplyStatus:
                    ApplyResolvedStatusPreview(effect.status, value, passiveSystem, ref data);
                    break;
                case SkillEffectType.ConsumeStatus:
                    if (effect.status == StatusKind.Burn)
                        data.previewBurnAfter = 0;
                    else if (effect.status == StatusKind.Mark)
                        data.previewMarkedAfter = false;
                    break;
                case SkillEffectType.GainGuard:
                    if (effectTarget == caster)
                        data.selfGuardGain += value;
                    else
                        data.previewGuardAfter += value;
                    break;
                case SkillEffectType.Heal:
                    if (effectTarget == caster)
                        data.selfHealGain += value;
                    data.previewHpAfter = Mathf.Min(data.currentMaxHp, data.previewHpAfter + value);
                    data.hpLost = data.currentHp - data.previewHpAfter;
                    break;
                case SkillEffectType.GainAP:
                    if (effectTarget == caster)
                        data.selfFocusGain += value;
                    break;
                case SkillEffectType.ClearGuard:
                    ApplyClearGuardToData(ref data);
                    break;
            }

            bundle.targetPreviews[effectTarget] = data;
            bundle.valid |= data.valid;
        }

        foreach (var pair in bundle.targetPreviews)
        {
            bundle.totalSelfFocusGain += Mathf.Max(0, pair.Value.selfFocusGain);
            bool previewAlreadyOnCaster = pair.Key == caster && (rt.kind == SkillKind.Guard || rt.kind == SkillKind.Utility);
            if (!previewAlreadyOnCaster)
                bundle.totalSelfGuardGain += Mathf.Max(0, pair.Value.selfGuardGain);
            bundle.totalSelfHealGain += Mathf.Max(0, pair.Value.selfHealGain);
        }

        if (bundle.totalSelfGuardGain > 0)
            AddCasterGuardPreview(caster, bundle.totalSelfGuardGain, ref bundle);
        if (bundle.totalSelfHealGain > 0)
            AddCasterHealPreview(caster, bundle.totalSelfHealGain, ref bundle);

        if (totalShockProcCount > 0 && shockDamagePerProc > 0)
            ApplyResolvedLightningShockPreview(caster, clickedTarget, shockDamagePerProc, totalShockProcCount, ref bundle);

        return bundle;
    }

    private static TargetPreviewData CreateBaselineData(CombatActor caster, CombatActor target)
    {
        TargetPreviewData data = default;
        if (target == null)
            return data;

        data.valid = true;
        data.currentHp = target.hp;
        data.currentMaxHp = target.maxHP;
        data.currentGuard = target.guardPool;
        data.currentlyStaggered = target.status != null && target.status.staggered;
        data.previewHpAfter = target.hp;
        data.previewGuardAfter = target.guardPool;

        int initialBurn = target.status != null ? target.status.burnStacks : 0;
        int initialBleed = target.status != null ? target.status.bleedStacks : 0;
        bool initialMark = target.status != null && target.status.marked;
        bool initialFreeze = target.status != null && target.status.frozen;

        data.currentBurn = initialBurn;
        data.currentBleed = initialBleed;
        data.currentMarked = initialMark;
        data.currentFrozen = initialFreeze;

        data.previewBurnAfter = initialBurn;
        data.previewBleedAfter = initialBleed;
        data.previewMarkedAfter = initialMark;
        data.previewFrozenAfter = initialFreeze;

        data.isSelfTarget = caster == target;
        return data;
    }

    private static void BuildAttackPreview(SkillRuntime rt, CombatActor caster, CombatActor target, int dieValue, ref TargetPreviewData data)
    {
        SkillExecutor.AttackPreview ap = AttackPreviewCalculator.BuildAttackPreview(rt, caster, target, dieValue);
        ApplyDamageToData(target, ref data, ap.finalDamage, rt.bypassGuard, rt.clearsGuard, canBreakGuard: true, canConsumeStagger: ap.consumesStagger);

        SkillDamageSO sourceSkill = SkillGameplayResolver.GetSourceSkill(rt);
        if (SkillGameplayResolver.CanResolveWithNewPipeline(sourceSkill))
        {
            ApplyEmberWeaponBurnPreview(rt, caster, ap.finalDamage, ref data);
            ApplyResolvedGameplayPreview(rt, caster, target, ref data);
            return;
        }

        PassiveSystem ps = caster != null ? caster.GetComponent<PassiveSystem>() : null;
        bool targetHadBurnBeforeHit = data.currentBurn > 0;
        bool targetHadMarkBeforeHit = data.currentMarked;
        bool targetWasFrozenOrChilled = target != null &&
                                        target.status != null &&
                                        (target.status.frozen || target.status.chilledTurns > 0);

        if (rt.consumesBurn && target.status != null)
            data.previewBurnAfter = 0;

        if (rt.applyBurn)
            data.previewBurnAfter += GetBurnStacksToApply(rt, dieValue, caster);

        if (rt.conditionMet && rt.conditionalOutcomeEnabled && rt.conditionalOutcomeType == ConditionalOutcomeType.ApplyBurn)
            data.previewBurnAfter += GetConditionalBurnStacks(rt, dieValue, caster);


        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, FireDamageBehaviorId.Hellfire) && targetHadBurnBeforeHit)
        {
            int sevensRolled = SkillBehaviorRuntimeUtility.CountBaseValuesEqual(rt, 7);
            if (sevensRolled > 0)
            {
                int reapplyBurn = (7 * sevensRolled) + (ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Burn) : 0);
                data.previewBurnAfter += reapplyBurn;
            }
        }
        else if (rt.element == ElementType.Fire && rt.fireReapplyBurnPerExactBase)
        {
            bool canReapply = !rt.fireRequireBurnBeforeHitForReapply || targetHadBurnBeforeHit;
            if (canReapply)
            {
                int matchCount = SkillBehaviorRuntimeUtility.CountBaseValuesEqual(rt, rt.fireExactBaseForReapply);
                if (matchCount > 0)
                {
                    int reapplyBurn = (Mathf.Max(0, rt.fireBurnPerExactMatch) * matchCount) +
                                      (ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Burn) : 0);
                    data.previewBurnAfter += reapplyBurn;
                }
            }
        }

        if (rt.applyMark)
            data.previewMarkedAfter = true;
        if (AttackPreviewCalculator.CanUseMarkPayoff(rt, target) && rt.element != ElementType.Lightning)
            data.previewMarkedAfter = false;

        if (rt.applyBleed)
            data.previewBleedAfter += GetBleedStacksToApply(rt, caster);

        if (rt.applyFreeze)
        {
            bool canFreeze = target.status == null || (!target.status.frozen && target.status.chilledTurns <= 0);
            if (canFreeze)
                data.previewFrozenAfter = true;
        }

        if (rt.element == ElementType.Ice && rt.gainIceRewardOnFrozenOrChilledHit && targetWasFrozenOrChilled)
        {
            int guardReward = Mathf.Max(0, rt.iceRewardGuard);
            int focusReward = Mathf.Max(0, rt.iceRewardFocus);
            if (ps != null && focusReward > 0)
                focusReward += ps.GetFreezeBreakFocusBonusAdd();

            data.selfGuardGain += guardReward;
            data.selfFocusGain += Mathf.Max(0, focusReward);
        }

        if (rt.conditionMet && rt.conditionalOutcomeEnabled && rt.conditionalOutcomeType == ConditionalOutcomeType.GainGuard)
            data.selfGuardGain += GetConditionalGuardValue(rt, dieValue);

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, FireDamageBehaviorId.BiteTheDust))
            data.selfHealGain += Mathf.FloorToInt(Mathf.Max(0, data.currentBurn) / 5f) * 3;

        if (rt.fireGainGuardFromHighestBase)
        {
            int guardIndex = SkillBehaviorRuntimeUtility.GetHighestBaseValueIndex(rt);
            int guard = SkillBehaviorRuntimeUtility.GetPerDieResolvedOutput(rt, guardIndex);
            data.selfGuardGain += Mathf.Max(0, guard);
        }
    }

    private static void ApplyResolvedGameplayPreview(SkillRuntime rt, CombatActor caster, CombatActor target, ref TargetPreviewData data)
    {
        SkillResolvedResult resolved = SkillGameplayResolver.Resolve(rt, caster, target);
        if (resolved == null || resolved.effects == null)
            return;

        PassiveSystem passiveSystem = caster != null ? caster.GetComponent<PassiveSystem>() : null;
        for (int i = 0; i < resolved.effects.Count; i++)
        {
            ResolvedEffect effect = resolved.effects[i];
            if (effect == null || !effect.previewable)
                continue;

            int value = Mathf.Max(0, effect.value);
            switch (effect.type)
            {
                case SkillEffectType.ApplyStatus:
                    ApplyResolvedStatusPreview(effect.status, value, passiveSystem, ref data);
                    break;
                case SkillEffectType.ConsumeStatus:
                    if (effect.status == StatusKind.Burn)
                        data.previewBurnAfter = 0;
                    break;
                case SkillEffectType.GainGuard:
                    if (effect.target == SkillEffectTarget.Self)
                        data.selfGuardGain += value;
                    break;
                case SkillEffectType.Heal:
                    if (effect.target == SkillEffectTarget.Self)
                        data.selfHealGain += value;
                    break;
                case SkillEffectType.GainAP:
                    if (effect.target == SkillEffectTarget.Self)
                        data.selfFocusGain += value;
                    break;
            }
        }
    }

    private static void ApplyResolvedStatusPreview(StatusKind status, int value, PassiveSystem passiveSystem, ref TargetPreviewData data)
    {
        if (value <= 0 && status != StatusKind.Mark && status != StatusKind.Freeze)
            return;

        switch (status)
        {
            case StatusKind.Burn:
                data.previewBurnAfter += value + (passiveSystem != null ? passiveSystem.GetBonusStatusStacksApplied(StatusKind.Burn) : 0);
                break;
            case StatusKind.Mark:
                data.previewMarkedAfter = true;
                break;
            case StatusKind.Bleed:
                data.previewBleedAfter += value + (passiveSystem != null ? passiveSystem.GetBonusStatusStacksApplied(StatusKind.Bleed) : 0);
                break;
            case StatusKind.Freeze:
                data.previewFrozenAfter = true;
                break;
        }
    }

    private static void ApplyEmberWeaponBurnPreview(SkillRuntime rt, CombatActor caster, int finalDamage, ref TargetPreviewData data)
    {
        if (rt == null || rt.coreAction != CoreAction.BasicStrike || caster == null || caster.status == null)
            return;
        if (caster.status.emberWeaponTurns <= 0 || !caster.status.emberWeaponBurnEqualsDamage)
            return;
        if (caster.status.emberWeaponBurnOnCritOnly && !rt.localCritAny)
            return;

        int emberBurn = Mathf.Max(0, finalDamage);
        PassiveSystem passiveSystem = caster.GetComponent<PassiveSystem>();
        emberBurn += passiveSystem != null ? passiveSystem.GetBonusStatusStacksApplied(StatusKind.Burn) : 0;
        if (emberBurn > 0)
            data.previewBurnAfter += emberBurn;
    }

    private static void BuildUtilityPreview(SkillRuntime rt, int dieValue, ref TargetPreviewData data)
    {
        int healAmount = 0;
        if (rt.sourceAsset is SkillBuffDebuffSO buffDebuffAsset && buffDebuffAsset.effects != null)
        {
            for (int i = 0; i < buffDebuffAsset.effects.Count; i++)
            {
                BuffDebuffEffectEntry effect = buffDebuffAsset.effects[i];
                if (effect == null)
                    continue;

                if (effect.id == BuffDebuffEffectId.HealFlat)
                    healAmount += Mathf.Max(0, effect.GetHealAmount());
                else if (effect.id == BuffDebuffEffectId.HealByDiceSum)
                    healAmount += Mathf.Max(0, dieValue);
            }
        }

        if (healAmount > 0)
        {
            int hpAfter = Mathf.Min(data.currentMaxHp, data.currentHp + healAmount);
            data.previewHpAfter = hpAfter;
            data.hpLost = data.currentHp - hpAfter;
        }
    }

    private static List<CombatActor> ResolveActionTargets(
        SkillRuntime rt,
        CombatActor caster,
        CombatActor clickedTarget,
        BattlePartyManager2D party,
        CombatActor fallbackEnemy)
    {
        var list = new List<CombatActor>();
        if (rt == null)
            return list;

        IReadOnlyList<CombatActor> aoeTargets = TurnManagerCombatUtility.ResolveAoeTargets(rt, caster, clickedTarget, party, fallbackEnemy);
        if (aoeTargets != null && aoeTargets.Count > 0)
        {
            for (int i = 0; i < aoeTargets.Count; i++)
            {
                CombatActor target = aoeTargets[i];
                if (target != null && !target.IsDead)
                    list.Add(target);
            }
        }
        else if (clickedTarget != null && !clickedTarget.IsDead)
        {
            list.Add(clickedTarget);
        }

        return list;
    }

    private static void ApplyLightningShockBoardPreview(
        SkillRuntime rt,
        CombatActor caster,
        BattlePartyManager2D party,
        CombatActor fallbackEnemy,
        int shockDamagePerProc,
        int shockProcCount,
        ref ActionPreviewBundle bundle)
    {
        IReadOnlyList<CombatActor> shockTargets = caster.team == CombatActor.TeamSide.Enemy
            ? TurnManagerCombatUtility.ResolveAliveAlliesSnapshot(party, caster)
            : TurnManagerCombatUtility.ResolveAliveEnemiesSnapshot(party, fallbackEnemy);

        if (shockTargets == null)
            return;

        for (int i = 0; i < shockTargets.Count; i++)
        {
            CombatActor target = shockTargets[i];
            if (target == null || target.IsDead || target == caster || target.team == caster.team)
                continue;

            TargetPreviewData data = bundle.targetPreviews.TryGetValue(target, out TargetPreviewData existing)
                ? existing
                : CreateBaselineData(caster, target);

            for (int proc = 0; proc < shockProcCount; proc++)
                ApplyDamageToData(target, ref data, shockDamagePerProc, bypassGuard: false, clearsGuard: false, canBreakGuard: false, canConsumeStagger: false);

            bundle.targetPreviews[target] = data;
            bundle.valid |= data.valid;
        }
    }

    private static void ApplyResolvedLightningShockPreview(
        CombatActor caster,
        CombatActor clickedTarget,
        int shockDamagePerProc,
        int shockProcCount,
        ref ActionPreviewBundle bundle)
    {
        var context = new SkillResolveContext
        {
            caster = caster,
            target = clickedTarget
        };

        List<CombatActor> shockTargets = SkillGameplayResolver.ResolveEffectTargets(SkillEffectTarget.AllEnemies, context);
        if (shockTargets == null)
            return;

        for (int i = 0; i < shockTargets.Count; i++)
        {
            CombatActor target = shockTargets[i];
            if (target == null || target.IsDead || target == caster || target.team == caster.team)
                continue;

            TargetPreviewData data = bundle.targetPreviews.TryGetValue(target, out TargetPreviewData existing)
                ? existing
                : CreateBaselineData(caster, target);

            for (int proc = 0; proc < shockProcCount; proc++)
                ApplyDamageToData(target, ref data, shockDamagePerProc, bypassGuard: false, clearsGuard: false, canBreakGuard: false, canConsumeStagger: false);

            bundle.targetPreviews[target] = data;
            bundle.valid |= data.valid;
        }
    }

    private static void AddCasterGuardPreview(CombatActor caster, int totalGuardGain, ref ActionPreviewBundle bundle)
    {
        if (caster == null || totalGuardGain <= 0)
            return;

        TargetPreviewData casterPreview = bundle.targetPreviews.TryGetValue(caster, out TargetPreviewData existing)
            ? existing
            : CreateBaselineData(caster, caster);

        casterPreview.isSelfTarget = true;
        casterPreview.selfGuardGain += totalGuardGain;
        casterPreview.previewGuardAfter += totalGuardGain;
        bundle.targetPreviews[caster] = casterPreview;
        bundle.valid |= casterPreview.valid;
    }

    private static void AddCasterHealPreview(CombatActor caster, int totalHealGain, ref ActionPreviewBundle bundle)
    {
        if (caster == null || totalHealGain <= 0)
            return;

        TargetPreviewData casterPreview = bundle.targetPreviews.TryGetValue(caster, out TargetPreviewData existing)
            ? existing
            : CreateBaselineData(caster, caster);

        casterPreview.isSelfTarget = true;
        casterPreview.selfHealGain += totalHealGain;
        int hpAfter = Mathf.Min(casterPreview.currentMaxHp, casterPreview.previewHpAfter + totalHealGain);
        casterPreview.previewHpAfter = hpAfter;
        casterPreview.hpLost = casterPreview.currentHp - hpAfter;
        bundle.targetPreviews[caster] = casterPreview;
        bundle.valid |= casterPreview.valid;
    }

    private static void ApplyDamageToData(
        CombatActor target,
        ref TargetPreviewData data,
        int damage,
        bool bypassGuard,
        bool clearsGuard,
        bool canBreakGuard,
        bool canConsumeStagger)
    {
        if (target == null || damage <= 0)
            return;

        int guardBefore = data.previewGuardAfter;
        int hpBefore = data.previewHpAfter;
        int remaining = Mathf.Max(0, damage);
        int guardAfter = guardBefore;
        int hpAfter = hpBefore;
        bool guardBroken = false;

        if (!bypassGuard && guardBefore > 0)
        {
            int blocked = Mathf.Min(guardBefore, remaining);
            guardAfter = guardBefore - blocked;
            remaining -= blocked;
            guardBroken = canBreakGuard && guardBefore > 0 && guardAfter <= 0 && blocked > 0;
        }

        if (bypassGuard)
            guardAfter = guardBefore;

        if (clearsGuard)
            guardAfter = 0;

        if (remaining > 0)
            hpAfter = Mathf.Max(0, hpBefore - remaining);

        data.previewHpAfter = hpAfter;
        data.previewGuardAfter = guardAfter;
        data.hpLost = data.currentHp - hpAfter;
        data.guardLost = data.currentGuard - guardAfter;

        if (guardBroken)
            data.willBreakGuard = true;
        if (canConsumeStagger)
            data.willConsumeStagger = true;
    }

    private static void ApplyClearGuardToData(ref TargetPreviewData data)
    {
        int guardBefore = Mathf.Max(0, data.previewGuardAfter);
        if (guardBefore <= 0)
            return;

        data.previewGuardAfter = 0;
        data.guardLost = data.currentGuard - data.previewGuardAfter;
        data.willBreakGuard = true;
    }

    private static bool ShouldTriggerLightningShock(SkillRuntime rt, CombatActor target)
    {
        return rt != null &&
               rt.element == ElementType.Lightning &&
               rt.triggerLightningMarkShock &&
               AttackPreviewCalculator.CanUseMarkPayoff(rt, target);
    }

    private static int GetLightningShockDamagePerProc(SkillRuntime rt, CombatActor caster)
    {
        if (rt == null)
            return 0;

        PassiveSystem ps = caster != null ? caster.GetComponent<PassiveSystem>() : null;
        float shockMul = 1f;
        if (ps != null)
            shockMul += Mathf.Max(0f, ps.GetLightningVsMarkMultiplierAdd());

        int baseShockDamage = Mathf.Max(0, rt.lightningMarkShockDamage);
        return Mathf.FloorToInt(baseShockDamage * shockMul);
    }

    private static int GetBurnStacksToApply(SkillRuntime rt, int dieValue, CombatActor caster)
    {
        if (rt == null || !rt.applyBurn)
            return 0;

        PassiveSystem ps = caster != null ? caster.GetComponent<PassiveSystem>() : null;

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, FireDamageBehaviorId.FireSlash))
            return rt.conditionMet ? Mathf.Max(0, rt.burnAddStacks) : 0;

        int burn = SkillOutputValueUtility.ResolveStatusStacks(
            rt.burnAddStacks,
            rt,
            rt.baseBurnValueMode,
            dieValue,
            rt.fireApplyBurnFromResolvedValue);

        if (rt.fireGrantBonusBurnOnOddBase &&
            SkillBehaviorRuntimeUtility.TryGetSingleBaseValue(rt, out int fireBaseValue) &&
            (fireBaseValue % 2) != 0)
        {
            burn += Mathf.Max(0, rt.fireOddBaseBonusBurn);
        }

        burn += ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Burn) : 0;
        return Mathf.Max(0, burn);
    }

    private static int GetConditionalBurnStacks(SkillRuntime rt, int dieValue, CombatActor caster)
    {
        if (rt == null || !rt.conditionMet || !rt.conditionalOutcomeEnabled || rt.conditionalOutcomeType != ConditionalOutcomeType.ApplyBurn)
            return 0;

        int burn = SkillOutputValueUtility.ResolveConditionalStatusStacks(
            Mathf.Max(0, rt.conditionalOutcomeFlatValue),
            rt,
            rt.conditionalOutcomeValueMode,
            dieValue);

        PassiveSystem ps = caster != null ? caster.GetComponent<PassiveSystem>() : null;
        burn += ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Burn) : 0;
        return Mathf.Max(0, burn);
    }

    private static int GetConditionalGuardValue(SkillRuntime rt, int dieValue)
    {
        if (rt == null || !rt.conditionMet || !rt.conditionalOutcomeEnabled || rt.conditionalOutcomeType != ConditionalOutcomeType.GainGuard)
            return 0;

        return rt.conditionalOutcomeValueMode == ConditionalOutcomeValueMode.X
            ? SkillOutputValueUtility.ResolveXValue(dieValue, rt)
            : SkillOutputValueUtility.AddActionAddedValue(Mathf.Max(0, rt.conditionalOutcomeFlatValue), rt);
    }

    private static int GetBleedStacksToApply(SkillRuntime rt, CombatActor caster)
    {
        if (rt == null || !rt.applyBleed)
            return 0;

        PassiveSystem ps = caster != null ? caster.GetComponent<PassiveSystem>() : null;
        int bleed = SkillOutputValueUtility.ResolveFlatStatusStacks(rt.bleedTurns);
        bleed += ps != null ? ps.GetBonusStatusStacksApplied(StatusKind.Bleed) : 0;
        return Mathf.Max(0, bleed);
    }

    private static bool IsBasicStrike(SkillRuntime rt)
    {
        return rt != null && rt.coreAction == CoreAction.BasicStrike;
    }
}

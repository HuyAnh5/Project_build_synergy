using System.Collections.Generic;
using UnityEngine;

// Converts SkillGameplayResolver effects into non-mutating target preview data.
public static partial class TargetPreviewBuilder
{
    // Builds a full action preview for skills authored through the resolved gameplay pipeline.
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

            ApplyResolvedEffectPreview(
                rt,
                caster,
                effect,
                effectTarget,
                passiveSystem,
                primaryDamagePreviewTargets,
                markPayoffAppliedTargets,
                iceRewardAppliedTargets,
                ref data,
                ref shockDamagePerProc,
                ref totalShockProcCount);

            bundle.targetPreviews[effectTarget] = data;
            bundle.valid |= data.valid;
        }

        AddResolvedSelfPreviewTotals(rt, caster, ref bundle);

        if (totalShockProcCount > 0 && shockDamagePerProc > 0)
            ApplyResolvedLightningShockPreview(caster, clickedTarget, shockDamagePerProc, totalShockProcCount, ref bundle);

        return bundle;
    }

    // Applies one resolver effect to a single target preview.
    private static void ApplyResolvedEffectPreview(
        SkillRuntime rt,
        CombatActor caster,
        ResolvedEffect effect,
        CombatActor effectTarget,
        PassiveSystem passiveSystem,
        HashSet<CombatActor> primaryDamagePreviewTargets,
        HashSet<CombatActor> markPayoffAppliedTargets,
        HashSet<CombatActor> iceRewardAppliedTargets,
        ref TargetPreviewData data,
        ref int shockDamagePerProc,
        ref int totalShockProcCount)
    {
        int value = Mathf.Max(0, effect.value);
        switch (effect.type)
        {
            case SkillEffectType.DealDamage:
            case SkillEffectType.DealSecondaryDamage:
                ApplyResolvedDamagePreview(
                    rt,
                    caster,
                    effect,
                    effectTarget,
                    passiveSystem,
                    primaryDamagePreviewTargets,
                    markPayoffAppliedTargets,
                    iceRewardAppliedTargets,
                    ref data,
                    ref value,
                    ref shockDamagePerProc,
                    ref totalShockProcCount);
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
    }

    // Applies resolved direct or secondary damage, including Stagger, Mark, Ember, and Ice rewards.
    private static void ApplyResolvedDamagePreview(
        SkillRuntime rt,
        CombatActor caster,
        ResolvedEffect effect,
        CombatActor effectTarget,
        PassiveSystem passiveSystem,
        HashSet<CombatActor> primaryDamagePreviewTargets,
        HashSet<CombatActor> markPayoffAppliedTargets,
        HashSet<CombatActor> iceRewardAppliedTargets,
        ref TargetPreviewData data,
        ref int value,
        ref int shockDamagePerProc,
        ref int totalShockProcCount)
    {
        bool isPrimaryDamage = effect.type == SkillEffectType.DealDamage;
        bool isFirstPrimaryDamageForTarget = isPrimaryDamage && primaryDamagePreviewTargets.Add(effectTarget);
        bool canConsumeStagger = effect.sameActionFollowUp
            ? (AttackPreviewCalculator.CanConsumeStagger(rt, effectTarget) || data.willBreakGuard)
            : (isFirstPrimaryDamageForTarget &&
               (AttackPreviewCalculator.CanConsumeStagger(rt, effectTarget) || data.willBreakGuard));
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
    }

    // Adds self rewards from resolved effects back into the bundle totals.
    private static void AddResolvedSelfPreviewTotals(SkillRuntime rt, CombatActor caster, ref ActionPreviewBundle bundle)
    {
        foreach (var pair in bundle.targetPreviews)
        {
            bundle.totalSelfFocusGain += Mathf.Max(0, pair.Value.selfFocusGain);
            bool previewAlreadyOnCaster = pair.Key == caster;
            if (!previewAlreadyOnCaster)
                bundle.totalSelfGuardGain += Mathf.Max(0, pair.Value.selfGuardGain);
            if (!previewAlreadyOnCaster)
                bundle.totalSelfHealGain += Mathf.Max(0, pair.Value.selfHealGain);
        }

        if (bundle.totalSelfGuardGain > 0)
            AddCasterGuardPreview(caster, bundle.totalSelfGuardGain, ref bundle);
        if (bundle.totalSelfHealGain > 0)
            AddCasterHealPreview(caster, bundle.totalSelfHealGain, ref bundle);
    }

    // Applies target-local resolved effects when building a single-target preview.
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

    // Applies Burn, Mark, Bleed, or Freeze status preview values.
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

    // Applies lightning Mark shock to every enemy resolved by the new target pipeline.
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
}

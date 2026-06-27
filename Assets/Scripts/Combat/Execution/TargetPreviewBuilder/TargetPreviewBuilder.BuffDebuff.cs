using System.Collections.Generic;
using UnityEngine;

public static partial class TargetPreviewBuilder
{
    private static ActionPreviewBundle TryBuildBuffDebuffFlowBundle(
        SkillRuntime rt,
        CombatActor caster,
        CombatActor clickedTarget,
        ActionPreviewBundle bundle)
    {
        if (rt == null || rt.sourceAsset is not SkillBuffDebuffSO skill || caster == null)
            return bundle;

        List<BuffDebuffFlowEffectData> effects = BuffDebuffFlowRuntimeUtility.ResolveEffects(skill, rt, caster, clickedTarget);
        for (int i = 0; i < effects.Count; i++)
            ApplyBuffDebuffFlowEffectPreview(effects[i], caster, clickedTarget, ref bundle);

        return bundle;
    }

    private static void ApplyBuffDebuffFlowEffectPreview(
        BuffDebuffFlowEffectData effect,
        CombatActor caster,
        CombatActor selectedTarget,
        ref ActionPreviewBundle bundle)
    {
        if (effect == null || !effect.previewable || caster == null || bundle.targetPreviews == null)
            return;

        CombatActor target = BuffDebuffFlowTargetResolver.Resolve(effect.target, caster, selectedTarget);
        if (target == null)
            return;

        TargetPreviewData data = bundle.targetPreviews.TryGetValue(target, out TargetPreviewData existing)
            ? existing
            : CreateBaselineData(caster, target);

        int amount = Mathf.Max(0, effect.amount);
        bool applied = true;
        switch (effect.type)
        {
            case BuffDebuffFlowEffectType.GainAP:
                ApplyBuffDebuffFocusPreview(caster, target, amount, ref data, ref bundle);
                break;
            case BuffDebuffFlowEffectType.GainGuard:
                ApplyBuffDebuffGuardPreview(caster, target, amount, ref data, ref bundle);
                break;
            case BuffDebuffFlowEffectType.Heal:
                ApplyBuffDebuffHealPreview(caster, target, amount, ref data, ref bundle);
                break;
            case BuffDebuffFlowEffectType.ApplyStatus:
                ApplyBuffDebuffStatusPreview(effect.status, amount, ref data);
                break;
            default:
                applied = false;
                break;
        }

        if (!applied)
            return;

        bundle.targetPreviews[target] = data;
        bundle.valid |= data.valid;
    }

    private static void ApplyBuffDebuffFocusPreview(
        CombatActor caster,
        CombatActor target,
        int amount,
        ref TargetPreviewData data,
        ref ActionPreviewBundle bundle)
    {
        bundle.totalSelfFocusGain += amount;
        if (target == caster)
            data.selfFocusGain += amount;
    }

    private static void ApplyBuffDebuffGuardPreview(
        CombatActor caster,
        CombatActor target,
        int amount,
        ref TargetPreviewData data,
        ref ActionPreviewBundle bundle)
    {
        data.previewGuardAfter += amount;
        if (target != caster)
            return;

        data.selfGuardGain += amount;
        bundle.totalSelfGuardGain += amount;
    }

    private static void ApplyBuffDebuffHealPreview(
        CombatActor caster,
        CombatActor target,
        int amount,
        ref TargetPreviewData data,
        ref ActionPreviewBundle bundle)
    {
        data.previewHpAfter = Mathf.Min(data.currentMaxHp, data.previewHpAfter + amount);
        data.hpLost = data.currentHp - data.previewHpAfter;
        if (target != caster)
            return;

        data.selfHealGain += amount;
        bundle.totalSelfHealGain += amount;
    }

    private static void ApplyBuffDebuffStatusPreview(StatusKind status, int amount, ref TargetPreviewData data)
    {
        if (amount <= 0 && status != StatusKind.Mark && status != StatusKind.Freeze)
            return;

        switch (status)
        {
            case StatusKind.Burn:
                data.previewBurnAfter += amount;
                break;
            case StatusKind.Mark:
                data.previewMarkedAfter = true;
                break;
            case StatusKind.Bleed:
                data.previewBleedAfter += amount;
                break;
            case StatusKind.Freeze:
                data.previewFrozenAfter = true;
                break;
        }
    }
}

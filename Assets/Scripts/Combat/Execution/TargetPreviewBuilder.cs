using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds TargetPreviewData snapshots from a SkillRuntime + caster + targets.
/// Simulates combat outcome WITHOUT mutating any real combat state.
/// </summary>
public static partial class TargetPreviewBuilder
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

    public static void ApplyRepeatPreviewMultiplier(ref ActionPreviewBundle bundle, int multiplier)
    {
        if (multiplier <= 1 || bundle.targetPreviews == null)
            return;

        var keys = new List<CombatActor>(bundle.targetPreviews.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            CombatActor actor = keys[i];
            TargetPreviewData data = bundle.targetPreviews[actor];
            data.previewHpAfter = Mathf.Clamp(data.currentHp - ((data.currentHp - data.previewHpAfter) * multiplier), 0, data.currentMaxHp);
            data.previewGuardAfter = Mathf.Max(0, data.currentGuard + ((data.previewGuardAfter - data.currentGuard) * multiplier));
            data.hpLost = data.currentHp - data.previewHpAfter;
            data.guardLost = data.currentGuard - data.previewGuardAfter;
            data.previewBurnAfter = Mathf.Max(0, data.currentBurn + ((data.previewBurnAfter - data.currentBurn) * multiplier));
            data.previewBleedAfter = Mathf.Max(0, data.currentBleed + ((data.previewBleedAfter - data.currentBleed) * multiplier));
            data.selfGuardGain *= multiplier;
            data.selfFocusGain *= multiplier;
            data.selfHealGain *= multiplier;
            bundle.targetPreviews[actor] = data;
        }

        bundle.totalSelfFocusGain *= multiplier;
        bundle.totalSelfGuardGain *= multiplier;
        bundle.totalSelfHealGain *= multiplier;
    }

    public static void AddSelfResourcePreview(CombatActor caster, int guardGain, int healGain, ref ActionPreviewBundle bundle)
    {
        if (caster == null || bundle.targetPreviews == null)
            return;

        TargetPreviewData data = bundle.targetPreviews.TryGetValue(caster, out TargetPreviewData existing)
            ? existing
            : CreateBaselineData(caster, caster);

        if (guardGain > 0)
        {
            data.selfGuardGain += guardGain;
            data.previewGuardAfter += guardGain;
            bundle.totalSelfGuardGain += guardGain;
        }

        if (healGain > 0)
        {
            data.selfHealGain += healGain;
            int hpAfter = Mathf.Min(data.currentMaxHp, data.previewHpAfter + healGain);
            data.previewHpAfter = hpAfter;
            data.hpLost = data.currentHp - hpAfter;
            bundle.totalSelfHealGain += healGain;
        }

        bundle.targetPreviews[caster] = data;
        bundle.valid |= data.valid;
    }

}

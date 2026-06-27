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
            return data;

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
        CombatActor fallbackEnemy,
        int resolveCount = 1,
        SkillDamageSO sourceSkill = null)
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

        if (rt.kind == SkillKind.Utility && rt.sourceAsset is SkillBuffDebuffSO)
            return TryBuildBuffDebuffFlowBundle(rt, caster, clickedTarget, bundle);

        PassiveSystem passiveSystem = caster.GetComponent<PassiveSystem>();
        if (passiveSystem != null)
            bundle.totalSelfFocusGain += passiveSystem.PreviewRuntimeCritFocus(rt);

        ScriptableObject previousSourceAsset = rt.sourceAsset;
        if (sourceSkill != null)
            rt.sourceAsset = sourceSkill;

        try
        {
            sourceSkill = sourceSkill != null ? sourceSkill : SkillGameplayResolver.GetSourceSkill(rt);
            if (SkillGameplayResolver.CanResolveWithNewPipeline(sourceSkill))
            {
                SkillResolvedResult resolved = SkillGameplayResolver.Resolve(rt, caster, clickedTarget);
                int actionExecutionCount = resolved != null ? Mathf.Max(1, resolved.executionCount) : 1;
                int totalResolveCount = Mathf.Max(1, resolveCount) * actionExecutionCount;
                if (CombatActionPreviewSimulator.TrySimulateTargetFinalState(rt, caster, clickedTarget, dieValue, totalResolveCount, out TargetPreviewData simulatedData))
                {
                    bundle.targetPreviews[clickedTarget] = simulatedData;
                    bundle.valid = simulatedData.valid;
                    ApplyResolvedSelfResourceTotals(rt, caster, clickedTarget, totalResolveCount, ref bundle);
                    return bundle;
                }

                ActionPreviewBundle resolvedBundle = BuildResolvedGameplayBundle(rt, caster, clickedTarget, bundle);
                ApplyRepeatPreviewMultiplier(ref resolvedBundle, totalResolveCount);
                return resolvedBundle;
            }

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
        finally
        {
            rt.sourceAsset = previousSourceAsset;
        }
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

    private static void ApplyResolvedSelfResourceTotals(
        SkillRuntime rt,
        CombatActor caster,
        CombatActor clickedTarget,
        int resolveCount,
        ref ActionPreviewBundle bundle)
    {
        SkillResolvedResult resolved = SkillGameplayResolver.Resolve(rt, caster, clickedTarget);
        if (resolved == null || !resolved.canCast || resolved.effects == null)
            return;

        int selfFocusGain = 0;
        int selfGuardGain = 0;
        int selfHealGain = 0;
        for (int i = 0; i < resolved.effects.Count; i++)
        {
            ResolvedEffect effect = resolved.effects[i];
            if (effect == null || !effect.previewable || effect.sameActionFollowUp)
                continue;

            CombatActor effectTarget = effect.targetActor != null ? effect.targetActor : clickedTarget;
            if (effectTarget != caster)
                continue;

            int value = Mathf.Max(0, effect.value);
            switch (effect.type)
            {
                case SkillEffectType.GainAP:
                    selfFocusGain += value;
                    break;
                case SkillEffectType.GainGuard:
                    selfGuardGain += value;
                    break;
                case SkillEffectType.Heal:
                    selfHealGain += value;
                    break;
            }
        }

        int multiplier = Mathf.Max(1, resolveCount);
        selfFocusGain *= multiplier;
        selfGuardGain *= multiplier;
        selfHealGain *= multiplier;
        bundle.totalSelfFocusGain += selfFocusGain + Mathf.Max(0, rt.focusGainOnCast) * (multiplier - 1);
        if (selfGuardGain > 0 || selfHealGain > 0)
            AddSelfResourcePreview(caster, selfGuardGain, selfHealGain, ref bundle);
    }

}

internal static class CombatActionPreviewSimulator
{
    public static bool TrySimulateTargetFinalState(
        SkillRuntime runtime,
        CombatActor caster,
        CombatActor target,
        int dieValue,
        int resolveCount,
        out TargetPreviewData preview)
    {
        preview = CreateBaseline(caster, target);
        if (runtime == null || caster == null || target == null || !preview.valid)
            return false;

        SkillDamageSO sourceSkill = SkillGameplayResolver.GetSourceSkill(runtime);
        if (!SkillGameplayResolver.CanResolveWithNewPipeline(sourceSkill))
            return false;

        CombatActor targetClone = null;
        ActorSnapshot casterSnapshot = ActorSnapshot.Capture(caster);
        using (new CombatSimulationContext.Scope(suppressPresentation: true))
        {
            try
            {
                targetClone = CloneActorForPreview(target);
                int count = Mathf.Max(1, resolveCount);
                for (int i = 0; i < count; i++)
                {
                    SkillExecutor.AttackApplyResult result = SkillAttackResolutionUtility.ApplyAttack(
                        runtime,
                        caster,
                        targetClone,
                        dieValue,
                        popups: null,
                        context: null,
                        buildAttackPreview: (_, _, _, _) => default);

                    if (result.HasDelayedFollowUpEffects)
                        SkillAttackResolutionUtility.ApplyResolvedGameplayFollowUpEffects(runtime, caster, targetClone, result.delayedFollowUpEffects, popups: null);
                    if (result.HasDelayedPassiveMeleeFollowUpEffects)
                        SkillAttackResolutionUtility.ApplyPassiveMeleeFollowUpEffects(runtime, caster, targetClone, result.delayedPassiveMeleeFollowUpEffects, popups: null);
                }

                preview = CreateFinalDiff(caster, target, targetClone);
                return true;
            }
            finally
            {
                casterSnapshot.Restore(caster);
                if (targetClone != null)
                    UnityEngine.Object.DestroyImmediate(targetClone.gameObject);
            }
        }
    }

    private static TargetPreviewData CreateBaseline(CombatActor caster, CombatActor target)
    {
        TargetPreviewData data = default;
        if (target == null)
            return data;

        data.valid = true;
        data.currentHp = target.hp;
        data.currentMaxHp = target.maxHP;
        data.currentGuard = target.guardPool;
        data.previewHpAfter = target.hp;
        data.previewGuardAfter = target.guardPool;
        data.currentlyStaggered = target.status != null && target.status.staggered;
        data.currentBurn = target.status != null ? target.status.burnStacks : 0;
        data.currentBleed = target.status != null ? target.status.bleedStacks : 0;
        data.currentMarked = target.status != null && target.status.marked;
        data.currentFrozen = target.status != null && target.status.frozen;
        data.previewBurnAfter = data.currentBurn;
        data.previewBleedAfter = data.currentBleed;
        data.previewMarkedAfter = data.currentMarked;
        data.previewFrozenAfter = data.currentFrozen;
        data.isSelfTarget = caster == target;
        return data;
    }

    private static TargetPreviewData CreateFinalDiff(CombatActor caster, CombatActor source, CombatActor clone)
    {
        TargetPreviewData data = CreateBaseline(caster, source);
        data.previewHpAfter = Mathf.Max(0, clone.hp);
        data.previewGuardAfter = Mathf.Max(0, clone.guardPool);
        data.hpLost = data.currentHp - data.previewHpAfter;
        data.guardLost = data.currentGuard - data.previewGuardAfter;

        StatusController status = clone.status;
        data.previewBurnAfter = status != null ? Mathf.Max(0, status.burnStacks) : 0;
        data.previewBleedAfter = status != null ? Mathf.Max(0, status.bleedStacks) : 0;
        data.previewMarkedAfter = status != null && status.marked;
        data.previewFrozenAfter = status != null && status.frozen;
        bool finalStaggered = status != null && status.staggered;
        data.willBreakGuard = !data.currentlyStaggered && finalStaggered;
        data.willConsumeStagger = data.currentlyStaggered && !finalStaggered;
        data.valid = true;
        return data;
    }

    private static CombatActor CloneActorForPreview(CombatActor source)
    {
        GameObject go = new GameObject($"PreviewClone_{source.name}");
        go.hideFlags = HideFlags.HideAndDontSave;
        CombatActor clone = go.AddComponent<CombatActor>();
        clone.team = source.team;
        clone.row = source.row;
        clone.isPlayer = source.isPlayer;
        clone.maxHP = source.maxHP;
        clone.hp = source.hp;
        clone.maxFocus = source.maxFocus;
        clone.focus = source.focus;
        clone.startingFocus = source.startingFocus;
        clone.guardPool = source.guardPool;

        if (source.status != null)
        {
            StatusController status = go.AddComponent<StatusController>();
            CopyStatus(source.status, status);
            clone.status = status;
        }

        return clone;
    }

    private static void CopyStatus(StatusController source, StatusController destination)
    {
        if (source == null || destination == null)
            return;

        destination.burnStacks = source.burnStacks;
        destination.burnTurns = source.burnTurns;
        CopyBurnBatches(source, destination);
        destination.marked = source.marked;
        destination.bleedStacks = source.bleedStacks;
        destination.frozen = source.frozen;
        destination.chilledTurns = source.chilledTurns;
        destination.staggered = source.staggered;
        destination.emberWeaponTurns = source.emberWeaponTurns;
        destination.emberWeaponBonusDamage = source.emberWeaponBonusDamage;
        destination.emberWeaponBurnEqualsDamage = source.emberWeaponBurnEqualsDamage;
        destination.emberWeaponBurnOnCritOnly = source.emberWeaponBurnOnCritOnly;
        destination.cinderbrandTurns = source.cinderbrandTurns;
        destination.cinderbrandBonusPerBurn = source.cinderbrandBonusPerBurn;
    }

    private static void CopyBurnBatches(StatusController source, StatusController destination)
    {
        List<StatusController.BurnBatchState> sourceBatches = source.GetBurnBatches();
        List<StatusController.BurnBatchState> destinationBatches = destination.GetBurnBatches();
        destinationBatches.Clear();

        for (int i = 0; i < sourceBatches.Count; i++)
        {
            StatusController.BurnBatchState batch = sourceBatches[i];
            if (batch == null)
                continue;

            destinationBatches.Add(new StatusController.BurnBatchState
            {
                stacks = batch.stacks,
                turnsRemaining = batch.turnsRemaining
            });
        }
    }

    private readonly struct ActorSnapshot
    {
        private readonly int _hp;
        private readonly int _focus;
        private readonly int _guard;
        private readonly StatusSnapshot _status;

        private ActorSnapshot(CombatActor actor)
        {
            _hp = actor != null ? actor.hp : 0;
            _focus = actor != null ? actor.focus : 0;
            _guard = actor != null ? actor.guardPool : 0;
            _status = StatusSnapshot.Capture(actor != null ? actor.status : null);
        }

        public static ActorSnapshot Capture(CombatActor actor) => new ActorSnapshot(actor);

        public void Restore(CombatActor actor)
        {
            if (actor == null)
                return;

            actor.hp = _hp;
            actor.focus = _focus;
            actor.guardPool = _guard;
            _status.Restore(actor.status);
        }
    }

    private readonly struct StatusSnapshot
    {
        private readonly struct BurnBatchSnapshot
        {
            public readonly int stacks;
            public readonly int turnsRemaining;

            public BurnBatchSnapshot(int stacks, int turnsRemaining)
            {
                this.stacks = stacks;
                this.turnsRemaining = turnsRemaining;
            }
        }

        private readonly bool _hasStatus;
        private readonly int _burnStacks;
        private readonly int _burnTurns;
        private readonly BurnBatchSnapshot[] _burnBatches;
        private readonly bool _marked;
        private readonly int _bleedStacks;
        private readonly bool _frozen;
        private readonly int _chilledTurns;
        private readonly bool _staggered;

        private StatusSnapshot(StatusController status)
        {
            _hasStatus = status != null;
            _burnStacks = status != null ? status.burnStacks : 0;
            _burnTurns = status != null ? status.burnTurns : 0;
            _burnBatches = CaptureBurnBatches(status);
            _marked = status != null && status.marked;
            _bleedStacks = status != null ? status.bleedStacks : 0;
            _frozen = status != null && status.frozen;
            _chilledTurns = status != null ? status.chilledTurns : 0;
            _staggered = status != null && status.staggered;
        }

        public static StatusSnapshot Capture(StatusController status) => new StatusSnapshot(status);

        public void Restore(StatusController status)
        {
            if (!_hasStatus || status == null)
                return;

            status.burnStacks = _burnStacks;
            status.burnTurns = _burnTurns;
            RestoreBurnBatches(status, _burnBatches);
            status.marked = _marked;
            status.bleedStacks = _bleedStacks;
            status.frozen = _frozen;
            status.chilledTurns = _chilledTurns;
            status.staggered = _staggered;
        }

        private static BurnBatchSnapshot[] CaptureBurnBatches(StatusController status)
        {
            if (status == null)
                return null;

            List<StatusController.BurnBatchState> burnBatches = status.GetBurnBatches();
            if (burnBatches == null || burnBatches.Count == 0)
                return System.Array.Empty<BurnBatchSnapshot>();

            var snapshot = new BurnBatchSnapshot[burnBatches.Count];
            for (int i = 0; i < burnBatches.Count; i++)
            {
                StatusController.BurnBatchState batch = burnBatches[i];
                snapshot[i] = batch == null
                    ? new BurnBatchSnapshot(0, 0)
                    : new BurnBatchSnapshot(batch.stacks, batch.turnsRemaining);
            }

            return snapshot;
        }

        private static void RestoreBurnBatches(StatusController status, BurnBatchSnapshot[] burnBatches)
        {
            List<StatusController.BurnBatchState> destination = status.GetBurnBatches();
            destination.Clear();

            if (burnBatches == null)
                return;

            for (int i = 0; i < burnBatches.Length; i++)
            {
                BurnBatchSnapshot batch = burnBatches[i];
                if (batch.stacks <= 0 || batch.turnsRemaining <= 0)
                    continue;

                destination.Add(new StatusController.BurnBatchState
                {
                    stacks = batch.stacks,
                    turnsRemaining = batch.turnsRemaining
                });
            }
        }
    }
}

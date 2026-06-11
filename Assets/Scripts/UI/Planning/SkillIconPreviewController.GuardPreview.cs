using System.Collections.Generic;
using UnityEngine;

internal sealed partial class SkillIconPreviewController
{
    private static int ResolveTargetPreviewDieValue(SkillRuntime runtime, int fallbackDieValue, int guardLocalIndex)
    {
        if (runtime == null || runtime.kind != SkillKind.Guard || runtime.guardValueMode != BaseEffectValueMode.X)
            return fallbackDieValue;

        int guardValue = SkillBehaviorRuntimeUtility.GetPerDieResolvedOutput(runtime, Mathf.Max(0, guardLocalIndex));
        return guardValue > 0 ? guardValue : fallbackDieValue;
    }

    private bool TryBuildSelfGuardFinalPreview(
        SkillRuntime runtime,
        SkillDamageSO sourceSkill,
        CombatActor hoveredActor,
        int dieValue,
        int guardLocalIndex,
        int resolveCount,
        int diceEnchantGuardGain,
        out TargetPreviewData data)
    {
        data = default;
        if (runtime == null || _turn == null || _turn.player == null || hoveredActor != _turn.player || runtime.kind != SkillKind.Guard)
            return false;

        CombatActor player = _turn.player;
        int skillGuardGain = ResolveSelfGuardGain(runtime, sourceSkill, player, dieValue, guardLocalIndex);
        if (resolveCount > 1)
            skillGuardGain *= resolveCount;

        int totalGuardGain = Mathf.Max(0, skillGuardGain) + Mathf.Max(0, diceEnchantGuardGain);
        if (totalGuardGain <= 0)
            return false;

        data = new TargetPreviewData
        {
            valid = true,
            currentHp = player.hp,
            currentMaxHp = player.maxHP,
            currentGuard = player.guardPool,
            previewHpAfter = player.hp,
            previewGuardAfter = player.guardPool + totalGuardGain,
            currentlyStaggered = player.status != null && player.status.staggered,
            currentBurn = player.status != null ? player.status.burnStacks : 0,
            currentBleed = player.status != null ? player.status.bleedStacks : 0,
            currentMarked = player.status != null && player.status.marked,
            currentFrozen = player.status != null && player.status.frozen,
            previewBurnAfter = player.status != null ? player.status.burnStacks : 0,
            previewBleedAfter = player.status != null ? player.status.bleedStacks : 0,
            previewMarkedAfter = player.status != null && player.status.marked,
            previewFrozenAfter = player.status != null && player.status.frozen,
            isSelfTarget = true,
            selfGuardGain = totalGuardGain
        };
        return true;
    }

    private static int ResolveSelfGuardGain(SkillRuntime runtime, SkillDamageSO sourceSkill, CombatActor caster, int dieValue, int guardLocalIndex)
    {
        if (runtime == null || runtime.kind != SkillKind.Guard)
            return 0;

        if (sourceSkill == null)
            sourceSkill = SkillGameplayResolver.GetSourceSkill(runtime);
        if (SkillGameplayResolver.CanResolveWithNewPipeline(sourceSkill))
        {
            SkillResolvedResult resolved = SkillGameplayResolver.Resolve(
                sourceSkill,
                runtime,
                caster,
                caster,
                SkillGameplayResolver.BuildConditionContext(runtime, caster, caster));
            if (resolved == null || !resolved.canCast || resolved.effects == null)
                return 0;

            int resolvedGuard = 0;
            for (int i = 0; i < resolved.effects.Count; i++)
            {
                ResolvedEffect effect = resolved.effects[i];
                if (effect == null || effect.sameActionFollowUp || effect.type != SkillEffectType.GainGuard)
                    continue;
                CombatActor target = effect.targetActor != null ? effect.targetActor : caster;
                if (target == caster)
                    resolvedGuard += Mathf.Max(0, effect.value);
            }

            return Mathf.Max(0, resolvedGuard);
        }

        int baseGuard;
        if (runtime.guardValueMode == BaseEffectValueMode.Flat && runtime.guardFlat > 0)
            baseGuard = SkillOutputValueUtility.AddActionAddedValue(runtime.guardFlat, runtime);
        else if (runtime.guardValueMode == BaseEffectValueMode.X)
            baseGuard = ResolveTargetPreviewDieValue(runtime, dieValue, guardLocalIndex);
        else
            baseGuard = runtime.CalculateGuard(dieValue);

        PassiveSystem passiveSystem = caster != null ? caster.GetComponent<PassiveSystem>() : null;
        float pct = passiveSystem != null ? passiveSystem.GetGuardGainPercent() : 0f;
        float multiplier = 1f + Mathf.Max(-0.99f, pct);
        return Mathf.Max(0, Mathf.FloorToInt(baseGuard * multiplier));
    }

    private void ShowActionPreviewBundle(TargetPreviewBuilder.ActionPreviewBundle bundle)
    {
        if (_cachedActorWorldUis == null || bundle.targetPreviews == null)
            return;

        foreach (KeyValuePair<CombatActor, TargetPreviewData> kvp in bundle.targetPreviews)
        {
            if (kvp.Key == null || !kvp.Value.valid)
                continue;

            foreach (ActorWorldUI ui in _cachedActorWorldUis)
            {
                if (ui != null && ui.actor == kvp.Key)
                {
                    ui.ShowTargetPreview(kvp.Value);
                    break;
                }
            }
        }

        CombatHUD hud = GetCachedHud();
        ScriptableObject asset = _getSkillAsset();
        if (hud != null && _turn != null && _turn.player != null && asset != null &&
            SkillUiMetadataUtility.TryGetSkillCosts(asset, out int focusCost, out _))
        {
            hud.ShowFocusPreview(focusCost, Mathf.Max(0, bundle.totalSelfFocusGain + _simpleEnchantPreview.focusGain), _turn.player.focus < focusCost);
        }
    }

    private DiceCombatEnchantRuntimeUtility.SimpleEnchantPreview ComputeSimpleEnchantPreview(int slotsRequired)
    {
        if (_turn == null || _turn.diceRig == null || _turn.player == null || !_turn.diceRig.HasRolledThisTurn)
            return default;

        ScriptableObject asset = _getSkillAsset();
        return asset != null && _turn.TryGetPrototypeSkillSimpleEnchantPreview(asset, slotsRequired, out var preview)
            ? preview
            : default;
    }

    private void ShowSelfEnchantGuardPreview()
    {
        if (_turn == null || _turn.player == null || _simpleEnchantPreview.guardGain <= 0)
            return;

        ActorWorldUI playerUi = FindActorWorldUi(_turn.player);
        if (playerUi == null)
            return;

        CombatActor player = _turn.player;
        TargetPreviewData data = new TargetPreviewData
        {
            valid = true,
            currentHp = player.hp,
            currentMaxHp = player.maxHP,
            currentGuard = player.guardPool,
            previewHpAfter = player.hp,
            previewGuardAfter = player.guardPool + _simpleEnchantPreview.guardGain,
            currentlyStaggered = player.status != null && player.status.staggered,
            currentBurn = player.status != null ? player.status.burnStacks : 0,
            currentBleed = player.status != null ? player.status.bleedStacks : 0,
            currentMarked = player.status != null && player.status.marked,
            currentFrozen = player.status != null && player.status.frozen,
            previewBurnAfter = player.status != null ? player.status.burnStacks : 0,
            previewBleedAfter = player.status != null ? player.status.bleedStacks : 0,
            previewMarkedAfter = player.status != null && player.status.marked,
            previewFrozenAfter = player.status != null && player.status.frozen,
            isSelfTarget = true,
            selfGuardGain = _simpleEnchantPreview.guardGain
        };

        playerUi.ShowTargetPreview(data);
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TargetClickable2D : MonoBehaviour, IPointerClickHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public TurnManager turn;
    private CombatActor _actor;
    private ActorWorldUI _worldUI;

    void Awake()
    {
        _actor = GetComponent<CombatActor>();
        if (_actor == null) _actor = GetComponentInParent<CombatActor>();
        if (turn == null)
            turn = FindObjectOfType<TurnManager>(true);
        _worldUI = GetComponentInParent<ActorWorldUI>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        DraggableSkillIcon selected = UiDragState.SelectedSkill;
        if (selected != null && _actor != null && turn != null)
        {
            ScriptableObject asset = selected.GetSkillAsset();
            if (asset != null)
            {
                bool accepted = turn.TryCastDraggedSkillToTarget(asset, _actor);
                if (accepted)
                {
                    UiDragState.DeselectSkill();
                    return;
                }

                return;
            }
        }

        if (_actor)
        {
            ConsumableBarUIManager consumableUi = FindObjectOfType<ConsumableBarUIManager>(true);
            if (consumableUi != null && consumableUi.TryHandleTargetClick(_actor))
                return;
        }

        if (!turn || !_actor)
            return;

        turn.OnTargetClicked(_actor);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!turn || eventData == null || eventData.pointerDrag == null)
            return;

        DraggableSkillIcon drag = eventData.pointerDrag.GetComponent<DraggableSkillIcon>();
        if (drag == null)
            return;

        ScriptableObject asset = drag.GetSkillAsset();
        if (asset == null)
            return;

        bool accepted = _actor != null && turn.TryCastDraggedSkillToTarget(asset, _actor);
        if (accepted)
            drag.NotifyDropAccepted();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_actor == null || turn == null)
            return;

        DraggableSkillIcon skillSource = null;
        if (UiDragState.IsDragging && eventData.pointerDrag != null)
            skillSource = eventData.pointerDrag.GetComponent<DraggableSkillIcon>();
        if (skillSource == null)
            skillSource = UiDragState.SelectedSkill;
        if (skillSource == null)
            return;

        SkillRuntime rt = GetSelectedRuntime(skillSource);
        if (rt == null)
            return;

        if (!TurnManagerTargetingUtility.IsValidTargetForPendingSkill(rt, _actor, turn.player, turn.party, turn.enemy))
            return;

        TargetingArrowUI.SetWorldTarget(_actor.transform);

        TurnManager.PreviewPaymentPlan previewPlan = default;
        ScriptableObject selectedAsset = skillSource.GetSkillAsset();
        bool hasPreviewPlan = turn != null && turn.TryGetPrototypePreviewPaymentPlan(selectedAsset, out previewPlan);
        if (hasPreviewPlan && previewPlan.runtime != null)
            rt = previewPlan.runtime;

        int rawDieValue = hasPreviewPlan ? previewPlan.resolvedDieValue : skillSource.GetPublicPreviewDieValue(rt);
        int guardLocalIndex = hasPreviewPlan ? Mathf.Clamp(previewPlan.anchor0 - previewPlan.start0, 0, 2) : 0;
        int dieValue = ResolveTargetPreviewDieValue(rt, rawDieValue, guardLocalIndex);
        int resolveCount = hasPreviewPlan ? Mathf.Max(1, previewPlan.repeatCount + 1) : 1;
        SkillDamageSO selectedDamageSkill = selectedAsset as SkillDamageSO;
        SkillDamageSO sourceSkill = selectedDamageSkill != null ? selectedDamageSkill : SkillGameplayResolver.GetSourceSkill(rt);
        TargetPreviewBuilder.ActionPreviewBundle bundle =
            TargetPreviewBuilder.BuildActionBundle(rt, turn.player, _actor, dieValue, turn.party, turn.enemy, resolveCount, sourceSkill);
        if (!SkillGameplayResolver.CanResolveWithNewPipeline(sourceSkill) && hasPreviewPlan && previewPlan.repeatCount > 0)
            TargetPreviewBuilder.ApplyRepeatPreviewMultiplier(ref bundle, previewPlan.repeatCount + 1);

        DiceCombatEnchantRuntimeUtility.SimpleEnchantPreview simplePreview = default;
        if (selectedAsset != null && SkillUiMetadataUtility.TryGetSkillCosts(selectedAsset, out _, out int slotsRequired))
            simplePreview = GetSimpleEnchantPreview(selectedAsset, rt, slotsRequired);

        if (TryBuildSelfGuardFinalPreview(rt, sourceSkill, dieValue, guardLocalIndex, resolveCount, simplePreview.guardGain, out TargetPreviewData selfGuardPreview))
        {
            bundle.targetPreviews[turn.player] = selfGuardPreview;
            bundle.valid = true;
        }
        else
        {
            TargetPreviewBuilder.AddSelfResourcePreview(turn.player, simplePreview.guardGain, 0, ref bundle);
        }

        if (!bundle.valid)
            return;

        ActorWorldUI[] allUIs = FindObjectsOfType<ActorWorldUI>(true);
        ClearAllPreviews(allUIs);
        ShowBundlePreviews(bundle, allUIs);
        ShowHudResourcePreview(skillSource.GetSkillAsset(), rt, bundle.totalSelfFocusGain);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TargetingArrowUI.ClearWorldTarget();

        ActorWorldUI[] allUIs = FindObjectsOfType<ActorWorldUI>(true);
        ClearAllPreviews(allUIs);

        ScriptableObject asset = null;
        DraggableSkillIcon previewSource = null;
        if (UiDragState.IsDragging && eventData != null && eventData.pointerDrag != null)
        {
            DraggableSkillIcon drag = eventData.pointerDrag.GetComponent<DraggableSkillIcon>();
            if (drag != null)
            {
                previewSource = drag;
                asset = drag.GetSkillAsset();
            }
        }

        if (asset == null && UiDragState.SelectedSkill != null)
        {
            previewSource = UiDragState.SelectedSkill;
            asset = UiDragState.SelectedSkill.GetSkillAsset();
        }

        if (previewSource != null && asset != null)
            previewSource.ShowResourcePreview(asset);
        else
            RestoreHudResourceBaseline(asset);
    }

    private ActorWorldUI GetWorldUI()
    {
        if (_worldUI != null)
            return _worldUI;

        ActorWorldUI[] allUIs = FindObjectsOfType<ActorWorldUI>(true);
        return FindUIForActor(_actor, allUIs);
    }

    private SkillRuntime GetSelectedRuntime(DraggableSkillIcon selected)
    {
        ScriptableObject asset = selected.GetSkillAsset();
        if (asset == null || asset is SkillPassiveSO)
            return null;

        SkillRuntime rt = null;
        if (turn != null)
            turn.TryGetPrototypeSkillTooltipRuntime(asset, out rt);

        if (rt == null)
        {
            if (asset is SkillDamageSO ds) rt = SkillRuntime.FromDamage(ds);
            else if (asset is SkillBuffDebuffSO bds) rt = SkillRuntime.FromBuffDebuff(bds);
        }

        return rt;
    }

    private ActorWorldUI FindUIForActor(CombatActor targetActor, ActorWorldUI[] allUIs)
    {
        if (targetActor == null || allUIs == null)
            return null;

        for (int i = 0; i < allUIs.Length; i++)
        {
            if (allUIs[i] != null && allUIs[i].actor == targetActor)
                return allUIs[i];
        }

        return null;
    }

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
        int dieValue,
        int guardLocalIndex,
        int resolveCount,
        int diceEnchantGuardGain,
        out TargetPreviewData data)
    {
        data = default;
        if (runtime == null || turn == null || turn.player == null || _actor != turn.player || runtime.kind != SkillKind.Guard)
            return false;

        CombatActor player = turn.player;
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

    

    private void ShowBundlePreviews(TargetPreviewBuilder.ActionPreviewBundle bundle, ActorWorldUI[] allUIs)
    {
        if (bundle.targetPreviews == null || allUIs == null)
            return;

        foreach (KeyValuePair<CombatActor, TargetPreviewData> kvp in bundle.targetPreviews)
        {
            if (kvp.Key == null || !kvp.Value.valid)
                continue;

            ActorWorldUI ui = FindUIForActor(kvp.Key, allUIs);
            if (ui != null)
                ui.ShowTargetPreview(kvp.Value);
        }
    }

    private static void ClearAllPreviews(ActorWorldUI[] allUIs)
    {
        if (allUIs == null)
            return;

        for (int i = 0; i < allUIs.Length; i++)
        {
            if (allUIs[i] != null)
                allUIs[i].ClearTargetPreview();
        }
    }

    private void ShowHudResourcePreview(ScriptableObject asset, SkillRuntime runtime, int focusGain)
    {
        if (turn == null || turn.player == null || asset == null)
            return;
        if (!SkillUiMetadataUtility.TryGetSkillCosts(asset, out int focusCost, out int slotsRequired))
            return;

        CombatHUD hud = FindObjectOfType<CombatHUD>(true);
        if (hud == null)
            return;

        DiceCombatEnchantRuntimeUtility.SimpleEnchantPreview enchantPreview = GetSimpleEnchantPreview(asset, runtime, slotsRequired);
        bool isInvalid = turn.player.focus < focusCost;
        hud.ShowFocusPreview(focusCost, Mathf.Max(0, focusGain + enchantPreview.focusGain), isInvalid);
    }

    private void RestoreHudResourceBaseline(ScriptableObject asset)
    {
        CombatHUD hud = FindObjectOfType<CombatHUD>(true);
        if (hud == null)
            return;

        if (turn == null || turn.player == null || asset == null || !SkillUiMetadataUtility.TryGetSkillCosts(asset, out int focusCost, out int slotsRequired))
        {
            hud.ClearFocusPreview();
            return;
        }

        SkillRuntime runtime = GetRuntimeForAsset(asset);
        DiceCombatEnchantRuntimeUtility.SimpleEnchantPreview enchantPreview = GetSimpleEnchantPreview(asset, runtime, slotsRequired);
        bool isInvalid = turn.player.focus < focusCost;
        hud.ShowFocusPreview(focusCost, enchantPreview.focusGain, isInvalid);
    }

    private SkillRuntime GetRuntimeForAsset(ScriptableObject asset)
    {
        if (asset == null || asset is SkillPassiveSO)
            return null;

        SkillRuntime runtime = null;
        if (turn != null)
            turn.TryGetPrototypeSkillTooltipRuntime(asset, out runtime);

        if (runtime == null)
        {
            if (asset is SkillDamageSO damage) runtime = SkillRuntime.FromDamage(damage);
            else if (asset is SkillBuffDebuffSO buff) runtime = SkillRuntime.FromBuffDebuff(buff);
        }

        return runtime;
    }

    private DiceCombatEnchantRuntimeUtility.SimpleEnchantPreview GetSimpleEnchantPreview(
        ScriptableObject asset,
        SkillRuntime runtime,
        int slotsRequired)
    {
        if (turn == null || asset == null)
            return default;

        int slotCost = runtime != null ? runtime.slotsRequired : slotsRequired;
        return turn.TryGetPrototypeSkillSimpleEnchantPreview(asset, Mathf.Clamp(slotCost, 1, 3), out var preview)
            ? preview
            : default;
    }
}

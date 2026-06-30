using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TargetClickable2D : MonoBehaviour, IPointerClickHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public TurnManager turn;
    private CombatActor _actor;
    private CombatHUD _hud;

    void Awake()
    {
        _actor = GetComponent<CombatActor>();
        if (_actor == null) _actor = GetComponentInParent<CombatActor>();
        if (turn == null)
            turn = TurnManagerRegistry.Get();
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

                selected.RejectSelectedTargetFeedback();
                UiDragState.DeselectSkill();
                return;
            }
        }

        if (_actor)
        {
            ConsumableBarUIManager consumableUi = ConsumableBarUiManagerRegistry.Get();
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
        int dieValue = CombatGuardPreviewUtility.ResolveGuardPreviewDieValue(rt, rawDieValue, guardLocalIndex);
        int repeatPreviewCount = (hasPreviewPlan ? Mathf.Max(0, previewPlan.repeatCount) : 0) + GetReadyStatusRepeatCount(turn.player);
        SkillDamageSO selectedDamageSkill = selectedAsset as SkillDamageSO;
        SkillDamageSO sourceSkill = selectedDamageSkill != null ? selectedDamageSkill : SkillGameplayResolver.GetSourceSkill(rt);

        DiceCombatEnchantRuntimeUtility.SimpleEnchantPreview simplePreview = default;
        if (selectedAsset != null && SkillUiMetadataUtility.TryGetSkillCosts(selectedAsset, out _, out int slotsRequired))
            simplePreview = GetSimpleEnchantPreview(selectedAsset, rt, slotsRequired);

        TargetPreviewBuilder.ActionPreviewBundle bundle =
            CombatPreviewBundleUtility.BuildActionBundleWithSelfGuard(
                rt,
                sourceSkill,
                turn.player,
                _actor,
                turn.player,
                turn.party,
                turn.enemy,
                dieValue,
                guardLocalIndex,
                repeatPreviewCount,
                simplePreview.guardGain);

        if (!bundle.valid)
            return;

        ActorWorldUI[] allUIs = ActorWorldUiRegistry.GetAllSnapshot();
        ClearAllPreviews(allUIs);
        CombatTargetPreviewPresenter.ShowBundle(bundle, allUIs, GetHud());
        ShowHudResourcePreview(skillSource.GetSkillAsset(), rt, bundle.totalSelfFocusGain);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TargetingArrowUI.ClearWorldTarget();

        ActorWorldUI[] allUIs = ActorWorldUiRegistry.GetAllSnapshot();
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

    private static int GetReadyStatusRepeatCount(CombatActor actor)
        => actor != null && actor.status != null ? actor.status.PeekRepeatFirstSkillReady() : 0;

    

    private void ClearAllPreviews(ActorWorldUI[] allUIs)
    {
        CombatTargetPreviewPresenter.ClearAll(allUIs, GetHud());
    }

    private void ShowHudResourcePreview(ScriptableObject asset, SkillRuntime runtime, int focusGain)
    {
        if (turn == null || turn.player == null || asset == null)
            return;
        if (!SkillUiMetadataUtility.TryGetSkillCosts(asset, out int focusCost, out int slotsRequired))
            return;

        CombatHUD hud = GetHud();
        if (hud == null)
            return;

        DiceCombatEnchantRuntimeUtility.SimpleEnchantPreview enchantPreview = GetSimpleEnchantPreview(asset, runtime, slotsRequired);
        bool isInvalid = turn.player.focus < focusCost;
        hud.ShowFocusPreview(focusCost, Mathf.Max(0, focusGain + enchantPreview.focusGain), isInvalid);
    }

    private void RestoreHudResourceBaseline(ScriptableObject asset)
    {
        CombatHUD hud = GetHud();
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

    private CombatHUD GetHud()
    {
        if (_hud == null)
            _hud = CombatHudRegistry.Get();
        return _hud;
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

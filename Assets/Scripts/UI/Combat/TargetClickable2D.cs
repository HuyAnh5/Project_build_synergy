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

        int dieValue = skillSource.GetPublicPreviewDieValue(rt);
        TargetPreviewBuilder.ActionPreviewBundle bundle =
            TargetPreviewBuilder.BuildActionBundle(rt, turn.player, _actor, dieValue, turn.party, turn.enemy);
        if (!bundle.valid)
            return;

        ActorWorldUI[] allUIs = FindObjectsOfType<ActorWorldUI>(true);
        ClearAllPreviews(allUIs);
        ShowBundlePreviews(bundle, allUIs);
        ShowHudFocusPreview(skillSource.GetSkillAsset(), bundle.totalSelfFocusGain);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TargetingArrowUI.ClearWorldTarget();

        ActorWorldUI[] allUIs = FindObjectsOfType<ActorWorldUI>(true);
        ClearAllPreviews(allUIs);

        ScriptableObject asset = null;
        if (UiDragState.IsDragging && eventData != null && eventData.pointerDrag != null)
        {
            DraggableSkillIcon drag = eventData.pointerDrag.GetComponent<DraggableSkillIcon>();
            if (drag != null)
                asset = drag.GetSkillAsset();
        }

        if (asset == null && UiDragState.SelectedSkill != null)
            asset = UiDragState.SelectedSkill.GetSkillAsset();

        RestoreHudFocusBaseline(asset);
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

    private void ShowHudFocusPreview(ScriptableObject asset, int focusGain)
    {
        if (turn == null || turn.player == null || asset == null)
            return;
        if (!SkillUiMetadataUtility.TryGetSkillCosts(asset, out int focusCost, out _))
            return;

        CombatHUD hud = FindObjectOfType<CombatHUD>(true);
        if (hud == null)
            return;

        bool isInvalid = turn.player.focus < focusCost;
        hud.ShowFocusPreview(focusCost, Mathf.Max(0, focusGain), isInvalid);
    }

    private void RestoreHudFocusBaseline(ScriptableObject asset)
    {
        CombatHUD hud = FindObjectOfType<CombatHUD>(true);
        if (hud == null)
            return;

        if (turn == null || turn.player == null || asset == null || !SkillUiMetadataUtility.TryGetSkillCosts(asset, out int focusCost, out _))
        {
            hud.ClearFocusPreview();
            return;
        }

        bool isInvalid = turn.player.focus < focusCost;
        hud.ShowFocusPreview(focusCost, 0, isInvalid);
    }
}

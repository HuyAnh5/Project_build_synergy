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
        // Nếu đang có skill được click-to-select → cast vào target này
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
                else
                {
                    // Target không hợp lệ — vẫn giữ selected, shake feedback nhẹ
                    return;
                }
            }
        }

        // Consumable click handler
        if (_actor)
        {
            ConsumableBarUIManager consumableUi = FindObjectOfType<ConsumableBarUIManager>(true);
            if (consumableUi != null && consumableUi.TryHandleTargetClick(_actor))
                return;
        }

        if (!turn || !_actor) return;
        turn.OnTargetClicked(_actor);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!turn) return;
        if (eventData == null || eventData.pointerDrag == null) return;

        DraggableSkillIcon drag = eventData.pointerDrag.GetComponent<DraggableSkillIcon>();
        if (drag == null) return;

        ScriptableObject asset = drag.GetSkillAsset();
        if (asset == null) return;

        bool accepted = false;
        if (_actor != null)
            accepted = turn.TryCastDraggedSkillToTarget(asset, _actor);

        if (accepted)
            drag.NotifyDropAccepted();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_actor == null || turn == null) return;

        DraggableSkillIcon skillSource = null;

        // Ưu tiên: Đang drag skill vào target này
        if (UiDragState.IsDragging && eventData.pointerDrag != null)
        {
            skillSource = eventData.pointerDrag.GetComponent<DraggableSkillIcon>();
        }

        // Fallback: Click-to-select
        if (skillSource == null)
            skillSource = UiDragState.SelectedSkill;

        if (skillSource == null) return;

        SkillRuntime rt = GetSelectedRuntime(skillSource);
        if (rt == null) return;

        // Kiểm tra target chính có hợp lệ không
        if (!TurnManagerTargetingUtility.IsValidTargetForPendingSkill(rt, _actor, turn.player, turn.party, turn.enemy))
            return;

        int dieValue = skillSource.GetPublicPreviewDieValue(rt);

        // --- Hỗ trợ AoE / Multi-target preview ---
        IReadOnlyList<CombatActor> aoeTargets = TurnManagerCombatUtility.ResolveAoeTargets(rt, turn.player, _actor, turn.party, turn.enemy);
        
        if (aoeTargets != null && aoeTargets.Count > 0)
        {
            // Tối ưu: Lấy danh sách UI một lần
            ActorWorldUI[] allUIs = FindObjectsOfType<ActorWorldUI>(true);

            // Hiển thị preview cho TẤT CẢ mục tiêu trong vùng ảnh hưởng
            foreach (CombatActor targetActor in aoeTargets)
            {
                ActorWorldUI targetUI = FindUIForActor(targetActor, allUIs);
                if (targetUI != null)
                {
                    TargetPreviewData preview = TargetPreviewBuilder.Build(rt, turn.player, targetActor, dieValue);
                    if (preview.valid)
                        targetUI.ShowTargetPreview(preview);
                }
            }
        }
        else
        {
            // Single target preview (default)
            ActorWorldUI ui = GetWorldUI();
            if (ui != null)
            {
                TargetPreviewData preview = TargetPreviewBuilder.Build(rt, turn.player, _actor, dieValue);
                if (preview.valid)
                    ui.ShowTargetPreview(preview);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Clear preview cho toàn bộ UI trong scene để đảm bảo không bị sót preview AoE
        ActorWorldUI[] allUIs = FindObjectsOfType<ActorWorldUI>(true);
        foreach (ActorWorldUI ui in allUIs)
        {
            ui.ClearTargetPreview();
        }
    }

    private ActorWorldUI GetWorldUI()
    {
        if (_worldUI != null) return _worldUI;
        // Fallback: tìm UI khớp actor
        ActorWorldUI[] allUIs = FindObjectsOfType<ActorWorldUI>(true);
        return FindUIForActor(_actor, allUIs);
    }

    private SkillRuntime GetSelectedRuntime(DraggableSkillIcon selected)
    {
        ScriptableObject asset = selected.GetSkillAsset();
        if (asset == null || asset is SkillPassiveSO) return null;

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
        if (targetActor == null || allUIs == null) return null;
        
        for (int i = 0; i < allUIs.Length; i++)
        {
            if (allUIs[i].actor == targetActor) return allUIs[i];
        }
        return null;
    }
}

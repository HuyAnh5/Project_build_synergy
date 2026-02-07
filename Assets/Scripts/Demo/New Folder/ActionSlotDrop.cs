using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ActionSlotDrop : MonoBehaviour, IDropHandler
{
    public TurnManager turn;
    public int slotIndex = 1; // 1..3

    [Header("UI")]
    public Image iconPreview;

    private void Awake()
    {
        if (turn) turn.RegisterDrop(this);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!turn || !turn.IsPlanning) return;

        var drag = eventData.pointerDrag;
        if (!drag) return;

        var d = drag.GetComponent<DraggableSkillIcon>();
        if (!d || d.skill == null) return;

        bool ok = turn.TryAssignSkillToSlot(slotIndex, d.skill);
        if (!ok) return;

        transform.DOKill();
        transform.DOPunchScale(Vector3.one * 0.12f, 0.18f, 8, 0.8f);
    }

    public void SetPreview(SkillSO skill)
    {
        if (!iconPreview) return;
        iconPreview.sprite = (skill != null) ? skill.icon : null;
        iconPreview.enabled = (iconPreview.sprite != null);
        iconPreview.raycastTarget = true;
    }

    public void ClearPreview()
    {
        if (!iconPreview) return;
        iconPreview.sprite = null;
        iconPreview.enabled = false;
        iconPreview.raycastTarget = false;
    }
}

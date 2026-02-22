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
        if (!d) return;

        // Reject passive (equip-only)
        if (d.IsPassive) return;

        bool ok = false;

        // ✅ NEW: read from unified API instead of fields
        var asset = d.GetSkillAsset();
        if (asset == null) return;

        if (asset is SkillDamageSO dmg) ok = turn.TryAssignSkillToSlot(slotIndex, dmg);
        else if (asset is SkillBuffDebuffSO bd) ok = turn.TryAssignSkillToSlot(slotIndex, bd);
        else if (asset is SkillSO legacy) ok = turn.TryAssignSkillToSlot(slotIndex, legacy);

        if (!ok) return;

        transform.DOKill();
        transform.DOPunchScale(Vector3.one * 0.12f, 0.18f, 8, 0.8f);
    }

    // ----------------------
    // Preview
    // ----------------------

    // Legacy entry point
    public void SetPreview(SkillSO skill)
    {
        SetPreview((ScriptableObject)skill);
    }

    // New entry point: accepts SkillSO / SkillDamageSO / SkillBuffDebuffSO
    public void SetPreview(ScriptableObject skillAsset)
    {
        if (!iconPreview) return;

        Sprite sp = null;
        switch (skillAsset)
        {
            case SkillSO s: sp = s.icon; break;
            case SkillDamageSO d: sp = d.icon; break;
            case SkillBuffDebuffSO b: sp = b.icon; break;
            default: sp = null; break;
        }

        iconPreview.sprite = sp;
        iconPreview.enabled = (sp != null);
        iconPreview.raycastTarget = (sp != null);

        if (sp != null) iconPreview.preserveAspect = true;
    }

    public void ClearPreview()
    {
        if (!iconPreview) return;
        iconPreview.sprite = null;
        iconPreview.enabled = false;
        iconPreview.raycastTarget = false;
    }
}

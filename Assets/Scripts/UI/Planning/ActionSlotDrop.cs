using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ActionSlotDrop : MonoBehaviour, IDropHandler
{
    public TurnManager turn;
    public int slotIndex = 1; // 1..3
    public int HomeSlotIndex { get; private set; } = 1;

    [Header("UI")]
    public Image iconPreview;

    private Transform _previewHomeParent;
    private Canvas _previewCanvas;
    private Vector3 _homeWorldPosition;
    private bool _hasHomeWorldPosition;

    private void Awake()
    {
        HomeSlotIndex = Mathf.Clamp(slotIndex, 1, 3);
        if (iconPreview != null)
        {
            _previewHomeParent = iconPreview.transform.parent;
            _previewCanvas = iconPreview.GetComponentInParent<Canvas>();
        }

        CacheHomeWorldPosition();
        if (turn) turn.RegisterDrop(this);
    }

    public void SetVisualLaneIndex(int lane1Based)
    {
        slotIndex = Mathf.Clamp(lane1Based, 1, 3);
        if (turn) turn.RegisterDrop(this);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!turn || !turn.CanInteractWithSkills) return;

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

        if (!ok) return;

        transform.DOKill();
        transform.DOPunchScale(Vector3.one * 0.12f, 0.18f, 8, 0.8f);
    }

    // ----------------------
    // Preview
    // ----------------------

    // New entry point: accepts SkillDamageSO / SkillBuffDebuffSO
    public void SetPreview(ScriptableObject skillAsset)
    {
        if (!iconPreview) return;

        Sprite sp = null;
        switch (skillAsset)
        {
            case SkillDamageSO d: sp = d.icon; break;
            case SkillBuffDebuffSO b: sp = b.icon; break;
            default: sp = null; break;
        }

        iconPreview.sprite = sp;
        iconPreview.enabled = (sp != null);
        iconPreview.raycastTarget = (sp != null);

        if (sp != null) iconPreview.preserveAspect = true;
    }

    public void SetPreviewDetached(bool detached)
    {
        if (!iconPreview) return;

        if (_previewHomeParent == null)
            _previewHomeParent = iconPreview.transform.parent;
        if (_previewCanvas == null)
            _previewCanvas = iconPreview.GetComponentInParent<Canvas>();

        Transform targetParent = detached
            ? (_previewCanvas != null ? _previewCanvas.transform : _previewHomeParent)
            : _previewHomeParent;

        if (targetParent != null && iconPreview.transform.parent != targetParent)
            iconPreview.transform.SetParent(targetParent, true);
    }

    public Vector3 GetHomeWorldPosition()
    {
        if (!_hasHomeWorldPosition)
            CacheHomeWorldPosition();
        return _homeWorldPosition;
    }

    public void ClearPreview()
    {
        if (!iconPreview) return;
        SetPreviewDetached(false);
        iconPreview.sprite = null;
        iconPreview.enabled = false;
        iconPreview.raycastTarget = false;
    }

    private void CacheHomeWorldPosition()
    {
        _homeWorldPosition = ((RectTransform)transform).position;
        _hasHomeWorldPosition = true;
    }
}

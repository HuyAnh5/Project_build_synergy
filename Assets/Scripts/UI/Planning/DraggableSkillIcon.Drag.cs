using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class DraggableSkillIcon
{
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!turn || !turn.CanInteractWithSkills) return;

        var a = GetSkillAsset();
        if (a == null) return;
        if (a is SkillPassiveSO) return;
        
        if (!CanDragCurrentSkill())
        {
            RejectActionFeedback();
            return;
        }

        // Deselect click-to-select náº¿u Ä‘ang selected khi báº¯t Ä‘áº§u drag
        if (_selected)
            UiDragState.DeselectSkill();

        _dropAccepted = false;
        SkillTooltipUI.HideCurrent();
        ClearResourcePreview();
        ShowResourcePreview(a);
        UiDragState.BeginDrag(this);
        _dragRegistered = true;
        CreateGhost();
        MoveGhost(eventData.position);
        _cg.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_ghostRT != null)
            MoveGhost(eventData.position);

        // Target preview: detect actor under cursor
        if (_dragRegistered)
            UpdateTargetPreviewUnderCursor(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        ClearResourcePreview();
        _cg.blocksRaycasts = true;
        if (_dragRegistered)
        {
            UiDragState.EndDrag(this);
            _dragRegistered = false;
        }

        if (_ghostRT == null) return;

        if (!_dropAccepted &&
            eventData != null &&
            IsSelfTargetSkill(GetSkillAsset()) &&
            selfCastZone != null &&
            selfCastZone.ContainsScreenPoint(eventData.position, _uiCam))
        {
            _dropAccepted = turn != null && turn.TryCastDraggedSkillToSelf(GetSkillAsset());
        }

        if (_dropAccepted)
        {
            Destroy(_ghostRT.gameObject);
            _ghostRT = null;
            return;
        }

        _ghostRT.DOKill();
        _ghostRT.DOAnchorPos(_ghostHomeAnchoredPos, invalidDropReturnDuration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                if (_ghostRT != null)
                    Destroy(_ghostRT.gameObject);
                _ghostRT = null;
            });
    }

    public void NotifyDropAccepted()
    {
        _dropAccepted = true;
    }

    private void CreateGhost()
    {
        var go = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        go.transform.SetParent(_canvas.transform, false);
        go.transform.SetAsLastSibling();

        _ghostRT = (RectTransform)go.transform;
        _ghostRT.sizeDelta = ((RectTransform)transform).rect.size;
        _ghostRT.pivot = new Vector2(0.5f, 0.5f);
        _ghostRT.anchorMin = new Vector2(0.5f, 0.5f);
        _ghostRT.anchorMax = new Vector2(0.5f, 0.5f);

        RectTransform sourceRt = transform as RectTransform;
        if (sourceRt != null && _canvasRT != null)
        {
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(_uiCam, sourceRt.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRT, screenPos, _uiCam, out _ghostHomeAnchoredPos);
        }
        else
        {
            _ghostHomeAnchoredPos = Vector2.zero;
        }

        var img = go.GetComponent<Image>();
        img.sprite = _img ? _img.sprite : null;
        img.preserveAspect = true;
        img.raycastTarget = false;

        var cg = go.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.alpha = 0.9f;
    }

    private void MoveGhost(Vector2 screenPos)
    {
        if (_ghostRT == null || _canvasRT == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRT, screenPos, _uiCam, out var localPoint);

        _ghostRT.anchoredPosition = localPoint;
    }

    public void SetBindToInventory(RunInventoryManager inv, bool isFixed, int index)
    {
        bindToInventorySlot = true;
        inventory = inv;
        inventorySource = isFixed ? InventorySkillSource.Fixed : InventorySkillSource.Owned;
        inventoryIndex = index;
        Refresh();
    }

    private bool CanDragCurrentSkill()
    {
        if (turn == null) return false;
        ScriptableObject asset = GetSkillAsset();
        if (asset == null || asset is SkillPassiveSO) return false;
        return turn.CanPrototypeCastSkillNow(asset);
    }

    public bool IsSelfTargetSkillAsset()
        => IsSelfTargetSkill(GetSkillAsset());

    public static bool IsSelfTargetSkill(ScriptableObject asset)
    {
        switch (asset)
        {
            case SkillDamageSO damage:
                return damage.target == SkillTargetRule.Self;
            case SkillBuffDebuffSO buffDebuff:
                return buffDebuff.target == SkillTargetRule.Self;
            default:
                return false;
        }
    }
}

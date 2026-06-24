using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class ConsumableBarUIManager
{
    private void HideLegacyTooltips()
    {
        if (tooltipRoot != null)
            CombatUiDirtySetUtility.SetActiveIfChanged(tooltipRoot.gameObject, false);

        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i]?.localTooltipRoot != null)
                CombatUiDirtySetUtility.SetActiveIfChanged(slots[i].localTooltipRoot.gameObject, false);
        }
    }

    private void HideAllActionPanels()
    {
        if (actionPanelRoot != null)
            CombatUiDirtySetUtility.SetActiveIfChanged(actionPanelRoot.gameObject, false);

        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i]?.localActionPanelRoot != null)
                CombatUiDirtySetUtility.SetActiveIfChanged(slots[i].localActionPanelRoot.gameObject, false);
        }
    }

    private void HideAllTooltips()
    {
        SkillTooltipUI.HideCurrent();
        HideConsumableKeywordTooltips();
        HideLegacyTooltips();
    }

    private RectTransform GetActiveActionPanelRoot()
    {
        ConsumableSlotView slot = GetSlot(_selectedSlot);
        if (slot != null && slot.localActionPanelRoot != null && slot.localActionPanelRoot.gameObject.activeSelf)
            return slot.localActionPanelRoot;

        return actionPanelRoot != null && actionPanelRoot.gameObject.activeSelf ? actionPanelRoot : null;
    }

    private RectTransform GetActiveTooltipRoot()
    {
        ConsumableSlotView slot = GetSlot(_hoveredSlot);
        if (slot != null && slot.localTooltipRoot != null && slot.localTooltipRoot.gameObject.activeSelf)
            return slot.localTooltipRoot;

        return tooltipRoot != null && tooltipRoot.gameObject.activeSelf ? tooltipRoot : null;
    }

    private RectTransform GetSlotVisualTarget(ConsumableSlotView slot)
    {
        if (slot == null)
            return null;

        return slot.root;
    }

    private RectTransform GetSlotActionAnchor(ConsumableSlotView slot)
    {
        if (slot == null)
            return null;

        return slot.actionAnchor != null ? slot.actionAnchor : slot.root;
    }

    private RectTransform GetSlotTooltipAnchor(ConsumableSlotView slot)
    {
        if (slot == null)
            return null;

        return slot.tooltipAnchor != null ? slot.tooltipAnchor : slot.root;
    }

    private void RefreshRowLayout(bool instant)
    {
        RectTransform container = GetLayoutContainer();
        if (container == null || slots == null)
            return;

        int count = GetDisplayedConsumableCount();
        if (count <= 0)
            return;

        List<ConsumableSlotView> displayed = BuildDisplayedOrder(count);
        for (int i = 0; i < displayed.Count; i++)
        {
            ConsumableSlotView slot = displayed[i];
            if (slot == null || slot.root == null || slot.root == _dragRect)
                continue;

            SnapViewToAnchor(slot.root, container, GetPositionForIndex(i, displayed.Count), instant);
            slot.root.SetSiblingIndex(Mathf.Min(i, container.childCount - 1));
        }
    }

    private void RestoreBaseSlotSiblingOrder()
    {
        RectTransform container = GetLayoutContainer();
        if (container == null || slots == null)
            return;

        int count = runInventory != null ? runInventory.GetConsumableCount() : 0;
        if (count <= 0)
            return;

        List<ConsumableSlotView> displayed = BuildDisplayedOrder(count);
        int siblingIndex = 0;
        for (int i = 0; i < displayed.Count; i++)
        {
            ConsumableSlotView slot = displayed[i];
            if (slot == null || slot.root == null || slot.root == _dragRect || !slot.root.gameObject.activeSelf)
                continue;

            if (slot.root.parent != container)
                slot.root.SetParent(container, worldPositionStays: true);

            slot.root.SetSiblingIndex(Mathf.Min(siblingIndex, container.childCount - 1));
            siblingIndex++;
        }
    }

    private void BringSlotToFront(int index)
    {
        ConsumableSlotView slot = GetSlot(index);
        if (slot == null || slot.root == null || slot.root == _dragRect || !slot.root.gameObject.activeSelf)
            return;

        slot.root.SetAsLastSibling();
    }

    private List<ConsumableSlotView> BuildDisplayedOrder(int count)
    {
        List<ConsumableSlotView> displayed = new List<ConsumableSlotView>(count);
        bool usePreview = _dragSlot >= 0 && _dragSourceIndex >= 0 && _previewInsertIndex >= 0;
        if (!usePreview)
        {
            for (int i = 0; i < count && i < slots.Length; i++)
                displayed.Add(slots[i]);
            return displayed;
        }

        int insertIndex = Mathf.Clamp(_previewInsertIndex, 0, Mathf.Max(0, count - 1));
        ConsumableSlotView dragged = GetSlot(_dragSourceIndex);
        for (int i = 0; i < count && i < slots.Length; i++)
        {
            ConsumableSlotView current = slots[i];
            if (current == dragged)
                continue;

            if (displayed.Count == insertIndex)
                displayed.Add(dragged);

            displayed.Add(current);
        }

        if (!displayed.Contains(dragged))
            displayed.Add(dragged);

        return displayed;
    }

    private Vector2 GetPositionForIndex(int index, int count)
    {
        if (count <= 1)
            return Vector2.zero;

        float width = fallbackRowWidth;
        RectTransform container = GetLayoutContainer();
        if (container != null && container.rect.width > 1f)
            width = container.rect.width;

        float cardWidth = Mathf.Max(1f, cardSize.x);
        float fitSpacing = Mathf.Max(1f, (width - cardWidth) / Mathf.Max(1, count - 1));
        float spacing = Mathf.Min(relaxedSpacing, fitSpacing);
        if (fitSpacing >= minStackedSpacing)
            spacing = Mathf.Max(spacing, minStackedSpacing);

        float x = (index - (count - 1) / 2f) * spacing;
        return new Vector2(x, 0f);
    }

    private int GetInsertIndexFromScreenPosition(Vector2 screenPosition, Camera eventCamera)
    {
        int count = runInventory != null ? runInventory.GetConsumableCount() : 0;
        RectTransform container = GetLayoutContainer();
        if (container == null || count <= 1)
            return 0;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screenPosition, eventCamera, out Vector2 local))
            return Mathf.Clamp(_dragSourceIndex, 0, Mathf.Max(0, count - 1));

        for (int i = 0; i < count - 1; i++)
        {
            float midpoint = (GetPositionForIndex(i, count).x + GetPositionForIndex(i + 1, count).x) * 0.5f;
            if (local.x < midpoint)
                return i;
        }

        return count - 1;
    }

    private void MoveSlotView(int fromIndex, int insertIndex)
    {
        if (slots == null || fromIndex < 0 || fromIndex >= slots.Length)
            return;

        List<ConsumableSlotView> ordered = new List<ConsumableSlotView>(slots);
        ConsumableSlotView moving = ordered[fromIndex];
        ordered.RemoveAt(fromIndex);
        insertIndex = Mathf.Clamp(insertIndex, 0, ordered.Count);
        ordered.Insert(insertIndex, moving);
        slots = ordered.ToArray();

        for (int i = 0; i < slots.Length; i++)
            ConfigureSlotView(slots[i], i);
    }

    private void SnapViewToAnchor(RectTransform view, RectTransform parent, Vector2 anchoredPos, bool instant)
    {
        if (view == null || parent == null)
            return;

        if (view.parent != parent)
            view.SetParent(parent, worldPositionStays: true);

        view.anchorMin = view.anchorMax = new Vector2(0.5f, 0.5f);
        view.pivot = new Vector2(0.5f, 0.5f);
        view.sizeDelta = cardSize;

        view.DOKill();
        if (instant)
        {
            view.anchoredPosition = anchoredPos;
            view.localScale = Vector3.one;
            return;
        }

        view.DOAnchorPos(anchoredPos, Mathf.Max(0.01f, dragSnapDuration)).SetEase(dragMoveEase).SetUpdate(true);
        view.DOScale(1f, Mathf.Max(0.01f, dragSnapDuration)).SetEase(dragScaleEase).SetUpdate(true);
    }

    private RectTransform GetLayoutContainer()
    {
        if (layoutContainer == null)
        {
            ConsumableSlotView first = FindFirstUsableSlotView();
            if (first != null && first.root != null)
                layoutContainer = first.root.parent as RectTransform;
        }

        if (layoutContainer == null)
            layoutContainer = transform as RectTransform;

        return layoutContainer;
    }

    private ConsumableSlotView GetSlot(int index)
    {
        if (slots == null || index < 0 || index >= slots.Length)
            return null;

        return slots[index];
    }

    private void EnsureDragLayer()
    {
        if (dragLayer != null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform.parent;
        if (parent == null)
            return;

        Transform existing = parent.Find("ConsumableDragLayer");
        if (existing != null)
        {
            dragLayer = existing as RectTransform;
            return;
        }

        GameObject go = new GameObject("ConsumableDragLayer", typeof(RectTransform));
        dragLayer = go.GetComponent<RectTransform>();
        dragLayer.SetParent(parent, false);
        dragLayer.anchorMin = Vector2.zero;
        dragLayer.anchorMax = Vector2.one;
        dragLayer.offsetMin = Vector2.zero;
        dragLayer.offsetMax = Vector2.zero;
        dragLayer.SetAsLastSibling();
    }

    private void CacheDragPointerOffset(Vector2 screenPos, Camera eventCamera)
    {
        if (_dragRect == null || dragLayer == null)
        {
            _dragPointerOffset = Vector2.zero;
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, screenPos, eventCamera, out Vector2 local))
        {
            _dragPointerOffset = Vector2.zero;
            return;
        }

        _dragPointerOffset = local - _dragRect.anchoredPosition;
    }

    private void MoveGhostWithPointer(Vector2 screenPos, Camera eventCamera)
    {
        if (_dragRect == null || dragLayer == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, screenPos, eventCamera, out Vector2 local))
            return;

        _dragRect.anchoredPosition = local - _dragPointerOffset;
    }

    private void ClearDragGhost(bool instant)
    {
        _dragMoveTween?.Kill();
        _dragScaleTween?.Kill();
        _dragMoveTween = null;
        _dragScaleTween = null;

        if (_dragRect != null)
        {
            if (_dragGhostCanvasGroup != null)
            {
                _dragGhostCanvasGroup.blocksRaycasts = true;
                _dragGhostCanvasGroup.alpha = 1f;
            }

            if (_dragRect.parent != layoutContainer && layoutContainer != null)
                _dragRect.SetParent(layoutContainer, worldPositionStays: true);

            if (instant)
            {
                _dragRect.localScale = Vector3.one;
            }
            else
            {
                _dragRect.DOScale(1f, Mathf.Max(0.01f, dragSnapDuration)).SetEase(dragScaleEase).SetUpdate(true);
            }
        }

        _dragRect = null;
        _dragGhostCanvasGroup = null;
    }

    private void SetSlotCanvasAlpha(int index, float alpha)
    {
        ConsumableSlotView slot = GetSlot(index);
        if (slot == null || slot.root == null)
            return;

        CanvasGroup group = slot.root.GetComponent<CanvasGroup>();
        if (group == null)
            group = slot.root.gameObject.AddComponent<CanvasGroup>();

        group.alpha = alpha;
    }

}




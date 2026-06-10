using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class ConsumableBarUIManager
{
    private void HideAllActionPanels()
    {
        if (actionPanelRoot != null)
            actionPanelRoot.gameObject.SetActive(false);

        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i]?.localActionPanelRoot != null)
                slots[i].localActionPanelRoot.gameObject.SetActive(false);
        }
    }

    private void HideAllTooltips()
    {
        if (tooltipRoot != null)
            tooltipRoot.gameObject.SetActive(false);

        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i]?.localTooltipRoot != null)
                slots[i].localTooltipRoot.gameObject.SetActive(false);
        }
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

    private void PositionPanelAtAnchor(RectTransform panel, RectTransform anchor, Vector2 offset)
    {
        PositionPresentationAtAnchor(panel, anchor, offset);
    }

    private void PositionTooltipAtAnchor(RectTransform tooltip, RectTransform anchor, Vector2 offset)
    {
        PositionPresentationAtAnchor(tooltip, anchor, offset);
    }

    private static void PositionPresentationAtAnchor(RectTransform presentation, RectTransform anchor, Vector2 offset)
    {
        if (presentation == null || anchor == null)
            return;

        RectTransform anchorParent = anchor.parent as RectTransform;
        if (anchorParent == null)
        {
            presentation.position = anchor.position + (Vector3)offset;
            return;
        }

        if (presentation.parent != anchorParent)
            presentation.SetParent(anchorParent, worldPositionStays: false);

        EnsureIgnoreLayout(presentation);
        presentation.anchorMin = anchor.anchorMin;
        presentation.anchorMax = anchor.anchorMax;
        presentation.anchoredPosition = anchor.anchoredPosition + offset;
        presentation.localScale = Vector3.one;
    }

    private static void EnsureIgnoreLayout(RectTransform rect)
    {
        if (rect == null)
            return;

        LayoutElement layoutElement = rect.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = rect.gameObject.AddComponent<LayoutElement>();

        layoutElement.ignoreLayout = true;
    }

    private static void EnsureTooltipAutoSize(RectTransform tooltip, TMP_Text title, TMP_Text body)
    {
        if (tooltip == null)
            return;

        VerticalLayoutGroup layout = tooltip.GetComponent<VerticalLayoutGroup>();
        RectOffset padding = layout != null ? layout.padding : new RectOffset();
        float spacing = layout != null ? layout.spacing : 0f;
        if (layout != null)
            layout.enabled = false;

        ContentSizeFitter fitter = tooltip.GetComponent<ContentSizeFitter>();
        if (fitter != null)
            fitter.enabled = false;

        EnsureTooltipTextLayout(title);
        EnsureTooltipTextLayout(body);
        float contentWidth = Mathf.Max(120f, tooltip.rect.width - padding.left - padding.right);
        float titleHeight = GetPreferredHeight(title, contentWidth);
        float bodyHeight = GetPreferredHeight(body, contentWidth);

        LayoutTooltipBlock(title, padding.left, padding.top, contentWidth, titleHeight);
        float bodyTop = padding.top + titleHeight + (title != null && title.gameObject.activeSelf && body != null && body.gameObject.activeSelf ? spacing : 0f);
        LayoutTooltipBlock(body, padding.left, bodyTop, contentWidth, bodyHeight);

        float totalHeight = padding.top + padding.bottom;
        if (title != null && title.gameObject.activeSelf)
            totalHeight += titleHeight;
        if (body != null && body.gameObject.activeSelf)
        {
            if (title != null && title.gameObject.activeSelf)
                totalHeight += spacing;
            totalHeight += bodyHeight;
        }

        tooltip.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Ceil(totalHeight));
    }

    private static void EnsureTooltipTextLayout(TMP_Text text)
    {
        if (text == null)
            return;

        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;

        LayoutElement layout = text.GetComponent<LayoutElement>();
        if (layout == null)
            layout = text.gameObject.AddComponent<LayoutElement>();

        layout.ignoreLayout = false;
        layout.flexibleHeight = 0f;
        layout.preferredHeight = -1f;
    }

    private static void LayoutTooltipBlock(TMP_Text text, float left, float top, float width, float height)
    {
        if (text == null)
            return;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(left, -top);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Ceil(height));
    }

    private static float GetPreferredHeight(TMP_Text text, float width)
    {
        if (text == null || !text.gameObject.activeSelf)
            return 0f;

        return text.GetPreferredValues(text.text ?? string.Empty, width, 0f).y;
    }

    private Vector2 GetPresentationOffset(bool isTooltip)
    {
        if (useExactManualAnchors)
            return isTooltip ? tooltipStableOffset : actionPanelStableOffset;

        return isTooltip ? tooltipOffset : actionPanelOffset;
    }

    private bool IsPointerOverTooltipPresentation(int slotIndex)
    {
        ConsumableSlotView slot = GetSlot(slotIndex);
        RectTransform source = GetSlotVisualTarget(slot);
        RectTransform tooltip = GetTooltipRootForSlot(slot);
        if (source == null || tooltip == null || !tooltip.gameObject.activeInHierarchy)
            return false;

        Canvas canvas = source.GetComponentInParent<Canvas>();
        Camera eventCamera = GetCanvasEventCamera(canvas);
        Vector2 screenPoint = Input.mousePosition;
        if (RectTransformUtility.RectangleContainsScreenPoint(source, screenPoint, eventCamera) ||
            RectTransformUtility.RectangleContainsScreenPoint(tooltip, screenPoint, eventCamera))
        {
            return true;
        }

        if (!TryBuildHoverBridgeScreenRect(source, tooltip, eventCamera, out Rect bridgeRect))
            return false;

        return bridgeRect.Contains(screenPoint);
    }

    private RectTransform GetTooltipRootForSlot(ConsumableSlotView slot)
    {
        if (slot != null && slot.localTooltipRoot != null)
            return slot.localTooltipRoot;

        return tooltipRoot;
    }

    private static bool TryBuildHoverBridgeScreenRect(RectTransform source, RectTransform tooltip, Camera eventCamera, out Rect bridgeRect)
    {
        bridgeRect = default;
        if (source == null || tooltip == null)
            return false;

        Rect sourceRect = GetScreenRect(source, eventCamera);
        Rect tooltipRect = GetScreenRect(tooltip, eventCamera);
        if (sourceRect.width <= 0f || sourceRect.height <= 0f ||
            tooltipRect.width <= 0f || tooltipRect.height <= 0f ||
            sourceRect.Overlaps(tooltipRect, true))
        {
            return false;
        }

        bool verticalGap = sourceRect.yMax < tooltipRect.yMin || tooltipRect.yMax < sourceRect.yMin;
        bool horizontalGap = sourceRect.xMax < tooltipRect.xMin || tooltipRect.xMax < sourceRect.xMin;

        if (verticalGap && !horizontalGap)
        {
            float xMin = Mathf.Max(sourceRect.xMin, tooltipRect.xMin);
            float xMax = Mathf.Min(sourceRect.xMax, tooltipRect.xMax);
            float yMin = Mathf.Min(sourceRect.yMax, tooltipRect.yMax);
            float yMax = Mathf.Max(sourceRect.yMin, tooltipRect.yMin);
            bridgeRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
        else if (horizontalGap && !verticalGap)
        {
            float xMin = Mathf.Min(sourceRect.xMax, tooltipRect.xMax);
            float xMax = Mathf.Max(sourceRect.xMin, tooltipRect.xMin);
            float yMin = Mathf.Max(sourceRect.yMin, tooltipRect.yMin);
            float yMax = Mathf.Min(sourceRect.yMax, tooltipRect.yMax);
            bridgeRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
        else
        {
            Vector2 sourcePoint = ClosestPointOnRect(sourceRect, tooltipRect.center);
            Vector2 tooltipPoint = ClosestPointOnRect(tooltipRect, sourcePoint);
            sourcePoint = ClosestPointOnRect(sourceRect, tooltipPoint);
            bridgeRect = Rect.MinMaxRect(
                Mathf.Min(sourcePoint.x, tooltipPoint.x),
                Mathf.Min(sourcePoint.y, tooltipPoint.y),
                Mathf.Max(sourcePoint.x, tooltipPoint.x),
                Mathf.Max(sourcePoint.y, tooltipPoint.y));
        }

        bridgeRect = ExpandThinRect(bridgeRect, 6f);
        return bridgeRect.width > 0f && bridgeRect.height > 0f;
    }

    private static Rect GetScreenRect(RectTransform rect, Camera camera)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 max = min;
        for (int i = 1; i < corners.Length; i++)
        {
            Vector2 screenCorner = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
            min = Vector2.Min(min, screenCorner);
            max = Vector2.Max(max, screenCorner);
        }

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private static Vector2 ClosestPointOnRect(Rect rect, Vector2 point)
    {
        return new Vector2(
            Mathf.Clamp(point.x, rect.xMin, rect.xMax),
            Mathf.Clamp(point.y, rect.yMin, rect.yMax));
    }

    private static Rect ExpandThinRect(Rect rect, float minThickness)
    {
        if (rect.width < minThickness)
        {
            float extra = (minThickness - rect.width) * 0.5f;
            rect.xMin -= extra;
            rect.xMax += extra;
        }

        if (rect.height < minThickness)
        {
            float extra = (minThickness - rect.height) * 0.5f;
            rect.yMin -= extra;
            rect.yMax += extra;
        }

        return rect;
    }

    private static Camera GetCanvasEventCamera(Canvas canvas)
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }
}




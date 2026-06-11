using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class ConsumableBarUIManager
{
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

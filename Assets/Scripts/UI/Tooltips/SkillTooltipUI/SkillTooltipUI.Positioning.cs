using UnityEngine;
using UnityEngine.UI;

// Owns screen placement and hover-bridge hit testing so tooltip persistence stays
// separate from content formatting.
public sealed partial class SkillTooltipUI
{
    private void PositionNear(RectTransform target)
    {
        if (_canvasRect == null || target == null)
            return;

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector3 topCenterWorld = (corners[1] + corners[2]) * 0.5f;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(_targetCamera, topCenterWorld);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, _uiCamera, out Vector2 anchorLocal))
            return;

        Vector2 size = _root.rect.size;
        Rect rect = _canvasRect.rect;
        Vector2 desired = anchorLocal + new Vector2(0f, TooltipVerticalOffset);

        float halfWidth = size.x * 0.5f;
        float minX = rect.xMin + halfWidth + TooltipHorizontalCanvasPadding;
        float maxX = rect.xMax - halfWidth - TooltipHorizontalCanvasPadding;
        float minY = rect.yMin + TooltipVerticalCanvasPadding;
        float maxY = rect.yMax - size.y - TooltipVerticalCanvasPadding;

        desired.x = Mathf.Clamp(desired.x, minX, maxX);
        desired.y = Mathf.Clamp(desired.y, minY, maxY);
        _root.anchoredPosition = desired;
    }

    private void EnsureHoverBridge()
    {
        if (_hoverBridge != null)
            return;

        Transform existing = transform.parent != null ? transform.parent.Find(TooltipHoverBridgeName) : null;
        if (existing != null)
            _hoverBridge = existing as RectTransform;

        if (_hoverBridge == null)
        {
            GameObject bridgeGo = new GameObject(TooltipHoverBridgeName, typeof(RectTransform), typeof(Image));
            _hoverBridge = bridgeGo.GetComponent<RectTransform>();
            if (_root != null && _root.parent != null)
                _hoverBridge.SetParent(_root.parent, false);
        }

        if (_hoverBridge == null)
            return;

        _hoverBridge.anchorMin = new Vector2(0.5f, 0.5f);
        _hoverBridge.anchorMax = new Vector2(0.5f, 0.5f);
        _hoverBridge.pivot = new Vector2(0.5f, 0.5f);

        _hoverBridgeImage = _hoverBridge.GetComponent<Image>();
        if (_hoverBridgeImage == null)
            _hoverBridgeImage = _hoverBridge.gameObject.AddComponent<Image>();

        _hoverBridgeImage.color = new Color(1f, 1f, 1f, 0f);
        _hoverBridgeImage.raycastTarget = false;
    }

    private void BindHoverBridgeToTargetCanvas(Canvas targetCanvas)
    {
        if (_hoverBridge == null || targetCanvas == null)
            return;

        Transform parent = targetCanvas.transform;
        if (_hoverBridge.parent != parent)
            _hoverBridge.SetParent(parent, false);

        _hoverBridgeCanvasRect = targetCanvas.transform as RectTransform;
        _hoverBridgeCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;
    }

    private void PositionHoverBridge(RectTransform target)
    {
        if (_hoverBridge == null || _currentTarget == null || _hoverBridgeCanvasRect == null)
            return;

        if (!TryGetHoverBridgeScreenRect(out Rect bridgeScreenRect))
        {
            _hoverBridge.gameObject.SetActive(false);
            return;
        }

        Vector2 bridgeMinScreen = new Vector2(bridgeScreenRect.xMin, bridgeScreenRect.yMin);
        Vector2 bridgeMaxScreen = new Vector2(bridgeScreenRect.xMax, bridgeScreenRect.yMax);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_hoverBridgeCanvasRect, bridgeMinScreen, _hoverBridgeCamera, out Vector2 bridgeMinLocal) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(_hoverBridgeCanvasRect, bridgeMaxScreen, _hoverBridgeCamera, out Vector2 bridgeMaxLocal))
            return;

        Vector2 localMin = Vector2.Min(bridgeMinLocal, bridgeMaxLocal);
        Vector2 localMax = Vector2.Max(bridgeMinLocal, bridgeMaxLocal);
        Vector2 localSize = localMax - localMin;
        Vector2 localCenter = (localMin + localMax) * 0.5f;

        _hoverBridge.SetAsLastSibling();
        _hoverBridge.anchoredPosition = localCenter;
        _hoverBridge.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, localSize.x);
        _hoverBridge.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, localSize.y);
        _hoverBridge.gameObject.SetActive(localSize.x > 0.01f && localSize.y > 0.01f);
    }

    private bool IsScreenPointInsideRect(RectTransform rect, Vector2 screenPoint, Camera camera)
    {
        return rect != null &&
               rect.gameObject.activeInHierarchy &&
               RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, camera);
    }

    private bool IsScreenPointInsideHoverBridgeZone(Vector2 screenPoint)
    {
        if (!TryGetHoverBridgeScreenRect(out Rect bridgeScreenRect))
            return false;

        return bridgeScreenRect.Contains(screenPoint);
    }

    private bool TryGetHoverBridgeScreenRect(out Rect bridgeScreenRect)
    {
        bridgeScreenRect = default;
        if (_currentTarget == null || _root == null || !_root.gameObject.activeInHierarchy)
            return false;

        Rect targetScreenRect = GetScreenRect(_currentTarget, _targetCamera);
        Rect tooltipScreenRect = GetScreenRect(_root, _uiCamera);
        if (targetScreenRect.width <= 0f || targetScreenRect.height <= 0f ||
            tooltipScreenRect.width <= 0f || tooltipScreenRect.height <= 0f)
        {
            return false;
        }

        if (targetScreenRect.Overlaps(tooltipScreenRect, true))
            return false;

        bool verticalGap = targetScreenRect.yMax < tooltipScreenRect.yMin || tooltipScreenRect.yMax < targetScreenRect.yMin;
        bool horizontalGap = targetScreenRect.xMax < tooltipScreenRect.xMin || tooltipScreenRect.xMax < targetScreenRect.xMin;

        if (verticalGap && !horizontalGap)
        {
            float xMin = Mathf.Max(targetScreenRect.xMin, tooltipScreenRect.xMin);
            float xMax = Mathf.Min(targetScreenRect.xMax, tooltipScreenRect.xMax);
            float yMin = Mathf.Min(targetScreenRect.yMax, tooltipScreenRect.yMax);
            float yMax = Mathf.Max(targetScreenRect.yMin, tooltipScreenRect.yMin);
            bridgeScreenRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
        else if (horizontalGap && !verticalGap)
        {
            float xMin = Mathf.Min(targetScreenRect.xMax, tooltipScreenRect.xMax);
            float xMax = Mathf.Max(targetScreenRect.xMin, tooltipScreenRect.xMin);
            float yMin = Mathf.Max(targetScreenRect.yMin, tooltipScreenRect.yMin);
            float yMax = Mathf.Min(targetScreenRect.yMax, tooltipScreenRect.yMax);
            bridgeScreenRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
        else
        {
            Vector2 targetPoint = ClosestPointOnRect(targetScreenRect, tooltipScreenRect.center);
            Vector2 tooltipPoint = ClosestPointOnRect(tooltipScreenRect, targetPoint);
            targetPoint = ClosestPointOnRect(targetScreenRect, tooltipPoint);
            bridgeScreenRect = Rect.MinMaxRect(
                Mathf.Min(targetPoint.x, tooltipPoint.x),
                Mathf.Min(targetPoint.y, tooltipPoint.y),
                Mathf.Max(targetPoint.x, tooltipPoint.x),
                Mathf.Max(targetPoint.y, tooltipPoint.y));
        }

        bridgeScreenRect = ExpandThinRect(bridgeScreenRect, 6f);
        return bridgeScreenRect.width > 0f && bridgeScreenRect.height > 0f;
    }

    private static Rect GetScreenRect(RectTransform rect, Camera camera)
    {
        if (rect == null)
            return default;

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
        float width = rect.width;
        float height = rect.height;
        if (width < minThickness)
        {
            float extra = (minThickness - width) * 0.5f;
            rect.xMin -= extra;
            rect.xMax += extra;
        }

        if (height < minThickness)
        {
            float extra = (minThickness - height) * 0.5f;
            rect.yMin -= extra;
            rect.yMax += extra;
        }

        return rect;
    }
}

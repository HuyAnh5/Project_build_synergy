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
        _hoverBridge.pivot = new Vector2(0.5f, 0f);

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

        Vector3[] targetCorners = new Vector3[4];
        _currentTarget.GetWorldCorners(targetCorners);
        Vector3 targetTopCenterWorld = (targetCorners[1] + targetCorners[2]) * 0.5f;
        Vector2 targetTopScreen = RectTransformUtility.WorldToScreenPoint(_targetCamera, targetTopCenterWorld);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_hoverBridgeCanvasRect, targetTopScreen, _hoverBridgeCamera, out Vector2 targetTopLocal))
            return;

        Vector3 tooltipBottomCenterWorld = _root.TransformPoint(new Vector3(0f, 0f, 0f));
        Vector2 tooltipBottomScreen = RectTransformUtility.WorldToScreenPoint(_uiCamera, tooltipBottomCenterWorld);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_hoverBridgeCanvasRect, tooltipBottomScreen, _hoverBridgeCamera, out Vector2 tooltipBottomLocal))
            return;

        float width = _currentTarget.rect.width;
        float height = Mathf.Max(0f, tooltipBottomLocal.y - targetTopLocal.y);

        _hoverBridge.SetAsLastSibling();
        _hoverBridge.anchoredPosition = targetTopLocal;
        _hoverBridge.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        _hoverBridge.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    private bool IsScreenPointInsideRect(RectTransform rect, Vector2 screenPoint, Camera camera)
    {
        return rect != null &&
               rect.gameObject.activeInHierarchy &&
               RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, camera);
    }

    private bool IsScreenPointInsideHoverBridgeZone(Vector2 screenPoint)
    {
        if (_currentTarget == null || _root == null || !_root.gameObject.activeInHierarchy)
            return false;

        Vector3[] targetCorners = new Vector3[4];
        _currentTarget.GetWorldCorners(targetCorners);

        Vector2 targetBottomLeft = RectTransformUtility.WorldToScreenPoint(_targetCamera, targetCorners[0]);
        Vector2 targetTopLeft = RectTransformUtility.WorldToScreenPoint(_targetCamera, targetCorners[1]);
        Vector2 targetTopRight = RectTransformUtility.WorldToScreenPoint(_targetCamera, targetCorners[2]);

        Vector3 tooltipBottomCenterWorld = _root.TransformPoint(Vector3.zero);
        Vector2 tooltipBottomCenter = RectTransformUtility.WorldToScreenPoint(_uiCamera, tooltipBottomCenterWorld);

        float minX = Mathf.Min(targetTopLeft.x, targetTopRight.x);
        float maxX = Mathf.Max(targetTopLeft.x, targetTopRight.x);
        float minY = Mathf.Min(targetTopLeft.y, tooltipBottomCenter.y);
        float maxY = Mathf.Max(targetTopLeft.y, tooltipBottomCenter.y);

        if (maxY <= minY)
            return false;

        bool insideBridgeColumn = screenPoint.x >= minX && screenPoint.x <= maxX;
        bool insideBridgeHeight = screenPoint.y >= minY && screenPoint.y <= maxY;
        if (insideBridgeColumn && insideBridgeHeight)
            return true;

        bool stillOnIcon = screenPoint.x >= Mathf.Min(targetBottomLeft.x, targetTopRight.x) &&
                           screenPoint.x <= Mathf.Max(targetBottomLeft.x, targetTopRight.x) &&
                           screenPoint.y >= Mathf.Min(targetBottomLeft.y, targetTopLeft.y) &&
                           screenPoint.y <= Mathf.Max(targetBottomLeft.y, targetTopLeft.y);

        return stillOnIcon;
    }
}

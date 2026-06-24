using UnityEngine;

public partial class DiceSpinnerGeneric : ISkillTooltipSource
{
    private void UpdateWorldEnchantTooltip()
    {
        if (_previewSandboxMode)
        {
            ClearWorldEnchantTooltip();
            return;
        }

        Camera cam = Camera.main;
        if (cam == null || UiDragState.IsDragging || !ValidateFaces())
        {
            ClearWorldEnchantTooltip();
            return;
        }

        if (!TryResolveCurrentWorldTooltipFace(cam, out int logicalFaceIndex, out Rect hoverRect) ||
            logicalFaceIndex < 0 ||
            logicalFaceIndex >= faces.Length)
        {
            ClearWorldEnchantTooltip();
            return;
        }

        DiceFace face = faces[logicalFaceIndex];
        DiceFaceEnchantKind displayedEnchant = GetDisplayedFaceEnchant(logicalFaceIndex);
        bool showBrokenTooltip = face.broken && iconLibrary != null && iconLibrary.TryGetBrokenFaceIcon(out _, out _);
        if ((!showBrokenTooltip && face.broken) || (!face.broken && !DiceFaceEnchantUtility.HasEnchant(displayedEnchant)))
        {
            ClearWorldEnchantTooltip();
            return;
        }

        if (!TryUpdateWorldTooltipAnchor(hoverRect))
        {
            ClearWorldEnchantTooltip();
            return;
        }

        EnsureWorldTooltipAsset();
        _worldTooltipAsset.Configure(displayedEnchant, face.value, name, face.broken);
        _worldTooltipFaceIndex = logicalFaceIndex;
        SkillTooltipUI.Show(this);
    }

    public bool TryResolveHoveredEnchantFace(Camera cam, out int logicalFaceIndex, out Rect hoverRect)
    {
        return TryResolveWorldTooltipFace(cam, out logicalFaceIndex, out hoverRect);
    }

    public bool TryGetFaceEnchantScreenRect(Camera cam, int faceIndex, out Rect screenRect)
    {
        screenRect = default;
        if (!ValidateFaces() || faceIndex < 0 || faceIndex >= faces.Length)
            return false;

        DiceFace face = faces[faceIndex];
        DiceFaceEnchantKind displayedEnchant = GetDisplayedFaceEnchant(faceIndex);
        bool showBrokenTooltip = face.broken && iconLibrary != null && iconLibrary.TryGetBrokenFaceIcon(out _, out _);
        if ((!showBrokenTooltip && face.broken) || (!face.broken && !DiceFaceEnchantUtility.HasEnchant(displayedEnchant)))
            return false;

        return TryBuildFaceEnchantIconScreenRect(face, cam, out screenRect, padding: 2f);
    }

    private bool TryResolveCurrentWorldTooltipFace(Camera cam, out int logicalFaceIndex, out Rect hoverRect)
    {
        logicalFaceIndex = -1;
        hoverRect = default;

        int currentFaceIndex = LastFaceIndex;
        if (currentFaceIndex < 0 || currentFaceIndex >= faces.Length)
            return false;

        DiceFace face = faces[currentFaceIndex];
        DiceFaceEnchantKind displayedEnchant = GetDisplayedFaceEnchant(currentFaceIndex);
        bool showBrokenTooltip = face.broken && iconLibrary != null && iconLibrary.TryGetBrokenFaceIcon(out _, out _);
        if ((!showBrokenTooltip && face.broken) || (!face.broken && !DiceFaceEnchantUtility.HasEnchant(displayedEnchant)))
            return false;

        if (!TryBuildFaceEnchantIconScreenRect(face, cam, out Rect screenRect, padding: 2f))
            return false;

        if (!screenRect.Contains(Input.mousePosition))
            return false;

        logicalFaceIndex = currentFaceIndex;
        hoverRect = screenRect;
        return true;
    }

    private bool TryResolveWorldTooltipFace(Camera cam, out int logicalFaceIndex, out Rect hoverRect)
    {
        logicalFaceIndex = -1;
        hoverRect = default;
        Vector2 pointer = Input.mousePosition;

        for (int i = 0; i < faces.Length; i++)
        {
            DiceFace face = faces[i];
            DiceFaceEnchantKind displayedEnchant = GetDisplayedFaceEnchant(i);
            bool showBrokenTooltip = face.broken && iconLibrary != null && iconLibrary.TryGetBrokenFaceIcon(out _, out _);
            if ((!showBrokenTooltip && face.broken) || (!face.broken && !DiceFaceEnchantUtility.HasEnchant(displayedEnchant)))
                continue;

            if (!TryBuildFaceEnchantIconScreenRect(face, cam, out Rect screenRect, padding: 2f))
                continue;

            if (!screenRect.Contains(pointer))
                continue;

            logicalFaceIndex = i;
            hoverRect = screenRect;
            return true;
        }

        return false;
    }

    

    private bool TryBuildFaceEnchantIconScreenRect(DiceFace face, Camera cam, out Rect screenRect, float padding)
    {
        return TryBuildRendererScreenRect(face.faceIconSpriteRenderer, cam, out screenRect, padding);
    }

    

    private bool TryBuildRendererScreenRect(Renderer renderer, Camera cam, out Rect screenRect, float padding)
    {
        screenRect = default;
        if (renderer == null || cam == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
            return false;

        Bounds bounds = renderer.bounds;
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;
        bool hasPoint = false;
        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 worldCorner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                    Vector3 screenPoint = cam.WorldToScreenPoint(worldCorner);
                    if (screenPoint.z <= 0f)
                        continue;

                    hasPoint = true;
                    minX = Mathf.Min(minX, screenPoint.x);
                    minY = Mathf.Min(minY, screenPoint.y);
                    maxX = Mathf.Max(maxX, screenPoint.x);
                    maxY = Mathf.Max(maxY, screenPoint.y);
                }
            }
        }

        if (!hasPoint)
            return false;

        screenRect = Rect.MinMaxRect(minX - padding, minY - padding, maxX + padding, maxY + padding);
        return screenRect.width > 0f && screenRect.height > 0f;
    }

    private void ClearWorldEnchantTooltip()
    {
        _worldTooltipFaceIndex = -1;
        if (SkillTooltipUI.IsCurrentSource(this))
            SkillTooltipUI.HideCurrentUnlessPointerOverTooltip();
    }

    public bool TryGetSkillTooltip(out Canvas canvas, out RectTransform target, out ScriptableObject asset, out SkillRuntime runtime)
    {
        canvas = ResolveWorldTooltipCanvas();
        target = _worldTooltipAnchor;
        asset = _worldTooltipAsset;
        runtime = null;
        return canvas != null && target != null && asset != null && _worldTooltipFaceIndex >= 0;
    }

    private Canvas ResolveWorldTooltipCanvas()
    {
        if (_worldTooltipCanvas != null)
            return _worldTooltipCanvas;

        Canvas sourceCanvas = null;
        if (DiceDraggableUI.TryGetRegisteredDiceUi(this, out DiceDraggableUI registeredUi) && registeredUi != null)
            sourceCanvas = registeredUi.GetComponentInParent<Canvas>();

        _worldTooltipCanvas = SkillTooltipUI.GetOrCreateSharedOverlayCanvas(sourceCanvas);
        return _worldTooltipCanvas;
    }

    private void EnsureWorldTooltipAsset()
    {
        if (_worldTooltipAsset != null)
            return;

        _worldTooltipAsset = ScriptableObject.CreateInstance<DiceFaceEnchantTooltipAsset>();
        _worldTooltipAsset.hideFlags = HideFlags.HideAndDontSave;
    }

    private bool TryUpdateWorldTooltipAnchor(Rect screenRect)
    {
        Canvas canvas = ResolveWorldTooltipCanvas();
        if (canvas == null || screenRect.width <= 0f || screenRect.height <= 0f)
            return false;

        EnsureWorldTooltipAnchor(canvas);
        if (_worldTooltipAnchor == null)
            return false;

        RectTransform canvasRect = canvas.transform as RectTransform;
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (canvasRect == null ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenRect.center, eventCamera, out Vector2 localPoint))
        {
            return false;
        }

        _worldTooltipAnchor.SetParent(canvasRect, false);
        _worldTooltipAnchor.anchorMin = new Vector2(0.5f, 0.5f);
        _worldTooltipAnchor.anchorMax = new Vector2(0.5f, 0.5f);
        _worldTooltipAnchor.pivot = new Vector2(0.5f, 0.5f);
        _worldTooltipAnchor.anchoredPosition = localPoint + new Vector2(0f, CombatWorldTooltipExtraGap * 0.5f);
        _worldTooltipAnchor.sizeDelta = BuildWorldTooltipHoverSize(screenRect, canvasRect, eventCamera) +
                                        new Vector2(0f, CombatWorldTooltipExtraGap);
        return true;
    }

    private Vector2 BuildWorldTooltipHoverSize(
        Rect screenRect,
        RectTransform canvasRect,
        Camera eventCamera)
    {
        Vector2 min = screenRect.min;
        Vector2 max = screenRect.max;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, min, eventCamera, out Vector2 localMin) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, max, eventCamera, out Vector2 localMax))
        {
            return new Vector2(96f, 96f);
        }

        float width = Mathf.Abs(localMax.x - localMin.x);
        float height = Mathf.Abs(localMax.y - localMin.y);
        return new Vector2(Mathf.Max(96f, width), Mathf.Max(96f, height));
    }

    private void EnsureWorldTooltipAnchor(Canvas canvas)
    {
        if (_worldTooltipAnchor != null)
            return;

        GameObject anchorGo = new GameObject($"{name}_WorldEnchantTooltipAnchor", typeof(RectTransform));
        anchorGo.hideFlags = HideFlags.HideAndDontSave;
        _worldTooltipAnchor = anchorGo.GetComponent<RectTransform>();
        _worldTooltipAnchor.SetParent(canvas.transform, false);
    }
}

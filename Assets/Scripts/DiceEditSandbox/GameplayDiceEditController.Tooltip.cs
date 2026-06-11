using UnityEngine;

public partial class GameplayDiceEditController
{
    private void RefreshFaceEnchantTooltip(Camera cam, bool pointerOverUi)
    {
        if (!IsPanelOpen || _activeInteractable == null || cam == null || _activeDragInteractable != null)
        {
            ClearFaceEnchantTooltip();
            return;
        }

        if (pointerOverUi && !IsPointerOverActiveInspectDice(cam))
        {
            ClearFaceEnchantTooltip();
            return;
        }

        if (!_activeInteractable.TryResolveHoveredLogicalFace(cam, Input.mousePosition, out int logicalFaceIndex))
        {
            ClearFaceEnchantTooltip();
            return;
        }

        DiceSpinnerGeneric spinner = _activeInteractable.Spinner;
        DiceFace face = spinner != null ? spinner.GetFace(logicalFaceIndex) : default;
        DiceFaceEnchantKind displayedEnchant = spinner != null
            ? spinner.GetDisplayedFaceEnchant(logicalFaceIndex)
            : DiceFaceEnchantKind.None;
        if (spinner == null ||
            face.broken ||
            !DiceFaceEnchantUtility.HasEnchant(displayedEnchant) ||
            face.faceIconSpriteRenderer == null ||
            !face.faceIconSpriteRenderer.enabled)
        {
            ClearFaceEnchantTooltip();
            return;
        }

        if (!TryUpdateTooltipAnchor(face.faceIconSpriteRenderer, cam))
        {
            ClearFaceEnchantTooltip();
            return;
        }

        EnsureHoverTooltipAsset();
        _hoverTooltipAsset.Configure(displayedEnchant, face.value, spinner.name);

        _hoverTooltipInteractable = _activeInteractable;
        _hoverTooltipFaceIndex = logicalFaceIndex;
        SkillTooltipUI.Show(this);
    }

    private bool IsPointerOverActiveInspectDice(Camera cam)
    {
        return _activeInteractable != null &&
               cam != null &&
               _activeInteractable.TryResolveHoveredLogicalFace(cam, Input.mousePosition, out _);
    }

    private void ClearFaceEnchantTooltip()
    {
        _hoverTooltipInteractable = null;
        _hoverTooltipFaceIndex = -1;
        SkillTooltipUI.HideCurrentUnlessPointerOverTooltip();
    }

    private void ForceClearFaceEnchantTooltip()
    {
        _hoverTooltipInteractable = null;
        _hoverTooltipFaceIndex = -1;
        SkillTooltipUI.HideCurrent();
    }

    public bool TryGetSkillTooltip(out Canvas canvas, out RectTransform target, out ScriptableObject asset, out SkillRuntime runtime)
    {
        canvas = ResolveTooltipCanvas();
        target = _tooltipAnchor;
        asset = _hoverTooltipAsset;
        runtime = null;
        return canvas != null && target != null && asset != null && _hoverTooltipFaceIndex >= 0;
    }

    private bool TryUpdateTooltipAnchor(SpriteRenderer iconRenderer, Camera cam)
    {
        Canvas canvas = ResolveTooltipCanvas();
        if (canvas == null || iconRenderer == null)
            return false;

        EnsureTooltipAnchor(canvas);
        if (_tooltipAnchor == null)
            return false;

        Vector3 screenPoint = cam.WorldToScreenPoint(iconRenderer.bounds.center);
        if (screenPoint.z <= 0f)
            return false;

        RectTransform canvasRect = canvas.transform as RectTransform;
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (canvasRect == null ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, eventCamera, out Vector2 localPoint))
        {
            return false;
        }

        _tooltipAnchor.SetParent(canvasRect, false);
        _tooltipAnchor.anchorMin = new Vector2(0.5f, 0.5f);
        _tooltipAnchor.anchorMax = new Vector2(0.5f, 0.5f);
        _tooltipAnchor.pivot = new Vector2(0.5f, 0.5f);
        _tooltipAnchor.anchoredPosition = localPoint;
        _tooltipAnchor.sizeDelta = BuildTooltipHoverSize(iconRenderer, cam, canvasRect, eventCamera);
        return true;
    }

    private Vector2 BuildTooltipHoverSize(
        SpriteRenderer iconRenderer,
        Camera cam,
        RectTransform canvasRect,
        Camera eventCamera)
    {
        Bounds bounds = iconRenderer.bounds;
        Vector3 min = cam.WorldToScreenPoint(bounds.min);
        Vector3 max = cam.WorldToScreenPoint(bounds.max);

        if (min.z <= 0f || max.z <= 0f)
            return new Vector2(180f, 180f);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, min, eventCamera, out Vector2 localMin) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, max, eventCamera, out Vector2 localMax))
        {
            return new Vector2(180f, 180f);
        }

        float width = Mathf.Abs(localMax.x - localMin.x) + 160f;
        float height = Mathf.Abs(localMax.y - localMin.y) + 160f;
        return new Vector2(Mathf.Max(180f, width), Mathf.Max(180f, height));
    }

    private Canvas ResolveTooltipCanvas()
    {
        if (_tooltipCanvas != null)
            return _tooltipCanvas;

        if (panelUi != null)
            _tooltipCanvas = panelUi.GetComponentInParent<Canvas>();

        if (_tooltipCanvas == null && consumableBarUi != null)
            _tooltipCanvas = consumableBarUi.GetComponentInParent<Canvas>();

        if (_tooltipCanvas == null)
            _tooltipCanvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);

        return _tooltipCanvas;
    }

    private void EnsureTooltipAnchor(Canvas canvas)
    {
        if (_tooltipAnchor != null)
            return;

        GameObject anchorGo = new GameObject("DiceFaceEnchantTooltipAnchor", typeof(RectTransform));
        anchorGo.hideFlags = HideFlags.HideAndDontSave;
        _tooltipAnchor = anchorGo.GetComponent<RectTransform>();
        _tooltipAnchor.SetParent(canvas.transform, false);
        _tooltipAnchor.sizeDelta = Vector2.zero;
    }

    private void EnsureHoverTooltipAsset()
    {
        if (_hoverTooltipAsset != null)
            return;

        _hoverTooltipAsset = ScriptableObject.CreateInstance<DiceFaceEnchantTooltipAsset>();
        _hoverTooltipAsset.hideFlags = HideFlags.HideAndDontSave;
    }
}

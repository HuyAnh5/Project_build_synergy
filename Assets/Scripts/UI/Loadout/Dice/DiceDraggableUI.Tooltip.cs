using UnityEngine;

public partial class DiceDraggableUI
{
    private void RefreshHoverTooltip()
    {
        if (!IsHoverTooltipActive || dice == null || UiDragState.IsDragging)
        {
            SkillTooltipUI.HideCurrentUnlessPointerOverTooltip(gameObject);
            return;
        }

        int faceIndex = ResolveTooltipEnchantFaceIndex();

        if (faceIndex < 0)
        {
            SkillTooltipUI.HideCurrentUnlessPointerOverTooltip(gameObject);
            return;
        }

        DiceFace face = dice.GetFace(faceIndex);
        DiceFaceEnchantKind displayedEnchant = dice.GetDisplayedFaceEnchant(faceIndex);
        bool showBrokenTooltip = face.broken;
        if ((!showBrokenTooltip && face.broken) || (!face.broken && !DiceFaceEnchantUtility.HasEnchant(displayedEnchant)))
        {
            SkillTooltipUI.HideCurrentUnlessPointerOverTooltip(gameObject);
            return;
        }

        if (_hoverTooltipAsset == null)
        {
            _hoverTooltipAsset = ScriptableObject.CreateInstance<DiceFaceEnchantTooltipAsset>();
            _hoverTooltipAsset.hideFlags = HideFlags.HideAndDontSave;
        }

        _hoverTooltipAsset.Configure(displayedEnchant, face.value, dice.name, face.broken);
        Canvas canvas = _rootCanvas != null ? SkillTooltipUI.GetOrCreateSharedOverlayCanvas(_rootCanvas) : null;
        if (canvas == null || _rt == null)
        {
            SkillTooltipUI.HideCurrentUnlessPointerOverTooltip(gameObject);
            return;
        }

        SkillTooltipUI.Show(canvas, GetDiceEnchantTooltipTarget(), _hoverTooltipAsset);
    }

    private RectTransform GetDiceEnchantTooltipTarget()
    {
        if (_rt == null)
            return null;

        if (_diceEnchantTooltipAnchor == null)
        {
            Transform existing = _rt.Find("DiceEnchantTooltipAnchor");
            if (existing != null)
                _diceEnchantTooltipAnchor = existing as RectTransform;

            if (_diceEnchantTooltipAnchor == null)
            {
                GameObject anchorObject = new GameObject("DiceEnchantTooltipAnchor", typeof(RectTransform));
                _diceEnchantTooltipAnchor = anchorObject.GetComponent<RectTransform>();
                _diceEnchantTooltipAnchor.SetParent(_rt, false);
            }
        }

        _diceEnchantTooltipAnchor.anchorMin = new Vector2(0f, 0f);
        _diceEnchantTooltipAnchor.anchorMax = new Vector2(1f, 1f);
        _diceEnchantTooltipAnchor.pivot = new Vector2(0.5f, 0.5f);
        _diceEnchantTooltipAnchor.offsetMin = new Vector2(0f, diceEnchantTooltipLiftY);
        _diceEnchantTooltipAnchor.offsetMax = new Vector2(0f, diceEnchantTooltipLiftY);
        _diceEnchantTooltipAnchor.SetAsFirstSibling();
        return _diceEnchantTooltipAnchor;
    }

    private int ResolveTooltipFaceIndex()
    {
        if (dice == null)
            return -1;

        if (dice.LastFaceIndex >= 0)
            return dice.LastFaceIndex;

        Camera cam = ResolveTooltipCamera();
        return cam != null ? dice.GetBestFacingFaceIndex(cam) : -1;
    }

    private int ResolveTooltipEnchantFaceIndex()
    {
        int lastFaceIndex = dice != null ? dice.LastFaceIndex : -1;
        if (HasTooltipEnchant(lastFaceIndex))
            return lastFaceIndex;

        Camera cam = ResolveTooltipCamera();
        int facingFaceIndex = cam != null ? dice.GetBestFacingFaceIndex(cam) : -1;
        if (HasTooltipEnchant(facingFaceIndex))
            return facingFaceIndex;

        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam != cam)
        {
            int mainFacingFaceIndex = dice.GetBestFacingFaceIndex(mainCam);
            if (HasTooltipEnchant(mainFacingFaceIndex))
                return mainFacingFaceIndex;
        }

        return lastFaceIndex >= 0 ? lastFaceIndex : facingFaceIndex;
    }

    private bool HasTooltipEnchant(int faceIndex)
    {
        if (dice == null || faceIndex < 0)
            return false;

        DiceFace face = dice.GetFace(faceIndex);
        if (face.broken)
            return true;

        return DiceFaceEnchantUtility.HasEnchant(dice.GetDisplayedFaceEnchant(faceIndex));
    }

    private bool TryResolveHoveredEnchantFace(out int faceIndex)
    {
        faceIndex = -1;
        if (dice == null)
            return false;

        Camera cam = ResolveTooltipCamera();
        if (cam != null && dice.TryResolveHoveredEnchantFace(cam, out faceIndex, out _))
            return true;

        Camera mainCam = Camera.main;
        return mainCam != null &&
               mainCam != cam &&
               dice.TryResolveHoveredEnchantFace(mainCam, out faceIndex, out _);
    }

    private Camera ResolveTooltipCamera()
    {
        Camera cam = manager != null ? manager.GetDiceWorldHoverCamera() : null;
        return cam != null ? cam : Camera.main;
    }

    public bool TryGetSkillTooltip(out Canvas canvas, out RectTransform target, out ScriptableObject asset, out SkillRuntime runtime)
    {
        EnsureInitialized();
        canvas = _rootCanvas != null ? SkillTooltipUI.GetOrCreateSharedOverlayCanvas(_rootCanvas) : null;
        target = _rt;
        asset = _hoverTooltipAsset;
        runtime = null;
        return canvas != null && target != null && asset != null;
    }
}

using DG.Tweening;
using TMPro;
using UnityEngine;

public partial class DiceSpinnerGeneric
{
    private void ApplyFaceIconMaterialColor(SpriteRenderer iconRenderer, Color color)
    {
        if (iconRenderer == null)
            return;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        iconRenderer.GetPropertyBlock(block);
        block.SetColor("_Color", color);
        iconRenderer.SetPropertyBlock(block);
    }

    private void EnsureFacePreviewState()
    {
        int faceCount = faces != null ? faces.Length : 0;
        if (faceCount <= 0)
            return;

        if (_facePreviewTexts == null || _facePreviewTexts.Length != faceCount)
            _facePreviewTexts = new string[faceCount];
        if (_facePreviewBlink == null || _facePreviewBlink.Length != faceCount)
            _facePreviewBlink = new bool[faceCount];
        if (_facePreviewTweens == null || _facePreviewTweens.Length != faceCount)
            _facePreviewTweens = new Tween[faceCount];
        if (_faceBaseColors == null || _faceBaseColors.Length != faceCount)
            _faceBaseColors = new Color[faceCount];
        if (_facePreviewEnchants == null || _facePreviewEnchants.Length != faceCount)
            _facePreviewEnchants = new DiceFaceEnchantKind[faceCount];
        if (_facePreviewEnchantBlink == null || _facePreviewEnchantBlink.Length != faceCount)
            _facePreviewEnchantBlink = new bool[faceCount];
        if (_facePreviewBroken == null || _facePreviewBroken.Length != faceCount)
            _facePreviewBroken = new bool[faceCount];
        if (_facePreviewBrokenBlink == null || _facePreviewBrokenBlink.Length != faceCount)
            _facePreviewBrokenBlink = new bool[faceCount];
        if (_facePreviewIconTweens == null || _facePreviewIconTweens.Length != faceCount)
            _facePreviewIconTweens = new Tween[faceCount];
        if (_faceIconBaseColors == null || _faceIconBaseColors.Length != faceCount)
            _faceIconBaseColors = new Color[faceCount];
        if (_faceIconBaseLocalScales == null || _faceIconBaseLocalScales.Length != faceCount)
            _faceIconBaseLocalScales = new Vector3[faceCount];
        if (_faceIconBaseLocalScaleCached == null || _faceIconBaseLocalScaleCached.Length != faceCount)
            _faceIconBaseLocalScaleCached = new bool[faceCount];
    }

    private void CacheFaceBaseColor(int faceIndex, TMP_Text faceText)
    {
        if (_faceBaseColors == null || faceIndex < 0 || faceIndex >= _faceBaseColors.Length || faceText == null)
            return;

        if (_facePreviewTweens != null &&
            faceIndex < _facePreviewTweens.Length &&
            _facePreviewTweens[faceIndex] != null &&
            _facePreviewTweens[faceIndex].IsActive())
        {
            return;
        }

        _faceBaseColors[faceIndex] = faceText.color;
    }

    private void CacheFaceIconBaseColor(int faceIndex, SpriteRenderer iconRenderer)
    {
        if (_faceIconBaseColors == null || faceIndex < 0 || faceIndex >= _faceIconBaseColors.Length || iconRenderer == null)
            return;

        if (_facePreviewIconTweens != null &&
            faceIndex < _facePreviewIconTweens.Length &&
            _facePreviewIconTweens[faceIndex] != null &&
            _facePreviewIconTweens[faceIndex].IsActive())
        {
            return;
        }

        _faceIconBaseColors[faceIndex] = iconRenderer.color;
    }

    private void CacheFaceIconBaseLocalScale(int faceIndex, SpriteRenderer iconRenderer)
    {
        if (_faceIconBaseLocalScales == null ||
            _faceIconBaseLocalScaleCached == null ||
            faceIndex < 0 ||
            faceIndex >= _faceIconBaseLocalScales.Length ||
            faceIndex >= _faceIconBaseLocalScaleCached.Length ||
            iconRenderer == null)
        {
            return;
        }

        if (_faceIconBaseLocalScaleCached[faceIndex])
            return;

        _faceIconBaseLocalScales[faceIndex] = iconRenderer.transform.localScale;
        _faceIconBaseLocalScaleCached[faceIndex] = true;
    }

    private void ApplyFaceEnchantIconFit(int faceIndex, SpriteRenderer iconRenderer, Sprite icon)
    {
        if (iconRenderer == null)
            return;

        Vector3 baseScale = GetFaceIconBaseLocalScale(faceIndex, iconRenderer);
        if (icon == null)
        {
            iconRenderer.transform.localScale = baseScale;
            return;
        }

        Vector2 maxSize = faceEnchantIconMaxLocalSize;
        if (maxSize.x <= 0f || maxSize.y <= 0f)
        {
            iconRenderer.transform.localScale = baseScale;
            return;
        }

        Vector2 spriteSize = icon.bounds.size;
        float baseWidth = spriteSize.x * Mathf.Abs(baseScale.x);
        float baseHeight = spriteSize.y * Mathf.Abs(baseScale.y);
        if (baseWidth <= 0f || baseHeight <= 0f)
        {
            iconRenderer.transform.localScale = baseScale;
            return;
        }

        float fitScale = Mathf.Min(maxSize.x / baseWidth, maxSize.y / baseHeight);
        if (!upscaleSmallFaceEnchantIcons)
            fitScale = Mathf.Min(1f, fitScale);

        fitScale = Mathf.Max(0.001f, fitScale);
        iconRenderer.transform.localScale = new Vector3(
            baseScale.x * fitScale,
            baseScale.y * fitScale,
            baseScale.z);
    }

    private Vector3 GetFaceIconBaseLocalScale(int faceIndex, SpriteRenderer iconRenderer)
    {
        if (_faceIconBaseLocalScales != null &&
            _faceIconBaseLocalScaleCached != null &&
            faceIndex >= 0 &&
            faceIndex < _faceIconBaseLocalScales.Length &&
            faceIndex < _faceIconBaseLocalScaleCached.Length &&
            _faceIconBaseLocalScaleCached[faceIndex])
        {
            return _faceIconBaseLocalScales[faceIndex];
        }

        return iconRenderer != null ? iconRenderer.transform.localScale : Vector3.one;
    }

    private void ApplyFaceBlink(int faceIndex, TMP_Text faceText, bool shouldBlink)
    {
        if (_facePreviewTweens == null || faceIndex < 0 || faceIndex >= _facePreviewTweens.Length || faceText == null)
            return;

        _facePreviewTweens[faceIndex]?.Kill();
        _facePreviewTweens[faceIndex] = null;

        Color baseColor = (_faceBaseColors != null && faceIndex < _faceBaseColors.Length)
            ? _faceBaseColors[faceIndex]
            : faceText.color;

        faceText.color = baseColor;

        if (!shouldBlink)
            return;

        float baseAlpha = Mathf.Clamp01(baseColor.a);
        faceText.alpha = baseAlpha;
        _facePreviewTweens[faceIndex] = faceText
            .DOFade(Mathf.Max(0.3f, baseAlpha * 0.4f), 0.8f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void ApplyFaceIconBlink(int faceIndex, SpriteRenderer iconRenderer, bool shouldBlink)
    {
        if (_facePreviewIconTweens == null || faceIndex < 0 || faceIndex >= _facePreviewIconTweens.Length || iconRenderer == null)
            return;

        _facePreviewIconTweens[faceIndex]?.Kill();
        _facePreviewIconTweens[faceIndex] = null;

        Color baseColor = (_faceIconBaseColors != null && faceIndex < _faceIconBaseColors.Length)
            ? _faceIconBaseColors[faceIndex]
            : iconRenderer.color;
        iconRenderer.color = baseColor;

        if (!shouldBlink || !iconRenderer.enabled)
            return;

        float baseAlpha = Mathf.Clamp01(baseColor.a);
        iconRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseAlpha);
        _facePreviewIconTweens[faceIndex] = iconRenderer
            .DOFade(Mathf.Max(0.3f, baseAlpha * 0.4f), 0.8f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private TMP_Text[] ExtractFaceTextBindings()
    {
        if (faces == null || faces.Length == 0)
            return null;

        TMP_Text[] bindings = new TMP_Text[faces.Length];
        for (int i = 0; i < faces.Length; i++)
            bindings[i] = faces[i].faceValueText3D;
        return bindings;
    }

    private SpriteRenderer[] ExtractFaceIconBindings()
    {
        if (faces == null || faces.Length == 0)
            return null;

        SpriteRenderer[] bindings = new SpriteRenderer[faces.Length];
        for (int i = 0; i < faces.Length; i++)
            bindings[i] = faces[i].faceIconSpriteRenderer;
        return bindings;
    }

    private void RestoreFaceTextBindings(TMP_Text[] bindings)
    {
        if (bindings == null || faces == null)
            return;

        int count = Mathf.Min(bindings.Length, faces.Length);
        for (int i = 0; i < count; i++)
        {
            DiceFace face = faces[i];
            face.faceValueText3D = bindings[i];
            faces[i] = face;
        }
    }

    private void RestoreFaceIconBindings(SpriteRenderer[] bindings)
    {
        if (bindings == null || faces == null)
            return;

        int count = Mathf.Min(bindings.Length, faces.Length);
        for (int i = 0; i < count; i++)
        {
            DiceFace face = faces[i];
            face.faceIconSpriteRenderer = bindings[i];
            faces[i] = face;
        }
    }
}

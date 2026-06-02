using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public partial class DiceSpinnerGeneric
{
    public void RefreshDisplayedState()
    {
        RefreshAllFaceValueTexts();
    }

    public void SetCombatRollFeedback(bool crit, bool fail)
    {
        bool failTriggered = !_feedbackFail && fail;
        if (_feedbackCrit == crit && _feedbackFail == fail)
            return;

        _feedbackCrit = crit;
        _feedbackFail = fail;
        ApplyWholeDieVisuals();
        ApplyFeedbackOutlineVisuals();

        if (failTriggered)
            PlayFailFeedbackShake();
    }

    public void SetFacePreviewValue(int faceIndex, int previewValue, bool blink = true)
    {
        SetFacePreviewText(faceIndex, ClampFaceValue(previewValue).ToString(), blink);
    }

    public void SetFacePreviewText(int faceIndex, string previewText, bool blink = true)
    {
        if (faces == null || faceIndex < 0 || faceIndex >= faces.Length)
            return;

        EnsureFacePreviewState();
        _facePreviewTexts[faceIndex] = previewText;
        _facePreviewBlink[faceIndex] = blink;
        RefreshFaceValueText(faceIndex);
    }

    public void ClearFacePreview(int faceIndex)
    {
        if (faces == null || faceIndex < 0 || faceIndex >= faces.Length)
            return;

        EnsureFacePreviewState();
        _facePreviewTexts[faceIndex] = null;
        _facePreviewBlink[faceIndex] = false;
        RefreshFaceValueText(faceIndex);
    }

    public void ClearAllFacePreviews()
    {
        if (_facePreviewTexts == null && _facePreviewTweens == null)
            return;

        EnsureFacePreviewState();
        for (int i = 0; i < faces.Length; i++)
        {
            _facePreviewTexts[i] = null;
            _facePreviewBlink[i] = false;
            RefreshFaceValueText(i);
        }
    }

    public string GetFaceDebugLabel(int faceIndex)
    {
        if (!ValidateFaces() || faceIndex < 0 || faceIndex >= faces.Length)
            return string.Empty;

        DiceFace face = faces[faceIndex];
        int displayedValue = face.value;
        DiceFaceEnchantKind enchant = face.enchant;
        if (face.broken)
            return $"Broken {GetEnchantShortLabel(enchant)}";
        if (enchant == DiceFaceEnchantKind.Stone)
            return $"Stone {GetEnchantShortLabel(enchant)}";
        if (enchant == DiceFaceEnchantKind.None)
            return displayedValue.ToString();

        return $"{displayedValue} {GetEnchantShortLabel(enchant)}";
    }

    public string GetCurrentFaceDebugLabel()
    {
        if (!ValidateFaces() || LastFaceIndex < 0 || LastFaceIndex >= faces.Length)
            return string.Empty;

        return GetFaceDebugLabel(LastFaceIndex);
    }

    private bool ValidateFaces()
    {
        if (pivot == null)
            pivot = transform;
        if (flightSpinTarget == null)
            flightSpinTarget = pivot;
        _pivotBaseLocalPosition = pivot.localPosition;
        AutoWireTextReferences();

        if (faces == null || faces.Length == 0)
        {
            Debug.LogError($"{name}: faces is empty. Add face rotations in Inspector.");
            return false;
        }

        return true;
    }

    private void OnValidate()
    {
        if (pivot == null)
            pivot = transform;
        if (flightSpinTarget == null)
            flightSpinTarget = pivot;
        _pivotBaseLocalPosition = pivot.localPosition;
        AutoWireTextReferences();

        if (wholeDieRenderers == null || wholeDieRenderers.Length == 0)
            AutoCollectWholeDieRenderers();

        if (CanUseWholeDieMaterialInstances())
        {
            EnsureWholeDieMaterialInstances();
            ApplyWholeDieVisuals();
        }

        RefreshAllFaceValueTexts();
        RefreshDisplayedState();
    }

    private void OnDestroy()
    {
        _feedbackShakeTween?.Kill();
        _rollStatePopupTween?.Kill();
        if (_rollStatePopupInstance != null)
        {
            if (Application.isPlaying)
                Destroy(_rollStatePopupInstance.gameObject);
            else
                DestroyImmediate(_rollStatePopupInstance.gameObject);
        }
        ReleaseFeedbackOutlineRenderers();
        ReleaseWholeDieMaterialInstances();
    }

    private void RefreshAllFaceValueTexts()
    {
        if (faces == null)
            return;

        for (int i = 0; i < faces.Length; i++)
            RefreshFaceValueText(i);
    }

    private void RefreshFaceValueText(int faceIndex)
    {
        if (faces == null || faceIndex < 0 || faceIndex >= faces.Length)
            return;

        TMP_Text faceText = faces[faceIndex].faceValueText3D;
        if (faceText == null)
            return;

        EnsureFacePreviewState();
        CacheFaceBaseColor(faceIndex, faceText);

        string previewText = _facePreviewTexts != null && faceIndex < _facePreviewTexts.Length
            ? _facePreviewTexts[faceIndex]
            : null;

        if (!string.IsNullOrEmpty(previewText))
        {
            faceText.text = FormatFaceDisplayText(previewText);
        }
        else if (faces[faceIndex].broken)
        {
            faceText.text = "X";
        }
        else if (faces[faceIndex].enchant == DiceFaceEnchantKind.Stone)
        {
            faceText.text = "STONE";
        }
        else
        {
            faceText.text = FormatFaceDisplayText(faces[faceIndex].value.ToString());
        }

        bool shouldBlink = _facePreviewBlink != null &&
                           faceIndex < _facePreviewBlink.Length &&
                           _facePreviewBlink[faceIndex] &&
                           !string.IsNullOrEmpty(previewText);
        ApplyFaceBlink(faceIndex, faceText, shouldBlink);
    }

    private static string FormatFaceDisplayText(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return rawText;

        string trimmed = rawText.Trim();
        if (!ShouldUnderlineAmbiguousSixNine(trimmed))
            return rawText;

        return $"<u>{trimmed}</u>";
    }

    private static bool ShouldUnderlineAmbiguousSixNine(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c != '6' && c != '9')
                return false;
        }

        return true;
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

    private TMP_Text[] ExtractFaceTextBindings()
    {
        if (faces == null || faces.Length == 0)
            return null;

        TMP_Text[] bindings = new TMP_Text[faces.Length];
        for (int i = 0; i < faces.Length; i++)
            bindings[i] = faces[i].faceValueText3D;
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

}

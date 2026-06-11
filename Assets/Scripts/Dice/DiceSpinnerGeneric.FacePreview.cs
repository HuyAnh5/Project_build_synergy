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
        if (_previewSandboxMode)
        {
            if (_feedbackCrit == false && _feedbackFail == false)
                return;

            _feedbackCrit = false;
            _feedbackFail = false;
            ApplyWholeDieVisuals();
            ApplyFeedbackOutlineVisuals();
            return;
        }

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

    public void SetFacePreviewEnchant(int faceIndex, DiceFaceEnchantKind previewEnchant, bool blink = true)
    {
        if (faces == null || faceIndex < 0 || faceIndex >= faces.Length)
            return;

        EnsureFacePreviewState();
        _facePreviewEnchants[faceIndex] = previewEnchant;
        _facePreviewEnchantBlink[faceIndex] = blink;
        RefreshFaceValueText(faceIndex);
    }

    public void SetFacePreviewBroken(int faceIndex, bool blink = true)
    {
        if (faces == null || faceIndex < 0 || faceIndex >= faces.Length)
            return;

        EnsureFacePreviewState();
        _facePreviewBroken[faceIndex] = true;
        _facePreviewBrokenBlink[faceIndex] = blink;
        RefreshFaceValueText(faceIndex);
    }

    public void ClearFacePreview(int faceIndex)
    {
        if (faces == null || faceIndex < 0 || faceIndex >= faces.Length)
            return;

        EnsureFacePreviewState();
        _facePreviewTexts[faceIndex] = null;
        _facePreviewBlink[faceIndex] = false;
        _facePreviewEnchants[faceIndex] = DiceFaceEnchantKind.None;
        _facePreviewEnchantBlink[faceIndex] = false;
        _facePreviewBroken[faceIndex] = false;
        _facePreviewBrokenBlink[faceIndex] = false;
        RefreshFaceValueText(faceIndex);
    }

    public void ClearAllFacePreviews()
    {
        if (_facePreviewTexts == null &&
            _facePreviewTweens == null &&
            _facePreviewEnchants == null &&
            _facePreviewIconTweens == null)
            return;

        EnsureFacePreviewState();
        for (int i = 0; i < faces.Length; i++)
        {
            _facePreviewTexts[i] = null;
            _facePreviewBlink[i] = false;
            _facePreviewEnchants[i] = DiceFaceEnchantKind.None;
            _facePreviewEnchantBlink[i] = false;
            _facePreviewBroken[i] = false;
            _facePreviewBrokenBlink[i] = false;
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
        if (_worldTooltipAsset != null)
            Destroy(_worldTooltipAsset);
        if (_worldTooltipAnchor != null)
            Destroy(_worldTooltipAnchor.gameObject);
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
            faceText.text = previewText;
        }
        else if (faces[faceIndex].broken)
        {
            faceText.text = string.Empty;
        }
        else if (faces[faceIndex].enchant == DiceFaceEnchantKind.Stone)
        {
            faceText.text = string.Empty;
        }
        else
        {
            faceText.text = faces[faceIndex].value.ToString();
        }

        bool shouldBlink = _facePreviewBlink != null &&
                           faceIndex < _facePreviewBlink.Length &&
                           _facePreviewBlink[faceIndex] &&
                           !string.IsNullOrEmpty(previewText);
        ApplyFaceBlink(faceIndex, faceText, shouldBlink);
        RefreshFaceEnchantIcon(faceIndex);
    }

    private void RefreshFaceEnchantIcon(int faceIndex)
    {
        if (faces == null || faceIndex < 0 || faceIndex >= faces.Length)
            return;

        SpriteRenderer iconRenderer = faces[faceIndex].faceIconSpriteRenderer;
        if (iconRenderer == null)
            return;

        DiceFace face = faces[faceIndex];
        EnsureFacePreviewState();

        DiceFaceEnchantKind previewEnchant = _facePreviewEnchants != null && faceIndex < _facePreviewEnchants.Length
            ? _facePreviewEnchants[faceIndex]
            : DiceFaceEnchantKind.None;
        bool hasPreviewEnchant = _facePreviewEnchantBlink != null &&
                                 faceIndex < _facePreviewEnchantBlink.Length &&
                                 (_facePreviewEnchantBlink[faceIndex] || previewEnchant != DiceFaceEnchantKind.None);
        bool hasPreviewBroken = _facePreviewBroken != null &&
                                faceIndex < _facePreviewBroken.Length &&
                                _facePreviewBroken[faceIndex];
        DiceFaceEnchantKind displayedEnchant = hasPreviewEnchant ? previewEnchant : face.enchant;

        Sprite icon = null;
        Color iconTint = Color.white;
        bool hasEnchantIcon = false;

        if (iconLibrary != null && (face.broken || hasPreviewBroken))
        {
            hasEnchantIcon = iconLibrary.TryGetBrokenFaceIcon(out icon, out iconTint) && icon != null;
        }

        if (!hasEnchantIcon)
        {
            hasEnchantIcon =
                !face.broken &&
                DiceFaceEnchantUtility.HasEnchant(displayedEnchant) &&
                iconLibrary != null &&
                iconLibrary.TryGetDiceFaceEnchantIcon(displayedEnchant, out icon, out iconTint) &&
                icon != null;
        }

        CacheFaceIconBaseLocalScale(faceIndex, iconRenderer);
        iconRenderer.sprite = hasEnchantIcon ? icon : null;
        iconRenderer.color = hasEnchantIcon ? iconTint : Color.white;
        iconRenderer.enabled = hasEnchantIcon;
        ApplyFaceIconMaterialColor(iconRenderer, hasEnchantIcon ? iconTint : Color.clear);
        if (_faceIconBaseColors != null && faceIndex < _faceIconBaseColors.Length)
            _faceIconBaseColors[faceIndex] = hasEnchantIcon ? iconTint : Color.white;
        ApplyFaceEnchantIconFit(faceIndex, iconRenderer, hasEnchantIcon ? icon : null);
        bool shouldBlinkIcon =
            hasEnchantIcon &&
            ((hasPreviewEnchant && _facePreviewEnchantBlink[faceIndex]) ||
             (hasPreviewBroken && _facePreviewBrokenBlink != null && faceIndex < _facePreviewBrokenBlink.Length && _facePreviewBrokenBlink[faceIndex]));
        ApplyFaceIconBlink(faceIndex, iconRenderer, shouldBlinkIcon);
    }

}

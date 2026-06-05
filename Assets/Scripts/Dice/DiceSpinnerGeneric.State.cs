using TMPro;
using UnityEngine;

using DG.Tweening;

public partial class DiceSpinnerGeneric
{
    public void ConfigureAsPreviewSandbox()
    {
        _previewSandboxMode = true;
        animateCritFailPopup = false;
        showResultAtStart = false;
        clearStateWhileRolling = false;
        showAddedValueInRollState = false;
        rollStateText = null;
        _feedbackCrit = false;
        _feedbackFail = false;
        _feedbackShakeTween?.Kill();
        ClearRollStatePopupVisuals(clearText: true);
        ApplyWholeDieVisuals();
        ApplyFeedbackOutlineVisuals();
    }

    public void CopyRuntimeStateFrom(DiceSpinnerGeneric other, bool copyRotation)
    {
        if (other == null)
            return;

        if (other.faces != null)
        {
            TMP_Text[] existingFaceTexts = ExtractFaceTextBindings();
            SpriteRenderer[] existingFaceIcons = ExtractFaceIconBindings();
            faces = (DiceFace[])other.faces.Clone();
            RestoreFaceTextBindings(existingFaceTexts);
            RestoreFaceIconBindings(existingFaceIcons);
        }

        wholeDieTag = other.wholeDieTag;
        iconLibrary = other.iconLibrary;
        ApplyWholeDieVisuals();

        if (copyRotation && pivot != null && other.pivot != null)
            pivot.localRotation = other.pivot.localRotation;

        LastFaceIndex = other.LastFaceIndex;
        LastRolledValue = other.LastRolledValue;
        _doubleValueActiveForTurn = other._doubleValueActiveForTurn;
        _doubleValueOriginalFaceValues = other._doubleValueOriginalFaceValues != null
            ? (int[])other._doubleValueOriginalFaceValues.Clone()
            : null;
        IsRolling = false;
        ClearAllFacePreviews();
        RefreshAllFaceValueTexts();
        RefreshDisplayedState();
    }

    public DiceWholeDieTag GetWholeDieTag()
    {
        return wholeDieTag;
    }

    public void SetWholeDieTag(DiceWholeDieTag tag)
    {
        wholeDieTag = tag;
        ApplyWholeDieVisuals();
        RefreshDisplayedState();
    }

    public DiceFace GetFace(int faceIndex)
    {
        if (!ValidateFaces())
            return default;

        faceIndex = Mathf.Clamp(faceIndex, 0, faces.Length - 1);
        return faces[faceIndex];
    }

    public DiceFace GetCurrentFace()
    {
        if (!ValidateFaces() || LastFaceIndex < 0 || LastFaceIndex >= faces.Length)
            return default;

        return faces[LastFaceIndex];
    }

    public DiceFaceEnchantKind GetFaceEnchant(int faceIndex)
    {
        if (!ValidateFaces())
            return DiceFaceEnchantKind.None;

        faceIndex = Mathf.Clamp(faceIndex, 0, faces.Length - 1);
        return faces[faceIndex].enchant;
    }

    public DiceFaceEnchantKind GetDisplayedFaceEnchant(int faceIndex)
    {
        if (!ValidateFaces() || faceIndex < 0 || faceIndex >= faces.Length)
            return DiceFaceEnchantKind.None;

        if (_facePreviewEnchants != null &&
            faceIndex < _facePreviewEnchants.Length &&
            _facePreviewEnchants[faceIndex] != DiceFaceEnchantKind.None)
        {
            return _facePreviewEnchants[faceIndex];
        }

        return faces[faceIndex].enchant;
    }

    public DiceFaceEnchantKind GetCurrentFaceEnchant()
    {
        if (!ValidateFaces() || LastFaceIndex < 0 || LastFaceIndex >= faces.Length)
            return DiceFaceEnchantKind.None;

        return faces[LastFaceIndex].enchant;
    }

    public int GetFaceAddedValue(int faceIndex)
    {
        return DiceFaceEnchantUtility.GetFlatAddedValue(GetFaceEnchant(faceIndex));
    }

    public int GetCurrentFaceAddedValue()
    {
        return DiceFaceEnchantUtility.GetFlatAddedValue(GetCurrentFaceEnchant());
    }

    public bool SetFaceEnchant(int faceIndex, DiceFaceEnchantKind enchant)
    {
        if (!ValidateFaces() || faceIndex < 0 || faceIndex >= faces.Length)
            return false;

        DiceFace face = faces[faceIndex];
        face.enchant = enchant;
        face.broken = false;
        faces[faceIndex] = face;

        RefreshFaceValueText(faceIndex);

        if (faceIndex == LastFaceIndex)
            RefreshDisplayedState();

        return true;
    }

    public bool SetFaceValue(int faceIndex, int value)
    {
        if (!ValidateFaces() || faceIndex < 0 || faceIndex >= faces.Length)
            return false;

        int clampedValue = ClampFaceValue(value);
        DiceFace face = faces[faceIndex];
        if (_doubleValueActiveForTurn && _doubleValueOriginalFaceValues != null && faceIndex < _doubleValueOriginalFaceValues.Length)
        {
            int currentDisplayedValue = face.value;
            int delta = clampedValue - currentDisplayedValue;
            _doubleValueOriginalFaceValues[faceIndex] = ClampFaceValue(_doubleValueOriginalFaceValues[faceIndex] + delta);
        }

        face.value = clampedValue;
        faces[faceIndex] = face;
        RefreshFaceValueText(faceIndex);

        if (faceIndex == LastFaceIndex)
        {
            LastRolledValue = face.value;
            RefreshDisplayedState();
        }

        return true;
    }

    public string GetRollStateLabel()
    {
        if (animateCritFailPopup && Application.isPlaying)
            return normalText;

        DiceFaceEnchantKind currentEnchant = GetCurrentFaceEnchant();

        if (LastRollIsCrit)
        {
            if (DiceFaceEnchantUtility.SuppressesCritBonus(currentEnchant))
                return normalText;

            if (!showAddedValueInRollState)
                return critText;
            int added = GetCritDisplayAddedValue(GetCritFailDisplayValue());
            return $"{critText}: +{added}";
        }

        if (LastRollIsFail)
        {
            if (DiceFaceEnchantUtility.SuppressesFailPenalty(currentEnchant))
                return normalText;
            return failText;
        }

        return normalText;
    }

    public int GetMinFaceValue()
    {
        if (!ValidateFaces())
            return 1;

        int min = int.MaxValue;
        for (int i = 0; i < faces.Length; i++)
        {
            if (faces[i].broken || !DiceFaceEnchantUtility.IsNumericFace(faces[i].enchant))
                continue;
            min = Mathf.Min(min, faces[i].value);
        }

        return min == int.MaxValue ? 1 : min;
    }

    public int GetMaxFaceValue()
    {
        if (!ValidateFaces())
            return 6;

        int max = int.MinValue;
        for (int i = 0; i < faces.Length; i++)
        {
            if (faces[i].broken || !DiceFaceEnchantUtility.IsNumericFace(faces[i].enchant))
                continue;
            max = Mathf.Max(max, faces[i].value);
        }

        return max == int.MinValue ? 6 : max;
    }

    public void GetRollExtents(out int minFace, out int maxFace)
    {
        minFace = GetMinFaceValue();
        maxFace = GetMaxFaceValue();
    }

    public bool IsCritValue(int value)
    {
        if (!ValidateFaces())
            return false;
        return value == GetMaxFaceValue();
    }

    public bool IsFailValue(int value)
    {
        if (!ValidateFaces())
            return false;

        int min = GetMinFaceValue();
        int max = GetMaxFaceValue();
        if (min == max)
            return false;

        return value == min && value != max;
    }

    public int GetCritDisplayAddedValue(int baseValue)
    {
        return Mathf.FloorToInt(Mathf.Max(0f, baseValue) * DiceSlotRig.GenericCritPercent);
    }

    public int GetFailDisplayAddedValueMagnitude(int baseValue)
    {
        return Mathf.FloorToInt(Mathf.Max(0f, baseValue) * 0.50f);
    }

}


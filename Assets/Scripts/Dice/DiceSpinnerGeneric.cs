using UnityEngine;
using DG.Tweening;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DiceSpinnerGeneric : MonoBehaviour
{
    public const int MinFaceValue = 1;
    public const int MaxFaceValue = 99;

    public Transform pivot;
    public DiceFace[] faces;

    [Header("Whole-Die Tag")]
    public DiceWholeDieTag wholeDieTag = DiceWholeDieTag.None;

    [Header("TextMeshPro")]
    public TMP_Text valueText;
    public TMP_Text enchantText;
    public string rollingText = "...";
    public bool showResultAtStart = false;

    [Header("Roll State Text (optional)")]
    [Tooltip("Optional small label shown after roll, for example CRIT / FAIL.")]
    public TMP_Text rollStateText;
    public bool clearStateWhileRolling = true;
    public string critText = "CRIT";
    public string failText = "FAIL";
    public string normalText = "";
    [Tooltip("Debug/helper display. Example: CRIT: +1 or FAIL: -1.")]
    public bool showAddedValueInRollState = true;

    [Header("Input")]
    public bool enableSpaceKey = true;
    public KeyCode rollKey = KeyCode.Space;

    [Header("Timing")]
    public float accelTime = 0.1f;
    public float totalTime = 1.5f;
    public float inspectSnapTime = 0.22f;

    [Header("Loops (adds 360 degrees)")]
    public Vector3Int loopsMin = new Vector3Int(2, 2, 2);
    public Vector3Int loopsMax = new Vector3Int(5, 5, 5);
    [Range(0.05f, 0.6f)] public float accelPortion = 0.25f;

    private Tween _tween;
    private bool _doubleValueActiveForTurn;
    private int[] _doubleValueOriginalFaceValues;

    public int LastRolledValue { get; private set; }
    public int LastFaceIndex { get; private set; } = -1;
    public bool IsRolling { get; private set; }

    public bool LastRollIsCrit => IsCritValue(LastRolledValue) || DiceFaceEnchantUtility.CountsAsCritForConditions(GetCurrentFaceEnchant());
    public bool LastRollIsFail => IsFailValue(LastRolledValue) || DiceFaceEnchantUtility.CountsAsFailForConditions(GetCurrentFaceEnchant());

    public System.Action<DiceSpinnerGeneric> onRollComplete;

    private void Awake()
    {
        if (pivot == null) pivot = transform;
        RefreshDisplayedState();
    }

    private void Update()
    {
        if (!enableSpaceKey) return;

#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame)
            RollRandomFace();
#else
        if (Input.GetKeyDown(rollKey))
            RollRandomFace();
#endif
    }

    public int RollRandomFace()
    {
        if (!ValidateFaces()) return 0;

        int idx = Random.Range(0, faces.Length);
        RollToFaceIndex(idx);
        return faces[idx].value;
    }

    public int ShowRandomPresentationFace()
    {
        if (!ValidateFaces())
            return -1;

        int idx = Random.Range(0, faces.Length);
        SnapToFaceIndexImmediate(idx, syncRollState: false);
        return idx;
    }

    public void RollToFaceIndex(int faceIndex)
    {
        if (!ValidateFaces()) return;

        faceIndex = Mathf.Clamp(faceIndex, 0, faces.Length - 1);

        LastFaceIndex = faceIndex;
        LastRolledValue = faces[faceIndex].value;

        if (valueText != null)
        {
            valueText.text = rollingText;
            if (showResultAtStart)
                valueText.text = LastRolledValue.ToString();
        }

        if (enchantText != null)
            enchantText.text = showResultAtStart ? GetCurrentFaceDebugLabel() : string.Empty;

        if (rollStateText != null && clearStateWhileRolling)
            rollStateText.text = string.Empty;

        Vector3 targetEuler = NormalizeEuler(faces[faceIndex].localEuler);

        _tween?.Kill();
        IsRolling = true;

        Vector3 startEuler = pivot.localEulerAngles;

        int lx = Random.Range(loopsMin.x, loopsMax.x + 1);
        int ly = Random.Range(loopsMin.y, loopsMax.y + 1);
        int lz = Random.Range(loopsMin.z, loopsMax.z + 1);

        Vector3 endEuler = targetEuler + new Vector3(360f * lx, 360f * ly, 360f * lz);

        float slowTime = Mathf.Max(0.01f, totalTime - accelTime);
        Vector3 midEuler = Vector3.Lerp(startEuler, endEuler, accelPortion);

        Sequence seq = DOTween.Sequence();
        seq.Append(pivot.DOLocalRotate(midEuler, accelTime, RotateMode.FastBeyond360).SetEase(Ease.InQuad));
        seq.Append(pivot.DOLocalRotate(endEuler, slowTime, RotateMode.FastBeyond360).SetEase(Ease.OutQuart));

        seq.OnComplete(() =>
        {
            IsRolling = false;

            if (valueText != null && !showResultAtStart)
                valueText.text = LastRolledValue.ToString();

            RefreshDisplayedState();
            onRollComplete?.Invoke(this);
        });

        _tween = seq;
    }

    public void SnapToFaceIndexImmediate(int faceIndex, bool syncRollState = false)
    {
        if (!ValidateFaces())
            return;

        faceIndex = Mathf.Clamp(faceIndex, 0, faces.Length - 1);
        _tween?.Kill();
        IsRolling = false;

        pivot.localEulerAngles = NormalizeEuler(faces[faceIndex].localEuler);

        if (syncRollState)
        {
            LastFaceIndex = faceIndex;
            LastRolledValue = faces[faceIndex].value;
        }

        RefreshDisplayedState();
    }

    public void SnapToFaceIndexAnimated(int faceIndex, bool syncRollState = false)
    {
        if (!ValidateFaces())
            return;

        faceIndex = Mathf.Clamp(faceIndex, 0, faces.Length - 1);
        _tween?.Kill();
        IsRolling = true;

        Vector3 targetEuler = NormalizeEuler(faces[faceIndex].localEuler);
        Sequence seq = DOTween.Sequence();
        seq.Append(pivot.DOLocalRotate(targetEuler, Mathf.Max(0.01f, inspectSnapTime), RotateMode.Fast).SetEase(Ease.OutSine));
        seq.OnComplete(() =>
        {
            IsRolling = false;

            if (syncRollState)
            {
                LastFaceIndex = faceIndex;
                LastRolledValue = faces[faceIndex].value;
            }

            RefreshDisplayedState();
            onRollComplete?.Invoke(this);
        });

        _tween = seq;
    }

    public void SnapToFaceIndexAnimatedQuaternion(int faceIndex, bool syncRollState = false)
    {
        if (!ValidateFaces())
            return;

        faceIndex = Mathf.Clamp(faceIndex, 0, faces.Length - 1);
        _tween?.Kill();
        IsRolling = true;

        Quaternion targetRotation = Quaternion.Euler(NormalizeEuler(faces[faceIndex].localEuler));
        Sequence seq = DOTween.Sequence();
        seq.Append(pivot.DOLocalRotateQuaternion(targetRotation, Mathf.Max(0.01f, inspectSnapTime)).SetEase(Ease.OutSine));
        seq.OnComplete(() =>
        {
            IsRolling = false;

            if (syncRollState)
            {
                LastFaceIndex = faceIndex;
                LastRolledValue = faces[faceIndex].value;
            }

            RefreshDisplayedState();
            onRollComplete?.Invoke(this);
        });

        _tween = seq;
    }

    public int GetBestFacingFaceIndex(Camera cam)
    {
        if (!ValidateFaces())
            return -1;

        Vector3 desiredNormal = cam != null ? -cam.transform.forward : Vector3.forward;
        Quaternion currentRotation = pivot != null ? pivot.rotation : transform.rotation;

        float bestScore = float.NegativeInfinity;
        int bestFaceIndex = -1;

        for (int faceIndex = 0; faceIndex < faces.Length; faceIndex++)
        {
            Quaternion faceRotation = Quaternion.Euler(NormalizeEuler(faces[faceIndex].localEuler));
            Vector3 localFaceNormal = Quaternion.Inverse(faceRotation) * Vector3.back;
            Vector3 worldFaceNormal = currentRotation * localFaceNormal;
            float score = Vector3.Dot(worldFaceNormal, desiredNormal);
            if (score > bestScore)
            {
                bestScore = score;
                bestFaceIndex = faceIndex;
            }
        }

        return bestFaceIndex;
    }

    public int GetBaseValue() => LastRolledValue;

    public static int ClampFaceValue(int value)
    {
        return Mathf.Clamp(value, MinFaceValue, MaxFaceValue);
    }

    public bool HasDoubleValueForTurn => _doubleValueActiveForTurn;

    public void EnableDoubleValueForTurn()
    {
        if (!ValidateFaces())
            return;
        if (_doubleValueActiveForTurn)
            return;

        _doubleValueOriginalFaceValues = new int[faces.Length];
        for (int i = 0; i < faces.Length; i++)
        {
            _doubleValueOriginalFaceValues[i] = faces[i].value;

            DiceFace face = faces[i];
            face.value = ClampFaceValue(face.value * 2);
            faces[i] = face;
        }

        _doubleValueActiveForTurn = true;
        if (LastFaceIndex >= 0 && LastFaceIndex < faces.Length)
            LastRolledValue = faces[LastFaceIndex].value;
        RefreshDisplayedState();
    }

    public void ClearTemporaryTurnEffects()
    {
        if (_doubleValueActiveForTurn && _doubleValueOriginalFaceValues != null && ValidateFaces())
        {
            int count = Mathf.Min(faces.Length, _doubleValueOriginalFaceValues.Length);
            for (int i = 0; i < count; i++)
            {
                DiceFace face = faces[i];
                face.value = ClampFaceValue(_doubleValueOriginalFaceValues[i]);
                faces[i] = face;
            }
        }

        _doubleValueActiveForTurn = false;
        _doubleValueOriginalFaceValues = null;
        if (LastFaceIndex >= 0 && LastFaceIndex < faces.Length)
            LastRolledValue = faces[LastFaceIndex].value;
        RefreshDisplayedState();
    }

    public int GetModifiedFaceValue(int baseValue)
    {
        return ClampFaceValue(baseValue);
    }

    public int GetDisplayedRolledValue()
    {
        return ClampFaceValue(LastRolledValue);
    }

    public void CopyRuntimeStateFrom(DiceSpinnerGeneric other, bool copyRotation)
    {
        if (other == null)
            return;

        if (other.faces != null)
            faces = (DiceFace[])other.faces.Clone();

        wholeDieTag = other.wholeDieTag;

        if (copyRotation && pivot != null && other.pivot != null)
            pivot.localRotation = other.pivot.localRotation;

        LastFaceIndex = other.LastFaceIndex;
        LastRolledValue = other.LastRolledValue;
        _doubleValueActiveForTurn = other._doubleValueActiveForTurn;
        _doubleValueOriginalFaceValues = other._doubleValueOriginalFaceValues != null
            ? (int[])other._doubleValueOriginalFaceValues.Clone()
            : null;
        IsRolling = false;
        RefreshDisplayedState();
    }

    public DiceWholeDieTag GetWholeDieTag()
    {
        return wholeDieTag;
    }

    public void SetWholeDieTag(DiceWholeDieTag tag)
    {
        wholeDieTag = tag;
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
        faces[faceIndex] = face;

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

        if (faceIndex == LastFaceIndex)
        {
            LastRolledValue = face.value;
            RefreshDisplayedState();
        }

        return true;
    }

    public string GetRollStateLabel()
    {
        DiceFaceEnchantKind currentEnchant = GetCurrentFaceEnchant();

        if (LastRollIsCrit)
        {
            if (DiceFaceEnchantUtility.SuppressesCritBonus(currentEnchant))
                return normalText;

            if (!showAddedValueInRollState) return critText;
            int added = GetCritDisplayAddedValue(LastRolledValue);
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
        if (!ValidateFaces()) return 1;

        int min = int.MaxValue;
        for (int i = 0; i < faces.Length; i++)
            min = Mathf.Min(min, faces[i].value);

        return min == int.MaxValue ? 1 : min;
    }

    public int GetMaxFaceValue()
    {
        if (!ValidateFaces()) return 6;

        int max = int.MinValue;
        for (int i = 0; i < faces.Length; i++)
            max = Mathf.Max(max, faces[i].value);

        return max == int.MinValue ? 6 : max;
    }

    public void GetRollExtents(out int minFace, out int maxFace)
    {
        minFace = GetMinFaceValue();
        maxFace = GetMaxFaceValue();
    }

    public bool IsCritValue(int value)
    {
        if (!ValidateFaces()) return false;
        return value == GetMaxFaceValue();
    }

    public bool IsFailValue(int value)
    {
        if (!ValidateFaces()) return false;

        int min = GetMinFaceValue();
        int max = GetMaxFaceValue();
        if (min == max) return false;

        return value == min && value != max;
    }

    public int GetCritDisplayAddedValue(int baseValue)
    {
        return Mathf.FloorToInt(Mathf.Max(0f, baseValue) * 0.20f);
    }

    public int GetFailDisplayAddedValueMagnitude(int baseValue)
    {
        return Mathf.FloorToInt(Mathf.Max(0f, baseValue) * 0.50f);
    }

    public void RefreshDisplayedState()
    {
        if (rollStateText != null)
            rollStateText.text = GetRollStateLabel();

        if (valueText != null && LastFaceIndex >= 0 && LastFaceIndex < faces.Length && !IsRolling)
            valueText.text = LastRolledValue.ToString();

        if (enchantText != null)
            enchantText.text = GetCurrentFaceDebugLabel();
    }

    public string GetFaceDebugLabel(int faceIndex)
    {
        if (!ValidateFaces() || faceIndex < 0 || faceIndex >= faces.Length)
            return string.Empty;

        DiceFace face = faces[faceIndex];
        int displayedValue = face.value;
        if (face.enchant == DiceFaceEnchantKind.None)
            return displayedValue.ToString();

        return $"{displayedValue} {GetEnchantShortLabel(face.enchant)}";
    }

    public string GetCurrentFaceDebugLabel()
    {
        if (!ValidateFaces() || LastFaceIndex < 0 || LastFaceIndex >= faces.Length)
            return string.Empty;

        return GetFaceDebugLabel(LastFaceIndex);
    }

    private bool ValidateFaces()
    {
        if (pivot == null) pivot = transform;

        if (faces == null || faces.Length == 0)
        {
            Debug.LogError($"{name}: faces is empty. Add face rotations in Inspector.");
            return false;
        }

        return true;
    }

    private static Vector3 NormalizeEuler(Vector3 e)
    {
        return new Vector3(Norm(e.x), Norm(e.y), Norm(e.z));

        static float Norm(float a)
        {
            a %= 360f;
            if (a < 0f) a += 360f;
            return a;
        }
    }

    private static string GetEnchantShortLabel(DiceFaceEnchantKind enchant)
    {
        switch (enchant)
        {
            case DiceFaceEnchantKind.ValuePlusN:
                return "Plus";
            case DiceFaceEnchantKind.GuardBoost:
                return "Guard";
            case DiceFaceEnchantKind.Fire:
                return "Fire";
            case DiceFaceEnchantKind.Bleed:
                return "Bleed";
            case DiceFaceEnchantKind.Ice:
                return "Ice";
            case DiceFaceEnchantKind.Lightning:
                return "Bolt";
            case DiceFaceEnchantKind.GoldProc:
                return "Gold";
            default:
                return DiceFaceEnchantUtility.GetDisplayName(enchant);
        }
    }
}

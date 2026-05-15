using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DiceSpinnerGeneric : MonoBehaviour
{
    public const int MinFaceValue = 1;
    public const int MaxFaceValue = 99;

    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
    private const string FeedbackOutlineShaderName = "Hidden/BuildSynergy/DieFeedbackOutlineURP";

    public Transform pivot;
    public DiceFace[] faces;

    [Header("Whole-Die Tag")]
    public DiceWholeDieTag wholeDieTag = DiceWholeDieTag.None;
    [SerializeField] private Renderer[] wholeDieRenderers;
    [SerializeField] private Color patinaColor = new Color(0.42f, 0.78f, 0.67f, 1f);

    [Header("Combat Feedback")]
    [SerializeField, HideInInspector] private bool enableRollResultOutline = true;
    [SerializeField, HideInInspector] private Color critOutlineColor = new Color(1f, 0.82f, 0.15f, 1f);
    [SerializeField, HideInInspector] private Color failOutlineColor = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField, HideInInspector] private float outlineScaleMultiplier = 1.08f;
    [SerializeField, HideInInspector] private float failShakeDuration = 0.16f;
    [SerializeField, HideInInspector] private Vector3 failShakeStrength = new Vector3(0.035f, 0.035f, 0.035f);
    [SerializeField, HideInInspector] private int failShakeVibrato = 22;
    [SerializeField, HideInInspector, Range(0f, 1f)] private float failShakeElasticity = 0.75f;

    [HideInInspector] public TMP_Text valueText;
    [HideInInspector] public TMP_Text enchantText;
    [HideInInspector] public string rollingText = "...";
    [HideInInspector] public bool showResultAtStart = false;

    [Header("Crit Fail Popup")]
    [HideInInspector] public TMP_Text rollStateText;
    [HideInInspector] public bool clearStateWhileRolling = true;
    [HideInInspector] public string critText = "CRIT";
    [HideInInspector] public string failText = "FAIL";
    [HideInInspector] public string normalText = "";
    [HideInInspector] public bool showAddedValueInRollState = true;
    [SerializeField] private bool animateCritFailPopup = true;
    [SerializeField] private Color rollStatePopupColor = Color.white;
    [SerializeField, Min(18f)] private float rollStatePopupFontSize = 70f;
    [SerializeField, Min(0f)] private float rollStatePopupRiseDistance = 26f;
    [SerializeField, Min(0.05f)] private float rollStatePopupDuration = 0.6f;

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
    private string[] _facePreviewTexts;
    private bool[] _facePreviewBlink;
    private Tween[] _facePreviewTweens;
    private Color[] _faceBaseColors;
    private Material[][] _wholeDieMaterialInstances;
    private Color[][] _wholeDieOriginalColors;
    private GameObject[] _feedbackOutlineObjects;
    private Renderer[] _feedbackOutlineRenderers;
    private Material[] _feedbackOutlineMaterials;
    private Tween _feedbackShakeTween;
    private Tween _rollStatePopupTween;
    private Vector3 _pivotBaseLocalPosition;
    private TMP_Text _rollStatePopupInstance;
    private Canvas _rollStatePopupCanvas;
    private DiceDraggableUI _cachedDiceDraggableUi;
    private bool _feedbackCrit;
    private bool _feedbackFail;

    public int LastRolledValue { get; private set; }
    public int LastFaceIndex { get; private set; } = -1;
    public bool IsRolling { get; private set; }

    public bool LastRollIsCrit => IsCritValue(LastRolledValue) || DiceFaceEnchantUtility.CountsAsCritForConditions(GetCurrentFaceEnchant());
    public bool LastRollIsFail => IsFailValue(LastRolledValue) || DiceFaceEnchantUtility.CountsAsFailForConditions(GetCurrentFaceEnchant());

    public System.Action<DiceSpinnerGeneric> onRollComplete;

    private void Awake()
    {
        if (pivot == null)
            pivot = transform;
        _pivotBaseLocalPosition = pivot.localPosition;
        AutoWireTextReferences();

        EnsureWholeDieMaterialInstances();
        ApplyWholeDieVisuals();
        ApplyFeedbackOutlineVisuals();
        RefreshAllFaceValueTexts();
        RefreshDisplayedState();
    }

    private void Update()
    {
        if (!enableSpaceKey)
            return;

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
        if (!ValidateFaces())
            return 0;

        int idx = Random.Range(0, faces.Length);
        RollToFaceIndex(idx);
        return faces[idx].value;
    }

    public int RollRandomFaceWithTiming(float accelDuration, float totalDuration)
    {
        if (!ValidateFaces())
            return 0;

        int idx = Random.Range(0, faces.Length);
        RollToFaceIndexWithTiming(idx, accelDuration, totalDuration);
        return faces[idx].value;
    }

    public int RollRandomFaceTurnStart(float accelDuration, float baseTotalDuration, float tailDuration, int extraTailLoops, Vector3Int? sharedBaseLoops = null)
    {
        if (!ValidateFaces())
            return 0;

        int idx = Random.Range(0, faces.Length);
        RollToFaceIndexTurnStartProfile(idx, accelDuration, baseTotalDuration, tailDuration, extraTailLoops, sharedBaseLoops);
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
        RollToFaceIndexWithTiming(faceIndex, accelTime, totalTime);
    }

    public void RollToFaceIndexWithTiming(int faceIndex, float accelDuration, float totalDuration)
    {
        if (!ValidateFaces())
            return;

        faceIndex = Mathf.Clamp(faceIndex, 0, faces.Length - 1);

        LastFaceIndex = faceIndex;
        LastRolledValue = faces[faceIndex].value;

        ClearRollStatePopupVisuals(clearText: true);

        Vector3 targetEuler = NormalizeEuler(faces[faceIndex].localEuler);

        _tween?.Kill();
        IsRolling = true;

        Vector3 startEuler = pivot.localEulerAngles;

        int lx = Random.Range(loopsMin.x, loopsMax.x + 1);
        int ly = Random.Range(loopsMin.y, loopsMax.y + 1);
        int lz = Random.Range(loopsMin.z, loopsMax.z + 1);

        Vector3 endEuler = targetEuler + new Vector3(360f * lx, 360f * ly, 360f * lz);

        float safeAccelTime = Mathf.Max(0.01f, accelDuration);
        float safeTotalTime = Mathf.Max(safeAccelTime + 0.01f, totalDuration);
        float slowTime = Mathf.Max(0.01f, safeTotalTime - safeAccelTime);
        Vector3 midEuler = Vector3.Lerp(startEuler, endEuler, accelPortion);

        Sequence seq = DOTween.Sequence();
        seq.Append(pivot.DOLocalRotate(midEuler, safeAccelTime, RotateMode.FastBeyond360).SetEase(Ease.InQuad));
        seq.Append(pivot.DOLocalRotate(endEuler, slowTime, RotateMode.FastBeyond360).SetEase(Ease.OutQuart));

        seq.OnComplete(() =>
        {
            IsRolling = false;

            RefreshDisplayedState();
            PlayRollStatePopupIfNeeded();
            onRollComplete?.Invoke(this);
        });

        _tween = seq;
    }

    public void RollToFaceIndexTurnStartProfile(int faceIndex, float accelDuration, float baseTotalDuration, float tailDuration, int extraTailLoops, Vector3Int? sharedBaseLoops = null)
    {
        if (!ValidateFaces())
            return;

        faceIndex = Mathf.Clamp(faceIndex, 0, faces.Length - 1);

        LastFaceIndex = faceIndex;
        LastRolledValue = faces[faceIndex].value;

        ClearRollStatePopupVisuals(clearText: true);

        Vector3 targetEuler = NormalizeEuler(faces[faceIndex].localEuler);

        _tween?.Kill();
        IsRolling = true;

        Vector3 startEuler = pivot.localEulerAngles;

        Vector3Int loopProfile = sharedBaseLoops ?? new Vector3Int(
            Random.Range(loopsMin.x, loopsMax.x + 1),
            Random.Range(loopsMin.y, loopsMax.y + 1),
            Random.Range(loopsMin.z, loopsMax.z + 1));

        int lx = loopProfile.x;
        int ly = loopProfile.y;
        int lz = loopProfile.z;

        Vector3 commonEndEuler = targetEuler + new Vector3(360f * lx, 360f * ly, 360f * lz);
        Vector3 finalEndEuler = commonEndEuler + new Vector3(0f, 0f, 360f * Mathf.Max(0, extraTailLoops));

        float safeAccelTime = Mathf.Max(0.01f, accelDuration);
        float safeBaseTotalTime = Mathf.Max(safeAccelTime + 0.01f, baseTotalDuration);
        float baseSlowTime = Mathf.Max(0.01f, safeBaseTotalTime - safeAccelTime);
        float safeTailTime = Mathf.Max(0f, tailDuration);
        Vector3 midEuler = Vector3.Lerp(startEuler, commonEndEuler, accelPortion);

        Sequence seq = DOTween.Sequence();
        seq.Append(pivot.DOLocalRotate(midEuler, safeAccelTime, RotateMode.FastBeyond360).SetEase(Ease.InQuad));
        seq.Append(pivot.DOLocalRotate(commonEndEuler, baseSlowTime, RotateMode.FastBeyond360).SetEase(Ease.OutQuart));
        if (safeTailTime > 0f && extraTailLoops > 0)
            seq.Append(pivot.DOLocalRotate(finalEndEuler, safeTailTime, RotateMode.FastBeyond360).SetEase(Ease.OutQuart));

        seq.OnComplete(() =>
        {
            IsRolling = false;

            RefreshDisplayedState();
            PlayRollStatePopupIfNeeded();
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
        RefreshAllFaceValueTexts();
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
        RefreshAllFaceValueTexts();
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
        {
            TMP_Text[] existingFaceTexts = ExtractFaceTextBindings();
            faces = (DiceFace[])other.faces.Clone();
            RestoreFaceTextBindings(existingFaceTexts);
        }

        wholeDieTag = other.wholeDieTag;
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
        if (!ValidateFaces())
            return 1;

        int min = int.MaxValue;
        for (int i = 0; i < faces.Length; i++)
            min = Mathf.Min(min, faces[i].value);

        return min == int.MaxValue ? 1 : min;
    }

    public int GetMaxFaceValue()
    {
        if (!ValidateFaces())
            return 6;

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
        return Mathf.FloorToInt(Mathf.Max(0f, baseValue) * 0.20f);
    }

    public int GetFailDisplayAddedValueMagnitude(int baseValue)
    {
        return Mathf.FloorToInt(Mathf.Max(0f, baseValue) * 0.50f);
    }

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
        if (pivot == null)
            pivot = transform;
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

        faceText.text = string.IsNullOrEmpty(previewText)
            ? faces[faceIndex].value.ToString()
            : previewText;

        bool shouldBlink = _facePreviewBlink != null &&
                           faceIndex < _facePreviewBlink.Length &&
                           _facePreviewBlink[faceIndex] &&
                           !string.IsNullOrEmpty(previewText);
        ApplyFaceBlink(faceIndex, faceText, shouldBlink);
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

    private void EnsureWholeDieMaterialInstances()
    {
        if (!CanUseWholeDieMaterialInstances())
            return;

        AutoCollectWholeDieRenderers();

        if (wholeDieRenderers == null || wholeDieRenderers.Length == 0)
            return;

        if (_wholeDieMaterialInstances != null &&
            _wholeDieOriginalColors != null &&
            _wholeDieMaterialInstances.Length == wholeDieRenderers.Length &&
            _wholeDieOriginalColors.Length == wholeDieRenderers.Length)
        {
            return;
        }

        ReleaseWholeDieMaterialInstances();

        _wholeDieMaterialInstances = new Material[wholeDieRenderers.Length][];
        _wholeDieOriginalColors = new Color[wholeDieRenderers.Length][];

        for (int rendererIndex = 0; rendererIndex < wholeDieRenderers.Length; rendererIndex++)
        {
            Renderer renderer = wholeDieRenderers[rendererIndex];
            if (renderer == null)
                continue;

            Material[] materials = renderer.materials;
            if (materials == null)
                continue;

            _wholeDieMaterialInstances[rendererIndex] = materials;
            _wholeDieOriginalColors[rendererIndex] = new Color[materials.Length];

            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                _wholeDieOriginalColors[rendererIndex][materialIndex] = GetMaterialColor(materials[materialIndex]);
        }
    }

    private void ApplyWholeDieVisuals()
    {
        EnsureWholeDieMaterialInstances();

        if (_wholeDieMaterialInstances == null || _wholeDieOriginalColors == null)
            return;

        for (int rendererIndex = 0; rendererIndex < _wholeDieMaterialInstances.Length; rendererIndex++)
        {
            Material[] materials = _wholeDieMaterialInstances[rendererIndex];
            Color[] originalColors = rendererIndex < _wholeDieOriginalColors.Length ? _wholeDieOriginalColors[rendererIndex] : null;
            if (materials == null)
                continue;

            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null)
                    continue;

                Color targetColor = originalColors != null && materialIndex < originalColors.Length
                    ? originalColors[materialIndex]
                    : Color.white;

                switch (wholeDieTag)
                {
                    case DiceWholeDieTag.Patina:
                        targetColor = patinaColor;
                        break;
                }

                SetMaterialColor(material, targetColor);
            }
        }
    }

    private void EnsureFeedbackOutlineRenderers()
    {
        if (!Application.isPlaying || !enableRollResultOutline)
            return;

        AutoCollectWholeDieRenderers();
        if (wholeDieRenderers == null || wholeDieRenderers.Length == 0)
            return;

        if (_feedbackOutlineRenderers != null &&
            _feedbackOutlineObjects != null &&
            _feedbackOutlineMaterials != null &&
            _feedbackOutlineRenderers.Length == wholeDieRenderers.Length &&
            _feedbackOutlineObjects.Length == wholeDieRenderers.Length &&
            _feedbackOutlineMaterials.Length == wholeDieRenderers.Length)
        {
            return;
        }

        ReleaseFeedbackOutlineRenderers();

        Shader outlineShader = Shader.Find(FeedbackOutlineShaderName);
        if (outlineShader == null)
            return;

        _feedbackOutlineObjects = new GameObject[wholeDieRenderers.Length];
        _feedbackOutlineRenderers = new Renderer[wholeDieRenderers.Length];
        _feedbackOutlineMaterials = new Material[wholeDieRenderers.Length];

        for (int rendererIndex = 0; rendererIndex < wholeDieRenderers.Length; rendererIndex++)
        {
            Renderer sourceRenderer = wholeDieRenderers[rendererIndex];
            if (sourceRenderer == null)
                continue;

            GameObject outlineGo = TryCreateOutlineRenderer(sourceRenderer, outlineShader, out Renderer outlineRenderer, out Material outlineMaterial);
            if (outlineGo == null || outlineRenderer == null || outlineMaterial == null)
                continue;

            _feedbackOutlineObjects[rendererIndex] = outlineGo;
            _feedbackOutlineRenderers[rendererIndex] = outlineRenderer;
            _feedbackOutlineMaterials[rendererIndex] = outlineMaterial;
        }
    }

    private void ApplyFeedbackOutlineVisuals()
    {
        EnsureFeedbackOutlineRenderers();
        if (_feedbackOutlineRenderers == null || _feedbackOutlineMaterials == null)
            return;

        bool showOutline = enableRollResultOutline && (_feedbackCrit || _feedbackFail);
        Color outlineColor = _feedbackFail ? failOutlineColor : critOutlineColor;

        for (int rendererIndex = 0; rendererIndex < _feedbackOutlineRenderers.Length; rendererIndex++)
        {
            Renderer outlineRenderer = _feedbackOutlineRenderers[rendererIndex];
            Material outlineMaterial = rendererIndex < _feedbackOutlineMaterials.Length ? _feedbackOutlineMaterials[rendererIndex] : null;
            GameObject outlineGo = rendererIndex < _feedbackOutlineObjects.Length ? _feedbackOutlineObjects[rendererIndex] : null;
            Renderer sourceRenderer = wholeDieRenderers != null && rendererIndex < wholeDieRenderers.Length ? wholeDieRenderers[rendererIndex] : null;

            if (outlineRenderer == null || outlineMaterial == null || outlineGo == null)
                continue;

            bool active = showOutline && sourceRenderer != null && sourceRenderer.enabled && sourceRenderer.gameObject.activeInHierarchy;
            if (outlineGo.activeSelf != active)
                outlineGo.SetActive(active);

            if (!active)
                continue;

            outlineGo.transform.localScale = Vector3.one * Mathf.Max(1.001f, outlineScaleMultiplier);
            SetMaterialColor(outlineMaterial, outlineColor);
        }
    }

    private void AutoCollectWholeDieRenderers()
    {
        if (wholeDieRenderers != null && wholeDieRenderers.Length > 0)
            return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        List<Renderer> filtered = new List<Renderer>(renderers.Length);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;
            if (renderer.GetComponent<TMP_Text>() != null)
                continue;

            filtered.Add(renderer);
        }

        wholeDieRenderers = filtered.ToArray();
    }

    private void ReleaseWholeDieMaterialInstances()
    {
        if (_wholeDieMaterialInstances == null)
            return;

        for (int rendererIndex = 0; rendererIndex < _wholeDieMaterialInstances.Length; rendererIndex++)
        {
            Material[] materials = _wholeDieMaterialInstances[rendererIndex];
            if (materials == null)
                continue;

            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(material);
                else
                    DestroyImmediate(material);
            }
        }

        _wholeDieMaterialInstances = null;
        _wholeDieOriginalColors = null;
    }

    private void ReleaseFeedbackOutlineRenderers()
    {
        if (_feedbackOutlineMaterials != null)
        {
            for (int i = 0; i < _feedbackOutlineMaterials.Length; i++)
            {
                Material material = _feedbackOutlineMaterials[i];
                if (material == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(material);
                else
                    DestroyImmediate(material);
            }
        }

        if (_feedbackOutlineObjects != null)
        {
            for (int i = 0; i < _feedbackOutlineObjects.Length; i++)
            {
                GameObject outlineGo = _feedbackOutlineObjects[i];
                if (outlineGo == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(outlineGo);
                else
                    DestroyImmediate(outlineGo);
            }
        }

        _feedbackOutlineObjects = null;
        _feedbackOutlineRenderers = null;
        _feedbackOutlineMaterials = null;
    }

    private bool CanUseWholeDieMaterialInstances()
    {
        if (!Application.isPlaying)
            return false;

#if UNITY_EDITOR
        if (EditorUtility.IsPersistent(this) && !PrefabUtility.IsPartOfPrefabInstance(this))
            return false;
#endif
        return true;
    }

    private void PlayRollStatePopupIfNeeded()
    {
        if (!Application.isPlaying || !animateCritFailPopup)
            return;

        string label = null;
        if (LastRollIsCrit)
            label = critText;
        else if (LastRollIsFail)
            label = failText;

        if (string.IsNullOrWhiteSpace(label))
            return;

        TMP_Text popup = GetOrCreateRollStatePopupInstance();
        if (popup == null)
            return;

        _rollStatePopupTween?.Kill();

        PositionRollStatePopup(popup);
        popup.gameObject.SetActive(true);
        popup.text = label;
        Color popupColor = rollStatePopupColor;
        popupColor.a = 1f;
        popup.color = popupColor;

        Sequence seq = DOTween.Sequence();
        if (popup.rectTransform != null)
        {
            Vector2 start = popup.rectTransform.anchoredPosition;
            seq.Append(popup.rectTransform.DOAnchorPosY(start.y + rollStatePopupRiseDistance, Mathf.Max(0.01f, rollStatePopupDuration)).SetEase(Ease.OutQuad));
        }
        else
        {
            Vector3 start = popup.transform.position;
            seq.Append(popup.transform.DOMoveY(start.y + rollStatePopupRiseDistance, Mathf.Max(0.01f, rollStatePopupDuration)).SetEase(Ease.OutQuad));
        }

        seq.Join(popup.DOFade(0f, Mathf.Max(0.01f, rollStatePopupDuration)).SetEase(Ease.OutQuad));
        seq.SetUpdate(true);
        seq.OnComplete(() =>
        {
            ClearRollStatePopupVisuals(clearText: true);
            _rollStatePopupTween = null;
        });
        _rollStatePopupTween = seq;
    }

    private void ClearRollStatePopupVisuals(bool clearText)
    {
        _rollStatePopupTween?.Kill();
        _rollStatePopupTween = null;

        if (_rollStatePopupInstance == null)
            return;

        if (clearText)
            _rollStatePopupInstance.text = string.Empty;

        _rollStatePopupInstance.gameObject.SetActive(false);
    }

    private TMP_Text GetOrCreateRollStatePopupInstance()
    {
        Canvas popupCanvas = ResolveRollStatePopupCanvas();
        Transform popupParent = popupCanvas != null ? popupCanvas.transform : transform;
        if (popupParent == null)
            return null;

        bool needsRecreate =
            _rollStatePopupInstance == null ||
            _rollStatePopupCanvas != popupCanvas ||
            _rollStatePopupInstance.transform.parent != popupParent;

        if (!needsRecreate)
            return _rollStatePopupInstance;

        if (_rollStatePopupInstance != null)
        {
            if (Application.isPlaying)
                Destroy(_rollStatePopupInstance.gameObject);
            else
                DestroyImmediate(_rollStatePopupInstance.gameObject);
        }

        _rollStatePopupCanvas = popupCanvas;
        GameObject popupGo = new GameObject("CritFailPopup", typeof(RectTransform), typeof(TextMeshProUGUI));
        popupGo.layer = gameObject.layer;
        popupGo.transform.SetParent(popupParent, false);
        _rollStatePopupInstance = popupGo.GetComponent<TextMeshProUGUI>();
        _rollStatePopupInstance.raycastTarget = false;
        _rollStatePopupInstance.text = string.Empty;
        _rollStatePopupInstance.enableAutoSizing = false;
        _rollStatePopupInstance.fontSize = Mathf.Max(18f, rollStatePopupFontSize);
        _rollStatePopupInstance.enableWordWrapping = false;
        _rollStatePopupInstance.overflowMode = TextOverflowModes.Overflow;
        _rollStatePopupInstance.alignment = TextAlignmentOptions.Center;
        _rollStatePopupInstance.color = rollStatePopupColor;
        if (TMP_Settings.defaultFontAsset != null)
            _rollStatePopupInstance.font = TMP_Settings.defaultFontAsset;
        RectTransform popupRect = _rollStatePopupInstance.rectTransform;
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.sizeDelta = new Vector2(240f, Mathf.Max(56f, rollStatePopupFontSize * 1.4f));
        _rollStatePopupInstance.transform.localScale = Vector3.one;
        _rollStatePopupInstance.gameObject.SetActive(false);
        return _rollStatePopupInstance;
    }

    private Canvas ResolveRollStatePopupCanvas()
    {
        if (_rollStatePopupCanvas != null && !_rollStatePopupCanvas.transform.IsChildOf(pivot))
            return _rollStatePopupCanvas;

        RectTransform anchor = GetCritFailPopupAnchor();
        Canvas sourceCanvas = anchor != null ? anchor.GetComponentInParent<Canvas>() : null;
        if (sourceCanvas != null && sourceCanvas.rootCanvas != null && !sourceCanvas.rootCanvas.transform.IsChildOf(pivot))
            return sourceCanvas.rootCanvas;

        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        Canvas fallback = sourceCanvas != null ? sourceCanvas.rootCanvas : null;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || !canvas.isRootCanvas)
                continue;

            if (pivot != null && canvas.transform.IsChildOf(pivot))
                continue;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || canvas.renderMode == RenderMode.ScreenSpaceCamera)
                return canvas;

            if (fallback == null)
                fallback = canvas;
        }

        return fallback;
    }

    private void PositionRollStatePopup(TMP_Text popup)
    {
        RectTransform sourceAnchor = GetCritFailPopupAnchor();
        if (popup == null || sourceAnchor == null)
            return;

        RectTransform popupRect = popup.rectTransform;
        RectTransform sourceRect = sourceAnchor;
        if (popupRect == null || sourceRect == null)
        {
            popup.transform.position = sourceAnchor.position;
            return;
        }

        Canvas popupCanvas = _rollStatePopupCanvas != null ? _rollStatePopupCanvas : popup.canvas;
        RectTransform popupCanvasRect = popupCanvas != null ? popupCanvas.transform as RectTransform : null;
        if (popupCanvasRect == null)
        {
            popup.transform.position = sourceAnchor.position;
            return;
        }

        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = sourceRect.pivot;

        Camera sourceCamera = GetCanvasEventCamera(sourceRect.GetComponentInParent<Canvas>());
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(sourceCamera, sourceRect.position);
        Camera popupCamera = GetCanvasEventCamera(popupCanvas);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(popupCanvasRect, screenPoint, popupCamera, out Vector2 localPoint))
            popupRect.anchoredPosition = localPoint;
        else
            popup.transform.position = sourceAnchor.position;
    }

    private static Camera GetCanvasEventCamera(Canvas canvas)
    {
        if (canvas == null)
            return null;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    private void AutoWireTextReferences()
    {
        valueText = null;
        enchantText = null;
        rollStateText = null;
    }

    private RectTransform GetCritFailPopupAnchor()
    {
        DiceDraggableUI diceUi = GetDiceDraggableUi();
        if (diceUi != null)
            return diceUi.GetCritFailPopupAnchor();
        return null;
    }

    private DiceDraggableUI GetDiceDraggableUi()
    {
        if (_cachedDiceDraggableUi != null && _cachedDiceDraggableUi.dice == this)
            return _cachedDiceDraggableUi;

        DiceDraggableUI[] allDiceUi = FindObjectsOfType<DiceDraggableUI>(true);
        for (int i = 0; i < allDiceUi.Length; i++)
        {
            DiceDraggableUI candidate = allDiceUi[i];
            if (candidate != null && candidate.dice == this)
            {
                _cachedDiceDraggableUi = candidate;
                return candidate;
            }
        }

        return null;
    }
    private static Color GetMaterialColor(Material material)
    {
        if (material == null)
            return Color.white;
        if (material.HasProperty(BaseColorPropertyId))
            return material.GetColor(BaseColorPropertyId);
        if (material.HasProperty(ColorPropertyId))
            return material.GetColor(ColorPropertyId);
        return Color.white;
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material == null)
            return;
        if (material.HasProperty(BaseColorPropertyId))
            material.SetColor(BaseColorPropertyId, color);
        if (material.HasProperty(ColorPropertyId))
            material.SetColor(ColorPropertyId, color);
    }

    private void PlayFailFeedbackShake()
    {
        if (!Application.isPlaying || pivot == null)
            return;

        _feedbackShakeTween?.Kill();
        pivot.localPosition = _pivotBaseLocalPosition;
        _feedbackShakeTween = pivot.DOPunchPosition(
                failShakeStrength,
                Mathf.Max(0.01f, failShakeDuration),
                Mathf.Max(1, failShakeVibrato),
                Mathf.Clamp01(failShakeElasticity),
                snapping: false)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (pivot != null)
                    pivot.localPosition = _pivotBaseLocalPosition;
                _feedbackShakeTween = null;
            });
    }

    private GameObject TryCreateOutlineRenderer(Renderer sourceRenderer, Shader outlineShader, out Renderer outlineRenderer, out Material outlineMaterial)
    {
        outlineRenderer = null;
        outlineMaterial = null;

        if (sourceRenderer == null || outlineShader == null)
            return null;

        int materialCount = sourceRenderer.sharedMaterials != null && sourceRenderer.sharedMaterials.Length > 0
            ? sourceRenderer.sharedMaterials.Length
            : 1;

        Material sharedOutlineMaterial = new Material(outlineShader)
        {
            name = $"{sourceRenderer.name}_FeedbackOutline",
            hideFlags = HideFlags.HideAndDontSave
        };

        if (sourceRenderer is MeshRenderer meshRenderer)
        {
            MeshFilter sourceFilter = meshRenderer.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
            {
                if (Application.isPlaying)
                    Destroy(sharedOutlineMaterial);
                else
                    DestroyImmediate(sharedOutlineMaterial);
                return null;
            }

            GameObject outlineGo = new GameObject($"{sourceRenderer.name}__FeedbackOutline", typeof(Transform), typeof(MeshFilter), typeof(MeshRenderer));
            outlineGo.hideFlags = HideFlags.HideAndDontSave;
            outlineGo.layer = sourceRenderer.gameObject.layer;
            outlineGo.transform.SetParent(sourceRenderer.transform, false);
            outlineGo.transform.localPosition = Vector3.zero;
            outlineGo.transform.localRotation = Quaternion.identity;
            outlineGo.transform.localScale = Vector3.one * Mathf.Max(1.001f, outlineScaleMultiplier);

            MeshFilter outlineFilter = outlineGo.GetComponent<MeshFilter>();
            outlineFilter.sharedMesh = sourceFilter.sharedMesh;

            MeshRenderer createdRenderer = outlineGo.GetComponent<MeshRenderer>();
            createdRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            createdRenderer.receiveShadows = false;
            createdRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            createdRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            createdRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            Material[] materials = new Material[materialCount];
            for (int i = 0; i < materials.Length; i++)
                materials[i] = sharedOutlineMaterial;
            createdRenderer.sharedMaterials = materials;
            createdRenderer.enabled = false;

            outlineRenderer = createdRenderer;
            outlineMaterial = sharedOutlineMaterial;
            return outlineGo;
        }

        if (sourceRenderer is SkinnedMeshRenderer skinnedRenderer)
        {
            if (skinnedRenderer.sharedMesh == null)
            {
                if (Application.isPlaying)
                    Destroy(sharedOutlineMaterial);
                else
                    DestroyImmediate(sharedOutlineMaterial);
                return null;
            }

            GameObject outlineGo = new GameObject($"{sourceRenderer.name}__FeedbackOutline", typeof(Transform), typeof(SkinnedMeshRenderer));
            outlineGo.hideFlags = HideFlags.HideAndDontSave;
            outlineGo.layer = sourceRenderer.gameObject.layer;
            outlineGo.transform.SetParent(sourceRenderer.transform, false);
            outlineGo.transform.localPosition = Vector3.zero;
            outlineGo.transform.localRotation = Quaternion.identity;
            outlineGo.transform.localScale = Vector3.one * Mathf.Max(1.001f, outlineScaleMultiplier);

            SkinnedMeshRenderer createdRenderer = outlineGo.GetComponent<SkinnedMeshRenderer>();
            createdRenderer.sharedMesh = skinnedRenderer.sharedMesh;
            createdRenderer.rootBone = skinnedRenderer.rootBone;
            createdRenderer.bones = skinnedRenderer.bones;
            createdRenderer.updateWhenOffscreen = true;
            createdRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            createdRenderer.receiveShadows = false;
            createdRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            createdRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            createdRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            Material[] materials = new Material[materialCount];
            for (int i = 0; i < materials.Length; i++)
                materials[i] = sharedOutlineMaterial;
            createdRenderer.sharedMaterials = materials;
            createdRenderer.enabled = false;

            outlineRenderer = createdRenderer;
            outlineMaterial = sharedOutlineMaterial;
            return outlineGo;
        }

        if (Application.isPlaying)
            Destroy(sharedOutlineMaterial);
        else
            DestroyImmediate(sharedOutlineMaterial);

        return null;
    }

    private static Vector3 NormalizeEuler(Vector3 e)
    {
        return new Vector3(Norm(e.x), Norm(e.y), Norm(e.z));

        static float Norm(float a)
        {
            a %= 360f;
            if (a < 0f)
                a += 360f;
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

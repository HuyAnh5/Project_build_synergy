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

public partial class DiceSpinnerGeneric : MonoBehaviour
{
    public const int MinFaceValue = 1;
    public const int MaxFaceValue = 99;

    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
    private const string FeedbackOutlineShaderName = "Hidden/BuildSynergy/DieFeedbackOutlineURP";

    public Transform pivot;
    [Header("Presentation")]
    [Tooltip("Optional inner transform used for cast/flying spin presentation. Falls back to pivot when empty.")]
    public Transform flightSpinTarget;
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
    private int _phaseValueModifier;
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

    public bool LastRollIsCrit => IsCurrentFaceUsable() && IsCurrentFaceNumeric() && (IsCritValue(LastRolledValue) || DiceFaceEnchantUtility.CountsAsCritForConditions(GetCurrentFaceEnchant()));
    public bool LastRollIsFail => IsCurrentFaceUsable() && IsCurrentFaceNumeric() && (IsFailValue(LastRolledValue) || DiceFaceEnchantUtility.CountsAsFailForConditions(GetCurrentFaceEnchant()));
    public Tween ActiveTween => _tween;
    public Transform FlightSpinTarget => flightSpinTarget != null ? flightSpinTarget : pivot;

    public System.Action<DiceSpinnerGeneric> onRollComplete;

    private void Awake()
    {
        if (pivot == null)
            pivot = transform;
        if (flightSpinTarget == null)
            flightSpinTarget = pivot;
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

        int idx = RollWeightedFaceIndex();
        RollToFaceIndex(idx);
        return faces[idx].value;
    }

    public int RollRandomFaceWithTiming(float accelDuration, float totalDuration)
    {
        if (!ValidateFaces())
            return 0;

        int idx = RollWeightedFaceIndex();
        RollToFaceIndexWithTiming(idx, accelDuration, totalDuration);
        return faces[idx].value;
    }

    public int RollRandomFaceTurnStart(float accelDuration, float baseTotalDuration, float tailDuration, int extraTailLoops, Vector3Int? sharedBaseLoops = null)
    {
        if (!ValidateFaces())
            return 0;

        int idx = RollWeightedFaceIndex();
        RollToFaceIndexTurnStartProfile(idx, accelDuration, baseTotalDuration, tailDuration, extraTailLoops, sharedBaseLoops);
        return faces[idx].value;
    }

    public int ShowRandomPresentationFace()
    {
        if (!ValidateFaces())
            return -1;

        int idx = RollWeightedFaceIndex();
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

    public Tween PlayPresentationRollToFaceIndex(int faceIndex, float accelDuration, float totalDuration, int extraLoopsPerAxis = 0)
    {
        if (!ValidateFaces())
            return null;

        faceIndex = Mathf.Clamp(faceIndex, 0, faces.Length - 1);

        Vector3 targetEuler = NormalizeEuler(faces[faceIndex].localEuler);

        _tween?.Kill();
        ClearRollStatePopupVisuals(clearText: false);
        IsRolling = true;

        Vector3 startEuler = pivot.localEulerAngles;

        int lx = Random.Range(loopsMin.x, loopsMax.x + 1) + Mathf.Max(0, extraLoopsPerAxis);
        int ly = Random.Range(loopsMin.y, loopsMax.y + 1) + Mathf.Max(0, extraLoopsPerAxis);
        int lz = Random.Range(loopsMin.z, loopsMax.z + 1) + Mathf.Max(0, extraLoopsPerAxis);

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
            _tween = null;
        });
        seq.OnKill(() =>
        {
            if (_tween == seq)
                _tween = null;
        });

        _tween = seq;
        return seq;
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
        _phaseValueModifier = 0;
        if (LastFaceIndex >= 0 && LastFaceIndex < faces.Length)
            LastRolledValue = faces[LastFaceIndex].value;
        RefreshAllFaceValueTexts();
        RefreshDisplayedState();
    }

    public int GetModifiedFaceValue(int baseValue)
    {
        return ClampFaceValue(baseValue + _phaseValueModifier);
    }

    public int GetDisplayedRolledValue()
    {
        return ClampFaceValue(LastRolledValue);
    }

    public int GetCurrentPhaseValueModifier()
    {
        return _phaseValueModifier;
    }

    public void AddPhaseValueModifier(int amount)
    {
        _phaseValueModifier += amount;
        RefreshDisplayedState();
    }

    public bool IsCurrentFaceUsable()
    {
        return ValidateFaces() && LastFaceIndex >= 0 && LastFaceIndex < faces.Length && !faces[LastFaceIndex].broken;
    }

    public bool IsFaceBroken(int faceIndex)
    {
        if (!ValidateFaces() || faceIndex < 0 || faceIndex >= faces.Length)
            return false;

        return faces[faceIndex].broken;
    }

    public bool IsCurrentFaceBroken()
    {
        return ValidateFaces() && LastFaceIndex >= 0 && LastFaceIndex < faces.Length && faces[LastFaceIndex].broken;
    }

    public bool IsCurrentFaceNumeric()
    {
        return DiceFaceEnchantUtility.IsNumericFace(GetCurrentFaceEnchant());
    }

    public bool SetFaceBroken(int faceIndex, bool broken)
    {
        if (!ValidateFaces() || faceIndex < 0 || faceIndex >= faces.Length)
            return false;

        DiceFace face = faces[faceIndex];
        face.broken = broken;
        faces[faceIndex] = face;
        RefreshFaceValueText(faceIndex);
        if (faceIndex == LastFaceIndex)
            RefreshDisplayedState();
        return true;
    }

    public bool BreakCurrentFace()
    {
        if (!ValidateFaces() || LastFaceIndex < 0 || LastFaceIndex >= faces.Length)
            return false;

        return SetFaceBroken(LastFaceIndex, true);
    }

    private int RollWeightedFaceIndex()
    {
        if (!ValidateFaces())
            return 0;

        int totalWeight = 0;
        int[] weights = new int[faces.Length];
        for (int i = 0; i < faces.Length; i++)
        {
            int weight = 1 + CountGumSourcesForFace(i);
            weights[i] = Mathf.Max(0, weight);
            totalWeight += weights[i];
        }

        if (totalWeight <= 0)
            return Random.Range(0, faces.Length);

        int roll = Random.Range(0, totalWeight);
        for (int i = 0; i < weights.Length; i++)
        {
            if (roll < weights[i])
                return i;
            roll -= weights[i];
        }

        return weights.Length - 1;
    }

    private int CountGumSourcesForFace(int faceIndex)
    {
        int count = 0;
        for (int i = 0; i < faces.Length; i++)
        {
            if (faces[i].broken)
                continue;
            if (faces[i].enchant != DiceFaceEnchantKind.Gum)
                continue;
            if (GetOppositeFaceIndex(i) == faceIndex)
                count++;
        }

        return count;
    }

    private int GetOppositeFaceIndex(int faceIndex)
    {
        if (faces == null || faces.Length <= 0)
            return faceIndex;

        return Mathf.Clamp(faces.Length - 1 - faceIndex, 0, faces.Length - 1);
    }

}

using UnityEngine;
using DG.Tweening;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DiceSpinnerGeneric : MonoBehaviour
{
    public Transform pivot;
    public DiceFace[] faces;

    [Header("TextMeshPro")]
    public TMP_Text valueText;
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

    [Header("Loops (adds 360 degrees)")]
    public Vector3Int loopsMin = new Vector3Int(2, 2, 2);
    public Vector3Int loopsMax = new Vector3Int(5, 5, 5);
    [Range(0.05f, 0.6f)] public float accelPortion = 0.25f;

    private Tween _tween;

    public int LastRolledValue { get; private set; }
    public int LastFaceIndex { get; private set; } = -1;
    public bool IsRolling { get; private set; }

    public bool LastRollIsCrit => IsCritValue(LastRolledValue);
    public bool LastRollIsFail => IsFailValue(LastRolledValue);

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

    public int GetBaseValue() => LastRolledValue;

    public string GetRollStateLabel()
    {
        if (LastRollIsCrit)
        {
            if (!showAddedValueInRollState) return critText;
            int added = GetCritDisplayAddedValue(LastRolledValue);
            return $"{critText}: +{added}";
        }

        if (LastRollIsFail)
        {
            if (!showAddedValueInRollState) return failText;
            int added = GetFailDisplayAddedValueMagnitude(LastRolledValue);
            return $"{failText}: -{added}";
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
        if (rollStateText == null) return;
        rollStateText.text = GetRollStateLabel();
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
}

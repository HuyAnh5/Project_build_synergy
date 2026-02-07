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
    public TMP_Text valueText;              // kéo TextMeshProUGUI vào đây
    public string rollingText = "…";        // hiện khi đang quay
    public bool showResultAtStart = false;  // true: hiện số ngay khi bắt đầu; false: hiện lúc kết thúc

    [Header("Input")]
    public bool enableSpaceKey = true;
    public KeyCode rollKey = KeyCode.Space; // (Input System sẽ luôn dùng Space)

    [Header("Timing")]
    public float accelTime = 0.1f;
    public float totalTime = 1.5f;

    [Header("Loops (adds 360°)")]
    public Vector3Int loopsMin = new Vector3Int(2, 2, 2);
    public Vector3Int loopsMax = new Vector3Int(5, 5, 5);
    [Range(0.05f, 0.6f)] public float accelPortion = 0.25f;

    Tween _tween;

    public int LastRolledValue { get; private set; }
    public int LastFaceIndex { get; private set; } = -1;

    public bool IsRolling { get; private set; } = false;

    public System.Action<DiceSpinnerGeneric> onRollComplete;

    void Awake()
    {
        if (pivot == null) pivot = transform;
    }

    void Update()
    {
        if (!enableSpaceKey) return;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
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

        // chốt kết quả ngay từ đầu
        LastFaceIndex = faceIndex;
        LastRolledValue = faces[faceIndex].value;

        if (valueText != null)
        {
            valueText.text = rollingText;
            if (showResultAtStart) valueText.text = LastRolledValue.ToString();
        }

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

            onRollComplete?.Invoke(this);
        });

        _tween = seq;
    }

    bool ValidateFaces()
    {
        if (pivot == null) pivot = transform;

        if (faces == null || faces.Length == 0)
        {
            Debug.LogError($"{name}: faces is empty. Add face rotations in Inspector.");
            return false;
        }
        return true;
    }

    static Vector3 NormalizeEuler(Vector3 e)
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

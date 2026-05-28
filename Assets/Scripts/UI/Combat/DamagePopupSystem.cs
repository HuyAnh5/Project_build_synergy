using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

public class DamagePopupSystem : MonoBehaviour
{
    private sealed class TotalDamageState
    {
        public TMP_Text popup;
        public int total;
    }

    [Header("Prefab & Parent (UI or World)")]
    [Tooltip("Prefab nên là TextMeshProUGUI (UI) hoặc TMP_Text (world).")]
    public TMP_Text popupPrefab;

    [Tooltip("Nếu popup là UI TextMeshProUGUI: đặt spawnParent là RectTransform dưới Canvas (PopUpDMG). " +
             "Nếu để null: sẽ spawn theo transform của DamagePopupSystem.")]
    public Transform spawnParent;

    [Header("Pool")]
    public int prewarmCount = 30;
    public bool allowExpand = true;

    [Header("Scale (ALL types): normal -> bigger -> shrink+fade")]
    public float popUpScale = 1.25f;     // to lên
    public float popUpTime = 0.10f;
    public float shrinkTo = 0.75f;       // nhỏ dần
    public float shrinkTime = 0.70f;     // chậm hơn chút

    [Header("Timing")]
    public float hpDuration = 0.90f;
    public float guardDuration = 0.85f;
    public float totalDamageDuration = 1.10f;
    public float totalDamageFadeDuration = 0.22f;

    [Header("HP/Heal Arc (world or UI local units)")]
    public float arcUp = 0.45f;          // hất lên
    public float arcDown = 0.95f;        // rơi xuống
    public float arcSide = 0.35f;        // random trái/phải (biên độ)
    public float arcSideDrift = 0.10f;   // drift nhẹ theo thời gian

    [Header("Total Damage Float")]
    public float totalDamageUp = 0.45f;
    public float totalDamageStartScale = 1f;
    public float totalDamagePopScale = 1.25f;
    public float totalDamageSettleScale = 1f;

    [Header("Guard S-curve (world or UI local units)")]
    public float sUp = 1.00f;            // đi lên
    public float sAmp = 0.20f;           // độ uốn S
    public float sSideDrift = 0.12f;     // drift trái/phải

    [Header("Colors")]
    public Color hpColor = Color.white;
    public Color guardColor = new Color(0.3f, 0.75f, 1f, 1f);
    public Color totalDamageColor = new Color(1f, 0.82f, 0.22f, 1f);
    public Color healColor = new Color(0.25f, 1f, 0.35f, 1f);
    public Color focusGainColor = new Color(0.22f, 0.74f, 1f, 1f);

    [Header("Spawn Offsets")]
    [Tooltip("Điểm bắt đầu popup HP/Heal tính từ uiAnchor hoặc transform của target.")]
    public Vector3 hpSpawnOffset = new Vector3(0f, 1.2f, 0f);

    [Tooltip("Điểm bắt đầu popup Guard/Focus tính từ uiAnchor hoặc transform của target.")]
    public Vector3 guardSpawnOffset = new Vector3(-0.25f, 1f, 0f);

    [Tooltip("Điểm bắt đầu popup vàng tổng damage tính từ uiAnchor hoặc transform của target.")]
    public Vector3 totalDamageSpawnOffset = new Vector3(0f, 1.65f, 0f);

    [Header("Debug")]
    public bool logIfSpawnFails = true;

    private readonly Queue<TMP_Text> _pool = new Queue<TMP_Text>(64);
    private readonly Dictionary<CombatActor, TotalDamageState> _activeTotalDamagePopups = new Dictionary<CombatActor, TotalDamageState>();
    private float SecondPopupDelay => SkillExecutor.GlobalDelayedSecondaryStep;

    private void Awake()
    {
        Prewarm();
    }

    public void Prewarm()
    {
        if (popupPrefab == null)
        {
            if (logIfSpawnFails) Debug.LogWarning("[DamagePopupSystem] popupPrefab is NULL.", this);
            return;
        }

        while (_pool.Count < prewarmCount)
        {
            var t = Instantiate(popupPrefab, spawnParent != null ? spawnParent : transform);
            t.gameObject.SetActive(false);
            _pool.Enqueue(t);
        }
    }

    private TMP_Text Rent()
    {
        if (_pool.Count > 0) return _pool.Dequeue();
        if (!allowExpand) return null;

        if (popupPrefab == null) return null;
        var t = Instantiate(popupPrefab, spawnParent != null ? spawnParent : transform);
        t.gameObject.SetActive(false);
        return t;
    }

    private void Return(TMP_Text t)
    {
        if (t == null) return;

        t.DOKill();
        t.transform.DOKill();

        // reset alpha/scale for next use
        var c = t.color; c.a = 1f; t.color = c;
        t.transform.localScale = Vector3.one;

        t.gameObject.SetActive(false);
        _pool.Enqueue(t);
    }

    // ======================
    // PUBLIC API
    // ======================

    /// <summary>
    /// Spawn split: Guard trước (S), rồi HP sau (Arc).
    /// </summary>
    public void SpawnDamageSplit(CombatActor attacker, CombatActor target, int blocked, int hpLost)
    {
        if (target == null) return;

        if (blocked > 0)
            SpawnGuardSCurve(attacker, target, blocked.ToString());

        if (hpLost > 0)
        {
            SpawnTotalDamage(attacker, target, hpLost);

            if (blocked > 0)
            {
                DOVirtual.DelayedCall(SecondPopupDelay, () =>
                {
                    SpawnHpArc(attacker, target, hpLost.ToString(), hpColor);
                });
            }
            else
            {
                SpawnHpArc(attacker, target, hpLost.ToString(), hpColor);
            }
        }
    }

    /// <summary>
    /// Heal: giống HP Arc, chỉ khác màu, KHÔNG dấu +.
    /// </summary>
    public void SpawnHeal(CombatActor healer, CombatActor target, int amount)
    {
        if (target == null) return;
        int a = Mathf.Max(0, amount);
        if (a <= 0) return;

        SpawnHpArc(healer, target, a.ToString(), healColor);
    }

    public void SpawnFocusGain(CombatActor source, CombatActor target, int amount)
    {
        if (target == null) return;
        int a = Mathf.Max(0, amount);
        if (a <= 0) return;

        SpawnGuardSCurve(source, target, a.ToString(), focusGainColor);
    }

    // ======================
    // INTERNAL (ANIM)
    // ======================

    private enum PopupSpawnKind
    {
        Hp,
        Guard,
        TotalDamage
    }

    private Vector3 GetCenter(CombatActor target, PopupSpawnKind kind)
    {
        if (target == null) return Vector3.zero;

        Vector3 anchor = target.uiAnchor != null ? target.uiAnchor.position : target.transform.position;
        return anchor + GetSpawnOffset(kind);
    }

    private Vector3 GetSpawnOffset(PopupSpawnKind kind)
    {
        switch (kind)
        {
            case PopupSpawnKind.Guard:
                return guardSpawnOffset;
            case PopupSpawnKind.TotalDamage:
                return totalDamageSpawnOffset;
            case PopupSpawnKind.Hp:
            default:
                return hpSpawnOffset;
        }
    }

    private float RandomSideSign()
    {
        // random -1 or +1
        return Random.value < 0.5f ? -1f : 1f;
    }

    private void ApplyScalePopAndShrink(Sequence seq, Transform tr)
    {
        // start normal
        tr.localScale = Vector3.one;

        // to lên rồi shrink dần
        seq.Append(tr.DOScale(popUpScale, popUpTime).SetEase(Ease.OutBack));
        seq.Append(tr.DOScale(shrinkTo, shrinkTime).SetEase(Ease.InQuad));
    }

    /// <summary>
    /// HP/Heal: Arc rơi xuống (parabola) + random trái phải, đồng thời scale pop -> shrink + fade.
    /// </summary>
    private void SpawnHpArc(CombatActor source, CombatActor target, string text, Color color, PopupSpawnKind kind = PopupSpawnKind.Hp)
    {
        TMP_Text t = Rent();
        if (t == null) return;

        if (spawnParent != null)
            t.transform.SetParent(spawnParent, false);

        t.gameObject.SetActive(true);
        t.text = text;
        t.color = color;

        Vector3 start = GetCenter(target, kind);
        t.transform.position = start;

        float side = Random.value < 0.5f ? -1f : 1f;
        float duration = Mathf.Max(0.01f, hpDuration);

        // Single arc controlled only by total duration and spatial offsets.
        float peakX = side * (arcSide * 0.5f);
        float peakY = arcUp;
        float endX = side * (arcSide + arcSideDrift);
        float endY = -arcDown;
        Vector3 control = start + new Vector3(peakX, peakY, 0f);
        Vector3 end = start + new Vector3(endX, endY, 0f);

        Sequence seq = DOTween.Sequence();

        // Scale animation
        t.transform.localScale = Vector3.one;
        seq.Append(t.transform.DOScale(popUpScale, popUpTime).SetEase(Ease.OutBack));
        seq.Append(t.transform.DOScale(shrinkTo, shrinkTime).SetEase(Ease.InQuad));

        seq.Join(DOTween.To(() => 0f, v =>
        {
            float oneMinusT = 1f - v;
            Vector3 position =
                (oneMinusT * oneMinusT * start) +
                (2f * oneMinusT * v * control) +
                (v * v * end);

            t.transform.position = position;
        }, 1f, duration).SetEase(Ease.OutQuad));

        seq.Join(t.DOFade(0f, duration).SetEase(Ease.InQuad));

        seq.OnComplete(() => Return(t));
    }

    private void SpawnTotalDamage(CombatActor source, CombatActor target, int amount)
    {
        if (target == null) return;

        int safeAmount = Mathf.Max(0, amount);
        if (safeAmount <= 0) return;

        int total = safeAmount;
        if (_activeTotalDamagePopups.TryGetValue(target, out TotalDamageState activeState) && activeState != null)
        {
            total += Mathf.Max(0, activeState.total);
            if (activeState.popup != null)
                Return(activeState.popup);
            _activeTotalDamagePopups.Remove(target);
        }

        TMP_Text t = Rent();
        if (t == null) return;

        if (spawnParent != null)
            t.transform.SetParent(spawnParent, false);

        t.gameObject.SetActive(true);
        t.text = total.ToString();
        t.color = totalDamageColor;

        Vector3 start = GetCenter(target, PopupSpawnKind.TotalDamage);
        t.transform.position = start;

        float duration = Mathf.Max(0.01f, totalDamageDuration);
        float fadeDuration = Mathf.Clamp(totalDamageFadeDuration, 0.01f, duration);
        float fadeStart = Mathf.Max(0f, duration - fadeDuration);

        Sequence seq = DOTween.Sequence();
        t.transform.localScale = Vector3.one * Mathf.Max(0.01f, totalDamageStartScale);
        seq.Append(t.transform.DOScale(Mathf.Max(0.01f, totalDamagePopScale), popUpTime).SetEase(Ease.OutBack));
        seq.Append(t.transform.DOScale(Mathf.Max(0.01f, totalDamageSettleScale), shrinkTime).SetEase(Ease.OutQuad));
        seq.Join(t.transform.DOMoveY(start.y + totalDamageUp, duration).SetEase(Ease.OutQuad));
        seq.Join(t.DOFade(1f, 0f));
        if (fadeStart > 0f)
        {
            seq.Insert(fadeStart, t.DOFade(0f, fadeDuration).SetEase(Ease.InQuad));
        }
        else
        {
            seq.Join(t.DOFade(0f, fadeDuration).SetEase(Ease.InQuad));
        }
        TotalDamageState state = new TotalDamageState
        {
            popup = t,
            total = total
        };
        _activeTotalDamagePopups[target] = state;
        seq.OnComplete(() =>
        {
            if (_activeTotalDamagePopups.TryGetValue(target, out TotalDamageState current) &&
                current != null &&
                current.popup == t)
                _activeTotalDamagePopups.Remove(target);
            Return(t);
        });
    }

    /// <summary>
    /// Guard: S-curve đi lên + random trái phải, scale pop -> shrink + fade.
    /// </summary>
    private void SpawnGuardSCurve(CombatActor source, CombatActor target, string text)
    {
        SpawnGuardSCurve(source, target, text, guardColor);
    }

    private void SpawnGuardSCurve(CombatActor source, CombatActor target, string text, Color color)
    {
        if (popupPrefab == null)
        {
            if (logIfSpawnFails) Debug.LogWarning("[DamagePopupSystem] popupPrefab is NULL.", this);
            return;
        }

        TMP_Text t = Rent();
        if (t == null)
        {
            if (logIfSpawnFails) Debug.LogWarning("[DamagePopupSystem] pool empty and allowExpand=false.", this);
            return;
        }

        // ensure parent
        if (spawnParent != null) t.transform.SetParent(spawnParent, false);

        t.gameObject.SetActive(true);
        t.text = text;
        t.color = color;

        Vector3 start = GetCenter(target, PopupSpawnKind.Guard);
        t.transform.position = start;

        float side = RandomSideSign();
        float tv = 0f;

        Sequence seq = DOTween.Sequence();

        ApplyScalePopAndShrink(seq, t.transform);

        seq.Join(DOTween.To(() => tv, v =>
        {
            tv = v;

            // S-curve in X + drift
            float x = Mathf.Sin(tv * Mathf.PI * 2f) * sAmp + (sSideDrift * side) * tv;

            // go up
            float y = tv * sUp;

            t.transform.position = start + new Vector3(x, y, 0f);
        }, 1f, guardDuration).SetEase(Ease.OutQuad));

        seq.Join(t.DOFade(0f, guardDuration).SetEase(Ease.InQuad));

        seq.OnComplete(() => Return(t));
    }

}

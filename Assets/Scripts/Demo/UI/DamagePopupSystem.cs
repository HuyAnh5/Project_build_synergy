using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

public class DamagePopupSystem : MonoBehaviour
{
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
    public float secondPopupDelay = 0.08f;
    public float hpDuration = 0.90f;
    public float guardDuration = 0.85f;

    [Header("HP/Heal Arc (world or UI local units)")]
    public float arcUp = 0.45f;          // hất lên
    public float arcDown = 0.95f;        // rơi xuống
    public float arcSide = 0.35f;        // random trái/phải (biên độ)
    public float arcSideDrift = 0.10f;   // drift nhẹ theo thời gian

    [Header("Guard S-curve (world or UI local units)")]
    public float sUp = 1.00f;            // đi lên
    public float sAmp = 0.20f;           // độ uốn S
    public float sSideDrift = 0.12f;     // drift trái/phải

    [Header("Colors")]
    public Color hpColor = Color.white;
    public Color guardColor = new Color(0.3f, 0.75f, 1f, 1f);
    public Color healColor = new Color(0.25f, 1f, 0.35f, 1f);

    [Header("Debug")]
    public bool logIfSpawnFails = true;

    private readonly Queue<TMP_Text> _pool = new Queue<TMP_Text>(64);

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
            if (blocked > 0)
            {
                DOVirtual.DelayedCall(secondPopupDelay, () =>
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

    // ======================
    // INTERNAL (ANIM)
    // ======================

    private Vector3 GetCenter(CombatActor target)
    {
        if (target == null) return Vector3.zero;
        if (target.uiAnchor != null) return target.uiAnchor.position;
        return target.transform.position;
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
    private void SpawnHpArc(CombatActor source, CombatActor target, string text, Color color)
    {
        TMP_Text t = Rent();
        if (t == null) return;

        if (spawnParent != null)
            t.transform.SetParent(spawnParent, false);

        t.gameObject.SetActive(true);
        t.text = text;
        t.color = color;

        Vector3 start = GetCenter(target);
        t.transform.position = start;

        float side = Random.value < 0.5f ? -1f : 1f;

        float height = arcUp;          // độ cao tối đa
        float fallDistance = arcDown;  // độ rơi xuống
        float duration = hpDuration;

        float tv = 0f;

        Sequence seq = DOTween.Sequence();

        // Scale animation
        t.transform.localScale = Vector3.one;
        seq.Append(t.transform.DOScale(popUpScale, popUpTime).SetEase(Ease.OutBack));
        seq.Append(t.transform.DOScale(shrinkTo, shrinkTime).SetEase(Ease.InQuad));

        // Arc motion
        // Arc motion (UP then DROP below center)
        seq.Join(DOTween.To(() => tv, v =>
        {
            tv = v;

            // X drift trái/phải
            float x = side * arcSide * tv + side * arcSideDrift * (tv * tv);

            // Y: hất lên bằng sin(pi*t), rồi kéo rơi xuống tuyến tính theo arcDown
            // t=1 => y = -arcDown  (đúng ý: rơi xuống dưới tâm)
            float y = Mathf.Sin(tv * Mathf.PI) * height - (fallDistance * tv);

            t.transform.position = start + new Vector3(x, y, 0f);

        }, 1f, duration).SetEase(Ease.OutQuad));

        seq.Join(t.DOFade(0f, duration).SetEase(Ease.InQuad));

        seq.OnComplete(() => Return(t));
    }

    /// <summary>
    /// Guard: S-curve đi lên + random trái phải, scale pop -> shrink + fade.
    /// </summary>
    private void SpawnGuardSCurve(CombatActor source, CombatActor target, string text)
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
        t.color = guardColor;

        Vector3 start = GetCenter(target);
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
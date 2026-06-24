using DG.Tweening;
using TMPro;
using UnityEngine;

public partial class DamagePopupSystem
{
    private enum PopupSpawnKind
    {
        Hp,
        Guard,
        TotalDamage
    }

    private Vector3 GetCenter(CombatActor target, PopupSpawnKind kind)
    {
        if (target == null)
        {
            return Vector3.zero;
        }

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
        return Random.value < 0.5f ? -1f : 1f;
    }

    private void ApplyScalePopAndShrink(Sequence sequence, Transform targetTransform)
    {
        targetTransform.localScale = Vector3.one;
        sequence.Append(targetTransform.DOScale(popUpScale, popUpTime).SetEase(Ease.OutBack));
        sequence.Append(targetTransform.DOScale(shrinkTo, shrinkTime).SetEase(Ease.InQuad));
    }

    /// <summary>
    /// Spawns the arc motion used by HP loss and healing popups.
    /// </summary>
    private void SpawnHpArc(CombatActor source, CombatActor target, string text, Color color, PopupSpawnKind kind = PopupSpawnKind.Hp)
    {
        TMP_Text popup = Rent();
        if (popup == null)
        {
            return;
        }

        if (spawnParent != null)
        {
            popup.transform.SetParent(spawnParent, false);
        }

        popup.gameObject.SetActive(true);
        popup.raycastTarget = false;
        popup.text = text;
        popup.color = color;

        Vector3 start = GetCenter(target, kind);
        popup.transform.position = start;

        float side = RandomSideSign();
        float duration = Mathf.Max(0.01f, hpDuration);
        float peakX = side * (arcSide * 0.5f);
        float peakY = arcUp;
        float endX = side * (arcSide + arcSideDrift);
        float endY = -arcDown;
        Vector3 control = start + new Vector3(peakX, peakY, 0f);
        Vector3 end = start + new Vector3(endX, endY, 0f);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(popup.transform.DOScale(popUpScale, popUpTime).SetEase(Ease.OutBack));
        sequence.Append(popup.transform.DOScale(shrinkTo, shrinkTime).SetEase(Ease.InQuad));
        sequence.Join(DOTween.To(() => 0f, value =>
        {
            float oneMinusT = 1f - value;
            Vector3 position =
                (oneMinusT * oneMinusT * start) +
                (2f * oneMinusT * value * control) +
                (value * value * end);

            popup.transform.position = position;
        }, 1f, duration).SetEase(Ease.OutQuad));
        sequence.Join(popup.DOFade(0f, duration).SetEase(Ease.InQuad));
        sequence.OnComplete(() => Return(popup));
    }

    /// <summary>
    /// Spawns the yellow accumulated-damage popup shown after multiple hits land in quick succession.
    /// </summary>
    private void SpawnTotalDamage(CombatActor target, int amount)
    {
        if (target == null)
        {
            return;
        }

        int safeAmount = Mathf.Max(0, amount);
        if (safeAmount <= 0)
        {
            return;
        }

        m_activeTotalDamagePopups.TryGetValue(target, out TotalDamageState activeState);
        if (activeState != null && activeState.popup != null)
        {
            Return(activeState.popup);
        }

        TMP_Text popup = Rent();
        if (popup == null)
        {
            return;
        }

        if (spawnParent != null)
        {
            popup.transform.SetParent(spawnParent, false);
        }

        popup.gameObject.SetActive(true);
        popup.raycastTarget = false;
        popup.text = safeAmount.ToString();
        popup.color = totalDamageColor;

        Vector3 start = GetCenter(target, PopupSpawnKind.TotalDamage);
        popup.transform.position = start;

        float duration = Mathf.Max(0.01f, totalDamageDuration);
        float fadeDuration = Mathf.Clamp(totalDamageFadeDuration, 0.01f, duration);
        float fadeStart = Mathf.Max(0f, duration - fadeDuration);

        Sequence sequence = DOTween.Sequence();
        popup.transform.localScale = Vector3.one * Mathf.Max(0.01f, totalDamageStartScale);
        sequence.Append(popup.transform.DOScale(Mathf.Max(0.01f, totalDamagePopScale), popUpTime).SetEase(Ease.OutBack));
        sequence.Append(popup.transform.DOScale(Mathf.Max(0.01f, totalDamageSettleScale), shrinkTime).SetEase(Ease.OutQuad));
        sequence.Join(popup.transform.DOMoveY(start.y + totalDamageUp, duration).SetEase(Ease.OutQuad));
        sequence.Join(popup.DOFade(1f, 0f));

        if (fadeStart > 0f)
        {
            sequence.Insert(fadeStart, popup.DOFade(0f, fadeDuration).SetEase(Ease.InQuad));
        }
        else
        {
            sequence.Join(popup.DOFade(0f, fadeDuration).SetEase(Ease.InQuad));
        }

        TotalDamageState state = new TotalDamageState
        {
            popup = popup,
            total = activeState != null ? activeState.total : safeAmount,
            hitCount = activeState != null ? activeState.hitCount : 2,
            lastHitTime = activeState != null ? activeState.lastHitTime : Time.unscaledTime
        };
        m_activeTotalDamagePopups[target] = state;

        sequence.OnComplete(() =>
        {
            if (m_activeTotalDamagePopups.TryGetValue(target, out TotalDamageState current) &&
                current != null &&
                current.popup == popup)
            {
                current.popup = null;
                if (Time.unscaledTime - current.lastHitTime > Mathf.Max(0.01f, totalDamageDuration))
                {
                    m_activeTotalDamagePopups.Remove(target);
                }
            }

            Return(popup);
        });
    }

    /// <summary>
    /// Spawns the curved popup used by guard and focus-gain values.
    /// Guard changes can happen during enemy attacks, so this keeps the old
    /// S-curve feel without DOTween path allocation or per-frame trigonometry.
    /// </summary>
    private void SpawnGuardSCurve(CombatActor source, CombatActor target, string text)
    {
        SpawnGuardSCurve(source, target, text, guardColor);
    }

    private void SpawnGuardSCurve(CombatActor source, CombatActor target, string text, Color color)
    {
        if (popupPrefab == null)
        {
            LogMissingPrefabWarning();
            return;
        }

        TMP_Text popup = Rent();
        if (popup == null)
        {
            if (logIfSpawnFails)
            {
                Debug.LogWarning("[DamagePopupSystem] pool empty and allowExpand=false.", this);
            }

            return;
        }

        if (spawnParent != null)
        {
            popup.transform.SetParent(spawnParent, false);
        }

        popup.gameObject.SetActive(true);
        popup.raycastTarget = false;
        popup.text = text;
        popup.color = color;

        Vector3 start = GetCenter(target, PopupSpawnKind.Guard);
        popup.transform.position = start;

        float duration = Mathf.Max(0.01f, guardDuration);
        float side = RandomSideSign();
        float amp = sAmp * side;
        float drift = sSideDrift * side;

        Sequence sequence = DOTween.Sequence();
        ApplyScalePopAndShrink(sequence, popup.transform);
        sequence.Join(DOTween.To(() => 0f, value =>
        {
            float x = EvaluateGuardCurveX(value, amp, drift);
            float y = value * sUp;
            popup.transform.position = start + new Vector3(x, y, 0f);
        }, 1f, duration).SetEase(Ease.OutQuad));
        sequence.Join(popup.DOFade(0f, duration).SetEase(Ease.InQuad));
        sequence.OnComplete(() => Return(popup));
    }

    private static float EvaluateGuardCurveX(float t, float amp, float drift)
    {
        // Smooth cubic approximation of the old sine S-curve:
        // starts at 0, bends to one side, crosses back through center, then settles with drift.
        float centered = (t * 2f) - 1f;
        float sCurve = centered * (1f - (centered * centered));
        return (sCurve * amp) + (drift * t);
    }
}

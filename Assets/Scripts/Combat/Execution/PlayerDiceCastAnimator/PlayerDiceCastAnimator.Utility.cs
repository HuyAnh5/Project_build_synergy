using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public partial class PlayerDiceCastAnimator
{
    private Tween CreateQuadraticTween(
        Transform target,
        Vector3 p0,
        Vector3 pc,
        Vector3 p1,
        float duration,
        Quaternion rot0,
        Quaternion rot1)
    {
        if (target == null)
        {
            return null;
        }

        return RegisterTween(DOVirtual.Float(0f, 1f, Mathf.Max(0.01f, duration), t =>
        {
            float eased = DOVirtual.EasedValue(0f, 1f, t, Ease.InOutSine);
            target.position = EvaluateQuadratic(p0, pc, p1, eased);
            target.rotation = Quaternion.SlerpUnclamped(rot0, rot1, eased);
        }).SetEase(Ease.Linear));
    }

    private Vector3 ResolveEnemyImpactPoint(CombatActor primaryTarget, Vector3 fallback)
    {
        if (primaryTarget == null)
        {
            return fallback;
        }

        ActorWorldUI worldUi = primaryTarget.GetComponent<ActorWorldUI>();
        if (worldUi != null)
        {
            Transform uiTransform = worldUi.transform;
            return uiTransform.position + new Vector3(0f, 0.1f, 0f);
        }

        Transform targetTransform = primaryTarget.transform;
        return targetTransform != null
            ? targetTransform.position + new Vector3(0f, 0.1f, 0f)
            : fallback;
    }

    private Vector3 BuildArcControlPoint(Vector3 start, Vector3 end, float arcHeight, float forwardBias)
    {
        Vector3 midpoint = Vector3.Lerp(start, end, 0.5f);
        Vector3 travel = end - start;
        Vector3 planar = new Vector3(travel.x, 0f, travel.z);
        Vector3 forward = planar.sqrMagnitude > 0.0001f ? planar.normalized : Vector3.right;
        return midpoint + Vector3.up * arcHeight + forward * forwardBias;
    }

    private List<SlotVisualRefs> CollectSlotVisuals(DiceSlotRig diceRig, int start0, int span, int paymentMask = -1)
    {
        List<SlotVisualRefs> results = new List<SlotVisualRefs>();
        if (diceRig == null || diceRig.slots == null)
        {
            return results;
        }

        start0 = Mathf.Clamp(start0, 0, 2);
        span = Mathf.Clamp(span, 1, 3);

        int begin = paymentMask >= 0 ? 0 : start0;
        int end = paymentMask >= 0 ? diceRig.slots.Length : Mathf.Min(start0 + span, diceRig.slots.Length);
        for (int i = begin; i < end; i++)
        {
            if (paymentMask >= 0 && (paymentMask & (1 << i)) == 0)
                continue;

            DiceSlotRig.Entry entry = diceRig.slots[i];
            if (entry == null)
            {
                continue;
            }

            Transform dieRoot = entry.diceRoot != null
                ? entry.diceRoot.transform
                : (entry.dice != null ? entry.dice.transform : null);
            if (dieRoot == null)
            {
                continue;
            }

            Transform plateRoot = entry.slotRoot != null ? entry.slotRoot.transform : null;
            DiceSpinnerGeneric spinner = entry.dice != null ? entry.dice : dieRoot.GetComponentInChildren<DiceSpinnerGeneric>(true);
            results.Add(new SlotVisualRefs
            {
                dieRoot = dieRoot,
                plateRoot = plateRoot,
                spinner = spinner,
                spinnerTransform = spinner != null ? spinner.transform : dieRoot
            });
        }

        return results;
    }

    private void CacheTransformState(Transform target)
    {
        if (target == null || _baselineStates.ContainsKey(target))
        {
            return;
        }

        _baselineStates[target] = new TransformState
        {
            position = target.position,
            rotation = target.rotation,
            scale = target.localScale
        };
    }

    private TransformState GetTransformState(Transform target)
    {
        if (target == null)
        {
            return default;
        }

        if (_baselineStates.TryGetValue(target, out TransformState state))
        {
            return state;
        }

        state = new TransformState
        {
            position = target.position,
            rotation = target.rotation,
            scale = target.localScale
        };
        _baselineStates[target] = state;
        return state;
    }

    private Tween RegisterTween(Tween tween)
    {
        if (tween == null)
        {
            return null;
        }

        _activeTweens.Add(tween);
        tween.OnKill(() => _activeTweens.Remove(tween));
        return tween;
    }

    private void PlayTargetImpactFeedback(CombatActor actor, bool finalImpact)
    {
        if (actor == null)
        {
            return;
        }

        float strength = Mathf.Max(0f, impactPunchScale) * (finalImpact ? 1.35f : 0.8f);
        RegisterTween(actor.transform.DOPunchScale(Vector3.one * strength, impactPunchDuration, 1, 0.55f));
    }

    private Tween StartExistingSpinnerPresentationRoll(DiceSpinnerGeneric spinner, int orderIndex, float duration)
    {
        if (spinner == null)
        {
            return null;
        }

        Transform spinTarget = spinner.FlightSpinTarget;
        if (spinTarget == null)
        {
            return null;
        }

        spinTarget.DOKill(complete: false);

        Vector3 startEuler = spinTarget.localEulerAngles;
        Vector3 targetEuler = spinTarget == spinner.pivot
            ? ResolvePivotTargetEulerForCurrentFace(spinner, startEuler)
            : Vector3.zero;

        float safeDuration = Mathf.Max(0.02f, duration);
        float accelDuration = Mathf.Clamp(launchPrepDuration, 0.04f, safeDuration * 0.25f);
        float settleDuration = Mathf.Max(0.01f, safeDuration - accelDuration);

        int loopsX = spinner.loopsMin.x + (spinner.loopsMax.x - spinner.loopsMin.x) / 2 + (orderIndex % 2);
        int loopsY = spinner.loopsMin.y + (spinner.loopsMax.y - spinner.loopsMin.y) / 2 + ((orderIndex + 1) % 2);
        int loopsZ = spinner.loopsMin.z + (spinner.loopsMax.z - spinner.loopsMin.z) / 2 + ((orderIndex + 2) % 2);

        Vector3 endEuler = targetEuler + new Vector3(360f * loopsX, 360f * loopsY, 360f * loopsZ);
        Vector3 midEuler = Vector3.Lerp(startEuler, endEuler, spinner.accelPortion);

        Sequence seq = DOTween.Sequence();
        seq.Append(RegisterTween(spinTarget.DOLocalRotate(midEuler, accelDuration, RotateMode.FastBeyond360).SetEase(Ease.InQuad)));
        seq.Append(RegisterTween(spinTarget.DOLocalRotate(endEuler, settleDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuart)));
        return RegisterTween(seq);
    }

    private DiceDraggableUI ResolveDiceUi(DiceSpinnerGeneric spinner)
    {
        if (spinner == null)
        {
            return null;
        }

        if (_diceUiBySpinner.TryGetValue(spinner, out DiceDraggableUI cached) && cached != null && cached.dice == spinner)
        {
            return cached;
        }

        DiceDraggableUI[] allDiceUi = FindObjectsOfType<DiceDraggableUI>(true);
        for (int i = 0; i < allDiceUi.Length; i++)
        {
            DiceDraggableUI candidate = allDiceUi[i];
            if (candidate == null || candidate.dice != spinner)
            {
                continue;
            }

            _diceUiBySpinner[spinner] = candidate;
            return candidate;
        }

        _diceUiBySpinner.Remove(spinner);
        return null;
    }

    private static Vector3 ResolvePivotTargetEulerForCurrentFace(DiceSpinnerGeneric spinner, Vector3 fallback)
    {
        if (spinner == null || spinner.faces == null)
        {
            return fallback;
        }

        int faceIndex = spinner.LastFaceIndex;
        if (faceIndex < 0 || faceIndex >= spinner.faces.Length)
        {
            return fallback;
        }

        Vector3 euler = spinner.faces[faceIndex].localEuler;
        return new Vector3(NormalizeAngle(euler.x), NormalizeAngle(euler.y), NormalizeAngle(euler.z));
    }

    private static float NormalizeAngle(float value)
    {
        value %= 360f;
        if (value < 0f)
        {
            value += 360f;
        }

        return value;
    }

    private void ReleaseWorldSyncForCast(Transform die, Transform plate)
    {
        TryReleaseWorldSyncRoot(die);
        if (plate != null && plate != die)
        {
            TryReleaseWorldSyncRoot(plate);
        }
    }

    private void RestoreWorldSyncForCast(Transform die, Transform plate)
    {
        TryRestoreWorldSyncRoot(die);
        if (plate != null && plate != die)
        {
            TryRestoreWorldSyncRoot(plate);
        }
    }

    private void TryReleaseWorldSyncRoot(Transform root)
    {
        if (root == null || !_temporarilyReleasedWorldSyncRoots.Add(root))
        {
            return;
        }

        DiceEquipWorldSyncUtility.BeginTemporaryRelease(root);
    }

    private void TryRestoreWorldSyncRoot(Transform root)
    {
        if (root == null || !_temporarilyReleasedWorldSyncRoots.Remove(root))
        {
            return;
        }

        DiceEquipWorldSyncUtility.EndTemporaryRelease(root);
    }

    private void RestoreDetachedTransform(Transform target, DetachedTransformState state)
    {
        if (target == null)
        {
            return;
        }

        target.SetParent(state.parent, true);

        if (state.parent != null)
        {
            int maxIndex = Mathf.Max(0, state.parent.childCount - 1);
            target.SetSiblingIndex(Mathf.Clamp(state.siblingIndex, 0, maxIndex));
        }

        target.localPosition = state.localPosition;
        target.localRotation = state.localRotation;
        target.localScale = state.localScale;
    }

    private static void SetRenderableVisibility(Transform root, bool visible)
    {
        if (root == null)
        {
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = visible;
            }
        }
    }

    private void PlayAoeTargetFeedback(IReadOnlyList<CombatActor> aoeTargets, CombatActor primaryTarget)
    {
        if (aoeTargets == null)
        {
            return;
        }

        for (int i = 0; i < aoeTargets.Count; i++)
        {
            CombatActor actor = aoeTargets[i];
            if (actor == null || actor.IsDead || actor == primaryTarget)
            {
                continue;
            }

            RegisterTween(actor.transform.DOPunchScale(Vector3.one * (impactPunchScale * 0.75f), impactPunchDuration * 0.9f, 1, 0.5f).SetDelay(i * 0.03f));
        }
    }

    private void PlayPlateCatch(Transform plate)
    {
        if (plate == null)
        {
            return;
        }

        float strength = Mathf.Max(0f, plateCatchPunchScale);
        if (strength <= 0f)
        {
            return;
        }

        RegisterTween(plate.DOPunchScale(Vector3.one * strength, plateCatchPunchDuration, 1, 0.6f));
    }

    private void SnapToUsedState(Transform die, Transform plate, TransformState dieBase, TransformState plateBase)
    {
        if (die == null)
        {
            return;
        }

        die.position = dieBase.position + Vector3.down * usedDropDistance;
        die.rotation = dieBase.rotation;

        if (plate != null && plate != die)
        {
            plate.position = plateBase.position + Vector3.down * (usedDropDistance * 0.7f);
            plate.rotation = plateBase.rotation;
        }
    }

    private static Vector3 EvaluateQuadratic(Vector3 p0, Vector3 pc, Vector3 p1, float t)
    {
        float u = 1f - t;
        return (u * u * p0) + (2f * u * t * pc) + (t * t * p1);
    }
}

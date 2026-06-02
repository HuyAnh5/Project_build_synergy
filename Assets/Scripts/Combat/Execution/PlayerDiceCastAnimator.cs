using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public partial class PlayerDiceCastAnimator : MonoBehaviour
{
    public enum CastMode
    {
        Self,
        EnemySingle,
        EnemyAoeAnchor
    }

    [Header("Shared Timing")]
    [Min(0f)] public float interDieDelay = 0.12f;
    [Min(0.01f)] public float launchPrepDuration = 0.10f;
    [Min(0f)] public float launchPrepLift = 0.18f;
    [Min(0f)] public float usedDropDistance = 0.24f;
    [Min(0.01f)] public float usedDropDuration = 0.10f;
    [Range(0f, 1f)] public float liftCommitPortionBeforeThrow = 0.1f;
    [Range(0f, 1f)] public float returnCatchOverlapPortion = 0.22f;

    [Header("Enemy Throw")]
    [Min(0.01f)] public float throwDuration = 0.24f;
    [Min(0.01f)] public float returnDuration = 0.41f;
    [Min(0f)] public float outwardArcHeight = 0.38f;
    [Min(0f)] public float returnArcHeight = 4.8f;
    [Min(0f)] public float impactHoldDuration = 0.03f;
    [Min(0.01f)] public float enemyImpactScale = 0.5f;
    [Min(0.01f)] public float enemyReturnPeakScale = 1.5f;

    [Header("Self Slam")]
    [Min(0.01f)] public float selfHopDuration = 0.10f;
    [Min(0.01f)] public float selfSlamDuration = 0.16f;
    [Min(0f)] public float selfHopHeight = 1.5f;

    [Header("Impact Feel")]
    [Min(0f)] public float impactPunchScale = 0.08f;
    [Min(0.01f)] public float impactPunchDuration = 0.16f;
    [Min(0f)] public float plateCatchPunchScale = 0.12f;
    [Min(0.01f)] public float plateCatchPunchDuration = 0.18f;

    private struct TransformState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    private sealed class SlotVisualRefs
    {
        public Transform dieRoot;
        public Transform plateRoot;
        public DiceSpinnerGeneric spinner;
        public Transform spinnerTransform;
    }

    private struct DetachedTransformState
    {
        public Transform parent;
        public int siblingIndex;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
        public Vector3 worldScale;
    }

    private readonly Dictionary<Transform, TransformState> _baselineStates = new Dictionary<Transform, TransformState>();
    private readonly Dictionary<Transform, DetachedTransformState> _detachedStates = new Dictionary<Transform, DetachedTransformState>();
    private readonly Dictionary<DiceSpinnerGeneric, DiceDraggableUI> _diceUiBySpinner = new Dictionary<DiceSpinnerGeneric, DiceDraggableUI>();
    private readonly List<Tween> _activeTweens = new List<Tween>();
    private readonly List<GameObject> _activeProxyObjects = new List<GameObject>();
    private readonly HashSet<Transform> _hiddenOriginalRoots = new HashSet<Transform>();
    private readonly HashSet<Transform> _temporarilyReleasedWorldSyncRoots = new HashSet<Transform>();

    public IEnumerator PlayCast(
        DiceSlotRig diceRig,
        int start0,
        int span,
        int paymentMask,
        CombatActor caster,
        CombatActor primaryTarget,
        IReadOnlyList<CombatActor> aoeTargets,
        CastMode mode,
        Action onFinalImpact)
    {
        List<SlotVisualRefs> slots = CollectSlotVisuals(diceRig, start0, span, paymentMask);
        if (slots.Count == 0)
        {
            onFinalImpact?.Invoke();
            yield break;
        }

        int completedCount = 0;
        bool finalImpactTriggered = false;

        for (int i = 0; i < slots.Count; i++)
        {
            SlotVisualRefs slot = slots[i];
            int orderIndex = i;
            bool isFinal = i == slots.Count - 1;
            StartCoroutine(AnimateSlotRoutine(
                slot,
                orderIndex,
                isFinal,
                caster,
                primaryTarget,
                aoeTargets,
                mode,
                () =>
                {
                    if (isFinal && !finalImpactTriggered)
                    {
                        finalImpactTriggered = true;
                        onFinalImpact?.Invoke();
                    }
                },
                () => completedCount++));
        }

        while (completedCount < slots.Count)
        {
            yield return null;
        }
    }

    public void ResetAllVisualState()
    {
        for (int i = _activeTweens.Count - 1; i >= 0; i--)
        {
            Tween tween = _activeTweens[i];
            if (tween != null && tween.active)
            {
                tween.Kill(false);
            }
        }

        _activeTweens.Clear();

        for (int i = _activeProxyObjects.Count - 1; i >= 0; i--)
        {
            GameObject proxy = _activeProxyObjects[i];
            if (proxy != null)
            {
                Destroy(proxy);
            }
        }

        _activeProxyObjects.Clear();

        foreach (Transform releasedRoot in _temporarilyReleasedWorldSyncRoots)
        {
            DiceEquipWorldSyncUtility.EndTemporaryRelease(releasedRoot);
        }

        _temporarilyReleasedWorldSyncRoots.Clear();

        foreach (KeyValuePair<Transform, DetachedTransformState> kvp in _detachedStates)
        {
            RestoreDetachedTransform(kvp.Key, kvp.Value);
        }

        _detachedStates.Clear();

        foreach (Transform hiddenRoot in _hiddenOriginalRoots)
        {
            SetRenderableVisibility(hiddenRoot, true);
        }

        _hiddenOriginalRoots.Clear();

        foreach (KeyValuePair<Transform, TransformState> kvp in _baselineStates)
        {
            Transform target = kvp.Key;
            if (target == null)
            {
                continue;
            }

            TransformState state = kvp.Value;
            target.position = state.position;
            target.rotation = state.rotation;
            target.localScale = state.scale;
        }

        _baselineStates.Clear();
    }

    private IEnumerator AnimateSlotRoutine(
        SlotVisualRefs slot,
        int orderIndex,
        bool isFinal,
        CombatActor caster,
        CombatActor primaryTarget,
        IReadOnlyList<CombatActor> aoeTargets,
        CastMode mode,
        Action onImpact,
        Action onComplete)
    {
        if (slot == null || slot.dieRoot == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        if (orderIndex > 0 && interDieDelay > 0f)
        {
            yield return new WaitForSeconds(interDieDelay * orderIndex);
        }

        CacheTransformState(slot.dieRoot);
        if (slot.plateRoot != null && slot.plateRoot != slot.dieRoot)
        {
            CacheTransformState(slot.plateRoot);
        }

        if (mode == CastMode.Self)
        {
            yield return AnimateSelfSlam(slot, isFinal, caster, onImpact);
        }
        else
        {
            yield return AnimateEnemyThrow(slot, orderIndex, isFinal, primaryTarget, aoeTargets, mode, onImpact);
        }

        onComplete?.Invoke();
    }
}

using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public partial class PlayerDiceCastAnimator
{
    private IEnumerator AnimateSelfSlam(
        SlotVisualRefs slot,
        bool isFinal,
        CombatActor caster,
        Action onImpact)
    {
        Transform die = slot.dieRoot;
        Transform plate = slot.plateRoot;
        DiceSpinnerGeneric spinner = slot.spinner;
        DiceDraggableUI diceUi = ResolveDiceUi(spinner);
        bool uiCardControlsLiftDrop = diceUi != null;
        bool plateCarriesDie = plate != null && plate != die && die != null && die.IsChildOf(plate);
        Tween uiLiftTween = null;
        Tween plateLiftTween = null;
        float liftLeadTime = Mathf.Clamp(launchPrepDuration * Mathf.Clamp01(liftCommitPortionBeforeThrow), 0f, launchPrepDuration);

        if (uiCardControlsLiftDrop)
        {
            uiLiftTween = diceUi.AnimateCastDisplayToReady(launchPrepDuration, Ease.OutBack);
        }

        if (!uiCardControlsLiftDrop && plateCarriesDie)
        {
            Vector3 plateReadyPosition = plate.position + Vector3.up * (usedDropDistance * 0.7f);
            plateLiftTween = RegisterTween(plate.DOMove(plateReadyPosition, launchPrepDuration).SetEase(Ease.OutBack));
        }

        if (liftLeadTime > 0f)
        {
            yield return new WaitForSeconds(liftLeadTime);
        }

        TransformState dieBase = GetTransformState(die);
        TransformState plateBase = plate != null && plate != die ? GetTransformState(plate) : default;
        ReleaseWorldSyncForCast(die, plate);

        Vector3 apex = dieBase.position + Vector3.up * selfHopHeight;
        Tween hopTween = RegisterTween(die.DOMoveY(apex.y, selfHopDuration).SetEase(Ease.OutQuad));
        yield return hopTween.WaitForCompletion();

        Vector3 slamTarget = dieBase.position + Vector3.down * usedDropDistance;
        Tween slamTween = RegisterTween(die.DOMoveY(slamTarget.y, selfSlamDuration).SetEase(Ease.InQuad));

        float returnOverlapLead = Mathf.Clamp(selfSlamDuration * Mathf.Clamp01(returnCatchOverlapPortion), 0f, selfSlamDuration);
        float slamPreCatchTime = Mathf.Max(0f, selfSlamDuration - returnOverlapLead);
        if (slamPreCatchTime > 0f)
        {
            yield return new WaitForSeconds(slamPreCatchTime);
        }

        Tween dropTween = null;
        if (uiCardControlsLiftDrop)
        {
            RestoreWorldSyncForCast(die, plate);
            dropTween = diceUi.AnimateCastDisplayToSpent(usedDropDuration, Ease.InOutQuad);
        }
        else if (plate != null && plate != die)
        {
            Vector3 plateUsed = plateBase.position + Vector3.down * (usedDropDistance * 0.7f);
            PlayPlateCatch(plate);
            dropTween = RegisterTween(plate.DOMoveY(plateUsed.y, usedDropDuration).SetEase(Ease.InOutQuad));
        }

        yield return slamTween.WaitForCompletion();

        PlayTargetImpactFeedback(caster, isFinal);
        if (isFinal)
        {
            onImpact?.Invoke();
        }

        die.position = slamTarget;
        die.rotation = dieBase.rotation;

        if (uiLiftTween != null && uiLiftTween.active)
        {
            yield return uiLiftTween.WaitForCompletion();
        }

        if (plateLiftTween != null && plateLiftTween.active)
        {
            yield return plateLiftTween.WaitForCompletion();
        }

        if (dropTween != null && dropTween.active)
        {
            yield return dropTween.WaitForCompletion();
        }

        if (uiCardControlsLiftDrop)
        {
            diceUi.EndCastMotionLock();
        }
        else
        {
            RestoreWorldSyncForCast(die, plate);
        }
    }
}

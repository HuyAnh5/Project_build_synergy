using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public partial class PlayerDiceCastAnimator
{
    private IEnumerator AnimateEnemyThrow(
        SlotVisualRefs slot,
        int orderIndex,
        bool isFinal,
        CombatActor primaryTarget,
        IReadOnlyList<CombatActor> aoeTargets,
        CastMode mode,
        Action onImpact)
    {
        Transform die = slot.dieRoot;
        Transform plate = slot.plateRoot;
        DiceSpinnerGeneric spinner = slot.spinner;
        Transform moveTarget = spinner != null && spinner.pivot != null ? spinner.pivot : die;
        DiceDraggableUI diceUi = ResolveDiceUi(spinner);
        bool uiCardControlsLiftDrop = diceUi != null;
        float liftLeadTime = Mathf.Clamp(launchPrepDuration * Mathf.Clamp01(liftCommitPortionBeforeThrow), 0f, launchPrepDuration);

        if (uiCardControlsLiftDrop)
        {
            diceUi.AnimateCastDisplayToReady(launchPrepDuration, Ease.OutBack);
        }

        bool plateCarriesDie = plate != null && plate != die && die != null && die.IsChildOf(plate);
        Vector3 plateReadyPosition = plate != null && plate != die
            ? plate.position + Vector3.up * (usedDropDistance * 0.7f)
            : Vector3.zero;

        if (!uiCardControlsLiftDrop && plateCarriesDie)
        {
            RegisterTween(plate.DOMove(plateReadyPosition, launchPrepDuration).SetEase(Ease.OutBack));
        }

        if (liftLeadTime > 0f)
        {
            yield return new WaitForSeconds(liftLeadTime);
        }

        TransformState dieBase = GetTransformState(die);
        TransformState plateBase = plate != null && plate != die ? GetTransformState(plate) : default;
        TransformState moveBase = GetTransformState(moveTarget);
        if (plate != null && plate != die)
        {
            plateReadyPosition = plateBase.position + Vector3.up * (usedDropDistance * 0.7f);
        }

        ReleaseWorldSyncForCast(die, plate);

        float totalRollDuration = throwDuration + Mathf.Max(0f, impactHoldDuration) + returnDuration;
        Tween rollTween = StartExistingSpinnerPresentationRoll(spinner, orderIndex, totalRollDuration);

        Vector3 impactPoint = ResolveEnemyImpactPoint(primaryTarget, moveBase.position);
        Vector3 outwardControl = BuildArcControlPoint(moveBase.position, impactPoint, outwardArcHeight, 0f);
        Quaternion moveRotation = moveBase.rotation;

        Tween outwardTween = CreateQuadraticTween(
            moveTarget,
            moveBase.position,
            outwardControl,
            impactPoint,
            throwDuration,
            moveRotation,
            moveRotation);

        if (outwardTween != null)
        {
            yield return outwardTween.WaitForCompletion();
        }

        PlayTargetImpactFeedback(primaryTarget, isFinal);
        if (mode == CastMode.EnemyAoeAnchor && isFinal)
        {
            PlayAoeTargetFeedback(aoeTargets, primaryTarget);
        }

        if (isFinal)
        {
            onImpact?.Invoke();
        }

        if (impactHoldDuration > 0f)
        {
            yield return new WaitForSeconds(impactHoldDuration);
        }

        Vector3 returnControl = BuildArcControlPoint(impactPoint, moveBase.position, returnArcHeight, 0f);
        Tween returnTween = CreateQuadraticTween(
            moveTarget,
            impactPoint,
            returnControl,
            moveBase.position,
            returnDuration,
            moveRotation,
            moveRotation);

        float returnOverlapLead = Mathf.Clamp(returnDuration * Mathf.Clamp01(returnCatchOverlapPortion), 0f, returnDuration);
        float returnPreCatchTime = Mathf.Max(0f, returnDuration - returnOverlapLead);
        if (returnPreCatchTime > 0f)
        {
            yield return new WaitForSeconds(returnPreCatchTime);
        }

        Tween dropTween = null;
        if (uiCardControlsLiftDrop)
        {
            RestoreWorldSyncForCast(die, plate);
            dropTween = diceUi.AnimateCastDisplayToSpent(usedDropDuration, Ease.InOutQuad);
        }
        else if (plateCarriesDie)
        {
            PlayPlateCatch(plate);
            dropTween = RegisterTween(plate.DOMove(plateBase.position, usedDropDuration).SetEase(Ease.InOutQuad));
        }

        if (returnTween != null)
        {
            yield return returnTween.WaitForCompletion();
        }

        if (rollTween != null && rollTween.active)
        {
            yield return rollTween.WaitForCompletion();
        }

        if (dropTween != null && dropTween.active)
        {
            yield return dropTween.WaitForCompletion();
        }

        if (!plateCarriesDie)
        {
            moveTarget.position = moveBase.position;
            moveTarget.rotation = moveBase.rotation;
            die.position = dieBase.position;
            die.rotation = dieBase.rotation;
        }

        if (plate != null && plate != die)
        {
            plate.position = plateCarriesDie ? plateReadyPosition : plateBase.position;
            plate.rotation = plateBase.rotation;
        }

        if (uiCardControlsLiftDrop)
        {
            diceUi.EndCastMotionLock();
        }
        else if (plateCarriesDie)
        {
            RestoreWorldSyncForCast(die, plate);
        }
        else
        {
            RestoreWorldSyncForCast(die, plate);
            SnapToUsedState(die, plate, dieBase, plateBase);
        }
    }
}

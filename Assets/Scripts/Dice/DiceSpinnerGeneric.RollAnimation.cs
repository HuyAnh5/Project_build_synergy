using UnityEngine;
using DG.Tweening;

public partial class DiceSpinnerGeneric
{
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
}

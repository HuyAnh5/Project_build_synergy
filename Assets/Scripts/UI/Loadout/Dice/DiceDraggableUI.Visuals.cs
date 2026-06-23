using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public partial class DiceDraggableUI
{
    private static Color WithRenderableAlpha(Color color)
    {
        color.a = Mathf.Clamp01(color.a);
        return color;
    }

    private void AnimateToAnchoredHome(Transform parent, Vector2 anchoredPos, bool instant)
    {
        EnsureInitialized();

        RectTransform targetParent = parent as RectTransform;
        Vector2 target = GetDisplayAnchoredPosition(anchoredPos);
        float duration = GetSnapDuration();
        _homeAnchoredPos = anchoredPos;

        KillTweens();

        if (targetParent == null)
        {
            if (parent != null)
                _rt.SetParent(parent, worldPositionStays: false);

            if (instant)
            {
                _rt.anchoredPosition = target;
                _rt.localScale = Vector3.one;
            }
            else
            {
                _moveTween = _rt.DOAnchorPos(target, duration).SetEase(snapMoveEase).SetUpdate(true);
                _scaleTween = _rt.DOScale(1f, duration).SetEase(snapScaleEase).SetUpdate(true);
            }

            RefreshVisualState();
            return;
        }

        if (instant)
        {
            _rt.SetParent(targetParent, worldPositionStays: false);
            _rt.anchoredPosition = target;
            _rt.localScale = Vector3.one;
            RefreshVisualState();
            return;
        }

        if (_rt.parent != targetParent)
            _rt.SetParent(targetParent, worldPositionStays: true);

        _moveTween = _rt.DOAnchorPos(target, duration).SetEase(snapMoveEase).SetUpdate(true);
        _scaleTween = _rt.DOScale(1f, duration).SetEase(snapScaleEase).SetUpdate(true);
        RefreshVisualState();
    }

    private float GetSnapDuration()
    {
        return Mathf.Max(0.22f, tweenDuration);
    }

    public void BeginCastMotionLock()
    {
        EnsureInitialized();
        _castMotionLocked = true;
        KillMoveTweens();
        _castYOffsetOverride = _rt.anchoredPosition.y - _homeAnchoredPos.y;
        if (_cg != null)
            _cg.blocksRaycasts = false;
    }

    public void EndCastMotionLock()
    {
        EnsureInitialized();
        _castMotionLocked = false;
        _castYOffsetOverride = null;
        if (_cg != null && !_dragging)
            _cg.blocksRaycasts = true;
    }

    public Tween AnimateCastDisplayToReady(float duration, Ease ease)
        => AnimateCastDisplayToYOffset(0f, duration, ease);

    public Tween BeginCastLaunch(float duration, Ease ease)
    {
        EnsureInitialized();
        float currentYOffset = _rt.anchoredPosition.y - _homeAnchoredPos.y;
        float spentThreshold = -Mathf.Max(1f, spentDropY * 0.5f);

        if (currentYOffset <= spentThreshold)
            return AnimateCastDisplayToReady(duration, ease);

        // The first cast already starts at preview/selected y+.
        // Hold it there so its eventual spent transition is y+ -> y-.
        BeginCastMotionLock();
        return null;
    }

    public Tween AnimateCastDisplayToSpent(float duration, Ease ease)
        => AnimateCastDisplayToYOffset(-spentDropY, duration, ease);

    private Tween AnimateCastDisplayToYOffset(float targetYOffset, float duration, Ease ease)
    {
        EnsureInitialized();
        BeginCastMotionLock();

        Vector2 target = _homeAnchoredPos + new Vector2(0f, targetYOffset);
        if (duration <= 0f)
        {
            _castYOffsetOverride = targetYOffset;
            _rt.anchoredPosition = target;
            return null;
        }

        KillMoveTweens();
        _moveTween = _rt.DOAnchorPos(target, duration)
            .SetEase(ease)
            .SetUpdate(true)
            .OnUpdate(() => _castYOffsetOverride = _rt.anchoredPosition.y - _homeAnchoredPos.y)
            .OnComplete(() =>
            {
                _castYOffsetOverride = targetYOffset;
                _moveTween = null;
            });
        return _moveTween;
    }

    private Vector2 GetDisplayAnchoredPosition(Vector2 anchoredPos)
    {
        if (_castMotionLocked && _castYOffsetOverride.HasValue)
            return anchoredPos + new Vector2(0f, _castYOffsetOverride.Value);

        float yOffset = _spent
            ? -spentDropY
            : ((_previewSpentLike || _selected) ? selectedLiftY : 0f);
        return anchoredPos + new Vector2(0f, yOffset);
    }

    private void RefreshVisualState()
    {
        bool showFail = !_spent && (_fail || _previewFail);
        bool showCrit = !_spent && (_crit || _previewCrit) && !showFail;
        SyncWorldMeshResultOutline(showCrit, showFail);

        if (backgroundImage != null && (_backgroundColorTween == null || !_backgroundColorTween.IsActive()))
        {
            if (!_hasPreviewTint)
            {
                backgroundImage.color = WithRenderableAlpha(GetBaseBackgroundColor());
            }
        }

        if (outlineEffect != null)
        {
            if (_spent)
            {
                outlineEffect.enabled = false;
                return;
            }

            if (_hasPreviewTint && _forceHideOutline)
            {
                outlineEffect.enabled = false;
                return;
            }

            outlineEffect.effectDistance = outlineDistance;
            outlineEffect.useGraphicAlpha = false;
            bool useUiResultOutline = enableResultOutlineOnUi;
            if (useUiResultOutline && showFail)
            {
                outlineEffect.enabled = true;
                outlineEffect.effectColor = failOutlineColor;
            }
            else if (useUiResultOutline && showCrit)
            {
                outlineEffect.enabled = true;
                outlineEffect.effectColor = critOutlineColor;
            }
            else
            {
                outlineEffect.enabled = false;
            }
        }

        if (!_dragging && _cg != null)
            _cg.alpha = _hasPreviewTint ? 1f : _restingAlpha;
    }

    private void MoveToDisplayPosition(bool instant)
    {
        if (_castMotionLocked)
            return;

        KillMoveTweens();
        Vector2 target = GetDisplayAnchoredPosition(_homeAnchoredPos);
        if (instant)
            _rt.anchoredPosition = target;
        else
            _moveTween = _rt.DOAnchorPos(target, GetSnapDuration()).SetEase(snapMoveEase).SetUpdate(true);
    }

    private void KillMoveTweens()
    {
        _moveTween?.Kill();
        _shakeTween?.Kill();
    }

    private void PlayShake(Vector2 strength, float duration, int vibrato)
    {
        KillMoveTweens();
        _rt.anchoredPosition = GetDisplayAnchoredPosition(_homeAnchoredPos);
        _shakeTween = _rt.DOShakeAnchorPos(duration, strength, Mathf.Max(1, vibrato), randomness: 0f, snapping: false, fadeOut: true)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _rt.anchoredPosition = GetDisplayAnchoredPosition(_homeAnchoredPos);
                _shakeTween = null;
            });
    }

    private void FlashInvalidBackground()
    {
        if (backgroundImage == null)
            return;

        _backgroundColorTween?.Kill();
        Color baseColor = GetBaseBackgroundColor();
        backgroundImage.color = WithRenderableAlpha(baseColor);
        _backgroundColorTween = DOTween.Sequence()
            .Append(backgroundImage.DOColor(WithRenderableAlpha(invalidFlashColor), 0.08f).SetUpdate(true))
            .Append(backgroundImage.DOColor(WithRenderableAlpha(baseColor), 0.12f).SetUpdate(true))
            .OnComplete(() =>
            {
                _backgroundColorTween = null;
                RefreshVisualState();
            });
    }

    public void PlayTransientBuffFlash()
    {
        EnsureInitialized();
        if (backgroundImage == null)
            return;

        _backgroundColorTween?.Kill();
        Color baseColor = GetBaseBackgroundColor();
        backgroundImage.color = WithRenderableAlpha(baseColor);
        _backgroundColorTween = DOTween.Sequence()
            .Append(backgroundImage.DOColor(WithRenderableAlpha(transientBuffFlashColor), Mathf.Max(0.01f, transientBuffFlashInDuration)).SetUpdate(true))
            .Append(backgroundImage.DOColor(WithRenderableAlpha(baseColor), Mathf.Max(0.01f, transientBuffFlashOutDuration)).SetUpdate(true))
            .OnComplete(() =>
            {
                _backgroundColorTween = null;
                RefreshVisualState();
            });
    }

    private Color GetBaseBackgroundColor()
    {
        return _selected ? selectedBackgroundColor : _defaultBackgroundColor;
    }

    private void EnsureCritFailPopupAnchor()
    {
        if (_rt == null)
            return;

        if (critFailPopupAnchor == null)
        {
            Transform existing = _rt.Find(CritFailPopupAnchorName);
            if (existing != null)
                critFailPopupAnchor = existing as RectTransform;
        }

        if (critFailPopupAnchor != null)
            return;

        GameObject anchorGo = new GameObject(CritFailPopupAnchorName, typeof(RectTransform));
        anchorGo.layer = gameObject.layer;
        RectTransform anchor = anchorGo.GetComponent<RectTransform>();
        anchor.SetParent(_rt, false);
        anchor.anchorMin = new Vector2(0.5f, 1f);
        anchor.anchorMax = new Vector2(0.5f, 1f);
        anchor.pivot = new Vector2(0.5f, 0f);
        anchor.anchoredPosition = new Vector2(0f, 6f);
        anchor.sizeDelta = Vector2.zero;
        critFailPopupAnchor = anchor;
    }

    private bool _hasPreviewTint;
    private bool _forceHideOutline;

    /// <summary>
    /// Áp tint màu nhấp nháy lên background image khi dice này đang trong vùng consume preview.
    /// </summary>
    public void SetPreviewTint(Color tint, bool hideOutline = false)
    {
        EnsureInitialized();
        _hasPreviewTint = true;
        _forceHideOutline = hideOutline;
        if (backgroundImage != null)
            backgroundImage.color = WithRenderableAlpha(tint);

        RefreshVisualState();
    }

    public void SetPreviewSpentLike(bool active, bool instant = false, bool suppressMove = false)
    {
        EnsureInitialized();
        if (_previewSpentLike == active)
            return;

        _previewSpentLike = active;

        if (!_dragging && !_castMotionLocked && !suppressMove)
            MoveToDisplayPosition(instant);

        RefreshVisualState();
    }

    /// <summary>
    /// Xoá tint preview, trả background về trạng thái bình thường.
    /// </summary>
    public void ClearPreviewTint()
    {
        if (!_hasPreviewTint) return;
        _hasPreviewTint = false;
        _forceHideOutline = false;
        EnsureInitialized();
        RefreshVisualState();
    }

    public void ClearPreviewSpentLike(bool instant = false, bool suppressMove = false)
    {
        if (!_previewSpentLike)
            return;

        EnsureInitialized();
        _previewSpentLike = false;

        if (!_dragging && !_castMotionLocked && !suppressMove)
            MoveToDisplayPosition(instant);

        RefreshVisualState();
    }

    public void ClearPreviewRollFeedback()
    {
        if (!_previewCrit && !_previewFail)
            return;

        EnsureInitialized();
        _previewCrit = false;
        _previewFail = false;
        RefreshVisualState();
    }
}

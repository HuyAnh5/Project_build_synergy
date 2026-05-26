using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public partial class DiceDraggableUI
{
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

    private Vector2 GetDisplayAnchoredPosition(Vector2 anchoredPos)
    {
        float yOffset = _selected ? selectedLiftY : ((_spent || _previewSpentLike) ? -spentDropY : 0f);
        return anchoredPos + new Vector2(0f, yOffset);
    }

    private void RefreshVisualState()
    {
        if (backgroundImage != null && (_backgroundColorTween == null || !_backgroundColorTween.IsActive()))
        {
            if (!_hasPreviewTint)
            {
                backgroundImage.color = GetBaseBackgroundColor();
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
            if (enableResultOutlineOnUi && _fail)
            {
                outlineEffect.enabled = true;
                outlineEffect.effectColor = failOutlineColor;
            }
            else if (enableResultOutlineOnUi && _crit)
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
            _cg.alpha = _restingAlpha;
    }

    private void MoveToDisplayPosition(bool instant)
    {
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
        backgroundImage.color = baseColor;
        _backgroundColorTween = DOTween.Sequence()
            .Append(backgroundImage.DOColor(invalidFlashColor, 0.08f).SetUpdate(true))
            .Append(backgroundImage.DOColor(baseColor, 0.12f).SetUpdate(true))
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
            backgroundImage.color = tint;

        RefreshVisualState();
    }

    public void SetPreviewSpentLike(bool active)
    {
        EnsureInitialized();
        if (_previewSpentLike == active)
            return;

        _previewSpentLike = active;

        if (!_dragging)
            MoveToDisplayPosition(instant: false);

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

    public void ClearPreviewSpentLike()
    {
        if (!_previewSpentLike)
            return;

        EnsureInitialized();
        _previewSpentLike = false;

        if (!_dragging)
            MoveToDisplayPosition(instant: false);

        RefreshVisualState();
    }
}

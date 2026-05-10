using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class DiceDraggableUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private const string CritFailPopupAnchorName = "DiceCard_Pivot";

    public DiceSpinnerGeneric dice;
    [HideInInspector] public DiceEquipUIManager manager;

    [Header("Tween")]
    public float dragScale = 1.08f;
    public float tweenDuration = 0.18f;
    public Ease snapMoveEase = Ease.OutQuart;
    public Ease snapScaleEase = Ease.OutCubic;

    [Header("Selection")]
    public Image backgroundImage;
    public Color selectedBackgroundColor = new Color(1f, 0.84f, 0.2f, 1f);
    public float selectedLiftY = 14f;

    [Header("Combat State")]
    public float spentDropY = 20f;
    public bool enableResultOutlineOnUi = true;
    public Outline outlineEffect;
    public Color critOutlineColor = new Color(1f, 0.85f, 0.2f, 1f);
    public Color failOutlineColor = new Color(1f, 0.25f, 0.25f, 1f);
    public Color invalidFlashColor = new Color(1f, 0.35f, 0.35f, 1f);
    public Vector2 outlineDistance = new Vector2(6f, -6f);
    public float failShakeDuration = 0.16f;
    public Vector2 failShakeStrength = new Vector2(10f, 0f);
    public int failShakeVibrato = 16;
    public float invalidShakeDuration = 0.18f;
    public Vector2 invalidShakeStrength = new Vector2(14f, 0f);
    public int invalidShakeVibrato = 18;
    [SerializeField] private RectTransform critFailPopupAnchor;

    private RectTransform _rt;
    private Canvas _rootCanvas;
    private CanvasGroup _cg;
    private Color _defaultBackgroundColor = Color.white;
    private Transform _prevParent;
    private Vector2 _prevAnchoredPos;
    private Vector2 _homeAnchoredPos;
    private bool _dragging;
    private bool _selected;
    private Tween _moveTween;
    private Tween _scaleTween;
    private Tween _shakeTween;
    private Tween _backgroundColorTween;
    private Vector2 _dragPointerOffset;
    private float _restingAlpha = 1f;
    private bool _dragRegistered;
    private bool _spent;
    private bool _crit;
    private bool _fail;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (_rt != null)
            return;

        _rt = GetComponent<RectTransform>();
        _rootCanvas = GetComponentInParent<Canvas>();
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();
        if (outlineEffect == null) outlineEffect = GetComponent<Outline>();
        if (outlineEffect == null && backgroundImage != null) outlineEffect = gameObject.AddComponent<Outline>();
        if (backgroundImage != null) _defaultBackgroundColor = backgroundImage.color;
        if (outlineEffect != null)
        {
            outlineEffect.effectDistance = outlineDistance;
            outlineEffect.enabled = false;
        }
        EnsureCritFailPopupAnchor();
        _homeAnchoredPos = _rt.anchoredPosition;
    }

    public RectTransform GetCritFailPopupAnchor()
    {
        EnsureInitialized();
        return critFailPopupAnchor != null ? critFailPopupAnchor : _rt;
    }

    public void CacheHome()
    {
        EnsureInitialized();
        _prevParent = _rt.parent;
        _prevAnchoredPos = _homeAnchoredPos;
        _homeAnchoredPos = _prevAnchoredPos;
    }

    public void SetRestingAlpha(float alpha)
    {
        EnsureInitialized();
        _restingAlpha = Mathf.Clamp01(alpha);
        if (!_dragging && _cg != null)
            _cg.alpha = _restingAlpha;
    }

    public void ReturnToCachedHome()
    {
        EnsureInitialized();
        AnimateToAnchoredHome(_prevParent, _prevAnchoredPos, instant: false);
    }

    public void SnapToAnchorAnimated(Transform parent, Vector2 anchoredPos)
    {
        SnapToAnchorAnimated(parent, anchoredPos, instant: false);
    }

    public void SnapToAnchorAnimated(Transform parent, Vector2 anchoredPos, bool instant)
    {
        EnsureInitialized();
        AnimateToAnchoredHome(parent, anchoredPos, instant);
    }

    public void SetSelected(bool selected, bool instant = false)
    {
        EnsureInitialized();
        _selected = selected;
        RefreshVisualState();

        if (_dragging)
            return;

        MoveToDisplayPosition(instant);
    }

    public void SetCombatVisualState(bool spent, bool crit, bool fail, bool instant = false)
    {
        EnsureInitialized();
        bool failTriggered = !_fail && fail;
        bool changed = _spent != spent || _crit != crit || _fail != fail;

        _spent = spent;
        _crit = crit;
        _fail = fail;

        if (!changed)
            return;

        RefreshVisualState();

        if (_dragging)
            return;

        MoveToDisplayPosition(instant);
        if (failTriggered)
            PlayShake(failShakeStrength, failShakeDuration, failShakeVibrato);
    }

    public void PlayInvalidSelectionFeedback()
    {
        EnsureInitialized();
        if (_dragging)
            return;

        MoveToDisplayPosition(instant: true);
        PlayShake(invalidShakeStrength, invalidShakeDuration, invalidShakeVibrato);
        FlashInvalidBackground();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        EnsureInitialized();
        _dragging = false;
        if (manager == null) return;
        if (!manager.CanInteract()) return;

        CacheHome();
        _dragging = true;
        UiDragState.BeginDrag(this);
        _dragRegistered = true;
        manager.HandleDiceBeginDrag(this);

        KillTweens();

        RectTransform dragParent = manager.dragLayer != null
            ? manager.dragLayer
            : (manager.layoutContainer != null ? manager.layoutContainer : manager.transform as RectTransform);

        if (dragParent != null)
            _rt.SetParent(dragParent, worldPositionStays: true);

        _rt.SetAsLastSibling();
        _cg.blocksRaycasts = false;
        _cg.alpha = 0.92f;

        CachePointerOffset(eventData.position, eventData.pressEventCamera);
        MoveWithPointer(eventData.position, eventData.pressEventCamera);

        _scaleTween = _rt.DOScale(dragScale, 0.12f).SetEase(Ease.OutBack).SetUpdate(true);
        manager.NotifyBeginDrag(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        EnsureInitialized();
        if (!_dragging) return;

        if (manager != null && !manager.CanInteract())
        {
            _dragging = false;
            EndDragRegistration();
            _cg.blocksRaycasts = true;
            _cg.alpha = _restingAlpha;
            manager.HandleInvalidDrop(this);
            return;
        }

        MoveWithPointer(eventData.position, eventData.pressEventCamera);

        if (manager != null)
            manager.NotifyDrag(this, eventData.position, eventData.pressEventCamera);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EnsureInitialized();
        if (!_dragging)
        {
            EndDragRegistration();
            return;
        }

        _dragging = false;
        EndDragRegistration();
        _cg.blocksRaycasts = true;
        _cg.alpha = _restingAlpha;

        if (manager != null)
        {
            if (!manager.CanInteract())
            {
                manager.HandleInvalidDrop(this);
                return;
            }

            if (!manager.WasDropConsumedThisFrame)
                manager.NotifyEndDrag(this, eventData.position, eventData.pressEventCamera);
        }
        else
        {
            ReturnToCachedHome();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        EnsureInitialized();
        if (_dragging) return;
        if (manager == null) return;
        if (!manager.CanInteract()) return;
        manager.HandleDiceClicked(this);
    }

    private void OnDisable()
    {
        EndDragRegistration();
    }

    private void CachePointerOffset(Vector2 screenPos, Camera eventCamera)
    {
        EnsureInitialized();
        RectTransform parentRt = _rt.parent as RectTransform;
        if (parentRt == null)
        {
            _dragPointerOffset = Vector2.zero;
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRt, screenPos, eventCamera, out Vector2 local))
        {
            _dragPointerOffset = Vector2.zero;
            return;
        }

        _dragPointerOffset = local - _rt.anchoredPosition;
    }

    private void MoveWithPointer(Vector2 screenPos, Camera eventCamera)
    {
        EnsureInitialized();
        RectTransform parentRt = _rt.parent as RectTransform;
        if (parentRt == null) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRt, screenPos, eventCamera, out Vector2 local))
            return;

        _rt.anchoredPosition = local - _dragPointerOffset;
    }

    private void KillTweens()
    {
        _moveTween?.Kill();
        _scaleTween?.Kill();
        _shakeTween?.Kill();
    }

    private void EndDragRegistration()
    {
        if (!_dragRegistered)
            return;

        UiDragState.EndDrag(this);
        _dragRegistered = false;
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

    private Vector2 GetDisplayAnchoredPosition(Vector2 anchoredPos)
    {
        float yOffset = _selected ? selectedLiftY : (_spent ? -spentDropY : 0f);
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
}

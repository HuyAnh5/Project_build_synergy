using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class DiceDraggableUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
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
    private Vector2 _dragPointerOffset;
    private float _restingAlpha = 1f;
    private bool _dragRegistered;

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
        if (backgroundImage != null) _defaultBackgroundColor = backgroundImage.color;
        _homeAnchoredPos = _rt.anchoredPosition;
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

        KillTweens();
        Vector2 target = GetDisplayAnchoredPosition(_homeAnchoredPos);
        if (instant)
            _rt.anchoredPosition = target;
        else
            _moveTween = _rt.DOAnchorPos(target, GetSnapDuration()).SetEase(snapMoveEase).SetUpdate(true);
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
        return anchoredPos + new Vector2(0f, _selected ? selectedLiftY : 0f);
    }

    private void RefreshVisualState()
    {
        if (backgroundImage != null)
            backgroundImage.color = _selected ? selectedBackgroundColor : _defaultBackgroundColor;

        if (!_dragging && _cg != null)
            _cg.alpha = _restingAlpha;
    }
}

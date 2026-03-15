using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class PassiveDraggableUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public SkillPassiveSO passive;
    public Image iconImage;
    public TMP_Text nameText;

    [HideInInspector] public PassiveEquipUIManager manager;

    [Header("Tween")]
    public float dragScale = 1.06f;
    public float tweenDuration = 0.18f;

    private RectTransform _rt;
    private Canvas _rootCanvas;
    private CanvasGroup _cg;
    private Transform _prevParent;
    private Vector2 _prevAnchoredPos;
    private bool _dragging;
    private Tween _moveTween;
    private Tween _scaleTween;
    private Vector2 _dragPointerOffset;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _rootCanvas = GetComponentInParent<Canvas>();
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();

        RefreshVisual();
    }

    public void RefreshVisual()
    {
        if (iconImage != null)
        {
            iconImage.sprite = passive != null ? passive.icon : null;
            iconImage.enabled = iconImage.sprite != null;
            iconImage.raycastTarget = iconImage.sprite != null;
            iconImage.preserveAspect = true;
        }

        if (nameText != null)
            nameText.text = passive != null ? passive.displayName : string.Empty;
    }

    public void CacheHome()
    {
        _prevParent = _rt.parent;
        _prevAnchoredPos = _rt.anchoredPosition;
    }

    public void ReturnToCachedHome()
    {
        if (_prevParent != null)
            _rt.SetParent(_prevParent, worldPositionStays: true);

        KillTweens();
        _moveTween = _rt.DOAnchorPos(_prevAnchoredPos, tweenDuration).SetEase(Ease.OutCubic).SetUpdate(true);
        _scaleTween = _rt.DOScale(1f, tweenDuration).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void SnapToAnchorAnimated(Transform parent, Vector2 anchoredPos)
    {
        if (parent != null)
            _rt.SetParent(parent, worldPositionStays: true);

        KillTweens();
        _moveTween = _rt.DOAnchorPos(anchoredPos, tweenDuration).SetEase(Ease.OutCubic).SetUpdate(true);
        _scaleTween = _rt.DOScale(1f, tweenDuration).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _dragging = false;
        if (manager == null) return;
        if (!manager.CanInteract()) return;

        CacheHome();
        _dragging = true;

        KillTweens();

        RectTransform dragParent = manager.dragLayer != null
            ? manager.dragLayer
            : manager.transform as RectTransform;

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
        if (!_dragging) return;

        MoveWithPointer(eventData.position, eventData.pressEventCamera);

        if (manager != null)
            manager.NotifyDrag(this, eventData.position, eventData.pressEventCamera);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_dragging) return;

        _dragging = false;
        _cg.blocksRaycasts = true;
        _cg.alpha = 1f;

        if (manager != null)
        {
            if (!manager.WasDropConsumedThisFrame)
                manager.NotifyEndDrag(this, eventData.position, eventData.pressEventCamera);
        }
        else
        {
            ReturnToCachedHome();
        }
    }

    private void CachePointerOffset(Vector2 screenPos, Camera eventCamera)
    {
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
}

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

    public float tweenDuration = 0.18f;

    private RectTransform _rt;
    private Transform _prevParent;
    private Vector2 _prevAnchoredPos;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        if (manager == null) manager = GetComponentInParent<PassiveEquipUIManager>();

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
            _rt.SetParent(_prevParent, worldPositionStays: false);

        _rt.anchoredPosition = _prevAnchoredPos;
        _rt.localScale = Vector3.one;
    }

    public void SnapToAnchorAnimated(Transform parent, Vector2 anchoredPos)
    {
        if (parent != null)
            _rt.SetParent(parent, worldPositionStays: false);

        _rt.anchoredPosition = anchoredPos;
        _rt.localScale = Vector3.one;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        ReturnToCachedHome();
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        ReturnToCachedHome();
    }
}

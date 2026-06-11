using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class DiceDraggableUI
{
    internal void HandleEnchantHoverEnter()
    {
        EnsureInitialized();
        _enchantHoverTooltipActive = true;
        RefreshHoverTooltip();
    }

    internal void HandleEnchantHoverExit()
    {
        _enchantHoverTooltipActive = false;
        SkillTooltipUI.HideCurrentUnlessPointerOverTooltip(gameObject);
    }

    private void EnsureEnchantHoverZone()
    {
        if (_rt == null)
            return;

        RectTransform parent = ResolveEnchantHoverZoneParent();
        if (parent == null)
            return;

        if (_enchantHoverZone == null)
        {
            Transform existing = parent.Find(GetEnchantHoverZoneName());
            if (existing != null)
                _enchantHoverZone = existing as RectTransform;
        }

        if (_enchantHoverZone == null)
        {
            GameObject zoneGo = new GameObject(GetEnchantHoverZoneName(), typeof(RectTransform), typeof(Image), typeof(DiceEnchantHoverProxy));
            zoneGo.layer = gameObject.layer;
            _enchantHoverZone = zoneGo.GetComponent<RectTransform>();
            _enchantHoverZone.SetParent(parent, false);
        }

        if (_enchantHoverZone.parent != parent)
            _enchantHoverZone.SetParent(parent, false);

        _enchantHoverZone.anchorMin = new Vector2(0.5f, 0.5f);
        _enchantHoverZone.anchorMax = new Vector2(0.5f, 0.5f);
        _enchantHoverZone.pivot = new Vector2(0.5f, 0.5f);
        _enchantHoverZone.SetAsLastSibling();

        _enchantHoverZoneImage = _enchantHoverZone.GetComponent<Image>();
        if (_enchantHoverZoneImage == null)
            _enchantHoverZoneImage = _enchantHoverZone.gameObject.AddComponent<Image>();

        _enchantHoverZoneImage.color = new Color(1f, 1f, 1f, 0f);
        _enchantHoverZoneImage.raycastTarget = true;

        DiceEnchantHoverProxy proxy = _enchantHoverZone.GetComponent<DiceEnchantHoverProxy>();
        if (proxy == null)
            proxy = _enchantHoverZone.gameObject.AddComponent<DiceEnchantHoverProxy>();
        proxy.Bind(this);
        _enchantHoverZone.gameObject.SetActive(false);
    }

    private void UpdateEnchantHoverZone()
    {
        if (_enchantHoverZone == null)
            return;

        _enchantHoverZoneFaceIndex = -1;
        RectTransform zoneParent = ResolveEnchantHoverZoneParent();
        if (_dragging || UiDragState.IsDragging || dice == null || _rt == null || !_rt.gameObject.activeInHierarchy)
        {
            _enchantHoverZone.gameObject.SetActive(false);
            return;
        }

        if (zoneParent == null)
        {
            _enchantHoverZone.gameObject.SetActive(false);
            return;
        }

        if (_enchantHoverZone.parent != zoneParent)
            _enchantHoverZone.SetParent(zoneParent, false);

        int faceIndex = ResolveTooltipFaceIndex();
        if (faceIndex < 0)
        {
            _enchantHoverZone.gameObject.SetActive(false);
            return;
        }

        if (!TryGetFaceEnchantScreenRectNearCard(faceIndex, out Rect screenRect))
        {
            _enchantHoverZone.gameObject.SetActive(false);
            return;
        }

        Camera eventCamera = GetCanvasEventCamera(_rootCanvas);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(zoneParent, screenRect.center, eventCamera, out Vector2 localCenter) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(zoneParent, screenRect.min, eventCamera, out Vector2 localMin) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(zoneParent, screenRect.max, eventCamera, out Vector2 localMax))
        {
            _enchantHoverZone.gameObject.SetActive(false);
            return;
        }

        Vector2 size = new Vector2(Mathf.Abs(localMax.x - localMin.x), Mathf.Abs(localMax.y - localMin.y));
        if (size.x <= 1f || size.y <= 1f)
        {
            _enchantHoverZone.gameObject.SetActive(false);
            return;
        }

        _enchantHoverZoneFaceIndex = faceIndex;
        _enchantHoverZone.anchoredPosition = localCenter;
        _enchantHoverZone.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        _enchantHoverZone.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        _enchantHoverZone.SetAsLastSibling();
        _enchantHoverZone.gameObject.SetActive(true);
    }

    private RectTransform ResolveEnchantHoverZoneParent()
    {
        if (manager != null)
        {
            RectTransform hoverContainer = manager.GetDiceHoverZoneContainer();
            if (hoverContainer != null)
                return hoverContainer;
        }

        return _rt != null ? _rt.parent as RectTransform : null;
    }

    private bool TryGetFaceEnchantScreenRectNearCard(int faceIndex, out Rect screenRect)
    {
        screenRect = default;
        if (dice == null || _rt == null)
            return false;

        Canvas canvas = _rootCanvas != null ? _rootCanvas : GetComponentInParent<Canvas>();
        Camera eventCamera = GetCanvasEventCamera(canvas);
        Rect cardScreenRect = BuildRectTransformScreenRect(_rt, eventCamera);
        Rect acceptedArea = ExpandRect(cardScreenRect, Mathf.Max(cardScreenRect.width, cardScreenRect.height));

        Camera primary = ResolveTooltipCamera();
        if (TryGetAcceptedFaceEnchantScreenRect(primary, faceIndex, acceptedArea, cardScreenRect, out screenRect))
            return true;

        Camera main = Camera.main;
        if (main != primary && TryGetAcceptedFaceEnchantScreenRect(main, faceIndex, acceptedArea, cardScreenRect, out screenRect))
            return true;

        if (eventCamera != null && eventCamera != primary && eventCamera != main &&
            TryGetAcceptedFaceEnchantScreenRect(eventCamera, faceIndex, acceptedArea, cardScreenRect, out screenRect))
        {
            return true;
        }

        return false;
    }

    private bool TryGetAcceptedFaceEnchantScreenRect(
        Camera camera,
        int faceIndex,
        Rect acceptedArea,
        Rect cardScreenRect,
        out Rect screenRect)
    {
        screenRect = default;
        if (camera == null || !dice.TryGetFaceEnchantScreenRect(camera, faceIndex, out Rect candidate))
            return false;

        if (candidate.width <= 0f || candidate.height <= 0f)
            return false;

        if (!acceptedArea.Contains(candidate.center))
            return false;

        float maxReasonableSize = Mathf.Max(cardScreenRect.width, cardScreenRect.height);
        if (candidate.width > maxReasonableSize || candidate.height > maxReasonableSize)
            candidate = ClampRectSize(candidate, maxReasonableSize);

        screenRect = candidate;
        return true;
    }

    private static Rect BuildRectTransformScreenRect(RectTransform rectTransform, Camera eventCamera)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        Vector2 first = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
        float minX = first.x;
        float minY = first.y;
        float maxX = first.x;
        float maxY = first.y;
        for (int i = 1; i < corners.Length; i++)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[i]);
            minX = Mathf.Min(minX, point.x);
            minY = Mathf.Min(minY, point.y);
            maxX = Mathf.Max(maxX, point.x);
            maxY = Mathf.Max(maxY, point.y);
        }

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    private static Rect ExpandRect(Rect rect, float amount)
    {
        rect.xMin -= amount;
        rect.xMax += amount;
        rect.yMin -= amount;
        rect.yMax += amount;
        return rect;
    }

    private static Rect ClampRectSize(Rect rect, float maxSize)
    {
        Vector2 center = rect.center;
        float width = Mathf.Min(rect.width, maxSize);
        float height = Mathf.Min(rect.height, maxSize);
        return new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
    }

    private string GetEnchantHoverZoneName()
    {
        return $"{EnchantHoverZoneName}_{GetInstanceID()}";
    }

    private static Camera GetCanvasEventCamera(Canvas canvas)
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }
}

public sealed class DiceEnchantHoverProxy : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private DiceDraggableUI _owner;

    public DiceDraggableUI Owner => _owner;

    public void Bind(DiceDraggableUI owner)
    {
        _owner = owner;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _owner?.HandleEnchantHoverEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _owner?.HandleEnchantHoverExit();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _owner?.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        _owner?.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _owner?.OnEndDrag(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _owner?.OnPointerClick(eventData);
    }
}

using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public partial class DiceDraggableUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, ISkillTooltipSource
{
    private const string CritFailPopupAnchorName = "DiceCard_Pivot";
    private const string EnchantHoverZoneName = "EnchantHoverZone";
    private static readonly Dictionary<DiceSpinnerGeneric, DiceDraggableUI> s_diceToUiMap = new();

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
    public Color transientBuffFlashColor = new Color(0.36f, 0.88f, 1f, 1f);
    public Vector2 outlineDistance = new Vector2(6f, -6f);
    public float failShakeDuration = 0.16f;
    public Vector2 failShakeStrength = new Vector2(10f, 0f);
    public int failShakeVibrato = 16;
    public float invalidShakeDuration = 0.18f;
    public Vector2 invalidShakeStrength = new Vector2(14f, 0f);
    public int invalidShakeVibrato = 18;
    public float transientBuffFlashInDuration = 0.06f;
    public float transientBuffFlashOutDuration = 0.12f;
    [SerializeField] private RectTransform critFailPopupAnchor;

    [Header("Dice Enchant Tooltip")]
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
    private bool _previewSpentLike;
    private bool _previewCrit;
    private bool _previewFail;
    private bool _castMotionLocked;
    private float? _castYOffsetOverride;
    private DiceSpinnerGeneric _registeredDice;
    private bool _cardHoverTooltipActive;
    private bool _enchantHoverTooltipActive;
    private RectTransform _enchantHoverZone;
    private Image _enchantHoverZoneImage;
    private int _enchantHoverZoneFaceIndex = -1;
    private DiceFaceEnchantTooltipAsset _hoverTooltipAsset;

    private void Awake()
    {
        EnsureInitialized();
        RegisterDiceBinding();
    }

    private void OnEnable()
    {
        EnsureInitialized();
        RegisterDiceBinding();
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
        RegisterDiceBinding();
    }

    public static bool TryGetRegisteredDiceUi(DiceSpinnerGeneric die, out DiceDraggableUI diceUi)
    {
        if (die == null)
        {
            diceUi = null;
            return false;
        }

        if (s_diceToUiMap.TryGetValue(die, out diceUi) && diceUi != null)
            return true;

        if (diceUi != null)
            s_diceToUiMap.Remove(die);

        diceUi = null;
        return false;
    }

    public RectTransform GetCritFailPopupAnchor()
    {
        EnsureInitialized();
        return critFailPopupAnchor != null ? critFailPopupAnchor : _rt;
    }

    public void EnsureManagedEnchantHoverZone()
    {
        EnsureInitialized();
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

        if (_castMotionLocked)
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

        if (_castMotionLocked)
            return;

        MoveToDisplayPosition(instant);
        if (failTriggered)
            PlayShake(failShakeStrength, failShakeDuration, failShakeVibrato);
    }

    public void SetPreviewRollFeedback(bool crit, bool fail)
    {
        EnsureInitialized();
        if (_previewCrit == crit && _previewFail == fail)
            return;

        _previewCrit = crit;
        _previewFail = fail;
        RefreshVisualState();
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        EnsureInitialized();
        _cardHoverTooltipActive = true;
        RefreshHoverTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _cardHoverTooltipActive = false;
        SkillTooltipUI.HideCurrentUnlessPointerOverTooltip(gameObject);
    }

    private void OnDisable()
    {
        _cardHoverTooltipActive = false;
        _enchantHoverTooltipActive = false;
        _enchantHoverZoneFaceIndex = -1;
        if (_enchantHoverZone != null)
            _enchantHoverZone.gameObject.SetActive(false);
        if (_hoverTooltipAsset != null)
            Destroy(_hoverTooltipAsset);
        EndDragRegistration();
        UnregisterDiceBinding();
    }

    private void OnDestroy()
    {
        if (_enchantHoverZone != null)
            Destroy(_enchantHoverZone.gameObject);
    }

    private void Update()
    {
        RefreshCardHoverTooltipStateFromPointer();
        if (IsHoverTooltipActive)
            RefreshHoverTooltip();
    }

    private void RefreshCardHoverTooltipStateFromPointer()
    {
        if (_rt == null || !gameObject.activeInHierarchy || UiDragState.IsDragging)
        {
            if (_cardHoverTooltipActive)
            {
                _cardHoverTooltipActive = false;
                SkillTooltipUI.HideCurrentUnlessPointerOverTooltip(gameObject);
            }
            return;
        }

        Canvas canvas = _rootCanvas != null ? _rootCanvas : GetComponentInParent<Canvas>();
        Camera eventCamera = GetCanvasEventCamera(canvas);
        bool pointerInside = RectTransformUtility.RectangleContainsScreenPoint(_rt, Input.mousePosition, eventCamera);
        if (pointerInside == _cardHoverTooltipActive)
            return;

        _cardHoverTooltipActive = pointerInside;
        if (_cardHoverTooltipActive)
            RefreshHoverTooltip();
        else
            SkillTooltipUI.HideCurrentUnlessPointerOverTooltip(gameObject);
    }

    private bool IsHoverTooltipActive => _cardHoverTooltipActive || _enchantHoverTooltipActive;

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

    private void RegisterDiceBinding()
    {
        if (_registeredDice != null && _registeredDice != dice)
            s_diceToUiMap.Remove(_registeredDice);

        _registeredDice = dice;
        if (_registeredDice != null)
            s_diceToUiMap[_registeredDice] = this;
    }

    private void UnregisterDiceBinding()
    {
        if (_registeredDice == null)
            return;

        if (s_diceToUiMap.TryGetValue(_registeredDice, out DiceDraggableUI registered) && registered == this)
            s_diceToUiMap.Remove(_registeredDice);

        _registeredDice = null;
    }

    private void RefreshHoverTooltip()
    {
        if (!IsHoverTooltipActive || dice == null || UiDragState.IsDragging)
        {
            SkillTooltipUI.HideCurrentUnlessPointerOverTooltip(gameObject);
            return;
        }

        int faceIndex = ResolveTooltipEnchantFaceIndex();

        if (faceIndex < 0)
        {
            SkillTooltipUI.HideCurrentUnlessPointerOverTooltip(gameObject);
            return;
        }

        DiceFace face = dice.GetFace(faceIndex);
        DiceFaceEnchantKind displayedEnchant = dice.GetDisplayedFaceEnchant(faceIndex);
        bool showBrokenTooltip = face.broken;
        if ((!showBrokenTooltip && face.broken) || (!face.broken && !DiceFaceEnchantUtility.HasEnchant(displayedEnchant)))
        {
            SkillTooltipUI.HideCurrentUnlessPointerOverTooltip(gameObject);
            return;
        }

        if (_hoverTooltipAsset == null)
        {
            _hoverTooltipAsset = ScriptableObject.CreateInstance<DiceFaceEnchantTooltipAsset>();
            _hoverTooltipAsset.hideFlags = HideFlags.HideAndDontSave;
        }

        _hoverTooltipAsset.Configure(displayedEnchant, face.value, dice.name, face.broken);
        Canvas canvas = _rootCanvas != null ? SkillTooltipUI.GetOrCreateSharedOverlayCanvas(_rootCanvas) : null;
        if (canvas == null || _rt == null)
        {
            SkillTooltipUI.HideCurrentUnlessPointerOverTooltip(gameObject);
            return;
        }

        EnsureEnchantHoverZone();
        UpdateEnchantHoverZone();
        if (_enchantHoverZone == null || !_enchantHoverZone.gameObject.activeInHierarchy)
        {
            SkillTooltipUI.HideCurrentUnlessPointerOverTooltip(gameObject);
            return;
        }

        SkillTooltipUI.Show(canvas, _enchantHoverZone, _hoverTooltipAsset);
    }

    private int ResolveTooltipFaceIndex()
    {
        if (dice == null)
            return -1;

        if (dice.LastFaceIndex >= 0)
            return dice.LastFaceIndex;

        Camera cam = ResolveTooltipCamera();
        return cam != null ? dice.GetBestFacingFaceIndex(cam) : -1;
    }

    private int ResolveTooltipEnchantFaceIndex()
    {
        int lastFaceIndex = dice != null ? dice.LastFaceIndex : -1;
        if (HasTooltipEnchant(lastFaceIndex))
            return lastFaceIndex;

        Camera cam = ResolveTooltipCamera();
        int facingFaceIndex = cam != null ? dice.GetBestFacingFaceIndex(cam) : -1;
        if (HasTooltipEnchant(facingFaceIndex))
            return facingFaceIndex;

        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam != cam)
        {
            int mainFacingFaceIndex = dice.GetBestFacingFaceIndex(mainCam);
            if (HasTooltipEnchant(mainFacingFaceIndex))
                return mainFacingFaceIndex;
        }

        return lastFaceIndex >= 0 ? lastFaceIndex : facingFaceIndex;
    }

    private bool HasTooltipEnchant(int faceIndex)
    {
        if (dice == null || faceIndex < 0)
            return false;

        DiceFace face = dice.GetFace(faceIndex);
        if (face.broken)
            return true;

        return DiceFaceEnchantUtility.HasEnchant(dice.GetDisplayedFaceEnchant(faceIndex));
    }

    private bool TryResolveHoveredEnchantFace(out int faceIndex)
    {
        faceIndex = -1;
        if (dice == null)
            return false;

        Camera cam = ResolveTooltipCamera();
        if (cam != null && dice.TryResolveHoveredEnchantFace(cam, out faceIndex, out _))
            return true;

        Camera mainCam = Camera.main;
        return mainCam != null &&
               mainCam != cam &&
               dice.TryResolveHoveredEnchantFace(mainCam, out faceIndex, out _);
    }

    private Camera ResolveTooltipCamera()
    {
        Camera cam = manager != null ? manager.GetDiceWorldHoverCamera() : null;
        return cam != null ? cam : Camera.main;
    }

    public bool TryGetSkillTooltip(out Canvas canvas, out RectTransform target, out ScriptableObject asset, out SkillRuntime runtime)
    {
        EnsureInitialized();
        canvas = _rootCanvas != null ? SkillTooltipUI.GetOrCreateSharedOverlayCanvas(_rootCanvas) : null;
        target = _enchantHoverZone != null && _enchantHoverZone.gameObject.activeInHierarchy ? _enchantHoverZone : null;
        asset = _hoverTooltipAsset;
        runtime = null;
        return canvas != null && target != null && asset != null;
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

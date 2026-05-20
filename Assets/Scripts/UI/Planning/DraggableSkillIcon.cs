using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI icon for a skill. Source of truth should be RunInventoryManager.
/// - If Bind To Inventory Slot = true: skill is resolved from inventory (Fixed/Owned + index)
/// - Else: use Skill Asset Override (single ScriptableObject)
///
/// Supports drag/click equip for active skills (SkillDamageSO / SkillBuffDebuffSO).
/// Passive (SkillPassiveSO) is NOT draggable and NOT click-to-equip.
/// </summary>
public class DraggableSkillIcon : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, ISkillTooltipSource
{
    private const string FocusBadgeName = "FocusCostBadge";
    private const string SlotBadgeName = "SlotCostBadge";
    private const string ElementBadgeName = "ElementBadge";

    [Title("Source")]
    [Tooltip("If enabled, this icon always reads the skill from RunInventoryManager (Fixed/Owned slot).")]
    [SerializeField] private bool bindToInventorySlot = true;

    [ShowIf(nameof(bindToInventorySlot))]
    [SerializeField] private RunInventoryManager inventory;

    public enum InventorySkillSource { Fixed, Owned }

    [ShowIf(nameof(bindToInventorySlot))]
    [SerializeField] private InventorySkillSource inventorySource = InventorySkillSource.Owned;

    [ShowIf(nameof(bindToInventorySlot))]
    [Min(0)]
    [SerializeField] private int inventoryIndex = 0;

    [HideIf(nameof(bindToInventorySlot))]
    [Tooltip("Used only when not bound to inventory. Single reference, no legacy/new split.")]
    [SerializeField] private ScriptableObject skillAssetOverride;

    [Title("Turn")]
    [SerializeField] private TurnManager turn;

    [Title("Visual")]
    [Range(0f, 1f)]
    [SerializeField] private float inUseAlpha = 0.6f;
    [Range(0f, 1f)]
    [SerializeField] private float unavailableAlpha = 0.4f;
    [SerializeField] private float invalidDropReturnDuration = 0.16f;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private SelfCastDropZone selfCastZone;
    [SerializeField] private SkillUiIconLibrarySO iconLibrary;
    [SerializeField] private Image focusCostBadgeBackground;
    [SerializeField] private TMP_Text focusCostBadgeText;
    [SerializeField] private Image slotCostBadgeBackground;
    [SerializeField] private Image slotCostBadgeIcon;
    [SerializeField] private TMP_Text slotCostBadgeText;
    [SerializeField] private Image elementBadgeBackground;
    [SerializeField] private Image elementBadgeIcon;
    [SerializeField] private Image skillBackgroundImage;
    [SerializeField] private SkillSlotLayout skillSlotLayout;

    private static SkillUiIconLibrarySO _sharedIconLibrary;

    private Canvas _canvas;
    private RectTransform _canvasRT;
    private Camera _uiCam;
    private Image _img;
    private CanvasGroup _cg;
    private RectTransform _ghostRT;
    private ScriptableObject _resolvedAsset;
    private bool _dropAccepted;
    private Vector2 _ghostHomeAnchoredPos;
    private bool _inUse;
    private bool _castable = true;
    private bool _dragRegistered;
    private ScriptableObject _lastVisualAsset;
    private Sprite _lastVisualIcon;
    private string _lastVisualName;
    private int _lastVisualFocusCost = int.MinValue;
    private int _lastVisualSlotsRequired = int.MinValue;
    private bool _lastVisualHasElement;
    private ElementType _lastVisualElement = ElementType.Neutral;

    // --- Resource Preview ---
    private CombatHUD _cachedHud;
    private bool _resourcePreviewActive;

    // --- Target Preview (Drag) ---
    private ActorWorldUI _currentPreviewTarget;
    private SkillRuntime _cachedDragRuntime;

    // --- Click-to-Select ---
    private bool _selected;
    private Coroutine _blinkCoroutine;
    private static readonly Color SelectedBlinkColorA = new Color(1f, 0.92f, 0.3f, 1f);  // đỉnh sáng
    private static readonly Color SelectedBlinkColorB = new Color(1f, 0.65f, 0.1f, 0.5f); // đỉnh mờ

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        _canvasRT = _canvas.transform as RectTransform;
        _uiCam = (_canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _canvas.worldCamera;

        _img = GetComponent<Image>();
        _cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        if (skillSlotLayout == null)
            skillSlotLayout = GetComponent<SkillSlotLayout>();
        ApplyLayoutBindings();
        if (nameText == null)
            nameText = GetComponentInChildren<TMP_Text>(includeInactive: true);
        if (skillBackgroundImage == null)
            skillBackgroundImage = GetComponent<Image>();
        if (selfCastZone == null)
            selfCastZone = FindObjectOfType<SelfCastDropZone>(true);
        if (iconLibrary != null)
            _sharedIconLibrary = iconLibrary;

        EnsureCostBadgeUi();
        Refresh();
        SetInUse(false);
        SetCastable(true);
    }

    private void OnEnable()
    {
        if (bindToInventorySlot && inventory != null)
            inventory.InventoryChanged += OnInventoryChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.InventoryChanged -= OnInventoryChanged;

        if (_dragRegistered)
        {
            UiDragState.EndDrag(this);
            _dragRegistered = false;
        }

        // Deselect nếu icon bị disable
        if (_selected)
            UiDragState.DeselectSkill();

        StopBlinkCoroutine();
        SkillTooltipUI.HideCurrent();
    }

    private void OnInventoryChanged()
    {
        Refresh();
    }

    public bool IsPassive
    {
        get
        {
            var a = GetSkillAsset();
            return a is SkillPassiveSO;
        }
    }

    public ScriptableObject GetSkillAsset()
    {
        if (bindToInventorySlot && inventory != null)
        {
            var src = (inventorySource == InventorySkillSource.Fixed)
                ? RunInventoryManager.SkillSource.Fixed
                : RunInventoryManager.SkillSource.Owned;

            _resolvedAsset = inventory.GetSkill(src, inventoryIndex);
            return _resolvedAsset;
        }

        _resolvedAsset = skillAssetOverride;
        return _resolvedAsset;
    }

    private Sprite GetIcon()
    {
        var a = GetSkillAsset();
        if (a is SkillDamageSO ds) return ds.icon;
        if (a is SkillBuffDebuffSO bd) return bd.icon;
        if (a is SkillPassiveSO ps) return ps.icon;
        return null;
    }

    public void Refresh()
    {
        EnsureCostBadgeUi();
        if (_img != null)
        {
            _img.sprite = GetIcon();
            _img.preserveAspect = true;
        }
        RefreshLabel();
        RefreshCostBadges();
        RefreshElementBadge();
        ApplyVisualState();
        CaptureVisualSnapshot();
    }

    public void SetInUse(bool inUse)
    {
        _inUse = inUse;
        ApplyVisualState();
    }

    public void SetCastable(bool castable)
    {
        _castable = castable;
        ApplyVisualState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (UiDragState.IsDragging)
            return;

        ScriptableObject asset = GetSkillAsset();
        if (asset == null)
            return;

        SkillTooltipUI.Show(this);
        ShowResourcePreview(asset);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (UiDragState.IsDragging)
            return;

        SkillTooltipUI.HideCurrentUnlessPointerOverTooltip(eventData != null ? eventData.pointerCurrentRaycast.gameObject : null);

        DraggableSkillIcon selected = UiDragState.SelectedSkill;

        // Nếu bản thân đang được chọn, KHÔNG BAO GIỜ clear resource preview khi chuột rời đi
        if (selected == this)
            return;

        // Nếu có skill khác đang được chọn, khôi phục preview của nó
        if (selected != null)
        {
            ScriptableObject selectedAsset = selected.GetSkillAsset();
            if (selectedAsset != null)
                selected.ShowResourcePreview(selectedAsset);
            return;
        }

        // Nếu không có gì được chọn, clear bình thường
        ClearResourcePreview();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!turn || !turn.CanInteractWithSkills) return;
        var a = GetSkillAsset();
        if (a == null || a is SkillPassiveSO) return;

        if (!CanDragCurrentSkill())
        {
            RejectActionFeedback();
            return;
        }

        // Toggle select/deselect
        if (_selected)
        {
            UiDragState.DeselectSkill();
        }
        else
        {
            UiDragState.SelectSkill(this);
        }
    }

    // ---------------------------
    // Click-to-Select API (called by UiDragState)
    // ---------------------------

    public void OnSelected()
    {
        _selected = true;
        StartBlinkCoroutine();
        var a = GetSkillAsset();
        if (a != null)
        {
            _cachedDragRuntime = null; // reset để rebuild
            ShowResourcePreview(a);
        }
    }

    public void OnDeselected()
    {
        _selected = false;
        StopBlinkCoroutine();
        ClearResourcePreview();
        ClearTargetPreviewIfActive();
        if (_img != null)
            _img.color = Color.white;
    }

    private void StartBlinkCoroutine()
    {
        StopBlinkCoroutine();
        _blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    private void StopBlinkCoroutine()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }
    }

    private System.Collections.IEnumerator BlinkRoutine()
    {
        while (_selected)
        {
            float t = Mathf.PingPong(UnityEngine.Time.time * 3f, 1f);
            if (_img != null)
                _img.color = Color.Lerp(SelectedBlinkColorB, SelectedBlinkColorA, t);
            yield return null;
        }
    }

    private void RejectActionFeedback()
    {
        transform.DOKill(complete: true);
        transform.DOShakePosition(0.3f, new Vector3(10f, 0, 0), 30, 90f, false, true).SetUpdate(true);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!turn || !turn.CanInteractWithSkills) return;

        var a = GetSkillAsset();
        if (a == null) return;
        if (a is SkillPassiveSO) return;
        
        if (!CanDragCurrentSkill())
        {
            RejectActionFeedback();
            return;
        }

        // Deselect click-to-select nếu đang selected khi bắt đầu drag
        if (_selected)
            UiDragState.DeselectSkill();

        _dropAccepted = false;
        SkillTooltipUI.HideCurrent();
        ClearResourcePreview();
        ShowResourcePreview(a);
        UiDragState.BeginDrag(this);
        _dragRegistered = true;
        CreateGhost();
        MoveGhost(eventData.position);
        _cg.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_ghostRT != null)
            MoveGhost(eventData.position);

        // Target preview: detect actor under cursor
        if (_dragRegistered)
            UpdateTargetPreviewUnderCursor(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        ClearTargetPreviewIfActive();
        ClearResourcePreview();
        _cg.blocksRaycasts = true;
        if (_dragRegistered)
        {
            UiDragState.EndDrag(this);
            _dragRegistered = false;
        }

        if (_ghostRT == null) return;

        if (!_dropAccepted &&
            eventData != null &&
            IsSelfTargetSkill(GetSkillAsset()) &&
            selfCastZone != null &&
            selfCastZone.ContainsScreenPoint(eventData.position, _uiCam))
        {
            _dropAccepted = turn != null && turn.TryCastDraggedSkillToSelf(GetSkillAsset());
        }

        if (_dropAccepted)
        {
            Destroy(_ghostRT.gameObject);
            _ghostRT = null;
            return;
        }

        _ghostRT.DOKill();
        _ghostRT.DOAnchorPos(_ghostHomeAnchoredPos, invalidDropReturnDuration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                if (_ghostRT != null)
                    Destroy(_ghostRT.gameObject);
                _ghostRT = null;
            });
    }

    public void NotifyDropAccepted()
    {
        _dropAccepted = true;
    }

    private void CreateGhost()
    {
        var go = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        go.transform.SetParent(_canvas.transform, false);
        go.transform.SetAsLastSibling();

        _ghostRT = (RectTransform)go.transform;
        _ghostRT.sizeDelta = ((RectTransform)transform).rect.size;
        _ghostRT.pivot = new Vector2(0.5f, 0.5f);
        _ghostRT.anchorMin = new Vector2(0.5f, 0.5f);
        _ghostRT.anchorMax = new Vector2(0.5f, 0.5f);

        RectTransform sourceRt = transform as RectTransform;
        if (sourceRt != null && _canvasRT != null)
        {
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(_uiCam, sourceRt.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRT, screenPos, _uiCam, out _ghostHomeAnchoredPos);
        }
        else
        {
            _ghostHomeAnchoredPos = Vector2.zero;
        }

        var img = go.GetComponent<Image>();
        img.sprite = _img ? _img.sprite : null;
        img.preserveAspect = true;
        img.raycastTarget = false;

        var cg = go.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.alpha = 0.9f;
    }

    private void MoveGhost(Vector2 screenPos)
    {
        if (_ghostRT == null || _canvasRT == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRT, screenPos, _uiCam, out var localPoint);

        _ghostRT.anchoredPosition = localPoint;
    }

    public void SetBindToInventory(RunInventoryManager inv, bool isFixed, int index)
    {
        bindToInventorySlot = true;
        inventory = inv;
        inventorySource = isFixed ? InventorySkillSource.Fixed : InventorySkillSource.Owned;
        inventoryIndex = index;
        Refresh();
    }

    private bool CanDragCurrentSkill()
    {
        if (turn == null) return false;
        ScriptableObject asset = GetSkillAsset();
        if (asset == null || asset is SkillPassiveSO) return false;
        return turn.CanPrototypeCastSkillNow(asset);
    }

    public bool IsSelfTargetSkillAsset()
        => IsSelfTargetSkill(GetSkillAsset());

    public static bool IsSelfTargetSkill(ScriptableObject asset)
    {
        switch (asset)
        {
            case SkillDamageSO damage:
                return damage.target == SkillTargetRule.Self;
            case SkillBuffDebuffSO buffDebuff:
                return buffDebuff.target == SkillTargetRule.Self;
            default:
                return false;
        }
    }

    private void RefreshLabel()
    {
        if (nameText == null)
            return;

        if (bindToInventorySlot && inventory != null)
        {
            var src = inventorySource == InventorySkillSource.Fixed
                ? RunInventoryManager.SkillSource.Fixed
                : RunInventoryManager.SkillSource.Owned;
            nameText.text = inventory.GetSkillDisplayName(src, inventoryIndex);
        }
        else
        {
            nameText.text = SkillUiMetadataUtility.ResolveDisplayName(GetSkillAsset());
        }
    }

    public bool TryGetSkillTooltip(out Canvas canvas, out RectTransform target, out ScriptableObject asset, out SkillRuntime runtime)
    {
        canvas = _canvas;
        target = transform as RectTransform;
        asset = GetSkillAsset();
        runtime = null;
        if (turn != null && asset != null && !(asset is SkillPassiveSO))
            turn.TryGetPrototypeSkillTooltipRuntime(asset, out runtime);

        return canvas != null && target != null && asset != null;
    }

    private void RefreshCostBadges()
    {
        SkillUiIconLibrarySO resolvedIconLibrary = ResolveIconLibrary();
        bool showBadges = SkillUiMetadataUtility.TryGetSkillCosts(GetSkillAsset(), out int focusCost, out int slotsRequired);
        SetCostBadgeVisible(focusCostBadgeBackground, focusCostBadgeText, showBadges);
        SetCostBadgeVisible(slotCostBadgeBackground, slotCostBadgeText, showBadges);
        if (slotCostBadgeIcon != null)
            slotCostBadgeIcon.gameObject.SetActive(showBadges);

        if (!showBadges)
            return;

        if (focusCostBadgeText != null)
            focusCostBadgeText.text = focusCost.ToString();

        Sprite diceCostIcon = resolvedIconLibrary != null ? resolvedIconLibrary.GetDiceCostIcon(slotsRequired) : null;
        if (slotCostBadgeIcon != null)
        {
            slotCostBadgeIcon.sprite = diceCostIcon;
            slotCostBadgeIcon.enabled = diceCostIcon != null;
        }

        if (slotCostBadgeText != null)
        {
            bool useFallbackText = diceCostIcon == null;
            slotCostBadgeText.gameObject.SetActive(useFallbackText);
            if (useFallbackText)
                slotCostBadgeText.text = slotsRequired.ToString();
        }
    }

    private void RefreshElementBadge()
    {
        ScriptableObject asset = GetSkillAsset();
        SkillUiIconLibrarySO resolvedIconLibrary = ResolveIconLibrary();
        Sprite icon = null;
        Color backgroundColor = Color.white;
        Color iconTint = Color.white;
        Color slotBackgroundColor = Color.white;
        bool hasElement = false;
        if (resolvedIconLibrary != null && SkillUiMetadataUtility.TryGetElementType(asset, out ElementType element))
            hasElement = resolvedIconLibrary.TryGetElementVisual(element, out slotBackgroundColor, out icon, out backgroundColor, out iconTint);

        if (elementBadgeBackground != null)
            elementBadgeBackground.gameObject.SetActive(hasElement);
        if (elementBadgeIcon != null)
        {
            elementBadgeIcon.gameObject.SetActive(hasElement);
            if (hasElement)
            {
                if (elementBadgeBackground != null)
                    elementBadgeBackground.color = backgroundColor;
                elementBadgeIcon.sprite = icon;
                elementBadgeIcon.color = iconTint;
            }
        }

        if (skillBackgroundImage != null)
        {
            if (hasElement)
                skillBackgroundImage.color = slotBackgroundColor;
            else
                skillBackgroundImage.color = Color.white;
        }
    }

    private void EnsureCostBadgeUi()
    {
        focusCostBadgeBackground = EnsureBadge(ref focusCostBadgeBackground, ref focusCostBadgeText, FocusBadgeName, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(6f, -6f), new Color(0.1f, 0.22f, 0.35f, 0.92f));
        slotCostBadgeBackground = EnsureBadge(ref slotCostBadgeBackground, ref slotCostBadgeText, SlotBadgeName, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-6f, -6f), new Color(0.28f, 0.2f, 0.55f, 0.92f));
        slotCostBadgeIcon = EnsureBadgeIcon(slotCostBadgeBackground, ref slotCostBadgeIcon, "Icon");
        elementBadgeBackground = EnsureElementBadge(ref elementBadgeBackground, ref elementBadgeIcon);
    }

    public void BindLayout(SkillSlotLayout layout)
    {
        skillSlotLayout = layout;
        ApplyLayoutBindings();
    }

    private void ApplyLayoutBindings()
    {
        if (skillSlotLayout == null)
            return;

        if (skillSlotLayout.SkillArt != null)
            _img = skillSlotLayout.SkillArt;
        if (skillSlotLayout.TitleText != null)
            nameText = skillSlotLayout.TitleText;
        if (skillSlotLayout.BackgroundImage != null)
            skillBackgroundImage = skillSlotLayout.BackgroundImage;
        if (skillSlotLayout.FocusBadgeBackground != null)
            focusCostBadgeBackground = skillSlotLayout.FocusBadgeBackground;
        if (skillSlotLayout.FocusBadgeText != null)
            focusCostBadgeText = skillSlotLayout.FocusBadgeText;
        if (skillSlotLayout.DiceBadgeBackground != null)
            slotCostBadgeBackground = skillSlotLayout.DiceBadgeBackground;
        if (skillSlotLayout.DiceBadgeIcon != null)
            slotCostBadgeIcon = skillSlotLayout.DiceBadgeIcon;
        if (skillSlotLayout.DiceBadgeFallbackText != null)
            slotCostBadgeText = skillSlotLayout.DiceBadgeFallbackText;
        if (skillSlotLayout.ElementBadgeBackground != null)
            elementBadgeBackground = skillSlotLayout.ElementBadgeBackground;
        if (skillSlotLayout.ElementBadgeIcon != null)
            elementBadgeIcon = skillSlotLayout.ElementBadgeIcon;
        if (iconLibrary != null)
            _sharedIconLibrary = iconLibrary;
    }

    private Image EnsureBadge(
        ref Image badgeBackground,
        ref TMP_Text badgeText,
        string badgeName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Color backgroundColor)
    {
        if (!(transform is RectTransform))
            return badgeBackground;

        if (badgeBackground != null && badgeText != null)
            return badgeBackground;

        RectTransform badgeRoot = badgeBackground != null ? badgeBackground.rectTransform : transform.Find(badgeName) as RectTransform;
        bool createdBadge = badgeRoot == null;
        if (badgeRoot == null)
        {
            GameObject badgeGo = new GameObject(badgeName, typeof(RectTransform), typeof(Image));
            badgeRoot = badgeGo.GetComponent<RectTransform>();
            badgeRoot.SetParent(transform, false);
        }

        if (createdBadge)
        {
            badgeRoot.anchorMin = anchorMin;
            badgeRoot.anchorMax = anchorMax;
            badgeRoot.pivot = pivot;
            badgeRoot.sizeDelta = new Vector2(28f, 22f);
            badgeRoot.anchoredPosition = anchoredPosition;
        }

        badgeBackground = badgeRoot.GetComponent<Image>();
        if (badgeBackground == null)
            badgeBackground = badgeRoot.gameObject.AddComponent<Image>();
        badgeBackground.color = backgroundColor;
        badgeBackground.raycastTarget = false;

        RectTransform textRoot = badgeText != null ? badgeText.rectTransform : badgeRoot.Find("Value") as RectTransform;
        bool createdText = textRoot == null;
        if (textRoot == null)
        {
            GameObject textGo = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
            textRoot = textGo.GetComponent<RectTransform>();
            textRoot.SetParent(badgeRoot, false);
        }

        if (createdText)
        {
            textRoot.anchorMin = Vector2.zero;
            textRoot.anchorMax = Vector2.one;
            textRoot.offsetMin = Vector2.zero;
            textRoot.offsetMax = Vector2.zero;
        }

        badgeText = textRoot.GetComponent<TMP_Text>();
        if (badgeText == null)
            badgeText = textRoot.gameObject.AddComponent<TextMeshProUGUI>();
        badgeText.fontSize = 16f;
        badgeText.fontStyle = FontStyles.Bold;
        badgeText.alignment = TextAlignmentOptions.Center;
        badgeText.color = Color.white;
        badgeText.raycastTarget = false;
        if (badgeText.font == null && nameText != null)
            badgeText.font = nameText.font;

        return badgeBackground;
    }

    private Image EnsureBadgeIcon(Image badgeBackground, ref Image badgeIcon, string childName)
    {
        if (badgeBackground == null)
            return badgeIcon;

        if (badgeIcon != null)
            return badgeIcon;

        RectTransform iconRoot = badgeIcon != null ? badgeIcon.rectTransform : badgeBackground.transform.Find(childName) as RectTransform;
        bool createdIcon = iconRoot == null;
        if (iconRoot == null)
        {
            GameObject iconGo = new GameObject(childName, typeof(RectTransform), typeof(Image));
            iconRoot = iconGo.GetComponent<RectTransform>();
            iconRoot.SetParent(badgeBackground.transform, false);
        }

        if (createdIcon)
        {
            iconRoot.anchorMin = Vector2.zero;
            iconRoot.anchorMax = Vector2.one;
            iconRoot.offsetMin = new Vector2(3f, 3f);
            iconRoot.offsetMax = new Vector2(-3f, -3f);
        }

        badgeIcon = iconRoot.GetComponent<Image>();
        if (badgeIcon == null)
            badgeIcon = iconRoot.gameObject.AddComponent<Image>();
        badgeIcon.preserveAspect = true;
        badgeIcon.raycastTarget = false;
        badgeIcon.color = Color.white;
        return badgeIcon;
    }

    private Image EnsureElementBadge(ref Image badgeBackground, ref Image badgeIcon)
    {
        if (!(transform is RectTransform))
            return badgeBackground;

        if (badgeBackground != null && badgeIcon != null)
            return badgeBackground;

        RectTransform badgeRoot = badgeBackground != null ? badgeBackground.rectTransform : transform.Find(ElementBadgeName) as RectTransform;
        bool createdBadge = badgeRoot == null;
        if (badgeRoot == null)
        {
            GameObject badgeGo = new GameObject(ElementBadgeName, typeof(RectTransform), typeof(Image));
            badgeRoot = badgeGo.GetComponent<RectTransform>();
            badgeRoot.SetParent(transform, false);
        }

        if (createdBadge)
        {
            badgeRoot.anchorMin = new Vector2(1f, 0f);
            badgeRoot.anchorMax = new Vector2(1f, 0f);
            badgeRoot.pivot = new Vector2(1f, 0f);
            badgeRoot.sizeDelta = new Vector2(24f, 24f);
            badgeRoot.anchoredPosition = new Vector2(-6f, 6f);
            badgeRoot.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        badgeBackground = badgeRoot.GetComponent<Image>();
        if (badgeBackground == null)
            badgeBackground = badgeRoot.gameObject.AddComponent<Image>();
        badgeBackground.raycastTarget = false;

        RectTransform iconRoot = badgeIcon != null ? badgeIcon.rectTransform : badgeRoot.Find("Icon") as RectTransform;
        bool createdIcon = iconRoot == null;
        if (iconRoot == null)
        {
            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconRoot = iconGo.GetComponent<RectTransform>();
            iconRoot.SetParent(badgeRoot, false);
        }

        if (createdIcon)
        {
            iconRoot.anchorMin = Vector2.zero;
            iconRoot.anchorMax = Vector2.one;
            iconRoot.offsetMin = new Vector2(4f, 4f);
            iconRoot.offsetMax = new Vector2(-4f, -4f);
            iconRoot.localRotation = Quaternion.Euler(0f, 0f, -45f);
        }

        badgeIcon = iconRoot.GetComponent<Image>();
        if (badgeIcon == null)
            badgeIcon = iconRoot.gameObject.AddComponent<Image>();
        badgeIcon.raycastTarget = false;
        badgeIcon.preserveAspect = true;
        badgeIcon.color = Color.white;

        return badgeBackground;
    }

    private static void SetCostBadgeVisible(Image badgeBackground, TMP_Text badgeText, bool visible)
    {
        if (badgeBackground != null)
            badgeBackground.gameObject.SetActive(visible);
        if (badgeText != null && badgeText.gameObject != badgeBackground?.gameObject)
            badgeText.gameObject.SetActive(visible);
    }

    // ---------------------------
    // Resource Preview Helpers
    // ---------------------------

    public void ShowResourcePreview(ScriptableObject asset)
    {
        if (turn == null || asset == null || asset is SkillPassiveSO)
            return;
        if (!SkillUiMetadataUtility.TryGetSkillCosts(asset, out int focusCost, out int slotsRequired))
            return;

        // --- Focus preview ---
        CombatHUD hud = GetCachedHud();
        if (hud != null && turn.player != null)
        {
            bool isInvalid = turn.player.focus < focusCost;
            hud.ShowFocusPreview(focusCost, 0, isInvalid);
        }

        // --- Dice consume preview ---
        if (turn.diceRig != null)
        {
            BuildSpentDiceSet();
            turn.diceRig.ShowConsumePreview(slotsRequired, _cachedSpentSet);
        }

        // --- Targetability overlay ---
        ShowTargetOverlays(asset);

        _resourcePreviewActive = true;
    }

    private void ClearResourcePreview()
    {
        if (!_resourcePreviewActive)
            return;

        _resourcePreviewActive = false;

        CombatHUD hud = GetCachedHud();
        if (hud != null)
            hud.ClearFocusPreview();

        if (turn != null && turn.diceRig != null)
            turn.diceRig.ClearConsumePreview();

        ClearTargetOverlays();
    }

    private CombatHUD GetCachedHud()
    {
        if (_cachedHud == null)
            _cachedHud = FindObjectOfType<CombatHUD>(true);
        return _cachedHud;
    }

    private static readonly System.Collections.Generic.HashSet<DiceSpinnerGeneric> _cachedSpentSet
        = new System.Collections.Generic.HashSet<DiceSpinnerGeneric>();

    private void BuildSpentDiceSet()
    {
        _cachedSpentSet.Clear();
        if (turn != null && turn.SpentDiceThisTurn != null)
        {
            foreach (var d in turn.SpentDiceThisTurn)
                _cachedSpentSet.Add(d);
        }
    }

    private void Update()
    {
        RefreshIfVisualMetadataChanged();
        // Update dice preview blink mỗi frame khi đang active
        if (_resourcePreviewActive && turn != null && turn.diceRig != null)
        {
            BuildSpentDiceSet();
            turn.diceRig.UpdateConsumePreviewVisuals(_cachedSpentSet);
        }
    }

    // ---------------------------
    // Target Preview (HP/Guard/Status)
    // ---------------------------

    private void UpdateTargetPreviewUnderCursor(PointerEventData eventData)
    {
        if (turn == null || turn.player == null)
            return;

        // Raycast to find TargetClickable2D under cursor
        CombatActor hoveredActor = RaycastForActor(eventData);
        ActorWorldUI hoveredUI = null;
        bool hoveringSelfZone = false;

        if (hoveredActor != null && _cachedActorWorldUIs != null)
        {
            foreach (ActorWorldUI ui in _cachedActorWorldUIs)
            {
                if (ui != null && ui.actor == hoveredActor)
                {
                    hoveredUI = ui;
                    break;
                }
            }
        }

        // Same target as before — skip rebuild
        hoveringSelfZone = hoveredActor == null &&
                           IsSelfTargetSkill(GetSkillAsset()) &&
                           selfCastZone != null &&
                           selfCastZone.ContainsScreenPoint(eventData.position, _uiCam);

        if (hoveringSelfZone && _cachedActorWorldUIs != null)
        {
            hoveredActor = turn.player;
            foreach (ActorWorldUI ui in _cachedActorWorldUIs)
            {
                if (ui != null && ui.actor == turn.player)
                {
                    hoveredUI = ui;
                    break;
                }
            }
        }

        if (hoveredUI == _currentPreviewTarget && hoveredUI != null)
            return;

        // Clear old
        ClearTargetPreviewIfActive();

        // No valid target
        if (hoveredUI == null || hoveredActor == null)
            return;

        // Check target is valid
        if (_cachedDragRuntime == null)
        {
            ScriptableObject asset = GetSkillAsset();
            if (asset != null && !(asset is SkillPassiveSO))
            {
                // Thử lấy prototype (có tính toán placement). Nếu thất bại (board đầy), tạo fallback runtime để vẫn hiện preview
                if (!turn.TryGetPrototypeSkillTooltipRuntime(asset, out _cachedDragRuntime))
                {
                    if (asset is SkillDamageSO ds) _cachedDragRuntime = SkillRuntime.FromDamage(ds);
                    else if (asset is SkillBuffDebuffSO bds) _cachedDragRuntime = SkillRuntime.FromBuffDebuff(bds);
                }
            }
        }

        if (_cachedDragRuntime == null)
            return;

        if (!TurnManagerTargetingUtility.IsValidTargetForPendingSkill(_cachedDragRuntime, hoveredActor, turn.player, turn.party, turn.enemy))
            return;

        int dieValue = GetPreviewDieValue(_cachedDragRuntime);
        TargetPreviewBuilder.ActionPreviewBundle bundle =
            TargetPreviewBuilder.BuildActionBundle(_cachedDragRuntime, turn.player, hoveredActor, dieValue, turn.party, turn.enemy);

        if (!bundle.valid)
            return;

        _currentPreviewTarget = hoveredUI;
        ShowActionPreviewBundle(bundle);
    }

    private void ClearTargetPreviewIfActive()
    {
        if (_cachedActorWorldUIs != null)
        {
            foreach (ActorWorldUI ui in _cachedActorWorldUIs)
            {
                if (ui != null)
                    ui.ClearTargetPreview();
            }
        }

        _currentPreviewTarget = null;

        if (_resourcePreviewActive && turn != null && turn.player != null)
        {
            ScriptableObject asset = GetSkillAsset();
            if (asset != null && SkillUiMetadataUtility.TryGetSkillCosts(asset, out int focusCost, out _))
            {
                CombatHUD hud = GetCachedHud();
                if (hud != null)
                    hud.ShowFocusPreview(focusCost, 0, turn.player.focus < focusCost);
            }
        }
    }

    private void ShowActionPreviewBundle(TargetPreviewBuilder.ActionPreviewBundle bundle)
    {
        if (_cachedActorWorldUIs == null || bundle.targetPreviews == null)
            return;

        foreach (KeyValuePair<CombatActor, TargetPreviewData> kvp in bundle.targetPreviews)
        {
            if (kvp.Key == null || !kvp.Value.valid)
                continue;

            foreach (ActorWorldUI ui in _cachedActorWorldUIs)
            {
                if (ui != null && ui.actor == kvp.Key)
                {
                    ui.ShowTargetPreview(kvp.Value);
                    break;
                }
            }
        }

        CombatHUD hud = GetCachedHud();
        ScriptableObject asset = GetSkillAsset();
        if (hud != null && turn != null && turn.player != null && asset != null &&
            SkillUiMetadataUtility.TryGetSkillCosts(asset, out int focusCost, out _))
        {
            hud.ShowFocusPreview(focusCost, Mathf.Max(0, bundle.totalSelfFocusGain), turn.player.focus < focusCost);
        }
    }

    private CombatActor RaycastForActor(PointerEventData eventData)
    {
        // 1. Thử dùng dữ liệu raycast UI (áp dụng cho UI Elements hoặc World có PhysicsRaycaster)
        GameObject hitGo = eventData.pointerCurrentRaycast.gameObject;
        CombatActor actor = GetActorFromGameObject(hitGo);
        if (actor != null) return actor;

        // 2. Fallback: Dùng Physics2D (áp dụng cho World Objects với Collider2D nhưng thiếu PhysicsRaycaster trên Camera)
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, -cam.transform.position.z));
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            if (hit != null)
            {
                actor = GetActorFromGameObject(hit.gameObject);
                if (actor != null) return actor;
            }
        }

        return null;
    }

    private CombatActor GetActorFromGameObject(GameObject go)
    {
        if (go == null) return null;
        TargetClickable2D clickable = go.GetComponent<TargetClickable2D>();
        if (clickable == null) clickable = go.GetComponentInParent<TargetClickable2D>();

        if (clickable != null)
        {
            CombatActor actor = clickable.GetComponent<CombatActor>();
            if (actor == null) actor = clickable.GetComponentInParent<CombatActor>();
            return actor;
        }
        return null;
    }

    /// <summary>Public accessor so TargetClickable2D can get the expected die value for hover preview.</summary>
    public int GetPublicPreviewDieValue(SkillRuntime rt) => GetPreviewDieValue(rt);

    private int GetPreviewDieValue(SkillRuntime rt)
    {
        if (turn == null || turn.diceRig == null || !turn.diceRig.HasRolledThisTurn)
            return 0;

        // Find the next available die that would be consumed by this skill
        BuildSpentDiceSet();
        int value = 0;
        int slotsNeeded = Mathf.Clamp(rt.slotsRequired, 1, 3);
        int found = 0;

        for (int i = 0; i < 3 && found < slotsNeeded; i++)
        {
            if (!turn.diceRig.IsSlotActive(i)) continue;
            DiceSpinnerGeneric die = turn.diceRig.GetDice(i);
            if (die == null) continue;
            if (_cachedSpentSet.Contains(die)) continue;

            value += turn.diceRig.GetResolvedContribution(i, turn.player, rt.element);
            found++;
        }

        return value;
    }

    // ---------------------------
    // Targetability Overlay
    // ---------------------------

    private ActorWorldUI[] _cachedActorWorldUIs;
    private bool _overlaysShown;

    private void ShowTargetOverlays(ScriptableObject asset)
    {
        if (turn == null) return;

        SkillRuntime runtime = null;
        if (!(asset is SkillPassiveSO))
            turn.TryGetPrototypeSkillTooltipRuntime(asset, out runtime);

        _cachedActorWorldUIs = FindObjectsOfType<ActorWorldUI>(true);

        foreach (ActorWorldUI ui in _cachedActorWorldUIs)
        {
            if (ui == null || ui.actor == null) continue;

            CombatActor a = ui.actor;
            bool isValid = false;

            if (!a.IsDead)
            {
                if (runtime != null)
                {
                    isValid = TurnManagerTargetingUtility.IsValidTargetForPendingSkill(runtime, a, turn.player, turn.party, turn.enemy);
                }
                else
                {
                    // Fallback an toàn nếu không lấy được runtime (chủ yếu là cho kỹ năng passive hoặc lỗi)
                    if (SkillUiMetadataUtility.TryGetTargetRule(asset, out SkillTargetRule rule))
                    {
                        bool isEnemySide = SkillTargetRuleUtility.IsEnemySideTarget(rule);
                        bool isSelfOnly = rule == SkillTargetRule.Self;
                        bool isAllySide = SkillTargetRuleUtility.IsAllySideTarget(rule);

                        if (isSelfOnly)
                            isValid = (a == turn.player);
                        else if (isEnemySide)
                            isValid = (a != turn.player && (turn.party == null || a.team != turn.player.team));
                        else if (isAllySide)
                            isValid = (a == turn.player || (turn.party != null && a.team == turn.player.team));
                    }
                }
            }

            if (isValid)
                ui.ShowTargetOverlay(true);
            else
                ui.HideTargetOverlay();
        }

        _overlaysShown = true;
    }

    private void ClearTargetOverlays()
    {
        if (!_overlaysShown) return;
        _overlaysShown = false;

        ClearTargetPreviewIfActive();
        _cachedDragRuntime = null;

        if (_cachedActorWorldUIs != null)
        {
            foreach (ActorWorldUI ui in _cachedActorWorldUIs)
            {
                if (ui != null)
                    ui.HideTargetOverlay();
            }
        }

        _cachedActorWorldUIs = null;
    }

    private void ApplyVisualState()
    {
        if (_img == null) return;

        float alpha = _inUse ? inUseAlpha : 1f;
        if (!_castable)
            alpha *= unavailableAlpha;

        Color c = _img.color;
        c.a = alpha;
        _img.color = c;

        if (_cg != null && !_dragRegistered)
            _cg.blocksRaycasts = true;
    }

    public void SetIconLibrary(SkillUiIconLibrarySO library)
    {
        iconLibrary = library;
        if (iconLibrary != null)
            _sharedIconLibrary = iconLibrary;
        Refresh();
    }

    private SkillUiIconLibrarySO ResolveIconLibrary()
    {
        if (iconLibrary != null)
        {
            _sharedIconLibrary = iconLibrary;
            return iconLibrary;
        }

        if (_sharedIconLibrary != null)
            return _sharedIconLibrary;

        ActorWorldUI[] worldUis = FindObjectsOfType<ActorWorldUI>(true);
        for (int i = 0; i < worldUis.Length; i++)
        {
            if (worldUis[i] != null && worldUis[i].iconLibrary != null)
            {
                _sharedIconLibrary = worldUis[i].iconLibrary;
                return _sharedIconLibrary;
            }
        }

        return null;
    }

    private void RefreshIfVisualMetadataChanged()
    {
        ScriptableObject asset = GetSkillAsset();
        Sprite currentIcon = GetIcon();
        string currentName = bindToInventorySlot && inventory != null
            ? inventory.GetSkillDisplayName(inventorySource == InventorySkillSource.Fixed ? RunInventoryManager.SkillSource.Fixed : RunInventoryManager.SkillSource.Owned, inventoryIndex)
            : SkillUiMetadataUtility.ResolveDisplayName(asset);

        bool hasCosts = SkillUiMetadataUtility.TryGetSkillCosts(asset, out int focusCost, out int slotsRequired);
        if (!hasCosts)
        {
            focusCost = -1;
            slotsRequired = -1;
        }

        bool hasElement = SkillUiMetadataUtility.TryGetElementType(asset, out ElementType element);
        if (asset == _lastVisualAsset &&
            currentIcon == _lastVisualIcon &&
            string.Equals(currentName, _lastVisualName) &&
            focusCost == _lastVisualFocusCost &&
            slotsRequired == _lastVisualSlotsRequired &&
            hasElement == _lastVisualHasElement &&
            (!hasElement || element == _lastVisualElement))
        {
            return;
        }

        Refresh();
    }

    private void CaptureVisualSnapshot()
    {
        ScriptableObject asset = GetSkillAsset();
        _lastVisualAsset = asset;
        _lastVisualIcon = GetIcon();
        _lastVisualName = bindToInventorySlot && inventory != null
            ? inventory.GetSkillDisplayName(inventorySource == InventorySkillSource.Fixed ? RunInventoryManager.SkillSource.Fixed : RunInventoryManager.SkillSource.Owned, inventoryIndex)
            : SkillUiMetadataUtility.ResolveDisplayName(asset);

        if (SkillUiMetadataUtility.TryGetSkillCosts(asset, out int focusCost, out int slotsRequired))
        {
            _lastVisualFocusCost = focusCost;
            _lastVisualSlotsRequired = slotsRequired;
        }
        else
        {
            _lastVisualFocusCost = -1;
            _lastVisualSlotsRequired = -1;
        }

        _lastVisualHasElement = SkillUiMetadataUtility.TryGetElementType(asset, out _lastVisualElement);
        if (!_lastVisualHasElement)
            _lastVisualElement = ElementType.Neutral;
    }
}

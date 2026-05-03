using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ConsumableBarUIManager : MonoBehaviour
{
    [Serializable]
    public class ConsumableSlotView
    {
        public RectTransform root;
        public Button button;
        public Image background;
        public Image icon;
        public TMP_Text titleText;
        public TMP_Text chargesText;
        public ConsumableSlotInteractionProxy interactionProxy;
    }

    [Header("Runtime Links")]
    public RunInventoryManager runInventory;
    public TurnManager turnManager;
    public CombatHUD combatHud;
    public BattlePartyManager2D party;
    public CombatActor player;
    public GameplayDiceEditController diceEditController;

    [Header("Consumable Row")]
    public RectTransform layoutContainer;
    public ConsumableSlotView[] slots = new ConsumableSlotView[RunInventoryManager.DEFAULT_CONSUMABLE_CAPACITY];
    public Vector2 cardSize = new Vector2(96f, 128f);
    public float relaxedSpacing = 112f;
    public float minStackedSpacing = 42f;
    public float fallbackRowWidth = 520f;
    public bool autoCreateMissingCards = true;

    [Header("Action Panel")]
    public RectTransform actionPanelRoot;
    public Button useButton;
    public TMP_Text useButtonText;
    public Button sellButton;
    public TMP_Text sellButtonText;

    [Header("Tooltip")]
    public RectTransform tooltipRoot;
    public TMP_Text tooltipTitleText;
    public TMP_Text tooltipBodyText;

    [Header("Visual")]
    public Vector2 actionPanelOffset = new Vector2(10f, 0f);
    public Vector2 tooltipOffset = new Vector2(0f, -10f);
    public Color selectedColor = new Color(0.33f, 0.55f, 0.98f, 1f);
    public Color targetingColor = new Color(0.98f, 0.78f, 0.22f, 1f);
    public Color normalColor = new Color(0.16f, 0.19f, 0.25f, 0.96f);
    public Color emptyColor = new Color(0.11f, 0.12f, 0.16f, 0.7f);
    public Color disabledButtonColor = new Color(0.35f, 0.35f, 0.35f, 0.9f);
    public Color enabledUseButtonColor = new Color(0.73f, 0.18f, 0.18f, 1f);
    public Color enabledSellButtonColor = new Color(0.17f, 0.7f, 0.54f, 1f);

    [Header("Drag & Layout")]
    public RectTransform dragLayer;
    public float dragScale = 1.08f;
    public float dragSnapDuration = 0.18f;
    public Ease dragMoveEase = Ease.OutQuart;
    public Ease dragScaleEase = Ease.OutCubic;

    private int _selectedSlot = -1;
    private int _hoveredSlot = -1;
    private int _pendingTargetSlot = -1;
    private int _latchedDiceTargetSlot = -1;
    private int _dragSlot = -1;
    private int _dragSourceIndex = -1;
    private int _previewInsertIndex = -1;
    private CombatActor _pendingTargetActor;
    private DiceSpinnerGeneric _latchedDiceTarget;
    private GameplayDiceEditController _subscribedDiceEditController;
    private RectTransform _dragRect;
    private CanvasGroup _dragGhostCanvasGroup;
    private Vector2 _dragPointerOffset;
    private Tween _dragMoveTween;
    private Tween _dragScaleTween;
    private bool _suppressInventoryRefresh;
    private bool _dragRegistered;

    private void Awake()
    {
        AutoResolveReferences();
        WireButtons();
        RefreshAll();
    }

    private void OnEnable()
    {
        if (runInventory != null)
            runInventory.InventoryChanged += HandleInventoryChanged;
        UiDragState.DragStateChanged += HandleUiDragStateChanged;
        SubscribeDiceEditController();
    }

    private void Start()
    {
        RefreshAll();
    }

    private void OnDisable()
    {
        if (runInventory != null)
            runInventory.InventoryChanged -= HandleInventoryChanged;
        UiDragState.DragStateChanged -= HandleUiDragStateChanged;
        UnsubscribeDiceEditController();
        if (_dragSlot >= 0)
            SetSlotCanvasAlpha(_dragSlot, 1f);
        if (_dragRegistered)
        {
            UiDragState.EndDrag(this);
            _dragRegistered = false;
        }
        _dragSlot = -1;
        _dragSourceIndex = -1;
        _previewInsertIndex = -1;
        ClearDragGhost(instant: true);
    }

    public void HandleSlotHoverEnter(int index)
    {
        if (UiDragState.IsDragging)
        {
            _hoveredSlot = -1;
            RefreshTooltip();
            return;
        }

        _hoveredSlot = index;
        RefreshTooltip();
    }

    public void HandleSlotHoverExit(int index)
    {
        if (_hoveredSlot == index)
            _hoveredSlot = -1;
        RefreshTooltip();
    }

    public void HandleSlotClicked(int index)
    {
        if (_dragSlot >= 0)
            return;

        if (runInventory == null || runInventory.GetConsumable(index) == null)
        {
            _selectedSlot = -1;
            _pendingTargetSlot = -1;
            _pendingTargetActor = null;
            ClearLatchedDiceTarget();
            RefreshAll();
            return;
        }

        bool clickedSelected = _selectedSlot == index;
        bool clickedPendingTarget = _pendingTargetSlot == index;
        if (clickedSelected || clickedPendingTarget)
        {
            _selectedSlot = -1;
            _pendingTargetSlot = -1;
            _pendingTargetActor = null;
        }
        else
        {
            _selectedSlot = index;
            _pendingTargetSlot = -1;
            _pendingTargetActor = null;
        }

        if (_selectedSlot != _latchedDiceTargetSlot)
            ClearLatchedDiceTarget();

        RefreshAll();
    }

    public void HandleSlotBeginDrag(int index, PointerEventData eventData)
    {
        if (runInventory == null || index < 0 || runInventory.GetConsumable(index) == null)
            return;

        EnsureSlotViews();
        ConsumableSlotView slot = GetSlot(index);
        if (slot == null || slot.root == null || eventData == null)
            return;

        EnsureDragLayer();
        if (dragLayer == null)
            return;

        ClearDragGhost(instant: true);
        UiDragState.BeginDrag(this);
        _dragRegistered = true;
        _dragSlot = index;
        _dragSourceIndex = index;
        _previewInsertIndex = index;
        _hoveredSlot = -1;
        if (_selectedSlot == index || _pendingTargetSlot == index)
        {
            _selectedSlot = -1;
            _pendingTargetSlot = -1;
            _pendingTargetActor = null;
            ClearLatchedDiceTarget();
        }

        _dragRect = slot.root;
        _dragRect.SetParent(dragLayer, worldPositionStays: true);
        _dragRect.SetAsLastSibling();
        _dragGhostCanvasGroup = _dragRect.GetComponent<CanvasGroup>();
        if (_dragGhostCanvasGroup == null)
            _dragGhostCanvasGroup = _dragRect.gameObject.AddComponent<CanvasGroup>();
        _dragGhostCanvasGroup.blocksRaycasts = false;
        _dragGhostCanvasGroup.alpha = 0.92f;

        CacheDragPointerOffset(eventData.position, eventData.pressEventCamera);
        MoveGhostWithPointer(eventData.position, eventData.pressEventCamera);

        _dragScaleTween = _dragRect.DOScale(dragScale, 0.12f).SetEase(Ease.OutBack).SetUpdate(true);
        RefreshRowLayout(false);
        RefreshActionPanel();
        RefreshTooltip();
    }

    public void HandleSlotDrag(int index, PointerEventData eventData)
    {
        if (_dragSlot != index || _dragRect == null || eventData == null)
            return;

        MoveGhostWithPointer(eventData.position, eventData.pressEventCamera);

        int nextInsertIndex = GetInsertIndexFromScreenPosition(eventData.position, eventData.pressEventCamera);
        if (nextInsertIndex == _previewInsertIndex)
            return;

        _previewInsertIndex = nextInsertIndex;
        RefreshRowLayout(false);
    }

    public void HandleSlotEndDrag(int index, PointerEventData eventData)
    {
        if (_dragSlot != index)
            return;

        int source = _dragSourceIndex;
        int target = eventData != null ? GetInsertIndexFromScreenPosition(eventData.position, eventData.pressEventCamera) : source;
        bool moved = false;
        if (source >= 0 && runInventory != null)
        {
            _suppressInventoryRefresh = true;
            try
            {
                moved = runInventory.TryMoveConsumable(source, target);
            }
            finally
            {
                _suppressInventoryRefresh = false;
            }
        }

        int finalIndex = moved ? Mathf.Clamp(target, 0, Mathf.Max(0, runInventory.GetConsumableCount() - 1)) : source;
        if (moved)
            MoveSlotView(source, finalIndex);

        if (_dragRect != null && layoutContainer != null)
            _dragRect.SetParent(layoutContainer, worldPositionStays: true);

        if (_dragGhostCanvasGroup != null)
        {
            _dragGhostCanvasGroup.blocksRaycasts = true;
            _dragGhostCanvasGroup.alpha = 1f;
        }

        if (_dragRegistered)
        {
            UiDragState.EndDrag(this);
            _dragRegistered = false;
        }

        _dragSlot = -1;
        _dragSourceIndex = -1;
        _previewInsertIndex = -1;
        _dragRect = null;
        _dragGhostCanvasGroup = null;
        RefreshAll();
    }

    public bool TryHandleTargetClick(CombatActor clicked)
    {
        if (runInventory == null || _pendingTargetSlot < 0)
            return false;

        ConsumableDataSO data = runInventory.GetConsumable(_pendingTargetSlot);
        if (data == null)
        {
            _pendingTargetSlot = -1;
            _pendingTargetActor = null;
            RefreshAll();
            return false;
        }

        if (!IsValidManualTarget(data, clicked))
            return true;

        _pendingTargetActor = clicked;
        ExecuteConsumable(_pendingTargetSlot, data, ResolveUser(), _pendingTargetActor, ResolveTargetDie(data));
        _pendingTargetSlot = -1;
        _pendingTargetActor = null;
        RefreshAll();
        return true;
    }

    public void RefreshAll()
    {
        AutoResolveReferences();
        ClampSelection();
        RefreshSlots();
        RefreshActionPanel();
        RefreshTooltip();
    }

    [ContextMenu("Rebind UI")]
    public void RebindUi()
    {
        WireButtons();
        RefreshAll();
    }

    private void AutoResolveReferences()
    {
        if (runInventory == null)
            runInventory = GetComponentInParent<RunInventoryManager>(true);

        if (turnManager == null)
            turnManager = FindObjectOfType<TurnManager>(true);

        if (combatHud == null)
            combatHud = FindObjectOfType<CombatHUD>(true);

        if (party == null)
        {
            if (combatHud != null && combatHud.party != null)
                party = combatHud.party;
            else
                party = FindObjectOfType<BattlePartyManager2D>(true);
        }

        if (player == null)
        {
            if (turnManager != null && turnManager.player != null)
                player = turnManager.player;
            else if (combatHud != null && combatHud.player != null)
                player = combatHud.player;
        }

        if (diceEditController == null)
            diceEditController = FindObjectOfType<GameplayDiceEditController>(true);

        SubscribeDiceEditController();
        EnsureDragLayer();
    }

    private void WireButtons()
    {
        EnsureSlotViews();

        if (slots != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                ConsumableSlotView slot = slots[i];
                if (slot == null)
                    continue;

                if (slot.interactionProxy == null && slot.root != null)
                    slot.interactionProxy = slot.root.GetComponent<ConsumableSlotInteractionProxy>();

                if (slot.interactionProxy != null)
                {
                    slot.interactionProxy.manager = this;
                    slot.interactionProxy.slotIndex = i;
                }
            }
        }

        if (useButton != null)
        {
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(UseSelectedConsumable);
        }

        if (sellButton != null)
        {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(SellSelectedConsumable);
        }
    }

    private void RefreshSlots()
    {
        EnsureSlotViews();

        if (slots == null)
            return;

        int visibleCount = runInventory != null ? runInventory.GetConsumableCount() : 0;
        for (int i = 0; i < slots.Length; i++)
        {
            ConsumableSlotView slot = slots[i];
            if (slot == null)
                continue;

            bool showSlot = i < visibleCount;
            ConsumableDataSO data = showSlot && runInventory != null ? runInventory.GetConsumable(i) : null;
            int charges = showSlot && runInventory != null ? runInventory.GetConsumableCharges(i) : 0;
            bool selected = i == _selectedSlot && data != null;
            bool targeting = i == _pendingTargetSlot && data != null;

            if (slot.root != null && slot.root.gameObject.activeSelf != showSlot)
                slot.root.gameObject.SetActive(showSlot);

            if (slot.icon != null)
            {
                slot.icon.sprite = data != null ? data.icon : null;
                slot.icon.enabled = data != null && data.icon != null;
                slot.icon.color = data != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            }

            if (slot.titleText != null)
                slot.titleText.text = data != null ? data.displayName : string.Empty;

            if (slot.chargesText != null)
                slot.chargesText.text = data != null && charges > 1 ? $"x{charges}" : string.Empty;

            if (slot.background != null)
                slot.background.color = data == null ? emptyColor : (targeting ? targetingColor : (selected ? selectedColor : normalColor));

            if (slot.button != null)
                slot.button.interactable = data != null;

            if (slot.root != null && data != null && i != _dragSlot)
                SetSlotCanvasAlpha(i, 1f);
        }

        RefreshRowLayout(false);
    }

    private void RefreshActionPanel()
    {
        if (actionPanelRoot == null)
            return;

        bool show = runInventory != null && _selectedSlot >= 0 && runInventory.GetConsumable(_selectedSlot) != null;
        actionPanelRoot.gameObject.SetActive(show);
        if (!show)
            return;

        ConsumableSlotView slot = GetSlot(_selectedSlot);
        RectTransform visualTarget = GetSlotVisualTarget(slot);
        if (visualTarget != null)
            PositionPanelAtSlotRight(actionPanelRoot, visualTarget, actionPanelOffset);

        ConsumableDataSO data = runInventory.GetConsumable(_selectedSlot);
        CombatActor user = ResolveUser();
        CombatActor target = ResolveTarget(data, user);
        DiceSpinnerGeneric targetDie = ResolveTargetDie(data);
        bool canUse = CanUseFromActionPanel(data, user, target, targetDie);
        if (data != null && data.family == ConsumableFamily.Zodiac)
        {
            Debug.Log(
                $"[ConsumableBarUI] Zodiac action panel refresh: slot={_selectedSlot} data={data.displayName} canUse={canUse}",
                this);
        }

        bool targeting = _pendingTargetSlot == _selectedSlot;
        string useLabel = targeting ? "CANCEL" : "USE";
        RefreshActionButton(useButton, useButtonText, useLabel, canUse || NeedsManualTargetSelection(data) || targeting, enabledUseButtonColor);
        RefreshActionButton(sellButton, sellButtonText, "SELL", true, enabledSellButtonColor);
    }

    private void RefreshActionButton(Button button, TMP_Text label, string text, bool enabled, Color enabledColor)
    {
        if (button != null)
        {
            button.interactable = enabled;
            Image image = button.GetComponent<Image>();
            if (image != null)
                image.color = enabled ? enabledColor : disabledButtonColor;
        }

        if (label != null)
            label.text = text;
    }

    private void RefreshTooltip()
    {
        if (tooltipRoot == null)
            return;

        if (UiDragState.IsDragging)
        {
            tooltipRoot.gameObject.SetActive(false);
            return;
        }

        int tooltipSlot = _hoveredSlot;
        bool show = runInventory != null && tooltipSlot >= 0 && runInventory.GetConsumable(tooltipSlot) != null;
        tooltipRoot.gameObject.SetActive(show);
        if (!show)
            return;

        ConsumableSlotView slot = GetSlot(tooltipSlot);
        ConsumableDataSO data = runInventory.GetConsumable(tooltipSlot);
        int charges = runInventory.GetConsumableCharges(tooltipSlot);

        RectTransform visualTarget = GetSlotVisualTarget(slot);
        if (visualTarget != null)
            PositionTooltipBelowSlot(tooltipRoot, visualTarget, tooltipOffset);

        if (tooltipTitleText != null)
            tooltipTitleText.text = data.displayName;

        if (tooltipBodyText != null)
            tooltipBodyText.text = BuildTooltipBody(data, charges);
    }

    private string BuildTooltipBody(ConsumableDataSO data, int charges)
    {
        if (data == null)
            return string.Empty;

        return $"{data.description}\nCharges: {charges}\n{data.useContext} | {data.targetKind}";
    }

    private void UseSelectedConsumable()
    {
        if (runInventory == null || _selectedSlot < 0)
            return;

        ConsumableDataSO data = runInventory.GetConsumable(_selectedSlot);
        if (data == null)
            return;

        if (_pendingTargetSlot == _selectedSlot)
        {
            _pendingTargetSlot = -1;
            _pendingTargetActor = null;
            RefreshAll();
            return;
        }

        CombatActor user = ResolveUser();
        DiceSpinnerGeneric targetDie = ResolveTargetDie(data);
        if (data.family == ConsumableFamily.Zodiac && diceEditController != null)
        {
            Debug.Log($"[ConsumableBarUI] Attempt open dice edit for slot={_selectedSlot} data={data.displayName}", this);
            if (diceEditController.TryOpenPanelFromSelections(_selectedSlot, data))
            {
                Debug.Log("[ConsumableBarUI] Dice edit panel opened.", this);
                RefreshAll();
                return;
            }

            Debug.LogWarning("[ConsumableBarUI] Dice edit open failed. Check selected die state logs.", this);
            RefreshAll();
            return;
        }

        if (NeedsManualTargetSelection(data))
        {
            _pendingTargetSlot = _selectedSlot;
            _pendingTargetActor = null;
            Debug.Log($"[ConsumableBarUI] Awaiting target for {data.displayName}.", this);
            RefreshAll();
            return;
        }

        CombatActor target = ResolveTarget(data, user);
        ExecuteConsumable(_selectedSlot, data, user, target, targetDie);
        RefreshAll();
    }

    private void SellSelectedConsumable()
    {
        if (runInventory == null || _selectedSlot < 0)
            return;

        runInventory.ClearConsumable(_selectedSlot);
        _selectedSlot = -1;
        RefreshAll();
    }

    private CombatActor ResolveUser()
    {
        if (player != null)
            return player;
        if (turnManager != null && turnManager.player != null)
            return turnManager.player;
        if (combatHud != null && combatHud.player != null)
            return combatHud.player;
        return null;
    }

    private CombatActor ResolveTarget(ConsumableDataSO data, CombatActor user)
    {
        if (data == null)
            return null;

        switch (data.targetKind)
        {
            case ConsumableTargetKind.None:
            case ConsumableTargetKind.Self:
                return user;
            case ConsumableTargetKind.Enemy:
                return _pendingTargetActor;
            default:
                return null;
        }
    }

    private void ClampSelection()
    {
        if (_selectedSlot >= 0 && (runInventory == null || runInventory.GetConsumable(_selectedSlot) == null))
            _selectedSlot = -1;

        if (_hoveredSlot >= 0 && (runInventory == null || runInventory.GetConsumable(_hoveredSlot) == null))
            _hoveredSlot = -1;

        if (_pendingTargetSlot >= 0 && (runInventory == null || runInventory.GetConsumable(_pendingTargetSlot) == null))
        {
            _pendingTargetSlot = -1;
            _pendingTargetActor = null;
        }

        if (_latchedDiceTargetSlot >= 0 && _latchedDiceTargetSlot != _selectedSlot)
            ClearLatchedDiceTarget();
    }

    private void HandleInventoryChanged()
    {
        if (_suppressInventoryRefresh || _dragSlot >= 0)
            return;

        RefreshAll();
    }

    private void HandleUiDragStateChanged()
    {
        if (!UiDragState.IsDragging)
            return;

        _hoveredSlot = -1;
        if (tooltipRoot != null)
            tooltipRoot.gameObject.SetActive(false);
    }

    private RectTransform GetSlotVisualTarget(ConsumableSlotView slot)
    {
        if (slot == null)
            return null;

        if (slot.icon != null)
            return slot.icon.rectTransform;

        return slot.root;
    }

    private void EnsureSlotViews()
    {
        if (layoutContainer == null)
        {
            ConsumableSlotView existing = FindFirstUsableSlotView();
            if (existing != null && existing.root != null)
                layoutContainer = existing.root.parent as RectTransform;
        }

        if (layoutContainer == null)
            layoutContainer = transform as RectTransform;

        DisableAutoLayout(layoutContainer);

        int capacity = runInventory != null ? runInventory.ConsumableCapacity : RunInventoryManager.DEFAULT_CONSUMABLE_CAPACITY;
        capacity = Mathf.Clamp(capacity, 1, RunInventoryManager.MAX_CONSUMABLE_CAPACITY);
        if (slots == null)
            slots = new ConsumableSlotView[capacity];
        else if (slots.Length < capacity)
            Array.Resize(ref slots, capacity);

        ConsumableSlotView template = FindFirstUsableSlotView();
        for (int i = 0; i < capacity; i++)
        {
            if (slots[i] == null || slots[i].root == null)
            {
                if (!autoCreateMissingCards || template == null || template.root == null || layoutContainer == null)
                    continue;

                RectTransform cloneRoot = Instantiate(template.root, layoutContainer);
                cloneRoot.name = $"ConsumableCard_{i + 1}";
                slots[i] = BuildSlotViewFromRoot(cloneRoot);
            }

            ConfigureSlotView(slots[i], i);
        }
    }

    private ConsumableSlotView FindFirstUsableSlotView()
    {
        if (slots == null)
            return null;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].root != null)
                return slots[i];
        }

        return null;
    }

    private ConsumableSlotView BuildSlotViewFromRoot(RectTransform root)
    {
        if (root == null)
            return null;

        return new ConsumableSlotView
        {
            root = root,
            button = root.GetComponent<Button>(),
            background = root.GetComponent<Image>(),
            icon = FindChildComponent<Image>(root, "Icon"),
            titleText = FindChildComponent<TMP_Text>(root, "Title"),
            chargesText = FindChildComponent<TMP_Text>(root, "Charges"),
            interactionProxy = root.GetComponent<ConsumableSlotInteractionProxy>()
        };
    }

    private static T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        if (root == null)
            return null;

        Transform direct = root.Find(childName);
        if (direct != null && direct.TryGetComponent(out T directComponent))
            return directComponent;

        T[] components = root.GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null && components[i].transform != root)
                return components[i];
        }

        return null;
    }

    private void ConfigureSlotView(ConsumableSlotView slot, int index)
    {
        if (slot == null || slot.root == null)
            return;

        slot.root.anchorMin = slot.root.anchorMax = new Vector2(0.5f, 0.5f);
        slot.root.pivot = new Vector2(0.5f, 0.5f);
        slot.root.sizeDelta = cardSize;

        LayoutElement layout = slot.root.GetComponent<LayoutElement>();
        if (layout == null)
            layout = slot.root.gameObject.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;
        layout.preferredWidth = cardSize.x;
        layout.preferredHeight = cardSize.y;

        if (slot.interactionProxy == null)
            slot.interactionProxy = slot.root.GetComponent<ConsumableSlotInteractionProxy>();
        if (slot.interactionProxy == null)
            slot.interactionProxy = slot.root.gameObject.AddComponent<ConsumableSlotInteractionProxy>();

        slot.interactionProxy.manager = this;
        slot.interactionProxy.slotIndex = index;

        if (slot.button != null && slot.background != null)
            slot.button.targetGraphic = slot.background;
    }

    private static void DisableAutoLayout(RectTransform container)
    {
        if (container == null)
            return;

        HorizontalLayoutGroup horizontal = container.GetComponent<HorizontalLayoutGroup>();
        if (horizontal != null)
            horizontal.enabled = false;

        ContentSizeFitter fitter = container.GetComponent<ContentSizeFitter>();
        if (fitter != null)
            fitter.enabled = false;
    }

    private void RefreshRowLayout(bool instant)
    {
        RectTransform container = GetLayoutContainer();
        if (container == null || slots == null)
            return;

        int count = runInventory != null ? runInventory.GetConsumableCount() : 0;
        if (count <= 0)
            return;

        List<ConsumableSlotView> displayed = BuildDisplayedOrder(count);
        for (int i = 0; i < displayed.Count; i++)
        {
            ConsumableSlotView slot = displayed[i];
            if (slot == null || slot.root == null || slot.root == _dragRect)
                continue;

            SnapViewToAnchor(slot.root, container, GetPositionForIndex(i, displayed.Count), instant);
            slot.root.SetSiblingIndex(Mathf.Min(i, container.childCount - 1));
        }
    }

    private List<ConsumableSlotView> BuildDisplayedOrder(int count)
    {
        List<ConsumableSlotView> displayed = new List<ConsumableSlotView>(count);
        bool usePreview = _dragSlot >= 0 && _dragSourceIndex >= 0 && _previewInsertIndex >= 0;
        if (!usePreview)
        {
            for (int i = 0; i < count && i < slots.Length; i++)
                displayed.Add(slots[i]);
            return displayed;
        }

        int insertIndex = Mathf.Clamp(_previewInsertIndex, 0, Mathf.Max(0, count - 1));
        ConsumableSlotView dragged = GetSlot(_dragSourceIndex);
        for (int i = 0; i < count && i < slots.Length; i++)
        {
            ConsumableSlotView current = slots[i];
            if (current == dragged)
                continue;

            if (displayed.Count == insertIndex)
                displayed.Add(dragged);

            displayed.Add(current);
        }

        if (!displayed.Contains(dragged))
            displayed.Add(dragged);

        return displayed;
    }

    private Vector2 GetPositionForIndex(int index, int count)
    {
        if (count <= 1)
            return Vector2.zero;

        float width = fallbackRowWidth;
        RectTransform container = GetLayoutContainer();
        if (container != null && container.rect.width > 1f)
            width = container.rect.width;

        float cardWidth = Mathf.Max(1f, cardSize.x);
        float fitSpacing = Mathf.Max(1f, (width - cardWidth) / Mathf.Max(1, count - 1));
        float spacing = Mathf.Min(relaxedSpacing, fitSpacing);
        if (fitSpacing >= minStackedSpacing)
            spacing = Mathf.Max(spacing, minStackedSpacing);

        float x = (index - (count - 1) / 2f) * spacing;
        return new Vector2(x, 0f);
    }

    private int GetInsertIndexFromScreenPosition(Vector2 screenPosition, Camera eventCamera)
    {
        int count = runInventory != null ? runInventory.GetConsumableCount() : 0;
        RectTransform container = GetLayoutContainer();
        if (container == null || count <= 1)
            return 0;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screenPosition, eventCamera, out Vector2 local))
            return Mathf.Clamp(_dragSourceIndex, 0, Mathf.Max(0, count - 1));

        for (int i = 0; i < count - 1; i++)
        {
            float midpoint = (GetPositionForIndex(i, count).x + GetPositionForIndex(i + 1, count).x) * 0.5f;
            if (local.x < midpoint)
                return i;
        }

        return count - 1;
    }

    private void MoveSlotView(int fromIndex, int insertIndex)
    {
        if (slots == null || fromIndex < 0 || fromIndex >= slots.Length)
            return;

        List<ConsumableSlotView> ordered = new List<ConsumableSlotView>(slots);
        ConsumableSlotView moving = ordered[fromIndex];
        ordered.RemoveAt(fromIndex);
        insertIndex = Mathf.Clamp(insertIndex, 0, ordered.Count);
        ordered.Insert(insertIndex, moving);
        slots = ordered.ToArray();

        for (int i = 0; i < slots.Length; i++)
            ConfigureSlotView(slots[i], i);
    }

    private void SnapViewToAnchor(RectTransform view, RectTransform parent, Vector2 anchoredPos, bool instant)
    {
        if (view == null || parent == null)
            return;

        if (view.parent != parent)
            view.SetParent(parent, worldPositionStays: true);

        view.anchorMin = view.anchorMax = new Vector2(0.5f, 0.5f);
        view.pivot = new Vector2(0.5f, 0.5f);
        view.sizeDelta = cardSize;

        view.DOKill();
        if (instant)
        {
            view.anchoredPosition = anchoredPos;
            view.localScale = Vector3.one;
            return;
        }

        view.DOAnchorPos(anchoredPos, Mathf.Max(0.01f, dragSnapDuration)).SetEase(dragMoveEase).SetUpdate(true);
        view.DOScale(1f, Mathf.Max(0.01f, dragSnapDuration)).SetEase(dragScaleEase).SetUpdate(true);
    }

    private RectTransform GetLayoutContainer()
    {
        if (layoutContainer == null)
        {
            ConsumableSlotView first = FindFirstUsableSlotView();
            if (first != null && first.root != null)
                layoutContainer = first.root.parent as RectTransform;
        }

        if (layoutContainer == null)
            layoutContainer = transform as RectTransform;

        return layoutContainer;
    }

    private ConsumableSlotView GetSlot(int index)
    {
        if (slots == null || index < 0 || index >= slots.Length)
            return null;

        return slots[index];
    }

    private void EnsureDragLayer()
    {
        if (dragLayer != null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform.parent;
        if (parent == null)
            return;

        Transform existing = parent.Find("ConsumableDragLayer");
        if (existing != null)
        {
            dragLayer = existing as RectTransform;
            return;
        }

        GameObject go = new GameObject("ConsumableDragLayer", typeof(RectTransform));
        dragLayer = go.GetComponent<RectTransform>();
        dragLayer.SetParent(parent, false);
        dragLayer.anchorMin = Vector2.zero;
        dragLayer.anchorMax = Vector2.one;
        dragLayer.offsetMin = Vector2.zero;
        dragLayer.offsetMax = Vector2.zero;
        dragLayer.SetAsLastSibling();
    }

    private void CacheDragPointerOffset(Vector2 screenPos, Camera eventCamera)
    {
        if (_dragRect == null || dragLayer == null)
        {
            _dragPointerOffset = Vector2.zero;
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, screenPos, eventCamera, out Vector2 local))
        {
            _dragPointerOffset = Vector2.zero;
            return;
        }

        _dragPointerOffset = local - _dragRect.anchoredPosition;
    }

    private void MoveGhostWithPointer(Vector2 screenPos, Camera eventCamera)
    {
        if (_dragRect == null || dragLayer == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, screenPos, eventCamera, out Vector2 local))
            return;

        _dragRect.anchoredPosition = local - _dragPointerOffset;
    }

    private void ClearDragGhost(bool instant)
    {
        _dragMoveTween?.Kill();
        _dragScaleTween?.Kill();
        _dragMoveTween = null;
        _dragScaleTween = null;

        if (_dragRect != null)
        {
            if (_dragGhostCanvasGroup != null)
            {
                _dragGhostCanvasGroup.blocksRaycasts = true;
                _dragGhostCanvasGroup.alpha = 1f;
            }

            if (_dragRect.parent != layoutContainer && layoutContainer != null)
                _dragRect.SetParent(layoutContainer, worldPositionStays: true);

            if (instant)
            {
                _dragRect.localScale = Vector3.one;
            }
            else
            {
                _dragRect.DOScale(1f, Mathf.Max(0.01f, dragSnapDuration)).SetEase(dragScaleEase).SetUpdate(true);
            }
        }

        _dragRect = null;
        _dragGhostCanvasGroup = null;
    }

    private void SetSlotCanvasAlpha(int index, float alpha)
    {
        ConsumableSlotView slot = GetSlot(index);
        if (slot == null || slot.root == null)
            return;

        CanvasGroup group = slot.root.GetComponent<CanvasGroup>();
        if (group == null)
            group = slot.root.gameObject.AddComponent<CanvasGroup>();

        group.alpha = alpha;
    }

    private void PositionPanelAtSlotRight(RectTransform panel, RectTransform slotRoot, Vector2 offset)
    {
        if (panel == null || slotRoot == null)
            return;

        RectTransform parentRect = panel.parent as RectTransform;
        if (parentRect == null)
        {
            panel.position = slotRoot.position + (Vector3)offset;
            return;
        }

        Vector3[] corners = new Vector3[4];
        slotRoot.GetWorldCorners(corners);
        Vector3 rightMidWorld = (corners[2] + corners[3]) * 0.5f;
        Vector2 localPoint = parentRect.InverseTransformPoint(rightMidWorld);
        panel.anchoredPosition = localPoint + offset;
    }

    private void PositionTooltipBelowSlot(RectTransform tooltip, RectTransform slotRoot, Vector2 offset)
    {
        if (tooltip == null || slotRoot == null)
            return;

        RectTransform parentRect = tooltip.parent as RectTransform;
        if (parentRect == null)
        {
            tooltip.position = slotRoot.position + (Vector3)offset;
            return;
        }

        Vector3[] corners = new Vector3[4];
        slotRoot.GetWorldCorners(corners);
        Vector3 bottomMidWorld = (corners[0] + corners[3]) * 0.5f;
        Vector2 localPoint = parentRect.InverseTransformPoint(bottomMidWorld);
        tooltip.anchoredPosition = localPoint + offset;
    }

    private bool NeedsManualTargetSelection(ConsumableDataSO data)
    {
        if (data == null)
            return false;

        return data.targetKind == ConsumableTargetKind.Enemy || data.targetKind == ConsumableTargetKind.Ally;
    }

    private bool IsReadyToOpenDiceEdit(ConsumableDataSO data)
    {
        return data != null &&
               data.family == ConsumableFamily.Zodiac &&
               diceEditController != null;
    }

    private bool CanUseFromActionPanel(ConsumableDataSO data, CombatActor user, CombatActor target, DiceSpinnerGeneric targetDie)
    {
        if (data == null)
            return false;

        if (IsReadyToOpenDiceEdit(data))
            return diceEditController.CanOpenPanelFromSelections(_selectedSlot, data);

        return ConsumableRuntimeUtility.CanUseInCombat(data, user, target, runInventory, targetDie);
    }

    private void SubscribeDiceEditController()
    {
        if (_subscribedDiceEditController == diceEditController)
            return;

        if (_subscribedDiceEditController != null)
            _subscribedDiceEditController.SelectionStateChanged -= HandleDiceEditSelectionStateChanged;

        _subscribedDiceEditController = diceEditController;

        if (_subscribedDiceEditController != null)
            _subscribedDiceEditController.SelectionStateChanged += HandleDiceEditSelectionStateChanged;
    }

    private void UnsubscribeDiceEditController()
    {
        if (_subscribedDiceEditController != null)
            _subscribedDiceEditController.SelectionStateChanged -= HandleDiceEditSelectionStateChanged;

        _subscribedDiceEditController = null;
    }

    private void HandleDiceEditSelectionStateChanged()
    {
        RefreshAll();
    }

    private bool IsValidManualTarget(ConsumableDataSO data, CombatActor clicked)
    {
        if (data == null || clicked == null)
            return false;

        switch (data.targetKind)
        {
            case ConsumableTargetKind.Enemy:
                return clicked.team == CombatActor.TeamSide.Enemy && !clicked.IsDead;
            case ConsumableTargetKind.Ally:
                return clicked.team == CombatActor.TeamSide.Ally && !clicked.IsDead;
            default:
                return false;
        }
    }

    private DiceSpinnerGeneric ResolveTargetDie(ConsumableDataSO data)
    {
        if (data == null || data.targetKind != ConsumableTargetKind.Dice)
            return null;

        if (_selectedSlot >= 0 && _selectedSlot == _latchedDiceTargetSlot && _latchedDiceTarget != null)
            return _latchedDiceTarget;

        if (diceEditController == null)
            return null;

        diceEditController.TryGetSelectedSceneDie(out DiceSpinnerGeneric die);
        return die;
    }

    private void ExecuteConsumable(int slotIndex, ConsumableDataSO data, CombatActor user, CombatActor target, DiceSpinnerGeneric targetDie)
    {
        ConsumableUseResult result = ConsumableRuntimeUtility.TryUseInCombat(data, user, target, runInventory, targetDie);
        Debug.Log($"[ConsumableBarUI] {result.message}", this);

        if (!result.success)
            return;

        if (data != null && data.targetKind == ConsumableTargetKind.Dice)
        {
            _latchedDiceTargetSlot = slotIndex;
            _latchedDiceTarget = targetDie;
        }
        else
        {
            ClearLatchedDiceTarget();
        }

        SyncInventoryDiceLayoutFromRig();

        if (turnManager != null && turnManager.diceRig != null)
            turnManager.diceRig.RefreshRollInfoCache();
        if (targetDie != null)
            targetDie.RefreshDisplayedState();

        runInventory.TryConsumeConsumableCharge(slotIndex, 1);
        if (runInventory.GetConsumable(slotIndex) != data)
            _selectedSlot = -1;
    }

    private void ClearLatchedDiceTarget()
    {
        _latchedDiceTargetSlot = -1;
        _latchedDiceTarget = null;
    }

    private void SyncInventoryDiceLayoutFromRig()
    {
        if (runInventory == null || turnManager == null || turnManager.diceRig == null || turnManager.diceRig.slots == null)
            return;

        DiceSpinnerGeneric[] layout = new DiceSpinnerGeneric[RunInventoryManager.EQUIPPED_DICE_COUNT];
        int count = Mathf.Min(layout.Length, turnManager.diceRig.slots.Length);
        for (int i = 0; i < count; i++)
            layout[i] = turnManager.diceRig.slots[i] != null ? turnManager.diceRig.slots[i].dice : null;

        runInventory.SetDiceLayout(layout, notifyChanged: false);
    }
}

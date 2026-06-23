using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public partial class ConsumableBarUIManager : MonoBehaviour
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
        public RectTransform actionAnchor;
        public RectTransform tooltipAnchor;
        public RectTransform localActionPanelRoot;
        public Button localUseButton;
        public TMP_Text localUseButtonText;
        public Button localSellButton;
        public TMP_Text localSellButtonText;
        public RectTransform localTooltipRoot;
        public TMP_Text localTooltipTitleText;
        public TMP_Text localTooltipBodyText;
    }

    [Header("Runtime Links")]
    public RunInventoryManager runInventory;
    public TurnManager turnManager;
    public CombatHUD combatHud;
    public BattlePartyManager2D party;
    public CombatActor player;
    public DiceEquipUIManager diceEquipUiManager;
    public GameplayDiceEditController diceEditController;

    [Header("Consumable Row")]
    public RectTransform layoutContainer;
    public RectTransform slotTemplatePrefab;
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
    public bool useExactManualAnchors = true;
    public Vector2 actionPanelOffset = new Vector2(10f, 0f);
    public Vector2 tooltipOffset = new Vector2(0f, -10f);
    public Vector2 actionPanelStableOffset = new Vector2(0f, -18f);
    public Vector2 tooltipStableOffset = new Vector2(0f, -32f);
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
    private DiceEquipUIManager _subscribedDiceEquipUiManager;
    private RectTransform _dragRect;
    private CanvasGroup _dragGhostCanvasGroup;
    private Vector2 _dragPointerOffset;
    private Tween _dragMoveTween;
    private Tween _dragScaleTween;
    private bool _suppressInventoryRefresh;
    private bool _dragRegistered;
    private bool _lastInteractionLocked;
    private int _suppressSlotClicksUntilFrame = -1;
    private bool _suppressSlotClicksUntilPointerRelease;
    private float _suppressSlotClicksUntilTime = -1f;
    private int _suppressClickSlot = -1;
    private int _ignoreHoverSlotUntilExit = -1;
    private bool _rewardChoiceMode;
    private ConsumableDataSO[] _rewardChoiceData = Array.Empty<ConsumableDataSO>();
    private Action<int, ConsumableDataSO> _rewardChoicePicked;

    private void Awake()
    {
        ConsumableBarUiManagerRegistry.Register(this);
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
        _lastInteractionLocked = IsInteractionLocked();
    }

    private void Update()
    {
        UpdateHoveredSlotFromPointer();

        bool locked = IsInteractionLocked();
        if (locked == _lastInteractionLocked)
            return;

        _lastInteractionLocked = locked;
        if (locked)
        {
            ClearConsumableUiSelection(-1);
        }

        RefreshAll();
    }

    private void OnDisable()
    {
        if (runInventory != null)
            runInventory.InventoryChanged -= HandleInventoryChanged;
        UiDragState.DragStateChanged -= HandleUiDragStateChanged;
        UnsubscribeDiceEditController();
        UnsubscribeDiceEquipUiManager();
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

    private void OnDestroy()
    {
        ConsumableBarUiManagerRegistry.Unregister(this);
    }

    public void HandleSlotHoverEnter(int index)
    {
        if (UiDragState.IsDragging)
        {
            SetHoveredSlot(-1);
            return;
        }

        if (index == _ignoreHoverSlotUntilExit)
        {
            SetHoveredSlot(-1);
            return;
        }

        SetHoveredSlot(index);
    }

    public void HandleSlotHoverExit(int index)
    {
        if (_hoveredSlot == index && !IsPointerOverTooltipPresentation(index))
            SetHoveredSlot(-1);

        if (_ignoreHoverSlotUntilExit == index)
            _ignoreHoverSlotUntilExit = -1;

        if (_suppressClickSlot == index && !_suppressSlotClicksUntilPointerRelease)
            _suppressClickSlot = -1;
    }

    private void UpdateHoveredSlotFromPointer()
    {
        if (UiDragState.IsDragging)
        {
            SetHoveredSlot(-1);
            return;
        }

        if (_hoveredSlot >= 0)
        {
            if (!IsPointerOverTooltipPresentation(_hoveredSlot))
                SetHoveredSlot(-1);

            return;
        }

        if (TryGetHoveredSlotUnderPointer(out int hoveredSlot))
            SetHoveredSlot(hoveredSlot);
    }

    private bool SetHoveredSlot(int slot)
    {
        if (_hoveredSlot == slot)
            return false;

        _hoveredSlot = slot;
        RefreshFloatingPresentation();
        return true;
    }

    public void HandleSlotClicked(int index)
    {
        if (_rewardChoiceMode)
        {
            if (_dragSlot >= 0 || ShouldSuppressSlotClick(index))
                return;

            ConsumableDataSO rewardChoice = GetDisplayedConsumable(index);
            if (rewardChoice != null)
                _rewardChoicePicked?.Invoke(index, rewardChoice);
            return;
        }

        if (IsInteractionLocked())
            return;

        if (_dragSlot >= 0)
            return;

        if (ShouldSuppressSlotClick(index))
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
        RefreshPresentationLayers();
    }

    private void RefreshFloatingPresentation()
    {
        RefreshActionPanel();
        RefreshTooltip();
        RefreshPresentationLayers();
    }

    [ContextMenu("Rebind UI")]
    public void RebindUi()
    {
        WireButtons();
        RefreshAll();
    }

    public void ShowRewardChoices(IList<ConsumableDataSO> choices, Action<int, ConsumableDataSO> onPicked)
    {
        int count = choices != null ? choices.Count : 0;
        _rewardChoiceData = new ConsumableDataSO[count];
        for (int i = 0; i < count; i++)
            _rewardChoiceData[i] = choices[i];

        _rewardChoicePicked = onPicked;
        _rewardChoiceMode = true;
        _selectedSlot = -1;
        _pendingTargetSlot = -1;
        _pendingTargetActor = null;
        _hoveredSlot = -1;
        RefreshAll();
    }

    public void ClearRewardChoices()
    {
        _rewardChoiceMode = false;
        _rewardChoiceData = Array.Empty<ConsumableDataSO>();
        _rewardChoicePicked = null;
        _selectedSlot = -1;
        _pendingTargetSlot = -1;
        _pendingTargetActor = null;
        _hoveredSlot = -1;
        RefreshAll();
    }

    private int GetDisplayedConsumableCount()
    {
        return _rewardChoiceMode
            ? (_rewardChoiceData != null ? _rewardChoiceData.Length : 0)
            : (runInventory != null ? runInventory.GetConsumableCount() : 0);
    }

    private ConsumableDataSO GetDisplayedConsumable(int index)
    {
        if (index < 0)
            return null;

        if (_rewardChoiceMode)
            return _rewardChoiceData != null && index < _rewardChoiceData.Length ? _rewardChoiceData[index] : null;

        return runInventory != null ? runInventory.GetConsumable(index) : null;
    }
}

internal static class ConsumableBarUiManagerRegistry
{
    private static ConsumableBarUIManager _instance;

    public static void Register(ConsumableBarUIManager manager)
    {
        if (manager == null)
            return;

        _instance = manager;
    }

    public static void Unregister(ConsumableBarUIManager manager)
    {
        if (_instance == manager)
            _instance = null;
    }

    public static ConsumableBarUIManager Get()
    {
        if (_instance != null)
            return _instance;

#if UNITY_2023_1_OR_NEWER
        _instance = UnityEngine.Object.FindFirstObjectByType<ConsumableBarUIManager>(FindObjectsInactive.Include);
#else
        _instance = UnityEngine.Object.FindObjectOfType<ConsumableBarUIManager>(true);
#endif
        return _instance;
    }
}

using System;
using TMPro;
using UnityEngine;
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

    [Header("Slots")]
    public ConsumableSlotView[] slots = new ConsumableSlotView[RunInventoryManager.RELIC_SLOT_COUNT];

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

    private int _selectedSlot = -1;
    private int _hoveredSlot = -1;
    private int _pendingTargetSlot = -1;
    private int _latchedDiceTargetSlot = -1;
    private CombatActor _pendingTargetActor;
    private DiceSpinnerGeneric _latchedDiceTarget;
    private GameplayDiceEditController _subscribedDiceEditController;

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
        UnsubscribeDiceEditController();
    }

    public void HandleSlotHoverEnter(int index)
    {
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
    }

    private void WireButtons()
    {
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
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            ConsumableSlotView slot = slots[i];
            if (slot == null)
                continue;

            ConsumableDataSO data = runInventory != null ? runInventory.GetConsumable(i) : null;
            int charges = runInventory != null ? runInventory.GetConsumableCharges(i) : 0;
            bool selected = i == _selectedSlot && data != null;
            bool targeting = i == _pendingTargetSlot && data != null;

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
        }
    }

    private void RefreshActionPanel()
    {
        if (actionPanelRoot == null)
            return;

        bool show = runInventory != null && _selectedSlot >= 0 && runInventory.GetConsumable(_selectedSlot) != null;
        actionPanelRoot.gameObject.SetActive(show);
        if (!show)
            return;

        ConsumableSlotView slot = slots[_selectedSlot];
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

        int tooltipSlot = _hoveredSlot;
        bool show = runInventory != null && tooltipSlot >= 0 && runInventory.GetConsumable(tooltipSlot) != null;
        tooltipRoot.gameObject.SetActive(show);
        if (!show)
            return;

        ConsumableSlotView slot = slots[tooltipSlot];
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
        RefreshAll();
    }

    private RectTransform GetSlotVisualTarget(ConsumableSlotView slot)
    {
        if (slot == null)
            return null;

        if (slot.icon != null)
            return slot.icon.rectTransform;

        return slot.root;
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
        if (runInventory.GetConsumable(slotIndex) == null)
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

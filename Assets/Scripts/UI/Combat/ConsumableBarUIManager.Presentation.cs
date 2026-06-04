using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class ConsumableBarUIManager
{
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

        if (diceEquipUiManager == null)
            diceEquipUiManager = FindObjectOfType<DiceEquipUIManager>(true);

        if (diceEditController == null)
            diceEditController = FindObjectOfType<GameplayDiceEditController>(true);

        SubscribeDiceEditController();
        SubscribeDiceEquipUiManager();
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
                slot.chargesText.text = string.Empty;

            if (slot.background != null)
                slot.background.color = data == null ? emptyColor : (targeting ? targetingColor : (selected ? selectedColor : normalColor));

            if (slot.button != null)
                slot.button.interactable = data != null && !IsInteractionLocked();

            if (slot.root != null && data != null && i != _dragSlot)
                SetSlotCanvasAlpha(i, 1f);
        }

        RefreshRowLayout(false);
    }

    private void RefreshPresentationLayers()
    {
        RestoreBaseSlotSiblingOrder();

        RectTransform root = transform as RectTransform;
        if (root != null)
            root.SetAsLastSibling();

        BringSlotToFront(_pendingTargetSlot);
        BringSlotToFront(_selectedSlot);

        if (_dragRect != null)
            _dragRect.SetAsLastSibling();

        RectTransform activeActionPanel = GetActiveActionPanelRoot();
        if (activeActionPanel != null && activeActionPanel.gameObject.activeSelf)
            activeActionPanel.SetAsLastSibling();

        RectTransform activeTooltip = GetActiveTooltipRoot();
        if (activeTooltip != null && activeTooltip.gameObject.activeSelf)
            activeTooltip.SetAsLastSibling();
    }

    private void RefreshActionPanel()
    {
        HideAllActionPanels();

        bool show = !IsInteractionLocked() && runInventory != null && _selectedSlot >= 0 && runInventory.GetConsumable(_selectedSlot) != null;
        if (!show)
            return;

        ConsumableSlotView slot = GetSlot(_selectedSlot);
        RectTransform activePanelRoot = slot != null && slot.localActionPanelRoot != null ? slot.localActionPanelRoot : actionPanelRoot;
        Button activeUseButton = slot != null && slot.localUseButton != null ? slot.localUseButton : useButton;
        TMP_Text activeUseLabel = slot != null && slot.localUseButtonText != null ? slot.localUseButtonText : useButtonText;
        Button activeSellButton = slot != null && slot.localSellButton != null ? slot.localSellButton : sellButton;
        TMP_Text activeSellLabel = slot != null && slot.localSellButtonText != null ? slot.localSellButtonText : sellButtonText;
        if (activePanelRoot == null)
            return;

        activePanelRoot.gameObject.SetActive(true);
        if (activePanelRoot == actionPanelRoot)
        {
            RectTransform actionAnchor = GetSlotActionAnchor(slot);
            if (actionAnchor != null)
                PositionPanelAtAnchor(activePanelRoot, actionAnchor, GetPresentationOffset(isTooltip: false));
        }

        ConsumableDataSO data = runInventory.GetConsumable(_selectedSlot);
        CombatActor user = ResolveUser();
        CombatActor target = ResolveTarget(data, user);
        DiceSpinnerGeneric targetDie = ResolveTargetDie(data);
        DiceSpinnerGeneric selectedCombatDie = ResolveSelectedCombatDie();
        int selectedDiceCount = GetSelectedDiceCount();
        bool canUse = CanUseFromActionPanel(data, user, target, targetDie, selectedCombatDie);
        if (data != null && data.family == ConsumableFamily.Zodiac)
        {
            Debug.Log(
                $"[ConsumableBarUI] Zodiac action panel refresh: slot={_selectedSlot} data={data.displayName} canUse={canUse} selectedDice={selectedDiceCount}",
                this);
        }

        bool targeting = _pendingTargetSlot == _selectedSlot;
        string useLabel = targeting ? "CANCEL" : "USE";
        RefreshActionButton(activeUseButton, activeUseLabel, useLabel, canUse || NeedsManualTargetSelection(data) || targeting, enabledUseButtonColor);
        RefreshActionButton(activeSellButton, activeSellLabel, "SELL", true, enabledSellButtonColor);
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
        HideAllTooltips();

        if (UiDragState.IsDragging)
        {
            return;
        }

        int tooltipSlot = _hoveredSlot;
        bool show = runInventory != null && tooltipSlot >= 0 && runInventory.GetConsumable(tooltipSlot) != null;
        if (!show)
            return;

        ConsumableSlotView slot = GetSlot(tooltipSlot);
        ConsumableDataSO data = runInventory.GetConsumable(tooltipSlot);
        RectTransform activeTooltipRoot = slot != null && slot.localTooltipRoot != null ? slot.localTooltipRoot : tooltipRoot;
        TMP_Text activeTooltipTitle = slot != null && slot.localTooltipTitleText != null ? slot.localTooltipTitleText : tooltipTitleText;
        TMP_Text activeTooltipBody = slot != null && slot.localTooltipBodyText != null ? slot.localTooltipBodyText : tooltipBodyText;
        if (activeTooltipRoot == null)
            return;

        activeTooltipRoot.gameObject.SetActive(true);
        if (activeTooltipRoot == tooltipRoot)
        {
            RectTransform tooltipAnchor = GetSlotTooltipAnchor(slot);
            if (tooltipAnchor != null)
                PositionTooltipAtAnchor(activeTooltipRoot, tooltipAnchor, GetPresentationOffset(isTooltip: true));
        }

        if (activeTooltipTitle != null)
            activeTooltipTitle.text = data.displayName;

        if (activeTooltipBody != null)
            activeTooltipBody.text = BuildTooltipBody(data);

        EnsureTooltipAutoSize(activeTooltipRoot, activeTooltipTitle, activeTooltipBody);
    }

    private string BuildTooltipBody(ConsumableDataSO data)
    {
        if (data == null)
            return string.Empty;

        string description = string.IsNullOrWhiteSpace(data.description) ? string.Empty : data.description.Trim();
        string contextLine = $"{data.useContext} | {data.targetKind}";

        if (string.IsNullOrEmpty(description))
            return contextLine;

        return $"{description}\n\n{contextLine}";
    }
}





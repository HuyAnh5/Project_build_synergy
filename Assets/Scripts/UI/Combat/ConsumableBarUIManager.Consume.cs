using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public partial class ConsumableBarUIManager
{
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

    private void SubscribeDiceEquipUiManager()
    {
        if (_subscribedDiceEquipUiManager == diceEquipUiManager)
            return;

        if (_subscribedDiceEquipUiManager != null)
            _subscribedDiceEquipUiManager.SelectionChanged -= HandleDiceEquipSelectionChanged;

        _subscribedDiceEquipUiManager = diceEquipUiManager;

        if (_subscribedDiceEquipUiManager != null)
            _subscribedDiceEquipUiManager.SelectionChanged += HandleDiceEquipSelectionChanged;
    }

    private void UnsubscribeDiceEquipUiManager()
    {
        if (_subscribedDiceEquipUiManager != null)
            _subscribedDiceEquipUiManager.SelectionChanged -= HandleDiceEquipSelectionChanged;

        _subscribedDiceEquipUiManager = null;
    }

    private void HandleDiceEquipSelectionChanged()
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

        if (diceEquipUiManager == null)
            return null;

        diceEquipUiManager.TryGetSelectedDice(out DiceSpinnerGeneric die);
        return die;
    }

    private int GetSelectedDiceCount()
    {
        return diceEquipUiManager != null ? diceEquipUiManager.GetSelectedDiceCount() : 0;
    }

    private DiceSpinnerGeneric ResolveSelectedCombatDie()
    {
        if (diceEquipUiManager == null)
            return null;

        diceEquipUiManager.TryGetSelectedDice(out DiceSpinnerGeneric die);
        return die;
    }

    private void ExecuteConsumable(int slotIndex, ConsumableDataSO data, CombatActor user, CombatActor target, DiceSpinnerGeneric targetDie)
    {
        if (data != null && data.effectId == ConsumableEffectId.DiceReroll)
        {
            if (!CanUseDiceRerollSelection(data, user, target))
                return;

            List<DiceSpinnerGeneric> selectedDice = new List<DiceSpinnerGeneric>();
            if (diceEquipUiManager != null)
                diceEquipUiManager.GetSelectedDice(selectedDice);
            List<DiceSpinnerGeneric> rerolledDice = new List<DiceSpinnerGeneric>(selectedDice);

            int pendingRerolls = 0;
            for (int i = 0; i < selectedDice.Count; i++)
            {
                DiceSpinnerGeneric die = selectedDice[i];
                if (die == null)
                    continue;

                pendingRerolls++;
                die.onRollComplete += HandleRerollRollComplete;
                die.RollRandomFace();
            }

            ClearLatchedDiceTarget();
            SyncInventoryDiceLayoutFromRig();

            ClearConsumableUiSelection(slotIndex);
            SuppressSlotClicksForCurrentInteraction(slotIndex);
            runInventory.TryConsumeConsumableCharge(slotIndex, 1);
            HandleConsumableUseSuccess(data);
            return;

            void HandleRerollRollComplete(DiceSpinnerGeneric rolledDie)
            {
                if (rolledDie == null)
                    return;

                rolledDie.onRollComplete -= HandleRerollRollComplete;
                pendingRerolls = Mathf.Max(0, pendingRerolls - 1);
                if (pendingRerolls > 0)
                    return;

                if (turnManager != null && turnManager.diceRig != null)
                {
                    turnManager.diceRig.RefreshRollInfoCache();
                    turnManager.RefreshPlanningAfterDiceValueReorder(rerolledDice);
                }
            }
        }

        ConsumableUseResult result = ConsumableRuntimeUtility.TryUseInCombat(data, user, target, runInventory, targetDie, turnManager);
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

        ClearConsumableUiSelection(slotIndex);
        SuppressSlotClicksForCurrentInteraction(slotIndex);
        runInventory.TryConsumeConsumableCharge(slotIndex, 1);
        HandleConsumableUseSuccess(data);
    }

    private void HandleConsumableUseSuccess(ConsumableDataSO data)
    {
        bool shouldClearDiceSelection = data != null &&
                                        (data.family == ConsumableFamily.Zodiac || data.UsesDiceSelection());

        if (shouldClearDiceSelection)
            diceEquipUiManager?.ClearSelectedDice(instant: false);
    }

    public void ClearSelectionForExternalConsumableResolution(int consumedSlot)
    {
        ClearConsumableUiSelection(consumedSlot);
        SuppressSlotClicksForCurrentInteraction(consumedSlot);
        RefreshAll();
    }
    private void ClearConsumableUiSelection(int consumedSlot)
    {
        _selectedSlot = -1;
        _pendingTargetSlot = -1;
        _pendingTargetActor = null;
        _hoveredSlot = -1;
        _ignoreHoverSlotUntilExit = consumedSlot;
        ClearLatchedDiceTarget();
        ClearUnitySelectedConsumableSlot();
        HideAllActionPanels();
        HideAllTooltips();
    }

    private void ClearUnitySelectedConsumableSlot()
    {
        EventSystem eventSystem = EventSystem.current;
        GameObject selectedObject = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
        if (selectedObject == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            ConsumableSlotView slot = slots[i];
            if (slot == null || slot.root == null)
                continue;

            if (selectedObject == slot.root.gameObject || selectedObject.transform.IsChildOf(slot.root))
            {
                eventSystem.SetSelectedGameObject(null);
                return;
            }
        }
    }

    private void SuppressSlotClicksForCurrentInteraction(int slotIndex)
    {
        _suppressSlotClicksUntilPointerRelease = true;
        _suppressSlotClicksUntilFrame = Mathf.Max(_suppressSlotClicksUntilFrame, Time.frameCount + 10);
        _suppressSlotClicksUntilTime = Mathf.Max(_suppressSlotClicksUntilTime, Time.unscaledTime + 0.35f);
        _suppressClickSlot = slotIndex;
    }

    private bool ShouldSuppressSlotClick(int slotIndex)
    {
        if (_suppressSlotClicksUntilPointerRelease)
        {
            if (Input.GetMouseButton(0))
                return true;

            _suppressSlotClicksUntilPointerRelease = false;
            _suppressClickSlot = -1;
        }

        if (slotIndex == _suppressClickSlot && Time.unscaledTime <= _suppressSlotClicksUntilTime)
            return true;

        return Time.frameCount <= _suppressSlotClicksUntilFrame;
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

    private bool IsInteractionLocked()
    {
        CombatActor user = ResolveUser();
        if (user == null || user.IsDead)
            return true;

        return turnManager != null && turnManager.ArePlayerCommandsLocked;
    }
}


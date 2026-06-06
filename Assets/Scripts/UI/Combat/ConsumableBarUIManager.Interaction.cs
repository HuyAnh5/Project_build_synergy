using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public partial class ConsumableBarUIManager
{
    public void HandleSlotBeginDrag(int index, PointerEventData eventData)
    {
        if (_rewardChoiceMode)
            return;

        if (IsInteractionLocked())
            return;

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
        RefreshFloatingPresentation();
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


    private void UseSelectedConsumable()
    {
        if (IsInteractionLocked())
            return;

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
        DiceSpinnerGeneric selectedCombatDie = ResolveSelectedCombatDie();
        if (IsReadyToOpenDiceEdit(data))
        {
            Debug.Log($"[ConsumableBarUI] Attempt open dice edit for slot={_selectedSlot} data={data.displayName}", this);
            if (diceEditController.TryOpenPanelForDie(_selectedSlot, data, selectedCombatDie))
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
        if (IsInteractionLocked())
            return;

        if (runInventory == null || _selectedSlot < 0)
            return;

        int consumedSlot = _selectedSlot;
        ClearConsumableUiSelection(consumedSlot);
        runInventory.ClearConsumable(consumedSlot);
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
        if (UiDragState.IsDragging)
        {
            _hoveredSlot = -1;
            HideAllTooltips();
            return;
        }

        if (TryGetHoveredSlotUnderPointer(out int hoveredSlot))
            _hoveredSlot = hoveredSlot;

        RefreshFloatingPresentation();
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
               data.targetKind == ConsumableTargetKind.DiceFace &&
               diceEditController != null;
    }

    private bool CanUseFromActionPanel(ConsumableDataSO data, CombatActor user, CombatActor target, DiceSpinnerGeneric targetDie, DiceSpinnerGeneric selectedCombatDie)
    {
        if (IsInteractionLocked())
            return false;

        if (data == null)
            return false;

        if (data.UsesDiceSelection() && !data.MatchesSelectedDiceCount(GetSelectedDiceCount()))
            return false;

        if (IsReadyToOpenDiceEdit(data))
            return diceEditController.CanOpenPanelForDie(_selectedSlot, data, selectedCombatDie);

        if (data.effectId == ConsumableEffectId.DiceReroll)
            return CanUseDiceRerollSelection(data, user, target);

        return ConsumableRuntimeUtility.CanUseInCombat(data, user, target, runInventory, targetDie, turnManager);
    }

    private bool CanUseDiceRerollSelection(ConsumableDataSO data, CombatActor user, CombatActor target)
    {
        if (data == null || diceEquipUiManager == null)
            return false;

        List<DiceSpinnerGeneric> selectedDice = new List<DiceSpinnerGeneric>();
        diceEquipUiManager.GetSelectedDice(selectedDice);
        if (selectedDice.Count <= 0)
            return false;

        for (int i = 0; i < selectedDice.Count; i++)
        {
            if (!ConsumableRuntimeUtility.CanUseInCombat(data, user, target, runInventory, selectedDice[i], turnManager))
                return false;
        }

        return true;
    }

    private bool TryGetHoveredSlotUnderPointer(out int hoveredSlot)
    {
        hoveredSlot = -1;
        if (slots == null)
            return false;

        Vector2 screenPoint = Input.mousePosition;
        for (int i = 0; i < slots.Length; i++)
        {
            ConsumableSlotView slot = slots[i];
            if (slot == null || slot.root == null || !slot.root.gameObject.activeInHierarchy)
                continue;

            Canvas slotCanvas = slot.root.GetComponentInParent<Canvas>();
            Camera eventCamera = slotCanvas != null && slotCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? slotCanvas.worldCamera
                : null;

            if (!RectTransformUtility.RectangleContainsScreenPoint(slot.root, screenPoint, eventCamera))
                continue;

            if (runInventory == null || runInventory.GetConsumable(i) == null)
                continue;

            if (i == _ignoreHoverSlotUntilExit)
                continue;

            hoveredSlot = i;
            return true;
        }

        return false;
    }

}




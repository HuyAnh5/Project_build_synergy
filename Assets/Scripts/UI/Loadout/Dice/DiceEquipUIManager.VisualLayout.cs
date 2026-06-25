using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class DiceEquipUIManager
{
    private const string RuntimeDiceCardName = "DiceCard";

    private void RefreshFromAuthoritativeOrder(bool instant)
    {
        CacheDiceUi();
        BuildOrderedDiceFromRuntime();
        RebuildUiOrderFromDiceList();
        UpdateEquippedArray();
        SyncOutputs(notifyInventoryChanged: false);
        ApplyUiActiveStates();
        RefreshRowLayout(instant);
        RefreshCombatDiceRuntimeState(instant);
        RefreshDiceSelectionVisuals(instant);
        RebindCombatLaneIndices();
        RebindWorldSlotOwners();
        SyncWorldSlotRootsToUI(instant);
    }

    private void BuildOrderedDiceFromRuntime()
    {
        _orderedDice.Clear();

        if (ShouldUseDiceRigOrderForRefresh() && diceRig != null && diceRig.slots != null)
        {
            for (int i = 0; i < diceRig.slots.Length; i++)
            {
                DiceSpinnerGeneric dice = diceRig.slots[i] != null ? diceRig.slots[i].dice : null;
                if (dice != null)
                    _orderedDice.Add(dice);
            }

            return;
        }

        if (runInventory != null)
        {
            for (int i = 0; i < RunInventoryManager.EQUIPPED_DICE_COUNT; i++)
            {
                DiceSpinnerGeneric dice = runInventory.GetEquippedDice(i);
                if (dice != null)
                    _orderedDice.Add(dice);
            }

            return;
        }

        foreach (KeyValuePair<DiceSpinnerGeneric, DiceDraggableUI> pair in _uiByDice)
        {
            if (pair.Key != null)
                _orderedDice.Add(pair.Key);
        }
    }

    private void CacheDiceUi()
    {
        CleanupManagedEnchantHoverZones();
        CleanupEmptyManagedDiceCards();
        _uiByDice.Clear();
        HashSet<DiceDraggableUI> seen = new HashSet<DiceDraggableUI>();
        CacheDiceUiFromRoot(transform, seen);
        if (layoutContainer != null && layoutContainer != transform)
            CacheDiceUiFromRoot(layoutContainer, seen);
        if (dragLayer != null && dragLayer != transform)
            CacheDiceUiFromRoot(dragLayer, seen);
    }

    private void CacheDiceUiFromRoot(Transform root, HashSet<DiceDraggableUI> seen)
    {
        if (root == null)
            return;

        DiceDraggableUI[] diceUi = root.GetComponentsInChildren<DiceDraggableUI>(true);
        for (int i = 0; i < diceUi.Length; i++)
        {
            DiceDraggableUI ui = diceUi[i];
            if (ui == null || !seen.Add(ui) || ui.dice == null)
                continue;

            if (_uiByDice.TryGetValue(ui.dice, out DiceDraggableUI existing) && existing != null)
            {
                DiceDraggableUI keep = ChooseDiceUiToKeep(existing, ui);
                DiceDraggableUI remove = keep == existing ? ui : existing;
                if (keep != existing)
                {
                    _uiByDice[ui.dice] = keep;
                    Register(keep);
                }

                DestroyDuplicateDiceUi(remove);
                continue;
            }

            _uiByDice.Add(ui.dice, ui);
            Register(ui);
        }
    }

    private void CleanupEmptyManagedDiceCards()
    {
        if (!Application.isPlaying)
            return;

        HashSet<DiceDraggableUI> seen = new HashSet<DiceDraggableUI>();
        CleanupEmptyManagedDiceCardsFromRoot(transform, seen);
        if (layoutContainer != null && layoutContainer != transform)
            CleanupEmptyManagedDiceCardsFromRoot(layoutContainer, seen);
        if (dragLayer != null && dragLayer != transform)
            CleanupEmptyManagedDiceCardsFromRoot(dragLayer, seen);
    }

    private static void CleanupEmptyManagedDiceCardsFromRoot(Transform root, HashSet<DiceDraggableUI> seen)
    {
        if (root == null)
            return;

        DiceDraggableUI[] diceUi = root.GetComponentsInChildren<DiceDraggableUI>(true);
        for (int i = 0; i < diceUi.Length; i++)
        {
            DiceDraggableUI ui = diceUi[i];
            if (ui == null || !seen.Add(ui) || ui.dice != null)
                continue;

            DestroyManagedObject(ui.gameObject);
        }
    }

    private static DiceDraggableUI ChooseDiceUiToKeep(DiceDraggableUI current, DiceDraggableUI candidate)
    {
        if (current == null)
            return candidate;
        if (candidate == null)
            return current;

        if (!current.gameObject.activeSelf && candidate.gameObject.activeSelf)
            return candidate;

        return current;
    }

    private static void DestroyDuplicateDiceUi(DiceDraggableUI duplicate)
    {
        if (duplicate == null)
            return;

        DestroyManagedObject(duplicate.gameObject);
    }

    private void CleanupManagedEnchantHoverZones()
    {
        RectTransform hoverContainer = GetLayoutContainer();
        DiceEnchantHoverProxy[] proxies = Object.FindObjectsByType<DiceEnchantHoverProxy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < proxies.Length; i++)
        {
            DiceEnchantHoverProxy proxy = proxies[i];
            if (proxy == null || !proxy.name.StartsWith("EnchantHoverZone_"))
                continue;

            DiceDraggableUI owner = proxy.Owner;
            bool orphaned = owner == null;
            bool belongsToThisManager = owner != null && owner.manager == this;
            bool wrongParent = hoverContainer != null && proxy.transform.parent != hoverContainer;
            if (orphaned || (belongsToThisManager && wrongParent))
                DestroyManagedObject(proxy.gameObject);
        }
    }

    private static void DestroyManagedObject(GameObject target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(target);
        else
            Object.DestroyImmediate(target);
    }

    private void RebuildUiOrderFromDiceList()
    {
        EnsureUiExistsForOrderedDice();
        _orderedUi.Clear();
        for (int i = 0; i < _orderedDice.Count; i++)
        {
            if (_uiByDice.TryGetValue(_orderedDice[i], out DiceDraggableUI ui) && ui != null)
                _orderedUi.Add(ui);
        }
    }

    private void EnsureUiExistsForOrderedDice()
    {
        if (!autoCreateMissingUi)
            return;

        for (int i = 0; i < _orderedDice.Count; i++)
        {
            DiceSpinnerGeneric dice = _orderedDice[i];
            if (dice == null || _uiByDice.ContainsKey(dice))
                continue;

            DiceDraggableUI created = CreateRuntimeDiceUi(dice, i);
            if (created == null)
                continue;

            _uiByDice[dice] = created;
            Register(created);
        }
    }

    private DiceDraggableUI CreateRuntimeDiceUi(DiceSpinnerGeneric dice, int orderIndex)
    {
        if (dice == null)
            return null;

        Transform parent = GetLayoutContainer() != null ? GetLayoutContainer() : transform;
        GameObject go = new GameObject(RuntimeDiceCardName, typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(DiceDraggableUI));
        go.layer = gameObject.layer;
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = diceUiSize;

        Image image = go.GetComponent<Image>();
        image.color = GetDefaultDiceUiColor(orderIndex);
        image.raycastTarget = true;

        DiceDraggableUI diceUi = go.GetComponent<DiceDraggableUI>();
        diceUi.dice = dice;
        diceUi.manager = this;
        diceUi.backgroundImage = image;
        diceUi.tweenDuration = itemSnapDuration;
        return diceUi;
    }

    private void UpdateEquippedArray()
    {
        for (int i = 0; i < equipped.Length; i++)
            equipped[i] = i < _orderedUi.Count ? _orderedUi[i] : null;
    }

    private void ApplyUiActiveStates()
    {
        foreach (KeyValuePair<DiceSpinnerGeneric, DiceDraggableUI> pair in _uiByDice)
        {
            bool active = _orderedDice.Contains(pair.Key);
            if (pair.Value != null && pair.Value.gameObject.activeSelf != active)
                pair.Value.gameObject.SetActive(active);
        }
    }

    private void RefreshVisualState(bool instant)
    {
        ApplyUiActiveStates();
        RefreshRowLayout(instant);
        RefreshCombatDiceRuntimeState(instant);
        RefreshDiceSelectionVisuals(instant);
        RebindCombatLaneIndices();
        RebindWorldSlotOwners();
        SyncWorldSlotRootsToUI(instant);
    }

    private void RefreshRowLayout(bool instant)
    {
        RectTransform container = GetLayoutContainer();
        if (container == null)
            return;

        List<DiceDraggableUI> displayed = BuildDisplayedOrder();
        for (int i = 0; i < displayed.Count; i++)
        {
            DiceDraggableUI diceUi = displayed[i];
            if (diceUi == null || diceUi == _draggingDice)
                continue;

            Register(diceUi);
            diceUi.SnapToAnchorAnimated(container, GetPositionForIndex(i, displayed.Count), instant);
        }
    }

    private List<DiceDraggableUI> BuildDisplayedOrder()
    {
        List<DiceDraggableUI> displayed = new List<DiceDraggableUI>(_orderedUi.Count);
        if (_draggingDice == null || _dragSourceIndex < 0 || _previewInsertIndex < 0)
        {
            displayed.AddRange(_orderedUi);
            return displayed;
        }

        for (int i = 0; i < _orderedUi.Count; i++)
        {
            DiceDraggableUI current = _orderedUi[i];
            if (current == _draggingDice)
                continue;

            if (displayed.Count == _previewInsertIndex)
                displayed.Add(_draggingDice);

            displayed.Add(current);
        }

        if (!displayed.Contains(_draggingDice))
            displayed.Add(_draggingDice);

        return displayed;
    }

    private Vector2 GetPositionForIndex(int index, int count)
    {
        float x = (index - (count - 1) / 2f) * spacing;
        return new Vector2(x, rowY);
    }

    private int GetInsertIndexFromScreenPosition(Vector2 screenPosition, Camera eventCamera)
    {
        RectTransform container = GetLayoutContainer();
        if (container == null || _orderedUi.Count <= 1)
            return 0;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screenPosition, eventCamera, out Vector2 local))
            return Mathf.Clamp(_dragSourceIndex, 0, Mathf.Max(0, _orderedUi.Count - 1));

        for (int i = 0; i < _orderedUi.Count - 1; i++)
        {
            float midpoint = (GetPositionForIndex(i, _orderedUi.Count).x + GetPositionForIndex(i + 1, _orderedUi.Count).x) * 0.5f;
            if (local.x < midpoint)
                return i;
        }

        return _orderedUi.Count - 1;
    }

    private int GetClosestTwoSlotStartFromScreenPosition(Vector2 screenPosition, Camera eventCamera)
    {
        if (_orderedUi.Count <= 2)
            return 0;

        RectTransform container = GetLayoutContainer();
        if (container == null)
            return 0;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screenPosition, eventCamera, out Vector2 local))
            return 0;

        float firstCenter = (GetPositionForIndex(0, _orderedUi.Count).x + GetPositionForIndex(1, _orderedUi.Count).x) * 0.5f;
        float secondCenter = (GetPositionForIndex(1, _orderedUi.Count).x + GetPositionForIndex(2, _orderedUi.Count).x) * 0.5f;
        return Mathf.Abs(local.x - firstCenter) <= Mathf.Abs(local.x - secondCenter) ? 0 : 1;
    }

    private void ToggleSelectedDice(DiceDraggableUI dice, bool instant = false)
    {
        if (dice == null)
            return;

        if (_selectedDice.Contains(dice))
            RemoveSelectedDice(dice, instant);
        else
            AddSelectedDice(dice, instant);
    }

    private void ClearDiceSelection(bool instant = false)
    {
        if (_selectedDice.Count == 0)
            return;

        for (int i = 0; i < _selectedDice.Count; i++)
        {
            if (_selectedDice[i] != null)
                _selectedDice[i].SetSelected(false, instant);
        }

        _selectedDice.Clear();
        SelectionChanged?.Invoke();
    }

    private void RefreshDiceSelectionVisuals(bool instant)
    {
        PruneSelection();

        foreach (KeyValuePair<DiceSpinnerGeneric, DiceDraggableUI> pair in _uiByDice)
        {
            DiceDraggableUI diceUi = pair.Value;
            if (diceUi == null)
                continue;

            diceUi.SetSelected(_selectedDice.Contains(diceUi), instant);
        }
    }

    private void AddSelectedDice(DiceDraggableUI dice, bool instant)
    {
        if (dice == null || _selectedDice.Contains(dice))
            return;

        _selectedDice.Add(dice);
        dice.SetSelected(true, instant);
        SelectionChanged?.Invoke();
    }

    private void RemoveSelectedDice(DiceDraggableUI dice, bool instant)
    {
        if (dice == null)
            return;

        if (!_selectedDice.Remove(dice))
            return;

        dice.SetSelected(false, instant);
        SelectionChanged?.Invoke();
    }

    private void PruneSelection()
    {
        bool changed = false;
        for (int i = _selectedDice.Count - 1; i >= 0; i--)
        {
            DiceDraggableUI diceUi = _selectedDice[i];
            if (diceUi == null || !_orderedUi.Contains(diceUi))
            {
                _selectedDice.RemoveAt(i);
                changed = true;
            }
        }

        if (changed)
            SelectionChanged?.Invoke();
    }

    private void RebindCombatLaneIndices()
    {
        DiceEquipStateUtility.RebindCombatSlotLaneIndices(linkedCombatSlotAnchors, turnManager);
    }

    private void RebindWorldSlotOwners()
    {
        DiceEquipWorldSyncUtility.RebindWorldSlotOwnersFromCurrentOrder(equipped, _worldSlotOwners, _worldSlotRoots, diceRig);
    }

    private void RefreshCombatDiceRuntimeState(bool instant)
    {
        RefreshTurnManagerRef();

        bool combatRigReady = turnManager != null && turnManager.diceRig != null;

        foreach (KeyValuePair<DiceSpinnerGeneric, DiceDraggableUI> pair in _uiByDice)
        {
            DiceSpinnerGeneric die = pair.Key;
            DiceDraggableUI diceUi = pair.Value;
            if (die == null || diceUi == null)
                continue;

            bool dieResolvedThisRoll = combatRigReady && !die.IsRolling && die.LastFaceIndex >= 0;
            bool pendingUsedVisual = dieResolvedThisRoll && turnManager.IsDiePendingUsedVisualThisTurn(die);
            bool spent = dieResolvedThisRoll && turnManager.ShouldShowDieAsSpentVisual(die);
            bool crit = dieResolvedThisRoll && die.LastRollIsCrit;
            bool fail = dieResolvedThisRoll && die.LastRollIsFail;
            if (pendingUsedVisual)
                diceUi.SetPreviewSpentLike(true, true);
            else
                diceUi.ClearPreviewSpentLike(instant, suppressMove: false);
            if (spent)
                diceUi.ClearPreviewSpentLike(true, true);
            die.SetCombatRollFeedback(crit, fail);
            diceUi.SetCombatVisualState(spent, crit, fail, instant);
        }
    }

}


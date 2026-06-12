using UnityEngine;

// Owns panel entry/exit and runtime reference binding for the combat dice editor.
public partial class GameplayDiceEditController
{
    // Checks whether the currently selected scene dice can be edited with the given Zodiac.
    public bool CanOpenPanelFromSelections(int slotIndex, ConsumableDataSO data)
    {
        AutoResolveReferences();
        AttachToDiceRig();
        if (data == null || data.family != ConsumableFamily.Zodiac)
            return false;
        if (runInventory == null || slotIndex < 0 || runInventory.GetConsumable(slotIndex) != data)
            return false;
        if (ResolveSelectedSceneInteractable() == null)
            return false;
        return true;
    }

    // Opens the edit panel using the dice selected in the scene.
    public bool TryOpenPanelFromSelections(int slotIndex, ConsumableDataSO data)
    {
        AutoResolveReferences();
        if (!CanOpenPanelFromSelections(slotIndex, data))
        {
            Debug.LogWarning(
                $"[GameplayDiceEdit] Cannot open panel. slot={slotIndex} data={(data != null ? data.displayName : "NULL")} " +
                $"selectedDie={(_selectedInteractable != null && _selectedInteractable.Spinner != null ? _selectedInteractable.Spinner.name : "NULL")} " +
                $"runInventory={(runInventory != null ? runInventory.name : "NULL")}",
                this);
            return false;
        }

        AttachToDiceRig();
        _pendingConsumableSlot = slotIndex;
        _activeConsumable = data;
        _selectedLogicalFaceIndices.Clear();
        _copySourceFaceIndex = -1;
        _copyTargetFaceIndex = -1;
        GameplayDiceEditInteractable selectedInteractable = ResolveSelectedSceneInteractable();
        Log($"Opening panel from selections. slot={slotIndex} zodiac={data.displayName} die={(selectedInteractable != null && selectedInteractable.Spinner != null ? selectedInteractable.Spinner.name : "NULL")}");
        OpenPanelForInteractable(selectedInteractable);

        return true;
    }

    // Checks whether a specific scene dice can be edited directly.
    public bool CanOpenPanelForDie(int slotIndex, ConsumableDataSO data, DiceSpinnerGeneric die)
    {
        AutoResolveReferences();
        AttachToDiceRig();
        if (data == null || data.family != ConsumableFamily.Zodiac)
            return false;
        if (runInventory == null || slotIndex < 0 || runInventory.GetConsumable(slotIndex) != data)
            return false;
        if (die == null)
            return false;
        return ResolveSceneInteractableForSpinner(die) != null;
    }

    // Opens the edit panel for an explicit scene dice.
    public bool TryOpenPanelForDie(int slotIndex, ConsumableDataSO data, DiceSpinnerGeneric die)
    {
        AutoResolveReferences();
        if (!CanOpenPanelForDie(slotIndex, data, die))
            return false;

        AttachToDiceRig();
        _pendingConsumableSlot = slotIndex;
        _activeConsumable = data;
        _selectedLogicalFaceIndices.Clear();
        _copySourceFaceIndex = -1;
        _copyTargetFaceIndex = -1;
        GameplayDiceEditInteractable selectedInteractable = ResolveSceneInteractableForSpinner(die);
        OpenPanelForInteractable(selectedInteractable);
        return true;
    }

    // Opens the inspect panel without binding a consumable, used by click-and-hold on combat dice.
    public bool TryOpenInspectPanelForDie(DiceSpinnerGeneric die)
    {
        AutoResolveReferences();
        AttachToDiceRig();
        if (die == null)
            return false;
        if (turnManager != null && turnManager.phase != TurnManager.Phase.Planning)
            return false;
        if (IsPanelOpen)
            return false;

        GameplayDiceEditInteractable selectedInteractable = ResolveSceneInteractableForSpinner(die);
        if (selectedInteractable == null)
            return false;

        _pendingConsumableSlot = -1;
        _activeConsumable = null;
        _selectedLogicalFaceIndices.Clear();
        _copySourceFaceIndex = -1;
        _copyTargetFaceIndex = -1;
        OpenPanelForInteractable(selectedInteractable);
        return true;
    }

    // Tells external UI that a Zodiac is selected but the player has not picked a dice yet.
    public bool IsAwaitingDieSelection()
    {
        return _pendingConsumableSlot >= 0 && _activeConsumable != null && !IsPanelOpen;
    }

    // Returns the selected scene dice, never the temporary inspect clone.
    public bool TryGetSelectedSceneDie(out DiceSpinnerGeneric die)
    {
        GameplayDiceEditInteractable interactable = ResolveSelectedSceneInteractable();
        die = interactable != null ? interactable.Spinner : null;
        return die != null;
    }

    // Closes the edit panel and restores selection to the original scene dice.
    public void CancelAndClose()
    {
        GameplayDiceEditInteractable sourceInteractable = ResolveSceneInteractableForSpinner(_sourceInteractable != null ? _sourceInteractable.Spinner : null);
        DestroyInspectClone();
        _activeInteractable = null;
        _sourceInteractable = null;
        _selectedInteractable = sourceInteractable;
        _focusedInteractable = sourceInteractable;
        _pendingConsumableSlot = -1;
        _activeConsumable = null;
        _selectedLogicalFaceIndices.Clear();
        _inspectFaceMarks.Clear();
        _inspectActiveMarkColorIndex = 0;
        _copySourceFaceIndex = -1;
        _copyTargetFaceIndex = -1;
        RefreshAllHighlights();
        if (panelUi != null)
        {
            panelUi.SetVisible(false);
            panelUi.Refresh();
        }
        SelectionStateChanged?.Invoke();
    }

    // Provides the active Zodiac name for the panel header.
    public string GetDisplayName()
    {
        if (IsInspectOnlyMode && _sourceInteractable != null && _sourceInteractable.Spinner != null)
            return _sourceInteractable.Spinner.name;

        return _activeConsumable != null ? _activeConsumable.displayName : "No Zodiac";
    }

    // Provides the active Zodiac rules text for the panel body.
    public string GetEffectText()
    {
        if (IsInspectOnlyMode)
            return "Inspect this die. Click faces to cycle highlight colors for notes, use Roll or Face Up to examine it, or Cancel to close.";

        return _activeConsumable != null ? _activeConsumable.description : string.Empty;
    }

    // Resolves scene references lazily because combat UI objects can be spawned inactive.
    private void AutoResolveReferences()
    {
        if (runInventory == null)
            runInventory = FindFirstObjectByType<RunInventoryManager>(FindObjectsInactive.Include);
        if (diceRig == null && runInventory != null)
            diceRig = runInventory.DiceRig;
        if (diceRig == null)
            diceRig = FindFirstObjectByType<DiceSlotRig>(FindObjectsInactive.Include);
        if (turnManager == null)
            turnManager = FindFirstObjectByType<TurnManager>(FindObjectsInactive.Include);
        if (diceEquipUiManager == null)
            diceEquipUiManager = FindFirstObjectByType<DiceEquipUIManager>(FindObjectsInactive.Include);
        if (panelUi == null)
            panelUi = FindFirstObjectByType<GameplayDiceEditPanelUI>(FindObjectsInactive.Include);
    }

    // Ensures each equipped dice has an interactable facade for editor selection and highlighting.
    private void AttachToDiceRig()
    {
        _interactables.Clear();
        if (diceRig == null || diceRig.slots == null)
        {
            Log("AttachToDiceRig aborted because diceRig or slots is null.");
            return;
        }

        for (int i = 0; i < diceRig.slots.Length; i++)
        {
            DiceSpinnerGeneric die = diceRig.slots[i] != null ? diceRig.slots[i].dice : null;
            if (die == null)
                continue;

            GameplayDiceEditInteractable interactable = die.GetComponent<GameplayDiceEditInteractable>();
            if (interactable == null)
                interactable = die.gameObject.AddComponent<GameplayDiceEditInteractable>();

            interactable.Configure(this, die);
            _interactables.Add(interactable);
            Log($"Attached interactable to die '{die.name}' at slot={i}.");
        }
    }
}

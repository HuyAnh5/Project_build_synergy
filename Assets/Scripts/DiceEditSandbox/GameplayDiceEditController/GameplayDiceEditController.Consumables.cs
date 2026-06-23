// Validates and commits Zodiac consumable effects from the inspect dice back to the run dice.
public partial class GameplayDiceEditController
{
    // Checks whether the current Zodiac has all required dice/face selections.
    public bool CanUseCurrentConsumable()
    {
        if (!IsPanelOpen || _activeConsumable == null || _pendingConsumableSlot < 0)
            return false;
        if (runInventory == null || runInventory.GetConsumable(_pendingConsumableSlot) == null)
            return false;

        if (IsCopyPasteFaceMode())
            return _copySourceFaceIndex >= 0 && _copyTargetFaceIndex >= 0;

        switch (_activeConsumable.targetKind)
        {
            case ConsumableTargetKind.None:
            case ConsumableTargetKind.Self:
                return true;
            case ConsumableTargetKind.Dice:
                return _activeInteractable != null;
            case ConsumableTargetKind.DiceFace:
                return _selectedLogicalFaceIndices.Count > 0 &&
                       _selectedLogicalFaceIndices.Count <= GetSandboxFaceSelectionLimit();
            default:
                return false;
        }
    }

    // Applies the selected Zodiac effect, syncs the scene dice, consumes a charge, then closes the panel.
    public void UseCurrentConsumable()
    {
        if (!CanUseCurrentConsumable() || _activeInteractable == null || _activeConsumable == null || _sourceInteractable == null)
            return;

        DiceSpinnerGeneric die = _activeInteractable.Spinner;
        DiceSpinnerGeneric sourceDie = _sourceInteractable.Spinner;
        bool success = false;

        if (IsCopyPasteFaceMode())
        {
            ConsumableUseResult result = ConsumableRuntimeUtility.TryCopyPasteFace(die, _copySourceFaceIndex, die, _copyTargetFaceIndex);
            success = result.success;
        }
        else if (_activeConsumable.targetKind == ConsumableTargetKind.DiceFace)
        {
            success = TryApplyCurrentConsumableToSelectedFaces(die);
        }
        else
        {
            ConsumableUseResult result = ConsumableRuntimeUtility.TryUseInSandbox(_activeConsumable, die, -1);
            success = result.success;
        }

        if (!success)
        {
            panelUi?.Refresh();
            return;
        }

        CommitConsumableUseToRunDice(die, sourceDie);
        CancelAndClose();
    }

    // Applies face-targeting Zodiac effects to every selected face.
    private bool TryApplyCurrentConsumableToSelectedFaces(DiceSpinnerGeneric die)
    {
        for (int i = 0; i < _selectedLogicalFaceIndices.Count; i++)
        {
            ConsumableUseResult result = ConsumableRuntimeUtility.TryUseInSandbox(_activeConsumable, die, _selectedLogicalFaceIndices[i]);
            if (!result.success)
                return false;
        }

        return true;
    }

    // Copies inspect dice state to the real dice and updates inventory/UI charge state.
    private void CommitConsumableUseToRunDice(DiceSpinnerGeneric die, DiceSpinnerGeneric sourceDie)
    {
        if (sourceDie != null && die != null)
            sourceDie.CopyRuntimeStateFrom(die, copyRotation: _activeConsumable.effectId == ConsumableEffectId.SetRolledFace);
        if (sourceDie != null)
            ConsumableRuntimeUtility.NotifyDiceStateChanged(sourceDie, turnManager);

        if (consumableBarUi == null)
            consumableBarUi = ConsumableBarUiManagerRegistry.Get();

        consumableBarUi?.ClearSelectionForExternalConsumableResolution(_pendingConsumableSlot);
        runInventory.TryConsumeConsumableCharge(_pendingConsumableSlot, 1);
        diceEquipUiManager?.ClearSelectedDice();
    }

    // Returns the maximum number of faces the current Zodiac may select.
    private int GetSandboxFaceSelectionLimit()
    {
        if (_activeConsumable == null || _activeConsumable.targetKind != ConsumableTargetKind.DiceFace)
            return 0;

        switch (_activeConsumable.effectId)
        {
            case ConsumableEffectId.AdjustBaseValue:
                return 3;
            case ConsumableEffectId.ApplyFaceEnchant:
                return 1;
            case ConsumableEffectId.CopyPasteFace:
            case ConsumableEffectId.SetRolledFace:
                return 1;
            default:
                return 1;
        }
    }

    // Identifies the special two-face source/target Zodiac mode.
    private bool IsCopyPasteFaceMode()
    {
        return _activeConsumable != null && _activeConsumable.effectId == ConsumableEffectId.CopyPasteFace;
    }
}

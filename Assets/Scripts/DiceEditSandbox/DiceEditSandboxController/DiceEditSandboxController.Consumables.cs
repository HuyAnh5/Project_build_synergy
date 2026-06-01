using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Handles consumable selection and sandbox Zodiac application against selected dice faces.
// This file owns effect use flow, while Selection.cs owns face selection state.
public partial class DiceEditSandboxController
{
    /// <summary>Sets a consumable selected from the external sandbox UI and clears incompatible face state.</summary>
    public void SetSandboxSelectedConsumable(ConsumableDataSO data)
    {
        _sandboxSelectedConsumable = data;
        _selectedConsumableSlot = -1;
        ClearSandboxFaceSelection();
        RefreshAllHighlights();
        RefreshUi();
    }

    public ConsumableDataSO GetSelectedConsumableData()
    {
        if (_sandboxSelectedConsumable != null)
            return _sandboxSelectedConsumable;

        if (_inventory == null || _selectedConsumableSlot < 0)
            return null;

        return _inventory.GetConsumable(_selectedConsumableSlot);
    }

    public int GetSelectedConsumableCharges()
    {
        if (_sandboxSelectedConsumable != null)
            return _sandboxSelectedConsumable.GetStartingCharges();

        if (_inventory == null || _selectedConsumableSlot < 0)
            return 0;

        return _inventory.GetConsumableCharges(_selectedConsumableSlot);
    }

    public bool IsSelectedConsumableZodiac()
    {
        ConsumableDataSO data = GetSelectedConsumableData();
        return data != null && data.family == ConsumableFamily.Zodiac;
    }

    public bool CanUseSelectedConsumableFromUi()
    {
        return CanUseSelectedConsumable();
    }

    public void TryUseSelectedConsumableFromUi()
    {
        UseSelectedConsumable();
    }

    public void DeselectConsumableFromUi()
    {
        if (_sandboxSelectedConsumable != null)
        {
            _sandboxSelectedConsumable = null;
            ClearSandboxFaceSelection();
            RefreshAllHighlights();
            RefreshUi();
            return;
        }

        if (_selectedConsumableSlot < 0)
            return;

        _selectedConsumableSlot = -1;
        RefreshUi();
    }

    private void SelectConsumableSlot(int index)
    {
        if (_sandboxSelectedConsumable != null)
            return;

        if (_inventory == null)
            return;

        if (_selectedConsumableSlot == index)
            _selectedConsumableSlot = -1;
        else if (_inventory.GetConsumable(index) != null)
            _selectedConsumableSlot = index;

        RefreshUi();
    }

    private bool CanUseSelectedConsumable()
    {
        ConsumableDataSO data = GetSelectedConsumableData();
        if (data == null)
            return false;

        if (IsSandboxDropdownMode())
            return CanUseSandboxZodiac(data);

        ResolveConsumableTarget(out DiceSpinnerGeneric targetDie, out int targetFaceIndex);
        return ConsumableRuntimeUtility.CanUseInSandbox(data, targetDie, targetFaceIndex);
    }

    /// <summary>Applies the selected consumable through either inventory slots or sandbox-only Zodiac UI.</summary>
    private void UseSelectedConsumable()
    {
        ConsumableDataSO data = GetSelectedConsumableData();
        if (data == null)
            return;

        if (IsSandboxDropdownMode())
        {
            UseSandboxZodiac(data);
            return;
        }

        ResolveConsumableTarget(out DiceSpinnerGeneric targetDie, out int targetFaceIndex);
        ConsumableUseResult result = ConsumableRuntimeUtility.TryUseInSandbox(data, targetDie, targetFaceIndex);
        _lastUseMessage = result.success ? $"Result: {result.message}" : $"Cannot use: {result.message}";

        if (!result.success)
        {
            RefreshUi();
            return;
        }

        _inventory.TryConsumeConsumableCharge(_selectedConsumableSlot, 1);
        if (_inventory.GetConsumable(_selectedConsumableSlot) == null)
            _selectedConsumableSlot = -1;

        RefreshAllHighlights();
        RefreshUi();
    }

    private void RefreshConsumableButtons()
    {
        for (int i = 0; i < ConsumableSlotCount; i++)
        {
            Button button = _consumableButtons[i];
            TMP_Text label = _consumableButtonLabels[i];
            if (button == null || label == null)
                continue;

            ConsumableDataSO data = _inventory != null ? _inventory.GetConsumable(i) : null;
            int charges = _inventory != null ? _inventory.GetConsumableCharges(i) : 0;

            label.fontSize = 16;
            label.text = data != null
                ? $"{data.displayName}\n{xCharges(charges)}"
                : $"Slot {i + 1}\nTrong";

            button.interactable = data != null;

            Image image = button.GetComponent<Image>();
            if (image != null)
                image.color = i == _selectedConsumableSlot
                    ? new Color(0.48f, 0.34f, 0.12f, 1f)
                    : new Color(0.21f, 0.27f, 0.35f, 1f);
        }
    }

    private string BuildConsumableStatusText()
    {
        ConsumableDataSO data = GetSelectedConsumableData();
        if (data == null)
            return "Consumable: none";

        int charges = GetSelectedConsumableCharges();
        return $"Consumable: {data.displayName} | Charges: {charges} | Target: {data.targetKind}";
    }

    private void ResolveConsumableTarget(out DiceSpinnerGeneric die, out int faceIndex)
    {
        if (IsSandboxDropdownMode())
        {
            die = _selectedDie;
            faceIndex = _selectedLogicalFaceIndices.Count > 0 ? _selectedLogicalFaceIndices[0] : -1;
            return;
        }

        if (_committedDie != null && _committedLogicalFaceIndex >= 0)
        {
            die = _committedDie;
            faceIndex = _committedLogicalFaceIndex;
            return;
        }

        die = _selectedDie;
        faceIndex = _selectedLogicalFaceIndex;
    }

    private static string xCharges(int charges)
    {
        return $"x{Mathf.Max(0, charges)}";
    }

    private bool UsesExternalZodiacSandbox()
    {
        if (_externalZodiacPanel == null)
            _externalZodiacPanel = FindFirstObjectByType<DiceEditSandboxZodiacPanelUI>(FindObjectsInactive.Include);

        return _externalZodiacPanel != null;
    }

    private bool IsSandboxDropdownMode()
    {
        return _sandboxSelectedConsumable != null || UsesExternalZodiacSandbox();
    }

    private DiceSpinnerGeneric ResolveSelectedDie()
    {
        if (IsCopyPasteFaceMode())
        {
            if (_copyTargetDie != null)
                return _copyTargetDie;
            if (_copySourceDie != null)
                return _copySourceDie;
        }

        if (_selectedDie != null)
            return _selectedDie;
        if (_committedDie != null)
            return _committedDie;
        return null;
    }

    private void ClearSandboxFaceSelection()
    {
        _selectedDie = null;
        _committedDie = null;
        _selectedLogicalFaceIndex = -1;
        _committedLogicalFaceIndex = -1;
        _selectedLogicalFaceIndices.Clear();
        ClearCopyPasteSelection();
    }

    private void ToggleSandboxFaceSelection(DiceSpinnerGeneric die, int logicalFaceIndex)
    {
        if (die == null)
            return;

        if (_selectedDie != die)
        {
            _selectedDie = die;
            _committedDie = null;
            _committedLogicalFaceIndex = -1;
            _selectedLogicalFaceIndices.Clear();
        }

        int limit = GetSandboxFaceSelectionLimit();
        if (limit <= 0)
        {
            _selectedLogicalFaceIndex = -1;
            RefreshAllHighlights();
            RefreshUi();
            return;
        }

        if (_selectedLogicalFaceIndices.Contains(logicalFaceIndex))
            _selectedLogicalFaceIndices.Remove(logicalFaceIndex);
        else if (_selectedLogicalFaceIndices.Count < limit)
            _selectedLogicalFaceIndices.Add(logicalFaceIndex);

        _selectedLogicalFaceIndex = _selectedLogicalFaceIndices.Count > 0
            ? _selectedLogicalFaceIndices[_selectedLogicalFaceIndices.Count - 1]
            : -1;

        RefreshAllHighlights();
        RefreshUi();
    }

    private bool CanUseSandboxZodiac(ConsumableDataSO data)
    {
        if (data == null || data.useContext == ConsumableUseContext.Combat || !IsSandboxSupportedZodiac(data))
            return false;

        if (data.effectId == ConsumableEffectId.CopyPasteFace)
            return _copySourceDie != null &&
                   _copySourceLogicalFaceIndex >= 0 &&
                   _copyTargetDie != null &&
                   _copyTargetLogicalFaceIndex >= 0;

        switch (data.targetKind)
        {
            case ConsumableTargetKind.None:
            case ConsumableTargetKind.Self:
                return true;
            case ConsumableTargetKind.Dice:
                return ResolveSelectedDie() != null;
            case ConsumableTargetKind.DiceFace:
                return ResolveSelectedDie() != null &&
                       _selectedLogicalFaceIndices.Count > 0 &&
                       _selectedLogicalFaceIndices.Count <= GetSandboxFaceSelectionLimit(data);
            default:
                return false;
        }
    }

    private void UseSandboxZodiac(ConsumableDataSO data)
    {
        if (!CanUseSandboxZodiac(data))
        {
            _lastUseMessage = "Cannot use: current Zodiac does not have a valid dice selection.";
            RefreshUi();
            return;
        }

        if (data.effectId == ConsumableEffectId.CopyPasteFace)
        {
            ConsumableUseResult copyResult = ConsumableRuntimeUtility.TryCopyPasteFace(
                _copySourceDie,
                _copySourceLogicalFaceIndex,
                _copyTargetDie,
                _copyTargetLogicalFaceIndex);
            _lastUseMessage = copyResult.success ? $"Result: {copyResult.message}" : $"Cannot use: {copyResult.message}";
            if (copyResult.success)
            {
                if (_copySourceDie != null)
                    ConsumableRuntimeUtility.NotifyDiceStateChanged(_copySourceDie);
                if (_copyTargetDie != null && _copyTargetDie != _copySourceDie)
                    ConsumableRuntimeUtility.NotifyDiceStateChanged(_copyTargetDie);
                ClearSandboxFaceSelection();
                ForceClearAllHighlights();
                DestroyAllHighlightObjectsGlobally();
                _pendingHighlightPurge = true;
                RefreshAllHighlights();
            }

            RefreshUi();
            return;
        }

        if (data.targetKind == ConsumableTargetKind.DiceFace)
        {
            DiceSpinnerGeneric die = ResolveSelectedDie();
            List<string> successMessages = new List<string>();

            for (int i = 0; i < _selectedLogicalFaceIndices.Count; i++)
            {
                ConsumableUseResult result = ConsumableRuntimeUtility.TryUseInSandbox(data, die, _selectedLogicalFaceIndices[i]);
                if (!result.success)
                {
                    _lastUseMessage = $"Cannot use: {result.message}";
                    RefreshUi();
                    return;
                }

                successMessages.Add(result.message);
            }

            _lastUseMessage = successMessages.Count > 0
                ? $"Result: {string.Join(" | ", successMessages)}"
                : "Result: no face changed.";
            if (die != null)
                ConsumableRuntimeUtility.NotifyDiceStateChanged(die);
            ClearSandboxFaceSelection();
            ForceClearAllHighlights();
            DestroyAllHighlightObjectsGlobally();
            _pendingHighlightPurge = true;
            RefreshAllHighlights();
            RefreshUi();
            return;
        }

        ResolveConsumableTarget(out DiceSpinnerGeneric targetDie, out int targetFaceIndex);
        ConsumableUseResult singleResult = ConsumableRuntimeUtility.TryUseInSandbox(data, targetDie, targetFaceIndex);
        _lastUseMessage = singleResult.success ? $"Result: {singleResult.message}" : $"Cannot use: {singleResult.message}";
        if (singleResult.success)
        {
            if (targetDie != null)
                ConsumableRuntimeUtility.NotifyDiceStateChanged(targetDie);
            ClearSandboxFaceSelection();
            ForceClearAllHighlights();
            DestroyAllHighlightObjectsGlobally();
            _pendingHighlightPurge = true;
            RefreshAllHighlights();
        }

        RefreshUi();
    }

    private int GetSandboxFaceSelectionLimit(ConsumableDataSO data)
    {
        if (data == null || data.targetKind != ConsumableTargetKind.DiceFace)
            return 0;

        switch (data.effectId)
        {
            case ConsumableEffectId.AdjustBaseValue:
                return 3;
            case ConsumableEffectId.ApplyFaceEnchant:
                return 2;
            case ConsumableEffectId.CopyPasteFace:
                return 1;
            default:
                return 1;
        }
    }

    private static bool IsSandboxSupportedZodiac(ConsumableDataSO data)
    {
        if (data == null)
            return false;

        switch (data.effectId)
        {
            case ConsumableEffectId.AdjustBaseValue:
            case ConsumableEffectId.ApplyFaceEnchant:
            case ConsumableEffectId.CopyPasteFace:
                return true;
            default:
                return false;
        }
    }

    private bool IsCopyPasteFaceMode()
    {
        ConsumableDataSO data = GetSelectedConsumableData();
        return data != null &&
               data.family == ConsumableFamily.Zodiac &&
               data.effectId == ConsumableEffectId.CopyPasteFace;
    }

    private void SetCopyPasteSelection(DiceSpinnerGeneric die, int logicalFaceIndex)
    {
        if (die == null || logicalFaceIndex < 0)
            return;

        bool clickedSource = _copySourceDie == die && _copySourceLogicalFaceIndex == logicalFaceIndex;
        bool clickedTarget = _copyTargetDie == die && _copyTargetLogicalFaceIndex == logicalFaceIndex;

        if (clickedSource)
        {
            _copySourceDie = null;
            _copySourceLogicalFaceIndex = -1;
        }
        else if (clickedTarget)
        {
            _copyTargetDie = null;
            _copyTargetLogicalFaceIndex = -1;
        }
        else if (_copySourceDie == null || _copySourceLogicalFaceIndex < 0)
        {
            _copySourceDie = die;
            _copySourceLogicalFaceIndex = logicalFaceIndex;
        }
        else if (_copyTargetDie == null || _copyTargetLogicalFaceIndex < 0)
        {
            _copyTargetDie = die;
            _copyTargetLogicalFaceIndex = logicalFaceIndex;
        }
        else
        {
            // Both copy slots are occupied; require the user to clear one by clicking its highlight.
            RefreshAllHighlights();
            RefreshUi();
            return;
        }

        if (_copySourceDie == null || _copySourceLogicalFaceIndex < 0)
        {
            _selectedDie = _copyTargetDie;
            _selectedLogicalFaceIndex = _copyTargetLogicalFaceIndex;
        }
        else if (_copyTargetDie == null || _copyTargetLogicalFaceIndex < 0)
        {
            _selectedDie = _copySourceDie;
            _selectedLogicalFaceIndex = _copySourceLogicalFaceIndex;
        }
        else
        {
            _selectedDie = die;
            _selectedLogicalFaceIndex = logicalFaceIndex;
        }

        if (_selectedDie == null)
        {
            _selectedLogicalFaceIndex = -1;
            _committedDie = null;
            _committedLogicalFaceIndex = -1;
        }

        RefreshAllHighlights();
        RefreshUi();
    }

    private void ClearCopyPasteSelection()
    {
        _copySourceDie = null;
        _copySourceLogicalFaceIndex = -1;
        _copyTargetDie = null;
        _copyTargetLogicalFaceIndex = -1;
    }
}

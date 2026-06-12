using TMPro;
using UnityEngine;

// Builds and tears down the temporary inspect dice used by the Zodiac edit panel.
public partial class GameplayDiceEditController
{
    // Opens modal editing for the selected scene dice.
    private void OpenPanelForInteractable(GameplayDiceEditInteractable interactable)
    {
        if (interactable == null)
            return;

        DestroyInspectClone();
        _sourceInteractable = interactable;
        _selectedInteractable = interactable;
        _selectedLogicalFaceIndices.Clear();
        _inspectFaceMarks.Clear();
        _inspectActiveMarkColorIndex = 0;
        _copySourceFaceIndex = -1;
        _copyTargetFaceIndex = -1;
        CreateInspectCloneFromSource();

        if (panelUi != null)
        {
            panelUi.SetVisible(true);
            panelUi.Refresh();
        }

        RefreshAllHighlights();
        SelectionStateChanged?.Invoke();
    }

    // Clones the scene dice under the inspect anchor so edits can preview before commit.
    private void CreateInspectCloneFromSource()
    {
        if (_sourceInteractable == null || _sourceInteractable.Spinner == null || inspectAnchor == null)
            return;

        _inspectCloneInstance = Instantiate(_sourceInteractable.Spinner.gameObject, inspectAnchor);
        _inspectCloneInstance.name = $"{_sourceInteractable.Spinner.name}_InspectClone";
        Vector3 cloneLocalPosition = inspectLocalPosition;
        cloneLocalPosition.z += inspectExtraDistanceFromCamera;
        _inspectCloneInstance.transform.localPosition = cloneLocalPosition;
        _inspectCloneInstance.transform.localRotation = Quaternion.Euler(inspectLocalEuler);
        _inspectCloneInstance.transform.localScale = inspectLocalScale;
        DisableGameplayComponentsOnInspectClone(_inspectCloneInstance);

        DiceSpinnerGeneric cloneSpinner = _inspectCloneInstance.GetComponent<DiceSpinnerGeneric>();
        if (cloneSpinner == null)
            cloneSpinner = _inspectCloneInstance.AddComponent<DiceSpinnerGeneric>();
        cloneSpinner.enableSpaceKey = false;
        cloneSpinner.onRollComplete = null;
        cloneSpinner.ConfigureAsPreviewSandbox();

        cloneSpinner.CopyRuntimeStateFrom(_sourceInteractable.Spinner, copyRotation: true);
        cloneSpinner.ClearAllFacePreviews();

        GameplayDiceEditInteractable interactable = _inspectCloneInstance.GetComponent<GameplayDiceEditInteractable>();
        if (interactable == null)
            interactable = _inspectCloneInstance.AddComponent<GameplayDiceEditInteractable>();

        interactable.Configure(this, cloneSpinner);
        _activeInteractable = interactable;
        _focusedInteractable = interactable;
        Log(
            $"Created inspect clone. source={_sourceInteractable.Spinner.name} clone={cloneSpinner.name} parent={inspectAnchor.name} localPos={_inspectCloneInstance.transform.localPosition} localScale={_inspectCloneInstance.transform.localScale} pivot={((cloneSpinner != null && cloneSpinner.pivot != null) ? cloneSpinner.pivot.name : "NULL")}");
    }

    // Disables gameplay-only behaviours on the inspect clone while preserving dice display/edit scripts.
    private static void DisableGameplayComponentsOnInspectClone(GameObject cloneRoot)
    {
        if (cloneRoot == null)
            return;

        MonoBehaviour[] behaviours = cloneRoot.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null)
                continue;

            if (behaviour is DiceSpinnerGeneric ||
                behaviour is GameplayDiceEditInteractable ||
                behaviour is DiceFaceSelectionMap ||
                behaviour is DiceFaceHighlightRenderer ||
                behaviour is TMP_Text)
            {
                continue;
            }

            behaviour.enabled = false;
        }
    }

    // Removes the inspect clone and restores any scene dice hidden during modal editing.
    private void DestroyInspectClone()
    {
        if (_inspectCloneInstance != null)
            Destroy(_inspectCloneInstance);

        _inspectCloneInstance = null;
        RestoreHiddenSceneDice();
    }

    // Hides real scene dice so the clone is the only visible edit target.
    private void HideSceneDiceDuringEdit()
    {
        _hiddenSceneDice.Clear();
        if (diceRig == null || diceRig.slots == null)
            return;

        for (int i = 0; i < diceRig.slots.Length; i++)
        {
            DiceSlotRig.Entry entry = diceRig.slots[i];
            if (entry == null)
                continue;

            GameObject target = entry.diceRoot != null
                ? entry.diceRoot
                : (entry.dice != null ? entry.dice.gameObject : null);

            if (target == null || target == _inspectCloneInstance)
                continue;

            _hiddenSceneDice.Add(new VisibilityState { target = target, wasActive = target.activeSelf });
            target.SetActive(false);
        }
    }

    // Restores scene dice visibility after the inspect clone is removed.
    private void RestoreHiddenSceneDice()
    {
        for (int i = 0; i < _hiddenSceneDice.Count; i++)
        {
            VisibilityState state = _hiddenSceneDice[i];
            if (state.target != null)
                state.target.SetActive(state.wasActive);
        }

        _hiddenSceneDice.Clear();
    }
}

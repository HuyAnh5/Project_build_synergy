using System.Collections.Generic;
using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class GameplayDiceEditController : MonoBehaviour
{
    private const bool DebugLogs = true;

    private struct VisibilityState
    {
        public GameObject target;
        public bool wasActive;
    }

    [Header("Runtime Links")]
    [SerializeField] private RunInventoryManager runInventory;
    [SerializeField] private DiceSlotRig diceRig;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private GameplayDiceEditPanelUI panelUi;
    [SerializeField] private Transform inspectAnchor;

    [Header("Inspect Presentation")]
    [SerializeField] private Vector3 inspectLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 inspectLocalEuler = Vector3.zero;
    [SerializeField] private Vector3 inspectLocalScale = new Vector3(2.2f, 2.2f, 2.2f);

    private readonly List<GameplayDiceEditInteractable> _interactables = new List<GameplayDiceEditInteractable>();
    private readonly List<VisibilityState> _hiddenSceneDice = new List<VisibilityState>();
    private readonly List<int> _selectedLogicalFaceIndices = new List<int>();

    private int _pendingConsumableSlot = -1;
    private ConsumableDataSO _activeConsumable;
    private GameplayDiceEditInteractable _selectedInteractable;
    private GameplayDiceEditInteractable _sourceInteractable;
    private GameplayDiceEditInteractable _activeInteractable;
    private GameplayDiceEditInteractable _focusedInteractable;
    private GameplayDiceEditInteractable _activeDragInteractable;
    private GameObject _inspectCloneInstance;

    private int _copySourceFaceIndex = -1;
    private int _copyTargetFaceIndex = -1;

    public bool IsPanelOpen => _inspectCloneInstance != null && _activeInteractable != null && _activeConsumable != null;
    public event System.Action SelectionStateChanged;

    private void Awake()
    {
        AutoResolveReferences();
        AttachToDiceRig();
        Log($"Awake. diceRig={(diceRig != null ? diceRig.name : "NULL")} runInventory={(runInventory != null ? runInventory.name : "NULL")} interactables={_interactables.Count}");
        if (panelUi != null)
        {
            panelUi.Initialize(this);
            panelUi.SetVisible(false);
        }
    }

    private void Update()
    {
        AutoResolveReferences();
        if (!CanReceiveSceneInput())
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        bool pointerOverUi = UnityEngine.EventSystems.EventSystem.current != null &&
                             UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        if (Input.GetMouseButtonDown(0))
        {
            if (IsPanelOpen)
            {
                bool overPanelUi = panelUi != null && panelUi.IsPointerBlockedByModal(Input.mousePosition);
                GameplayDiceEditInteractable clickedInspectDie = RaycastInteractable(cam);

                if (clickedInspectDie != null && clickedInspectDie == _activeInteractable)
                {
                    _activeDragInteractable = clickedInspectDie;
                    Log($"MouseDown routed to active inspect die '{_activeDragInteractable.name}'.");
                }
                else if (overPanelUi)
                {
                    _activeDragInteractable = null;
                    Log("MouseDown consumed by modal edit UI.");
                }
                else
                {
                    _activeDragInteractable = null;
                    Log("MouseDown ignored because edit mode is modal and pointer was outside inspect die/panel.");
                }
            }
            else
            {
                _activeDragInteractable = RaycastInteractable(cam);
                if (_activeDragInteractable == null && pointerOverUi)
                {
                    Log("MouseDown ignored because pointer is over UI and no dice was hit.");
                    return;
                }

                Log(_activeDragInteractable != null
                    ? $"MouseDown hit dice '{_activeDragInteractable.name}'."
                    : "MouseDown did not hit any GameplayDiceEditInteractable.");
            }

            if (_activeDragInteractable != null)
                _activeDragInteractable.HandleMouseDown();
        }

        if (_activeDragInteractable != null)
            _activeDragInteractable.HandleMouseDrag();

        if (Input.GetMouseButtonUp(0) && _activeDragInteractable != null)
        {
            _activeDragInteractable.HandleMouseUp();
            _activeDragInteractable = null;
        }
    }

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

    public bool CanManipulateInteractable(GameplayDiceEditInteractable interactable)
    {
        return IsPanelOpen && interactable != null && interactable == _activeInteractable;
    }

    public bool CanReceivePointer(GameplayDiceEditInteractable interactable)
    {
        if (interactable == null)
            return false;

        if (!IsPanelOpen)
            return true;

        return CanManipulateInteractable(interactable);
    }

    public bool IsAwaitingDieSelection()
    {
        return _pendingConsumableSlot >= 0 && _activeConsumable != null && !IsPanelOpen;
    }

    public bool TryGetSelectedSceneDie(out DiceSpinnerGeneric die)
    {
        GameplayDiceEditInteractable interactable = ResolveSelectedSceneInteractable();
        die = interactable != null ? interactable.Spinner : null;
        return die != null;
    }

    public void HandleFaceClicked(GameplayDiceEditInteractable interactable, int logicalFaceIndex)
    {
        if (interactable == null)
            return;

        if (!IsPanelOpen)
        {
            SetSelectedInteractable(interactable);
            return;
        }

        if (interactable != _activeInteractable)
            return;

        if (IsCopyPasteFaceMode())
            HandleCopyPasteFaceSelection(logicalFaceIndex);
        else
            HandleStandardFaceSelection(logicalFaceIndex);

        RefreshAllHighlights();
        panelUi?.Refresh();
    }

    public void SetFocusedInteractable(GameplayDiceEditInteractable interactable)
    {
        if (interactable != null)
            _focusedInteractable = interactable;
        panelUi?.Refresh();
    }

    public void SelectInteractable(GameplayDiceEditInteractable interactable)
    {
        if (!IsPanelOpen && interactable != null && interactable == _selectedInteractable)
        {
            ClearSelectedInteractable();
            return;
        }

        SetSelectedInteractable(interactable);
    }

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
            success = true;
            for (int i = 0; i < _selectedLogicalFaceIndices.Count; i++)
            {
                ConsumableUseResult result = ConsumableRuntimeUtility.TryUseInSandbox(_activeConsumable, die, _selectedLogicalFaceIndices[i]);
                if (!result.success)
                {
                    success = false;
                    break;
                }
            }
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

        if (sourceDie != null && die != null)
            sourceDie.CopyRuntimeStateFrom(die, copyRotation: false);

        runInventory.TryConsumeConsumableCharge(_pendingConsumableSlot, 1);
        if (sourceDie != null)
            sourceDie.RefreshDisplayedState();

        CancelAndClose();
    }

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

    public bool CanAutoUprightFocusedDie()
    {
        GameplayDiceEditInteractable interactable = ResolvePrimaryInteractable();
        return interactable != null && interactable.CanAutoUprightInspectDie();
    }

    public void AutoUprightFocusedDie()
    {
        GameplayDiceEditInteractable interactable = ResolvePrimaryInteractable();
        if (interactable == null)
            return;
        interactable.AutoUprightInspectDie();
        _focusedInteractable = interactable;
        panelUi?.Refresh();
    }

    public bool CanRollFocusedDie()
    {
        GameplayDiceEditInteractable interactable = ResolvePrimaryInteractable();
        return interactable != null && interactable.CanRollInspectDie();
    }

    public void RollFocusedDie()
    {
        GameplayDiceEditInteractable interactable = ResolvePrimaryInteractable();
        if (interactable == null)
            return;
        interactable.RollInspectDie();
        _focusedInteractable = interactable;
        panelUi?.Refresh();
    }

    public void NotifyInspectDieStateChanged()
    {
        panelUi?.Refresh();
    }

    public string GetDisplayName()
    {
        return _activeConsumable != null ? _activeConsumable.displayName : "No Zodiac";
    }

    public string GetEffectText()
    {
        return _activeConsumable != null ? _activeConsumable.description : string.Empty;
    }

    public DiceEditSandboxController.SandboxFaceHighlightKind GetHighlightKindForFace(GameplayDiceEditInteractable interactable, int logicalFaceIndex)
    {
        if (!IsPanelOpen)
            return DiceEditSandboxController.SandboxFaceHighlightKind.None;

        if (interactable == null || interactable != _activeInteractable)
            return DiceEditSandboxController.SandboxFaceHighlightKind.None;

        if (IsCopyPasteFaceMode())
        {
            if (logicalFaceIndex == _copySourceFaceIndex)
                return DiceEditSandboxController.SandboxFaceHighlightKind.CopySource;
            if (logicalFaceIndex == _copyTargetFaceIndex)
                return DiceEditSandboxController.SandboxFaceHighlightKind.CopyTarget;
            return DiceEditSandboxController.SandboxFaceHighlightKind.None;
        }

        return _selectedLogicalFaceIndices.Contains(logicalFaceIndex)
            ? DiceEditSandboxController.SandboxFaceHighlightKind.Preview
            : DiceEditSandboxController.SandboxFaceHighlightKind.None;
    }

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
        if (panelUi == null)
            panelUi = FindFirstObjectByType<GameplayDiceEditPanelUI>(FindObjectsInactive.Include);
    }

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

    private void OpenPanelForInteractable(GameplayDiceEditInteractable interactable)
    {
        if (interactable == null || _activeConsumable == null)
            return;

        DestroyInspectClone();
        _sourceInteractable = interactable;
        _selectedInteractable = interactable;
        _selectedLogicalFaceIndices.Clear();
        _copySourceFaceIndex = -1;
        _copyTargetFaceIndex = -1;
        CreateInspectCloneFromSource();
        HideSceneDiceDuringEdit();

        if (panelUi != null)
        {
            panelUi.SetVisible(true);
            panelUi.Refresh();
        }

        RefreshAllHighlights();
        SelectionStateChanged?.Invoke();
    }

    private void CreateInspectCloneFromSource()
    {
        if (_sourceInteractable == null || _sourceInteractable.Spinner == null || inspectAnchor == null)
            return;

        _inspectCloneInstance = Instantiate(_sourceInteractable.Spinner.gameObject, inspectAnchor);
        _inspectCloneInstance.name = $"{_sourceInteractable.Spinner.name}_InspectClone";
        _inspectCloneInstance.transform.localPosition = inspectLocalPosition;
        _inspectCloneInstance.transform.localRotation = Quaternion.identity;
        _inspectCloneInstance.transform.localScale = inspectLocalScale;
        DisableGameplayComponentsOnInspectClone(_inspectCloneInstance);

        DiceSpinnerGeneric cloneSpinner = _inspectCloneInstance.GetComponent<DiceSpinnerGeneric>();
        if (cloneSpinner == null)
            cloneSpinner = _inspectCloneInstance.AddComponent<DiceSpinnerGeneric>();
        cloneSpinner.enableSpaceKey = false;
        cloneSpinner.onRollComplete = null;

        cloneSpinner.CopyRuntimeStateFrom(_sourceInteractable.Spinner, copyRotation: true);

        GameplayDiceEditInteractable interactable = _inspectCloneInstance.GetComponent<GameplayDiceEditInteractable>();
        if (interactable == null)
            interactable = _inspectCloneInstance.AddComponent<GameplayDiceEditInteractable>();

        interactable.Configure(this, cloneSpinner);
        _activeInteractable = interactable;
        _focusedInteractable = interactable;
        Log(
            $"Created inspect clone. source={_sourceInteractable.Spinner.name} clone={cloneSpinner.name} parent={inspectAnchor.name} localPos={_inspectCloneInstance.transform.localPosition} localScale={_inspectCloneInstance.transform.localScale} pivot={((cloneSpinner != null && cloneSpinner.pivot != null) ? cloneSpinner.pivot.name : "NULL")}");
    }

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

    private void DestroyInspectClone()
    {
        if (_inspectCloneInstance != null)
            Destroy(_inspectCloneInstance);

        _inspectCloneInstance = null;
        RestoreHiddenSceneDice();
    }

    private void HandleStandardFaceSelection(int logicalFaceIndex)
    {
        int limit = GetSandboxFaceSelectionLimit();
        if (limit <= 0)
            return;

        if (_selectedLogicalFaceIndices.Contains(logicalFaceIndex))
            _selectedLogicalFaceIndices.Remove(logicalFaceIndex);
        else if (_selectedLogicalFaceIndices.Count < limit)
            _selectedLogicalFaceIndices.Add(logicalFaceIndex);
    }

    private void HandleCopyPasteFaceSelection(int logicalFaceIndex)
    {
        if (_copySourceFaceIndex == logicalFaceIndex)
            _copySourceFaceIndex = -1;
        else if (_copyTargetFaceIndex == logicalFaceIndex)
            _copyTargetFaceIndex = -1;
        else if (_copySourceFaceIndex < 0)
            _copySourceFaceIndex = logicalFaceIndex;
        else if (_copyTargetFaceIndex < 0)
            _copyTargetFaceIndex = logicalFaceIndex;
    }

    private int GetSandboxFaceSelectionLimit()
    {
        if (_activeConsumable == null || _activeConsumable.targetKind != ConsumableTargetKind.DiceFace)
            return 0;

        switch (_activeConsumable.effectId)
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

    private bool IsCopyPasteFaceMode()
    {
        return _activeConsumable != null && _activeConsumable.effectId == ConsumableEffectId.CopyPasteFace;
    }

    private GameplayDiceEditInteractable ResolvePrimaryInteractable()
    {
        if (_focusedInteractable != null && _focusedInteractable == _activeInteractable)
            return _focusedInteractable;
        return _activeInteractable;
    }

    private void RefreshAllHighlights()
    {
        for (int i = 0; i < _interactables.Count; i++)
        {
            if (_interactables[i] != null)
                _interactables[i].RefreshHighlight();
        }

        if (_activeInteractable != null && !_interactables.Contains(_activeInteractable))
            _activeInteractable.RefreshHighlight();
    }

    private bool CanReceiveSceneInput()
    {
        if (turnManager == null)
            return true;

        return turnManager.phase == TurnManager.Phase.Planning || IsPanelOpen;
    }

    private void SetSelectedInteractable(GameplayDiceEditInteractable interactable)
    {
        if (interactable == null)
            return;

        if (IsPanelOpen && interactable == _activeInteractable && _sourceInteractable != null)
            interactable = _sourceInteractable;

        _selectedInteractable = interactable;
        _focusedInteractable = interactable;
        Debug.Log(
            $"[GameplayDiceEdit] Selected die={(interactable.Spinner != null ? interactable.Spinner.name : interactable.name)}",
            this);
        RefreshAllHighlights();
        panelUi?.Refresh();
        SelectionStateChanged?.Invoke();
    }

    private void ClearSelectedInteractable()
    {
        if (_selectedInteractable == null && _focusedInteractable == null)
            return;

        _selectedInteractable = null;
        _focusedInteractable = null;
        RefreshAllHighlights();
        panelUi?.Refresh();
        SelectionStateChanged?.Invoke();
    }

    private GameplayDiceEditInteractable ResolveSelectedSceneInteractable()
    {
        if (_selectedInteractable != null && _interactables.Contains(_selectedInteractable))
            return _selectedInteractable;

        if (_selectedInteractable != null && _selectedInteractable.Spinner != null)
        {
            GameplayDiceEditInteractable resolved = ResolveSceneInteractableForSpinner(_selectedInteractable.Spinner);
            if (resolved != null)
            {
                _selectedInteractable = resolved;
                return resolved;
            }
        }

        return null;
    }

    private GameplayDiceEditInteractable ResolveSceneInteractableForSpinner(DiceSpinnerGeneric spinner)
    {
        if (spinner == null)
            return null;

        for (int i = 0; i < _interactables.Count; i++)
        {
            if (_interactables[i] != null && _interactables[i].Spinner == spinner)
                return _interactables[i];
        }

        return null;
    }

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

    private static GameplayDiceEditInteractable RaycastInteractable(Camera cam)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f))
            return null;

        if (hit.collider == null)
            return null;

        return hit.collider.GetComponentInParent<GameplayDiceEditInteractable>();
    }

    private void Log(string message)
    {
        if (DebugLogs)
            Debug.Log($"[GameplayDiceEdit] {message}", this);
    }
}

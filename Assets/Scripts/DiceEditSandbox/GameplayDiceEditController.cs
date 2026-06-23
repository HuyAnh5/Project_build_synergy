using System.Collections.Generic;
using UnityEngine;

// Coordinates the in-combat Zodiac dice editor while keeping Unity serialized references in one place.
[DisallowMultipleComponent]
public partial class GameplayDiceEditController : MonoBehaviour, ISkillTooltipSource
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
    [SerializeField] private DiceEquipUIManager diceEquipUiManager;
    [SerializeField] private GameplayDiceEditPanelUI panelUi;
    [SerializeField] private ConsumableBarUIManager consumableBarUi;
    [SerializeField] private Transform inspectAnchor;

    [Header("Inspect Presentation")]
    [SerializeField] private Vector3 inspectLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 inspectLocalEuler = Vector3.zero;
    [SerializeField] private Vector3 inspectLocalScale = new Vector3(2.2f, 2.2f, 2.2f);
    [SerializeField] private float inspectExtraDistanceFromCamera = 1.25f;


    private readonly List<GameplayDiceEditInteractable> _interactables = new List<GameplayDiceEditInteractable>();
    private readonly List<VisibilityState> _hiddenSceneDice = new List<VisibilityState>();
    private readonly List<int> _selectedLogicalFaceIndices = new List<int>();
    private readonly Dictionary<int, int> _inspectFaceMarks = new Dictionary<int, int>();
    private int _inspectActiveMarkColorIndex;

    private int _pendingConsumableSlot = -1;
    private ConsumableDataSO _activeConsumable;
    private GameplayDiceEditInteractable _selectedInteractable;
    private GameplayDiceEditInteractable _sourceInteractable;
    private GameplayDiceEditInteractable _activeInteractable;
    private GameplayDiceEditInteractable _focusedInteractable;
    private GameplayDiceEditInteractable _activeDragInteractable;
    private GameObject _inspectCloneInstance;
    private RectTransform _tooltipAnchor;
    private Canvas _tooltipCanvas;
    private DiceFaceEnchantTooltipAsset _hoverTooltipAsset;
    private GameplayDiceEditInteractable _hoverTooltipInteractable;
    private int _hoverTooltipFaceIndex = -1;

    private int _copySourceFaceIndex = -1;
    private int _copyTargetFaceIndex = -1;

    public bool IsPanelOpen => _inspectCloneInstance != null && _activeInteractable != null;
    public bool IsInspectOnlyMode => IsPanelOpen && _activeConsumable == null;
    public event System.Action SelectionStateChanged;

    // Initializes runtime links and binds scene dice so combat UI can open the edit panel safely.
    private void Awake()
    {
        GameplayDiceEditControllerRegistry.Register(this);
        AutoResolveReferences();
        AttachToDiceRig();
        Log($"Awake. diceRig={(diceRig != null ? diceRig.name : "NULL")} runInventory={(runInventory != null ? runInventory.name : "NULL")} interactables={_interactables.Count}");
        if (panelUi != null)
        {
            panelUi.Initialize(this);
            panelUi.SetVisible(false);
        }
    }

    private void OnDestroy()
    {
        GameplayDiceEditControllerRegistry.Unregister(this);

        if (_hoverTooltipAsset != null)
            Destroy(_hoverTooltipAsset);

        if (_tooltipAnchor != null)
            Destroy(_tooltipAnchor.gameObject);

    }

    // Handles a face click from either a scene dice or the temporary inspect dice.
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

        if (IsInspectOnlyMode)
            HandleInspectOnlyFaceSelection(logicalFaceIndex);
        else if (IsCopyPasteFaceMode())
            HandleCopyPasteFaceSelection(logicalFaceIndex);
        else
            HandleStandardFaceSelection(logicalFaceIndex);

        RefreshInspectDiePreview();
        RefreshAllHighlights();
        panelUi?.Refresh();
    }

    // Tracks which interactable should receive dice utility actions from the panel.
    public void SetFocusedInteractable(GameplayDiceEditInteractable interactable)
    {
        if (interactable != null)
            _focusedInteractable = interactable;
        panelUi?.Refresh();
    }

    // Selects a scene dice, or toggles it off when clicked again outside edit mode.
    public void SelectInteractable(GameplayDiceEditInteractable interactable)
    {
        if (!IsPanelOpen && interactable != null && interactable == _selectedInteractable)
        {
            ClearSelectedInteractable();
            return;
        }

        SetSelectedInteractable(interactable);
    }

    // Reports the per-face highlight state used by dice face renderers.
    public DiceEditSandboxController.SandboxFaceHighlightKind GetHighlightKindForFace(
        GameplayDiceEditInteractable interactable,
        int logicalFaceIndex)
    {
        if (!IsPanelOpen)
            return DiceEditSandboxController.SandboxFaceHighlightKind.None;

        if (interactable == null || interactable != _activeInteractable)
            return DiceEditSandboxController.SandboxFaceHighlightKind.None;

        if (IsInspectOnlyMode)
            return GetInspectOnlyHighlightKind(logicalFaceIndex);

        if (IsCopyPasteFaceMode())
        {
            if (logicalFaceIndex == _copySourceFaceIndex)
                return DiceEditSandboxController.SandboxFaceHighlightKind.CopySource;
            if (logicalFaceIndex == _copyTargetFaceIndex)
                return DiceEditSandboxController.SandboxFaceHighlightKind.CopyTarget;
            return DiceEditSandboxController.SandboxFaceHighlightKind.None;
        }

        if (_activeConsumable != null &&
            _activeConsumable.effectId == ConsumableEffectId.ApplyFaceEnchant &&
            _activeConsumable.faceEnchant == DiceFaceEnchantKind.Gum &&
            _selectedLogicalFaceIndices.Count > 0)
        {
            for (int i = 0; i < _selectedLogicalFaceIndices.Count; i++)
            {
                int selectedFaceIndex = _selectedLogicalFaceIndices[i];
                if (logicalFaceIndex == selectedFaceIndex)
                    return DiceEditSandboxController.SandboxFaceHighlightKind.Preview;

                DiceSpinnerGeneric spinner = interactable.Spinner;
                if (spinner != null && spinner.GetOppositeFaceIndex(selectedFaceIndex) == logicalFaceIndex)
                    return DiceEditSandboxController.SandboxFaceHighlightKind.GumLinked;
            }
        }

        return _selectedLogicalFaceIndices.Contains(logicalFaceIndex)
            ? DiceEditSandboxController.SandboxFaceHighlightKind.Preview
            : DiceEditSandboxController.SandboxFaceHighlightKind.None;
    }

    // Checks whether the focused inspect dice can be uprighted.
    public bool CanAutoUprightFocusedDie()
    {
        GameplayDiceEditInteractable interactable = ResolvePrimaryInteractable();
        return interactable != null && interactable.CanAutoUprightInspectDie();
    }

    // Uprights the focused inspect dice for easier face selection.
    public void AutoUprightFocusedDie()
    {
        GameplayDiceEditInteractable interactable = ResolvePrimaryInteractable();
        if (interactable == null)
            return;
        interactable.AutoUprightInspectDie();
        _focusedInteractable = interactable;
        panelUi?.Refresh();
    }

    // Checks whether the focused inspect dice can be re-rolled in the preview panel.
    public bool CanRollFocusedDie()
    {
        GameplayDiceEditInteractable interactable = ResolvePrimaryInteractable();
        return interactable != null && interactable.CanRollInspectDie();
    }

    // Re-rolls the focused inspect dice without committing changes to the scene dice.
    public void RollFocusedDie()
    {
        GameplayDiceEditInteractable interactable = ResolvePrimaryInteractable();
        if (interactable == null)
            return;
        interactable.RollInspectDie();
        _focusedInteractable = interactable;
        panelUi?.Refresh();
    }

    public bool CanClearInspectHighlights()
    {
        return IsInspectOnlyMode && _inspectFaceMarks.Count > 0;
    }

    public void ClearInspectHighlights()
    {
        if (!IsInspectOnlyMode || _inspectFaceMarks.Count <= 0)
            return;

        _inspectFaceMarks.Clear();
        RefreshAllHighlights();
        panelUi?.Refresh();
    }

    public int GetInspectActiveMarkColorIndex()
    {
        return Mathf.Clamp(_inspectActiveMarkColorIndex, 0, 3);
    }

    public void SetInspectActiveMarkColorIndex(int colorIndex)
    {
        if (!IsInspectOnlyMode)
            return;

        _inspectActiveMarkColorIndex = Mathf.Clamp(colorIndex, 0, 3);
        panelUi?.Refresh();
    }

    // Refreshes the panel when the inspect dice reports a local state change.
    public void NotifyInspectDieStateChanged()
    {
        panelUi?.Refresh();
    }

    // Toggles a standard face selection while honoring the consumable's selection limit.
    private void HandleStandardFaceSelection(int logicalFaceIndex)
    {
        int limit = GetSandboxFaceSelectionLimit();
        if (limit <= 0)
            return;

        if (_selectedLogicalFaceIndices.Contains(logicalFaceIndex))
        {
            _selectedLogicalFaceIndices.Remove(logicalFaceIndex);
            ForceClearFaceEnchantTooltip();
        }
        else if (_selectedLogicalFaceIndices.Count < limit)
            _selectedLogicalFaceIndices.Add(logicalFaceIndex);
    }

    // Captures source and target face choices for Copy/Paste Face.
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

    private void HandleInspectOnlyFaceSelection(int logicalFaceIndex)
    {
        if (logicalFaceIndex < 0)
            return;

        int activeColor = GetInspectActiveMarkColorIndex();
        if (_inspectFaceMarks.TryGetValue(logicalFaceIndex, out int colorIndex) && colorIndex == activeColor)
            _inspectFaceMarks.Remove(logicalFaceIndex);
        else
            _inspectFaceMarks[logicalFaceIndex] = activeColor;
    }

    private DiceEditSandboxController.SandboxFaceHighlightKind GetInspectOnlyHighlightKind(int logicalFaceIndex)
    {
        if (!_inspectFaceMarks.TryGetValue(logicalFaceIndex, out int colorIndex))
            return DiceEditSandboxController.SandboxFaceHighlightKind.None;

        switch (Mathf.Clamp(colorIndex, 0, 3))
        {
            case 0: return DiceEditSandboxController.SandboxFaceHighlightKind.MarkA;
            case 1: return DiceEditSandboxController.SandboxFaceHighlightKind.MarkB;
            case 2: return DiceEditSandboxController.SandboxFaceHighlightKind.MarkC;
            default: return DiceEditSandboxController.SandboxFaceHighlightKind.MarkD;
        }
    }

    // Applies temporary value previews to the inspect dice before the consumable is confirmed.
    private void RefreshInspectDiePreview()
    {
        DiceSpinnerGeneric inspectDie = _activeInteractable != null ? _activeInteractable.Spinner : null;
        if (inspectDie == null)
            return;

        inspectDie.ClearAllFacePreviews();

        if (_activeConsumable == null)
        {
            inspectDie.RefreshDisplayedState();
            return;
        }

        if (IsCopyPasteFaceMode())
        {
            PreviewCopyPasteFaces(inspectDie);
            return;
        }

        if (_activeConsumable.effectId == ConsumableEffectId.AdjustBaseValue)
            PreviewAdjustedBaseValues(inspectDie);
        else if (_activeConsumable.effectId == ConsumableEffectId.ApplyFaceEnchant)
            PreviewFaceEnchant(inspectDie);

        inspectDie.RefreshDisplayedState();
    }

    // Shows the source value on the target face for Copy/Paste Face preview.
    private void PreviewCopyPasteFaces(DiceSpinnerGeneric inspectDie)
    {
        if (_copySourceFaceIndex >= 0)
        {
            DiceFace sourceFace = inspectDie.GetFace(_copySourceFaceIndex);
            inspectDie.SetFacePreviewValue(_copySourceFaceIndex, sourceFace.value, blink: true);
            if (DiceFaceEnchantUtility.HasEnchant(sourceFace.enchant))
                inspectDie.SetFacePreviewEnchant(_copySourceFaceIndex, sourceFace.enchant, blink: true);
        }

        if (_copySourceFaceIndex >= 0 && _copyTargetFaceIndex >= 0)
        {
            DiceFace sourceFace = inspectDie.GetFace(_copySourceFaceIndex);
            inspectDie.SetFacePreviewValue(_copyTargetFaceIndex, sourceFace.value, blink: true);
            inspectDie.SetFacePreviewEnchant(_copyTargetFaceIndex, sourceFace.enchant, blink: true);
        }

        inspectDie.RefreshDisplayedState();
    }

    // Shows the post-adjustment base value on each selected face.
    private void PreviewAdjustedBaseValues(DiceSpinnerGeneric inspectDie)
    {
        for (int i = 0; i < _selectedLogicalFaceIndices.Count; i++)
        {
            int faceIndex = _selectedLogicalFaceIndices[i];
            DiceFace face = inspectDie.GetFace(faceIndex);
            int previewValue = DiceSpinnerGeneric.ClampFaceValue(face.value + _activeConsumable.valueA);
            inspectDie.SetFacePreviewValue(faceIndex, previewValue, blink: true);
        }
    }

    // Shows the post-enchant icon on each selected face before committing the consumable.
    private void PreviewFaceEnchant(DiceSpinnerGeneric inspectDie)
    {
        if (_activeConsumable == null)
            return;

        for (int i = 0; i < _selectedLogicalFaceIndices.Count; i++)
        {
            int faceIndex = _selectedLogicalFaceIndices[i];
            inspectDie.SetFacePreviewEnchant(faceIndex, _activeConsumable.faceEnchant, blink: true);
        }
    }

    // Refreshes scene dice and inspect clone highlights after selection changes.
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

    // Stores the selected scene dice and notifies UI listeners.
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

    // Clears the selected scene dice and related focused state.
    private void ClearSelectedInteractable()
    {
        if (_selectedInteractable == null && _focusedInteractable == null)
            return;

        _selectedInteractable = null;
        _focusedInteractable = null;
        ForceClearFaceEnchantTooltip();
        RefreshAllHighlights();
        panelUi?.Refresh();
        SelectionStateChanged?.Invoke();
    }

    // Resolves the stored selection back to a live scene interactable.
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

    // Finds the scene interactable that wraps a given dice spinner.
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

    // Picks the inspect interactable that should receive utility operations.
    private GameplayDiceEditInteractable ResolvePrimaryInteractable()
    {
        if (_focusedInteractable != null && _focusedInteractable == _activeInteractable)
            return _focusedInteractable;
        return _activeInteractable;
    }

    // Centralized debug gate for this controller's edit-flow tracing.
    private void Log(string message)
    {
        if (DebugLogs)
            Debug.Log($"[GameplayDiceEdit] {message}", this);
    }
}

internal static class GameplayDiceEditControllerRegistry
{
    private static GameplayDiceEditController _instance;

    public static void Register(GameplayDiceEditController controller)
    {
        if (controller == null)
            return;

        _instance = controller;
    }

    public static void Unregister(GameplayDiceEditController controller)
    {
        if (_instance == controller)
            _instance = null;
    }

    public static GameplayDiceEditController Get()
    {
        if (_instance != null)
            return _instance;

#if UNITY_2023_1_OR_NEWER
        _instance = UnityEngine.Object.FindFirstObjectByType<GameplayDiceEditController>(FindObjectsInactive.Include);
#else
        _instance = UnityEngine.Object.FindObjectOfType<GameplayDiceEditController>(true);
#endif
        return _instance;
    }
}



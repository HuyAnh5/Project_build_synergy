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

    public bool IsPanelOpen => _inspectCloneInstance != null && _activeInteractable != null && _activeConsumable != null;
    public event System.Action SelectionStateChanged;

    // Initializes runtime links and binds scene dice so combat UI can open the edit panel safely.
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

    private void OnDestroy()
    {
        if (_hoverTooltipAsset != null)
            Destroy(_hoverTooltipAsset);

        if (_tooltipAnchor != null)
            Destroy(_tooltipAnchor.gameObject);
    }

    private void RefreshFaceEnchantTooltip(Camera cam, bool pointerOverUi)
    {
        if (!IsPanelOpen || _activeInteractable == null || cam == null || _activeDragInteractable != null)
        {
            ClearFaceEnchantTooltip();
            return;
        }

        if (pointerOverUi && !IsPointerOverActiveInspectDice(cam))
        {
            ClearFaceEnchantTooltip();
            return;
        }

        if (!_activeInteractable.TryResolveHoveredLogicalFace(cam, Input.mousePosition, out int logicalFaceIndex))
        {
            ClearFaceEnchantTooltip();
            return;
        }

        DiceSpinnerGeneric spinner = _activeInteractable.Spinner;
        DiceFace face = spinner != null ? spinner.GetFace(logicalFaceIndex) : default;
        DiceFaceEnchantKind displayedEnchant = spinner != null
            ? spinner.GetDisplayedFaceEnchant(logicalFaceIndex)
            : DiceFaceEnchantKind.None;
        if (spinner == null ||
            face.broken ||
            !DiceFaceEnchantUtility.HasEnchant(displayedEnchant) ||
            face.faceIconSpriteRenderer == null ||
            !face.faceIconSpriteRenderer.enabled)
        {
            ClearFaceEnchantTooltip();
            return;
        }

        if (!TryUpdateTooltipAnchor(face.faceIconSpriteRenderer, cam))
        {
            ClearFaceEnchantTooltip();
            return;
        }

        EnsureHoverTooltipAsset();
        _hoverTooltipAsset.Configure(displayedEnchant, face.value, spinner.name);

        _hoverTooltipInteractable = _activeInteractable;
        _hoverTooltipFaceIndex = logicalFaceIndex;
        SkillTooltipUI.Show(this);
    }

    private bool IsPointerOverActiveInspectDice(Camera cam)
    {
        return _activeInteractable != null &&
               cam != null &&
               _activeInteractable.TryResolveHoveredLogicalFace(cam, Input.mousePosition, out _);
    }

    private void ClearFaceEnchantTooltip()
    {
        _hoverTooltipInteractable = null;
        _hoverTooltipFaceIndex = -1;
        SkillTooltipUI.HideCurrentUnlessPointerOverTooltip();
    }

    private void ForceClearFaceEnchantTooltip()
    {
        _hoverTooltipInteractable = null;
        _hoverTooltipFaceIndex = -1;
        SkillTooltipUI.HideCurrent();
    }

    public bool TryGetSkillTooltip(out Canvas canvas, out RectTransform target, out ScriptableObject asset, out SkillRuntime runtime)
    {
        canvas = ResolveTooltipCanvas();
        target = _tooltipAnchor;
        asset = _hoverTooltipAsset;
        runtime = null;
        return canvas != null && target != null && asset != null && _hoverTooltipFaceIndex >= 0;
    }

    private bool TryUpdateTooltipAnchor(SpriteRenderer iconRenderer, Camera cam)
    {
        Canvas canvas = ResolveTooltipCanvas();
        if (canvas == null || iconRenderer == null)
            return false;

        EnsureTooltipAnchor(canvas);
        if (_tooltipAnchor == null)
            return false;

        Vector3 screenPoint = cam.WorldToScreenPoint(iconRenderer.bounds.center);
        if (screenPoint.z <= 0f)
            return false;

        RectTransform canvasRect = canvas.transform as RectTransform;
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (canvasRect == null ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, eventCamera, out Vector2 localPoint))
        {
            return false;
        }

        _tooltipAnchor.SetParent(canvasRect, false);
        _tooltipAnchor.anchorMin = new Vector2(0.5f, 0.5f);
        _tooltipAnchor.anchorMax = new Vector2(0.5f, 0.5f);
        _tooltipAnchor.pivot = new Vector2(0.5f, 0.5f);
        _tooltipAnchor.anchoredPosition = localPoint;
        _tooltipAnchor.sizeDelta = BuildTooltipHoverSize(iconRenderer, cam, canvasRect, eventCamera);
        return true;
    }

    private Vector2 BuildTooltipHoverSize(
        SpriteRenderer iconRenderer,
        Camera cam,
        RectTransform canvasRect,
        Camera eventCamera)
    {
        Bounds bounds = iconRenderer.bounds;
        Vector3 min = cam.WorldToScreenPoint(bounds.min);
        Vector3 max = cam.WorldToScreenPoint(bounds.max);

        if (min.z <= 0f || max.z <= 0f)
            return new Vector2(180f, 180f);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, min, eventCamera, out Vector2 localMin) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, max, eventCamera, out Vector2 localMax))
        {
            return new Vector2(180f, 180f);
        }

        float width = Mathf.Abs(localMax.x - localMin.x) + 160f;
        float height = Mathf.Abs(localMax.y - localMin.y) + 160f;
        return new Vector2(Mathf.Max(180f, width), Mathf.Max(180f, height));
    }

    private Canvas ResolveTooltipCanvas()
    {
        if (_tooltipCanvas != null)
            return _tooltipCanvas;

        if (panelUi != null)
            _tooltipCanvas = panelUi.GetComponentInParent<Canvas>();

        if (_tooltipCanvas == null && consumableBarUi != null)
            _tooltipCanvas = consumableBarUi.GetComponentInParent<Canvas>();

        if (_tooltipCanvas == null)
            _tooltipCanvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);

        return _tooltipCanvas;
    }

    private void EnsureTooltipAnchor(Canvas canvas)
    {
        if (_tooltipAnchor != null)
            return;

        GameObject anchorGo = new GameObject("DiceFaceEnchantTooltipAnchor", typeof(RectTransform));
        anchorGo.hideFlags = HideFlags.HideAndDontSave;
        _tooltipAnchor = anchorGo.GetComponent<RectTransform>();
        _tooltipAnchor.SetParent(canvas.transform, false);
        _tooltipAnchor.sizeDelta = Vector2.zero;
    }

    private void EnsureHoverTooltipAsset()
    {
        if (_hoverTooltipAsset != null)
            return;

        _hoverTooltipAsset = ScriptableObject.CreateInstance<DiceFaceEnchantTooltipAsset>();
        _hoverTooltipAsset.hideFlags = HideFlags.HideAndDontSave;
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

        if (IsCopyPasteFaceMode())
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



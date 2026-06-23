using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Coordinates the dice edit sandbox scene: input, dice interactable registration, and shared state.
// UI, selection/highlight, and consumable application are split into partial files.
public partial class DiceEditSandboxController : MonoBehaviour
{
    public enum SandboxFaceHighlightKind
    {
        None,
        Preview,
        Committed,
        CopySource,
        CopyTarget,
        GumLinked,
        MarkA,
        MarkB,
        MarkC,
        MarkD
    }

    private static readonly bool DebugLogs = false;
    private const int ConsumableSlotCount = RunInventoryManager.DEFAULT_CONSUMABLE_CAPACITY;
    private readonly List<DiceEditInteractable> _interactables = new List<DiceEditInteractable>();

    private Canvas _canvas;
    private TMP_Text _selectionLabel;
    private TMP_Text _commitLabel;
    private TMP_Text _consumableLabel;
    private TMP_Text _resultLabel;
    private Button _useButton;
    private Button _clearButton;
    private Button _flipButton;
    private readonly Button[] _consumableButtons = new Button[ConsumableSlotCount];
    private readonly TMP_Text[] _consumableButtonLabels = new TMP_Text[ConsumableSlotCount];

    private DiceSpinnerGeneric _selectedDie;
    private DiceSpinnerGeneric _committedDie;
    private int _selectedLogicalFaceIndex = -1;
    private int _committedLogicalFaceIndex = -1;
    private readonly List<int> _selectedLogicalFaceIndices = new List<int>();
    private DiceSpinnerGeneric _copySourceDie;
    private int _copySourceLogicalFaceIndex = -1;
    private DiceSpinnerGeneric _copyTargetDie;
    private int _copyTargetLogicalFaceIndex = -1;
    private DiceEditInteractable _activeDragInteractable;
    private DiceEditInteractable _focusedInteractable;
    private RunInventoryManager _inventory;
    private int _selectedConsumableSlot = -1;
    private ConsumableDataSO _sandboxSelectedConsumable;
    private string _lastUseMessage = "Result: no consumable used yet.";
    private DiceEditSandboxZodiacPanelUI _externalZodiacPanel;
    private bool _pendingHighlightPurge;

    public event System.Action UiStateChanged;

    /// <summary>Bootstraps scene dependencies, UI, and dice interactables for the sandbox scene.</summary>
    public void InitializeForScene(Scene scene)
    {
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        EnsureInventory();
        EnsureEventSystem();
        if (!TryInitializeExternalUi())
            BuildRuntimeUi();
        AttachToDiceInScene();
        Log($"Initialized for scene '{scene.name}'. Found {_interactables.Count} dice interactables.");
        RefreshUi();
    }

    private void OnDestroy()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void Update()
    {
        if (_pendingHighlightPurge)
        {
            _pendingHighlightPurge = false;
            ForceClearAllHighlights();
            DestroyAllHighlightObjectsGlobally();
        }

        Camera cam = Camera.main;
        if (cam == null)
            return;

        bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (Input.GetMouseButtonDown(0))
        {
            if (pointerOverUi)
            {
                Log("MouseDown ignored because pointer is over UI.");
                return;
            }

            _activeDragInteractable = RaycastInteractable(cam);
            Log(_activeDragInteractable != null
                ? $"MouseDown hit interactable '{_activeDragInteractable.name}'."
                : "MouseDown did not hit any dice interactable.");
            if (_activeDragInteractable != null)
            {
                _focusedInteractable = _activeDragInteractable;
                _activeDragInteractable.HandleMouseDown();
            }
        }

        if (_activeDragInteractable != null)
            _activeDragInteractable.HandleMouseDrag();

        if (Input.GetMouseButtonUp(0) && _activeDragInteractable != null)
        {
            _activeDragInteractable.HandleMouseUp();
            _activeDragInteractable = null;
        }
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (scene.name == "SampleScene")
            Destroy(gameObject);
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    /// <summary>Finds all dice in the scene and adds a sandbox interactable facade when needed.</summary>
    private void AttachToDiceInScene()
    {
        _interactables.Clear();

        DiceSpinnerGeneric[] dice = FindObjectsByType<DiceSpinnerGeneric>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < dice.Length; i++)
        {
            DiceSpinnerGeneric die = dice[i];
            if (die == null)
                continue;

            DiceEditInteractable interactable = die.GetComponent<DiceEditInteractable>();
            if (interactable == null)
                interactable = die.gameObject.AddComponent<DiceEditInteractable>();

            interactable.Configure(this, die);
            _interactables.Add(interactable);
            Log($"Attached sandbox interactable to '{die.name}'.");
        }
    }

    public void RegisterExternalZodiacPanel(DiceEditSandboxZodiacPanelUI panel)
    {
        if (panel == null)
            return;

        _externalZodiacPanel = panel;
        UiStateChanged?.Invoke();
    }

    public void SetFocusedInteractable(DiceEditInteractable interactable)
    {
        if (interactable != null)
            _focusedInteractable = interactable;

        RefreshUi();
    }

    public bool CanAutoUprightFocusedDie()
    {
        DiceEditInteractable interactable = ResolvePrimaryInteractable();
        return interactable != null && interactable.CanAutoUprightInspectDie();
    }

    public void AutoUprightFocusedDie()
    {
        DiceEditInteractable interactable = ResolvePrimaryInteractable();
        if (interactable == null)
            return;

        interactable.AutoUprightInspectDie();
        _focusedInteractable = interactable;
        RefreshUi();
    }

    public bool CanRollFocusedDie()
    {
        DiceEditInteractable interactable = ResolvePrimaryInteractable();
        return interactable != null && interactable.CanRollInspectDie();
    }

    public void RollFocusedDie()
    {
        DiceEditInteractable interactable = ResolvePrimaryInteractable();
        if (interactable == null)
            return;

        interactable.RollInspectDie();
        _focusedInteractable = interactable;
        RefreshUi();
    }

    public void NotifyInspectDieStateChanged()
    {
        RefreshUi();
    }

    private static DiceEditInteractable RaycastInteractable(Camera cam)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f))
            return null;

        if (hit.collider == null)
            return null;

        if (DebugLogs)
            Debug.Log($"[DiceEditSandbox] Raycast hit collider '{hit.collider.name}' on object '{hit.collider.gameObject.name}'.");

        return hit.collider.GetComponentInParent<DiceEditInteractable>();
    }

    private static void Log(string message)
    {
        if (DebugLogs)
            Debug.Log($"[DiceEditSandbox] {message}");
    }

    private bool TryInitializeExternalUi()
    {
        _externalZodiacPanel = FindFirstObjectByType<DiceEditSandboxZodiacPanelUI>(FindObjectsInactive.Include);
        if (_externalZodiacPanel == null)
            return false;

        _externalZodiacPanel.Initialize(this);
        return true;
    }

    private void EnsureInventory()
    {
        _inventory = FindFirstObjectByType<RunInventoryManager>(FindObjectsInactive.Include);
        if (_inventory != null)
            return;

        GameObject go = new GameObject("RunInventoryRuntime");
        go.transform.SetParent(transform, false);
        _inventory = go.AddComponent<RunInventoryManager>();
    }
}

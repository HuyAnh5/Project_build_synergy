using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;

public class DiceEditSandboxController : MonoBehaviour
{
    public enum SandboxFaceHighlightKind
    {
        None,
        Preview,
        Committed,
        CopySource,
        CopyTarget
    }

    private const bool DebugLogs = false;
    private const int ConsumableSlotCount = RunInventoryManager.RELIC_SLOT_COUNT;
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

    private void BuildRuntimeUi()
    {
        GameObject canvasGo = new GameObject("DiceEditSandboxCanvas");
        canvasGo.transform.SetParent(transform, false);

        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        RectTransform panel = CreatePanel("Panel", canvasGo.transform as RectTransform, new Vector2(16f, 16f), new Vector2(420f, 360f), TextAnchor.LowerLeft);
        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.1f, 0.14f, 0.86f);

        CreateText("Title", panel, "Dice Edit Sandbox", 28, new Vector2(18f, -18f), new Vector2(320f, 32f), FontStyles.Bold, TextAlignmentOptions.Left);
        CreateText("Hint", panel, "Drag ngang de xoay. Drag doc de flip/chinh chieu. Vuot nhanh se co quan tinh.", 18, new Vector2(18f, -54f), new Vector2(324f, 44f), FontStyles.Normal, TextAlignmentOptions.Left);
        _selectionLabel = CreateText("Selection", panel, "Selected: none", 20, new Vector2(18f, -108f), new Vector2(320f, 28f), FontStyles.Bold, TextAlignmentOptions.Left);
        _commitLabel = CreateText("Commit", panel, "Committed: none", 18, new Vector2(18f, -138f), new Vector2(320f, 28f), FontStyles.Normal, TextAlignmentOptions.Left);
        _consumableLabel = CreateText("Consumable", panel, "Consumable: none", 18, new Vector2(18f, -168f), new Vector2(384f, 44f), FontStyles.Normal, TextAlignmentOptions.Left);
        _resultLabel = CreateText("Result", panel, "Result: no consumable used yet.", 16, new Vector2(18f, -214f), new Vector2(384f, 40f), FontStyles.Normal, TextAlignmentOptions.Left);

        float slotY = -262f;
        for (int i = 0; i < ConsumableSlotCount; i++)
        {
            int capturedIndex = i;
            Button slotButton = CreateButton($"ConsumableSlot{i + 1}", panel, $"Slot {i + 1}", new Vector2(18f + (132f * i), slotY), new Vector2(120f, 40f), out TMP_Text slotLabel);
            slotButton.onClick.AddListener(() => SelectConsumableSlot(capturedIndex));
            _consumableButtons[i] = slotButton;
            _consumableButtonLabels[i] = slotLabel;
        }

        _useButton = CreateButton("UseButton", panel, "Use", new Vector2(18f, -314f), new Vector2(120f, 32f), out _);
        _useButton.onClick.AddListener(UseSelectedConsumable);

        _clearButton = CreateButton("ClearButton", panel, "Clear", new Vector2(152f, -314f), new Vector2(120f, 32f), out _);
        _clearButton.onClick.AddListener(ClearSelection);

        _flipButton = CreateButton("FlipButton", panel, "Flip", new Vector2(286f, -314f), new Vector2(56f, 32f), out _);
        _flipButton.onClick.AddListener(FlipFocusedDie);
    }

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

    public void SetPreviewSelection(DiceSpinnerGeneric die, int logicalFaceIndex)
    {
        if (IsCopyPasteFaceMode())
        {
            SetCopyPasteSelection(die, logicalFaceIndex);
            return;
        }

        if (IsSandboxDropdownMode())
        {
            ToggleSandboxFaceSelection(die, logicalFaceIndex);
            return;
        }

        if (_selectedDie == die && _selectedLogicalFaceIndex == logicalFaceIndex)
        {
            _selectedDie = null;
            _selectedLogicalFaceIndex = -1;
            Debug.Log($"[DiceEditSelect] Deselected die='{die?.name}' faceValue={GetDisplayFaceValue(die, logicalFaceIndex)}");
            RefreshAllHighlights();
            RefreshUi();
            return;
        }

        _selectedDie = die;
        _selectedLogicalFaceIndex = logicalFaceIndex;
        Debug.Log($"[DiceEditSelect] Selected die='{die?.name}' faceValue={GetDisplayFaceValue(die, logicalFaceIndex)} value={GetFaceValue(die, logicalFaceIndex)}");
        RefreshAllHighlights();
        RefreshUi();
    }

    public void SetSandboxSelectedConsumable(ConsumableDataSO data)
    {
        _sandboxSelectedConsumable = data;
        _selectedConsumableSlot = -1;
        ClearSandboxFaceSelection();
        RefreshAllHighlights();
        RefreshUi();
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

    public bool IsCommittedSelection(DiceSpinnerGeneric die, int logicalFaceIndex)
    {
        return die == _committedDie &&
               logicalFaceIndex == _committedLogicalFaceIndex &&
               logicalFaceIndex >= 0;
    }

    public bool IsPreviewSelection(DiceSpinnerGeneric die, int logicalFaceIndex)
    {
        if (IsCopyPasteFaceMode())
            return false;

        if (IsSandboxDropdownMode())
            return die == _selectedDie &&
                   logicalFaceIndex >= 0 &&
                   _selectedLogicalFaceIndices.Contains(logicalFaceIndex);

        return die == _selectedDie &&
               logicalFaceIndex == _selectedLogicalFaceIndex &&
               logicalFaceIndex >= 0;
    }

    public void CommitCurrentSelection()
    {
        if (_selectedDie == null || _selectedLogicalFaceIndex < 0)
            return;

        _committedDie = _selectedDie;
        _committedLogicalFaceIndex = _selectedLogicalFaceIndex;
        RefreshAllHighlights();
        RefreshUi();
    }

    public void ClearSelection()
    {
        _selectedDie = null;
        _committedDie = null;
        _selectedLogicalFaceIndex = -1;
        _committedLogicalFaceIndex = -1;
        _selectedLogicalFaceIndices.Clear();
        ClearCopyPasteSelection();
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

    public bool TryGetResolvedConsumableTarget(out DiceSpinnerGeneric die, out int faceIndex)
    {
        ResolveConsumableTarget(out die, out faceIndex);
        return die != null;
    }

    public string BuildResolvedTargetLabel()
    {
        if (IsCopyPasteFaceMode())
        {
            if (_copySourceDie == null || _copySourceLogicalFaceIndex < 0)
                return "Target: choose source face";

            if (_copyTargetDie == null || _copyTargetLogicalFaceIndex < 0)
                return $"Target: source {_copySourceDie.name} face {GetDisplayFaceValue(_copySourceDie, _copySourceLogicalFaceIndex)} | choose target face";

            return $"Target: {_copySourceDie.name} face {GetDisplayFaceValue(_copySourceDie, _copySourceLogicalFaceIndex)} -> {_copyTargetDie.name} face {GetDisplayFaceValue(_copyTargetDie, _copyTargetLogicalFaceIndex)}";
        }

        DiceSpinnerGeneric die = ResolveSelectedDie();
        if (die == null)
            return "Target: no dice selected";

        if (IsSandboxDropdownMode())
        {
            if (_selectedLogicalFaceIndices.Count <= 0)
                return $"Target: {die.name}";

            return $"Target: {die.name} | Faces: {BuildDisplayFaceList(die, _selectedLogicalFaceIndices)}";
        }

        ResolveConsumableTarget(out _, out int faceIndex);
        if (faceIndex >= 0)
            return $"Target: {die.name} face {GetDisplayFaceValue(die, faceIndex)}";

        return $"Target: {die.name}";
    }

    public int GetSandboxFaceSelectionLimit()
    {
        return GetSandboxFaceSelectionLimit(GetSelectedConsumableData());
    }

    public int GetSelectedFaceCount()
    {
        if (IsCopyPasteFaceMode())
            return (_copySourceLogicalFaceIndex >= 0 ? 1 : 0) + (_copyTargetLogicalFaceIndex >= 0 ? 1 : 0);

        if (IsSandboxDropdownMode())
            return _selectedLogicalFaceIndices.Count;

        return _selectedLogicalFaceIndex >= 0 ? 1 : 0;
    }

    public void CopySelectedFacesTo(List<int> buffer)
    {
        if (buffer == null)
            return;

        buffer.Clear();
        if (IsCopyPasteFaceMode())
        {
            return;
        }

        if (UsesExternalZodiacSandbox())
        {
            buffer.AddRange(_selectedLogicalFaceIndices);
            return;
        }

        if (_selectedLogicalFaceIndex >= 0)
            buffer.Add(_selectedLogicalFaceIndex);
    }

    private void RefreshAllHighlights()
    {
        for (int i = 0; i < _interactables.Count; i++)
        {
            if (_interactables[i] != null)
                _interactables[i].RefreshHighlight();
        }
    }

    private void ForceClearAllHighlights()
    {
        for (int i = 0; i < _interactables.Count; i++)
        {
            if (_interactables[i] != null)
            {
                _interactables[i].ClearHighlight();
                DestroyHighlightChildren(_interactables[i].transform);
            }
        }
    }

    private static void DestroyAllHighlightObjectsGlobally()
    {
        Transform[] allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = allTransforms.Length - 1; i >= 0; i--)
        {
            Transform tr = allTransforms[i];
            if (tr == null || !tr.name.StartsWith("DiceFaceHighlight", StringComparison.Ordinal))
                continue;

            MeshFilter filter = tr.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                Mesh mesh = filter.sharedMesh;
                filter.sharedMesh = null;
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(mesh);
                else
                    UnityEngine.Object.DestroyImmediate(mesh);
            }

            MeshRenderer renderer = tr.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.enabled = false;

            tr.gameObject.SetActive(false);

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(tr.gameObject);
            else
                UnityEngine.Object.DestroyImmediate(tr.gameObject);
        }
    }

    private static void DestroyHighlightChildren(Transform root)
    {
        if (root == null)
            return;

        Transform[] allChildren = root.GetComponentsInChildren<Transform>(true);
        for (int i = allChildren.Length - 1; i >= 0; i--)
        {
            Transform child = allChildren[i];
            if (child == null || child == root)
                continue;

            if (!child.name.StartsWith("DiceFaceHighlight", StringComparison.Ordinal))
                continue;

            MeshFilter filter = child.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                Mesh mesh = filter.sharedMesh;
                filter.sharedMesh = null;
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(mesh);
                else
                    UnityEngine.Object.DestroyImmediate(mesh);
            }

            MeshRenderer renderer = child.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.enabled = false;

            child.gameObject.SetActive(false);

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(child.gameObject);
            else
                UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }

    private void RefreshUi()
    {
        string selectionText = "Selected: none";
        if (IsSandboxDropdownMode())
        {
            if (_selectedDie != null && _selectedLogicalFaceIndices.Count > 0)
                selectionText = $"Selected: {_selectedDie.name} | Faces {BuildDisplayFaceList(_selectedDie, _selectedLogicalFaceIndices)}";
            else if (_selectedDie != null)
                selectionText = $"Selected: {_selectedDie.name}";
        }
        else if (_selectedDie != null && _selectedLogicalFaceIndex >= 0)
        {
            selectionText = $"Selected: {_selectedDie.name} face {_selectedLogicalFaceIndex}";
        }

        string commitText = "Committed: none";
        if (_committedDie != null && _committedLogicalFaceIndex >= 0)
            commitText = $"Committed: {_committedDie.name} face {_committedLogicalFaceIndex}";

        if (_selectionLabel != null)
            _selectionLabel.text = selectionText;

        if (_commitLabel != null)
            _commitLabel.text = commitText;

        if (_consumableLabel != null)
            _consumableLabel.text = BuildConsumableStatusText();

        if (_resultLabel != null)
            _resultLabel.text = _lastUseMessage;

        RefreshConsumableButtons();

        if (_useButton != null)
            _useButton.interactable = CanUseSelectedConsumable();

        if (_clearButton != null)
            _clearButton.interactable = _selectedDie != null || _committedLogicalFaceIndex >= 0 || _selectedLogicalFaceIndices.Count > 0;

        if (_flipButton != null)
            _flipButton.interactable = _focusedInteractable != null;

        UiStateChanged?.Invoke();
    }

    private DiceEditInteractable ResolvePrimaryInteractable()
    {
        if (_focusedInteractable != null)
            return _focusedInteractable;

        DiceSpinnerGeneric selectedDie = ResolveSelectedDie();
        if (selectedDie == null)
            return null;

        for (int i = 0; i < _interactables.Count; i++)
        {
            DiceEditInteractable interactable = _interactables[i];
            if (interactable == null)
                continue;

            DiceSpinnerGeneric spinner = interactable.GetComponent<DiceSpinnerGeneric>();
            if (spinner == selectedDie)
                return interactable;
        }

        return null;
    }

    private static int GetFaceValue(DiceSpinnerGeneric die, int logicalFaceIndex)
    {
        if (die == null || die.faces == null || logicalFaceIndex < 0 || logicalFaceIndex >= die.faces.Length)
            return 0;

        return die.faces[logicalFaceIndex].value;
    }

    private static string GetDisplayFaceValue(DiceSpinnerGeneric die, int logicalFaceIndex)
    {
        return GetFaceValue(die, logicalFaceIndex).ToString();
    }

    private static string BuildDisplayFaceList(DiceSpinnerGeneric die, IReadOnlyList<int> logicalFaceIndices)
    {
        if (logicalFaceIndices == null || logicalFaceIndices.Count == 0)
            return string.Empty;

        List<string> labels = new List<string>(logicalFaceIndices.Count);
        for (int i = 0; i < logicalFaceIndices.Count; i++)
            labels.Add(GetDisplayFaceValue(die, logicalFaceIndices[i]));

        return string.Join(", ", labels);
    }

    private static RectTransform CreatePanel(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, TextAnchor anchor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor == TextAnchor.LowerLeft ? Vector2.zero : new Vector2(0.5f, 0.5f);
        rt.anchorMax = rt.anchorMin;
        rt.pivot = rt.anchorMin;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;
        return rt;
    }

    private static TMP_Text CreateText(string name, RectTransform parent, string text, float fontSize, Vector2 anchoredPosition, Vector2 size, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = alignment;
        label.color = Color.white;
        return label;
    }

    private static Button CreateButton(string name, RectTransform parent, string label, Vector2 anchoredPosition, Vector2 size, out TMP_Text labelText)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.21f, 0.27f, 0.35f, 1f);

        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.28f, 0.35f, 0.45f, 1f);
        colors.pressedColor = new Color(0.16f, 0.22f, 0.3f, 1f);
        colors.disabledColor = new Color(0.16f, 0.16f, 0.16f, 0.7f);
        button.colors = colors;

        labelText = CreateText("Label", rt, label, 20, new Vector2(size.x * 0.5f, -size.y * 0.5f), size, FontStyles.Bold, TextAlignmentOptions.Center);
        labelText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        labelText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        labelText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        labelText.rectTransform.anchoredPosition = Vector2.zero;

        return button;
    }

    private void FlipFocusedDie()
    {
        if (_focusedInteractable == null)
            return;

        _focusedInteractable.FlipInspectOrientation();
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
                _copySourceDie?.RefreshDisplayedState();
                if (_copyTargetDie != _copySourceDie)
                    _copyTargetDie?.RefreshDisplayedState();
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
                die.RefreshDisplayedState();
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
                targetDie.RefreshDisplayedState();
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

    public SandboxFaceHighlightKind GetHighlightKindForFace(DiceSpinnerGeneric die, int logicalFaceIndex)
    {
        if (IsCopyPasteFaceMode())
        {
            if (die == _copySourceDie && logicalFaceIndex == _copySourceLogicalFaceIndex)
                return SandboxFaceHighlightKind.CopySource;
            if (die == _copyTargetDie && logicalFaceIndex == _copyTargetLogicalFaceIndex)
                return SandboxFaceHighlightKind.CopyTarget;
            return SandboxFaceHighlightKind.None;
        }

        if (IsCommittedSelection(die, logicalFaceIndex))
            return SandboxFaceHighlightKind.Committed;
        if (IsPreviewSelection(die, logicalFaceIndex))
            return SandboxFaceHighlightKind.Preview;
        return SandboxFaceHighlightKind.None;
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
            // When both slots are occupied, a fresh click should replace the first available slot only
            // after the user explicitly clears one by clicking its current highlight.
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

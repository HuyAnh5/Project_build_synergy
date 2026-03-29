using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DiceEditSandboxController : MonoBehaviour
{
    private const bool DebugLogs = false;
    private readonly List<DiceEditInteractable> _interactables = new List<DiceEditInteractable>();

    private Canvas _canvas;
    private TMP_Text _selectionLabel;
    private TMP_Text _commitLabel;
    private Button _useButton;
    private Button _clearButton;
    private Button _flipButton;

    private DiceSpinnerGeneric _selectedDie;
    private DiceSpinnerGeneric _committedDie;
    private int _selectedLogicalFaceIndex = -1;
    private int _committedLogicalFaceIndex = -1;
    private DiceEditInteractable _activeDragInteractable;
    private DiceEditInteractable _focusedInteractable;

    public void InitializeForScene(Scene scene)
    {
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        EnsureEventSystem();
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

        RectTransform panel = CreatePanel("Panel", canvasGo.transform as RectTransform, new Vector2(16f, 16f), new Vector2(360f, 220f), TextAnchor.LowerLeft);
        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.1f, 0.14f, 0.86f);

        CreateText("Title", panel, "Dice Edit Sandbox", 28, new Vector2(18f, -18f), new Vector2(320f, 32f), FontStyles.Bold, TextAlignmentOptions.Left);
        CreateText("Hint", panel, "Drag ngang de xoay. Drag doc de flip/chinh chieu. Vuot nhanh se co quan tinh.", 18, new Vector2(18f, -54f), new Vector2(324f, 44f), FontStyles.Normal, TextAlignmentOptions.Left);
        _selectionLabel = CreateText("Selection", panel, "Selected: none", 20, new Vector2(18f, -108f), new Vector2(320f, 28f), FontStyles.Bold, TextAlignmentOptions.Left);
        _commitLabel = CreateText("Commit", panel, "Committed: none", 18, new Vector2(18f, -138f), new Vector2(320f, 28f), FontStyles.Normal, TextAlignmentOptions.Left);

        _useButton = CreateButton("UseButton", panel, "Use", new Vector2(18f, -176f), new Vector2(120f, 32f));
        _useButton.onClick.AddListener(CommitCurrentSelection);

        _clearButton = CreateButton("ClearButton", panel, "Clear", new Vector2(152f, -176f), new Vector2(120f, 32f));
        _clearButton.onClick.AddListener(ClearSelection);

        _flipButton = CreateButton("FlipButton", panel, "Flip", new Vector2(286f, -176f), new Vector2(56f, 32f));
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
        if (_selectedDie == die && _selectedLogicalFaceIndex == logicalFaceIndex)
        {
            _selectedDie = null;
            _selectedLogicalFaceIndex = -1;
            Debug.Log($"[DiceEditSelect] Deselected die='{die?.name}' faceIndex={logicalFaceIndex}");
            RefreshAllHighlights();
            RefreshUi();
            return;
        }

        _selectedDie = die;
        _selectedLogicalFaceIndex = logicalFaceIndex;
        Debug.Log($"[DiceEditSelect] Selected die='{die?.name}' faceIndex={logicalFaceIndex} value={GetFaceValue(die, logicalFaceIndex)}");
        RefreshAllHighlights();
        RefreshUi();
    }

    public void SetFocusedInteractable(DiceEditInteractable interactable)
    {
        if (interactable != null)
            _focusedInteractable = interactable;

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
        RefreshAllHighlights();
        RefreshUi();
    }

    private void RefreshAllHighlights()
    {
        for (int i = 0; i < _interactables.Count; i++)
        {
            if (_interactables[i] != null)
                _interactables[i].RefreshHighlight();
        }
    }

    private void RefreshUi()
    {
        string selectionText = "Selected: none";
        if (_selectedDie != null && _selectedLogicalFaceIndex >= 0)
        {
            int value = GetFaceValue(_selectedDie, _selectedLogicalFaceIndex);
            selectionText = $"Selected: {_selectedDie.name} face {_selectedLogicalFaceIndex} (value {value})";
        }

        string commitText = "Committed: none";
        if (_committedDie != null && _committedLogicalFaceIndex >= 0)
        {
            int value = GetFaceValue(_committedDie, _committedLogicalFaceIndex);
            commitText = $"Committed: {_committedDie.name} face {_committedLogicalFaceIndex} (value {value})";
        }

        if (_selectionLabel != null)
            _selectionLabel.text = selectionText;

        if (_commitLabel != null)
            _commitLabel.text = commitText;

        if (_useButton != null)
            _useButton.interactable = _selectedDie != null && _selectedLogicalFaceIndex >= 0;

        if (_clearButton != null)
            _clearButton.interactable = _selectedDie != null || _committedLogicalFaceIndex >= 0;

        if (_flipButton != null)
            _flipButton.interactable = _focusedInteractable != null;
    }

    private static int GetFaceValue(DiceSpinnerGeneric die, int logicalFaceIndex)
    {
        if (die == null || die.faces == null || logicalFaceIndex < 0 || logicalFaceIndex >= die.faces.Length)
            return 0;

        return die.faces[logicalFaceIndex].value;
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

    private static Button CreateButton(string name, RectTransform parent, string label, Vector2 anchoredPosition, Vector2 size)
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

        TMP_Text text = CreateText("Label", rt, label, 20, new Vector2(size.x * 0.5f, -size.y * 0.5f), size, FontStyles.Bold, TextAlignmentOptions.Center);
        text.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        text.rectTransform.anchoredPosition = Vector2.zero;

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
}

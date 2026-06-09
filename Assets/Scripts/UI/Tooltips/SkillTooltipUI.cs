using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Public facade and lifecycle owner for skill tooltips. Layout/content and
// positioning details are split into partial files to keep responsibilities small.
public sealed partial class SkillTooltipUI : MonoBehaviour
{
    private const string TooltipPrefabResourcePath = "UI/SkillTooltipLayout";
    private const string TooltipOverlayCanvasName = "SkillTooltipOverlayCanvas";
    private const string TooltipHoverBridgeName = "SkillTooltipHoverBridge";
    private const string TooltipRequiresHeader = "---------- Requires ----------";
    private const string TooltipConditionHeader = "---------- Condition ----------";
    private const float DefaultMinContentWidth = 170f;
    private const float DefaultMaxContentWidth = 460f;
    private const float TooltipHorizontalCanvasPadding = 8f;
    private const float TooltipVerticalCanvasPadding = 8f;
    private const float TooltipVerticalOffset = 10f;
    private const float PanelContentHorizontalInset = 24f;

    private static SkillTooltipUI _instance;

    private RectTransform _root;
    private TMP_Text _title;
    private TMP_Text _cost;
    private TMP_Text _targeting;
    private TMP_Text _effect;
    private TMP_Text _requiresHeader;
    private TMP_Text _requires;
    private TMP_Text _conditionHeader;
    private TMP_Text _condition;
    private Image _elementIcon;
    private LayoutElement _titleLayout;
    private LayoutElement _costLayout;
    private LayoutElement _targetingLayout;
    private LayoutElement _effectLayout;
    private LayoutElement _requiresHeaderLayout;
    private LayoutElement _requiresLayout;
    private LayoutElement _conditionHeaderLayout;
    private LayoutElement _conditionLayout;
    private RectTransform _hoverBridge;
    private Image _hoverBridgeImage;
    private RectTransform _hoverBridgeCanvasRect;
    private Camera _hoverBridgeCamera;
    private RectTransform _canvasRect;
    private Camera _uiCamera;
    private Camera _targetCamera;
    private RectTransform _currentTarget;
    private ISkillTooltipSource _currentSource;
    private bool _currentUsesPanel;
    private bool _currentPinnedToSelection;
    private SkillTooltipLayout _layout;
    private bool _lastExpandedState;
    private string _lastContentSignature;

    public static void Show(Canvas canvas, RectTransform target, ScriptableObject asset, SkillRuntime runtime = null)
        => ShowInternal(canvas, target, asset, runtime, null);

    /// <summary>Shows a tooltip by querying an ISkillTooltipSource for current asset/runtime data.</summary>
    public static void Show(ISkillTooltipSource source)
    {
        if (source == null ||
            !source.TryGetSkillTooltip(out Canvas canvas, out RectTransform target, out ScriptableObject asset, out SkillRuntime runtime))
        {
            HideCurrent();
            return;
        }

        ShowInternal(canvas, target, asset, runtime, source);
    }

    public static void ShowSkillPanel(ISkillTooltipSource source, bool pinToSelection)
    {
        if (source == null ||
            !source.TryGetSkillTooltip(out Canvas canvas, out RectTransform target, out ScriptableObject asset, out SkillRuntime runtime))
        {
            HideCurrent();
            return;
        }

        ShowInternal(canvas, target, asset, runtime, source, preferPanel: true, pinToSelection: pinToSelection);
    }

    /// <summary>Rebuilds the currently visible tooltip from its source, preserving hover ownership.</summary>
    public static void RefreshCurrent()
    {
        if (_instance == null || _instance._root == null || !_instance._root.gameObject.activeSelf)
            return;

        if (UiDragState.IsDragging)
        {
            HideCurrent();
            return;
        }

        ISkillTooltipSource source = _instance._currentSource;
        if (source == null)
            return;

        if (!source.TryGetSkillTooltip(out Canvas canvas, out RectTransform target, out ScriptableObject asset, out SkillRuntime runtime))
        {
            HideCurrent();
            return;
        }

        ShowInternal(canvas, target, asset, runtime, source, _instance._currentUsesPanel, _instance._currentPinnedToSelection);
    }

    public static void HideCurrent()
    {
        if (_instance != null && _instance._root != null)
        {
            _instance._root.gameObject.SetActive(false);
            if (_instance._hoverBridge != null)
                _instance._hoverBridge.gameObject.SetActive(false);
            _instance._currentSource = null;
            _instance._currentTarget = null;
            _instance._currentUsesPanel = false;
            _instance._currentPinnedToSelection = false;
            _instance._lastExpandedState = false;
            _instance._lastContentSignature = null;
        }
    }

    public static void HideCurrentIfSource(ISkillTooltipSource source)
    {
        if (IsPinnedSelectedSource(source))
            return;

        if (IsCurrentSource(source))
            HideCurrent();
    }

    public static void HideCurrentUnlessPointerOverTooltip(GameObject pointerTarget = null)
    {
        if (IsPinnedSelectedSource(_instance != null ? _instance._currentSource : null))
            return;

        if (IsPointerOverCurrentTooltip(pointerTarget))
            return;

        HideCurrent();
    }

    public static bool IsCurrentSource(ISkillTooltipSource source)
    {
        return source != null &&
               _instance != null &&
               _instance._root != null &&
               _instance._root.gameObject.activeInHierarchy &&
               ReferenceEquals(_instance._currentSource, source);
    }

    private static bool IsPinnedSelectedSource(ISkillTooltipSource source)
    {
        return source != null &&
               _instance != null &&
               _instance._root != null &&
               _instance._root.gameObject.activeInHierarchy &&
               _instance._currentUsesPanel &&
               _instance._currentPinnedToSelection &&
               ReferenceEquals(_instance._currentSource, source) &&
               ReferenceEquals(UiDragState.SelectedSkill, source);
    }

    public static bool IsPointerOverCurrentTooltip(GameObject pointerTarget = null)
    {
        if (_instance == null || _instance._root == null || !_instance._root.gameObject.activeInHierarchy)
            return false;

        Vector2 screenPoint = Input.mousePosition;
        if (_instance.IsScreenPointInsideRect(_instance._root, screenPoint, _instance._uiCamera))
            return true;

        if (_instance._hoverBridge != null &&
            _instance._hoverBridge.gameObject.activeInHierarchy &&
            _instance.IsScreenPointInsideRect(_instance._hoverBridge, screenPoint, _instance._hoverBridgeCamera))
        {
            return true;
        }

        if (_instance.IsScreenPointInsideHoverBridgeZone(screenPoint))
            return true;

        if (_instance._currentTarget != null &&
            _instance.IsScreenPointInsideRect(_instance._currentTarget, screenPoint, _instance._targetCamera))
        {
            return true;
        }

        return false;
    }

    private static void ShowInternal(Canvas canvas, RectTransform target, ScriptableObject asset, SkillRuntime runtime, ISkillTooltipSource source)
    {
        if (UiDragState.IsDragging || canvas == null || target == null || asset == null)
        {
            HideCurrent();
            return;
        }

        SkillTooltipUI tooltip = GetOrCreate(canvas);
        if (tooltip == null)
        {
            HideCurrent();
            return;
        }

        tooltip._currentSource = source;
        tooltip._currentUsesPanel = false;
        tooltip._currentPinnedToSelection = false;
        tooltip.ShowInternal(target, asset, runtime, null);
    }

    private static void ShowInternal(Canvas canvas, RectTransform target, ScriptableObject asset, SkillRuntime runtime, ISkillTooltipSource source, bool preferPanel, bool pinToSelection)
    {
        if (UiDragState.IsDragging || canvas == null || target == null || asset == null)
        {
            HideCurrent();
            return;
        }

        SkillTooltipUI tooltip = GetOrCreate(canvas);
        if (tooltip == null)
        {
            HideCurrent();
            return;
        }

        SkillTooltipPanelAnchor panelAnchor = preferPanel ? SkillTooltipPanelAnchor.FindForCanvas(canvas) : null;
        tooltip._currentSource = source;
        tooltip._currentUsesPanel = panelAnchor != null;
        tooltip._currentPinnedToSelection = panelAnchor != null && pinToSelection;
        tooltip.ShowInternal(target, asset, runtime, panelAnchor);
    }

    private static SkillTooltipUI GetOrCreate(Canvas canvas)
    {
        Canvas overlayCanvas = GetOrCreateOverlayCanvas(canvas);
        if (overlayCanvas == null)
            return null;

        if (_instance != null)
        {
            _instance.BindCanvas(overlayCanvas);
            return _instance;
        }

        SkillTooltipUI existing = overlayCanvas.GetComponentInChildren<SkillTooltipUI>(true);
        if (existing != null)
        {
            _instance = existing;
            _instance.InitializeFromExisting(overlayCanvas);
            return _instance;
        }

        GameObject prefab = Resources.Load<GameObject>(TooltipPrefabResourcePath);
        if (prefab == null)
        {
            Debug.LogError($"Skill tooltip prefab not found at Resources/{TooltipPrefabResourcePath}.", canvas);
            return null;
        }

        GameObject instance = Instantiate(prefab, overlayCanvas.transform);
        instance.name = "SkillTooltip";
        _instance = instance.GetComponent<SkillTooltipUI>();
        if (_instance == null)
        {
            Debug.LogError("Skill tooltip prefab is missing SkillTooltipUI.", instance);
            Destroy(instance);
            return null;
        }

        _instance.InitializeFromExisting(overlayCanvas);
        return _instance;
    }

    private static Canvas GetOrCreateOverlayCanvas(Canvas sourceCanvas)
    {
        Canvas existing = FindTooltipOverlayCanvas();
        if (existing != null)
        {
            ConfigureTooltipOverlayCanvas(existing, sourceCanvas);
            return existing;
        }

        GameObject canvasGo = new GameObject(
            TooltipOverlayCanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas overlayCanvas = canvasGo.GetComponent<Canvas>();
        ConfigureTooltipOverlayCanvas(overlayCanvas, sourceCanvas);

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return overlayCanvas;
    }

    private static void ConfigureTooltipOverlayCanvas(Canvas overlayCanvas, Canvas sourceCanvas)
    {
        if (overlayCanvas == null)
            return;

        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.worldCamera = null;
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = short.MaxValue;
        overlayCanvas.pixelPerfect = false;

        CanvasScaler scaler = overlayCanvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (sourceCanvas != null)
            overlayCanvas.gameObject.layer = sourceCanvas.gameObject.layer;
    }

    private static Canvas FindTooltipOverlayCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas != null && canvas.name == TooltipOverlayCanvasName)
                return canvas;
        }

        return null;
    }

    public static Canvas GetOrCreateSharedOverlayCanvas(Canvas sourceCanvas)
        => GetOrCreateOverlayCanvas(sourceCanvas);

    private void OnEnable()
    {
        UiDragState.DragStateChanged += HandleDragStateChanged;
    }

    private void OnDisable()
    {
        UiDragState.DragStateChanged -= HandleDragStateChanged;
    }

    private void HandleDragStateChanged()
    {
        if (UiDragState.IsDragging)
            HideCurrent();
    }

    private void Update()
    {
        if (_root == null || !_root.gameObject.activeSelf || UiDragState.IsDragging)
            return;

        bool expanded = IsExpandedInputActive();
        if (expanded != _lastExpandedState)
        {
            _lastExpandedState = expanded;
            RefreshCurrent();
            return;
        }

        if (_currentUsesPanel && _currentPinnedToSelection)
            return;

        if (!IsPointerOverCurrentTooltip())
            HideCurrent();
    }

    private static bool IsExpandedInputActive()
    {
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }
}

[DisallowMultipleComponent]
public sealed class SkillTooltipPanelAnchor : MonoBehaviour
{
    private static readonly string[] FallbackNames = { "SkillTooltipPanel", "SkillTooltipPanelAnchor" };

    [SerializeField] private bool useForSkillTooltips = true;
    [SerializeField] private Vector2 fallbackSize = new Vector2(470f, 220f);
    [SerializeField] private bool hidePreviewGraphicInPlayMode;

    public RectTransform RectTransform => transform as RectTransform;
    public Vector2 FallbackSize => fallbackSize;

    private void Awake()
    {
        if (!hidePreviewGraphicInPlayMode)
            return;

        Graphic graphic = GetComponent<Graphic>();
        if (graphic != null)
            graphic.enabled = false;
    }

    public static SkillTooltipPanelAnchor FindForCanvas(Canvas sourceCanvas)
    {
        SkillTooltipPanelAnchor[] anchors = FindObjectsByType<SkillTooltipPanelAnchor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        SkillTooltipPanelAnchor fallback = null;

        for (int i = 0; i < anchors.Length; i++)
        {
            SkillTooltipPanelAnchor anchor = anchors[i];
            if (anchor == null || !anchor.useForSkillTooltips || anchor.RectTransform == null)
                continue;

            if (fallback == null)
                fallback = anchor;

            Canvas anchorCanvas = anchor.GetComponentInParent<Canvas>();
            if (sourceCanvas != null && anchorCanvas == sourceCanvas)
                return anchor;
        }

        if (fallback != null)
            return fallback;

        RectTransform namedPanel = FindNamedPanel(sourceCanvas);
        return namedPanel != null ? namedPanel.gameObject.AddComponent<SkillTooltipPanelAnchor>() : null;
    }

    private static RectTransform FindNamedPanel(Canvas sourceCanvas)
    {
        for (int i = 0; i < FallbackNames.Length; i++)
        {
            GameObject found = GameObject.Find(FallbackNames[i]);
            RectTransform rect = found != null ? found.transform as RectTransform : null;
            if (rect == null)
                continue;

            Canvas canvas = rect.GetComponentInParent<Canvas>();
            if (sourceCanvas == null || canvas == sourceCanvas)
                return rect;
        }

        return null;
    }
}

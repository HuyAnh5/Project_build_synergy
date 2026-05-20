using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SkillTooltipUI : MonoBehaviour
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
    private SkillTooltipLayout _layout;
    private bool _lastExpandedState;

    public static void Show(Canvas canvas, RectTransform target, ScriptableObject asset, SkillRuntime runtime = null)
        => ShowInternal(canvas, target, asset, runtime, null);

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

        ShowInternal(canvas, target, asset, runtime, source);
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
            _instance._lastExpandedState = false;
        }
    }

    public static void HideCurrentUnlessPointerOverTooltip(GameObject pointerTarget = null)
    {
        if (IsPointerOverCurrentTooltip(pointerTarget))
            return;

        HideCurrent();
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
        tooltip.ShowInternal(target, asset, runtime);
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
            return existing;

        GameObject canvasGo = new GameObject(
            TooltipOverlayCanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas overlayCanvas = canvasGo.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = short.MaxValue;
        overlayCanvas.pixelPerfect = false;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (sourceCanvas != null)
            canvasGo.layer = sourceCanvas.gameObject.layer;

        DontDestroyOnLoad(canvasGo);
        return overlayCanvas;
    }

    private static Canvas FindTooltipOverlayCanvas()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas != null && canvas.name == TooltipOverlayCanvasName)
                return canvas;
        }

        return null;
    }

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

        if (!IsPointerOverCurrentTooltip())
            HideCurrent();
    }

    private void InitializeFromExisting(Canvas canvas)
    {
        _layout = GetComponent<SkillTooltipLayout>();
        _root = transform as RectTransform;
        _title = _layout != null ? _layout.TitleText : GetComponentInChildren<TMP_Text>(true);
        EnsureStructuredLayoutChildren();
        _titleLayout = EnsureLayoutElement(_title);
        _costLayout = EnsureLayoutElement(_cost);
        _targetingLayout = EnsureLayoutElement(_targeting);
        _effectLayout = EnsureLayoutElement(_effect);
        _requiresHeaderLayout = EnsureLayoutElement(_requiresHeader);
        _requiresLayout = EnsureLayoutElement(_requires);
        _conditionHeaderLayout = EnsureLayoutElement(_conditionHeader);
        _conditionLayout = EnsureLayoutElement(_condition);
        EnsureHoverBridge();
        NormalizeLayoutSettings();
        BindCanvas(canvas);
        _root.gameObject.SetActive(false);
        if (_hoverBridge != null)
            _hoverBridge.gameObject.SetActive(false);
    }

    private void NormalizeLayoutSettings()
    {
        if (_root == null)
            return;

        _root.anchorMin = new Vector2(0.5f, 0.5f);
        _root.anchorMax = new Vector2(0.5f, 0.5f);
        _root.pivot = new Vector2(0.5f, 0f);

        Image background = _layout != null ? _layout.Background : GetComponent<Image>();
        if (background != null)
            background.raycastTarget = false;

        VerticalLayoutGroup verticalLayout = _layout != null ? _layout.VerticalLayout : GetComponent<VerticalLayoutGroup>();
        if (verticalLayout != null)
        {
            verticalLayout.childAlignment = TextAnchor.UpperLeft;
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = true;
            verticalLayout.childForceExpandWidth = false;
            verticalLayout.childForceExpandHeight = false;
        }

        ContentSizeFitter fitter = _layout != null ? _layout.ContentSizeFitter : GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        NormalizeTextSettings(_title);
        NormalizeTextSettings(_cost);
        NormalizeTextSettings(_targeting);
        NormalizeTextSettings(_effect);
        NormalizeTextSettings(_requiresHeader);
        NormalizeTextSettings(_requires);
        NormalizeTextSettings(_conditionHeader);
        NormalizeTextSettings(_condition);
        if (_hoverBridgeImage != null)
            _hoverBridgeImage.raycastTarget = false;
        if (_elementIcon != null)
            _elementIcon.raycastTarget = false;
    }

    private static void NormalizeTextSettings(TMP_Text text)
    {
        if (text == null)
            return;

        RectTransform rect = text.rectTransform;
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
        }

        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.margin = Vector4.zero;
        text.raycastTarget = false;

        LayoutElement layout = EnsureLayoutElement(text);
        if (layout != null)
        {
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
        }
    }

    private void BindCanvas(Canvas canvas)
    {
        if (canvas == null)
            return;

        _canvasRect = canvas.transform as RectTransform;
        _uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (_root != null && _root.parent != canvas.transform)
            _root.SetParent(canvas.transform, false);
        if (_root != null)
            _root.SetAsLastSibling();
    }

    private void ShowInternal(RectTransform target, ScriptableObject asset, SkillRuntime runtime)
    {
        if (_root == null || _title == null || _targeting == null || _effect == null || _requiresHeader == null || _requires == null || _conditionHeader == null || _condition == null)
            return;

        Canvas targetCanvas = target != null ? target.GetComponentInParent<Canvas>() : null;
        _currentTarget = target;
        _targetCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? targetCanvas.worldCamera
            : null;
        BindHoverBridgeToTargetCanvas(targetCanvas);

        bool expanded = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        _lastExpandedState = expanded;
        SkillTooltipFormatter.TooltipContent content = SkillTooltipFormatter.BuildContent(asset, runtime, expanded);
        ApplyContent(content);
        _root.gameObject.SetActive(true);
        _root.SetAsLastSibling();
        if (_hoverBridge != null)
            _hoverBridge.gameObject.SetActive(true);

        ApplyDynamicSizing();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_root);
        PositionNear(target);
        PositionHoverBridge(target);
    }

    private void ApplyDynamicSizing()
    {
        if (_root == null || _title == null)
            return;

        float minContentWidth = _layout != null ? Mathf.Max(DefaultMinContentWidth, _layout.MinContentWidth) : DefaultMinContentWidth;
        float maxContentWidth = _layout != null ? Mathf.Max(DefaultMaxContentWidth, _layout.MaxContentWidth) : DefaultMaxContentWidth;
        float contentWidth = Mathf.Clamp(GetPreferredWidth(maxContentWidth), minContentWidth, maxContentWidth);

        if (_titleLayout != null)
            _titleLayout.preferredWidth = contentWidth;
        if (_costLayout != null)
            _costLayout.preferredWidth = contentWidth;
        if (_targetingLayout != null)
            _targetingLayout.preferredWidth = contentWidth;
        if (_effectLayout != null)
            _effectLayout.preferredWidth = contentWidth;
        if (_requiresHeaderLayout != null)
            _requiresHeaderLayout.preferredWidth = contentWidth;
        if (_requiresLayout != null)
            _requiresLayout.preferredWidth = contentWidth;
        if (_conditionHeaderLayout != null)
            _conditionHeaderLayout.preferredWidth = contentWidth;
        if (_conditionLayout != null)
            _conditionLayout.preferredWidth = contentWidth;

        float horizontalPadding = 0f;
        float verticalPadding = 0f;
        float spacing = 0f;
        VerticalLayoutGroup layoutGroup = _layout != null ? _layout.VerticalLayout : GetComponent<VerticalLayoutGroup>();
        if (layoutGroup != null)
        {
            horizontalPadding = layoutGroup.padding.left + layoutGroup.padding.right;
            verticalPadding = layoutGroup.padding.top + layoutGroup.padding.bottom;
            spacing = layoutGroup.spacing;
        }

        float preferredHeight = Mathf.Ceil(GetPreferredHeight(contentWidth) + verticalPadding + spacing * GetVisibleBlockCountPadding());
        float minHeight = _layout != null ? _layout.MinContentHeight : 0f;
        float maxHeight = _layout != null ? _layout.MaxContentHeight : 0f;
        if (maxHeight > 0f && maxHeight >= minHeight)
            preferredHeight = Mathf.Clamp(preferredHeight, minHeight, maxHeight);
        else if (minHeight > 0f)
            preferredHeight = Mathf.Max(preferredHeight, minHeight);

        _root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth + horizontalPadding);
        _root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);
    }

    private float GetPreferredWidth(float maxContentWidth)
    {
        float max = 0f;
        max = Mathf.Max(max, GetTextPreferredWidth(_title, maxContentWidth));
        max = Mathf.Max(max, GetTextPreferredWidth(_cost, maxContentWidth));
        max = Mathf.Max(max, GetTextPreferredWidth(_targeting, maxContentWidth));
        max = Mathf.Max(max, GetTextPreferredWidth(_effect, maxContentWidth));
        max = Mathf.Max(max, GetTextPreferredWidth(_requiresHeader, maxContentWidth));
        max = Mathf.Max(max, GetTextPreferredWidth(_requires, maxContentWidth));
        max = Mathf.Max(max, GetTextPreferredWidth(_conditionHeader, maxContentWidth));
        max = Mathf.Max(max, GetTextPreferredWidth(_condition, maxContentWidth));
        return max;
    }

    private float GetPreferredHeight(float contentWidth)
    {
        float total = 0f;
        total += GetTextPreferredHeight(_title, contentWidth);
        total += GetTextPreferredHeight(_cost, contentWidth);
        total += GetTextPreferredHeight(_targeting, contentWidth);
        total += GetTextPreferredHeight(_effect, contentWidth);
        total += GetTextPreferredHeight(_requiresHeader, contentWidth);
        total += GetTextPreferredHeight(_requires, contentWidth);
        total += GetTextPreferredHeight(_conditionHeader, contentWidth);
        total += GetTextPreferredHeight(_condition, contentWidth);
        return total;
    }

    private static float GetTextPreferredWidth(TMP_Text text, float maxContentWidth)
    {
        return text != null && text.gameObject.activeSelf
            ? text.GetPreferredValues(text.text ?? string.Empty, maxContentWidth, 0f).x
            : 0f;
    }

    private static float GetTextPreferredHeight(TMP_Text text, float width)
    {
        return text != null && text.gameObject.activeSelf
            ? text.GetPreferredValues(text.text ?? string.Empty, width, 0f).y
            : 0f;
    }

    private int GetVisibleBlockCountPadding()
    {
        int visible = 0;
        visible += IsVisible(_title) ? 1 : 0;
        visible += IsVisible(_cost) ? 1 : 0;
        visible += IsVisible(_targeting) ? 1 : 0;
        visible += IsVisible(_effect) ? 1 : 0;
        visible += IsVisible(_requiresHeader) ? 1 : 0;
        visible += IsVisible(_requires) ? 1 : 0;
        visible += IsVisible(_conditionHeader) ? 1 : 0;
        visible += IsVisible(_condition) ? 1 : 0;
        return Mathf.Max(0, visible - 1);
    }

    private static bool IsVisible(Component c) => c != null && c.gameObject.activeSelf;

    private void ApplyContent(SkillTooltipFormatter.TooltipContent content)
    {
        bool expanded = !string.IsNullOrWhiteSpace(content.costText);
        _title.text = expanded
            ? $"{content.title}    {content.costText}"
            : (content.title ?? string.Empty);
        _cost.text = string.Empty;
        _targeting.text = content.targeting ?? string.Empty;
        _effect.text = content.effectText ?? string.Empty;

        bool hasRequires = content.requires != null && content.requires.Count > 0;
        bool hasConditions = content.conditions != null && content.conditions.Count > 0;
        _requiresHeader.text = TooltipRequiresHeader;
        _requires.text = BuildSectionText(content.requires);
        _conditionHeader.text = TooltipConditionHeader;
        _condition.text = BuildSectionText(content.conditions);

        SetVisible(_cost, false);
        SetVisible(_targeting, !string.IsNullOrWhiteSpace(_targeting.text));
        SetVisible(_effect, !string.IsNullOrWhiteSpace(_effect.text));
        SetVisible(_requiresHeader, hasRequires);
        SetVisible(_requires, hasRequires);
        SetVisible(_conditionHeader, hasConditions);
        SetVisible(_condition, hasConditions);

        if (_elementIcon != null)
        {
            Sprite sprite = null;
            bool showIcon = expanded && content.element.HasValue && TryResolveElementIcon(content.element.Value, out sprite);
            _elementIcon.sprite = showIcon ? sprite : null;
            _elementIcon.gameObject.SetActive(showIcon);
        }
    }

    private static string BuildSectionText(List<string> lines)
    {
        if (lines == null || lines.Count == 0)
            return string.Empty;

        if (lines.Count == 1)
            return lines[0];

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < lines.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;
            if (sb.Length > 0)
                sb.Append('\n');
            sb.Append("- ").Append(lines[i]);
        }

        return sb.ToString();
    }

    private static void SetVisible(Component component, bool visible)
    {
        if (component != null)
            component.gameObject.SetActive(visible);
    }

    private bool TryResolveElementIcon(ElementType element, out Sprite icon)
    {
        icon = null;
        ActorWorldUI[] worldUis = FindObjectsOfType<ActorWorldUI>(true);
        for (int i = 0; i < worldUis.Length; i++)
        {
            SkillUiIconLibrarySO library = worldUis[i] != null ? worldUis[i].iconLibrary : null;
            if (library != null && library.TryGetElementIcon(element, out icon, out _, out _))
                return icon != null;
        }

        return false;
    }

    private void EnsureStructuredLayoutChildren()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        _cost = _layout != null ? _layout.CostText : FindText(texts, "Cost");
        _targeting = _layout != null ? _layout.TargetingText : FindText(texts, "Targeting");
        _effect = _layout != null ? _layout.EffectText : FindText(texts, "Effect");
        _requiresHeader = _layout != null ? _layout.RequiresHeaderText : FindText(texts, "RequiresHeader");
        _requires = _layout != null ? _layout.RequiresText : FindText(texts, "Requires");
        _conditionHeader = _layout != null ? _layout.ConditionHeaderText : FindText(texts, "ConditionHeader");
        _condition = _layout != null ? _layout.ConditionText : FindText(texts, "Condition");
        _elementIcon = _layout != null ? _layout.ElementIconImage : GetComponentInChildren<Image>(true);

        if (_cost == null) _cost = CreateText("Cost", 13f, FontStyles.Normal, TextAlignmentOptions.TopRight);
        if (_targeting == null) _targeting = CreateText("Targeting", 14f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        if (_effect == null) _effect = CreateText("Effect", 14f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        if (_requiresHeader == null) _requiresHeader = CreateText("RequiresHeader", 14f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        if (_requires == null) _requires = CreateText("Requires", 14f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        if (_conditionHeader == null) _conditionHeader = CreateText("ConditionHeader", 14f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        if (_condition == null) _condition = CreateText("Condition", 14f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        if (_elementIcon == null) _elementIcon = CreateElementIcon();

        _requiresHeader.text = TooltipRequiresHeader;
        _conditionHeader.text = TooltipConditionHeader;
    }

    private TMP_Text CreateText(string name, float fontSize, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(transform, false);
        TMP_Text text = go.GetComponent<TMP_Text>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    private Image CreateElementIcon()
    {
        GameObject go = new GameObject("ElementIcon", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        RectTransform rt = go.transform as RectTransform;
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(24f, 24f);
        Image image = go.GetComponent<Image>();
        image.raycastTarget = false;
        LayoutElement layout = go.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;
        return image;
    }

    private static TMP_Text FindText(TMP_Text[] texts, string name)
    {
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && string.Equals(texts[i].name, name, StringComparison.OrdinalIgnoreCase))
                return texts[i];
        }

        return null;
    }

    private static LayoutElement EnsureLayoutElement(TMP_Text text)
    {
        if (text == null)
            return null;

        LayoutElement layout = text.GetComponent<LayoutElement>();
        if (layout == null)
            layout = text.gameObject.AddComponent<LayoutElement>();
        return layout;
    }

    private void PositionNear(RectTransform target)
    {
        if (_canvasRect == null || target == null)
            return;

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector3 topCenterWorld = (corners[1] + corners[2]) * 0.5f;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(_targetCamera, topCenterWorld);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, _uiCamera, out Vector2 anchorLocal))
            return;

        Vector2 size = _root.rect.size;
        Rect rect = _canvasRect.rect;
        Vector2 desired = anchorLocal + new Vector2(0f, TooltipVerticalOffset);

        float halfWidth = size.x * 0.5f;
        float minX = rect.xMin + halfWidth + TooltipHorizontalCanvasPadding;
        float maxX = rect.xMax - halfWidth - TooltipHorizontalCanvasPadding;
        float minY = rect.yMin + TooltipVerticalCanvasPadding;
        float maxY = rect.yMax - size.y - TooltipVerticalCanvasPadding;

        desired.x = Mathf.Clamp(desired.x, minX, maxX);
        desired.y = Mathf.Clamp(desired.y, minY, maxY);
        _root.anchoredPosition = desired;
    }

    private void EnsureHoverBridge()
    {
        if (_hoverBridge != null)
            return;

        Transform existing = transform.parent != null ? transform.parent.Find(TooltipHoverBridgeName) : null;
        if (existing != null)
            _hoverBridge = existing as RectTransform;

        if (_hoverBridge == null)
        {
            GameObject bridgeGo = new GameObject(TooltipHoverBridgeName, typeof(RectTransform), typeof(Image));
            _hoverBridge = bridgeGo.GetComponent<RectTransform>();
            if (_root != null && _root.parent != null)
                _hoverBridge.SetParent(_root.parent, false);
        }

        if (_hoverBridge == null)
            return;

        _hoverBridge.anchorMin = new Vector2(0.5f, 0.5f);
        _hoverBridge.anchorMax = new Vector2(0.5f, 0.5f);
        _hoverBridge.pivot = new Vector2(0.5f, 0f);

        _hoverBridgeImage = _hoverBridge.GetComponent<Image>();
        if (_hoverBridgeImage == null)
            _hoverBridgeImage = _hoverBridge.gameObject.AddComponent<Image>();

        _hoverBridgeImage.color = new Color(1f, 1f, 1f, 0f);
        _hoverBridgeImage.raycastTarget = false;
    }

    private void BindHoverBridgeToTargetCanvas(Canvas targetCanvas)
    {
        if (_hoverBridge == null || targetCanvas == null)
            return;

        Transform parent = targetCanvas.transform;
        if (_hoverBridge.parent != parent)
            _hoverBridge.SetParent(parent, false);

        _hoverBridgeCanvasRect = targetCanvas.transform as RectTransform;
        _hoverBridgeCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;
    }

    private void PositionHoverBridge(RectTransform target)
    {
        if (_hoverBridge == null || _currentTarget == null || _hoverBridgeCanvasRect == null)
            return;

        Vector3[] targetCorners = new Vector3[4];
        _currentTarget.GetWorldCorners(targetCorners);
        Vector3 targetTopCenterWorld = (targetCorners[1] + targetCorners[2]) * 0.5f;
        Vector2 targetTopScreen = RectTransformUtility.WorldToScreenPoint(_targetCamera, targetTopCenterWorld);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_hoverBridgeCanvasRect, targetTopScreen, _hoverBridgeCamera, out Vector2 targetTopLocal))
            return;

        Vector3 tooltipBottomCenterWorld = _root.TransformPoint(new Vector3(0f, 0f, 0f));
        Vector2 tooltipBottomScreen = RectTransformUtility.WorldToScreenPoint(_uiCamera, tooltipBottomCenterWorld);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_hoverBridgeCanvasRect, tooltipBottomScreen, _hoverBridgeCamera, out Vector2 tooltipBottomLocal))
            return;

        float width = _currentTarget.rect.width;
        float height = Mathf.Max(0f, tooltipBottomLocal.y - targetTopLocal.y);

        _hoverBridge.SetAsLastSibling();
        _hoverBridge.anchoredPosition = targetTopLocal;
        _hoverBridge.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        _hoverBridge.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    private bool IsScreenPointInsideRect(RectTransform rect, Vector2 screenPoint, Camera camera)
    {
        return rect != null &&
               rect.gameObject.activeInHierarchy &&
               RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, camera);
    }

    private bool IsScreenPointInsideHoverBridgeZone(Vector2 screenPoint)
    {
        if (_currentTarget == null || _root == null || !_root.gameObject.activeInHierarchy)
            return false;

        Vector3[] targetCorners = new Vector3[4];
        _currentTarget.GetWorldCorners(targetCorners);

        Vector2 targetBottomLeft = RectTransformUtility.WorldToScreenPoint(_targetCamera, targetCorners[0]);
        Vector2 targetTopLeft = RectTransformUtility.WorldToScreenPoint(_targetCamera, targetCorners[1]);
        Vector2 targetTopRight = RectTransformUtility.WorldToScreenPoint(_targetCamera, targetCorners[2]);

        Vector3 tooltipBottomCenterWorld = _root.TransformPoint(Vector3.zero);
        Vector2 tooltipBottomCenter = RectTransformUtility.WorldToScreenPoint(_uiCamera, tooltipBottomCenterWorld);

        float minX = Mathf.Min(targetTopLeft.x, targetTopRight.x);
        float maxX = Mathf.Max(targetTopLeft.x, targetTopRight.x);
        float minY = Mathf.Min(targetTopLeft.y, tooltipBottomCenter.y);
        float maxY = Mathf.Max(targetTopLeft.y, tooltipBottomCenter.y);

        if (maxY <= minY)
            return false;

        bool insideBridgeColumn = screenPoint.x >= minX && screenPoint.x <= maxX;
        bool insideBridgeHeight = screenPoint.y >= minY && screenPoint.y <= maxY;
        if (insideBridgeColumn && insideBridgeHeight)
            return true;

        bool stillOnIcon = screenPoint.x >= Mathf.Min(targetBottomLeft.x, targetTopRight.x) &&
                           screenPoint.x <= Mathf.Max(targetBottomLeft.x, targetTopRight.x) &&
                           screenPoint.y >= Mathf.Min(targetBottomLeft.y, targetTopLeft.y) &&
                           screenPoint.y <= Mathf.Max(targetBottomLeft.y, targetTopLeft.y);

        return stillOnIcon;
    }

    private static bool IsExpandedInputActive()
    {
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

}

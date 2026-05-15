using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SkillTooltipUI : MonoBehaviour
{
    private const string TooltipPrefabResourcePath = "UI/SkillTooltipLayout";
    private const float DefaultMinContentWidth = 170f;
    private const float DefaultMaxContentWidth = 460f;

    private static SkillTooltipUI _instance;

    private RectTransform _root;
    private TMP_Text _title;
    private TMP_Text _body;
    private LayoutElement _titleLayout;
    private LayoutElement _bodyLayout;
    private RectTransform _canvasRect;
    private Camera _uiCamera;
    private ISkillTooltipSource _currentSource;
    private SkillTooltipLayout _layout;

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
            _instance._currentSource = null;
        }
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
        if (_instance != null)
        {
            _instance.BindCanvas(canvas);
            return _instance;
        }

        SkillTooltipUI existing = canvas != null ? canvas.GetComponentInChildren<SkillTooltipUI>(true) : null;
        if (existing != null)
        {
            _instance = existing;
            _instance.InitializeFromExisting(canvas);
            return _instance;
        }

        GameObject prefab = Resources.Load<GameObject>(TooltipPrefabResourcePath);
        if (prefab == null)
        {
            Debug.LogError($"Skill tooltip prefab not found at Resources/{TooltipPrefabResourcePath}.", canvas);
            return null;
        }

        GameObject instance = Instantiate(prefab, canvas.transform);
        instance.name = "SkillTooltip";
        _instance = instance.GetComponent<SkillTooltipUI>();
        if (_instance == null)
        {
            Debug.LogError("Skill tooltip prefab is missing SkillTooltipUI.", instance);
            Destroy(instance);
            return null;
        }

        _instance.InitializeFromExisting(canvas);
        return _instance;
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

    private void InitializeFromExisting(Canvas canvas)
    {
        _layout = GetComponent<SkillTooltipLayout>();
        _root = transform as RectTransform;
        _title = _layout != null ? _layout.TitleText : GetComponentInChildren<TMP_Text>(true);
        _body = _layout != null ? _layout.BodyText : null;

        if (_body == null)
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length > 1)
                _body = texts[1];
        }

        _titleLayout = EnsureLayoutElement(_title);
        _bodyLayout = EnsureLayoutElement(_body);
        NormalizeLayoutSettings();
        BindCanvas(canvas);
        _root.gameObject.SetActive(false);
    }

    private void NormalizeLayoutSettings()
    {
        if (_root == null)
            return;

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
        NormalizeTextSettings(_body);
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
        if (_root == null || _title == null || _body == null)
            return;

        _title.text = SkillTooltipFormatter.GetTitle(asset);
        _body.text = SkillTooltipFormatter.BuildBody(asset, runtime);
        _root.gameObject.SetActive(true);
        _root.SetAsLastSibling();

        ApplyDynamicSizing();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_root);
        PositionNear(target);
    }

    private void ApplyDynamicSizing()
    {
        if (_root == null || _title == null || _body == null)
            return;

        float minContentWidth = _layout != null ? Mathf.Max(DefaultMinContentWidth, _layout.MinContentWidth) : DefaultMinContentWidth;
        float maxContentWidth = _layout != null ? Mathf.Max(DefaultMaxContentWidth, _layout.MaxContentWidth) : DefaultMaxContentWidth;
        float titlePreferred = _title.GetPreferredValues(_title.text ?? string.Empty, maxContentWidth, 0f).x;
        float singleLineBodyWidth = _body.GetPreferredValues(_body.text ?? string.Empty, 4096f, 0f).x;
        float wrappedBodyWidth = _body.GetPreferredValues(_body.text ?? string.Empty, maxContentWidth, 0f).x;
        float targetBodyWidth = Mathf.Min(singleLineBodyWidth, maxContentWidth);
        float contentWidth = Mathf.Clamp(Mathf.Max(titlePreferred, wrappedBodyWidth, targetBodyWidth), minContentWidth, maxContentWidth);

        if (_titleLayout != null)
            _titleLayout.preferredWidth = contentWidth;
        if (_bodyLayout != null)
            _bodyLayout.preferredWidth = contentWidth;

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

        float titleHeight = _title.GetPreferredValues(_title.text ?? string.Empty, contentWidth, 0f).y;
        float bodyHeight = _body.GetPreferredValues(_body.text ?? string.Empty, contentWidth, 0f).y;
        float preferredHeight = Mathf.Ceil(titleHeight + bodyHeight + verticalPadding + spacing);
        float minHeight = _layout != null ? _layout.MinContentHeight : 0f;
        float maxHeight = _layout != null ? _layout.MaxContentHeight : 0f;
        if (maxHeight > 0f && maxHeight >= minHeight)
            preferredHeight = Mathf.Clamp(preferredHeight, minHeight, maxHeight);
        else if (minHeight > 0f)
            preferredHeight = Mathf.Max(preferredHeight, minHeight);

        _root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth + horizontalPadding);
        _root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);
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
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(_uiCamera, corners[2]);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, _uiCamera, out Vector2 local))
            return;

        Vector2 desired = local + new Vector2(14f, 10f);
        Vector2 size = _root.rect.size;
        Rect rect = _canvasRect.rect;

        float minX = rect.xMin + 8f;
        float maxX = rect.xMax - size.x - 8f;
        float minY = rect.yMin + size.y + 8f;
        float maxY = rect.yMax - 8f;

        desired.x = Mathf.Clamp(desired.x, minX, maxX);
        desired.y = Mathf.Clamp(desired.y, minY, maxY);
        _root.anchoredPosition = desired;
    }
}

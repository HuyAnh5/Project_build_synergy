using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SkillTooltipUI : MonoBehaviour
{
    private static SkillTooltipUI _instance;

    private RectTransform _root;
    private TMP_Text _title;
    private TMP_Text _body;
    private Canvas _canvas;
    private RectTransform _canvasRect;
    private Camera _uiCamera;
    private ISkillTooltipSource _currentSource;

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

    private static void ShowInternal(Canvas canvas, RectTransform target, ScriptableObject asset, SkillRuntime runtime, ISkillTooltipSource source)
    {
        if (UiDragState.IsDragging || canvas == null || target == null || asset == null)
        {
            HideCurrent();
            return;
        }

        SkillTooltipUI tooltip = GetOrCreate(canvas);
        tooltip._currentSource = source;
        tooltip.ShowInternal(target, asset, runtime);
    }

    public static void HideCurrent()
    {
        if (_instance != null && _instance._root != null)
        {
            _instance._root.gameObject.SetActive(false);
            _instance._currentSource = null;
        }
    }

    private static SkillTooltipUI GetOrCreate(Canvas canvas)
    {
        if (_instance != null)
        {
            _instance.BindCanvas(canvas);
            return _instance;
        }

        GameObject go = new GameObject("SkillTooltip", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(SkillTooltipUI));
        _instance = go.GetComponent<SkillTooltipUI>();
        _instance.Initialize(canvas);
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

    private void Initialize(Canvas canvas)
    {
        _root = transform as RectTransform;
        Image image = GetComponent<Image>();
        image.color = new Color(0.075f, 0.085f, 0.11f, 0.97f);
        image.raycastTarget = false;

        VerticalLayoutGroup layout = GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.spacing = 7f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _title = CreateText("Title", 19f, FontStyles.Bold);
        _body = CreateText("Body", 14f, FontStyles.Normal);
        _body.color = new Color(0.91f, 0.93f, 0.96f, 1f);

        _root.pivot = new Vector2(0f, 1f);
        _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 0.5f);
        _root.sizeDelta = new Vector2(330f, 120f);

        BindCanvas(canvas);
        _root.gameObject.SetActive(false);
    }

    private TMP_Text CreateText(string objectName, float fontSize, FontStyles style)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        RectTransform rt = go.transform as RectTransform;
        rt.SetParent(transform, false);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.enableWordWrapping = true;
        text.richText = true;
        text.raycastTarget = false;

        LayoutElement layout = go.GetComponent<LayoutElement>();
        layout.preferredWidth = 306f;

        return text;
    }

    private void BindCanvas(Canvas canvas)
    {
        if (canvas == null)
            return;

        _canvas = canvas;
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

        LayoutRebuilder.ForceRebuildLayoutImmediate(_root);
        PositionNear(target);
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

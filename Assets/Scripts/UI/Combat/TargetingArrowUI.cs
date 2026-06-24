using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class TargetingArrowUI : MonoBehaviour
{
    private const string RootName = "TargetingArrowRoot";
    private const float DefaultSegmentSpacing = 26f;
    private const float DefaultArcHeight = 150f;
    private const float DefaultSegmentWidth = 18f;
    private const float DefaultSegmentHeight = 24f;
    private const float DefaultArrowHeadSize = 42f;
    private const int DefaultPoolSize = 24;

    private static TargetingArrowUI _instance;

    [SerializeField] private Color segmentColor = new Color(0.9f, 0.14f, 0.18f, 0.95f);
    [SerializeField] private float segmentSpacing = DefaultSegmentSpacing;
    [SerializeField] private float arcHeight = DefaultArcHeight;
    [SerializeField] private Vector2 segmentSize = new Vector2(DefaultSegmentWidth, DefaultSegmentHeight);
    [SerializeField] private Vector2 arrowHeadSize = new Vector2(DefaultArrowHeadSize, DefaultArrowHeadSize);
    [SerializeField] private int initialPoolSize = DefaultPoolSize;

    private Canvas _overlayCanvas;
    private RectTransform _overlayRect;
    private RectTransform _root;
    private Camera _overlayCamera;
    private readonly List<Image> _segments = new List<Image>();
    private readonly List<Image> _activeSegments = new List<Image>();
    private Image _arrowHead;
    private DraggableSkillIcon _selectedSkill;
    private Transform _worldTarget;
    private Vector2 _lastStartScreen;
    private Vector2 _lastEndScreen;
    private int _lastDrawStyleSignature;
    private bool _hasLastDraw;
    private bool _visible;

    public static void EnsureFor(DraggableSkillIcon skill)
    {
        if (skill == null)
            return;

        Canvas sourceCanvas = null;
        RectTransform target = null;
        ScriptableObject asset = null;
        SkillRuntime runtime = null;
        if (!skill.TryGetSkillTooltip(out sourceCanvas, out target, out asset, out runtime))
            return;

        if (_instance == null)
            CreateInstance(sourceCanvas);

        if (_instance == null)
            return;

        _instance.RefreshSelectedSkill(skill, asset, runtime);
    }

    public static void RefreshFromSelection()
    {
        if (_instance == null)
            return;

        _instance.HandleSelectedSkillChanged();
    }

    public static void SetWorldTarget(Transform target)
    {
        if (_instance == null || !_instance._visible)
            return;

        if (_instance._worldTarget != target)
            _instance._hasLastDraw = false;

        _instance._worldTarget = target;
    }

    public static void ClearWorldTarget()
    {
        if (_instance == null)
            return;

        _instance._worldTarget = null;
    }

    public static void Hide()
    {
        if (_instance == null)
            return;

        _instance.HideInternal();
    }

    private static void CreateInstance(Canvas sourceCanvas)
    {
        Canvas overlayCanvas = SkillTooltipUI.GetOrCreateSharedOverlayCanvas(sourceCanvas);
        if (overlayCanvas == null)
            return;

        GameObject rootGo = new GameObject(RootName, typeof(RectTransform), typeof(TargetingArrowUI));
        rootGo.transform.SetParent(overlayCanvas.transform, false);
        _instance = rootGo.GetComponent<TargetingArrowUI>();
        _instance.Initialize(overlayCanvas);
    }

    private void Initialize(Canvas overlayCanvas)
    {
        _overlayCanvas = overlayCanvas;
        _overlayRect = overlayCanvas.transform as RectTransform;
        _overlayCamera = overlayCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : overlayCanvas.worldCamera;
        _root = transform as RectTransform;
        if (_root != null)
        {
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;
            PositionRootNearTooltipHoverBridge();
        }

        EnsurePool(Mathf.Max(4, initialPoolSize));
        EnsureArrowHead();
        HideInternal();
        UiDragState.SelectedSkillChanged += HandleSelectedSkillChanged;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;

        UiDragState.SelectedSkillChanged -= HandleSelectedSkillChanged;
    }

    private void Update()
    {
        if (!_visible || _selectedSkill == null || _overlayRect == null)
            return;

        if (!TryGetSkillRuntime(_selectedSkill, out ScriptableObject asset, out SkillRuntime runtime) ||
            !ShouldShowForRuntime(runtime))
        {
            HideInternal();
            return;
        }

        if (!TryGetSkillScreenPoint(_selectedSkill, out Vector2 startScreen) ||
            !TryGetTargetScreenPoint(out Vector2 endScreen))
        {
            HideInternal();
            return;
        }

        int styleSignature = BuildDrawStyleSignature();
        if (_hasLastDraw &&
            styleSignature == _lastDrawStyleSignature &&
            (startScreen - _lastStartScreen).sqrMagnitude < 0.01f &&
            (endScreen - _lastEndScreen).sqrMagnitude < 0.01f)
        {
            return;
        }

        _hasLastDraw = true;
        _lastStartScreen = startScreen;
        _lastEndScreen = endScreen;
        _lastDrawStyleSignature = styleSignature;
        DrawArrow(startScreen, endScreen);
    }

    private int BuildDrawStyleSignature()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + segmentColor.GetHashCode();
            hash = hash * 31 + segmentSpacing.GetHashCode();
            hash = hash * 31 + arcHeight.GetHashCode();
            hash = hash * 31 + segmentSize.GetHashCode();
            hash = hash * 31 + arrowHeadSize.GetHashCode();
            return hash;
        }
    }

    private void HandleSelectedSkillChanged()
    {
        DraggableSkillIcon selected = UiDragState.SelectedSkill;
        if (selected == null)
        {
            HideInternal();
            return;
        }

        EnsureFor(selected);
    }

    private void RefreshSelectedSkill(DraggableSkillIcon skill, ScriptableObject asset, SkillRuntime runtime)
    {
        if (_selectedSkill != skill)
            _hasLastDraw = false;

        _selectedSkill = skill;
        _worldTarget = null;

        if (!ShouldShowForRuntime(runtime))
        {
            HideInternal();
            return;
        }

        Show();
    }

    private void Show()
    {
        _visible = true;
        if (_root != null)
            _root.gameObject.SetActive(true);
    }

    private void HideInternal()
    {
        _visible = false;
        _worldTarget = null;
        _selectedSkill = null;
        _hasLastDraw = false;
        ReleaseSegments();
        if (_arrowHead != null)
            _arrowHead.gameObject.SetActive(false);
        if (_root != null)
            _root.gameObject.SetActive(false);
    }

    private void PositionRootNearTooltipHoverBridge()
    {
        if (_root == null || _overlayCanvas == null)
            return;

        Transform hoverBridge = _overlayCanvas.transform.Find("SkillTooltipHoverBridge");
        if (hoverBridge != null)
        {
            int siblingIndex = hoverBridge.GetSiblingIndex();
            _root.SetSiblingIndex(Mathf.Max(0, siblingIndex));
            return;
        }

        _root.SetAsFirstSibling();
    }

    private bool TryGetSkillRuntime(DraggableSkillIcon skill, out ScriptableObject asset, out SkillRuntime runtime)
    {
        asset = null;
        runtime = null;
        if (skill == null)
            return false;

        Canvas canvas;
        RectTransform target;
        return skill.TryGetSkillTooltip(out canvas, out target, out asset, out runtime) && asset != null;
    }

    private static bool ShouldShowForRuntime(SkillRuntime runtime)
    {
        if (runtime == null)
            return false;

        if (!runtime.useV2Targeting)
            return runtime.target == TargetRule.Enemy;

        switch (runtime.targetRuleV2)
        {
            case SkillTargetRule.SingleEnemy:
            case SkillTargetRule.RowEnemies:
            case SkillTargetRule.AllEnemies:
                return true;
            default:
                return false;
        }
    }

    private bool TryGetSkillScreenPoint(DraggableSkillIcon skill, out Vector2 screenPoint)
    {
        screenPoint = default;
        if (skill == null)
            return false;

        RectTransform rect = skill.transform as RectTransform;
        if (rect == null)
            return false;

        Vector3 worldPoint = rect.TransformPoint(Vector3.zero);
        Canvas skillCanvas = skill.GetComponentInParent<Canvas>();
        Camera skillCamera = skillCanvas != null && skillCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? skillCanvas.worldCamera
            : null;
        screenPoint = RectTransformUtility.WorldToScreenPoint(skillCamera, worldPoint);
        return true;
    }

    private bool TryGetTargetScreenPoint(out Vector2 screenPoint)
    {
        if (_worldTarget != null)
            return TryGetTransformScreenPoint(_worldTarget, out screenPoint);

        screenPoint = Input.mousePosition;
        return true;
    }

    private bool TryGetTransformScreenPoint(Transform target, out Vector2 screenPoint)
    {
        screenPoint = default;
        if (target == null)
            return false;

        Vector3 worldPoint = target.position;
        Collider2D collider2D = target.GetComponentInChildren<Collider2D>();
        if (collider2D != null)
            worldPoint = collider2D.bounds.center;
        else
        {
            Renderer renderer = target.GetComponentInChildren<Renderer>();
            if (renderer != null)
                worldPoint = renderer.bounds.center;
        }

        Camera worldCamera = Camera.main;
        if (worldCamera == null)
            return false;

        screenPoint = RectTransformUtility.WorldToScreenPoint(worldCamera, worldPoint);
        return true;
    }

    private void DrawArrow(Vector2 startScreen, Vector2 endScreen)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_overlayRect, startScreen, _overlayCamera, out Vector2 start) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(_overlayRect, endScreen, _overlayCamera, out Vector2 end))
            return;

        Vector2 mid = (start + end) * 0.5f;
        float lift = Mathf.Max(arcHeight, Mathf.Abs(end.x - start.x) * 0.18f);
        Vector2 control = mid + Vector2.up * lift;

        float approxLength = EstimateCurveLength(start, control, end, 18);
        int segmentCount = Mathf.Max(3, Mathf.CeilToInt(approxLength / Mathf.Max(8f, segmentSpacing)));
        EnsurePool(segmentCount);
        ReleaseSegments();

        for (int i = 0; i < segmentCount; i++)
        {
            float t = segmentCount == 1 ? 1f : i / (float)(segmentCount - 1);
            Vector2 point = EvaluateQuadratic(start, control, end, t);
            Vector2 tangent = EvaluateQuadraticTangent(start, control, end, t);
            float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg - 90f;

            Image segment = _segments[i];
            RectTransform rect = segment.rectTransform;
            rect.anchoredPosition = point;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
            rect.sizeDelta = segmentSize;
            segment.color = segmentColor;
            segment.gameObject.SetActive(true);
            _activeSegments.Add(segment);
        }

        EnsureArrowHead();
        Vector2 headTangent = EvaluateQuadraticTangent(start, control, end, 1f);
        float headAngle = Mathf.Atan2(headTangent.y, headTangent.x) * Mathf.Rad2Deg - 90f;
        RectTransform headRect = _arrowHead.rectTransform;
        headRect.anchoredPosition = end;
        headRect.localRotation = Quaternion.Euler(0f, 0f, headAngle);
        headRect.sizeDelta = arrowHeadSize;
        _arrowHead.color = segmentColor;
        _arrowHead.gameObject.SetActive(true);
    }

    private void EnsurePool(int count)
    {
        while (_segments.Count < count)
        {
            Image image = CreateSegment($"Segment_{_segments.Count + 1}");
            image.gameObject.SetActive(false);
            _segments.Add(image);
        }
    }

    private void EnsureArrowHead()
    {
        if (_arrowHead != null)
            return;

        _arrowHead = CreateSegment("ArrowHead");
        _arrowHead.sprite = null;
        _arrowHead.type = Image.Type.Simple;
        _arrowHead.gameObject.SetActive(false);
    }

    private Image CreateSegment(string objectName)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(_root, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Image image = go.GetComponent<Image>();
        image.raycastTarget = false;
        image.color = segmentColor;
        image.sprite = BuildSprite(objectName == "ArrowHead");
        image.SetNativeSize();
        return image;
    }

    private Sprite BuildSprite(bool arrowHead)
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0f, 0f, 0f, 0f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, clear);
            }
        }

        if (arrowHead)
        {
            for (int y = 0; y < size; y++)
            {
                float normalizedY = y / (float)(size - 1);
                int halfWidth = Mathf.CeilToInt(Mathf.Lerp(size * 0.08f, size * 0.45f, normalizedY));
                int center = size / 2;
                for (int x = center - halfWidth; x <= center + halfWidth; x++)
                {
                    if (x >= 0 && x < size)
                        texture.SetPixel(x, y, Color.white);
                }
            }
        }
        else
        {
            Vector2 center = new Vector2(size * 0.5f, size * 0.58f);
            float radius = size * 0.38f;
            Vector2 tip = new Vector2(size * 0.5f, size * 0.08f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x, y);
                    if (Vector2.Distance(point, center) <= radius && y >= size * 0.32f)
                    {
                        texture.SetPixel(x, y, Color.white);
                        continue;
                    }

                    if (IsPointInTriangle(point, new Vector2(size * 0.2f, size * 0.5f), new Vector2(size * 0.8f, size * 0.5f), tip))
                        texture.SetPixel(x, y, Color.white);
                }
            }
        }

        texture.Apply();
        Rect rect = new Rect(0f, 0f, size, size);
        return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), size);
    }

    private static bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float area = Sign(p, a, b);
        float area2 = Sign(p, b, c);
        float area3 = Sign(p, c, a);
        bool hasNeg = area < 0f || area2 < 0f || area3 < 0f;
        bool hasPos = area > 0f || area2 > 0f || area3 > 0f;
        return !(hasNeg && hasPos);
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        => (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);

    private void ReleaseSegments()
    {
        for (int i = 0; i < _activeSegments.Count; i++)
        {
            if (_activeSegments[i] != null)
                _activeSegments[i].gameObject.SetActive(false);
        }

        _activeSegments.Clear();
    }

    private static Vector2 EvaluateQuadratic(Vector2 start, Vector2 control, Vector2 end, float t)
    {
        float omt = 1f - t;
        return omt * omt * start + 2f * omt * t * control + t * t * end;
    }

    private static Vector2 EvaluateQuadraticTangent(Vector2 start, Vector2 control, Vector2 end, float t)
        => 2f * (1f - t) * (control - start) + 2f * t * (end - control);

    private static float EstimateCurveLength(Vector2 start, Vector2 control, Vector2 end, int steps)
    {
        float length = 0f;
        Vector2 prev = start;
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector2 point = EvaluateQuadratic(start, control, end, t);
            length += Vector2.Distance(prev, point);
            prev = point;
        }

        return length;
    }
}

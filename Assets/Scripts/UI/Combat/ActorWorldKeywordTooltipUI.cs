using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ActorWorldKeywordTooltipUI : MonoBehaviour
{
    private const string SharedTooltipHostCanvasName = "GameplayDiceEditPanelCanvas";
    private const string OverlayCanvasName = "ActorWorldKeywordTooltipOverlayCanvas";
    private const float HorizontalPadding = 8f;
    private const float VerticalPadding = 8f;
    private const float TooltipGap = 10f;
    private const float TooltipStackGap = 8f;
    private const float TooltipColumnGap = 130f;
    private const float TooltipMinWidth = 100f;
    private const float TooltipMaxWidth = 120f;
    private const float TooltipTextMaxWidth = 200f;

    public readonly struct TooltipContent
    {
        public readonly string title;
        public readonly string description;
        public readonly Sprite icon;

        public TooltipContent(string title, string description, Sprite icon)
        {
            this.title = title ?? string.Empty;
            this.description = description ?? string.Empty;
            this.icon = icon;
        }
    }

    private sealed class TooltipView
    {
        public RectTransform root;
        public Image icon;
        public TMP_Text title;
        public TMP_Text body;
        public LayoutElement titleLayout;
        public LayoutElement bodyLayout;
        public string contentSignature;
    }

    private static ActorWorldKeywordTooltipUI _instance;
    private static SkillTooltipPrefabSettingsSO _prefabSettings;

    private readonly List<TooltipView> _views = new List<TooltipView>();
    private RectTransform _canvasRect;
    private Camera _uiCamera;
    private Component _currentOwner;

    public static void Show(
        Canvas sourceCanvas,
        RectTransform tooltipAnchor,
        RectTransform tooltipBottomLimit,
        RectTransform hoverTarget,
        Camera targetCamera,
        IReadOnlyList<TooltipContent> contents,
        Component owner,
        bool preferRight)
    {
        if (sourceCanvas == null || hoverTarget == null || contents == null || contents.Count <= 0 || owner == null)
        {
            Hide();
            return;
        }

        ActorWorldKeywordTooltipUI instance = GetOrCreate(sourceCanvas);
        if (instance == null)
            return;

        instance.ShowInternal(tooltipAnchor, tooltipBottomLimit, hoverTarget, targetCamera, contents, owner, preferRight);
    }

    public static void Hide()
    {
        if (_instance == null)
            return;

        for (int i = 0; i < _instance._views.Count; i++)
        {
            if (_instance._views[i]?.root != null)
                _instance._views[i].root.gameObject.SetActive(false);
        }

        _instance._currentOwner = null;
    }

    public static bool IsShowingFor(Component owner)
    {
        return owner != null &&
               _instance != null &&
               _instance._currentOwner == owner;
    }

    private static ActorWorldKeywordTooltipUI GetOrCreate(Canvas sourceCanvas)
    {
        Canvas overlayCanvas = GetOrCreateOverlayCanvas(sourceCanvas);
        if (overlayCanvas == null)
            return null;

        if (_instance != null)
        {
            _instance.BindCanvas(overlayCanvas);
            return _instance;
        }

        ActorWorldKeywordTooltipUI existing = overlayCanvas.GetComponentInChildren<ActorWorldKeywordTooltipUI>(true);
        if (existing != null)
        {
            _instance = existing;
            _instance.BindCanvas(overlayCanvas);
            return _instance;
        }

        GameObject go = new GameObject("ActorWorldKeywordTooltipUI", typeof(RectTransform));
        go.transform.SetParent(overlayCanvas.transform, false);
        _instance = go.AddComponent<ActorWorldKeywordTooltipUI>();
        _instance.BindCanvas(overlayCanvas);
        return _instance;
    }

    private void BindCanvas(Canvas canvas)
    {
        _canvasRect = canvas.transform as RectTransform;
        _uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }

    private void ShowInternal(
        RectTransform tooltipAnchor,
        RectTransform tooltipBottomLimit,
        RectTransform hoverTarget,
        Camera targetCamera,
        IReadOnlyList<TooltipContent> contents,
        Component owner,
        bool preferRight)
    {
        _currentOwner = owner;

        Rect hoverRect = GetScreenRect(hoverTarget, targetCamera);
        bool usingExplicitAnchor = tooltipAnchor != null;
        Vector2 anchorPoint = usingExplicitAnchor
            ? GetAnchorScreenPoint(tooltipAnchor, targetCamera)
            : new Vector2(hoverRect.xMax + TooltipGap, hoverRect.yMax);
        Vector2 mirroredAnchorPoint = usingExplicitAnchor
            ? GetMirroredAnchorScreenPoint(tooltipAnchor, targetCamera, anchorPoint)
            : new Vector2(hoverRect.xMin, hoverRect.yMax);
        float columnTopY = preferRight ? anchorPoint.y : mirroredAnchorPoint.y;
        float currentTopY = columnTopY;
        float anchorX = preferRight ? anchorPoint.x : mirroredAnchorPoint.x;
        float bottomLimitY = GetBottomLimitScreenY(tooltipBottomLimit, targetCamera);
        float parentLeftX = hoverRect.xMin;
        bool placeRight = preferRight;

        for (int i = 0; i < contents.Count; i++)
        {
            TooltipView view = EnsureView(i);
            if (view == null || view.root == null)
                continue;

            string contentSignature = BuildContentSignature(contents[i]);
            bool wasActive = view.root.gameObject.activeSelf;
            bool contentChanged = view.contentSignature != contentSignature;
            if (contentChanged)
            {
                ApplyContent(view, contents[i]);
                view.contentSignature = contentSignature;
            }

            ApplyWidthConstraints(view);
            CombatUiDirtySetUtility.SetActiveIfChanged(view.root.gameObject, true);
            if (contentChanged || !wasActive)
                LayoutRebuilder.ForceRebuildLayoutImmediate(view.root);

            float width = Mathf.Clamp(view.root.rect.width, TooltipMinWidth, TooltipMaxWidth);
            float height = Mathf.Max(10f, view.root.rect.height);
            view.root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            if (contentChanged || !wasActive)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(view.root);
                height = Mathf.Max(10f, view.root.rect.height);
            }

            float preferredLeftX = placeRight
                ? anchorX
                : anchorX - TooltipGap - width;
            float topY = currentTopY;

            if (bottomLimitY > 0f && topY - height < bottomLimitY)
            {
                float columnShift = width + TooltipColumnGap;
                anchorX += placeRight ? columnShift : -columnShift;
                topY = columnTopY;
                preferredLeftX = placeRight
                    ? anchorX
                    : anchorX - TooltipGap - width;
            }

            bool overflowsRight = preferredLeftX + width > Screen.width - HorizontalPadding;
            bool overflowsLeft = preferredLeftX < HorizontalPadding;
            if (placeRight && overflowsRight)
            {
                placeRight = false;
                anchorX = usingExplicitAnchor ? mirroredAnchorPoint.x : parentLeftX;
                columnTopY = usingExplicitAnchor ? mirroredAnchorPoint.y : columnTopY;
                preferredLeftX = usingExplicitAnchor
                    ? mirroredAnchorPoint.x - TooltipGap - width
                    : parentLeftX - TooltipGap - width;
                topY = columnTopY;
            }
            else if (!placeRight && overflowsLeft)
            {
                placeRight = true;
                anchorX = usingExplicitAnchor ? anchorPoint.x : hoverRect.xMax + TooltipGap;
                columnTopY = usingExplicitAnchor ? anchorPoint.y : columnTopY;
                preferredLeftX = anchorX;
                topY = columnTopY;
            }

            float leftX = preferredLeftX;
            float clampedLeftX = Mathf.Clamp(leftX, HorizontalPadding, Screen.width - HorizontalPadding - width);
            float clampedTopY = Mathf.Clamp(topY, height + VerticalPadding, Screen.height - VerticalPadding);
            float screenAnchorX = placeRight ? clampedLeftX : clampedLeftX + width;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect,
                    new Vector2(screenAnchorX, clampedTopY),
                    _uiCamera,
                    out Vector2 localTopLeft))
            {
                view.root.anchorMin = new Vector2(0.5f, 0.5f);
                view.root.anchorMax = new Vector2(0.5f, 0.5f);
                view.root.pivot = placeRight ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
                view.root.anchoredPosition = localTopLeft;
                view.root.SetAsLastSibling();
            }

            currentTopY = clampedTopY - height - TooltipStackGap;
        }

        for (int i = contents.Count; i < _views.Count; i++)
        {
            if (_views[i]?.root != null)
                CombatUiDirtySetUtility.SetActiveIfChanged(_views[i].root.gameObject, false);
        }
    }

    private TooltipView EnsureView(int index)
    {
        while (_views.Count <= index)
        {
            TooltipView created = CreateView();
            if (created == null)
                return null;

            _views.Add(created);
        }

        return _views[index];
    }

    private TooltipView CreateView()
    {
        SkillTooltipKeywordTooltipTemplate prefab = GetKeywordTooltipPrefab();
        if (prefab == null || prefab.RectTransform == null)
            return null;

        SkillTooltipKeywordTooltipTemplate instance = Instantiate(prefab, transform);
        RectTransform root = instance.RectTransform;
        root.gameObject.SetActive(false);

        Image icon = instance.IconImage;
        if (icon != null)
            icon.preserveAspect = true;

        return new TooltipView
        {
            root = root,
            icon = icon,
            title = instance.TitleText,
            body = instance.BodyText,
            titleLayout = instance.TitleText != null ? EnsureLayoutElement(instance.TitleText) : null,
            bodyLayout = instance.BodyText != null ? EnsureLayoutElement(instance.BodyText) : null
        };
    }

    private static SkillTooltipKeywordTooltipTemplate GetKeywordTooltipPrefab()
    {
        SkillTooltipPrefabProvider provider = SkillTooltipPrefabProviderRegistry.Get();
        if (provider != null && provider.KeywordTooltipPrefab != null)
            return provider.KeywordTooltipPrefab;

        SkillTooltipPrefabSettingsSO settings = GetPrefabSettings();
        return settings != null ? settings.KeywordTooltipPrefab : null;
    }

    private static SkillTooltipPrefabSettingsSO GetPrefabSettings()
    {
        if (_prefabSettings == null)
            _prefabSettings = Resources.Load<SkillTooltipPrefabSettingsSO>("UI/SkillTooltipPrefabSettings");

        return _prefabSettings;
    }

    private static Canvas GetOrCreateOverlayCanvas(Canvas sourceCanvas)
    {
        Canvas sharedHost = FindSharedTooltipHostCanvas();
        if (sharedHost != null)
            return sharedHost;

        Canvas existing = SceneCanvasLookup.FindByName(OverlayCanvasName);
        if (existing != null)
            return existing;

        GameObject canvasGo = new GameObject(
            OverlayCanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas overlayCanvas = canvasGo.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.worldCamera = null;
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = short.MaxValue - 1;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (sourceCanvas != null)
            overlayCanvas.gameObject.layer = sourceCanvas.gameObject.layer;

        return overlayCanvas;
    }

    private static Canvas FindSharedTooltipHostCanvas()
    {
        return SceneCanvasLookup.FindByName(SharedTooltipHostCanvasName);
    }

    private static string BuildContentSignature(TooltipContent content)
    {
        int iconId = content.icon != null ? content.icon.GetInstanceID() : 0;
        return $"{content.title}\u001f{content.description}\u001f{iconId}";
    }

    private static void ApplyContent(TooltipView view, TooltipContent content)
    {
        if (view.title != null)
            CombatUiDirtySetUtility.SetTextIfChanged(view.title, content.title);

        if (view.body != null)
            CombatUiDirtySetUtility.SetTextIfChanged(view.body, content.description);

        if (view.icon != null)
        {
            if (view.icon.sprite != content.icon)
                view.icon.sprite = content.icon;
            bool showIcon = content.icon != null;
            if (view.icon.enabled != showIcon)
                view.icon.enabled = showIcon;
            CombatUiDirtySetUtility.SetColorIfChanged(view.icon, showIcon ? Color.white : new Color(1f, 1f, 1f, 0f));
        }
    }

    private static void ApplyWidthConstraints(TooltipView view)
    {
        ApplyTextWidthConstraint(view.title, view.titleLayout);
        ApplyTextWidthConstraint(view.body, view.bodyLayout);
    }

    private static void ApplyTextWidthConstraint(TMP_Text text, LayoutElement layout)
    {
        if (text == null || layout == null)
            return;

        text.textWrappingMode = TextWrappingModes.Normal;
        layout.preferredWidth = TooltipTextMaxWidth;
        layout.flexibleWidth = 0f;
    }

    private static LayoutElement EnsureLayoutElement(Component component)
    {
        if (component == null)
            return null;

        LayoutElement layout = component.GetComponent<LayoutElement>();
        if (layout == null)
            layout = component.gameObject.AddComponent<LayoutElement>();

        return layout;
    }

    private static Rect GetScreenRect(RectTransform rect, Camera camera)
    {
        if (rect == null)
            return default;

        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 max = min;
        for (int i = 1; i < corners.Length; i++)
        {
            Vector2 screenCorner = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
            min = Vector2.Min(min, screenCorner);
            max = Vector2.Max(max, screenCorner);
        }

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private static Vector2 GetAnchorScreenPoint(RectTransform tooltipAnchor, Camera camera)
    {
        if (tooltipAnchor == null)
            return default;

        return RectTransformUtility.WorldToScreenPoint(camera, tooltipAnchor.position);
    }

    private static Vector2 GetMirroredAnchorScreenPoint(RectTransform tooltipAnchor, Camera camera, Vector2 fallbackPoint)
    {
        if (tooltipAnchor == null || tooltipAnchor.parent == null)
            return fallbackPoint;

        RectTransform parentRect = tooltipAnchor.parent as RectTransform;
        if (parentRect == null)
            return fallbackPoint;

        Vector2 anchorScreenPoint = RectTransformUtility.WorldToScreenPoint(camera, tooltipAnchor.position);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, anchorScreenPoint, camera, out Vector2 localPoint))
            return fallbackPoint;

        localPoint.x = -localPoint.x;
        Vector3 mirroredWorld = parentRect.TransformPoint(new Vector3(localPoint.x, localPoint.y, 0f));
        return RectTransformUtility.WorldToScreenPoint(camera, mirroredWorld);
    }

    private static float GetBottomLimitScreenY(RectTransform tooltipBottomLimit, Camera camera)
    {
        if (tooltipBottomLimit == null)
            return 0f;

        return RectTransformUtility.WorldToScreenPoint(camera, tooltipBottomLimit.position).y;
    }
}

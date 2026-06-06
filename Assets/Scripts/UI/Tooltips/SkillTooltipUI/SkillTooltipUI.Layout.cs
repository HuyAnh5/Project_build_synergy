using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Handles binding prefab children, normalizing text layout, and applying formatted tooltip content.
// Static show/hide ownership stays in SkillTooltipUI.cs.
public sealed partial class SkillTooltipUI
{
    /// <summary>Initializes child references from the prefab or creates missing text/icon nodes.</summary>
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
            verticalLayout.enabled = false;
        }

        ContentSizeFitter fitter = _layout != null ? _layout.ContentSizeFitter : GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            fitter.enabled = false;
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

        text.textWrappingMode = TextWrappingModes.Normal;
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

    /// <summary>Applies formatted tooltip content to the active layout and refreshes size/position.</summary>
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
        string contentSignature = BuildContentSignature(content, expanded);
        bool contentChanged = !string.Equals(_lastContentSignature, contentSignature, StringComparison.Ordinal);
        ApplyContent(content);
        _root.gameObject.SetActive(true);
        _root.SetAsLastSibling();
        if (_hoverBridge != null)
            _hoverBridge.gameObject.SetActive(true);

        ApplyDynamicSizing();
        if (contentChanged)
            _lastContentSignature = contentSignature;

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
        ApplyManualTextLayout(contentWidth, horizontalPadding, verticalPadding, spacing);
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
        total += GetTextPreferredHeight(_requiresHeader, contentWidth);
        total += GetTextPreferredHeight(_requires, contentWidth);
        total += GetTextPreferredHeight(_effect, contentWidth);
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

    private void ApplyManualTextLayout(float contentWidth, float horizontalPadding, float verticalPadding, float spacing)
    {
        if (_root == null)
            return;

        VerticalLayoutGroup layoutGroup = _layout != null ? _layout.VerticalLayout : GetComponent<VerticalLayoutGroup>();
        RectOffset padding = layoutGroup != null ? layoutGroup.padding : new RectOffset();
        float left = padding.left;
        float top = padding.top;
        float right = padding.right;
        float currentY = -top;

        TMP_Text[] blocks =
        {
            _title, _cost, _targeting, _requiresHeader, _requires, _effect, _conditionHeader, _condition
        };

        for (int i = 0; i < blocks.Length; i++)
        {
            TMP_Text block = blocks[i];
            if (!IsVisible(block))
                continue;

            RectTransform rect = block.rectTransform;
            float height = Mathf.Ceil(GetTextPreferredHeight(block, contentWidth));
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, currentY);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            currentY -= height + spacing;
        }

        if (_elementIcon != null)
        {
            RectTransform iconRect = _elementIcon.rectTransform;
            iconRect.anchorMin = new Vector2(1f, 1f);
            iconRect.anchorMax = new Vector2(1f, 1f);
            iconRect.pivot = new Vector2(1f, 1f);
            iconRect.anchoredPosition = new Vector2(-right, -top);
        }
    }

    private static string BuildContentSignature(SkillTooltipFormatter.TooltipContent content, bool expanded)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder(256);
        sb.Append(expanded ? '1' : '0').Append('|');
        sb.Append(content.title).Append('|');
        sb.Append(content.costText).Append('|');
        sb.Append(content.targeting).Append('|');
        sb.Append(content.effectText).Append('|');
        AppendLines(sb, content.requires);
        sb.Append('|');
        AppendLines(sb, content.conditions);
        sb.Append('|');
        sb.Append(content.element.HasValue ? ((int)content.element.Value).ToString() : "none");
        return sb.ToString();
    }

    private static void AppendLines(System.Text.StringBuilder sb, List<string> lines)
    {
        if (lines == null)
            return;

        for (int i = 0; i < lines.Count; i++)
        {
            sb.Append(lines[i]).Append('\n');
        }
    }

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
        ApplyBlockSiblingOrder();

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

    private void ApplyBlockSiblingOrder()
    {
        SetSiblingIndex(_title, 0);
        SetSiblingIndex(_cost, 1);
        SetSiblingIndex(_targeting, 2);
        SetSiblingIndex(_requiresHeader, 3);
        SetSiblingIndex(_requires, 4);
        SetSiblingIndex(_effect, 5);
        SetSiblingIndex(_conditionHeader, 6);
        SetSiblingIndex(_condition, 7);
    }

    private static void SetSiblingIndex(Component component, int index)
    {
        if (component == null)
            return;

        component.transform.SetSiblingIndex(index);
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
        ActorWorldUI[] worldUis = FindObjectsByType<ActorWorldUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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
        text.textWrappingMode = TextWrappingModes.Normal;
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
}

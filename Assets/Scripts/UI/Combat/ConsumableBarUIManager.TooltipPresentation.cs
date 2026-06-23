using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class ConsumableBarUIManager
{
    private const string TooltipPrefabSettingsResourcePath = "UI/SkillTooltipPrefabSettings";
    private const float ConsumableKeywordTooltipGap = 10f;
    private const float ConsumableKeywordTooltipStackGap = 8f;
    private const float ConsumableKeywordTooltipMinWidth = 150f;
    private const float ConsumableKeywordTooltipMaxWidth = 240f;
    private static readonly Regex ConsumableRichTextTagRegex = new Regex("(<.*?>)", RegexOptions.Compiled);

    private sealed class ConsumableKeywordTooltipView
    {
        public RectTransform root;
        public Image background;
        public Image icon;
        public LayoutElement iconLayout;
        public TMP_Text title;
        public TMP_Text body;
        public VerticalLayoutGroup layout;
        public ContentSizeFitter fitter;
        public bool usesTemplate;
    }

    private struct ConsumableKeywordTooltipContent
    {
        public string keywordId;
        public string title;
        public string description;
        public Sprite icon;
    }

    private readonly List<ConsumableKeywordTooltipView> _consumableKeywordTooltipViews = new List<ConsumableKeywordTooltipView>();
    private string _activeConsumableKeywordId;
    private SkillTooltipKeywordGlossarySO _consumableKeywordGlossary;
    private SkillTooltipKeywordTooltipTemplate _consumableKeywordTooltipPrefab;
    private static SkillTooltipPrefabSettingsSO s_consumableTooltipPrefabSettings;

    private void PositionPanelAtAnchor(RectTransform panel, RectTransform anchor, Vector2 offset)
    {
        PositionPresentationAtAnchor(panel, anchor, offset);
    }

    private void PositionTooltipAtAnchor(RectTransform tooltip, RectTransform anchor, Vector2 offset)
    {
        PositionPresentationAtAnchor(tooltip, anchor, offset);
    }

    private static void PositionPresentationAtAnchor(RectTransform presentation, RectTransform anchor, Vector2 offset)
    {
        if (presentation == null || anchor == null)
            return;

        RectTransform anchorParent = anchor.parent as RectTransform;
        if (anchorParent == null)
        {
            presentation.position = anchor.position + (Vector3)offset;
            return;
        }

        if (presentation.parent != anchorParent)
            presentation.SetParent(anchorParent, worldPositionStays: false);

        EnsureIgnoreLayout(presentation);
        presentation.anchorMin = anchor.anchorMin;
        presentation.anchorMax = anchor.anchorMax;
        presentation.anchoredPosition = anchor.anchoredPosition + offset;
        presentation.localScale = Vector3.one;
    }

    private static void EnsureIgnoreLayout(RectTransform rect)
    {
        if (rect == null)
            return;

        LayoutElement layoutElement = rect.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = rect.gameObject.AddComponent<LayoutElement>();

        layoutElement.ignoreLayout = true;
    }

    private static void EnsureTooltipAutoSize(RectTransform tooltip, TMP_Text title, TMP_Text body)
    {
        if (tooltip == null)
            return;

        VerticalLayoutGroup layout = tooltip.GetComponent<VerticalLayoutGroup>();
        RectOffset padding = layout != null ? layout.padding : new RectOffset();
        float spacing = layout != null ? layout.spacing : 0f;
        if (layout != null)
            layout.enabled = false;

        ContentSizeFitter fitter = tooltip.GetComponent<ContentSizeFitter>();
        if (fitter != null)
            fitter.enabled = false;

        EnsureTooltipTextLayout(title);
        EnsureTooltipTextLayout(body);
        float contentWidth = Mathf.Max(120f, tooltip.rect.width - padding.left - padding.right);
        float titleHeight = GetPreferredHeight(title, contentWidth);
        float bodyHeight = GetPreferredHeight(body, contentWidth);

        LayoutTooltipBlock(title, padding.left, padding.top, contentWidth, titleHeight);
        float bodyTop = padding.top + titleHeight + (title != null && title.gameObject.activeSelf && body != null && body.gameObject.activeSelf ? spacing : 0f);
        LayoutTooltipBlock(body, padding.left, bodyTop, contentWidth, bodyHeight);

        float totalHeight = padding.top + padding.bottom;
        if (title != null && title.gameObject.activeSelf)
            totalHeight += titleHeight;
        if (body != null && body.gameObject.activeSelf)
        {
            if (title != null && title.gameObject.activeSelf)
                totalHeight += spacing;
            totalHeight += bodyHeight;
        }

        tooltip.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Ceil(totalHeight));
    }

    private static void EnsureTooltipTextLayout(TMP_Text text)
    {
        if (text == null)
            return;

        text.richText = true;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;

        LayoutElement layout = text.GetComponent<LayoutElement>();
        if (layout == null)
            layout = text.gameObject.AddComponent<LayoutElement>();

        layout.ignoreLayout = false;
        layout.flexibleHeight = 0f;
        layout.preferredHeight = -1f;
    }

    private static void LayoutTooltipBlock(TMP_Text text, float left, float top, float width, float height)
    {
        if (text == null)
            return;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(left, -top);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Ceil(height));
    }

    private static float GetPreferredHeight(TMP_Text text, float width)
    {
        if (text == null || !text.gameObject.activeSelf)
            return 0f;

        return text.GetPreferredValues(text.text ?? string.Empty, width, 0f).y;
    }

    private Vector2 GetPresentationOffset(bool isTooltip)
    {
        if (useExactManualAnchors)
            return isTooltip ? tooltipStableOffset : actionPanelStableOffset;

        return isTooltip ? tooltipOffset : actionPanelOffset;
    }

    private bool IsPointerOverTooltipPresentation(int slotIndex)
    {
        if (IsPointerOverAnyConsumableKeywordTooltip())
            return true;

        ConsumableSlotView slot = GetSlot(slotIndex);
        RectTransform source = GetSlotVisualTarget(slot);
        RectTransform tooltip = GetTooltipRootForSlot(slot);
        if (source == null || tooltip == null || !tooltip.gameObject.activeInHierarchy)
            return false;

        Canvas canvas = source.GetComponentInParent<Canvas>();
        Camera eventCamera = GetCanvasEventCamera(canvas);
        Vector2 screenPoint = Input.mousePosition;
        if (RectTransformUtility.RectangleContainsScreenPoint(source, screenPoint, eventCamera) ||
            RectTransformUtility.RectangleContainsScreenPoint(tooltip, screenPoint, eventCamera))
        {
            return true;
        }

        if (!TryBuildHoverBridgeScreenRect(source, tooltip, eventCamera, out Rect bridgeRect))
            return false;

        return bridgeRect.Contains(screenPoint);
    }

    private RectTransform GetTooltipRootForSlot(ConsumableSlotView slot)
    {
        if (slot != null && slot.localTooltipRoot != null)
            return slot.localTooltipRoot;

        return tooltipRoot;
    }

    private static bool TryBuildHoverBridgeScreenRect(RectTransform source, RectTransform tooltip, Camera eventCamera, out Rect bridgeRect)
    {
        bridgeRect = default;
        if (source == null || tooltip == null)
            return false;

        Rect sourceRect = GetScreenRect(source, eventCamera);
        Rect tooltipRect = GetScreenRect(tooltip, eventCamera);
        if (sourceRect.width <= 0f || sourceRect.height <= 0f ||
            tooltipRect.width <= 0f || tooltipRect.height <= 0f ||
            sourceRect.Overlaps(tooltipRect, true))
        {
            return false;
        }

        bool verticalGap = sourceRect.yMax < tooltipRect.yMin || tooltipRect.yMax < sourceRect.yMin;
        bool horizontalGap = sourceRect.xMax < tooltipRect.xMin || tooltipRect.xMax < sourceRect.xMin;

        if (verticalGap && !horizontalGap)
        {
            float xMin = Mathf.Max(sourceRect.xMin, tooltipRect.xMin);
            float xMax = Mathf.Min(sourceRect.xMax, tooltipRect.xMax);
            float yMin = Mathf.Min(sourceRect.yMax, tooltipRect.yMax);
            float yMax = Mathf.Max(sourceRect.yMin, tooltipRect.yMin);
            bridgeRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
        else if (horizontalGap && !verticalGap)
        {
            float xMin = Mathf.Min(sourceRect.xMax, tooltipRect.xMax);
            float xMax = Mathf.Max(sourceRect.xMin, tooltipRect.xMin);
            float yMin = Mathf.Max(sourceRect.yMin, tooltipRect.yMin);
            float yMax = Mathf.Min(sourceRect.yMax, tooltipRect.yMax);
            bridgeRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
        else
        {
            Vector2 sourcePoint = ClosestPointOnRect(sourceRect, tooltipRect.center);
            Vector2 tooltipPoint = ClosestPointOnRect(tooltipRect, sourcePoint);
            sourcePoint = ClosestPointOnRect(sourceRect, tooltipPoint);
            bridgeRect = Rect.MinMaxRect(
                Mathf.Min(sourcePoint.x, tooltipPoint.x),
                Mathf.Min(sourcePoint.y, tooltipPoint.y),
                Mathf.Max(sourcePoint.x, tooltipPoint.x),
                Mathf.Max(sourcePoint.y, tooltipPoint.y));
        }

        bridgeRect = ExpandThinRect(bridgeRect, 6f);
        return bridgeRect.width > 0f && bridgeRect.height > 0f;
    }

    private static Rect GetScreenRect(RectTransform rect, Camera camera)
    {
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

    private static Vector2 ClosestPointOnRect(Rect rect, Vector2 point)
    {
        return new Vector2(
            Mathf.Clamp(point.x, rect.xMin, rect.xMax),
            Mathf.Clamp(point.y, rect.yMin, rect.yMax));
    }

    private static Rect ExpandThinRect(Rect rect, float minThickness)
    {
        if (rect.width < minThickness)
        {
            float extra = (minThickness - rect.width) * 0.5f;
            rect.xMin -= extra;
            rect.xMax += extra;
        }

        if (rect.height < minThickness)
        {
            float extra = (minThickness - rect.height) * 0.5f;
            rect.yMin -= extra;
            rect.yMax += extra;
        }

        return rect;
    }

    private static Camera GetCanvasEventCamera(Canvas canvas)
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    private string ApplyConsumableKeywordMarkup(ConsumableDataSO data)
    {
        string body = BuildTooltipBody(data);
        if (string.IsNullOrEmpty(body))
            return string.Empty;

        EnsureConsumableKeywordResources();
        string[] segments = ConsumableRichTextTagRegex.Split(body);
        SkillTooltipKeywordGlossarySO.KeywordEntry[] entries =
            _consumableKeywordGlossary != null && _consumableKeywordGlossary.Entries != null
                ? (SkillTooltipKeywordGlossarySO.KeywordEntry[])_consumableKeywordGlossary.Entries.Clone()
                : Array.Empty<SkillTooltipKeywordGlossarySO.KeywordEntry>();
        if (entries.Length > 0)
            Array.Sort(entries, (a, b) => string.Compare(b.keyword, a.keyword, StringComparison.OrdinalIgnoreCase));

        for (int i = 0; i < segments.Length; i++)
        {
            if (string.IsNullOrEmpty(segments[i]) || segments[i][0] == '<')
                continue;

            string segment = segments[i];
            for (int entryIndex = 0; entryIndex < entries.Length; entryIndex++)
            {
                string keyword = entries[entryIndex].keyword;
                if (string.IsNullOrWhiteSpace(keyword))
                    continue;

                segment = Regex.Replace(
                    segment,
                    $@"(?<!\w){Regex.Escape(keyword)}(?!\w)",
                    match => $"<link=\"{keyword}\"><color=#FFD166>{match.Value}</color></link>",
                    RegexOptions.IgnoreCase);
            }

            foreach (DiceFaceEnchantKind enchant in Enum.GetValues(typeof(DiceFaceEnchantKind)))
            {
                if (!DiceFaceEnchantUtility.HasEnchant(enchant))
                    continue;

                string keyword = DiceFaceEnchantUtility.GetDisplayName(enchant);
                segment = Regex.Replace(
                    segment,
                    $@"(?<!\w){Regex.Escape(keyword)}(?!\w)",
                    match => $"<link=\"{keyword}\"><color=#FFD166>{match.Value}</color></link>",
                    RegexOptions.IgnoreCase);
            }

            segments[i] = segment;
        }

        return string.Concat(segments);
    }

    private void UpdateConsumableKeywordTooltips(RectTransform tooltipRootRect, TMP_Text bodyText)
    {
        if (tooltipRootRect == null || bodyText == null || !tooltipRootRect.gameObject.activeInHierarchy)
        {
            HideConsumableKeywordTooltips();
            return;
        }

        EnsureConsumableKeywordResources();
        bool showAll = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        List<ConsumableKeywordTooltipContent> contents = showAll
            ? CollectVisibleConsumableKeywordContents(bodyText)
            : CollectHoveredConsumableKeywordContents(bodyText);
        if (contents.Count == 0)
        {
            HideConsumableKeywordTooltips();
            if (!showAll && !IsPointerOverAnyConsumableKeywordTooltip())
                _activeConsumableKeywordId = null;
            return;
        }

        ShowConsumableKeywordTooltips(tooltipRootRect, contents);
    }

    private void EnsureConsumableKeywordResources()
    {
        if (_consumableKeywordGlossary != null && _consumableKeywordTooltipPrefab != null)
            return;

        if (s_consumableTooltipPrefabSettings == null)
            s_consumableTooltipPrefabSettings = Resources.Load<SkillTooltipPrefabSettingsSO>(TooltipPrefabSettingsResourcePath);

        GameObject skillTooltipPrefab = s_consumableTooltipPrefabSettings != null ? s_consumableTooltipPrefabSettings.SkillTooltipPrefab : null;
        SkillTooltipLayout layout = skillTooltipPrefab != null ? skillTooltipPrefab.GetComponent<SkillTooltipLayout>() : null;
        if (_consumableKeywordGlossary == null && layout != null)
            _consumableKeywordGlossary = layout.KeywordGlossary;
        if (_consumableKeywordTooltipPrefab == null)
        {
            _consumableKeywordTooltipPrefab = layout != null && layout.KeywordTooltipPrefab != null
                ? layout.KeywordTooltipPrefab
                : s_consumableTooltipPrefabSettings != null ? s_consumableTooltipPrefabSettings.KeywordTooltipPrefab : null;
        }
    }

    private List<ConsumableKeywordTooltipContent> CollectHoveredConsumableKeywordContents(TMP_Text bodyText)
    {
        List<ConsumableKeywordTooltipContent> results = new List<ConsumableKeywordTooltipContent>(1);
        if (TryGetHoveredConsumableKeywordContent(bodyText, out ConsumableKeywordTooltipContent hovered))
        {
            _activeConsumableKeywordId = hovered.keywordId;
            results.Add(hovered);
            return results;
        }

        if (!string.IsNullOrEmpty(_activeConsumableKeywordId) && IsPointerOverAnyConsumableKeywordTooltip())
        {
            if (TryBuildConsumableKeywordTooltipContent(_activeConsumableKeywordId, _activeConsumableKeywordId, out ConsumableKeywordTooltipContent persisted))
                results.Add(persisted);
        }

        return results;
    }

    private List<ConsumableKeywordTooltipContent> CollectVisibleConsumableKeywordContents(TMP_Text bodyText)
    {
        List<ConsumableKeywordTooltipContent> results = new List<ConsumableKeywordTooltipContent>();
        if (bodyText == null)
            return results;

        HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bodyText.ForceMeshUpdate();
        TMP_LinkInfo[] links = bodyText.textInfo.linkInfo;
        for (int linkIndex = 0; linkIndex < bodyText.textInfo.linkCount; linkIndex++)
        {
            string keywordId = links[linkIndex].GetLinkID();
            string linkText = links[linkIndex].GetLinkText();
            if (string.IsNullOrWhiteSpace(keywordId) || !seen.Add(keywordId))
                continue;

            if (TryBuildConsumableKeywordTooltipContent(keywordId, linkText, out ConsumableKeywordTooltipContent content))
                results.Add(content);
        }

        return results;
    }

    private bool TryGetHoveredConsumableKeywordContent(TMP_Text bodyText, out ConsumableKeywordTooltipContent content)
    {
        content = default;
        if (bodyText == null || !bodyText.gameObject.activeInHierarchy)
            return false;

        Canvas canvas = bodyText.GetComponentInParent<Canvas>();
        Camera eventCamera = GetCanvasEventCamera(canvas);
        bodyText.ForceMeshUpdate();
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(bodyText, Input.mousePosition, eventCamera);
        if (linkIndex < 0)
            return false;

        TMP_LinkInfo link = bodyText.textInfo.linkInfo[linkIndex];
        return TryBuildConsumableKeywordTooltipContent(link.GetLinkID(), link.GetLinkText(), out content);
    }

    private bool TryBuildConsumableKeywordTooltipContent(string keywordId, string currentValueText, out ConsumableKeywordTooltipContent content)
    {
        content = default;
        if (string.IsNullOrWhiteSpace(keywordId))
            return false;

        if (_consumableKeywordGlossary != null &&
            _consumableKeywordGlossary.TryGetEntry(keywordId, out SkillTooltipKeywordGlossarySO.KeywordEntry entry))
        {
            string description = _consumableKeywordGlossary.ResolveDescription(entry, currentValueText);
            if (!string.IsNullOrWhiteSpace(description))
            {
                content = new ConsumableKeywordTooltipContent
                {
                    keywordId = entry.keyword,
                    title = _consumableKeywordGlossary.ResolveDisplayName(entry),
                    description = description.Trim(),
                    icon = entry.icon
                };
                return true;
            }
        }

        if (!Enum.TryParse(keywordId, ignoreCase: true, out DiceFaceEnchantKind enchant) ||
            !DiceFaceEnchantUtility.HasEnchant(enchant))
        {
            return false;
        }

        string enchantDescription = DiceFaceEnchantUtility.GetShortRulesText(enchant);
        if (string.IsNullOrWhiteSpace(enchantDescription))
            return false;

        content = new ConsumableKeywordTooltipContent
        {
            keywordId = DiceFaceEnchantUtility.GetDisplayName(enchant),
            title = DiceFaceEnchantUtility.GetDisplayName(enchant),
            description = enchantDescription.Trim(),
            icon = null
        };
        return true;
    }

    private void ShowConsumableKeywordTooltips(RectTransform tooltipRootRect, List<ConsumableKeywordTooltipContent> contents)
    {
        Canvas canvas = tooltipRootRect != null ? tooltipRootRect.GetComponentInParent<Canvas>() : null;
        RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        Camera eventCamera = GetCanvasEventCamera(canvas);
        if (tooltipRootRect == null || canvasRect == null)
            return;

        Rect parentRect = GetScreenRect(tooltipRootRect, eventCamera);
        float centerY = parentRect.center.y;
        float rightX = parentRect.xMax + ConsumableKeywordTooltipGap;

        float totalHeight = 0f;
        for (int i = 0; i < contents.Count; i++)
        {
            ConsumableKeywordTooltipView view = EnsureConsumableKeywordTooltipView(i, canvasRect);
            if (view == null || view.root == null)
                return;

            ApplyConsumableKeywordTooltipContent(view, contents[i]);
            view.root.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(view.root);
            totalHeight += Mathf.Max(10f, view.root.rect.height);
            if (i < contents.Count - 1)
                totalHeight += ConsumableKeywordTooltipStackGap;
        }

        float currentTop = centerY + (totalHeight * 0.5f);
        for (int i = 0; i < contents.Count; i++)
        {
            ConsumableKeywordTooltipView view = _consumableKeywordTooltipViews[i];
            float width = Mathf.Clamp(view.root.rect.width, ConsumableKeywordTooltipMinWidth, ConsumableKeywordTooltipMaxWidth);
            float height = Mathf.Max(10f, view.root.rect.height);
            view.root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

            float clampedLeft = Mathf.Clamp(rightX, 8f, Screen.width - width - 8f);
            float clampedTop = Mathf.Clamp(currentTop, height + 8f, Screen.height - 8f);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, new Vector2(clampedLeft, clampedTop), eventCamera, out Vector2 localTopLeft))
            {
                view.root.anchorMin = new Vector2(0.5f, 0.5f);
                view.root.anchorMax = new Vector2(0.5f, 0.5f);
                view.root.pivot = new Vector2(0f, 1f);
                view.root.anchoredPosition = localTopLeft;
            }

            view.root.SetAsLastSibling();
            currentTop -= height + ConsumableKeywordTooltipStackGap;
        }

        for (int i = contents.Count; i < _consumableKeywordTooltipViews.Count; i++)
        {
            if (_consumableKeywordTooltipViews[i]?.root != null)
                _consumableKeywordTooltipViews[i].root.gameObject.SetActive(false);
        }
    }

    private ConsumableKeywordTooltipView EnsureConsumableKeywordTooltipView(int index, RectTransform parent)
    {
        while (_consumableKeywordTooltipViews.Count <= index)
        {
            ConsumableKeywordTooltipView createdView = _consumableKeywordTooltipPrefab != null
                ? CreateConsumableKeywordTooltipViewFromPrefab(parent)
                : CreateConsumableKeywordTooltipViewFallback(parent);
            if (createdView == null)
                return null;

            _consumableKeywordTooltipViews.Add(createdView);
        }

        ConsumableKeywordTooltipView view = _consumableKeywordTooltipViews[index];
        if (view.root.parent != parent)
            view.root.SetParent(parent, false);
        return view;
    }

    private ConsumableKeywordTooltipView CreateConsumableKeywordTooltipViewFromPrefab(RectTransform parent)
    {
        SkillTooltipKeywordTooltipTemplate prefabInstance = Instantiate(_consumableKeywordTooltipPrefab, parent);
        RectTransform rootRect = prefabInstance.RectTransform;
        rootRect.gameObject.name = "ConsumableKeywordTooltip";
        rootRect.gameObject.SetActive(false);
        Image icon = prefabInstance.IconImage;
        if (icon != null)
            icon.preserveAspect = true;

        return new ConsumableKeywordTooltipView
        {
            root = rootRect,
            background = prefabInstance.Background,
            icon = icon,
            iconLayout = icon != null ? icon.GetComponent<LayoutElement>() : null,
            title = prefabInstance.TitleText,
            body = prefabInstance.BodyText,
            layout = rootRect.GetComponent<VerticalLayoutGroup>(),
            fitter = rootRect.GetComponent<ContentSizeFitter>(),
            usesTemplate = true
        };
    }

    private ConsumableKeywordTooltipView CreateConsumableKeywordTooltipViewFallback(RectTransform parent)
    {
        GameObject rootGo = new GameObject("ConsumableKeywordTooltip", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        RectTransform rootRect = rootGo.GetComponent<RectTransform>();
        rootRect.SetParent(parent, false);
        rootRect.gameObject.SetActive(false);

        Image background = rootGo.GetComponent<Image>();
        background.raycastTarget = true;
        background.color = new Color(0.11f, 0.13f, 0.18f, 0.96f);

        VerticalLayoutGroup layout = rootGo.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 6f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        ContentSizeFitter fitter = rootGo.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        titleGo.transform.SetParent(rootRect, false);
        TMP_Text title = titleGo.GetComponent<TMP_Text>();
        title.fontSize = 30f;
        title.fontStyle = FontStyles.Bold;
        title.color = Color.white;
        title.alignment = TextAlignmentOptions.TopLeft;
        title.textWrappingMode = TextWrappingModes.Normal;
        title.overflowMode = TextOverflowModes.Overflow;
        title.raycastTarget = false;

        GameObject bodyGo = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        bodyGo.transform.SetParent(rootRect, false);
        TMP_Text body = bodyGo.GetComponent<TMP_Text>();
        body.fontSize = 14f;
        body.color = Color.white;
        body.alignment = TextAlignmentOptions.TopLeft;
        body.textWrappingMode = TextWrappingModes.Normal;
        body.overflowMode = TextOverflowModes.Overflow;
        body.raycastTarget = false;
        LayoutElement bodyLayout = bodyGo.GetComponent<LayoutElement>();
        bodyLayout.preferredWidth = ConsumableKeywordTooltipMaxWidth - 24f;
        bodyLayout.flexibleWidth = 0f;

        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        iconGo.transform.SetParent(rootRect, false);
        Image icon = iconGo.GetComponent<Image>();
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        LayoutElement iconLayout = iconGo.GetComponent<LayoutElement>();
        iconLayout.preferredWidth = 0f;
        iconLayout.preferredHeight = 0f;

        return new ConsumableKeywordTooltipView
        {
            root = rootRect,
            background = background,
            icon = icon,
            iconLayout = iconLayout,
            title = title,
            body = body,
            layout = layout,
            fitter = fitter,
            usesTemplate = false
        };
    }

    private void ApplyConsumableKeywordTooltipContent(ConsumableKeywordTooltipView view, ConsumableKeywordTooltipContent content)
    {
        if (view == null)
            return;

        if (view.title != null)
            view.title.text = content.title ?? string.Empty;
        if (view.body != null)
            view.body.text = content.description ?? string.Empty;
        if (view.icon != null)
        {
            bool showIcon = content.icon != null;
            view.icon.sprite = content.icon;
            view.icon.enabled = showIcon;
            if (view.iconLayout != null)
            {
                view.iconLayout.preferredWidth = showIcon ? 20f : 0f;
                view.iconLayout.preferredHeight = showIcon ? 20f : 0f;
            }
        }
    }

    private bool IsPointerOverAnyConsumableKeywordTooltip()
    {
        Vector2 screenPoint = Input.mousePosition;
        for (int i = 0; i < _consumableKeywordTooltipViews.Count; i++)
        {
            RectTransform rect = _consumableKeywordTooltipViews[i]?.root;
            if (rect == null || !rect.gameObject.activeInHierarchy)
                continue;

            Canvas canvas = rect.GetComponentInParent<Canvas>();
            if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, GetCanvasEventCamera(canvas)))
                return true;
        }

        return false;
    }

    private void HideConsumableKeywordTooltips()
    {
        for (int i = 0; i < _consumableKeywordTooltipViews.Count; i++)
        {
            if (_consumableKeywordTooltipViews[i]?.root != null)
                _consumableKeywordTooltipViews[i].root.gameObject.SetActive(false);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class SkillTooltipUI
{
    private const float KeywordTooltipGap = 10f;
    private const float KeywordTooltipStackGap = 8f;
    private const float KeywordTooltipMinWidth = 150f;
    private const float KeywordTooltipMaxWidth = 240f;
    private const float KeywordTooltipPadding = 12f;
    private const float KeywordTooltipIconSize = 20f;
    private static readonly Regex RichTextTagRegex = new Regex("(<.*?>)", RegexOptions.Compiled);

    private sealed class KeywordTooltipView
    {
        public RectTransform root;
        public Image background;
        public HorizontalLayoutGroup headerLayout;
        public Image icon;
        public LayoutElement iconLayout;
        public TMP_Text title;
        public TMP_Text body;
        public ContentSizeFitter fitter;
        public VerticalLayoutGroup layout;
        public string keywordId;
        public string contentSignature;
        public bool usesTemplate;
    }

    private SkillTooltipKeywordTooltipTemplate KeywordTooltipPrefab
    {
        get
        {
            if (_layout != null && _layout.KeywordTooltipPrefab != null)
                return _layout.KeywordTooltipPrefab;

            SkillTooltipPrefabProvider provider = SkillTooltipPrefabProviderRegistry.Get();
            if (provider != null && provider.KeywordTooltipPrefab != null)
                return provider.KeywordTooltipPrefab;

            SkillTooltipPrefabSettingsSO settings = GetPrefabSettings();
            return settings != null ? settings.KeywordTooltipPrefab : null;
        }
    }

    private void BindKeywordGlossary()
    {
        _keywordGlossary = _layout != null ? _layout.KeywordGlossary : null;
    }

    private string ApplyKeywordMarkup(string text)
    {
        if (string.IsNullOrEmpty(text) || _keywordGlossary == null || _keywordGlossary.Entries == null || _keywordGlossary.Entries.Length == 0)
            return text ?? string.Empty;

        string[] segments = RichTextTagRegex.Split(text);
        SkillTooltipKeywordGlossarySO.KeywordEntry[] entries = (SkillTooltipKeywordGlossarySO.KeywordEntry[])_keywordGlossary.Entries.Clone();
        Array.Sort(entries, (a, b) => string.Compare(b.keyword, a.keyword, StringComparison.OrdinalIgnoreCase));
        for (int i = 0; i < segments.Length; i++)
        {
            if (string.IsNullOrEmpty(segments[i]) || segments[i][0] == '<')
                continue;

            string segment = segments[i];
            for (int entryIndex = 0; entryIndex < entries.Length; entryIndex++)
            {
                string keyword = entries[entryIndex].keyword;
                if (string.IsNullOrWhiteSpace(keyword) || !HasResolvableDescription(entries[entryIndex]))
                    continue;

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

    private bool HasResolvableDescription(SkillTooltipKeywordGlossarySO.KeywordEntry entry)
    {
        if (_keywordGlossary == null || string.IsNullOrWhiteSpace(entry.keyword))
            return false;

        string resolved = _keywordGlossary.ResolveDescription(entry, entry.keyword);
        return !string.IsNullOrWhiteSpace(resolved);
    }

    

    private void HideAllKeywordTooltips()
    {
        for (int i = 0; i < _keywordTooltipViews.Count; i++)
        {
            if (_keywordTooltipViews[i]?.root != null)
                _keywordTooltipViews[i].root.gameObject.SetActive(false);
        }
    }

    private void UpdateKeywordTooltips()
    {
        if (_root == null || !_root.gameObject.activeInHierarchy || _keywordGlossary == null)
        {
            HideAllKeywordTooltips();
            _activeKeywordId = null;
            return;
        }

        bool showAll = IsExpandedInputActive();
        List<KeywordTooltipContent> contents = showAll ? CollectVisibleKeywordContents() : CollectHoveredKeywordContents();
        if (contents.Count == 0)
        {
            HideAllKeywordTooltips();
            if (!showAll && !IsPointerOverAnyKeywordTooltip())
                _activeKeywordId = null;
            return;
        }

        ShowKeywordTooltips(contents);
    }

    private List<KeywordTooltipContent> CollectHoveredKeywordContents()
    {
        List<KeywordTooltipContent> results = new List<KeywordTooltipContent>(1);
        if (TryGetHoveredKeywordContent(out KeywordTooltipContent hovered))
        {
            _activeKeywordId = hovered.keywordId;
            results.Add(hovered);
            return results;
        }

        if (!string.IsNullOrEmpty(_activeKeywordId) && IsPointerOverAnyKeywordTooltip())
        {
            if (TryBuildKeywordTooltipContent(_activeKeywordId, _activeKeywordId, out KeywordTooltipContent persisted))
                results.Add(persisted);
        }

        return results;
    }

    private List<KeywordTooltipContent> CollectVisibleKeywordContents()
    {
        List<KeywordTooltipContent> results = new List<KeywordTooltipContent>();
        HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        TMP_Text[] blocks = GetKeywordInteractiveBlocks();
        for (int blockIndex = 0; blockIndex < blocks.Length; blockIndex++)
        {
            TMP_Text block = blocks[blockIndex];
            if (block == null || !block.gameObject.activeInHierarchy)
                continue;

            EnsureKeywordTextMeshCurrent(block);
            TMP_LinkInfo[] links = block.textInfo.linkInfo;
            for (int linkIndex = 0; linkIndex < block.textInfo.linkCount; linkIndex++)
            {
                string keywordId = links[linkIndex].GetLinkID();
                string linkText = links[linkIndex].GetLinkText();
                if (string.IsNullOrWhiteSpace(keywordId) || !seen.Add(keywordId))
                    continue;

                if (TryBuildKeywordTooltipContent(keywordId, linkText, out KeywordTooltipContent content))
                    results.Add(content);
            }
        }

        return results;
    }

    private bool TryGetHoveredKeywordContent(out KeywordTooltipContent content)
    {
        TMP_Text[] blocks = GetKeywordInteractiveBlocks();
        for (int i = 0; i < blocks.Length; i++)
        {
            TMP_Text block = blocks[i];
            if (block == null || !block.gameObject.activeInHierarchy)
                continue;

            EnsureKeywordTextMeshCurrent(block);
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(block, Input.mousePosition, _uiCamera);
            if (linkIndex < 0)
                continue;

            TMP_LinkInfo link = block.textInfo.linkInfo[linkIndex];
            if (TryBuildKeywordTooltipContent(link.GetLinkID(), link.GetLinkText(), out content))
                return true;
        }

        content = default;
        return false;
    }

    private void EnsureKeywordTextMeshCurrent(TMP_Text block)
    {
        if (block == null)
            return;

        string currentText = block.text ?? string.Empty;
        if (_keywordMeshTextCache.TryGetValue(block, out string cachedText) &&
            cachedText == currentText &&
            !block.havePropertiesChanged)
        {
            return;
        }

        block.ForceMeshUpdate();
        _keywordMeshTextCache[block] = currentText;
    }

    private bool TryBuildKeywordTooltipContent(string keywordId, string currentValueText, out KeywordTooltipContent content)
    {
        content = default;
        if (string.IsNullOrWhiteSpace(keywordId))
            return false;

        if (_keywordGlossary != null &&
            _keywordGlossary.TryGetEntry(keywordId, out SkillTooltipKeywordGlossarySO.KeywordEntry entry))
        {
            string description = _keywordGlossary.ResolveDescription(entry, currentValueText);
            if (!string.IsNullOrWhiteSpace(description))
            {
                content = new KeywordTooltipContent
                {
                    keywordId = entry.keyword,
                    title = _keywordGlossary.ResolveDisplayName(entry),
                    description = description.Trim(),
                    icon = entry.icon
                };
                return true;
            }
        }

        if (!System.Enum.TryParse(keywordId, ignoreCase: true, out DiceFaceEnchantKind enchant) ||
            !DiceFaceEnchantUtility.HasEnchant(enchant))
        {
            return false;
        }

        string enchantDescription = DiceFaceEnchantUtility.GetShortRulesText(enchant);
        if (string.IsNullOrWhiteSpace(enchantDescription))
            return false;

        content = new KeywordTooltipContent
        {
            keywordId = DiceFaceEnchantUtility.GetDisplayName(enchant),
            title = DiceFaceEnchantUtility.GetDisplayName(enchant),
            description = enchantDescription.Trim(),
            icon = null
        };
        return true;
    }

    private void ShowKeywordTooltips(List<KeywordTooltipContent> contents)
    {
        RectTransform parent = _root != null ? _root.parent as RectTransform : null;
        if (parent == null || _canvasRect == null)
            return;

        float parentTopY = GetScreenRect(_root, _uiCamera).yMax;
        float parentRightX = GetScreenRect(_root, _uiCamera).xMax;
        float parentLeftX = GetScreenRect(_root, _uiCamera).xMin;
        bool placeRight = parentRightX + KeywordTooltipGap + KeywordTooltipMinWidth <= Screen.width - TooltipHorizontalCanvasPadding;
        bool placeLeft = !placeRight;
        float currentTopY = parentTopY;

        for (int i = 0; i < contents.Count; i++)
        {
            KeywordTooltipView view = EnsureKeywordTooltipView(i, parent);
            if (view == null || view.root == null)
            {
                HideAllKeywordTooltips();
                return;
            }

            string contentSignature = BuildKeywordTooltipSignature(contents[i]);
            bool wasActive = view.root.gameObject.activeSelf;
            bool contentChanged = view.contentSignature != contentSignature;
            if (contentChanged)
            {
                ApplyKeywordTooltipContent(view, contents[i]);
                view.contentSignature = contentSignature;
            }

            CombatUiDirtySetUtility.SetActiveIfChanged(view.root.gameObject, true);
            if (contentChanged || !wasActive)
                LayoutRebuilder.ForceRebuildLayoutImmediate(view.root);

            Rect sizeRect = view.root.rect;
            float width = view.usesTemplate
                ? Mathf.Max(sizeRect.width, 10f)
                : Mathf.Clamp(sizeRect.width, KeywordTooltipMinWidth, KeywordTooltipMaxWidth);
            float height = Mathf.Max(sizeRect.height, 10f);
            if (!view.usesTemplate)
                view.root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

            float leftX = placeLeft
                ? parentLeftX - KeywordTooltipGap - width
                : parentRightX + KeywordTooltipGap;
            float clampedLeftX = Mathf.Clamp(leftX, TooltipHorizontalCanvasPadding, Screen.width - TooltipHorizontalCanvasPadding - width);
            float desiredTopY = currentTopY;
            float clampedTopY = Mathf.Clamp(desiredTopY, height + TooltipVerticalCanvasPadding, Screen.height - TooltipVerticalCanvasPadding);
            Vector2 topLeftScreen = new Vector2(clampedLeftX, clampedTopY);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, topLeftScreen, _uiCamera, out Vector2 localTopLeft))
            {
                view.root.anchorMin = new Vector2(0.5f, 0.5f);
                view.root.anchorMax = new Vector2(0.5f, 0.5f);
                view.root.pivot = new Vector2(0f, 1f);
                view.root.anchoredPosition = localTopLeft;
            }

            view.root.SetAsLastSibling();
            currentTopY = clampedTopY - height - KeywordTooltipStackGap;
        }

        for (int i = contents.Count; i < _keywordTooltipViews.Count; i++)
        {
            if (_keywordTooltipViews[i]?.root != null)
                CombatUiDirtySetUtility.SetActiveIfChanged(_keywordTooltipViews[i].root.gameObject, false);
        }
    }

    private KeywordTooltipView EnsureKeywordTooltipView(int index, RectTransform parent)
    {
        while (_keywordTooltipViews.Count <= index)
        {
            KeywordTooltipView createdView = CreateKeywordTooltipView(parent);
            if (createdView == null)
                return null;

            _keywordTooltipViews.Add(createdView);
        }

        KeywordTooltipView view = _keywordTooltipViews[index];
        if (view.root.parent != parent)
            view.root.SetParent(parent, false);
        return view;
    }

    private KeywordTooltipView CreateKeywordTooltipView(RectTransform parent)
    {
        SkillTooltipKeywordTooltipTemplate prefab = KeywordTooltipPrefab;
        if (prefab != null && prefab.RectTransform != null)
            return CreateKeywordTooltipViewFromPrefab(parent, prefab);

        Debug.LogError(
            $"Keyword tooltip prefab is missing. Assign it on {nameof(SkillTooltipPrefabProvider)}, {nameof(SkillTooltipPrefabSettingsSO)}, or {nameof(SkillTooltipLayout)}.",
            this);
        return null;
    }

    private KeywordTooltipView CreateKeywordTooltipViewFromPrefab(RectTransform parent, SkillTooltipKeywordTooltipTemplate prefab)
    {
        SkillTooltipKeywordTooltipTemplate prefabInstance = UnityEngine.Object.Instantiate(prefab, parent);
        RectTransform rootRect = prefabInstance.RectTransform;
        rootRect.gameObject.name = "KeywordTooltip";
        rootRect.gameObject.SetActive(false);

        Image icon = prefabInstance.IconImage;
        if (icon != null)
            icon.preserveAspect = true;

        return new KeywordTooltipView
        {
            root = rootRect,
            background = prefabInstance.Background,
            headerLayout = icon != null ? icon.transform.parent?.GetComponent<HorizontalLayoutGroup>() : null,
            icon = icon,
            iconLayout = icon != null ? icon.GetComponent<LayoutElement>() : null,
            title = prefabInstance.TitleText,
            body = prefabInstance.BodyText,
            fitter = rootRect.GetComponent<ContentSizeFitter>(),
            layout = rootRect.GetComponent<VerticalLayoutGroup>(),
            usesTemplate = true
        };
    }

    

    

    private void ApplyKeywordTooltipContent(KeywordTooltipView view, KeywordTooltipContent content)
    {
        view.keywordId = content.keywordId;
        CombatUiDirtySetUtility.SetTextIfChanged(view.title, content.title ?? string.Empty);
        CombatUiDirtySetUtility.SetTextIfChanged(view.body, content.description ?? string.Empty);
        bool showIcon = content.icon != null;
        if (view.icon != null)
        {
            if (view.icon.sprite != content.icon)
                view.icon.sprite = content.icon;
            if (view.icon.enabled != showIcon)
                view.icon.enabled = showIcon;
            CombatUiDirtySetUtility.SetActiveIfChanged(view.icon.gameObject, showIcon);
            if (!view.usesTemplate && view.iconLayout != null)
            {
                Vector2 size = showIcon ? GetKeywordIconSize(content.icon) : Vector2.zero;
                view.iconLayout.preferredWidth = size.x;
                view.iconLayout.preferredHeight = size.y;
                view.iconLayout.minWidth = size.x;
                view.iconLayout.minHeight = size.y;
            }
        }
    }

    private static string BuildKeywordTooltipSignature(KeywordTooltipContent content)
    {
        int iconId = content.icon != null ? content.icon.GetInstanceID() : 0;
        return $"{content.keywordId}\u001f{content.title}\u001f{content.description}\u001f{iconId}";
    }

    private static Vector2 GetKeywordIconSize(Sprite icon)
    {
        if (icon == null)
            return Vector2.zero;

        Rect rect = icon.rect;
        float width = Mathf.Max(1f, rect.width);
        float height = Mathf.Max(1f, rect.height);
        float scale = Mathf.Min(KeywordTooltipIconSize / width, KeywordTooltipIconSize / height, 1f);
        return new Vector2(width * scale, height * scale);
    }

    private bool IsPointerOverAnyKeywordTooltip()
    {
        Vector2 screenPoint = Input.mousePosition;
        for (int i = 0; i < _keywordTooltipViews.Count; i++)
        {
            RectTransform rect = _keywordTooltipViews[i]?.root;
            if (IsScreenPointInsideRect(rect, screenPoint, _uiCamera))
                return true;
        }

        return false;
    }

    private TMP_Text[] GetKeywordInteractiveBlocks()
    {
        return new[]
        {
            _targeting,
            _requires,
            _effect,
            _condition
        };
    }

    private struct KeywordTooltipContent
    {
        public string keywordId;
        public string title;
        public string description;
        public Sprite icon;

        public KeywordTooltipContent(string keywordId, string title, string description, Sprite icon)
        {
            this.keywordId = keywordId;
            this.title = title;
            this.description = description;
            this.icon = icon;
        }
    }
}

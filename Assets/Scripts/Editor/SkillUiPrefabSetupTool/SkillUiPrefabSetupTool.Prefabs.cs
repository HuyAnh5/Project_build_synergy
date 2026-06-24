using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Builds the generated skill slot and tooltip prefabs used by the runtime scene setup.
public static partial class SkillUiPrefabSetupTool
{
    // Creates the draggable skill slot layout prefab and wires serialized layout references.
    private static void CreateSkillSlotPrefab(SkillUiIconLibrarySO iconLibrary)
    {
        GameObject root = new GameObject("SkillSlotLayout", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(DraggableSkillIcon), typeof(SkillSlotLayout));
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(120f, 120f);

        Image rootImage = root.GetComponent<Image>();
        rootImage.color = Color.white;
        rootImage.raycastTarget = true;

        SkillSlotLayout layout = root.GetComponent<SkillSlotLayout>();
        Image artImage = CreateSkillArt(root.transform);
        TMP_Text title = CreateSkillTitle(root.transform);
        Image focusBg = CreateBadge(root.transform, "FocusCostBadge", new Vector2(0f, 1f), new Vector2(6f, -6f), new Color(0.1f, 0.22f, 0.35f, 0.92f), out TMP_Text focusText);
        Image diceBg = CreateBadge(root.transform, "SlotCostBadge", new Vector2(1f, 1f), new Vector2(-6f, -6f), new Color(0.28f, 0.2f, 0.55f, 0.92f), out TMP_Text diceFallback);
        Image diceIcon = CreateBadgeIcon(diceBg.transform, Vector2.zero);
        Image elementBg = CreateElementBadge(root.transform, out Image elementIcon);

        WireSkillSlotLayout(layout, rootImage, artImage, title, focusBg, focusText, diceBg, diceIcon, diceFallback, elementBg, elementIcon);
        WireDraggableSkillIcon(root.GetComponent<DraggableSkillIcon>(), iconLibrary, layout, rootImage, title, focusBg, focusText, diceBg, diceIcon, diceFallback, elementBg, elementIcon);

        PrefabUtility.SaveAsPrefabAsset(root, SkillSlotPrefabPath);
        Object.DestroyImmediate(root);
    }

    // Creates the skill art image child.
    private static Image CreateSkillArt(Transform parent)
    {
        GameObject artGo = CreateChild(parent, "Art", typeof(RectTransform), typeof(Image));
        RectTransform artRt = artGo.GetComponent<RectTransform>();
        artRt.anchorMin = new Vector2(0f, 0f);
        artRt.anchorMax = new Vector2(1f, 1f);
        artRt.offsetMin = new Vector2(4f, 4f);
        artRt.offsetMax = new Vector2(-4f, -4f);
        Image artImage = artGo.GetComponent<Image>();
        artImage.preserveAspect = true;
        return artImage;
    }

    // Creates the skill title text child.
    private static TMP_Text CreateSkillTitle(Transform parent)
    {
        GameObject titleGo = CreateChild(parent, "Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0f);
        titleRt.anchorMax = new Vector2(1f, 0f);
        titleRt.pivot = new Vector2(0.5f, 0f);
        titleRt.sizeDelta = new Vector2(0f, 26f);
        titleRt.anchoredPosition = new Vector2(0f, -28f);
        TMP_Text title = titleGo.GetComponent<TMP_Text>();
        title.text = "Skill";
        title.fontSize = 16f;
        title.alignment = TextAlignmentOptions.Center;
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.raycastTarget = false;
        return title;
    }

    // Creates the dice or element icon image inside a badge.
    private static Image CreateBadgeIcon(Transform parent, Vector2 inset)
    {
        GameObject iconGo = CreateChild(parent, "Icon", typeof(RectTransform), typeof(Image));
        RectTransform iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.offsetMin = inset == Vector2.zero ? new Vector2(3f, 3f) : inset;
        iconRt.offsetMax = inset == Vector2.zero ? new Vector2(-3f, -3f) : -inset;
        Image icon = iconGo.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        return icon;
    }

    // Creates the rotated element badge and nested icon.
    private static Image CreateElementBadge(Transform parent, out Image elementIcon)
    {
        GameObject elementGo = CreateChild(parent, "ElementBadge", typeof(RectTransform), typeof(Image));
        RectTransform elementRt = elementGo.GetComponent<RectTransform>();
        elementRt.anchorMin = new Vector2(1f, 0f);
        elementRt.anchorMax = new Vector2(1f, 0f);
        elementRt.pivot = new Vector2(1f, 0f);
        elementRt.sizeDelta = new Vector2(24f, 24f);
        elementRt.anchoredPosition = new Vector2(-6f, 6f);
        elementRt.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Image elementBg = elementGo.GetComponent<Image>();
        elementBg.color = new Color(0.65f, 0.2f, 0.1f, 0.95f);
        elementBg.raycastTarget = false;

        elementIcon = CreateBadgeIcon(elementGo.transform, new Vector2(4f, 4f));
        elementIcon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);
        return elementBg;
    }

    // Wires generated child references into SkillSlotLayout.
    private static void WireSkillSlotLayout(
        SkillSlotLayout layout,
        Image rootImage,
        Image artImage,
        TMP_Text title,
        Image focusBg,
        TMP_Text focusText,
        Image diceBg,
        Image diceIcon,
        TMP_Text diceFallback,
        Image elementBg,
        Image elementIcon)
    {
        SerializedObject layoutSo = new SerializedObject(layout);
        layoutSo.FindProperty("backgroundImage").objectReferenceValue = rootImage;
        layoutSo.FindProperty("skillArt").objectReferenceValue = artImage;
        layoutSo.FindProperty("titleText").objectReferenceValue = title;
        layoutSo.FindProperty("focusBadgeBackground").objectReferenceValue = focusBg;
        layoutSo.FindProperty("focusBadgeText").objectReferenceValue = focusText;
        layoutSo.FindProperty("diceBadgeBackground").objectReferenceValue = diceBg;
        layoutSo.FindProperty("diceBadgeIcon").objectReferenceValue = diceIcon;
        layoutSo.FindProperty("diceBadgeFallbackText").objectReferenceValue = diceFallback;
        layoutSo.FindProperty("elementBadgeBackground").objectReferenceValue = elementBg;
        layoutSo.FindProperty("elementBadgeIcon").objectReferenceValue = elementIcon;
        layoutSo.ApplyModifiedPropertiesWithoutUndo();
    }

    // Wires generated child references into DraggableSkillIcon.
    private static void WireDraggableSkillIcon(
        DraggableSkillIcon icon,
        SkillUiIconLibrarySO iconLibrary,
        SkillSlotLayout layout,
        Image rootImage,
        TMP_Text title,
        Image focusBg,
        TMP_Text focusText,
        Image diceBg,
        Image diceIcon,
        TMP_Text diceFallback,
        Image elementBg,
        Image elementIcon)
    {
        SerializedObject iconSo = new SerializedObject(icon);
        iconSo.FindProperty("iconLibrary").objectReferenceValue = iconLibrary;
        iconSo.FindProperty("skillSlotLayout").objectReferenceValue = layout;
        iconSo.FindProperty("nameText").objectReferenceValue = title;
        iconSo.FindProperty("focusCostBadgeBackground").objectReferenceValue = focusBg;
        iconSo.FindProperty("focusCostBadgeText").objectReferenceValue = focusText;
        iconSo.FindProperty("slotCostBadgeBackground").objectReferenceValue = diceBg;
        iconSo.FindProperty("slotCostBadgeIcon").objectReferenceValue = diceIcon;
        iconSo.FindProperty("slotCostBadgeText").objectReferenceValue = diceFallback;
        iconSo.FindProperty("elementBadgeBackground").objectReferenceValue = elementBg;
        iconSo.FindProperty("elementBadgeIcon").objectReferenceValue = elementIcon;
        iconSo.FindProperty("skillBackgroundImage").objectReferenceValue = rootImage;
        iconSo.ApplyModifiedPropertiesWithoutUndo();
    }

    // Creates the tooltip layout prefab and wires serialized layout references.
    private static void CreateSkillTooltipPrefab()
    {
        GameObject root = new GameObject("SkillTooltipLayout", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(SkillTooltipUI), typeof(SkillTooltipLayout));
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(220f, 120f);
        rootRt.pivot = new Vector2(0f, 1f);

        Image bg = root.GetComponent<Image>();
        bg.color = new Color(0.075f, 0.085f, 0.11f, 0.97f);
        bg.raycastTarget = false;

        VerticalLayoutGroup layoutGroup = root.GetComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(12, 12, 10, 10);
        layoutGroup.spacing = 7f;
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;

        ContentSizeFitter fitter = root.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text title = CreateTooltipText(root.transform, "Title", 19f, FontStyles.Bold);
        TMP_Text body = CreateTooltipText(root.transform, "Body", 14f, FontStyles.Normal);
        body.color = new Color(0.91f, 0.93f, 0.96f, 1f);

        SkillTooltipLayout tooltipLayout = root.GetComponent<SkillTooltipLayout>();
        SerializedObject so = new SerializedObject(tooltipLayout);
        so.FindProperty("background").objectReferenceValue = bg;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("bodyText").objectReferenceValue = body;
        so.FindProperty("verticalLayout").objectReferenceValue = layoutGroup;
        so.FindProperty("contentSizeFitter").objectReferenceValue = fitter;
        so.FindProperty("minContentWidth").floatValue = 170f;
        so.FindProperty("maxContentWidth").floatValue = 320f;
        so.FindProperty("minContentHeight").floatValue = 0f;
        so.FindProperty("maxContentHeight").floatValue = 0f;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, SkillTooltipPrefabPath);
        Object.DestroyImmediate(root);
    }

    // Creates a cost badge with centered text.
    private static Image CreateBadge(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition, Color color, out TMP_Text text)
    {
        GameObject badgeGo = CreateChild(parent, name, typeof(RectTransform), typeof(Image));
        RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
        badgeRt.anchorMin = anchor;
        badgeRt.anchorMax = anchor;
        badgeRt.pivot = anchor;
        badgeRt.sizeDelta = new Vector2(28f, 22f);
        badgeRt.anchoredPosition = anchoredPosition;

        Image bg = badgeGo.GetComponent<Image>();
        bg.color = color;
        bg.raycastTarget = false;

        GameObject valueGo = CreateChild(badgeGo.transform, "Value", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform valueRt = valueGo.GetComponent<RectTransform>();
        valueRt.anchorMin = Vector2.zero;
        valueRt.anchorMax = Vector2.one;
        valueRt.offsetMin = Vector2.zero;
        valueRt.offsetMax = Vector2.zero;

        text = valueGo.GetComponent<TMP_Text>();
        text.text = "1";
        text.fontSize = 16f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        return bg;
    }

    // Creates tooltip TMP text with autosizing and preferred width.
    private static TMP_Text CreateTooltipText(Transform parent, string name, float size, FontStyles style)
    {
        GameObject textGo = CreateChild(parent, name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        TMP_Text text = textGo.GetComponent<TMP_Text>();
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = size - 3f;
        text.fontSizeMax = size + 2f;
        text.fontStyle = style;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.richText = true;
        text.raycastTarget = false;

        LayoutElement layout = textGo.GetComponent<LayoutElement>();
        layout.preferredWidth = 196f;
        return text;
    }

    // Creates a child GameObject and parents it without changing local UI transform assumptions.
    private static GameObject CreateChild(Transform parent, string name, params System.Type[] components)
    {
        GameObject child = new GameObject(name, components);
        RectTransform rt = child.transform as RectTransform;
        rt.SetParent(parent, false);
        return child;
    }
}

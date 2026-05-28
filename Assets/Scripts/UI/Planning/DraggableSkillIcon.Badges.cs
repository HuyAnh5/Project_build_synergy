using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class DraggableSkillIcon
{
    private void RefreshCostBadges()
    {
        SkillUiIconLibrarySO resolvedIconLibrary = ResolveIconLibrary();
        bool showBadges = SkillUiMetadataUtility.TryGetSkillCosts(GetSkillAsset(), out int focusCost, out int slotsRequired);
        SetCostBadgeVisible(focusCostBadgeBackground, focusCostBadgeText, showBadges);
        SetCostBadgeVisible(slotCostBadgeBackground, slotCostBadgeText, showBadges);
        if (slotCostBadgeIcon != null)
            slotCostBadgeIcon.gameObject.SetActive(showBadges);

        if (!showBadges)
            return;

        if (focusCostBadgeText != null)
            focusCostBadgeText.text = focusCost.ToString();

        Sprite diceCostIcon = resolvedIconLibrary != null ? resolvedIconLibrary.GetDiceCostIcon(slotsRequired) : null;
        if (slotCostBadgeIcon != null)
        {
            slotCostBadgeIcon.sprite = diceCostIcon;
            slotCostBadgeIcon.enabled = diceCostIcon != null;
        }

        if (slotCostBadgeText != null)
        {
            bool useFallbackText = diceCostIcon == null;
            slotCostBadgeText.gameObject.SetActive(useFallbackText);
            if (useFallbackText)
                slotCostBadgeText.text = slotsRequired.ToString();
        }
    }

    private void RefreshElementBadge()
    {
        ScriptableObject asset = GetSkillAsset();
        SkillUiIconLibrarySO resolvedIconLibrary = ResolveIconLibrary();
        Sprite icon = null;
        Color backgroundColor = Color.white;
        Color iconTint = Color.white;
        Color slotBackgroundColor = Color.white;
        bool hasElement = false;
        if (resolvedIconLibrary != null && SkillUiMetadataUtility.TryGetElementType(asset, out ElementType element))
            hasElement = resolvedIconLibrary.TryGetElementVisual(element, out slotBackgroundColor, out icon, out backgroundColor, out iconTint);

        if (elementBadgeBackground != null)
            elementBadgeBackground.gameObject.SetActive(hasElement);
        if (elementBadgeIcon != null)
        {
            elementBadgeIcon.gameObject.SetActive(hasElement);
            if (hasElement)
            {
                if (elementBadgeBackground != null)
                    elementBadgeBackground.color = backgroundColor;
                elementBadgeIcon.sprite = icon;
                elementBadgeIcon.color = iconTint;
            }
        }

        if (skillBackgroundImage != null)
        {
            if (hasElement)
                skillBackgroundImage.color = slotBackgroundColor;
            else
                skillBackgroundImage.color = Color.white;
        }
    }

    private void EnsureCostBadgeUi()
    {
        focusCostBadgeBackground = EnsureBadge(ref focusCostBadgeBackground, ref focusCostBadgeText, FocusBadgeName, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(6f, -6f), new Color(0.1f, 0.22f, 0.35f, 0.92f));
        slotCostBadgeBackground = EnsureBadge(ref slotCostBadgeBackground, ref slotCostBadgeText, SlotBadgeName, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-6f, -6f), new Color(0.28f, 0.2f, 0.55f, 0.92f));
        slotCostBadgeIcon = EnsureBadgeIcon(slotCostBadgeBackground, ref slotCostBadgeIcon, "Icon");
        elementBadgeBackground = EnsureElementBadge(ref elementBadgeBackground, ref elementBadgeIcon);
    }

    public void BindLayout(SkillSlotLayout layout)
    {
        skillSlotLayout = layout;
        ApplyLayoutBindings();
    }

    private void ApplyLayoutBindings()
    {
        if (skillSlotLayout == null)
            return;

        if (skillSlotLayout.SkillArt != null)
            _img = skillSlotLayout.SkillArt;
        if (skillSlotLayout.TitleText != null)
            nameText = skillSlotLayout.TitleText;
        if (skillSlotLayout.BackgroundImage != null)
            skillBackgroundImage = skillSlotLayout.BackgroundImage;
        if (skillSlotLayout.FocusBadgeBackground != null)
            focusCostBadgeBackground = skillSlotLayout.FocusBadgeBackground;
        if (skillSlotLayout.FocusBadgeText != null)
            focusCostBadgeText = skillSlotLayout.FocusBadgeText;
        if (skillSlotLayout.DiceBadgeBackground != null)
            slotCostBadgeBackground = skillSlotLayout.DiceBadgeBackground;
        if (skillSlotLayout.DiceBadgeIcon != null)
            slotCostBadgeIcon = skillSlotLayout.DiceBadgeIcon;
        if (skillSlotLayout.DiceBadgeFallbackText != null)
            slotCostBadgeText = skillSlotLayout.DiceBadgeFallbackText;
        if (skillSlotLayout.ElementBadgeBackground != null)
            elementBadgeBackground = skillSlotLayout.ElementBadgeBackground;
        if (skillSlotLayout.ElementBadgeIcon != null)
            elementBadgeIcon = skillSlotLayout.ElementBadgeIcon;
        if (iconLibrary != null)
            _sharedIconLibrary = iconLibrary;
    }

    private Image EnsureBadge(
        ref Image badgeBackground,
        ref TMP_Text badgeText,
        string badgeName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Color backgroundColor)
    {
        if (!(transform is RectTransform))
            return badgeBackground;

        if (badgeBackground != null && badgeText != null)
            return badgeBackground;

        RectTransform badgeRoot = badgeBackground != null ? badgeBackground.rectTransform : transform.Find(badgeName) as RectTransform;
        bool createdBadge = badgeRoot == null;
        if (badgeRoot == null)
        {
            GameObject badgeGo = new GameObject(badgeName, typeof(RectTransform), typeof(Image));
            badgeRoot = badgeGo.GetComponent<RectTransform>();
            badgeRoot.SetParent(transform, false);
        }

        if (createdBadge)
        {
            badgeRoot.anchorMin = anchorMin;
            badgeRoot.anchorMax = anchorMax;
            badgeRoot.pivot = pivot;
            badgeRoot.sizeDelta = new Vector2(28f, 22f);
            badgeRoot.anchoredPosition = anchoredPosition;
        }

        badgeBackground = badgeRoot.GetComponent<Image>();
        if (badgeBackground == null)
            badgeBackground = badgeRoot.gameObject.AddComponent<Image>();
        badgeBackground.color = backgroundColor;
        badgeBackground.raycastTarget = false;

        RectTransform textRoot = badgeText != null ? badgeText.rectTransform : badgeRoot.Find("Value") as RectTransform;
        bool createdText = textRoot == null;
        if (textRoot == null)
        {
            GameObject textGo = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
            textRoot = textGo.GetComponent<RectTransform>();
            textRoot.SetParent(badgeRoot, false);
        }

        if (createdText)
        {
            textRoot.anchorMin = Vector2.zero;
            textRoot.anchorMax = Vector2.one;
            textRoot.offsetMin = Vector2.zero;
            textRoot.offsetMax = Vector2.zero;
        }

        badgeText = textRoot.GetComponent<TMP_Text>();
        if (badgeText == null)
            badgeText = textRoot.gameObject.AddComponent<TextMeshProUGUI>();
        badgeText.fontSize = 16f;
        badgeText.fontStyle = FontStyles.Bold;
        badgeText.alignment = TextAlignmentOptions.Center;
        badgeText.color = Color.white;
        badgeText.raycastTarget = false;
        if (badgeText.font == null && nameText != null)
            badgeText.font = nameText.font;

        return badgeBackground;
    }

    private Image EnsureBadgeIcon(Image badgeBackground, ref Image badgeIcon, string childName)
    {
        if (badgeBackground == null)
            return badgeIcon;

        if (badgeIcon != null)
            return badgeIcon;

        RectTransform iconRoot = badgeIcon != null ? badgeIcon.rectTransform : badgeBackground.transform.Find(childName) as RectTransform;
        bool createdIcon = iconRoot == null;
        if (iconRoot == null)
        {
            GameObject iconGo = new GameObject(childName, typeof(RectTransform), typeof(Image));
            iconRoot = iconGo.GetComponent<RectTransform>();
            iconRoot.SetParent(badgeBackground.transform, false);
        }

        if (createdIcon)
        {
            iconRoot.anchorMin = Vector2.zero;
            iconRoot.anchorMax = Vector2.one;
            iconRoot.offsetMin = new Vector2(3f, 3f);
            iconRoot.offsetMax = new Vector2(-3f, -3f);
        }

        badgeIcon = iconRoot.GetComponent<Image>();
        if (badgeIcon == null)
            badgeIcon = iconRoot.gameObject.AddComponent<Image>();
        badgeIcon.preserveAspect = true;
        badgeIcon.raycastTarget = false;
        badgeIcon.color = Color.white;
        return badgeIcon;
    }

    private Image EnsureElementBadge(ref Image badgeBackground, ref Image badgeIcon)
    {
        if (!(transform is RectTransform))
            return badgeBackground;

        if (badgeBackground != null && badgeIcon != null)
            return badgeBackground;

        RectTransform badgeRoot = badgeBackground != null ? badgeBackground.rectTransform : transform.Find(ElementBadgeName) as RectTransform;
        bool createdBadge = badgeRoot == null;
        if (badgeRoot == null)
        {
            GameObject badgeGo = new GameObject(ElementBadgeName, typeof(RectTransform), typeof(Image));
            badgeRoot = badgeGo.GetComponent<RectTransform>();
            badgeRoot.SetParent(transform, false);
        }

        if (createdBadge)
        {
            badgeRoot.anchorMin = new Vector2(1f, 0f);
            badgeRoot.anchorMax = new Vector2(1f, 0f);
            badgeRoot.pivot = new Vector2(1f, 0f);
            badgeRoot.sizeDelta = new Vector2(24f, 24f);
            badgeRoot.anchoredPosition = new Vector2(-6f, 6f);
            badgeRoot.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        badgeBackground = badgeRoot.GetComponent<Image>();
        if (badgeBackground == null)
            badgeBackground = badgeRoot.gameObject.AddComponent<Image>();
        badgeBackground.raycastTarget = false;

        RectTransform iconRoot = badgeIcon != null ? badgeIcon.rectTransform : badgeRoot.Find("Icon") as RectTransform;
        bool createdIcon = iconRoot == null;
        if (iconRoot == null)
        {
            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconRoot = iconGo.GetComponent<RectTransform>();
            iconRoot.SetParent(badgeRoot, false);
        }

        if (createdIcon)
        {
            iconRoot.anchorMin = Vector2.zero;
            iconRoot.anchorMax = Vector2.one;
            iconRoot.offsetMin = new Vector2(4f, 4f);
            iconRoot.offsetMax = new Vector2(-4f, -4f);
            iconRoot.localRotation = Quaternion.Euler(0f, 0f, -45f);
        }

        badgeIcon = iconRoot.GetComponent<Image>();
        if (badgeIcon == null)
            badgeIcon = iconRoot.gameObject.AddComponent<Image>();
        badgeIcon.raycastTarget = false;
        badgeIcon.preserveAspect = true;
        badgeIcon.color = Color.white;

        return badgeBackground;
    }

    private static void SetCostBadgeVisible(Image badgeBackground, TMP_Text badgeText, bool visible)
    {
        if (badgeBackground != null)
            badgeBackground.gameObject.SetActive(visible);
        if (badgeText != null && badgeText.gameObject != badgeBackground?.gameObject)
            badgeText.gameObject.SetActive(visible);
    }
}

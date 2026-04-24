using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class MapPrototypeUIFactory
{
    public static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        if (parent != null)
            rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        return rect;
    }

    public static Image CreateImage(string name, Transform parent, Color color, bool raycastTarget = false)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    public static Button CreateButton(string name, Transform parent, string label, Color fillColor, Color textColor, int fontSize = 24)
    {
        Image image = CreateImage(name, parent, fillColor, true);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI text = CreateText("Label", image.transform, label, fontSize, FontStyles.Bold, textColor, TextAlignmentOptions.Center);
        Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
        return button;
    }

    public static TextMeshProUGUI CreateText(string name, Transform parent, string text, int fontSize, FontStyles styles, Color color, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI textComponent = rect.gameObject.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = styles;
        textComponent.color = color;
        textComponent.alignment = alignment;
        textComponent.enableWordWrapping = true;
        textComponent.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
            textComponent.font = TMP_Settings.defaultFontAsset;
        return textComponent;
    }

    public static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    public static void SetTopLeft(RectTransform rect, Vector2 size, Vector2 anchoredPosition)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
    }

    public static void SetTopStretch(RectTransform rect, float height, float left = 0f, float right = 0f, float top = 0f)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(-(left + right), height);
        rect.anchoredPosition = new Vector2((left - right) * 0.5f, -top);
    }

    public static void SetStretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    public static void ConfigureLayoutGroup(HorizontalOrVerticalLayoutGroup group, float spacing, RectOffset padding, bool controlWidth, bool controlHeight, bool expandWidth, bool expandHeight)
    {
        group.spacing = spacing;
        group.padding = padding;
        group.childControlWidth = controlWidth;
        group.childControlHeight = controlHeight;
        group.childForceExpandWidth = expandWidth;
        group.childForceExpandHeight = expandHeight;
    }

    public static LayoutElement AddLayoutElement(GameObject go, float preferredWidth = -1f, float preferredHeight = -1f, float flexibleWidth = -1f, float flexibleHeight = -1f)
    {
        LayoutElement layout = go.GetComponent<LayoutElement>();
        if (layout == null)
            layout = go.AddComponent<LayoutElement>();

        if (preferredWidth >= 0f) layout.preferredWidth = preferredWidth;
        if (preferredHeight >= 0f) layout.preferredHeight = preferredHeight;
        if (flexibleWidth >= 0f) layout.flexibleWidth = flexibleWidth;
        if (flexibleHeight >= 0f) layout.flexibleHeight = flexibleHeight;
        return layout;
    }
}
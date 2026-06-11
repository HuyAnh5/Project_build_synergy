using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Builds and resolves the tooltip's structured text/icon child nodes from the prefab or at runtime.
public sealed partial class SkillTooltipUI
{
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
        if (_cost == null) _cost = CreateText("Cost", 13f, FontStyles.Normal, TextAlignmentOptions.TopRight);
        if (_targeting == null) _targeting = CreateText("Targeting", 14f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        if (_effect == null) _effect = CreateText("Effect", 14f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        if (_requiresHeader == null)
        {
            _requiresHeader = CreateText("RequiresHeader", 14f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            _requiresHeader.text = TooltipRequiresHeader;
        }
        if (_requires == null) _requires = CreateText("Requires", 14f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        if (_conditionHeader == null)
        {
            _conditionHeader = CreateText("ConditionHeader", 14f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            _conditionHeader.text = TooltipConditionHeader;
        }
        if (_condition == null) _condition = CreateText("Condition", 14f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
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

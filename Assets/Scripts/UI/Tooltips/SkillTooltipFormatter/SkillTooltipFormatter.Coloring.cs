using System;
using System.Text;

// Provides rich-text coloring and quoted segment replacement helpers for skill tooltips.
public static partial class SkillTooltipFormatter
{
    // Colors gameplay keywords in gold.
    private static string FormatKeyword(string text) => "<color=#FFD166>" + text + "</color>";

    // Colors text as the default dynamic Added Value color.
    private static string Blue(string text) => ColorText(text, AddedValueColor);

    // Colors dynamic values based on whether they increased or decreased from baseline.
    private static string FormatAddedValueText(int currentValue, SkillValueData valueData)
    {
        int baseline = valueData != null ? UnityEngine.Mathf.Max(0, valueData.baseAmount) : currentValue;
        string color = AddedValueColor;
        if (currentValue < baseline)
            color = ReducedValueColor;
        else if (currentValue > baseline)
            color = IncreasedValueColor;

        return ColorText(currentValue.ToString(), color);
    }

    // Wraps text in a TMP rich-text color tag.
    private static string ColorText(string text, string color) => "<color=" + color + ">" + text + "</color>";

    // Colors quoted numeric/value segments and symbolic X tokens.
    private static string ColorQuotedAddedValues(string text)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains("\""))
            return ColorSymbolicXTokens(text);

        StringBuilder sb = new StringBuilder(text.Length + 32);
        bool inQuote = false;
        int segmentStart = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != '"')
                continue;

            if (!inQuote)
            {
                sb.Append(text, segmentStart, i - segmentStart);
                segmentStart = i + 1;
                inQuote = true;
            }
            else
            {
                string value = text.Substring(segmentStart, i - segmentStart);
                sb.Append(Blue(value));
                segmentStart = i + 1;
                inQuote = false;
            }
        }

        if (segmentStart < text.Length)
        {
            if (inQuote)
                sb.Append('"');
            sb.Append(text, segmentStart, text.Length - segmentStart);
        }

        return ColorSymbolicXTokens(sb.ToString());
    }

    // Colors symbolic X tokens when they represent output values.
    private static string ColorSymbolicXTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return SymbolicXValueRegex.Replace(text, match => Blue(match.Value));
    }

    // Replaces quoted template segments with runtime-resolved text while preserving quote marks.
    private static string ReplaceQuotedSegments(string text, Func<string, string> resolver)
    {
        if (string.IsNullOrEmpty(text) || resolver == null || !text.Contains("\""))
            return text;

        StringBuilder sb = new StringBuilder(text.Length + 32);
        bool inQuote = false;
        int segmentStart = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != '"')
                continue;

            if (!inQuote)
            {
                sb.Append(text, segmentStart, i - segmentStart);
                sb.Append('"');
                segmentStart = i + 1;
                inQuote = true;
            }
            else
            {
                string value = text.Substring(segmentStart, i - segmentStart);
                sb.Append(resolver(value) ?? value);
                sb.Append('"');
                segmentStart = i + 1;
                inQuote = false;
            }
        }

        if (segmentStart < text.Length)
            sb.Append(text, segmentStart, text.Length - segmentStart);

        return sb.ToString();
    }
}

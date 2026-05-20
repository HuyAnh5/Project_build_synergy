using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/UI/Skill Tooltip Keyword Glossary", fileName = "SkillTooltipKeywordGlossary")]
public sealed class SkillTooltipKeywordGlossarySO : ScriptableObject
{
    public enum KeywordDescriptionMode
    {
        StaticText = 0,
        CurrentValueTemplate = 1,
    }

    [Serializable]
    public struct KeywordEntry
    {
        [Tooltip("Token used for lookup in tooltip text, for example Burn or ...")]
        public string keyword;

        [Tooltip("Optional UI label override. If empty, keyword is used.")]
        public string displayName;

        [Tooltip("Optional icon for glossary UI.")]
        public Sprite icon;

        [Tooltip("How this keyword resolves its description.")]
        public KeywordDescriptionMode descriptionMode;

        [TextArea(2, 6)]
        [Tooltip("Static description or template. Use {value} for the current-value keyword.")]
        public string description;
    }

    [SerializeField] private KeywordEntry[] entries = Array.Empty<KeywordEntry>();

    public KeywordEntry[] Entries => entries;

    public bool TryGetEntry(string keyword, out KeywordEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (string.Equals(entries[i].keyword, keyword, StringComparison.OrdinalIgnoreCase))
                {
                    entry = entries[i];
                    return true;
                }
            }
        }

        entry = default;
        return false;
    }

    public string ResolveDescription(KeywordEntry entry, string currentValueText)
    {
        switch (entry.descriptionMode)
        {
            case KeywordDescriptionMode.CurrentValueTemplate:
                return string.IsNullOrEmpty(entry.description)
                    ? currentValueText ?? string.Empty
                    : entry.description.Replace("{value}", currentValueText ?? string.Empty);
            default:
                return entry.description ?? string.Empty;
        }
    }

    public string ResolveDisplayName(KeywordEntry entry)
    {
        return string.IsNullOrWhiteSpace(entry.displayName) ? entry.keyword : entry.displayName;
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

// Entry point for converting skill assets/runtime data into tooltip text blocks.
public static partial class SkillTooltipFormatter
{
    private const string AddedValueColor = "#5CCBFF";
    private const string ReducedValueColor = "#FF5C7A";
    private const string IncreasedValueColor = "#67E88D";
    private static readonly Regex SymbolicXValueRegex = new Regex(@"\bX(?=(?:\s+(?:action|damage|guard|burn|bleed|hp|focus|ap))\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public struct TooltipContent
    {
        public string title;
        public string targeting;
        public string effectText;
        public List<string> requires;
        public List<string> conditions;
        public string costText;
        public ElementType? element;
    }

    public static TooltipContent BuildContent(ScriptableObject asset, SkillRuntime runtime = null, bool expanded = false)
    {
        TooltipContent content = new TooltipContent
        {
            title = GetTitle(asset),
            targeting = SkillUiMetadataUtility.BuildTargetingSummary(asset, runtime),
            effectText = string.Empty,
            requires = new List<string>(),
            conditions = new List<string>(),
            costText = expanded ? BuildCostText(asset) : string.Empty,
            element = ResolveElement(asset)
        };

        switch (asset)
        {
            case SkillDamageSO damage:
                FillDamageContent(ref content, damage, runtime ?? SkillRuntime.FromDamage(damage));
                break;
            case SkillBuffDebuffSO buffDebuff:
                FillBuffContent(ref content, buffDebuff, runtime ?? BuildBuffRuntime(buffDebuff));
                break;
            case SkillPassiveSO passive:
                string passiveDescription = BuildPassiveDescription(passive);
                content.effectText = string.IsNullOrWhiteSpace(passiveDescription)
                    ? string.Empty
                    : ColorQuotedAddedValues(passiveDescription.Trim());
                break;
            case ConsumableDataSO consumable:
                FillConsumableContent(ref content, consumable);
                break;
            case DiceFaceEnchantTooltipAsset diceEnchant:
                content.targeting = string.Empty;
                content.effectText = diceEnchant.IsBroken
                    ? "This face is Broken and cannot be used until the next combat."
                    : DiceFaceEnchantUtility.GetShortRulesText(diceEnchant.Enchant);
                break;
        }

        return content;
    }

    public static string GetTitle(ScriptableObject asset)
    {
        switch (asset)
        {
            case SkillDamageSO damage:
                return string.IsNullOrWhiteSpace(damage.displayName) ? damage.name : damage.displayName;
            case SkillBuffDebuffSO buffDebuff:
                return string.IsNullOrWhiteSpace(buffDebuff.displayName) ? buffDebuff.name : buffDebuff.displayName;
            case SkillPassiveSO passive:
                return string.IsNullOrWhiteSpace(passive.displayName) ? passive.name : passive.displayName;
            case ConsumableDataSO consumable:
                return string.IsNullOrWhiteSpace(consumable.displayName) ? consumable.name : consumable.displayName;
            case DiceFaceEnchantTooltipAsset diceEnchant:
                return diceEnchant.IsBroken
                    ? "Broken"
                    : DiceFaceEnchantUtility.GetDisplayName(diceEnchant.Enchant);
            default:
                return asset != null ? asset.name : string.Empty;
        }
    }

    private static void FillConsumableContent(ref TooltipContent content, ConsumableDataSO consumable)
    {
        if (consumable == null)
            return;

        content.targeting = $"{consumable.useContext} | {consumable.targetKind}";
        content.effectText = BuildConsumableEffectText(consumable);
    }

    private static string BuildConsumableEffectText(ConsumableDataSO consumable)
    {
        if (consumable == null)
            return string.Empty;

        string description = string.IsNullOrWhiteSpace(consumable.description)
            ? string.Empty
            : consumable.description.Trim();

        if (consumable.effectId == ConsumableEffectId.ApplyFaceEnchant &&
            consumable.faceEnchant != DiceFaceEnchantKind.None)
        {
            string enchantName = DiceFaceEnchantUtility.GetDisplayName(consumable.faceEnchant);
            string highlightedEnchant = FormatKeyword(enchantName);
            if (!string.IsNullOrEmpty(description))
                return description.Replace(enchantName, highlightedEnchant);

            return $"Apply {highlightedEnchant} enchant to 1 face permanently.";
        }

        return ColorQuotedAddedValues(description);
    }

    private static string BuildPassiveDescription(SkillPassiveSO passive)
    {
        if (passive == null)
            return string.Empty;

        string runtimeDescription = BuildRuntimePassiveDescription(passive);
        if (!string.IsNullOrWhiteSpace(runtimeDescription))
            return runtimeDescription;

        return passive.GetAuthoringDescription();
    }

    private static string BuildRuntimePassiveDescription(SkillPassiveSO passive)
    {
        if (passive == null || passive.effects == null || passive.effects.Count == 0)
            return string.Empty;

        List<string> lines = null;
        for (int i = 0; i < passive.effects.Count; i++)
        {
            PassiveEffectEntry effect = passive.effects[i];
            if (effect == null)
                continue;

            string line = BuildRuntimePassiveEffectDescription(effect);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (lines == null)
                lines = new List<string>();
            lines.Add(line);
        }

        return lines != null && lines.Count > 0 ? string.Join(" ", lines) : string.Empty;
    }

    private static string BuildRuntimePassiveEffectDescription(PassiveEffectEntry effect)
    {
        if (effect == null)
            return string.Empty;

        switch (effect.id)
        {
            case PassiveEffectId.FailDiceCountdownCombatAddedValue:
                return BuildFailCountdownCombatAddedValueDescription(effect);
            default:
                return string.Empty;
        }
    }

    private static string BuildFailCountdownCombatAddedValueDescription(PassiveEffectEntry effect)
    {
        int threshold = Mathf.Max(1, effect.valueI);
        int addedValue = Mathf.Max(0, effect.value2I);
        PassiveSystem passiveSystem = PassiveSystemRegistry.GetPlayer();
        int remaining = passiveSystem != null
            ? passiveSystem.GetFailDiceCountdownRemaining(threshold)
            : threshold;
        int currentBonus = passiveSystem != null
            ? passiveSystem.GetCombatAddedValueBonus()
            : 0;

        string coloredRemaining = ColorText(remaining.ToString(), IncreasedValueColor);
        string coloredCurrentBonus = ColorText($"+{currentBonus}", IncreasedValueColor);
        return $"Every '{coloredRemaining}' Fail dice used, gain +{addedValue} Added Value for this combat. ({coloredCurrentBonus})";
    }

    private static void FillDamageContent(ref TooltipContent content, SkillDamageSO skill, SkillRuntime runtime)
    {
        if (SkillGameplayResolver.CanResolveWithNewPipeline(skill))
        {
            FillGameplayDamageContent(ref content, skill, runtime);
            return;
        }

        switch (content.title)
        {
            case "Ignite":
                content.effectText = $"Apply {Blue(GetPreviewBurn(runtime).ToString())} Burn.";
                content.conditions.Add("If Crit: Apply +2 Burn.");
                return;

            case "Fire Slash":
                content.effectText = $"Deal {Blue(GetPreviewDamage(runtime).ToString())} damage.";
                content.conditions.Add("If Odd: Apply 6 Burn.");
                return;

            case SkillUsageRequirementUtility.HellfireTitle:
                SkillUsageRequirementUtility.AppendTooltipRequirements(runtime, content.requires);
                content.effectText = "Consume all Burn. Deal 3 damage each.";
                content.conditions.Add("For each consumed die with value 7:\nApply 7 Burn to the target.");
                return;

            case SkillUsageRequirementUtility.BurnConsumeTitle:
                SkillUsageRequirementUtility.AppendTooltipRequirements(runtime, content.requires);
                content.effectText = "Consume all Burn. Deal 2 damage each.";
                return;

            case "Bite the Dust":
                content.conditions.Add("Target has Burn: Consume all Burn, deal 1 damage each.");
                content.conditions.Add("For every 5 Burn consumed: Heal 3 HP.");
                return;
        }

        string description = BuildRuntimeDamageDescription(skill, runtime);
        content.effectText = string.IsNullOrWhiteSpace(description)
            ? string.Empty
            : ColorQuotedAddedValues(description.Trim());
    }

}

[System.Serializable]
public sealed class DiceFaceEnchantTooltipAsset : ScriptableObject
{
    [SerializeField] private DiceFaceEnchantKind enchant;
    [SerializeField] private int faceValue;
    [SerializeField] private string sourceDieName;
    [SerializeField] private bool isBroken;

    public DiceFaceEnchantKind Enchant => enchant;
    public int FaceValue => faceValue;
    public string SourceDieName => sourceDieName;
    public bool IsBroken => isBroken;

    public void Configure(DiceFaceEnchantKind enchantKind, int currentFaceValue, string dieName, bool broken = false)
    {
        enchant = enchantKind;
        faceValue = currentFaceValue;
        sourceDieName = dieName ?? string.Empty;
        isBroken = broken;
        name = broken ? "DiceFaceEnchant_Broken" : $"DiceFaceEnchant_{enchantKind}";
    }
}

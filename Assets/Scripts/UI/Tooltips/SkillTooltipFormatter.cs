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
            element = expanded ? ResolveElement(asset) : null
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
                content.effectText = string.IsNullOrWhiteSpace(passive.description)
                    ? string.Empty
                    : ColorQuotedAddedValues(passive.description.Trim());
                break;
        }

        return content;
    }

    public static string GetTitle(ScriptableObject asset)
    {
        switch (asset)
        {
            case SkillDamageSO damage:
                if (damage.coreAction == CoreAction.BasicStrike)
                    return "Basic Attack";
                if (damage.coreAction == CoreAction.BasicGuard)
                    return "Basic Guard";
                return string.IsNullOrWhiteSpace(damage.displayName) ? damage.name : damage.displayName;
            case SkillBuffDebuffSO buffDebuff:
                return string.IsNullOrWhiteSpace(buffDebuff.displayName) ? buffDebuff.name : buffDebuff.displayName;
            case SkillPassiveSO passive:
                return string.IsNullOrWhiteSpace(passive.displayName) ? passive.name : passive.displayName;
            default:
                return asset != null ? asset.name : string.Empty;
        }
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

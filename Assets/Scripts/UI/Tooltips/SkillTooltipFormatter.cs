using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class SkillTooltipFormatter
{
    private const string AddedValueColor = "#5CCBFF";

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

    private static void FillBuffContent(ref TooltipContent content, SkillBuffDebuffSO skill, SkillRuntime runtime)
    {
        switch (content.title)
        {
            case "Ember Weapon":
                content.effectText = "For the next 3 turns, Basic Attack gains +1 Added Value.";
                content.conditions.Add("If that Basic Attack Crits: Apply Burn equal to total damage dealt.");
                return;
        }

        string description = BuildRuntimeBuffDebuffDescription(skill, runtime);
        content.effectText = string.IsNullOrWhiteSpace(description)
            ? string.Empty
            : ColorQuotedAddedValues(description.Trim());
    }

    private static string BuildCostText(ScriptableObject asset)
    {
        return SkillUiMetadataUtility.TryGetSkillCosts(asset, out int focusCost, out int slotsRequired)
            ? $"{focusCost} AP / {slotsRequired} Dice"
            : string.Empty;
    }

    private static ElementType? ResolveElement(ScriptableObject asset)
    {
        return SkillUiMetadataUtility.TryGetElementType(asset, out ElementType element) ? element : null;
    }

    private static string BuildRuntimeDamageDescription(SkillDamageSO skill, SkillRuntime runtime)
    {
        if (skill == null)
            return string.Empty;

        string template = GetDamageTemplate(skill);
        if (runtime == null)
            return template;

        return ReplaceQuotedSegments(template, quoted => ResolveDamageQuotedSegment(runtime, quoted));
    }

    private static string BuildRuntimeBuffDebuffDescription(SkillBuffDebuffSO skill, SkillRuntime runtime)
    {
        if (skill == null)
            return string.Empty;

        string template = GetBuffDebuffTemplate(skill);
        if (runtime == null)
            return template;

        return ReplaceQuotedSegments(template, quoted => ResolveBuffQuotedSegment(skill, runtime, quoted));
    }

    private static string GetDamageTemplate(SkillDamageSO skill)
    {
        if (skill == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(skill.spec.normalizedText))
            return skill.spec.normalizedText.Trim();

        return (skill.description ?? string.Empty).Trim();
    }

    private static string GetBuffDebuffTemplate(SkillBuffDebuffSO skill)
    {
        if (skill == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(skill.spec.normalizedText))
            return skill.spec.normalizedText.Trim();

        return (skill.description ?? string.Empty).Trim();
    }

    private static string ResolveDamageQuotedSegment(SkillRuntime runtime, string quoted)
    {
        if (runtime == null)
            return quoted;

        string lower = (quoted ?? string.Empty).Trim().ToLowerInvariant();
        if (lower.Contains("damage"))
            return GetPreviewDamage(runtime) + " damage";
        if (lower.Contains("guard"))
            return GetPreviewGenericGuard(runtime) + " Guard";
        if (lower.Contains("burn") || lower == "burn")
            return GetPreviewBurn(runtime) + " Burn";
        if (lower.Contains("bleed") || lower == "bleed")
            return GetPreviewBleed(runtime) + " Bleed";

        return quoted;
    }

    private static string ResolveBuffQuotedSegment(SkillBuffDebuffSO skill, SkillRuntime runtime, string quoted)
    {
        if (skill == null || runtime == null)
            return quoted;

        string lower = (quoted ?? string.Empty).Trim().ToLowerInvariant();
        if (skill.effects == null)
            return quoted;

        for (int i = 0; i < skill.effects.Count; i++)
        {
            BuffDebuffEffectEntry effect = skill.effects[i];
            if (effect == null)
                continue;

            if ((lower.Contains("hp") || lower.Contains("heal")) && effect.id == BuffDebuffEffectId.HealFlat)
                return effect.GetHealAmount() + " HP";
            if ((lower.Contains("hp") || lower.Contains("heal")) && effect.id == BuffDebuffEffectId.HealByDiceSum)
                return ResolveX(runtime) + " HP";
            if (lower.Contains("focus") && effect.id == BuffDebuffEffectId.FocusDelayed)
                return effect.GetFocusAmount() + " Focus";
        }

        return quoted;
    }

    private static int GetPreviewDamage(SkillRuntime runtime)
    {
        if (runtime == null || runtime.kind != SkillKind.Attack)
            return 0;

        return AttackPreviewCalculator.BuildAttackPreview(runtime, null, null, GetResolvedValueSum(runtime)).finalDamage;
    }

    private static int GetPreviewBurn(SkillRuntime runtime)
    {
        if (runtime == null || !runtime.applyBurn)
            return 0;

        if (runtime.baseBurnValueMode == BaseEffectValueMode.X || runtime.fireApplyBurnFromResolvedValue)
            return SkillOutputValueUtility.ResolveXValue(GetResolvedValueSum(runtime), runtime);

        return SkillOutputValueUtility.AddActionAddedValue(runtime.burnAddStacks, runtime);
    }

    private static int GetPreviewBleed(SkillRuntime runtime)
    {
        if (runtime == null || !runtime.applyBleed)
            return 0;

        return SkillOutputValueUtility.AddActionAddedValue(Mathf.Max(0, runtime.bleedTurns), runtime);
    }

    private static int GetPreviewGenericGuard(SkillRuntime runtime)
    {
        if (runtime == null)
            return 0;

        if (runtime.kind == SkillKind.Guard)
        {
            if (runtime.guardValueMode == BaseEffectValueMode.Flat && runtime.guardFlat > 0)
                return SkillOutputValueUtility.AddActionAddedValue(runtime.guardFlat, runtime);

            return SkillBehaviorRuntimeUtility.GetPerDieResolvedOutput(runtime, 0);
        }

        if (runtime.guardValueMode == BaseEffectValueMode.Flat && runtime.guardFlat > 0)
            return SkillOutputValueUtility.AddActionAddedValue(runtime.guardFlat, runtime);

        return 0;
    }

    private static int GetResolvedValueSum(SkillRuntime runtime)
    {
        if (runtime == null || runtime.localResolvedValues == null)
            return 0;

        int total = 0;
        for (int i = 0; i < runtime.localResolvedValues.Count; i++)
            total += Mathf.Max(0, runtime.localResolvedValues[i]);
        return Mathf.Max(0, total);
    }

    private static int ResolveX(SkillRuntime runtime)
    {
        return SkillOutputValueUtility.ResolveXValue(0, runtime);
    }

    private static SkillRuntime BuildBuffRuntime(SkillBuffDebuffSO skill)
    {
        if (skill == null)
            return null;

        return new SkillRuntime
        {
            sourceAsset = skill,
            useV2Targeting = true,
            targetRuleV2 = skill.target,
            kind = SkillKind.Utility,
            target = SkillTargetRuleUtility.IsEnemySideTarget(skill.target) ? TargetRule.Enemy : TargetRule.Self,
            slotsRequired = Mathf.Clamp(skill.slotsRequired, 1, 3),
            focusCost = Mathf.Max(0, skill.focusCost),
            focusGainOnCast = skill.focusGainOnCast
        };
    }

    private static string Blue(string text) => "<color=" + AddedValueColor + ">" + text + "</color>";

    private static string ColorQuotedAddedValues(string text)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains("\""))
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

        return sb.ToString();
    }

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

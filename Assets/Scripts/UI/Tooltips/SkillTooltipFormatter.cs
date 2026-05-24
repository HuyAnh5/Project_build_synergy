using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class SkillTooltipFormatter
{
    private const string AddedValueColor = "#5CCBFF";
    private const string ReducedValueColor = "#FF5C7A";
    private const string IncreasedValueColor = "#67E88D";

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

    private static void FillGameplayDamageContent(ref TooltipContent content, SkillDamageSO skill, SkillRuntime runtime)
    {
        SkillResolvedResult resolved = SkillGameplayResolver.Resolve(runtime ?? SkillRuntime.FromDamage(skill), null, null);
        if (resolved == null)
        {
            content.effectText = string.Empty;
            return;
        }

        if (skill.gameplay != null && !string.IsNullOrWhiteSpace(skill.gameplay.descriptionTemplate))
        {
            content.effectText = ColorGameplayTemplate(skill.gameplay.descriptionTemplate, skill, runtime);
        }
        else
        {
        var lines = new List<string>();
        if (skill.gameplay != null && skill.gameplay.baseEffects != null)
        {
            for (int i = 0; i < skill.gameplay.baseEffects.Count; i++)
                lines.Add(FormatEffect(skill.gameplay.baseEffects[i], runtime));
        }
        content.effectText = string.Join("\n", lines);
        }

        if (skill.gameplay != null && skill.gameplay.requirements != null)
        {
            for (int i = 0; i < skill.gameplay.requirements.Count; i++)
            {
                SkillRequirementData requirement = skill.gameplay.requirements[i];
                if (requirement != null)
                    content.requires.Add(string.IsNullOrWhiteSpace(requirement.failureText) ? "Requirement must be met." : requirement.failureText);
            }
        }

        if (skill.gameplay != null && skill.gameplay.conditionalOutcomes != null)
        {
            for (int i = 0; i < skill.gameplay.conditionalOutcomes.Count; i++)
            {
                SkillConditionalOutcomeDataV2 branch = skill.gameplay.conditionalOutcomes[i];
                if (branch == null || branch.condition == null || branch.effects == null || branch.effects.Count == 0)
                    continue;

                string conditionText = FormatCondition(branch.condition);
                var branchLines = new List<string>();
                for (int effectIndex = 0; effectIndex < branch.effects.Count; effectIndex++)
                    branchLines.Add(FormatEffect(branch.effects[effectIndex], runtime));

                content.conditions.Add($"If {conditionText}: {string.Join(" ", branchLines)}");
            }
        }
    }

    private static string ColorGameplayTemplate(string template, SkillDamageSO skill, SkillRuntime runtime)
    {
        if (string.IsNullOrWhiteSpace(template) || skill == null || skill.gameplay == null)
            return string.Empty;

        string text = template.Trim();
        if (skill.gameplay.baseEffects != null)
        {
            for (int i = 0; i < skill.gameplay.baseEffects.Count; i++)
                text = ReplaceEffectTokens(text, $"base{i + 1}", skill.gameplay.baseEffects[i], runtime);
        }

        if (skill.gameplay.conditionalOutcomes != null)
        {
            for (int branchIndex = 0; branchIndex < skill.gameplay.conditionalOutcomes.Count; branchIndex++)
            {
                SkillConditionalOutcomeDataV2 branch = skill.gameplay.conditionalOutcomes[branchIndex];
                if (branch == null || branch.effects == null)
                    continue;

                for (int effectIndex = 0; effectIndex < branch.effects.Count; effectIndex++)
                    text = ReplaceEffectTokens(text, $"cond{branchIndex + 1}_{effectIndex + 1}", branch.effects[effectIndex], runtime);
            }
        }

        text = text.Replace("{Burn}", FormatKeyword("Burn"));
        text = text.Replace("{Odd}", "Odd");
        text = text.Replace("{Even}", "Even");
        text = text.Replace("{Crit}", FormatKeyword("Crit"));
        text = text.Replace("{Guard}", FormatKeyword("Guard"));
        text = text.Replace("{Consume}", "Consume");
        return text;
    }

    private static string ReplaceEffectTokens(string text, string token, SkillEffectData effect, SkillRuntime runtime)
    {
        if (effect == null || effect.value == null)
            return text;

        int value = SkillGameplayResolver.ResolveValue(effect.value, new SkillResolveContext
        {
            runtime = runtime,
            consumedBurnStacks = effect.value.status == StatusKind.Burn ? 1 : 0,
            consumedBleedStacks = effect.value.status == StatusKind.Bleed ? 1 : 0,
            totalAddedValue = SkillOutputValueUtility.GetTotalActionAddedValue(runtime)
        });
        string valueText = effect.value.mode == SkillValueMode.AddedValueScaled
            ? FormatAddedValueText(value, effect.value)
            : value.ToString();
        return text.Replace("{" + token + "}", valueText);
    }

    private static void AppendResolvedEffectLines(List<string> lines, List<ResolvedEffect> effects)
    {
        if (lines == null || effects == null)
            return;

        for (int i = 0; i < effects.Count; i++)
        {
            ResolvedEffect effect = effects[i];
            if (effect == null)
                continue;
            lines.Add(FormatResolvedEffect(effect));
        }
    }

    private static string FormatResolvedEffect(ResolvedEffect effect)
    {
        string value = effect.isBlueValue ? Blue(effect.value.ToString()) : effect.value.ToString();
        switch (effect.type)
        {
            case SkillEffectType.DealDamage:
                return $"Deal {value} damage.";
            case SkillEffectType.ApplyStatus:
                return $"Apply {value} {FormatKeyword(effect.status.ToString())}.";
            case SkillEffectType.ConsumeStatus:
                return $"Consume {FormatKeyword(effect.status.ToString())}.";
            case SkillEffectType.GainGuard:
                return $"Gain {value} {FormatKeyword("Guard")}.";
            case SkillEffectType.Heal:
                return $"Heal {value} HP.";
            case SkillEffectType.GainAP:
                return $"Gain {value} AP.";
            default:
                return effect.type.ToString();
        }
    }

    private static string FormatEffect(SkillEffectData effect, SkillRuntime runtime)
    {
        if (effect == null)
            return string.Empty;

        int value = SkillGameplayResolver.ResolveValue(effect.value, new SkillResolveContext
        {
            runtime = runtime,
            target = null,
            consumedBurnStacks = effect.value != null && effect.value.status == StatusKind.Burn ? 1 : 0,
            consumedBleedStacks = effect.value != null && effect.value.status == StatusKind.Bleed ? 1 : 0,
            totalAddedValue = SkillOutputValueUtility.GetTotalActionAddedValue(runtime)
        });
        string valueText = effect.value != null && effect.value.mode == SkillValueMode.AddedValueScaled
            ? FormatAddedValueText(value, effect.value)
            : value.ToString();
        if (effect.value != null && effect.value.mode == SkillValueMode.ConsumedStatusStacksScaled)
            valueText = $"{effect.value.baseAmount} per consumed {FormatKeyword(effect.value.status.ToString())}";
        else if (effect.value != null && effect.value.mode == SkillValueMode.ConsumedStatusStacksDividedScaled)
            valueText = $"{effect.value.baseAmount} per {Mathf.Max(1, effect.value.divisor)} consumed {FormatKeyword(effect.value.status.ToString())}";
        else if (effect.value != null && effect.value.mode == SkillValueMode.MatchingBaseValueCountScaled)
            valueText = $"{effect.value.baseAmount} per die rolled {effect.value.matchBaseValue}";
        else if (effect.value != null && effect.value.mode == SkillValueMode.TargetStatusStacksScaled)
            valueText = $"{effect.value.baseAmount} per target {FormatKeyword(effect.value.status.ToString())}";

        switch (effect.type)
        {
            case SkillEffectType.DealDamage:
                return $"Deal {valueText} damage.";
            case SkillEffectType.ApplyStatus:
                return $"Apply {valueText} {FormatKeyword(effect.status.ToString())}.";
            case SkillEffectType.ConsumeStatus:
                return $"Consume {FormatKeyword(effect.status.ToString())}.";
            case SkillEffectType.GainGuard:
                return $"Gain {valueText} {FormatKeyword("Guard")}.";
            default:
                return effect.type.ToString();
        }
    }

    private static string FormatCondition(SkillConditionData condition)
    {
        if (condition == null || condition.clauses == null || condition.clauses.Count == 0)
            return "condition";

        var parts = new List<string>();
        for (int i = 0; i < condition.clauses.Count; i++)
        {
            SkillConditionClause clause = condition.clauses[i];
            if (clause == null)
                continue;
            parts.Add(FormatClause(clause));
        }

        string joiner = condition.logic == SkillConditionLogic.Any ? " or " : " and ";
        return string.Join(joiner, parts);
    }

    private static string FormatClause(SkillConditionClause clause)
    {
        if (clause.reference == SkillConditionReference.AnyBaseValue && clause.comparison == SkillConditionComparison.IsOdd)
            return "Odd";
        if (clause.reference == SkillConditionReference.AnyBaseValue && clause.comparison == SkillConditionComparison.IsEven)
            return "Even";
        if (clause.reference == SkillConditionReference.AnyBaseValue && clause.comparison == SkillConditionComparison.Equals)
            return $"Base Value = {clause.value}";
        return clause.Summary;
    }

    private static string FormatKeyword(string text) => "<color=#FFD166>" + text + "</color>";

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

        if (SkillGameplayResolver.CanResolveWithNewPipeline(skill) && skill.gameplay != null && !string.IsNullOrWhiteSpace(skill.gameplay.descriptionTemplate))
            return skill.gameplay.descriptionTemplate.Trim();

        if (!string.IsNullOrWhiteSpace(skill.spec.normalizedText))
            return skill.spec.normalizedText.Trim();

        return skill.GetAuthoringDescription();
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

    private static string Blue(string text) => ColorText(text, AddedValueColor);

    private static string FormatAddedValueText(int currentValue, SkillValueData valueData)
    {
        int baseline = valueData != null ? Mathf.Max(0, valueData.baseAmount) : currentValue;
        string color = AddedValueColor;
        if (currentValue < baseline)
            color = ReducedValueColor;
        else if (currentValue > baseline)
            color = IncreasedValueColor;

        return ColorText(currentValue.ToString(), color);
    }

    private static string ColorText(string text, string color) => "<color=" + color + ">" + text + "</color>";

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

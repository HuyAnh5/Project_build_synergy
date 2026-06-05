using System.Collections.Generic;
using UnityEngine;

// Formats tooltip text for skills authored through the gameplay resolver pipeline.
public static partial class SkillTooltipFormatter
{
    // Fills effect, requirement, and conditional branch text from gameplay authoring data.
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

        AppendGameplayRequirements(ref content, skill);
        AppendGameplayConditionalOutcomes(ref content, skill, runtime);
    }

    // Adds requirement failure text to tooltip requirements.
    private static void AppendGameplayRequirements(ref TooltipContent content, SkillDamageSO skill)
    {
        if (skill.gameplay == null || skill.gameplay.requirements == null)
            return;

        for (int i = 0; i < skill.gameplay.requirements.Count; i++)
        {
            SkillRequirementData requirement = skill.gameplay.requirements[i];
            if (requirement != null)
                content.requires.Add(FormatRequirement(requirement));
        }
    }

    // Adds conditional outcome branches to tooltip condition text.
    private static void AppendGameplayConditionalOutcomes(ref TooltipContent content, SkillDamageSO skill, SkillRuntime runtime)
    {
        if (skill.gameplay == null || skill.gameplay.conditionalOutcomes == null)
            return;

        for (int i = 0; i < skill.gameplay.conditionalOutcomes.Count; i++)
        {
            SkillConditionalOutcomeDataV2 branch = skill.gameplay.conditionalOutcomes[i];
            if (branch == null || branch.condition == null || branch.effects == null || branch.effects.Count == 0)
                continue;

            content.conditions.Add(FormatConditionalOutcome(branch, runtime));
        }
    }

    private static string FormatRequirement(SkillRequirementData requirement)
    {
        if (requirement == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(requirement.tooltipText))
            return FormatAuthoredTooltipText(requirement.tooltipText);

        string conditionText = FormatCondition(requirement.condition);
        return string.IsNullOrWhiteSpace(conditionText) || conditionText == "condition"
            ? "Requirement must be met."
            : $"Only use if {conditionText}.";
    }

    private static string FormatConditionalOutcome(SkillConditionalOutcomeDataV2 branch, SkillRuntime runtime)
    {
        if (branch == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(branch.tooltipText))
            return FormatAuthoredTooltipText(branch.tooltipText);

        string conditionText = FormatCondition(branch.condition);
        var branchLines = new List<string>();
        if (branch.effects != null)
        {
            for (int effectIndex = 0; effectIndex < branch.effects.Count; effectIndex++)
                branchLines.Add(FormatEffect(branch.effects[effectIndex], runtime));
        }

        return $"If {conditionText}: {string.Join(" ", branchLines)}";
    }

    private static string FormatAuthoredTooltipText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return ColorSymbolicXTokens(ColorQuotedAddedValues(text.Trim()));
    }

    // Replaces effect and keyword tokens inside an authored gameplay description template.
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
        text = text.Replace("{Mark}", FormatKeyword("Mark"));
        text = text.Replace("{Odd}", "Odd");
        text = text.Replace("{Even}", "Even");
        text = text.Replace("{Crit}", FormatKeyword("Crit"));
        text = text.Replace("{Guard}", FormatKeyword("Guard"));
        text = text.Replace("{Consume}", "Consume");
        return ColorSymbolicXTokens(text);
    }

    // Replaces one effect value token in a gameplay template.
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
        string valueText = FormatTooltipValueText(value, effect.value);
        return text.Replace("{" + token + "}", valueText);
    }

    // Converts one authored effect into a readable tooltip sentence.
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
        string valueText = FormatTooltipValueText(value, effect.value);
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
            case SkillEffectType.DealSecondaryDamage:
                return $"Deal secondary {valueText} damage.";
            case SkillEffectType.ApplyStatus:
                return $"Apply {valueText} {FormatKeyword(effect.status.ToString())}.";
            case SkillEffectType.ConsumeStatus:
                return $"Consume {FormatKeyword(effect.status.ToString())}.";
            case SkillEffectType.GainGuard:
                return $"Gain {valueText} {FormatKeyword("Guard")}.";
            case SkillEffectType.ClearGuard:
                return $"Clear all {FormatKeyword("Guard")}.";
            default:
                return effect.type.ToString();
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

// Builds runtime-aware tooltip text for damage and buff/debuff descriptions.
public static partial class SkillTooltipFormatter
{
    // Fills utility/buff tooltip text from the buff/debuff gameplay schema.
    private static void FillBuffContent(ref TooltipContent content, SkillBuffDebuffSO skill, SkillRuntime runtime)
    {
        string description = BuildRuntimeBuffDebuffDescription(skill, runtime);
        content.effectText = string.IsNullOrWhiteSpace(description)
            ? string.Empty
            : ColorQuotedAddedValues(description.Trim());

        AppendBuffDebuffRequirements(ref content, skill);
        AppendBuffDebuffConditionalOutcomes(ref content, skill);
    }

    // Builds compact AP and dice cost text for expanded tooltips.
    private static string BuildCostText(ScriptableObject asset)
    {
        return SkillUiMetadataUtility.TryGetSkillCosts(asset, out int focusCost, out int slotsRequired)
            ? $"{focusCost} AP / {slotsRequired} Dice"
            : string.Empty;
    }

    // Resolves the optional element badge value for expanded tooltips.
    private static ElementType? ResolveElement(ScriptableObject asset)
    {
        return SkillUiMetadataUtility.TryGetElementType(asset, out ElementType element) ? element : null;
    }

    // Builds legacy damage text by replacing quoted template segments with runtime preview values.
    private static string BuildRuntimeDamageDescription(SkillDamageSO skill, SkillRuntime runtime)
    {
        if (skill == null)
            return string.Empty;

        string template = GetDamageTemplate(skill);
        if (runtime == null)
            return template;

        return ReplaceQuotedSegments(template, quoted => ResolveDamageQuotedSegment(runtime, quoted));
    }

    // Builds buff/debuff text by replacing quoted template segments with runtime preview values.
    private static string BuildRuntimeBuffDebuffDescription(SkillBuffDebuffSO skill, SkillRuntime runtime)
    {
        if (skill == null)
            return string.Empty;

        string template = GetBuffDebuffTemplate(skill);
        if (runtime == null)
            return template;

        return ReplaceQuotedSegments(template, quoted => ResolveBuffQuotedSegment(skill, runtime, quoted));
    }

    // Chooses the best available authored damage description template.
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

    // Chooses the best available authored buff/debuff description template.
    private static string GetBuffDebuffTemplate(SkillBuffDebuffSO skill)
    {
        if (skill == null)
            return string.Empty;

        if (skill.gameplay != null && !string.IsNullOrWhiteSpace(skill.gameplay.descriptionTemplate))
            return skill.gameplay.descriptionTemplate.Trim();

        string generated = BuildBuffDebuffFlowSummary(skill);
        if (!string.IsNullOrWhiteSpace(generated))
            return generated;

        if (!string.IsNullOrWhiteSpace(skill.spec.normalizedText))
            return skill.spec.normalizedText.Trim();

        return skill.GetAuthoringDescription();
    }

    // Resolves one quoted damage template segment into a runtime preview value.
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

    // Resolves one quoted buff/debuff template segment into a runtime preview value.
    private static string ResolveBuffQuotedSegment(SkillBuffDebuffSO skill, SkillRuntime runtime, string quoted)
    {
        return quoted;
    }

    private static string BuildBuffDebuffFlowSummary(SkillBuffDebuffSO skill)
    {
        if (skill == null || skill.gameplay == null || skill.gameplay.baseEffects == null)
            return string.Empty;

        List<string> lines = new List<string>();
        for (int i = 0; i < skill.gameplay.baseEffects.Count; i++)
        {
            BuffDebuffFlowEffectData effect = skill.gameplay.baseEffects[i];
            if (effect != null && effect.previewable)
                lines.Add(effect.Summary + ".");
        }

        return string.Join("\n", lines);
    }

    private static void AppendBuffDebuffRequirements(ref TooltipContent content, SkillBuffDebuffSO skill)
    {
        if (skill == null || skill.gameplay == null || skill.gameplay.requirements == null)
            return;

        for (int i = 0; i < skill.gameplay.requirements.Count; i++)
        {
            SkillRequirementData requirement = skill.gameplay.requirements[i];
            if (requirement != null)
                content.requires.Add(FormatRequirement(requirement));
        }
    }

    private static void AppendBuffDebuffConditionalOutcomes(ref TooltipContent content, SkillBuffDebuffSO skill)
    {
        if (skill == null || skill.gameplay == null || skill.gameplay.conditionalOutcomes == null)
            return;

        for (int i = 0; i < skill.gameplay.conditionalOutcomes.Count; i++)
        {
            BuffDebuffFlowConditionalOutcomeData branch = skill.gameplay.conditionalOutcomes[i];
            if (branch == null || branch.effects == null || branch.effects.Count == 0)
                continue;

            if (!string.IsNullOrWhiteSpace(branch.tooltipText))
            {
                content.conditions.Add(FormatAuthoredTooltipText(branch.tooltipText));
                continue;
            }

            string conditionText = FormatCondition(branch.condition);
            List<string> effects = new List<string>();
            for (int effectIndex = 0; effectIndex < branch.effects.Count; effectIndex++)
            {
                BuffDebuffFlowEffectData effect = branch.effects[effectIndex];
                if (effect != null && effect.previewable)
                    effects.Add(effect.Summary);
            }

            if (effects.Count > 0)
                content.conditions.Add($"If {conditionText}: {string.Join(" ", effects)}.");
        }
    }

    // Creates a minimal utility runtime so buff tooltip helpers can resolve costs/targets consistently.
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
}

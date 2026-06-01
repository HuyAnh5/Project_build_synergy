using UnityEngine;

// Builds runtime-aware tooltip text for legacy damage and buff/debuff descriptions.
public static partial class SkillTooltipFormatter
{
    // Fills utility/buff tooltip text, including special-case authored copy.
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

        if (!string.IsNullOrWhiteSpace(skill.spec.normalizedText))
            return skill.spec.normalizedText.Trim();

        return (skill.description ?? string.Empty).Trim();
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

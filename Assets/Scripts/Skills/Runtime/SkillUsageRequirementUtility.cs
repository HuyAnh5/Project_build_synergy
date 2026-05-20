using UnityEngine;

public static class SkillUsageRequirementUtility
{
    public const string HellfireTitle = "Hellfire";
    public const string BurnConsumeTitle = "Burn Consume";
    public const string TargetHasBurnText = "Target has Burn.";

    public static bool IsTargetRequirementMet(SkillRuntime runtime, CombatActor target)
    {
        return TryValidateTargetRequirement(runtime, target, out _);
    }

    public static bool HasAnyRequirement(SkillRuntime runtime)
    {
        return RequiresTargetBurn(runtime);
    }

    public static void AppendTooltipRequirements(SkillRuntime runtime, System.Collections.Generic.List<string> requires)
    {
        if (requires == null || runtime == null)
            return;

        if (RequiresTargetBurn(runtime))
            requires.Add(TargetHasBurnText);
    }

    public static bool TryValidateTargetRequirement(SkillRuntime runtime, CombatActor target, out string reason)
    {
        reason = string.Empty;

        if (runtime == null)
        {
            reason = "runtime == null";
            return false;
        }

        if (!RequiresTargetBurn(runtime))
            return true;

        if (target == null)
        {
            reason = "target == null";
            return false;
        }

        if (target.status == null || target.status.burnStacks <= 0)
        {
            reason = "target requires Burn";
            return false;
        }

        return true;
    }

    private static bool RequiresTargetBurn(SkillRuntime runtime)
    {
        if (runtime == null)
            return false;

        if (runtime.sourceAsset is SkillDamageSO damage)
        {
            string title = !string.IsNullOrWhiteSpace(damage.displayName) ? damage.displayName : damage.name;
            if (title == HellfireTitle || title == BurnConsumeTitle)
                return true;
        }

        return false;
    }
}

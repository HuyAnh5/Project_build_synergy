using UnityEngine;

// Resolves numeric preview values used by tooltip text.
public static partial class SkillTooltipFormatter
{
    // Calculates preview damage for an attack runtime.
    private static int GetPreviewDamage(SkillRuntime runtime)
    {
        if (runtime == null || runtime.kind != SkillKind.Attack)
            return 0;

        return AttackPreviewCalculator.BuildAttackPreview(runtime, null, null, GetResolvedValueSum(runtime)).finalDamage;
    }

    // Calculates preview Burn stacks from runtime flags.
    private static int GetPreviewBurn(SkillRuntime runtime)
    {
        if (runtime == null || !runtime.applyBurn)
            return 0;

        if (runtime.baseBurnValueMode == BaseEffectValueMode.X || runtime.fireApplyBurnFromResolvedValue)
            return SkillOutputValueUtility.ResolveXValue(GetResolvedValueSum(runtime), runtime);

        return SkillOutputValueUtility.AddActionAddedValue(runtime.burnAddStacks, runtime);
    }

    // Calculates preview Bleed stacks from runtime flags.
    private static int GetPreviewBleed(SkillRuntime runtime)
    {
        if (runtime == null || !runtime.applyBleed)
            return 0;

        return SkillOutputValueUtility.AddActionAddedValue(Mathf.Max(0, runtime.bleedTurns), runtime);
    }

    // Calculates preview Guard for guard skills or conditional guard effects.
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

    // Sums resolved dice values for preview calculations.
    private static int GetResolvedValueSum(SkillRuntime runtime)
    {
        if (runtime == null || runtime.localResolvedValues == null)
            return 0;

        int total = 0;
        for (int i = 0; i < runtime.localResolvedValues.Count; i++)
            total += Mathf.Max(0, runtime.localResolvedValues[i]);
        return Mathf.Max(0, total);
    }

    // Resolves generic X text for utility skill previews.
    private static int ResolveX(SkillRuntime runtime)
    {
        return SkillOutputValueUtility.ResolveXValue(0, runtime);
    }

    // Formats a value and colors it when the value is Added Value dependent.
    private static string FormatTooltipValueText(int currentValue, SkillValueData valueData)
    {
        if (valueData == null)
            return currentValue.ToString();

        return IsBlueValueMode(valueData.mode)
            ? FormatAddedValueText(currentValue, valueData)
            : currentValue.ToString();
    }

    // Identifies value modes that should use dynamic Added Value coloring.
    private static bool IsBlueValueMode(SkillValueMode mode)
    {
        return mode == SkillValueMode.AddedValueScaled || mode == SkillValueMode.ActionX;
    }
}

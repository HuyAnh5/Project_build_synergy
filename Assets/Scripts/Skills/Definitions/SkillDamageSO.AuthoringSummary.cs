using System.Collections.Generic;

public partial class SkillDamageSO
{
    public string GetAuthoringDescription()
    {
        if (gameplay != null && !string.IsNullOrWhiteSpace(gameplay.descriptionTemplate))
        {
            return gameplay.descriptionTemplate.Trim();
        }

        return (description ?? string.Empty).Trim();
    }

    private string BuildGameplaySummary()
    {
        if (gameplay == null)
        {
            return "No gameplay data.";
        }

        List<string> lines = new List<string>();
        lines.Add(gameplay.useNewGameplayPipeline ? "Pipeline: New Gameplay" : "Pipeline: Legacy fallback");

        if (gameplay.requirements == null || gameplay.requirements.Count == 0)
        {
            lines.Add("Requirements: None");
        }
        else
        {
            lines.Add($"Requirements: {gameplay.requirements.Count}");
        }

        if (gameplay.baseEffects == null || gameplay.baseEffects.Count == 0)
        {
            lines.Add("Base Effects: None");
        }
        else
        {
            lines.Add("Base Effects:");
            for (int i = 0; i < gameplay.baseEffects.Count; i++)
            {
                SkillEffectData effect = gameplay.baseEffects[i];
                lines.Add($"- {(effect != null ? effect.Summary : "<null>")}");
            }
        }

        if (gameplay.conditionalOutcomes == null || gameplay.conditionalOutcomes.Count == 0)
        {
            lines.Add("Conditional Outcomes: None");
        }
        else
        {
            lines.Add("Conditional Outcomes:");
            for (int i = 0; i < gameplay.conditionalOutcomes.Count; i++)
            {
                SkillConditionalOutcomeDataV2 branch = gameplay.conditionalOutcomes[i];
                int effectCount = branch != null && branch.effects != null ? branch.effects.Count : 0;
                lines.Add($"- Branch {i + 1}: {effectCount} effect(s)");
            }
        }

        return string.Join("\n", lines);
    }

    private static IEnumerable<DiceParityConditionPreset> GetDiceParityOptions()
    {
        yield return DiceParityConditionPreset.Even;
        yield return DiceParityConditionPreset.Odd;
    }

    private static IEnumerable<CritFailConditionPreset> GetCritFailOptions()
    {
        yield return CritFailConditionPreset.Crit;
        yield return CritFailConditionPreset.Fail;
    }

    private static IEnumerable<ResourceConditionPreset> GetResourceOptions()
    {
        yield return ResourceConditionPreset.CurrentFocusGreaterOrEqualN;
        yield return ResourceConditionPreset.PlayerGuardGreaterOrEqualN;
        yield return ResourceConditionPreset.TargetGuardGreaterOrEqualN;
    }

    private static IEnumerable<BoardStateConditionPreset> GetBoardStateOptions()
    {
        yield return BoardStateConditionPreset.AliveEnemiesGreaterOrEqualN;
        yield return BoardStateConditionPreset.EnemiesWithStatusGreaterOrEqualN;
    }
}

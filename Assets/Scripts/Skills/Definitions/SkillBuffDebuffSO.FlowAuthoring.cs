using System.Collections.Generic;
using Sirenix.OdinInspector;

public partial class SkillBuffDebuffSO
{
    [TabGroup("Tabs", "Gameplay")]
    [TitleGroup("Tabs/Gameplay/New Buff Debuff Schema", "Flow-focused effects for dice, cast momentum, and configurable buffs.")]
    [HideLabel]
    public BuffDebuffFlowData gameplay = new BuffDebuffFlowData();

    [TabGroup("Tabs", "Gameplay")]
    [TitleGroup("Tabs/Gameplay/Summary")]
    [ShowInInspector, ReadOnly, HideLabel, MultiLineProperty(6)]
    public string GameplaySummary => BuildFlowGameplaySummary();

    public bool UsesNewBuffDebuffPipeline()
        => true;

    public string GetAuthoringDescription()
    {
        if (gameplay != null && !string.IsNullOrWhiteSpace(gameplay.descriptionTemplate))
            return gameplay.descriptionTemplate.Trim();

        return (description ?? string.Empty).Trim();
    }

    [TabGroup("Tabs", "Gameplay")]
    [TitleGroup("Tabs/Gameplay/Quick Add", "Common flow effects", Alignment = TitleAlignments.Centered)]
    [ButtonGroup("Tabs/Gameplay/Quick Add/Row1")]
    [Button("Reroll Used Dice", ButtonSizes.Medium)]
    private void AddFlowReroll()
    {
        AddFlowEffect(BuffDebuffFlowEffectType.RerollUsedDice);
    }

    [ButtonGroup("Tabs/Gameplay/Quick Add/Row1")]
    [Button("Transform High", ButtonSizes.Medium)]
    private void AddFlowTransformHigh()
    {
        AddFlowEffect(BuffDebuffFlowEffectType.TransformUsedDiceHigh);
    }

    [ButtonGroup("Tabs/Gameplay/Quick Add/Row1")]
    [Button("Transform Low", ButtonSizes.Medium)]
    private void AddFlowTransformLow()
    {
        AddFlowEffect(BuffDebuffFlowEffectType.TransformUsedDiceLow);
    }

    [ButtonGroup("Tabs/Gameplay/Quick Add/Row2")]
    [Button("Repeat Next Turn", ButtonSizes.Medium)]
    private void AddFlowRepeatNextTurn()
    {
        AddFlowEffect(BuffDebuffFlowEffectType.RepeatFirstSkillNextTurn, e => e.amount = 1);
    }

    [ButtonGroup("Tabs/Gameplay/Quick Add/Row2")]
    [Button("Next Skill +4", ButtonSizes.Medium)]
    private void AddFlowNextSkillAddValue()
    {
        AddFlowEffect(BuffDebuffFlowEffectType.NextSkillAddValue, e => e.amount = 4);
    }

    [ButtonGroup("Tabs/Gameplay/Quick Add/Row2")]
    [Button("Ember Weapon", ButtonSizes.Medium)]
    private void AddFlowEmberWeapon()
    {
        AddFlowEffect(BuffDebuffFlowEffectType.EmberWeapon, e =>
        {
            e.amount = 1;
            e.durationTurns = 3;
            e.emberBurnEqualsFinalDamage = true;
            e.emberBurnOnCritOnly = true;
            e.emberBurnTurns = 3;
        });
    }

    private void AddFlowEffect(BuffDebuffFlowEffectType type, System.Action<BuffDebuffFlowEffectData> init = null)
    {
        if (gameplay == null)
            gameplay = new BuffDebuffFlowData();
        if (gameplay.baseEffects == null)
            gameplay.baseEffects = new List<BuffDebuffFlowEffectData>();

        BuffDebuffFlowEffectData effect = new BuffDebuffFlowEffectData { type = type };
        switch (type)
        {
            case BuffDebuffFlowEffectType.RerollUsedDice:
            case BuffDebuffFlowEffectType.TransformUsedDiceHigh:
            case BuffDebuffFlowEffectType.TransformUsedDiceLow:
                effect.target = BuffDebuffFlowTarget.UsedDice;
                break;
            case BuffDebuffFlowEffectType.RepeatFirstSkillNextTurn:
                effect.amount = 1;
                break;
            case BuffDebuffFlowEffectType.NextSkillAddValue:
                effect.amount = 4;
                break;
            case BuffDebuffFlowEffectType.EmberWeapon:
                effect.amount = 1;
                effect.durationTurns = 3;
                effect.emberBurnEqualsFinalDamage = true;
                effect.emberBurnOnCritOnly = true;
                effect.emberBurnTurns = 3;
                break;
        }

        init?.Invoke(effect);
        gameplay.baseEffects.Add(effect);
    }

    private string BuildFlowGameplaySummary()
    {
        if (gameplay == null)
            return "No buff/debuff gameplay data.";

        List<string> lines = new List<string>();
        lines.Add("Pipeline: Buff/Debuff Gameplay");

        if (gameplay.requirements == null || gameplay.requirements.Count == 0)
            lines.Add("Requirements: None");
        else
            lines.Add($"Requirements: {gameplay.requirements.Count}");

        if (gameplay.baseEffects == null || gameplay.baseEffects.Count == 0)
        {
            lines.Add("Base Effects: None");
        }
        else
        {
            lines.Add("Base Effects:");
            for (int i = 0; i < gameplay.baseEffects.Count; i++)
            {
                BuffDebuffFlowEffectData effect = gameplay.baseEffects[i];
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
                BuffDebuffFlowConditionalOutcomeData branch = gameplay.conditionalOutcomes[i];
                int effectCount = branch != null && branch.effects != null ? branch.effects.Count : 0;
                lines.Add($"- Branch {i + 1}: {effectCount} effect(s)");
            }
        }

        return string.Join("\n", lines);
    }
}

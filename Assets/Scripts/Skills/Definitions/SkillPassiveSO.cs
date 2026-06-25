// SkillPassiveSO.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Passive effects are intentionally concrete and id-driven.
/// Runtime behavior lives in PassiveSystem hooks, not generic custom condition data.
/// </summary>
public enum PassiveEffectId
{
    CritFocusOnUsedDie = 1,
    GuardCounterDamage = 2,
    BloodCounterNextAttackDamage = 3,
    GuardBreakMark = 4,
    MeleeFollowUpDamage = 5,
    MinimumImpactDamage = 6,
    RollThreeRandomEnemyDamage = 7
}

[Serializable, InlineProperty]
public class PassiveEffectEntry
{
    [ShowInInspector, ReadOnly, HideLabel]
    [PropertyOrder(-20)]
    public string Summary => BuildSummary();

    [LabelText("Type")]
    public PassiveEffectId id = PassiveEffectId.CritFocusOnUsedDie;

    [LabelText("Value")]
    public int valueI = 1;

    public void ApplyDefaults()
    {
        if (valueI == 0)
            valueI = 1;
    }

    public string BuildSummary()
    {
        int value = Mathf.Max(0, valueI);
        switch (id)
        {
            case PassiveEffectId.CritFocusOnUsedDie:
                return $"When using a Crit die, gain {value} AP.";
            case PassiveEffectId.GuardCounterDamage:
                return $"When an enemy hits your Guard, deal {value} counter damage.";
            case PassiveEffectId.BloodCounterNextAttackDamage:
                return $"When an enemy hits your HP, your next attack gains +{value} Added Value.";
            case PassiveEffectId.GuardBreakMark:
                return "When your Guard breaks, apply Mark to the attacker.";
            case PassiveEffectId.MeleeFollowUpDamage:
                return $"Each Melee hit deals +{value} follow-up damage.";
            case PassiveEffectId.MinimumImpactDamage:
                return $"Damage you deal below {value} becomes {value}.";
            case PassiveEffectId.RollThreeRandomEnemyDamage:
                return $"Whenever you roll a 3, deal {value} damage to a random front enemy.";
            default:
                return id.ToString();
        }
    }
}

[CreateAssetMenu(menuName = "Game/Skill/Passive", fileName = "SkillPassive_")]
public class SkillPassiveSO : ScriptableObject
{
    [TabGroup("Tabs", "Core")]
    [HorizontalGroup("Tabs/Core/Header", Width = 74)]
    [HideLabel, PreviewField(70, ObjectFieldAlignment.Left)]
    public Sprite icon;

    [TabGroup("Tabs", "Core")]
    [VerticalGroup("Tabs/Core/Header/Info")]
    [LabelText("Display Name")]
    public string displayName;

    [TabGroup("Tabs", "Core")]
    [VerticalGroup("Tabs/Core/Header/Info")]
    [LabelText("Description")]
    [InfoBox("Write passive tooltip text here. Preview and tooltip read this field; leave empty to auto-generate from Base Effects.", InfoMessageType.Info)]
    [TextArea(2, 5)]
    [ShowInInspector]
    private string DescriptionTemplate
    {
        get => description;
        set => description = value;
    }

    [TabGroup("Tabs", "Core")]
    [TextArea(2, 4)]
    [HideInInspector]
    public string description;

    [TabGroup("Tabs", "Core")]
    [BoxGroup("Tabs/Core/Metadata")]
    [HideLabel]
    [InlineProperty]
    public SkillSpecMetadata spec = new SkillSpecMetadata();

    [TabGroup("Tabs", "Gameplay")]
    [TitleGroup("Tabs/Gameplay/Passive Schema", "Concrete id-driven passive effects.")]
    [BoxGroup("Tabs/Gameplay/Passive Schema/Base Effects")]
    [InfoBox("Effects are handled by PassiveSystem. Add one concrete passive effect per row.", InfoMessageType.Info)]
    [HideLabel]
    [ListDrawerSettings(DefaultExpandedState = true, DraggableItems = true, ShowIndexLabels = false, ListElementLabelName = "Summary")]
    public List<PassiveEffectEntry> effects = new();

    [TabGroup("Tabs", "Gameplay")]
    [TitleGroup("Tabs/Gameplay/Summary")]
    [ShowInInspector, ReadOnly, HideLabel, MultiLineProperty(6)]
    private string GameplaySummary => BuildGameplaySummary();

    [TabGroup("Tabs", "Gameplay")]
    [TitleGroup("Tabs/Gameplay/Quick Add", "Quick Add")]
    [ButtonGroup("Tabs/Gameplay/Quick Add/Row1")]
    [Button("Crit +1 AP")]
    private void AddCritFocus() => AddEffect(e => { e.id = PassiveEffectId.CritFocusOnUsedDie; e.valueI = 1; });

    [ButtonGroup("Tabs/Gameplay/Quick Add/Row1")]
    [Button("Guard Counter")]
    private void AddGuardCounter() => AddEffect(e => { e.id = PassiveEffectId.GuardCounterDamage; e.valueI = 1; });

    [ButtonGroup("Tabs/Gameplay/Quick Add/Row1")]
    [Button("Blood Counter")]
    private void AddBloodCounter() => AddEffect(e => { e.id = PassiveEffectId.BloodCounterNextAttackDamage; e.valueI = 1; });

    [ButtonGroup("Tabs/Gameplay/Quick Add/Row2")]
    [Button("Guard Break Mark")]
    private void AddGuardBreakMark() => AddEffect(e => { e.id = PassiveEffectId.GuardBreakMark; e.valueI = 1; });

    [ButtonGroup("Tabs/Gameplay/Quick Add/Row2")]
    [Button("Melee Follow-up")]
    private void AddMeleeFollowUp() => AddEffect(e => { e.id = PassiveEffectId.MeleeFollowUpDamage; e.valueI = 1; });

    [ButtonGroup("Tabs/Gameplay/Quick Add/Row2")]
    [Button("Minimum Impact 5")]
    private void AddMinimumImpact() => AddEffect(e => { e.id = PassiveEffectId.MinimumImpactDamage; e.valueI = 5; });

    [ButtonGroup("Tabs/Gameplay/Quick Add/Row3")]
    [Button("Roll 3 Hit")]
    private void AddRollThreeHit() => AddEffect(e => { e.id = PassiveEffectId.RollThreeRandomEnemyDamage; e.valueI = 1; });

    private void AddEffect(Action<PassiveEffectEntry> init)
    {
        if (effects == null)
            effects = new List<PassiveEffectEntry>();

        PassiveEffectEntry effect = new PassiveEffectEntry();
        effect.ApplyDefaults();
        init?.Invoke(effect);
        effects.Add(effect);
    }

    public string GetAuthoringDescription()
    {
        if (!string.IsNullOrWhiteSpace(description))
            return description.Trim();

        return BuildDefaultDescription();
    }

    private string BuildGameplaySummary()
    {
        if (effects == null || effects.Count == 0)
            return "Passive Gameplay\nBase Effects: None";

        List<string> lines = new List<string>
        {
            "Passive Gameplay",
            "Base Effects:"
        };

        for (int i = 0; i < effects.Count; i++)
        {
            PassiveEffectEntry effect = effects[i];
            if (effect == null)
                continue;

            lines.Add($"- {effect.BuildSummary()}");
        }

        return string.Join("\n", lines);
    }

    private string BuildDefaultDescription()
    {
        if (effects == null || effects.Count == 0)
            return string.Empty;

        List<string> lines = new List<string>();
        for (int i = 0; i < effects.Count; i++)
        {
            PassiveEffectEntry effect = effects[i];
            if (effect == null)
                continue;

            lines.Add(effect.BuildSummary());
        }

        return string.Join(" ", lines).Trim();
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = name;

        if (effects == null)
            effects = new List<PassiveEffectEntry>();

        for (int i = 0; i < effects.Count; i++)
            effects[i]?.ApplyDefaults();
    }
}

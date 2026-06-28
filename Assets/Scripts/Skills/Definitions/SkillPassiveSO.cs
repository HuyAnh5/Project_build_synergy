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
    RollThreeRandomEnemyDamage = 7,
    FailDieNextSkillAddedValue = 8,
    MarkPayoffMinHits = 9,
    FailDiceCountdownCombatAddedValue = 10,
    RangedHitChanceApplyMark = 11,
    LowHpRefillApOncePerCombat = 12,
    RandomCommonPassiveThisCombat = 13,
    OneTimeReviveThenEmptySlot = 14
}

[Serializable, InlineProperty]
public class PassiveEffectEntry
{
    [ShowInInspector, ReadOnly, HideLabel]
    [PropertyOrder(-20)]
    public string Summary => BuildSummary();

    [LabelText("Type")]
    [ValueDropdown(nameof(GetPassiveBehaviorOptions), DropdownTitle = "Passive Behavior")]
    public PassiveEffectId id = PassiveEffectId.CritFocusOnUsedDie;

    [LabelText("Value")]
    [ShowIf(nameof(UsesValue))]
    public int valueI = 1;

    [LabelText("Value 2")]
    [ShowIf(nameof(UsesValue2))]
    public int value2I = 0;

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
            case PassiveEffectId.FailDieNextSkillAddedValue:
                return $"When using a Fail die, your next skill this turn gains +{value} Added Value.";
            case PassiveEffectId.MarkPayoffMinHits:
                return "Mark you apply lasts for at least 2 payoff hits.";
            case PassiveEffectId.FailDiceCountdownCombatAddedValue:
                return $"Every {value} Fail dice used, gain +{Mathf.Max(0, value2I)} Added Value for this combat.";
            case PassiveEffectId.RangedHitChanceApplyMark:
                return $"Ranged hits have a {Mathf.Clamp(value, 0, 100)}% chance to apply Mark.";
            case PassiveEffectId.LowHpRefillApOncePerCombat:
                return $"Once per combat, when HP falls to {Mathf.Clamp(value, 1, 100)}% or lower, refill AP.";
            case PassiveEffectId.RandomCommonPassiveThisCombat:
                return "At combat start, gain a random Common passive for this combat.";
            case PassiveEffectId.OneTimeReviveThenEmptySlot:
                return $"Once, revive with {Mathf.Clamp(value, 1, 100)}% HP, then empty this passive slot.";
            default:
                return id.ToString();
        }
    }

    private static IEnumerable<ValueDropdownItem<PassiveEffectId>> GetPassiveBehaviorOptions()
    {
        yield return new ValueDropdownItem<PassiveEffectId>("Crit / Used Crit Die -> Gain AP", PassiveEffectId.CritFocusOnUsedDie);
        yield return new ValueDropdownItem<PassiveEffectId>("Guard / Counter Damage When Guard Blocks", PassiveEffectId.GuardCounterDamage);
        yield return new ValueDropdownItem<PassiveEffectId>("Blood / Next Attack Damage After HP Hit", PassiveEffectId.BloodCounterNextAttackDamage);
        yield return new ValueDropdownItem<PassiveEffectId>("Guard Break / Apply Mark To Attacker", PassiveEffectId.GuardBreakMark);
        yield return new ValueDropdownItem<PassiveEffectId>("Melee / Follow-up Damage", PassiveEffectId.MeleeFollowUpDamage);
        yield return new ValueDropdownItem<PassiveEffectId>("Damage / Minimum Impact", PassiveEffectId.MinimumImpactDamage);
        yield return new ValueDropdownItem<PassiveEffectId>("Dice / Roll Value 3 Random Hit", PassiveEffectId.RollThreeRandomEnemyDamage);
        yield return new ValueDropdownItem<PassiveEffectId>("Fail / Used Fail Die -> Next Skill +Value", PassiveEffectId.FailDieNextSkillAddedValue);
        yield return new ValueDropdownItem<PassiveEffectId>("Mark / Payoff Lasts Multiple Hits", PassiveEffectId.MarkPayoffMinHits);
        yield return new ValueDropdownItem<PassiveEffectId>("Fail / Dice Countdown -> Combat +Value", PassiveEffectId.FailDiceCountdownCombatAddedValue);
        yield return new ValueDropdownItem<PassiveEffectId>("Range / Hit Chance -> Apply Mark", PassiveEffectId.RangedHitChanceApplyMark);
        yield return new ValueDropdownItem<PassiveEffectId>("HP / Low HP Percent -> Refill AP", PassiveEffectId.LowHpRefillApOncePerCombat);
        yield return new ValueDropdownItem<PassiveEffectId>("Combat / Random Common Passive Behavior", PassiveEffectId.RandomCommonPassiveThisCombat);
        yield return new ValueDropdownItem<PassiveEffectId>("Death / One-Time Revive Then Empty Slot", PassiveEffectId.OneTimeReviveThenEmptySlot);
    }

    private bool UsesValue()
    {
        switch (id)
        {
            case PassiveEffectId.CritFocusOnUsedDie:
            case PassiveEffectId.GuardCounterDamage:
            case PassiveEffectId.BloodCounterNextAttackDamage:
            case PassiveEffectId.MeleeFollowUpDamage:
            case PassiveEffectId.MinimumImpactDamage:
            case PassiveEffectId.RollThreeRandomEnemyDamage:
            case PassiveEffectId.FailDieNextSkillAddedValue:
            case PassiveEffectId.FailDiceCountdownCombatAddedValue:
            case PassiveEffectId.RangedHitChanceApplyMark:
            case PassiveEffectId.LowHpRefillApOncePerCombat:
            case PassiveEffectId.OneTimeReviveThenEmptySlot:
                return true;
            default:
                return false;
        }
    }

    private bool UsesValue2()
        => id == PassiveEffectId.FailDiceCountdownCombatAddedValue;
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
    [InfoBox("Write passive tooltip text here. Preview and tooltip read this field; leave empty to auto-generate from Behaviors.", InfoMessageType.Info)]
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
    [TitleGroup("Tabs/Gameplay/Passive Behaviors", "Choose an existing behavior. Ask Codex to add new passive behavior code when the idea is unique.")]
    [HideLabel]
    [ListDrawerSettings(DefaultExpandedState = true, DraggableItems = true, ShowIndexLabels = false, ListElementLabelName = "Summary")]
    public List<PassiveEffectEntry> effects = new();

    [TabGroup("Tabs", "Gameplay")]
    [TitleGroup("Tabs/Gameplay/Summary")]
    [ShowInInspector, ReadOnly, HideLabel, MultiLineProperty(6)]
    private string GameplaySummary => BuildGameplaySummary();

    [TabGroup("Tabs", "Gameplay")]
    [TitleGroup("Tabs/Gameplay/Quick Add", "Behavior presets")]
    [ButtonGroup("Tabs/Gameplay/Quick Add/Row1")]
    [Button("Crit +1 AP")]
    private void AddCritFocus() => AddEffect(e => { e.id = PassiveEffectId.CritFocusOnUsedDie; e.valueI = 1; });

    [ButtonGroup("Tabs/Gameplay/Quick Add/Row1")]
    [Button("Minimum Impact 5")]
    private void AddMinimumImpact() => AddEffect(e => { e.id = PassiveEffectId.MinimumImpactDamage; e.valueI = 5; });

    [ButtonGroup("Tabs/Gameplay/Quick Add/Row1")]
    [Button("Roll 3 Hit")]
    private void AddRollThreeHit() => AddEffect(e => { e.id = PassiveEffectId.RollThreeRandomEnemyDamage; e.valueI = 1; });

    [ButtonGroup("Tabs/Gameplay/Quick Add/Row2")]
    [Button("Melee Follow-up")]
    private void AddMeleeFollowUp() => AddEffect(e => { e.id = PassiveEffectId.MeleeFollowUpDamage; e.valueI = 1; });

    [ButtonGroup("Tabs/Gameplay/Quick Add/Row2")]
    [Button("Guard Counter")]
    private void AddGuardCounter() => AddEffect(e => { e.id = PassiveEffectId.GuardCounterDamage; e.valueI = 1; });

    [ButtonGroup("Tabs/Gameplay/Quick Add/Row2")]
    [Button("Blood Counter")]
    private void AddBloodCounter() => AddEffect(e => { e.id = PassiveEffectId.BloodCounterNextAttackDamage; e.valueI = 1; });

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
        List<string> lines = new List<string>
        {
            "Passive Gameplay",
            "Behaviors:"
        };

        if (effects == null || effects.Count == 0)
        {
            lines.Add("- None");
            return string.Join("\n", lines);
        }

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

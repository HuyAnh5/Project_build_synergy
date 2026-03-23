// SkillPassiveSO.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Passive effect runtime currently kept intentionally narrow.
/// Content depth should come from named passives in spec, not from generic %stat packages.
/// </summary>
public enum PassiveEffectId
{
    FocusBonusOnTurnStart
}

[Serializable, InlineProperty]
public class PassiveEffectEntry
{
    [BoxGroup("Effect")]
    [LabelText("Effect")]
    public PassiveEffectId id = PassiveEffectId.FocusBonusOnTurnStart;

    [BoxGroup("Effect")]
    [LabelText("+Focus")]
    public int valueI = 1;

    public void ApplyDefaults()
    {
        valueI = 1;
    }
}

[CreateAssetMenu(menuName = "Game/Skill/Passive", fileName = "SkillPassive_")]
public class SkillPassiveSO : ScriptableObject
{
    [TabGroup("Tabs", "Overview")]
    [HorizontalGroup("Top", Width = 70)]
    [HideLabel, PreviewField(70, ObjectFieldAlignment.Left)]
    public Sprite icon;

    [TabGroup("Tabs", "Overview")]
    [VerticalGroup("Top/Info")]
    [LabelText("Display Name")]
    public string displayName;

    [TabGroup("Tabs", "Overview")]
    [VerticalGroup("Top/Info")]
    [TextArea(2, 4)]
    public string description;

    [TabGroup("Tabs", "Spec")]
    [BoxGroup("Tabs/Spec/Metadata")]
    [InlineProperty]
    public SkillSpecMetadata spec = new SkillSpecMetadata();

    [TabGroup("Tabs", "Spec")]
    [BoxGroup("Tabs/Spec/Metadata")]
    [LabelText("Behavior Id")]
    public PassiveBehaviorId behaviorId = PassiveBehaviorId.None;

    // ---- Quick Add (Foldout) ----
    [TabGroup("Tabs", "Effects")]
    [FoldoutGroup("Tabs/Effects/Quick Add", expanded: false)]
    [ButtonGroup("Tabs/Effects/Quick Add/Row0")]
    [Button("Turn Start +1 Focus")]
    private void AddTurnStartFocus() => AddEffect(e => { e.id = PassiveEffectId.FocusBonusOnTurnStart; e.valueI = 1; });

    // ---- Effects ----
    [TabGroup("Tabs", "Effects")]
    [TitleGroup("Tabs/Effects/Effect List", "Effects", Alignment = TitleAlignments.Centered)]
    [ListDrawerSettings(Expanded = true, DraggableItems = true, ShowIndexLabels = true)]
    public List<PassiveEffectEntry> effects = new();

    private void AddEffect(Action<PassiveEffectEntry> init)
    {
        if (effects == null) effects = new List<PassiveEffectEntry>();
        var e = new PassiveEffectEntry();
        e.ApplyDefaults();
        init?.Invoke(e);
        effects.Add(e);
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = name;

        if (effects == null) effects = new List<PassiveEffectEntry>();
    }
}

// SkillPassiveSO.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public enum PassiveAttackType
{
    Any,
    Melee,
    Ranged
}

public enum PassiveTargetRow
{
    Any,
    Front,
    Back
}

/// <summary>
/// Passive chuẩn hoá theo list bạn chốt.
/// </summary>
public enum PassiveEffectId
{
    // ---- A: Damage / Guard ----
    DamagePercentAll,               // +20% all dmg
    DamagePercentByElement,         // +20% dmg theo element
    ConditionalDamagePercent,       // +% dmg với filter (melee/ranged + front/back)
    GuardGainPercent,               // +20% guard gain
    GuardFlatAtTurnEnd,             // +X guard ở cuối turn (bị scale bởi GuardGainPercent)

    // ---- B: Status synergy ----
    BurnConsumeDamageMultiplier,    // burn consume dmg x2
    LightningVsMarkMultiplierAdd,   // lightning vs mark +0.5 (vd 2 -> 2.5)
    BleedTickBonusFlat,             // bleed tick +1
    FreezeBreakFocusBonusAdd,       // (optional) freeze break focus +1

    // ---- C: Economy ----
    FocusBonusOnTurnStart,          // start turn +1 focus thêm
    FocusBonusOnGuardBreak,         // break guard +1 focus
    HealOnGuardBreak,               // break guard heal nhỏ
    FocusBonusOnBasicStrike         // basic strike +1 focus thêm
}

[Serializable, InlineProperty]
public class PassiveEffectEntry
{
    [HorizontalGroup("Row", Width = 290)]
    [HideLabel]
    public PassiveEffectId id = PassiveEffectId.DamagePercentByElement;

    [ShowIf(nameof(UsesFloatValue))]
    [HorizontalGroup("Row", Width = 140)]
    [LabelText("@FloatLabel")]
    public float valueF = 0.2f;

    [ShowIf(nameof(UsesIntValue))]
    [HorizontalGroup("Row", Width = 140)]
    [LabelText("@IntLabel")]
    public int valueI = 1;

    // ---- Filters (chỉ khi cần) ----
    [ShowIf(nameof(UsesElement))]
    [BoxGroup("Filter")]
    [EnumToggleButtons]
    public ElementType element = ElementType.Physical;

    [ShowIf(nameof(UsesAttackFilter))]
    [BoxGroup("Filter")]
    [EnumToggleButtons]
    public PassiveAttackType attackType = PassiveAttackType.Any;

    [ShowIf(nameof(UsesRowFilter))]
    [BoxGroup("Filter")]
    [EnumToggleButtons]
    public PassiveTargetRow targetRow = PassiveTargetRow.Any;

    // ---- Notes ----
    [ShowIf(nameof(IsGuardFlatAtTurnEnd))]
    [InfoBox("GuardFlatAtTurnEnd sẽ được SCALE bởi GuardGainPercent. Ví dụ +5 và +20% => ceil(5*1.2)=6.", InfoMessageType.Info)]
    [HideInInspector] public bool _guardNote = true;

    private bool UsesElement => id == PassiveEffectId.DamagePercentByElement;

    private bool UsesAttackFilter => id == PassiveEffectId.ConditionalDamagePercent;
    private bool UsesRowFilter => id == PassiveEffectId.ConditionalDamagePercent;

    private bool UsesFloatValue =>
        id == PassiveEffectId.DamagePercentAll ||
        id == PassiveEffectId.DamagePercentByElement ||
        id == PassiveEffectId.ConditionalDamagePercent ||
        id == PassiveEffectId.GuardGainPercent ||
        id == PassiveEffectId.BurnConsumeDamageMultiplier ||
        id == PassiveEffectId.LightningVsMarkMultiplierAdd;

    private bool UsesIntValue =>
        id == PassiveEffectId.GuardFlatAtTurnEnd ||
        id == PassiveEffectId.BleedTickBonusFlat ||
        id == PassiveEffectId.FreezeBreakFocusBonusAdd ||
        id == PassiveEffectId.FocusBonusOnTurnStart ||
        id == PassiveEffectId.FocusBonusOnGuardBreak ||
        id == PassiveEffectId.HealOnGuardBreak ||
        id == PassiveEffectId.FocusBonusOnBasicStrike;

    private bool IsGuardFlatAtTurnEnd => id == PassiveEffectId.GuardFlatAtTurnEnd;

    private string FloatLabel
    {
        get
        {
            switch (id)
            {
                case PassiveEffectId.DamagePercentAll: return "+DMG% (All)";
                case PassiveEffectId.DamagePercentByElement: return "+DMG%";
                case PassiveEffectId.ConditionalDamagePercent: return "+DMG%";
                case PassiveEffectId.GuardGainPercent: return "+GUARD%";
                case PassiveEffectId.BurnConsumeDamageMultiplier: return "xBurn";
                case PassiveEffectId.LightningVsMarkMultiplierAdd: return "+MarkMult";
                default: return "Value";
            }
        }
    }

    private string IntLabel
    {
        get
        {
            switch (id)
            {
                case PassiveEffectId.GuardFlatAtTurnEnd: return "+Guard";
                case PassiveEffectId.BleedTickBonusFlat: return "+BleedTick";
                case PassiveEffectId.FreezeBreakFocusBonusAdd: return "+Focus";
                case PassiveEffectId.FocusBonusOnTurnStart: return "+Focus";
                case PassiveEffectId.FocusBonusOnGuardBreak: return "+Focus";
                case PassiveEffectId.HealOnGuardBreak: return "Heal";
                case PassiveEffectId.FocusBonusOnBasicStrike: return "+Focus";
                default: return "Value";
            }
        }
    }

    public void ApplyDefaults()
    {
        switch (id)
        {
            case PassiveEffectId.DamagePercentAll:
                valueF = 0.20f;
                break;

            case PassiveEffectId.DamagePercentByElement:
                valueF = 0.20f; element = ElementType.Physical;
                break;

            case PassiveEffectId.ConditionalDamagePercent:
                valueF = 0.25f; attackType = PassiveAttackType.Ranged; targetRow = PassiveTargetRow.Back;
                break;

            case PassiveEffectId.GuardGainPercent:
                valueF = 0.20f;
                break;

            case PassiveEffectId.GuardFlatAtTurnEnd:
                valueI = 5;
                break;

            case PassiveEffectId.BurnConsumeDamageMultiplier:
                valueF = 2f;
                break;

            case PassiveEffectId.LightningVsMarkMultiplierAdd:
                valueF = 0.5f;
                break;

            case PassiveEffectId.BleedTickBonusFlat:
            case PassiveEffectId.FreezeBreakFocusBonusAdd:
            case PassiveEffectId.FocusBonusOnTurnStart:
            case PassiveEffectId.FocusBonusOnGuardBreak:
            case PassiveEffectId.FocusBonusOnBasicStrike:
                valueI = 1;
                break;

            case PassiveEffectId.HealOnGuardBreak:
                valueI = 2;
                break;
        }
    }
}

[CreateAssetMenu(menuName = "Game/Skill/Passive", fileName = "SkillPassive_")]
public class SkillPassiveSO : ScriptableObject
{
    [Title("Passive Skill", bold: true)]
    [HorizontalGroup("Top", Width = 70)]
    [HideLabel, PreviewField(70, ObjectFieldAlignment.Left)]
    public Sprite icon;

    [VerticalGroup("Top/Info")]
    [LabelText("Display Name")]
    public string displayName;

    [VerticalGroup("Top/Info")]
    [TextArea(2, 4)]
    public string description;

    // ---- Quick Add (Foldout) ----
    [FoldoutGroup("Quick Add", expanded: false)]
    [ButtonGroup("Quick Add/Row0")]
    [Button("All +20%")]
    private void AddAll20() => AddEffect(e => { e.id = PassiveEffectId.DamagePercentAll; e.valueF = 0.20f; });

    [FoldoutGroup("Quick Add", expanded: false)]
    [ButtonGroup("Quick Add/Row1")]
    [Button("Phy +20%")]
    private void AddPhy20() => AddEffect(e => { e.id = PassiveEffectId.DamagePercentByElement; e.element = ElementType.Physical; e.valueF = 0.20f; });

    [FoldoutGroup("Quick Add", expanded: false)]
    [ButtonGroup("Quick Add/Row1")]
    [Button("Fire +20%")]
    private void AddFire20() => AddEffect(e => { e.id = PassiveEffectId.DamagePercentByElement; e.element = ElementType.Fire; e.valueF = 0.20f; });

    [FoldoutGroup("Quick Add", expanded: false)]
    [ButtonGroup("Quick Add/Row2")]
    [Button("Ice +20%")]
    private void AddIce20() => AddEffect(e => { e.id = PassiveEffectId.DamagePercentByElement; e.element = ElementType.Ice; e.valueF = 0.20f; });

    [FoldoutGroup("Quick Add", expanded: false)]
    [ButtonGroup("Quick Add/Row2")]
    [Button("Lightning +20%")]
    private void AddLgt20() => AddEffect(e => { e.id = PassiveEffectId.DamagePercentByElement; e.element = ElementType.Lightning; e.valueF = 0.20f; });

    [FoldoutGroup("Quick Add", expanded: false)]
    [ButtonGroup("Quick Add/Row3")]
    [Button("Backline Hunter (+25%)")]
    private void AddBacklineHunter() => AddEffect(e =>
    {
        e.id = PassiveEffectId.ConditionalDamagePercent;
        e.valueF = 0.25f;
        e.attackType = PassiveAttackType.Ranged;
        e.targetRow = PassiveTargetRow.Back;
    });

    [FoldoutGroup("Quick Add", expanded: false)]
    [ButtonGroup("Quick Add/Row3")]
    [Button("Frontline Hunter (+25%)")]
    private void AddFrontlineHunter() => AddEffect(e =>
    {
        e.id = PassiveEffectId.ConditionalDamagePercent;
        e.valueF = 0.25f;
        e.attackType = PassiveAttackType.Ranged;
        e.targetRow = PassiveTargetRow.Front;
    });

    [FoldoutGroup("Quick Add", expanded: false)]
    [ButtonGroup("Quick Add/Row4")]
    [Button("+20% Guard Gain")]
    private void AddGuard20() => AddEffect(e => { e.id = PassiveEffectId.GuardGainPercent; e.valueF = 0.20f; });

    [FoldoutGroup("Quick Add", expanded: false)]
    [ButtonGroup("Quick Add/Row4")]
    [Button("+5 Guard End Turn")]
    private void AddGuardFlatEnd() => AddEffect(e => { e.id = PassiveEffectId.GuardFlatAtTurnEnd; e.valueI = 5; });

    [FoldoutGroup("Quick Add", expanded: false)]
    [ButtonGroup("Quick Add/Row5")]
    [Button("Burn Consume x2")]
    private void AddBurnx2() => AddEffect(e => { e.id = PassiveEffectId.BurnConsumeDamageMultiplier; e.valueF = 2f; });

    [FoldoutGroup("Quick Add", expanded: false)]
    [ButtonGroup("Quick Add/Row5")]
    [Button("Lightning vs Mark +0.5")]
    private void AddMarkLgt() => AddEffect(e => { e.id = PassiveEffectId.LightningVsMarkMultiplierAdd; e.valueF = 0.5f; });

    [FoldoutGroup("Quick Add", expanded: false)]
    [ButtonGroup("Quick Add/Row6")]
    [Button("Bleed Tick +1")]
    private void AddBleedTick() => AddEffect(e => { e.id = PassiveEffectId.BleedTickBonusFlat; e.valueI = 1; });

    [FoldoutGroup("Quick Add", expanded: false)]
    [ButtonGroup("Quick Add/Row6")]
    [Button("Freeze Break Focus +1 (opt)")]
    private void AddFreezeBonus() => AddEffect(e => { e.id = PassiveEffectId.FreezeBreakFocusBonusAdd; e.valueI = 1; });

    [FoldoutGroup("Quick Add", expanded: false)]
    [ButtonGroup("Quick Add/Row7")]
    [Button("Turn Start +1 Focus")]
    private void AddTurnStartFocus() => AddEffect(e => { e.id = PassiveEffectId.FocusBonusOnTurnStart; e.valueI = 1; });

    [FoldoutGroup("Quick Add", expanded: false)]
    [ButtonGroup("Quick Add/Row7")]
    [Button("Guard Break +1 Focus")]
    private void AddGBFocus() => AddEffect(e => { e.id = PassiveEffectId.FocusBonusOnGuardBreak; e.valueI = 1; });

    [FoldoutGroup("Quick Add", expanded: false)]
    [ButtonGroup("Quick Add/Row8")]
    [Button("Guard Break Heal (small)")]
    private void AddGBHeal() => AddEffect(e => { e.id = PassiveEffectId.HealOnGuardBreak; e.valueI = 2; });

    [FoldoutGroup("Quick Add", expanded: false)]
    [ButtonGroup("Quick Add/Row8")]
    [Button("BasicStrike +1 Focus")]
    private void AddBasicStrikeFocus() => AddEffect(e => { e.id = PassiveEffectId.FocusBonusOnBasicStrike; e.valueI = 1; });

    // ---- Effects ----
    [Space(8)]
    [Title("Effects", bold: true)]
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

// SkillDamageSO.cs
using UnityEngine;
using Sirenix.OdinInspector;

public enum ElementTag
{
    Neutral,
    Fire,
    Ice,
    Lightning,
    Physical
}

public enum CoreAction
{
    None,
    BasicStrike,
    BasicGuard
}

public enum DamageBehaviorFamily
{
    None,
    Fire,
    Ice,
    Lightning,
    Bleed,
    Physical
}

[CreateAssetMenu(menuName = "Game/Skill/Damage", fileName = "SkillDamage_")]
public class SkillDamageSO : ScriptableObject
{
    [HorizontalGroup("Top", Width = 70)]
    [HideLabel, PreviewField(70, ObjectFieldAlignment.Left)]
    public Sprite icon;

    [VerticalGroup("Top/Info")]
    [LabelText("Display Name")]
    public string displayName;

    [VerticalGroup("Top/Info")]
    [TextArea(2, 4)]
    public string description;

    [TabGroup("Tabs", "Spec")]
    [BoxGroup("Tabs/Spec/Metadata")]
    [InlineProperty]
    public SkillSpecMetadata spec = new SkillSpecMetadata();

    [TabGroup("Tabs", "Spec")]
    [BoxGroup("Tabs/Spec/Behavior")]
    [LabelText("Behavior Family")]
    [ShowIf("@kind == SkillKind.Attack")]
    [EnumToggleButtons]
    public DamageBehaviorFamily behaviorFamily = DamageBehaviorFamily.None;

    [TabGroup("Tabs", "Spec")]
    [BoxGroup("Tabs/Spec/Behavior")]
    [LabelText("Fire Behavior")]
    [ShowIf("@kind == SkillKind.Attack && behaviorFamily == DamageBehaviorFamily.Fire")]
    public FireDamageBehaviorId fireBehaviorId = FireDamageBehaviorId.None;

    [TabGroup("Tabs", "Spec")]
    [BoxGroup("Tabs/Spec/Behavior")]
    [LabelText("Ice Behavior")]
    [ShowIf("@kind == SkillKind.Attack && behaviorFamily == DamageBehaviorFamily.Ice")]
    public IceDamageBehaviorId iceBehaviorId = IceDamageBehaviorId.None;

    [TabGroup("Tabs", "Spec")]
    [BoxGroup("Tabs/Spec/Behavior")]
    [LabelText("Lightning Behavior")]
    [ShowIf("@kind == SkillKind.Attack && behaviorFamily == DamageBehaviorFamily.Lightning")]
    public LightningDamageBehaviorId lightningBehaviorId = LightningDamageBehaviorId.None;

    [TabGroup("Tabs", "Spec")]
    [BoxGroup("Tabs/Spec/Behavior")]
    [LabelText("Bleed Behavior")]
    [ShowIf("@kind == SkillKind.Attack && behaviorFamily == DamageBehaviorFamily.Bleed")]
    public BleedDamageBehaviorId bleedBehaviorId = BleedDamageBehaviorId.None;

    [TabGroup("Tabs", "Spec")]
    [BoxGroup("Tabs/Spec/Behavior")]
    [LabelText("Physical Behavior")]
    [ShowIf("@kind == SkillKind.Attack && behaviorFamily == DamageBehaviorFamily.Physical")]
    public PhysicalDamageBehaviorId physicalBehaviorId = PhysicalDamageBehaviorId.None;

    // ------------------- Identity -------------------

    [TabGroup("Tabs", "Core")]
    [BoxGroup("Tabs/Core/Identity")]
    [EnumToggleButtons]
    public SkillKind kind = SkillKind.Attack;

    [TabGroup("Tabs", "Core")]
    [BoxGroup("Tabs/Core/Identity")]
    [LabelText("Core Action")]
    [EnumToggleButtons]
    public CoreAction coreAction = CoreAction.None;

    [TabGroup("Tabs", "Core")]
    [BoxGroup("Tabs/Core/Identity")]
    [EnumToggleButtons]
    public SkillTargetRule target = SkillTargetRule.SingleEnemy;

    [TabGroup("Tabs", "Core")]
    [BoxGroup("Tabs/Core/Identity")]
    [ShowIf("@kind == SkillKind.Attack")]
    [EnumToggleButtons]
    public DamageGroup group = DamageGroup.Strike;

    [TabGroup("Tabs", "Core")]
    [BoxGroup("Tabs/Core/Identity")]
    [ShowIf("@kind == SkillKind.Attack")]
    [EnumToggleButtons]
    public ElementTag element = ElementTag.Physical;

    [TabGroup("Tabs", "Core")]
    [BoxGroup("Tabs/Core/Identity")]
    [ShowIf("@kind == SkillKind.Attack")]
    [EnumToggleButtons]
    public RangeType range = RangeType.Ranged;

    // AoE flags (kept for compatibility / UI clarity, but derived from target)
    [TabGroup("Tabs", "Core")]
    [BoxGroup("Tabs/Core/Derived")]
    [ReadOnly]
    [LabelText("Hit All Enemies (Derived)")]
    public bool hitAllEnemies = false;

    [TabGroup("Tabs", "Core")]
    [BoxGroup("Tabs/Core/Derived")]
    [ReadOnly]
    [LabelText("Hit All Allies (Derived)")]
    public bool hitAllAllies = false;

    // ------------------- Slots -------------------

    [TabGroup("Tabs", "Core")]
    [BoxGroup("Tabs/Core/Slots & Cost")]
    [MinValue(1), MaxValue(3)]
    public int slotsRequired = 1;

    // ------------------- Condition -------------------

    [TabGroup("Tabs", "Condition")]
    [BoxGroup("Tabs/Condition/Rule")]
    [ToggleLeft]
    [LabelText("Has Condition")]
    public bool hasCondition = false;

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [BoxGroup("Tabs/Condition/Stack/Condition")]
    [ShowIf("hasCondition")]
    [PropertySpace(4)]
    [HideLabel]
    public SkillConditionData condition = new SkillConditionData();

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [FoldoutGroup("Tabs/Condition/Stack/When Condition Is Met", expanded: false)]
    [ShowIf(nameof(hasCondition))]
    [PropertySpace(SpaceBefore = 4, SpaceAfter = 6)]
    [InlineProperty, HideLabel]
    public SkillDamageConditionalOverrides whenConditionIsMet = new SkillDamageConditionalOverrides();

    // Back-compat alias naming style
    public SkillDamageConditionalOverrides conditionalOverrides => whenConditionIsMet;

    // ---------------------------
    // Cost
    // ---------------------------

    [TabGroup("Tabs", "Core")]
    [BoxGroup("Tabs/Core/Slots & Cost")]
    [Min(0)]
    public int focusCost = 0;

    [TabGroup("Tabs", "Core")]
    [BoxGroup("Tabs/Core/Slots & Cost")]
    public int focusGainOnCast = 0;

    // -------------------- DAMAGE --------------------

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Damage")]
    [ShowIf("@kind == SkillKind.Attack")]
    [Range(0f, 2f)]
    public float dieMultiplier = 1f;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Damage")]
    [ShowIf("@kind == SkillKind.Attack")]
    public int flatDamage = 0;

    // -------------------- SUNDER BONUS --------------------

    [TabGroup("Tabs", "Effects")]
    [ShowIf("@group == DamageGroup.Sunder")]
    [FoldoutGroup("Tabs/Effects/Sunder Bonus", expanded: false)]
    public bool sunderBonusIfTargetHasGuard = true;

    [ShowIf("@group == DamageGroup.Sunder && sunderBonusIfTargetHasGuard")]
    [TabGroup("Tabs", "Effects")]
    [FoldoutGroup("Tabs/Effects/Sunder Bonus", expanded: false)]
    [Min(0f)]
    public float sunderGuardDamageMultiplier = 2f;

    // -------------------- GUARD --------------------

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Guard")]
    [ShowIf("@kind == SkillKind.Guard")]
    [Range(0f, 2f)]
    public float guardDieMultiplier = 1f;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Guard")]
    [ShowIf("@kind == SkillKind.Guard")]
    public int guardFlat = 0;

    // -------------------- SPECIAL COMBAT --------------------

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Special Combat")]
    [ShowIf("@kind == SkillKind.Attack")]
    public bool bypassGuard = false;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Special Combat")]
    [ShowIf("@kind == SkillKind.Attack")]
    public bool clearsGuard = false;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Special Combat")]
    [ShowIf("@kind == SkillKind.Attack")]
    public bool canUseMarkMultiplier = true;

    // -------------------- BURN SPENDER --------------------

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Burn Spender (Fire)")]
    [ShowIf("@kind == SkillKind.Attack && element == ElementTag.Fire")]
    public bool consumesBurn = false;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Burn Spender (Fire)")]
    [ShowIf("@kind == SkillKind.Attack && element == ElementTag.Fire && consumesBurn")]
    [Min(0)]
    public int burnDamagePerStack = 2;

    // -------------------- APPLY STATUS --------------------

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Apply Status")]
    [ShowIf("@kind == SkillKind.Attack && group == DamageGroup.Effect")]
    public bool applyBurn;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Apply Status")]
    [ShowIf("@kind == SkillKind.Attack && group == DamageGroup.Effect && applyBurn")]
    [Min(0)]
    public int burnAddStacks = 2;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Apply Status")]
    [ShowIf("@kind == SkillKind.Attack && group == DamageGroup.Effect && applyBurn")]
    [Min(0)]
    public int burnRefreshTurns = 3;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Apply Status")]
    [ShowIf("@kind == SkillKind.Attack && group == DamageGroup.Effect")]
    public bool applyMark;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Apply Status")]
    [ShowIf("@kind == SkillKind.Attack && group == DamageGroup.Effect")]
    public bool applyBleed;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Apply Status")]
    [ShowIf("@kind == SkillKind.Attack && group == DamageGroup.Effect && applyBleed")]
    [Min(0)]
    public int bleedTurns = 3;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Apply Status")]
    [ShowIf("@kind == SkillKind.Attack && group == DamageGroup.Effect")]
    public bool applyFreeze;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Apply Status")]
    [ShowIf("@kind == SkillKind.Attack && group == DamageGroup.Effect && applyFreeze")]
    [Range(0f, 1f)]
    public float freezeChance = 0.4f;

    // -------------------- VFX --------------------

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/VFX")]
    [ShowIf("@kind == SkillKind.Attack && range == RangeType.Ranged")]
    public Projectile2D projectilePrefab;

    // -------------------- QUICK PRESETS --------------------

    [TabGroup("Tabs", "Presets")]
    [FoldoutGroup("Tabs/Presets/Presets", expanded: true)]
    [ButtonGroup("Tabs/Presets/Presets/Row0"), Button("Basic Strike (Built-in)")]
    private void PresetBasicStrike()
    {
        PresetMeleeStrike();
        coreAction = CoreAction.BasicStrike;
    }

    [TabGroup("Tabs", "Presets")]
    [FoldoutGroup("Tabs/Presets/Presets")]
    [ButtonGroup("Tabs/Presets/Presets/Row0"), Button("Basic Guard (Built-in)")]
    private void PresetBasicGuard()
    {
        PresetGuard();
        coreAction = CoreAction.BasicGuard;
    }

    [TabGroup("Tabs", "Presets")]
    [FoldoutGroup("Tabs/Presets/Presets")]
    [ButtonGroup("Tabs/Presets/Presets/Row1"), Button("Melee Strike")]
    private void PresetMeleeStrike()
    {
        coreAction = CoreAction.None;

        kind = SkillKind.Attack;
        target = SkillTargetRule.SingleEnemy;
        group = DamageGroup.Strike;
        element = ElementTag.Physical;
        range = RangeType.Melee;

        bypassGuard = clearsGuard = false;
        canUseMarkMultiplier = true;
        consumesBurn = false;

        applyBurn = applyMark = applyBleed = applyFreeze = false;
    }

    [TabGroup("Tabs", "Presets")]
    [FoldoutGroup("Tabs/Presets/Presets")]
    [ButtonGroup("Tabs/Presets/Presets/Row1"), Button("Ranged Strike")]
    private void PresetRangedStrike()
    {
        coreAction = CoreAction.None;

        kind = SkillKind.Attack;
        target = SkillTargetRule.SingleEnemy;
        group = DamageGroup.Strike;
        element = ElementTag.Physical;
        range = RangeType.Ranged;

        bypassGuard = clearsGuard = false;
        canUseMarkMultiplier = true;
        consumesBurn = false;

        applyBurn = applyMark = applyBleed = applyFreeze = false;
    }

    [TabGroup("Tabs", "Presets")]
    [FoldoutGroup("Tabs/Presets/Presets")]
    [ButtonGroup("Tabs/Presets/Presets/Row2"), Button("Sunder")]
    private void PresetSunder()
    {
        coreAction = CoreAction.None;

        kind = SkillKind.Attack;
        target = SkillTargetRule.SingleEnemy;
        group = DamageGroup.Sunder;
        element = ElementTag.Physical;
        range = RangeType.Melee;

        bypassGuard = true;
        clearsGuard = true;
        canUseMarkMultiplier = false;
        consumesBurn = false;

        applyBurn = applyMark = applyBleed = applyFreeze = false;
    }

    [TabGroup("Tabs", "Presets")]
    [FoldoutGroup("Tabs/Presets/Presets")]
    [ButtonGroup("Tabs/Presets/Presets/Row2"), Button("Effect Applier")]
    private void PresetEffectApplier()
    {
        coreAction = CoreAction.None;

        kind = SkillKind.Attack;
        target = SkillTargetRule.SingleEnemy;
        group = DamageGroup.Effect;
        element = ElementTag.Physical;
        range = RangeType.Ranged;

        bypassGuard = clearsGuard = false;
        canUseMarkMultiplier = false;
        consumesBurn = false;

        // bạn bật các apply* tuỳ skill
    }

    [TabGroup("Tabs", "Presets")]
    [FoldoutGroup("Tabs/Presets/Presets")]
    [ButtonGroup("Tabs/Presets/Presets/Row3"), Button("Guard (Self)")]
    private void PresetGuard()
    {
        coreAction = CoreAction.None;

        kind = SkillKind.Guard;
        target = SkillTargetRule.Self;
    }

    // -------------------- HELPERS --------------------

    public int CalculateDamage(int dieValue)
    {
        if (kind != SkillKind.Attack) return 0;
        int dmg = Mathf.FloorToInt(dieValue * dieMultiplier) + flatDamage;
        return Mathf.Max(0, dmg);
    }

    public int CalculateGuard(int dieValue)
    {
        if (kind != SkillKind.Guard) return 0;
        int g = Mathf.FloorToInt(dieValue * guardDieMultiplier) + guardFlat;
        return Mathf.Max(0, g);
    }

    public bool IsBehavior(FireDamageBehaviorId behaviorId)
        => kind == SkillKind.Attack && element == ElementTag.Fire && fireBehaviorId == behaviorId;

    public bool IsBehavior(IceDamageBehaviorId behaviorId)
        => kind == SkillKind.Attack && element == ElementTag.Ice && iceBehaviorId == behaviorId;

    public bool IsBehavior(LightningDamageBehaviorId behaviorId)
        => kind == SkillKind.Attack && element == ElementTag.Lightning && lightningBehaviorId == behaviorId;

    public bool IsBehavior(BleedDamageBehaviorId behaviorId)
        => kind == SkillKind.Attack && bleedBehaviorId == behaviorId;

    public bool IsBehavior(PhysicalDamageBehaviorId behaviorId)
        => kind == SkillKind.Attack && element == ElementTag.Physical && physicalBehaviorId == behaviorId;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = name;

        // CoreAction -> safety nhẹ
        if (coreAction == CoreAction.BasicGuard)
        {
            kind = SkillKind.Guard;
            target = SkillTargetRule.Self;
        }
        else if (coreAction == CoreAction.BasicStrike)
        {
            if (kind != SkillKind.Attack) kind = SkillKind.Attack;
            if (group != DamageGroup.Strike) group = DamageGroup.Strike;
        }

        // Guard luôn self
        if (kind == SkillKind.Guard)
            target = SkillTargetRule.Self;

        // Derived AoE flags (no combo targeting)
        hitAllEnemies = (target == SkillTargetRule.AllEnemies || target == SkillTargetRule.AllUnits);
        hitAllAllies = (target == SkillTargetRule.AllAllies || target == SkillTargetRule.AllUnits);

        // Safety: Guard shouldn't be AoE right now
        if (kind == SkillKind.Guard)
        {
            hitAllEnemies = false;
            hitAllAllies = false;
        }
    }
}

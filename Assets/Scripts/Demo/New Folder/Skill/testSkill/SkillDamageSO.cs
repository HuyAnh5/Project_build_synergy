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

[CreateAssetMenu(menuName = "Game/Skill/Damage", fileName = "SkillDamage_")]
public class SkillDamageSO : ScriptableObject
{
    [Title("Skill (Damage)", bold: true)]
    [HorizontalGroup("Top", Width = 70)]
    [HideLabel, PreviewField(70, ObjectFieldAlignment.Left)]
    public Sprite icon;

    [VerticalGroup("Top/Info")]
    [LabelText("Display Name")]
    public string displayName;

    [VerticalGroup("Top/Info")]
    [TextArea(2, 4)]
    public string description;

    // ------------------- Identity -------------------

    [Space(6)]
    [BoxGroup("Identity")]
    [EnumToggleButtons]
    public SkillKind kind = SkillKind.Attack;

    [BoxGroup("Identity")]
    [LabelText("Core Action")]
    [EnumToggleButtons]
    public CoreAction coreAction = CoreAction.None;

    [BoxGroup("Identity")]
    [EnumToggleButtons]
    public SkillTargetRule target = SkillTargetRule.SingleEnemy;

    [BoxGroup("Identity")]
    [ShowIf("@kind == SkillKind.Attack")]
    [EnumToggleButtons]
    public DamageGroup group = DamageGroup.Strike;

    [BoxGroup("Identity")]
    [ShowIf("@kind == SkillKind.Attack")]
    [EnumToggleButtons]
    public ElementTag element = ElementTag.Physical;

    [BoxGroup("Identity")]
    [ShowIf("@kind == SkillKind.Attack")]
    [EnumToggleButtons]
    public RangeType range = RangeType.Ranged;

    // AoE flags (kept for compatibility / UI clarity, but derived from target)
    [BoxGroup("Identity")]
    [ReadOnly]
    [LabelText("Hit All Enemies (Derived)")]
    public bool hitAllEnemies = false;

    [BoxGroup("Identity")]
    [ReadOnly]
    [LabelText("Hit All Allies (Derived)")]
    public bool hitAllAllies = false;

    // ------------------- Slots -------------------

    [BoxGroup("Slots")]
    [MinValue(1), MaxValue(3)]
    public int slotsRequired = 1;

    // ------------------- Condition -------------------

    [BoxGroup("Condition")]
    [ToggleLeft]
    [LabelText("Has Condition")]
    public bool hasCondition = false;

    [VerticalGroup("Condition/Stack")]
    [BoxGroup("Condition/Stack/Condition")]
    [ShowIf("hasCondition")]
    [PropertySpace(4)]
    [HideLabel]
    public SkillConditionData condition = new SkillConditionData();

    [VerticalGroup("Condition/Stack")]
    [FoldoutGroup("Condition/Stack/When Condition Is Met", expanded: false)]
    [ShowIf(nameof(hasCondition))]
    [PropertySpace(SpaceBefore = 4, SpaceAfter = 6)]
    [InlineProperty, HideLabel]
    public SkillDamageConditionalOverrides whenConditionIsMet = new SkillDamageConditionalOverrides();

    // Back-compat alias naming style
    public SkillDamageConditionalOverrides conditionalOverrides => whenConditionIsMet;

    // ---------------------------
    // Cost
    // ---------------------------

    [BoxGroup("Cost")]
    [Min(0)]
    public int focusCost = 0;

    [BoxGroup("Cost")]
    public int focusGainOnCast = 0;

    // -------------------- DAMAGE --------------------

    [BoxGroup("Damage")]
    [ShowIf("@kind == SkillKind.Attack")]
    [Range(0f, 2f)]
    public float dieMultiplier = 1f;

    [BoxGroup("Damage")]
    [ShowIf("@kind == SkillKind.Attack")]
    public int flatDamage = 0;

    // -------------------- SUNDER BONUS --------------------

    [Header("Sunder Bonus")]
    [ShowIf("@group == DamageGroup.Sunder")]
    [FoldoutGroup("Sunder Bonus", expanded: false)]
    public bool sunderBonusIfTargetHasGuard = true;

    [ShowIf("@group == DamageGroup.Sunder && sunderBonusIfTargetHasGuard")]
    [FoldoutGroup("Sunder Bonus", expanded: false)]
    [Min(0f)]
    public float sunderGuardDamageMultiplier = 2f;

    // -------------------- GUARD --------------------

    [BoxGroup("Guard")]
    [ShowIf("@kind == SkillKind.Guard")]
    [Range(0f, 2f)]
    public float guardDieMultiplier = 1f;

    [BoxGroup("Guard")]
    [ShowIf("@kind == SkillKind.Guard")]
    public int guardFlat = 0;

    // -------------------- SPECIAL COMBAT --------------------

    [BoxGroup("Special Combat")]
    [ShowIf("@kind == SkillKind.Attack")]
    public bool bypassGuard = false;

    [BoxGroup("Special Combat")]
    [ShowIf("@kind == SkillKind.Attack")]
    public bool clearsGuard = false;

    [BoxGroup("Special Combat")]
    [ShowIf("@kind == SkillKind.Attack")]
    public bool canUseMarkMultiplier = true;

    // -------------------- BURN SPENDER --------------------

    [BoxGroup("Burn Spender (Fire)")]
    [ShowIf("@kind == SkillKind.Attack && element == ElementTag.Fire")]
    public bool consumesBurn = false;

    [BoxGroup("Burn Spender (Fire)")]
    [ShowIf("@kind == SkillKind.Attack && element == ElementTag.Fire && consumesBurn")]
    [Min(0)]
    public int burnDamagePerStack = 1;

    // -------------------- APPLY STATUS --------------------

    [BoxGroup("Apply Status (Effect skills)")]
    [ShowIf("@kind == SkillKind.Attack && group == DamageGroup.Effect")]
    public bool applyBurn;

    [BoxGroup("Apply Status (Effect skills)")]
    [ShowIf("@kind == SkillKind.Attack && group == DamageGroup.Effect && applyBurn")]
    [Min(0)]
    public int burnAddStacks = 2;

    [BoxGroup("Apply Status (Effect skills)")]
    [ShowIf("@kind == SkillKind.Attack && group == DamageGroup.Effect && applyBurn")]
    [Min(0)]
    public int burnRefreshTurns = 3;

    [BoxGroup("Apply Status (Effect skills)")]
    [ShowIf("@kind == SkillKind.Attack && group == DamageGroup.Effect")]
    public bool applyMark;

    [BoxGroup("Apply Status (Effect skills)")]
    [ShowIf("@kind == SkillKind.Attack && group == DamageGroup.Effect")]
    public bool applyBleed;

    [BoxGroup("Apply Status (Effect skills)")]
    [ShowIf("@kind == SkillKind.Attack && group == DamageGroup.Effect && applyBleed")]
    [Min(0)]
    public int bleedTurns = 3;

    [BoxGroup("Apply Status (Effect skills)")]
    [ShowIf("@kind == SkillKind.Attack && group == DamageGroup.Effect")]
    public bool applyFreeze;

    [BoxGroup("Apply Status (Effect skills)")]
    [ShowIf("@kind == SkillKind.Attack && group == DamageGroup.Effect && applyFreeze")]
    [Range(0f, 1f)]
    public float freezeChance = 0.4f;

    // -------------------- VFX --------------------

    [BoxGroup("VFX")]
    [ShowIf("@kind == SkillKind.Attack && range == RangeType.Ranged")]
    public Projectile2D projectilePrefab;

    // -------------------- QUICK PRESETS --------------------

    [FoldoutGroup("Presets (click once)")]
    [ButtonGroup("Presets (click once)/Row0"), Button("Basic Strike (Built-in)")]
    private void PresetBasicStrike()
    {
        PresetMeleeStrike();
        coreAction = CoreAction.BasicStrike;
    }

    [ButtonGroup("Presets (click once)/Row0"), Button("Basic Guard (Built-in)")]
    private void PresetBasicGuard()
    {
        PresetGuard();
        coreAction = CoreAction.BasicGuard;
    }

    [ButtonGroup("Presets (click once)/Row1"), Button("Melee Strike")]
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

    [ButtonGroup("Presets (click once)/Row1"), Button("Ranged Strike")]
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

    [ButtonGroup("Presets (click once)/Row2"), Button("Sunder")]
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

    [ButtonGroup("Presets (click once)/Row2"), Button("Effect Applier")]
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

    [ButtonGroup("Presets (click once)/Row3"), Button("Guard (Self)")]
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
        int dmg = Mathf.RoundToInt(dieValue * dieMultiplier) + flatDamage;
        return Mathf.Max(0, dmg);
    }

    public int CalculateGuard(int dieValue)
    {
        if (kind != SkillKind.Guard) return 0;
        int g = Mathf.RoundToInt(dieValue * guardDieMultiplier) + guardFlat;
        return Mathf.Max(0, g);
    }

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

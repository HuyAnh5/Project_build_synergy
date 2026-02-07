using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Game/Skill")]
public class SkillSO : ScriptableObject
{
    [Title("Skill", bold: true)]
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
    [EnumToggleButtons]
    public TargetRule target = TargetRule.Enemy;

    [BoxGroup("Identity")]
    [ShowIf("@kind == SkillKind.Attack")]
    [EnumToggleButtons]
    public DamageGroup group = DamageGroup.Strike;

    [BoxGroup("Identity")]
    [ShowIf("@kind == SkillKind.Attack")]
    [EnumToggleButtons]
    public ElementType element = ElementType.Physical;

    [BoxGroup("Identity")]
    [ShowIf("@kind == SkillKind.Attack")]
    [EnumToggleButtons]
    public RangeType range = RangeType.Ranged;

    // AoE flags (base behavior)
    [BoxGroup("Identity")]
    [ShowIf("@kind == SkillKind.Attack && target == TargetRule.Enemy")]
    [ToggleLeft]
    [LabelText("Hit All Enemies")]
    public bool hitAllEnemies = false;


    [BoxGroup("Identity")]
    [ShowIf("@target == TargetRule.Self")]
    [ToggleLeft]
    [LabelText("Hit All Allies")]
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
    public SkillConditionalOverrides whenConditionIsMet = new SkillConditionalOverrides();

    // Back-compat alias (older code might still call this name)
    public SkillConditionalOverrides conditionalOverrides => whenConditionIsMet;

    // Back-compat alias (if you prefer the singular name)
    public bool hitAllAlly { get => hitAllAllies; set => hitAllAllies = value; }

    // ---------------------------
    // Cost (Odin layout requested)
    // ---------------------------

    [BoxGroup("Cost")]
    [Min(0)]
    public int focusCost = 0;

    [BoxGroup("Cost")]
    public int focusGainOnCast = 0; // cast xong +focus

    // -------------------- DAMAGE --------------------

    [BoxGroup("Damage")]
    [ShowIf("@kind == SkillKind.Attack")]
    [Range(0f, 2f)]
    public float dieMultiplier = 1f; // dmg = round(die*mult) + flat

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
    public bool bypassGuard = false;   // Sunder

    [BoxGroup("Special Combat")]
    [ShowIf("@kind == SkillKind.Attack")]
    public bool clearsGuard = false;   // Sunder

    [BoxGroup("Special Combat")]
    [ShowIf("@kind == SkillKind.Attack")]
    public bool canUseMarkMultiplier = true;

    // -------------------- BURN SPENDER --------------------

    [BoxGroup("Burn Spender (Fireball)")]
    [ShowIf("@kind == SkillKind.Attack && element == ElementType.Fire")]
    public bool consumesBurn = false;

    [BoxGroup("Burn Spender (Fireball)")]
    [ShowIf("@kind == SkillKind.Attack && element == ElementType.Fire && consumesBurn")]
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
    [ButtonGroup("Presets (click once)/Row1"), Button("Melee Strike")]
    private void PresetMeleeStrike()
    {
        kind = SkillKind.Attack;
        target = TargetRule.Enemy;
        group = DamageGroup.Strike;
        element = ElementType.Physical;
        range = RangeType.Melee;
        hitAllEnemies = false;
        hitAllAllies = false;

        bypassGuard = clearsGuard = false;
        canUseMarkMultiplier = true;
        consumesBurn = false;
        applyBurn = applyMark = applyBleed = applyFreeze = false;
    }

    [ButtonGroup("Presets (click once)/Row1"), Button("Ranged Strike")]
    private void PresetRangedStrike()
    {
        kind = SkillKind.Attack;
        target = TargetRule.Enemy;
        group = DamageGroup.Strike;
        element = ElementType.Physical;
        range = RangeType.Ranged;
        hitAllEnemies = false;
        hitAllAllies = false;

        bypassGuard = clearsGuard = false;
        canUseMarkMultiplier = true;
        consumesBurn = false;
        applyBurn = applyMark = applyBleed = applyFreeze = false;
    }

    [ButtonGroup("Presets (click once)/Row2"), Button("Sunder")]
    private void PresetSunder()
    {
        kind = SkillKind.Attack;
        target = TargetRule.Enemy;
        group = DamageGroup.Sunder;
        element = ElementType.Physical;
        range = RangeType.Melee;
        hitAllEnemies = false;
        hitAllAllies = false;

        bypassGuard = true;
        clearsGuard = true;
        canUseMarkMultiplier = false;
        consumesBurn = false;
        applyBurn = applyMark = applyBleed = applyFreeze = false;
    }

    [ButtonGroup("Presets (click once)/Row2"), Button("Effect Applier")]
    private void PresetEffectApplier()
    {
        kind = SkillKind.Attack;
        target = TargetRule.Enemy;
        group = DamageGroup.Effect;
        element = ElementType.Physical;
        range = RangeType.Ranged;
        hitAllEnemies = false;
        hitAllAllies = false;

        bypassGuard = clearsGuard = false;
        canUseMarkMultiplier = false;
        consumesBurn = false;
        // bạn bật các apply* tuỳ skill
    }

    [ButtonGroup("Presets (click once)/Row3"), Button("Guard (Self)")]
    private void PresetGuard()
    {
        kind = SkillKind.Guard;
        target = TargetRule.Self;
        hitAllEnemies = false;
        // hitAllAllies có thể bật nếu muốn guard cho cả team sau này
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

        // guard luôn self
        if (kind == SkillKind.Guard) target = TargetRule.Self;

        // Safety: only one AoE mode makes sense per target rule.
        if (target == TargetRule.Self) hitAllEnemies = false;
        if (target == TargetRule.Enemy) hitAllAllies = false;

        // Effect applier shouldn't consume mark multiplier by default, but keep user choice.
        // (no auto rules here)
    }
}

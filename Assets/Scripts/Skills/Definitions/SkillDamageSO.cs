// SkillDamageSO.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
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
    [ShowIf("@kind == SkillKind.Attack && element != ElementTag.Fire")]
    [EnumToggleButtons]
    public DamageBehaviorFamily behaviorFamily = DamageBehaviorFamily.None;

    [TabGroup("Tabs", "Spec")]
    [BoxGroup("Tabs/Spec/Behavior")]
    [LabelText("Fire Behavior")]
    [HideInInspector]
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
    [ShowIf("@kind == SkillKind.Attack || kind == SkillKind.Guard")]
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
    [BoxGroup("Tabs/Condition/Rule")]
    [ShowIf(nameof(hasCondition))]
    [EnumToggleButtons]
    [LabelText("Mode")]
    public ConditionEditorMode conditionEditorMode = ConditionEditorMode.Builder;

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [BoxGroup("Tabs/Condition/Stack/Builder")]
    [HideInInspector]
    public bool useSystemConditionPreset = true;

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [BoxGroup("Tabs/Condition/Stack/Builder")]
    [ShowIf(nameof(ShowConditionBuilder))]
    [EnumToggleButtons]
    [LabelText("Builder")]
    public SkillConditionFamily standardConditionFamily = SkillConditionFamily.DiceParity;

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [BoxGroup("Tabs/Condition/Stack/Option")]
    [ShowIf("@ShowConditionBuilder && standardConditionFamily == SkillConditionFamily.DiceParity")]
    [LabelText("Option")]
    [ValueDropdown(nameof(GetDiceParityOptions))]
    public DiceParityConditionPreset diceParityConditionPreset = DiceParityConditionPreset.Even;

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [BoxGroup("Tabs/Condition/Stack/Option")]
    [ShowIf("@ShowConditionBuilder && standardConditionFamily == SkillConditionFamily.CritFail")]
    [LabelText("Option")]
    [ValueDropdown(nameof(GetCritFailOptions))]
    public CritFailConditionPreset critFailConditionPreset = CritFailConditionPreset.Crit;

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [BoxGroup("Tabs/Condition/Stack/Option")]
    [ShowIf("@ShowConditionBuilder && standardConditionFamily == SkillConditionFamily.ExactValue")]
    [EnumToggleButtons]
    [LabelText("Exact Mode")]
    [FormerlySerializedAs("exactValueConditionPreset")]
    public SkillExactConditionMode exactConditionMode = SkillExactConditionMode.DieEqualsX;

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [BoxGroup("Tabs/Condition/Stack/Option")]
    [ShowIf("@ShowConditionBuilder && standardConditionFamily == SkillConditionFamily.ExactValue && ShowExactSingleValueInput")]
    [LabelText("Exact X")]
    public int exactValueX = 7;

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [BoxGroup("Tabs/Condition/Stack/Option")]
    [ShowIf("@ShowConditionBuilder && standardConditionFamily == SkillConditionFamily.ExactValue && ShowExactPatternInput")]
    [LabelText("Pattern")]
    [InfoBox("Nhập dãy số bằng dấu '-', ',', hoặc khoảng trắng. Ví dụ: 1-2-3-5", InfoMessageType.Info)]
    public string exactValuePattern = "1-2-3";

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [BoxGroup("Tabs/Condition/Stack/Option")]
    [ShowIf("@ShowConditionBuilder && standardConditionFamily == SkillConditionFamily.Resource")]
    [LabelText("Option")]
    [ValueDropdown(nameof(GetResourceOptions))]
    public ResourceConditionPreset resourceConditionPreset = ResourceConditionPreset.CurrentFocusGreaterOrEqualN;

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [BoxGroup("Tabs/Condition/Stack/Option")]
    [ShowIf("@ShowConditionBuilder && standardConditionFamily == SkillConditionFamily.LocalGroupRelation")]
    [EnumToggleButtons]
    [LabelText("Relation")]
    public LocalGroupRelationMode localGroupRelationMode = LocalGroupRelationMode.SelfPosition;

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [BoxGroup("Tabs/Condition/Stack/Option")]
    [ShowIf("@ShowConditionBuilder && standardConditionFamily == SkillConditionFamily.LocalGroupRelation && ShowLocalGroupSideSelection")]
    [EnumToggleButtons]
    [LabelText("Side")]
    public LocalGroupRelationSide localGroupRelationSide = LocalGroupRelationSide.Left;

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [BoxGroup("Tabs/Condition/Stack/Option")]
    [ShowIf("@ShowConditionBuilder && standardConditionFamily == SkillConditionFamily.LocalGroupRelation && localGroupRelationMode == LocalGroupRelationMode.SplitRole")]
    [EnumToggleButtons]
    [LabelText("Split Role")]
    public LocalGroupConditionPreset localGroupConditionPreset = LocalGroupConditionPreset.Highest;

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [BoxGroup("Tabs/Condition/Stack/Option")]
    [ShowIf("@ShowConditionBuilder && standardConditionFamily == SkillConditionFamily.TargetState")]
    [EnumToggleButtons]
    [LabelText("Option")]
    [InfoBox("Status history là note thiết kế để mở rộng sau; runtime hiện chưa có logic cho history.", InfoMessageType.Info)]
    public TargetStateConditionPreset targetStateConditionPreset = TargetStateConditionPreset.TargetHasBurn;

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [BoxGroup("Tabs/Condition/Stack/Option")]
    [ShowIf("@ShowConditionBuilder && standardConditionFamily == SkillConditionFamily.BoardState")]
    [LabelText("Option")]
    [ValueDropdown(nameof(GetBoardStateOptions))]
    public BoardStateConditionPreset boardStateConditionPreset = BoardStateConditionPreset.AliveEnemiesGreaterOrEqualN;

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [BoxGroup("Tabs/Condition/Stack/Legacy")]
    [HideInInspector]
    [ShowIf("@ShowConditionBuilder && useSystemConditionPreset && element == ElementTag.Fire")]
    [LabelText("Fire Preset (Legacy)")]
    public FireConditionPreset fireConditionPreset = FireConditionPreset.None;

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [BoxGroup("Tabs/Condition/Stack/Summary")]
    [ShowIf(nameof(ShowConditionBuilder))]
    [DisplayAsString(false)]
    [LabelText("Summary")]
    public string conditionPreviewText => BuildStandardConditionPreview();

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [BoxGroup("Tabs/Condition/Stack/Legacy")]
    [HideInInspector]
    [ShowIf("@ShowConditionBuilder && useSystemConditionPreset && element == ElementTag.Ice")]
    [LabelText("Ice Preset")]
    public IceConditionPreset iceConditionPreset = IceConditionPreset.None;

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [BoxGroup("Tabs/Condition/Stack/Legacy")]
    [HideInInspector]
    [ShowIf("@ShowConditionBuilder && useSystemConditionPreset && element == ElementTag.Lightning")]
    [LabelText("Lightning Preset")]
    public LightningConditionPreset lightningConditionPreset = LightningConditionPreset.None;

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [BoxGroup("Tabs/Condition/Stack/Legacy")]
    [HideInInspector]
    [ShowIf("@ShowConditionBuilder && useSystemConditionPreset && element == ElementTag.Physical")]
    [LabelText("Physical Preset")]
    public PhysicalConditionPreset physicalConditionPreset = PhysicalConditionPreset.None;

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [BoxGroup("Tabs/Condition/Stack/Legacy")]
    [HideInInspector]
    [ShowIf("@ShowConditionBuilder && useSystemConditionPreset && behaviorFamily == DamageBehaviorFamily.Bleed")]
    [LabelText("Bleed Preset")]
    public BleedConditionPreset bleedConditionPreset = BleedConditionPreset.None;

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [BoxGroup("Tabs/Condition/Stack/Builder")]
    [ShowIf("@ShowConditionBuilder && CurrentPresetNeedsValue()")]
    [LabelText("Preset N")]
    public int conditionPresetValue = 7;

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [BoxGroup("Tabs/Condition/Stack/Advanced")]
    [ShowIf(nameof(ShowConditionBuilder))]
    [HideInInspector]
    public bool showAdvancedCondition = false;

    [HideInInspector]
    [SerializeField]
    private bool exactConditionMigrated = false;

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [BoxGroup("Tabs/Condition/Stack/Advanced")]
    [ShowIf("@hasCondition && conditionEditorMode == ConditionEditorMode.Advanced")]
    [PropertySpace(4)]
    [HideLabel]
    public SkillConditionData condition = new SkillConditionData();

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [ShowIf(nameof(ShowConditionBuilder))]
    [InfoBox("Builder chỉ chọn trục điều kiện. If Condition Met mới quyết định skill được gì. Split Role dùng cho skill kiểu Lowest -> Burn / Highest -> Guard.", InfoMessageType.Info)]
    [PropertySpace(SpaceBefore = 4, SpaceAfter = 6)]
    [HideInInspector]
    public SkillDamageConditionalOverrides whenConditionIsMet = new SkillDamageConditionalOverrides();

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [BoxGroup("Tabs/Condition/Stack/If Condition Met")]
    [ShowIf(nameof(ShowConditionBuilder))]
    [InlineProperty]
    [HideLabel]
    public SkillConditionalOutcomeData conditionalOutcome = new SkillConditionalOutcomeData();

    [TabGroup("Tabs", "Condition")]
    [VerticalGroup("Tabs/Condition/Stack")]
    [BoxGroup("Tabs/Condition/Stack/Split Role")]
    [ShowIf(nameof(ShowConditionBuilder))]
    [InlineProperty]
    [HideLabel]
    public SkillSplitRoleData splitRole = new SkillSplitRoleData();

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
    [EnumToggleButtons]
    [LabelText("Damage Mode")]
    public BaseEffectValueMode baseDamageValueMode = BaseEffectValueMode.Flat;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Damage")]
    [ShowIf("@kind == SkillKind.Attack")]
    [Range(0f, 2f)]
    [HideInInspector]
    public float dieMultiplier = 1f;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Damage")]
    [ShowIf("@kind == SkillKind.Attack && baseDamageValueMode == BaseEffectValueMode.Flat")]
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
    [EnumToggleButtons]
    [LabelText("Guard Mode")]
    public BaseEffectValueMode guardValueMode = BaseEffectValueMode.X;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Guard")]
    [ShowIf("@kind == SkillKind.Guard")]
    [HideInInspector]
    [FormerlySerializedAs("guardDieMultiplier")]
    public float legacyGuardDieMultiplier = 1f;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Guard")]
    [ShowIf("@kind == SkillKind.Guard && guardValueMode == BaseEffectValueMode.Flat")]
    [LabelText("Guard Flat")]
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
    [ShowIf(nameof(ShowBurnSpenderSection))]
    public bool consumesBurn = false;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Burn Spender (Fire)")]
    [ShowIf("@ShowBurnSpenderSection && consumesBurn")]
    [Min(0)]
    public int burnDamagePerStack = 2;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Fire Modules")]
    [HideInInspector]
    public FireAttackModuleData fireModules = new FireAttackModuleData();

    // -------------------- APPLY STATUS --------------------

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Apply Status")]
    [ShowIf(nameof(ShowBurnStatusSection))]
    public bool applyBurn;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Apply Status")]
    [ShowIf("@ShowBurnStatusSection && applyBurn")]
    [EnumToggleButtons]
    [LabelText("Burn Mode")]
    public BaseEffectValueMode baseBurnValueMode = BaseEffectValueMode.Flat;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Apply Status")]
    [ShowIf("@ShowBurnStatusSection && applyBurn && baseBurnValueMode == BaseEffectValueMode.Flat")]
    [Min(0)]
    public int burnAddStacks = 2;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Apply Status")]
    [ShowIf("@ShowBurnStatusSection && applyBurn")]
    [Min(0)]
    public int burnRefreshTurns = 3;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Apply Status")]
    [ShowIf(nameof(ShowMarkStatusSection))]
    public bool applyMark;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Apply Status")]
    [ShowIf(nameof(ShowBleedStatusSection))]
    public bool applyBleed;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Apply Status")]
    [ShowIf("@ShowBleedStatusSection && applyBleed")]
    [Min(0)]
    public int bleedTurns = 3;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Apply Status")]
    [ShowIf(nameof(ShowFreezeStatusSection))]
    public bool applyFreeze;

    [TabGroup("Tabs", "Effects")]
    [BoxGroup("Tabs/Effects/Apply Status")]
    [ShowIf("@ShowFreezeStatusSection && applyFreeze")]
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
        element = ElementTag.Neutral;
        guardValueMode = BaseEffectValueMode.X;
        guardFlat = 0;
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
        int g = guardValueMode == BaseEffectValueMode.X
            ? dieValue
            : guardFlat;
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

    private bool ShowConditionBuilder => hasCondition && kind == SkillKind.Attack;
    private bool ShowElementEffectSection => kind == SkillKind.Attack && group == DamageGroup.Effect;
    private bool ShowBurnStatusSection => ShowElementEffectSection && element == ElementTag.Fire;
    private bool ShowFreezeStatusSection => ShowElementEffectSection && element == ElementTag.Ice;
    private bool ShowMarkStatusSection => ShowElementEffectSection && element == ElementTag.Lightning;
    private bool ShowBleedStatusSection => false;
    private bool ShowBurnSpenderSection => kind == SkillKind.Attack && element == ElementTag.Fire;
    private bool ShowExactSingleValueInput =>
        exactConditionMode == SkillExactConditionMode.DieEqualsX;
    private bool ShowExactPatternInput =>
        exactConditionMode == SkillExactConditionMode.GroupContainsPattern;
    private bool ShowLocalGroupSideSelection =>
        localGroupRelationMode == LocalGroupRelationMode.SelfPosition ||
        localGroupRelationMode == LocalGroupRelationMode.NeighborRelation;
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

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = name;

        if (!exactConditionMigrated)
        {
            MigrateLegacyExactCondition();
            exactConditionMigrated = true;
        }

        if (exactValueX <= 0 && conditionPresetValue > 0)
            exactValueX = conditionPresetValue;

        if (string.IsNullOrWhiteSpace(exactValuePattern))
            exactValuePattern = "1-2-3";

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

        if (conditionalOutcome != null &&
            conditionalOutcome.enabled &&
            conditionalOutcome.type == ConditionalOutcomeType.ApplyBurn &&
            element != ElementTag.Fire)
        {
            conditionalOutcome.type = ConditionalOutcomeType.None;
        }

        if (baseDamageValueMode == BaseEffectValueMode.X)
            dieMultiplier = 0f;

        useSystemConditionPreset = conditionEditorMode == ConditionEditorMode.Builder;

        if (hasCondition && useSystemConditionPreset)
            ApplySystemConditionPreset();
    }

    private void MigrateLegacyExactCondition()
    {
        if (standardConditionFamily != SkillConditionFamily.ExactValue || condition == null || condition.clauses == null || condition.clauses.Count == 0)
            return;

        bool hasHighestEquals = false;
        bool hasLowestEquals = false;
        bool hasAnyBaseEquals = false;
        int firstEqualsValue = 0;

        for (int i = 0; i < condition.clauses.Count; i++)
        {
            SkillConditionClause clause = condition.clauses[i];
            if (clause == null || clause.comparison != SkillConditionComparison.Equals)
                continue;

            int rawReference = (int)clause.reference;

            if (!hasHighestEquals && (clause.reference == SkillConditionReference.HighestBaseValueInGroup || rawReference == 1))
            {
                hasHighestEquals = true;
                firstEqualsValue = clause.value;
            }

            if (!hasLowestEquals && (clause.reference == SkillConditionReference.LowestBaseValueInGroup || rawReference == 2))
            {
                hasLowestEquals = true;
                if (firstEqualsValue <= 0)
                    firstEqualsValue = clause.value;
            }

            if (!hasAnyBaseEquals && clause.reference == SkillConditionReference.AnyBaseValue)
            {
                hasAnyBaseEquals = true;
                if (string.IsNullOrWhiteSpace(exactValuePattern) || exactValuePattern == "1-2-3")
                    exactValuePattern = clause.value.ToString();
            }
        }

        if (hasHighestEquals && hasLowestEquals)
        {
            exactConditionMode = SkillExactConditionMode.DieEqualsX;
            if (firstEqualsValue > 0)
                exactValueX = firstEqualsValue;
        }
        else if (hasAnyBaseEquals)
        {
            exactConditionMode = SkillExactConditionMode.GroupContainsPattern;
        }
    }

    private void ApplySystemConditionPreset()
    {
        if (condition == null)
            condition = new SkillConditionData();

        condition.scope = SkillConditionScope.SlotBound;
        condition.logic = SkillConditionLogic.All;
        if (condition.clauses == null)
            condition.clauses = new System.Collections.Generic.List<SkillConditionClause>();
        else
            condition.clauses.Clear();

        ApplyStandardConditionPreset();
    }

    private bool CurrentPresetNeedsValue()
    {
        switch (standardConditionFamily)
        {
            case SkillConditionFamily.Resource:
            case SkillConditionFamily.BoardState:
                return true;
            default:
                return false;
        }
    }

    private void ApplyStandardConditionPreset()
    {
        switch (standardConditionFamily)
        {
            case SkillConditionFamily.DiceParity:
                switch (diceParityConditionPreset)
                {
                    case DiceParityConditionPreset.Even:
                        AddConditionClause(SkillConditionReference.AllBaseValuesEven, SkillConditionComparison.IsTrue);
                        break;
                    case DiceParityConditionPreset.Odd:
                        AddConditionClause(SkillConditionReference.AllBaseValuesOdd, SkillConditionComparison.IsTrue);
                        break;
                }
                break;

            case SkillConditionFamily.CritFail:
                switch (critFailConditionPreset)
                {
                    case CritFailConditionPreset.Crit:
                        AddConditionClause(SkillConditionReference.AllDiceCrit, SkillConditionComparison.IsTrue);
                        break;
                    case CritFailConditionPreset.Fail:
                        AddConditionClause(SkillConditionReference.AllDiceFail, SkillConditionComparison.IsTrue);
                        break;
                }
                break;

            case SkillConditionFamily.ExactValue:
                switch (exactConditionMode)
                {
                    case SkillExactConditionMode.DieEqualsX:
                    case SkillExactConditionMode.RandomExactNumberOwned:
                    case SkillExactConditionMode.RandomExactNumberRandom:
                        condition.logic = SkillConditionLogic.All;
                        AddConditionClause(SkillConditionReference.HighestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                        AddConditionClause(SkillConditionReference.LowestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                        break;
                    case SkillExactConditionMode.GroupContainsPattern:
                        AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.Equals, conditionPresetValue);
                        break;
                }
                break;

            case SkillConditionFamily.Resource:
                switch (resourceConditionPreset)
                {
                    case ResourceConditionPreset.CurrentFocusGreaterOrEqualN:
                        AddConditionClause(SkillConditionReference.CurrentFocus, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                        break;
                    case ResourceConditionPreset.PlayerGuardGreaterOrEqualN:
                        AddConditionClause(SkillConditionReference.CurrentGuard, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                        break;
                    case ResourceConditionPreset.TargetGuardGreaterOrEqualN:
                        AddConditionClause(SkillConditionReference.TargetGuard, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                        break;
                }
                break;

            case SkillConditionFamily.LocalGroupRelation:
                switch (localGroupRelationMode)
                {
                    case LocalGroupRelationMode.SelfPosition:
                    case LocalGroupRelationMode.NeighborRelation:
                        if (localGroupRelationSide == LocalGroupRelationSide.Left)
                            AddConditionClause(SkillConditionReference.IsLeftmostAction, SkillConditionComparison.IsTrue);
                        else
                            AddConditionClause(SkillConditionReference.IsRightmostAction, SkillConditionComparison.IsTrue);
                        break;
                    case LocalGroupRelationMode.SplitRole:
                        switch (localGroupConditionPreset)
                        {
                            case LocalGroupConditionPreset.Highest:
                                AddConditionClause(SkillConditionReference.HighestBaseValueInGroup, SkillConditionComparison.GreaterOrEqual, 1);
                                break;
                            case LocalGroupConditionPreset.Lowest:
                                AddConditionClause(SkillConditionReference.LowestBaseValueInGroup, SkillConditionComparison.GreaterOrEqual, 1);
                                break;
                        }
                        break;
                }
                break;

            case SkillConditionFamily.TargetState:
                switch (targetStateConditionPreset)
                {
                    case TargetStateConditionPreset.TargetHasBurn:
                        AddConditionClause(SkillConditionReference.TargetHasBurn, SkillConditionComparison.IsTrue);
                        break;
                    case TargetStateConditionPreset.TargetHasFreeze:
                        AddConditionClause(SkillConditionReference.TargetHasFreeze, SkillConditionComparison.IsTrue);
                        break;
                    case TargetStateConditionPreset.TargetHasChilled:
                        AddConditionClause(SkillConditionReference.TargetHasChilled, SkillConditionComparison.IsTrue);
                        break;
                    case TargetStateConditionPreset.TargetHasMark:
                        AddConditionClause(SkillConditionReference.TargetHasMark, SkillConditionComparison.IsTrue);
                        break;
                    case TargetStateConditionPreset.TargetHasBleed:
                        AddConditionClause(SkillConditionReference.TargetHasBleed, SkillConditionComparison.IsTrue);
                        break;
                    case TargetStateConditionPreset.TargetHasStagger:
                        AddConditionClause(SkillConditionReference.TargetHasStagger, SkillConditionComparison.IsTrue);
                        break;
                    case TargetStateConditionPreset.StatusHistoryTodo:
                        // Placeholder only. Runtime history condition is intentionally not implemented yet.
                        break;
                }
                break;

            case SkillConditionFamily.BoardState:
                condition.scope = SkillConditionScope.Global;
                switch (boardStateConditionPreset)
                {
                    case BoardStateConditionPreset.AliveEnemiesGreaterOrEqualN:
                        AddConditionClause(SkillConditionReference.AliveEnemiesCount, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                        break;
                    case BoardStateConditionPreset.EnemiesWithStatusGreaterOrEqualN:
                        AddConditionClause(SkillConditionReference.EnemiesWithStatusCount, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                        break;
                }
                break;
        }
    }

    private string BuildStandardConditionPreview()
    {
        switch (standardConditionFamily)
        {
            case SkillConditionFamily.DiceParity:
                switch (diceParityConditionPreset)
                {
                    case DiceParityConditionPreset.Even: return "Cụm dice đều chẵn";
                    case DiceParityConditionPreset.Odd: return "Cụm dice đều lẻ";
                }
                break;
            case SkillConditionFamily.CritFail:
                switch (critFailConditionPreset)
                {
                    case CritFailConditionPreset.Crit: return "Cụm dice đều là Crit";
                    case CritFailConditionPreset.Fail: return "Cụm dice đều là Fail";
                }
                break;
            case SkillConditionFamily.ExactValue:
                switch (exactConditionMode)
                {
                    case SkillExactConditionMode.DieEqualsX: return $"Die Equals X, X = {exactValueX}";
                    case SkillExactConditionMode.GroupContainsPattern: return $"Group Contains: {exactValuePattern}";
                    case SkillExactConditionMode.RandomExactNumberOwned: return "Random Exact Number Owned";
                    case SkillExactConditionMode.RandomExactNumberRandom: return "Random Exact Number Random";
                }
                break;
            case SkillConditionFamily.Resource:
                switch (resourceConditionPreset)
                {
                    case ResourceConditionPreset.CurrentFocusGreaterOrEqualN: return $"Focus hiện tại >= {conditionPresetValue}";
                    case ResourceConditionPreset.PlayerGuardGreaterOrEqualN: return $"Guard player >= {conditionPresetValue}";
                    case ResourceConditionPreset.TargetGuardGreaterOrEqualN: return $"Guard target >= {conditionPresetValue}";
                }
                break;
            case SkillConditionFamily.LocalGroupRelation:
                switch (localGroupRelationMode)
                {
                    case LocalGroupRelationMode.SelfPosition: return localGroupRelationSide == LocalGroupRelationSide.Left ? "Self-position: Left" : "Self-position: Right";
                    case LocalGroupRelationMode.NeighborRelation: return localGroupRelationSide == LocalGroupRelationSide.Left ? "Neighbor relation: Left" : "Neighbor relation: Right";
                    case LocalGroupRelationMode.SplitRole: return localGroupConditionPreset == LocalGroupConditionPreset.Highest ? "Split-role: Highest" : "Split-role: Lowest";
                }
                break;
            case SkillConditionFamily.TargetState:
                switch (targetStateConditionPreset)
                {
                    case TargetStateConditionPreset.TargetHasBurn: return "Target đang có Burn";
                    case TargetStateConditionPreset.TargetHasFreeze: return "Target đang Freeze";
                    case TargetStateConditionPreset.TargetHasChilled: return "Target đang Chilled";
                    case TargetStateConditionPreset.TargetHasMark: return "Target đang có Mark";
                    case TargetStateConditionPreset.TargetHasBleed: return "Target đang có Bleed";
                    case TargetStateConditionPreset.TargetHasStagger: return "Target đang Stagger";
                    case TargetStateConditionPreset.StatusHistoryTodo: return "Status History (TODO - chưa có runtime logic)";
                }
                break;
            case SkillConditionFamily.BoardState:
                switch (boardStateConditionPreset)
                {
                    case BoardStateConditionPreset.AliveEnemiesGreaterOrEqualN: return $"Số enemy còn sống >= {conditionPresetValue}";
                    case BoardStateConditionPreset.EnemiesWithStatusGreaterOrEqualN: return $"Số enemy đang có status >= {conditionPresetValue}";
                }
                break;
        }

        return "Chưa có condition chuẩn";
    }

    private void ApplyFirePreset()
    {
        switch (fireConditionPreset)
        {
            case FireConditionPreset.AnyBaseOdd:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.IsOdd);
                break;
            case FireConditionPreset.AnyBaseEven:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.IsEven);
                break;
            case FireConditionPreset.AnyDieCrit:
                AddConditionClause(SkillConditionReference.AnyDieCrit, SkillConditionComparison.IsTrue);
                break;
            case FireConditionPreset.AnyDieFail:
                AddConditionClause(SkillConditionReference.AnyDieFail, SkillConditionComparison.IsTrue);
                break;
            case FireConditionPreset.ExactAllBasesEqualN:
                condition.logic = SkillConditionLogic.All;
                AddConditionClause(SkillConditionReference.HighestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                AddConditionClause(SkillConditionReference.LowestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case FireConditionPreset.AnyBaseEqualsN:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case FireConditionPreset.HighestBaseEqualsN:
                AddConditionClause(SkillConditionReference.HighestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case FireConditionPreset.LowestBaseEqualsN:
                AddConditionClause(SkillConditionReference.LowestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case FireConditionPreset.CurrentFocusGreaterOrEqualN:
                AddConditionClause(SkillConditionReference.CurrentFocus, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                break;
            case FireConditionPreset.OccupiedSlotsEqualsN:
                AddConditionClause(SkillConditionReference.OccupiedSlots, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case FireConditionPreset.RemainingSlotsEqualsN:
                AddConditionClause(SkillConditionReference.RemainingSlots, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case FireConditionPreset.EnemiesWithBurnGreaterOrEqualN:
                condition.scope = SkillConditionScope.Global;
                AddConditionClause(SkillConditionReference.EnemiesWithBurnCount, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                break;
            case FireConditionPreset.TargetHasBurn:
                AddConditionClause(SkillConditionReference.TargetHasBurn, SkillConditionComparison.IsTrue);
                break;
        }
    }

    private void ApplyIcePreset()
    {
        switch (iceConditionPreset)
        {
            case IceConditionPreset.AnyBaseOdd:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.IsOdd);
                break;
            case IceConditionPreset.AnyBaseEven:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.IsEven);
                break;
            case IceConditionPreset.AnyDieCrit:
                AddConditionClause(SkillConditionReference.AnyDieCrit, SkillConditionComparison.IsTrue);
                break;
            case IceConditionPreset.AnyDieFail:
                AddConditionClause(SkillConditionReference.AnyDieFail, SkillConditionComparison.IsTrue);
                break;
            case IceConditionPreset.ExactAllBasesEqualN:
                condition.logic = SkillConditionLogic.All;
                AddConditionClause(SkillConditionReference.HighestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                AddConditionClause(SkillConditionReference.LowestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case IceConditionPreset.AnyBaseEqualsN:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case IceConditionPreset.HighestBaseEqualsN:
                AddConditionClause(SkillConditionReference.HighestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case IceConditionPreset.LowestBaseEqualsN:
                AddConditionClause(SkillConditionReference.LowestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case IceConditionPreset.CurrentFocusGreaterOrEqualN:
                AddConditionClause(SkillConditionReference.CurrentFocus, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                break;
            case IceConditionPreset.OccupiedSlotsEqualsN:
                AddConditionClause(SkillConditionReference.OccupiedSlots, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case IceConditionPreset.RemainingSlotsEqualsN:
                AddConditionClause(SkillConditionReference.RemainingSlots, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case IceConditionPreset.TargetHasFreeze:
                AddConditionClause(SkillConditionReference.TargetHasFreeze, SkillConditionComparison.IsTrue);
                break;
            case IceConditionPreset.TargetHasChilled:
                AddConditionClause(SkillConditionReference.TargetHasChilled, SkillConditionComparison.IsTrue);
                break;
        }
    }

    private void ApplyLightningPreset()
    {
        switch (lightningConditionPreset)
        {
            case LightningConditionPreset.AnyBaseOdd:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.IsOdd);
                break;
            case LightningConditionPreset.AnyBaseEven:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.IsEven);
                break;
            case LightningConditionPreset.AnyDieCrit:
                AddConditionClause(SkillConditionReference.AnyDieCrit, SkillConditionComparison.IsTrue);
                break;
            case LightningConditionPreset.AnyDieFail:
                AddConditionClause(SkillConditionReference.AnyDieFail, SkillConditionComparison.IsTrue);
                break;
            case LightningConditionPreset.AnyBaseEqualsN:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case LightningConditionPreset.HighestBaseEqualsN:
                AddConditionClause(SkillConditionReference.HighestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case LightningConditionPreset.LowestBaseEqualsN:
                AddConditionClause(SkillConditionReference.LowestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case LightningConditionPreset.CurrentFocusGreaterOrEqualN:
                AddConditionClause(SkillConditionReference.CurrentFocus, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                break;
            case LightningConditionPreset.OccupiedSlotsEqualsN:
                AddConditionClause(SkillConditionReference.OccupiedSlots, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case LightningConditionPreset.RemainingSlotsEqualsN:
                AddConditionClause(SkillConditionReference.RemainingSlots, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case LightningConditionPreset.MarkedEnemiesGreaterOrEqualN:
                condition.scope = SkillConditionScope.Global;
                AddConditionClause(SkillConditionReference.MarkedEnemiesCount, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                break;
            case LightningConditionPreset.TargetHasMark:
                AddConditionClause(SkillConditionReference.TargetHasMark, SkillConditionComparison.IsTrue);
                break;
        }
    }

    private void ApplyPhysicalPreset()
    {
        switch (physicalConditionPreset)
        {
            case PhysicalConditionPreset.AnyBaseOdd:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.IsOdd);
                break;
            case PhysicalConditionPreset.AnyBaseEven:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.IsEven);
                break;
            case PhysicalConditionPreset.AnyDieCrit:
                AddConditionClause(SkillConditionReference.AnyDieCrit, SkillConditionComparison.IsTrue);
                break;
            case PhysicalConditionPreset.AnyDieFail:
                AddConditionClause(SkillConditionReference.AnyDieFail, SkillConditionComparison.IsTrue);
                break;
            case PhysicalConditionPreset.AnyBaseEqualsN:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case PhysicalConditionPreset.HighestBaseEqualsN:
                AddConditionClause(SkillConditionReference.HighestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case PhysicalConditionPreset.LowestBaseEqualsN:
                AddConditionClause(SkillConditionReference.LowestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case PhysicalConditionPreset.CurrentFocusGreaterOrEqualN:
                AddConditionClause(SkillConditionReference.CurrentFocus, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                break;
            case PhysicalConditionPreset.OccupiedSlotsEqualsN:
                AddConditionClause(SkillConditionReference.OccupiedSlots, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case PhysicalConditionPreset.RemainingSlotsEqualsN:
                AddConditionClause(SkillConditionReference.RemainingSlots, SkillConditionComparison.Equals, conditionPresetValue);
                break;
        }
    }

    private void ApplyBleedPreset()
    {
        switch (bleedConditionPreset)
        {
            case BleedConditionPreset.AnyBaseOdd:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.IsOdd);
                break;
            case BleedConditionPreset.AnyBaseEven:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.IsEven);
                break;
            case BleedConditionPreset.AnyDieCrit:
                AddConditionClause(SkillConditionReference.AnyDieCrit, SkillConditionComparison.IsTrue);
                break;
            case BleedConditionPreset.AnyDieFail:
                AddConditionClause(SkillConditionReference.AnyDieFail, SkillConditionComparison.IsTrue);
                break;
            case BleedConditionPreset.AnyBaseEqualsN:
                AddConditionClause(SkillConditionReference.AnyBaseValue, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case BleedConditionPreset.HighestBaseEqualsN:
                AddConditionClause(SkillConditionReference.HighestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case BleedConditionPreset.LowestBaseEqualsN:
                AddConditionClause(SkillConditionReference.LowestBaseValueInGroup, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case BleedConditionPreset.CurrentFocusGreaterOrEqualN:
                AddConditionClause(SkillConditionReference.CurrentFocus, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                break;
            case BleedConditionPreset.OccupiedSlotsEqualsN:
                AddConditionClause(SkillConditionReference.OccupiedSlots, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case BleedConditionPreset.RemainingSlotsEqualsN:
                AddConditionClause(SkillConditionReference.RemainingSlots, SkillConditionComparison.Equals, conditionPresetValue);
                break;
            case BleedConditionPreset.TotalBleedOnBoardGreaterOrEqualN:
                condition.scope = SkillConditionScope.Global;
                AddConditionClause(SkillConditionReference.TotalBleedOnBoard, SkillConditionComparison.GreaterOrEqual, conditionPresetValue);
                break;
            case BleedConditionPreset.TargetHasBleed:
                AddConditionClause(SkillConditionReference.TargetHasBleed, SkillConditionComparison.IsTrue);
                break;
        }
    }

    private void AddConditionClause(SkillConditionReference reference, SkillConditionComparison comparison, int value = 0)
    {
        condition.clauses.Add(new SkillConditionClause
        {
            reference = reference,
            comparison = comparison,
            value = value
        });
    }
}

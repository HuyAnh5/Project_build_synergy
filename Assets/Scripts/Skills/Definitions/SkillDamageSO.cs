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
public partial class SkillDamageSO : ScriptableObject
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
    [TextArea(2, 4)]
    [HideInInspector]
    public string description;

    [TabGroup("Tabs", "Gameplay")]
    [TitleGroup("Tabs/Gameplay/New Gameplay Schema", "Data-driven authoring. Runtime stays legacy until Use New Gameplay Pipeline is enabled.")]
    [HideLabel]
    public SkillGameplayData gameplay = new SkillGameplayData();

    [TabGroup("Tabs", "Gameplay")]
    [TitleGroup("Tabs/Gameplay/Summary")]
    [ShowInInspector, ReadOnly, HideLabel, MultiLineProperty(5)]
    public string GameplaySummary => BuildGameplaySummary();

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Spec/Metadata")]
    [InlineProperty]
    public SkillSpecMetadata spec = new SkillSpecMetadata();

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Spec/Behavior")]
    [LabelText("Behavior Family")]
    [ShowIf("@kind == SkillKind.Attack && element != ElementTag.Fire")]
    [EnumToggleButtons]
    public DamageBehaviorFamily behaviorFamily = DamageBehaviorFamily.None;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Spec/Behavior")]
    [LabelText("Fire Behavior")]
    public FireDamageBehaviorId fireBehaviorId = FireDamageBehaviorId.None;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Spec/Behavior")]
    [LabelText("Ice Behavior")]
    [ShowIf("@kind == SkillKind.Attack && behaviorFamily == DamageBehaviorFamily.Ice")]
    public IceDamageBehaviorId iceBehaviorId = IceDamageBehaviorId.None;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Spec/Behavior")]
    [LabelText("Lightning Behavior")]
    [ShowIf("@kind == SkillKind.Attack && behaviorFamily == DamageBehaviorFamily.Lightning")]
    public LightningDamageBehaviorId lightningBehaviorId = LightningDamageBehaviorId.None;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Spec/Behavior")]
    [LabelText("Bleed Behavior")]
    [ShowIf("@kind == SkillKind.Attack && behaviorFamily == DamageBehaviorFamily.Bleed")]
    public BleedDamageBehaviorId bleedBehaviorId = BleedDamageBehaviorId.None;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Spec/Behavior")]
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

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Damage")]
    [ShowIf("@kind == SkillKind.Attack")]
    [EnumToggleButtons]
    [LabelText("Damage Mode")]
    public BaseEffectValueMode baseDamageValueMode = BaseEffectValueMode.Flat;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Damage")]
    [ShowIf("@kind == SkillKind.Attack")]
    [Range(0f, 2f)]
    public float dieMultiplier = 1f;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Damage")]
    [ShowIf("@kind == SkillKind.Attack && baseDamageValueMode == BaseEffectValueMode.Flat")]
    public int flatDamage = 0;

    // -------------------- SUNDER BONUS --------------------

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [ShowIf("@group == DamageGroup.Sunder")]
    [FoldoutGroup("Tabs/Legacy/Effects/Sunder Bonus", expanded: false)]
    public bool sunderBonusIfTargetHasGuard = true;

    [ShowIf("@group == DamageGroup.Sunder && sunderBonusIfTargetHasGuard")]
    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [FoldoutGroup("Tabs/Legacy/Effects/Sunder Bonus", expanded: false)]
    [Min(0f)]
    public float sunderGuardDamageMultiplier = 2f;

    // -------------------- GUARD --------------------

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Guard")]
    [ShowIf("@kind == SkillKind.Guard")]
    [EnumToggleButtons]
    [LabelText("Guard Mode")]
    public BaseEffectValueMode guardValueMode = BaseEffectValueMode.X;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Guard")]
    [ShowIf("@kind == SkillKind.Guard")]
    [FormerlySerializedAs("guardDieMultiplier")]
    public float legacyGuardDieMultiplier = 1f;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Guard")]
    [ShowIf("@kind == SkillKind.Guard && guardValueMode == BaseEffectValueMode.Flat")]
    [LabelText("Guard Flat")]
    public int guardFlat = 0;

    // -------------------- SPECIAL COMBAT --------------------

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Special Combat")]
    [ShowIf("@kind == SkillKind.Attack")]
    public bool bypassGuard = false;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Special Combat")]
    [ShowIf("@kind == SkillKind.Attack")]
    public bool clearsGuard = false;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Special Combat")]
    [ShowIf("@kind == SkillKind.Attack")]
    public bool canUseMarkMultiplier = true;

    // -------------------- BURN SPENDER --------------------

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Burn Spender (Fire)")]
    [ShowIf(nameof(ShowBurnSpenderSection))]
    public bool consumesBurn = false;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Burn Spender (Fire)")]
    [ShowIf("@ShowBurnSpenderSection && consumesBurn")]
    [Min(0)]
    public int burnDamagePerStack = 2;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Frozen Or Chilled Reward (Ice)")]
    [ShowIf(nameof(ShowIceRewardSection))]
    [LabelText("Reward On Frozen/Chilled Hit")]
    public bool gainIceRewardOnFrozenOrChilledHit = true;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Frozen Or Chilled Reward (Ice)")]
    [ShowIf("@ShowIceRewardSection && gainIceRewardOnFrozenOrChilledHit")]
    [Min(0)]
    [LabelText("Focus Gain")]
    public int iceRewardFocus = 1;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Frozen Or Chilled Reward (Ice)")]
    [ShowIf("@ShowIceRewardSection && gainIceRewardOnFrozenOrChilledHit")]
    [Min(0)]
    [LabelText("Guard Gain")]
    public int iceRewardGuard = 3;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Mark Shock (Lightning)")]
    [ShowIf(nameof(ShowLightningMarkShockSection))]
    [LabelText("Shock On Mark Hit")]
    public bool triggerLightningMarkShock = true;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Mark Shock (Lightning)")]
    [ShowIf("@ShowLightningMarkShockSection && triggerLightningMarkShock")]
    [Min(0)]
    [LabelText("Shock Damage")]
    public int lightningMarkShockDamage = 3;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Fire Modules")]
    public FireAttackModuleData fireModules = new FireAttackModuleData();

    // -------------------- APPLY STATUS --------------------

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Apply Status")]
    [ShowIf(nameof(ShowBurnStatusSection))]
    public bool applyBurn;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Apply Status")]
    [ShowIf("@ShowBurnStatusSection && applyBurn")]
    [EnumToggleButtons]
    [LabelText("Burn Mode")]
    public BaseEffectValueMode baseBurnValueMode = BaseEffectValueMode.Flat;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Apply Status")]
    [ShowIf("@ShowBurnStatusSection && applyBurn && baseBurnValueMode == BaseEffectValueMode.Flat")]
    [Min(0)]
    public int burnAddStacks = 2;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Apply Status")]
    [ShowIf("@ShowBurnStatusSection && applyBurn")]
    [Min(0)]
    public int burnRefreshTurns = 3;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Apply Status")]
    [ShowIf(nameof(ShowMarkStatusSection))]
    public bool applyMark;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Apply Status")]
    [ShowIf(nameof(ShowBleedStatusSection))]
    public bool applyBleed;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Apply Status")]
    [ShowIf("@ShowBleedStatusSection && applyBleed")]
    [Min(0)]
    public int bleedTurns = 3;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Apply Status")]
    [ShowIf(nameof(ShowFreezeStatusSection))]
    public bool applyFreeze;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/Apply Status")]
    [ShowIf("@ShowFreezeStatusSection && applyFreeze")]
    [Range(0f, 1f)]
    public float freezeChance = 0.4f;

    // -------------------- VFX --------------------

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Effects/VFX")]
    [ShowIf("@kind == SkillKind.Attack && range == RangeType.Ranged")]
    public Projectile2D projectilePrefab;

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

    private bool IsMigratedToGameplay => gameplay != null && gameplay.useNewGameplayPipeline;
    private bool ShowConditionBuilder => hasCondition && kind == SkillKind.Attack;
    private bool ShowElementEffectSection => kind == SkillKind.Attack && group == DamageGroup.Effect;
    private bool ShowBurnStatusSection => ShowElementEffectSection && element == ElementTag.Fire;
    private bool ShowFreezeStatusSection => ShowElementEffectSection && element == ElementTag.Ice;
    private bool ShowMarkStatusSection => ShowElementEffectSection && element == ElementTag.Lightning;
    private bool ShowBleedStatusSection => false;
    private bool ShowBurnSpenderSection => kind == SkillKind.Attack && element == ElementTag.Fire;
    private bool ShowIceRewardSection => kind == SkillKind.Attack && element == ElementTag.Ice;
    private bool ShowLightningMarkShockSection => kind == SkillKind.Attack && element == ElementTag.Lightning;
    private bool ShowExactSingleValueInput =>
        exactConditionMode == SkillExactConditionMode.DieEqualsX;
    private bool ShowExactPatternInput =>
        exactConditionMode == SkillExactConditionMode.GroupContainsPattern;
    private bool ShowLocalGroupSideSelection =>
        localGroupRelationMode == LocalGroupRelationMode.SelfPosition ||
        localGroupRelationMode == LocalGroupRelationMode.NeighborRelation;

}




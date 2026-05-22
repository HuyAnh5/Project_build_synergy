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

    private void SeedConsumeBurnGameplayDataIfEmpty()
    {
        bool isConsumeBurn = string.Equals(displayName, "Burn Consume", System.StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(displayName, "Consume Burn", System.StringComparison.OrdinalIgnoreCase) ||
                             (!string.IsNullOrEmpty(name) && name.ToLowerInvariant().Contains("consume_burn"));
        if (!isConsumeBurn)
            return;

        if (gameplay != null && gameplay.baseEffects != null && gameplay.baseEffects.Count > 0)
        {
            gameplay.useNewGameplayPipeline = true;
            EnsureDefaultGameplayDescription();
            return;
        }

        SeedConsumeBurnGameplayData();
    }
    private void SeedFireSlashGameplayDataIfEmpty()
    {
        bool isFireSlash = fireBehaviorId == FireDamageBehaviorId.FireSlash || string.Equals(displayName, "Fire Slash", System.StringComparison.OrdinalIgnoreCase);
        if (!isFireSlash)
            return;

        if (gameplay != null && gameplay.baseEffects != null && gameplay.baseEffects.Count > 0)
        {
            gameplay.useNewGameplayPipeline = true;
            EnsureDefaultGameplayDescription();
            return;
        }

        SeedFireSlashGameplayData();
    }

    private void SeedMigratedFireGameplayDataIfEmpty()
    {
        bool isIgnite = fireBehaviorId == FireDamageBehaviorId.Ignite || string.Equals(displayName, "Ignite", System.StringComparison.OrdinalIgnoreCase);
        bool isHellfire = fireBehaviorId == FireDamageBehaviorId.Hellfire || string.Equals(displayName, "Hellfire", System.StringComparison.OrdinalIgnoreCase);
        bool isBiteTheDust = fireBehaviorId == FireDamageBehaviorId.BiteTheDust || string.Equals(displayName, "Bite the Dust", System.StringComparison.OrdinalIgnoreCase);
        if (!isIgnite && !isHellfire && !isBiteTheDust)
            return;

        if (gameplay != null && gameplay.baseEffects != null && gameplay.baseEffects.Count > 0)
        {
            gameplay.useNewGameplayPipeline = true;
            EnsureDefaultGameplayDescription();
            if (isHellfire)
                MigrateHellfireMatchSevenEffectToCondition();
            return;
        }

        if (isIgnite)
            SeedIgniteGameplayData();
        else if (isHellfire)
            SeedHellfireGameplayData();
        else if (isBiteTheDust)
            SeedBiteTheDustGameplayData();
    }
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

    // ------------------- Condition -------------------

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Condition/Rule")]
    [ToggleLeft]
    [LabelText("Has Condition")]
    public bool hasCondition = false;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [BoxGroup("Tabs/Legacy/Condition/Rule")]
    [ShowIf(nameof(hasCondition))]
    [EnumToggleButtons]
    [LabelText("Mode")]
    public ConditionEditorMode conditionEditorMode = ConditionEditorMode.Builder;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [VerticalGroup("Tabs/Legacy/Condition/Stack")]
    [BoxGroup("Tabs/Legacy/Condition/Stack/Builder")]
    public bool useSystemConditionPreset = true;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [VerticalGroup("Tabs/Legacy/Condition/Stack")]
    [BoxGroup("Tabs/Legacy/Condition/Stack/Builder")]
    [ShowIf(nameof(ShowConditionBuilder))]
    [EnumToggleButtons]
    [LabelText("Builder")]
    public SkillConditionFamily standardConditionFamily = SkillConditionFamily.DiceParity;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [VerticalGroup("Tabs/Legacy/Condition/Stack")]
    [BoxGroup("Tabs/Legacy/Condition/Stack/Option")]
    [ShowIf("@ShowConditionBuilder && standardConditionFamily == SkillConditionFamily.DiceParity")]
    [LabelText("Option")]
    [ValueDropdown(nameof(GetDiceParityOptions))]
    public DiceParityConditionPreset diceParityConditionPreset = DiceParityConditionPreset.Even;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [VerticalGroup("Tabs/Legacy/Condition/Stack")]
    [BoxGroup("Tabs/Legacy/Condition/Stack/Option")]
    [ShowIf("@ShowConditionBuilder && standardConditionFamily == SkillConditionFamily.CritFail")]
    [LabelText("Option")]
    [ValueDropdown(nameof(GetCritFailOptions))]
    public CritFailConditionPreset critFailConditionPreset = CritFailConditionPreset.Crit;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [VerticalGroup("Tabs/Legacy/Condition/Stack")]
    [BoxGroup("Tabs/Legacy/Condition/Stack/Option")]
    [ShowIf("@ShowConditionBuilder && standardConditionFamily == SkillConditionFamily.ExactValue")]
    [EnumToggleButtons]
    [LabelText("Exact Mode")]
    [FormerlySerializedAs("exactValueConditionPreset")]
    public SkillExactConditionMode exactConditionMode = SkillExactConditionMode.DieEqualsX;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [VerticalGroup("Tabs/Legacy/Condition/Stack")]
    [BoxGroup("Tabs/Legacy/Condition/Stack/Option")]
    [ShowIf("@ShowConditionBuilder && standardConditionFamily == SkillConditionFamily.ExactValue && ShowExactSingleValueInput")]
    [LabelText("Exact X")]
    public int exactValueX = 7;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [VerticalGroup("Tabs/Legacy/Condition/Stack")]
    [BoxGroup("Tabs/Legacy/Condition/Stack/Option")]
    [ShowIf("@ShowConditionBuilder && standardConditionFamily == SkillConditionFamily.ExactValue && ShowExactPatternInput")]
    [LabelText("Pattern")]
    [InfoBox("Nh?p d�y s? b?ng d?u '-', ',', ho?c kho?ng tr?ng. V� d?: 1-2-3-5", InfoMessageType.Info)]
    public string exactValuePattern = "1-2-3";

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [VerticalGroup("Tabs/Legacy/Condition/Stack")]
    [BoxGroup("Tabs/Legacy/Condition/Stack/Option")]
    [ShowIf("@ShowConditionBuilder && standardConditionFamily == SkillConditionFamily.Resource")]
    [LabelText("Option")]
    [ValueDropdown(nameof(GetResourceOptions))]
    public ResourceConditionPreset resourceConditionPreset = ResourceConditionPreset.CurrentFocusGreaterOrEqualN;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [VerticalGroup("Tabs/Legacy/Condition/Stack")]
    [BoxGroup("Tabs/Legacy/Condition/Stack/Option")]
    [ShowIf("@ShowConditionBuilder && standardConditionFamily == SkillConditionFamily.LocalGroupRelation")]
    [EnumToggleButtons]
    [LabelText("Relation")]
    public LocalGroupRelationMode localGroupRelationMode = LocalGroupRelationMode.SelfPosition;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [VerticalGroup("Tabs/Legacy/Condition/Stack")]
    [BoxGroup("Tabs/Legacy/Condition/Stack/Option")]
    [ShowIf("@ShowConditionBuilder && standardConditionFamily == SkillConditionFamily.LocalGroupRelation && ShowLocalGroupSideSelection")]
    [EnumToggleButtons]
    [LabelText("Side")]
    public LocalGroupRelationSide localGroupRelationSide = LocalGroupRelationSide.Left;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [VerticalGroup("Tabs/Legacy/Condition/Stack")]
    [BoxGroup("Tabs/Legacy/Condition/Stack/Option")]
    [ShowIf("@ShowConditionBuilder && standardConditionFamily == SkillConditionFamily.LocalGroupRelation && localGroupRelationMode == LocalGroupRelationMode.SplitRole")]
    [EnumToggleButtons]
    [LabelText("Split Role")]
    public LocalGroupConditionPreset localGroupConditionPreset = LocalGroupConditionPreset.Highest;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [VerticalGroup("Tabs/Legacy/Condition/Stack")]
    [BoxGroup("Tabs/Legacy/Condition/Stack/Option")]
    [ShowIf("@ShowConditionBuilder && standardConditionFamily == SkillConditionFamily.TargetState")]
    [EnumToggleButtons]
    [LabelText("Option")]
    [InfoBox("Status history l� note thi?t k? d? m? r?ng sau; runtime hi?n chua c� logic cho history.", InfoMessageType.Info)]
    public TargetStateConditionPreset targetStateConditionPreset = TargetStateConditionPreset.TargetHasBurn;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [VerticalGroup("Tabs/Legacy/Condition/Stack")]
    [BoxGroup("Tabs/Legacy/Condition/Stack/Option")]
    [ShowIf("@ShowConditionBuilder && standardConditionFamily == SkillConditionFamily.BoardState")]
    [LabelText("Option")]
    [ValueDropdown(nameof(GetBoardStateOptions))]
    public BoardStateConditionPreset boardStateConditionPreset = BoardStateConditionPreset.AliveEnemiesGreaterOrEqualN;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [VerticalGroup("Tabs/Legacy/Condition/Stack")]
    [BoxGroup("Tabs/Legacy/Condition/Stack/Legacy")]
    [ShowIf("@ShowConditionBuilder && useSystemConditionPreset && element == ElementTag.Fire")]
    [LabelText("Fire Preset (Legacy)")]
    public FireConditionPreset fireConditionPreset = FireConditionPreset.None;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [VerticalGroup("Tabs/Legacy/Condition/Stack")]
    [BoxGroup("Tabs/Legacy/Condition/Stack/Summary")]
    [ShowIf(nameof(ShowConditionBuilder))]
    [DisplayAsString(false)]
    [LabelText("Summary")]
    public string conditionPreviewText => BuildStandardConditionPreview();

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [VerticalGroup("Tabs/Legacy/Condition/Stack")]
    [BoxGroup("Tabs/Legacy/Condition/Stack/Legacy")]
    [ShowIf("@ShowConditionBuilder && useSystemConditionPreset && element == ElementTag.Ice")]
    [LabelText("Ice Preset")]
    public IceConditionPreset iceConditionPreset = IceConditionPreset.None;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [VerticalGroup("Tabs/Legacy/Condition/Stack")]
    [BoxGroup("Tabs/Legacy/Condition/Stack/Legacy")]
    [ShowIf("@ShowConditionBuilder && useSystemConditionPreset && element == ElementTag.Lightning")]
    [LabelText("Lightning Preset")]
    public LightningConditionPreset lightningConditionPreset = LightningConditionPreset.None;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [VerticalGroup("Tabs/Legacy/Condition/Stack")]
    [BoxGroup("Tabs/Legacy/Condition/Stack/Legacy")]
    [ShowIf("@ShowConditionBuilder && useSystemConditionPreset && element == ElementTag.Physical")]
    [LabelText("Physical Preset")]
    public PhysicalConditionPreset physicalConditionPreset = PhysicalConditionPreset.None;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [VerticalGroup("Tabs/Legacy/Condition/Stack")]
    [BoxGroup("Tabs/Legacy/Condition/Stack/Legacy")]
    [ShowIf("@ShowConditionBuilder && useSystemConditionPreset && behaviorFamily == DamageBehaviorFamily.Bleed")]
    [LabelText("Bleed Preset")]
    public BleedConditionPreset bleedConditionPreset = BleedConditionPreset.None;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [VerticalGroup("Tabs/Legacy/Condition/Stack")]
    [BoxGroup("Tabs/Legacy/Condition/Stack/Builder")]
    [ShowIf("@ShowConditionBuilder && CurrentPresetNeedsValue()")]
    [LabelText("Preset N")]
    public int conditionPresetValue = 7;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [VerticalGroup("Tabs/Legacy/Condition/Stack")]
    [BoxGroup("Tabs/Legacy/Condition/Stack/Advanced")]
    [ShowIf(nameof(ShowConditionBuilder))]
    public bool showAdvancedCondition = false;

    [HideInInspector]
    [SerializeField]
    private bool exactConditionMigrated = false;

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [VerticalGroup("Tabs/Legacy/Condition/Stack")]
    [BoxGroup("Tabs/Legacy/Condition/Stack/Advanced")]
    [ShowIf("@hasCondition && conditionEditorMode == ConditionEditorMode.Advanced")]
    [PropertySpace(4)]
    [HideLabel]
    public SkillConditionData condition = new SkillConditionData();

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [VerticalGroup("Tabs/Legacy/Condition/Stack")]
    [ShowIf(nameof(ShowConditionBuilder))]
    [InfoBox("Builder ch? ch?n tr?c di?u ki?n. If Condition Met m?i quy?t d?nh skill du?c g�. Split Role d�ng cho skill ki?u Lowest -> Burn / Highest -> Guard.", InfoMessageType.Info)]
    [PropertySpace(SpaceBefore = 4, SpaceAfter = 6)]
    public SkillDamageConditionalOverrides whenConditionIsMet = new SkillDamageConditionalOverrides();

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [VerticalGroup("Tabs/Legacy/Condition/Stack")]
    [BoxGroup("Tabs/Legacy/Condition/Stack/If Condition Met")]
    [ShowIf(nameof(ShowConditionBuilder))]
    [InlineProperty]
    [HideLabel]
    public SkillConditionalOutcomeData conditionalOutcome = new SkillConditionalOutcomeData();

    [HideInInspector]
    [HideIf(nameof(IsMigratedToGameplay))]
    [VerticalGroup("Tabs/Legacy/Condition/Stack")]
    [BoxGroup("Tabs/Legacy/Condition/Stack/Split Role")]
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

    // -------------------- QUICK PRESETS --------------------

    private void PresetBasicStrike()
    {
        PresetMeleeStrike();
        coreAction = CoreAction.BasicStrike;
    }

    private void PresetBasicGuard()
    {
        PresetGuard();
        coreAction = CoreAction.BasicGuard;
    }

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

    private void PresetSunder()
    {
        coreAction = CoreAction.None;

        kind = SkillKind.Attack;
        target = SkillTargetRule.SingleEnemy;
        group = DamageGroup.Sunder;
        element = ElementTag.Physical;
        range = RangeType.Melee;

        bypassGuard = true;
        clearsGuard = false;
        canUseMarkMultiplier = true;
        consumesBurn = false;

        applyBurn = applyMark = applyBleed = applyFreeze = false;
    }

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

        // b?n b?t c�c apply* tu? skill
    }

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

    public string GetAuthoringDescription()
    {
        if (gameplay != null && !string.IsNullOrWhiteSpace(gameplay.descriptionTemplate))
            return gameplay.descriptionTemplate.Trim();

        return (description ?? string.Empty).Trim();
    }

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
    private void EnsureDefaultGameplayDescription()
    {
        if (gameplay == null || !string.IsNullOrWhiteSpace(gameplay.descriptionTemplate))
            return;

        if (string.Equals(displayName, "Fire Slash", System.StringComparison.OrdinalIgnoreCase) || fireBehaviorId == FireDamageBehaviorId.FireSlash)
            gameplay.descriptionTemplate = "Deal {base1} damage. If {Odd}, apply {cond1_1} {Burn}.";
        else if (string.Equals(displayName, "Ignite", System.StringComparison.OrdinalIgnoreCase) || fireBehaviorId == FireDamageBehaviorId.Ignite)
            gameplay.descriptionTemplate = "Apply {base1} {Burn}. If {Odd}, apply {cond1_1} more {Burn}.";
        else if (string.Equals(displayName, "Hellfire", System.StringComparison.OrdinalIgnoreCase) || fireBehaviorId == FireDamageBehaviorId.Hellfire)
            gameplay.descriptionTemplate = "{Consume} all {Burn}. Deal {base2} damage.";
        else if (string.Equals(displayName, "Bite the Dust", System.StringComparison.OrdinalIgnoreCase) || fireBehaviorId == FireDamageBehaviorId.BiteTheDust)
            gameplay.descriptionTemplate = "{Consume} all {Burn}. Deal {base2} damage. Heal {base3} HP.";
        else if (string.Equals(displayName, "Burn Consume", System.StringComparison.OrdinalIgnoreCase) || string.Equals(displayName, "Consume Burn", System.StringComparison.OrdinalIgnoreCase) || (!string.IsNullOrEmpty(name) && name.ToLowerInvariant().Contains("consume_burn")))
            gameplay.descriptionTemplate = "{Consume} all {Burn}. Deal {base2} damage.";
    }

    [TabGroup("Tabs", "Gameplay")]
    [Button("Seed Fire Slash Gameplay Data")]
    [ShowIf("@displayName == \"Fire Slash\" || fireBehaviorId == FireDamageBehaviorId.FireSlash")]
    private void SeedFireSlashGameplayData()
    {
        if (gameplay == null)
            gameplay = new SkillGameplayData();

        gameplay.useNewGameplayPipeline = true;
        gameplay.descriptionTemplate = "Deal {base1} damage. If {Odd}, apply {cond1_1} {Burn}.";
        if (gameplay.requirements == null) gameplay.requirements = new List<SkillRequirementData>();
        if (gameplay.baseEffects == null) gameplay.baseEffects = new List<SkillEffectData>();
        if (gameplay.conditionalOutcomes == null) gameplay.conditionalOutcomes = new List<SkillConditionalOutcomeDataV2>();

        gameplay.requirements.Clear();
        gameplay.baseEffects.Clear();
        gameplay.conditionalOutcomes.Clear();

        gameplay.baseEffects.Add(new SkillEffectData
        {
            type = SkillEffectType.DealDamage,
            target = SkillEffectTarget.SelectedEnemy,
            value = new SkillValueData
            {
                baseAmount = 2,
                mode = SkillValueMode.AddedValueScaled
            },
            previewable = true
        });

        gameplay.conditionalOutcomes.Add(new SkillConditionalOutcomeDataV2
        {
            condition = new SkillConditionData
            {
                scope = SkillConditionScope.SlotBound,
                logic = SkillConditionLogic.All,
                clauses = new List<SkillConditionClause>
                {
                    new SkillConditionClause
                    {
                        reference = SkillConditionReference.AnyBaseValue,
                        comparison = SkillConditionComparison.IsOdd,
                        value = 0
                    }
                }
            },
            effects = new List<SkillEffectData>
            {
                new SkillEffectData
                {
                    type = SkillEffectType.ApplyStatus,
                    target = SkillEffectTarget.SelectedEnemy,
                    status = StatusKind.Burn,
                    value = new SkillValueData
                    {
                        baseAmount = 6,
                        mode = SkillValueMode.Fixed
                    },
                    previewable = true
                }
            }
        });
    }
    [TabGroup("Tabs", "Gameplay")]
    [Button("Seed Consume Burn Gameplay Data")]
    [ShowIf("@displayName == \"Burn Consume\" || displayName == \"Consume Burn\"")]
    private void SeedConsumeBurnGameplayData()
    {
        if (gameplay == null)
            gameplay = new SkillGameplayData();

        gameplay.useNewGameplayPipeline = true;
        gameplay.descriptionTemplate = "{Consume} all {Burn}. Deal {base2} damage.";
        if (gameplay.requirements == null) gameplay.requirements = new List<SkillRequirementData>();
        if (gameplay.baseEffects == null) gameplay.baseEffects = new List<SkillEffectData>();
        if (gameplay.conditionalOutcomes == null) gameplay.conditionalOutcomes = new List<SkillConditionalOutcomeDataV2>();

        gameplay.requirements.Clear();
        gameplay.baseEffects.Clear();
        gameplay.conditionalOutcomes.Clear();

        gameplay.requirements.Add(new SkillRequirementData
        {
            type = SkillRequirementType.Condition,
            failureText = "Target needs Burn.",
            condition = new SkillConditionData
            {
                scope = SkillConditionScope.SlotBound,
                logic = SkillConditionLogic.All,
                clauses = new List<SkillConditionClause>
                {
                    new SkillConditionClause
                    {
                        reference = SkillConditionReference.TargetHasBurn,
                        comparison = SkillConditionComparison.IsTrue,
                        value = 0
                    }
                }
            }
        });

        gameplay.baseEffects.Add(new SkillEffectData
        {
            type = SkillEffectType.ConsumeStatus,
            target = SkillEffectTarget.SelectedEnemy,
            status = StatusKind.Burn,
            previewable = true
        });

        gameplay.baseEffects.Add(new SkillEffectData
        {
            type = SkillEffectType.DealDamage,
            target = SkillEffectTarget.SelectedEnemy,
            value = new SkillValueData
            {
                baseAmount = 2,
                mode = SkillValueMode.ConsumedStatusStacksScaled,
                status = StatusKind.Burn
            },
            previewable = true
        });
    }

    [TabGroup("Tabs", "Gameplay")]
    [Button("Seed Ignite Gameplay Data")]
    [ShowIf("@displayName == \"Ignite\" || fireBehaviorId == FireDamageBehaviorId.Ignite")]
    private void SeedIgniteGameplayData()
    {
        EnsureGameplayCollections();
        gameplay.useNewGameplayPipeline = true;
        gameplay.descriptionTemplate = "Apply {base1} {Burn}. If {Odd}, apply {cond1_1} more {Burn}.";

        gameplay.requirements.Clear();
        gameplay.baseEffects.Clear();
        gameplay.conditionalOutcomes.Clear();

        gameplay.baseEffects.Add(new SkillEffectData
        {
            type = SkillEffectType.ApplyStatus,
            target = SkillEffectTarget.SelectedEnemy,
            status = StatusKind.Burn,
            value = new SkillValueData
            {
                baseAmount = 3,
                mode = SkillValueMode.AddedValueScaled
            },
            previewable = true
        });

        gameplay.conditionalOutcomes.Add(new SkillConditionalOutcomeDataV2
        {
            condition = BuildParityCondition(SkillConditionComparison.IsOdd),
            effects = new List<SkillEffectData>
            {
                new SkillEffectData
                {
                    type = SkillEffectType.ApplyStatus,
                    target = SkillEffectTarget.SelectedEnemy,
                    status = StatusKind.Burn,
                    value = new SkillValueData
                    {
                        baseAmount = 2,
                        mode = SkillValueMode.Fixed
                    },
                    previewable = true
                }
            }
        });
    }

    [TabGroup("Tabs", "Gameplay")]
    [Button("Seed Hellfire Gameplay Data")]
    [ShowIf("@displayName == \"Hellfire\" || fireBehaviorId == FireDamageBehaviorId.Hellfire")]
    private void SeedHellfireGameplayData()
    {
        EnsureGameplayCollections();
        gameplay.useNewGameplayPipeline = true;
        gameplay.descriptionTemplate = "{Consume} all {Burn}. Deal {base2} damage.";

        gameplay.requirements.Clear();
        gameplay.baseEffects.Clear();
        gameplay.conditionalOutcomes.Clear();

        gameplay.requirements.Add(BuildTargetHasBurnRequirement());

        gameplay.baseEffects.Add(new SkillEffectData
        {
            type = SkillEffectType.ConsumeStatus,
            target = SkillEffectTarget.SelectedEnemy,
            status = StatusKind.Burn,
            previewable = true
        });

        gameplay.baseEffects.Add(new SkillEffectData
        {
            type = SkillEffectType.DealDamage,
            target = SkillEffectTarget.SelectedEnemy,
            value = new SkillValueData
            {
                baseAmount = 3,
                mode = SkillValueMode.ConsumedStatusStacksScaled,
                status = StatusKind.Burn
            },
            previewable = true
        });

        gameplay.conditionalOutcomes.Add(new SkillConditionalOutcomeDataV2
        {
            condition = new SkillConditionData
            {
                scope = SkillConditionScope.SlotBound,
                logic = SkillConditionLogic.All,
                clauses = new List<SkillConditionClause>
                {
                    new SkillConditionClause
                    {
                        reference = SkillConditionReference.AnyBaseValue,
                        comparison = SkillConditionComparison.Equals,
                        value = 7
                    }
                }
            },
            effects = new List<SkillEffectData>
            {
                new SkillEffectData
                {
                    type = SkillEffectType.ApplyStatus,
                    target = SkillEffectTarget.SelectedEnemy,
                    status = StatusKind.Burn,
                    value = new SkillValueData
                    {
                        baseAmount = 7,
                        mode = SkillValueMode.MatchingBaseValueCountScaled,
                        matchBaseValue = 7
                    },
                    previewable = true
                }
            }
        });
    }

    private void MigrateHellfireMatchSevenEffectToCondition()
    {
        if (gameplay == null || gameplay.baseEffects == null)
            return;

        SkillEffectData matchSevenEffect = null;
        for (int i = gameplay.baseEffects.Count - 1; i >= 0; i--)
        {
            SkillEffectData effect = gameplay.baseEffects[i];
            if (effect == null ||
                effect.type != SkillEffectType.ApplyStatus ||
                effect.status != StatusKind.Burn ||
                effect.value == null ||
                effect.value.mode != SkillValueMode.MatchingBaseValueCountScaled ||
                effect.value.matchBaseValue != 7)
            {
                continue;
            }

            matchSevenEffect = effect;
            gameplay.baseEffects.RemoveAt(i);
        }

        if (matchSevenEffect == null)
            return;

        if (gameplay.conditionalOutcomes == null)
            gameplay.conditionalOutcomes = new List<SkillConditionalOutcomeDataV2>();

        bool alreadyHasMatchSevenCondition = false;
        for (int i = 0; i < gameplay.conditionalOutcomes.Count; i++)
        {
            SkillConditionalOutcomeDataV2 branch = gameplay.conditionalOutcomes[i];
            if (branch == null || branch.condition == null || branch.condition.clauses == null)
                continue;

            for (int clauseIndex = 0; clauseIndex < branch.condition.clauses.Count; clauseIndex++)
            {
                SkillConditionClause clause = branch.condition.clauses[clauseIndex];
                if (clause != null &&
                    clause.reference == SkillConditionReference.AnyBaseValue &&
                    clause.comparison == SkillConditionComparison.Equals &&
                    clause.value == 7)
                {
                    alreadyHasMatchSevenCondition = true;
                    if (branch.effects == null)
                        branch.effects = new List<SkillEffectData>();
                    branch.effects.Add(matchSevenEffect);
                    break;
                }
            }
        }

        if (!alreadyHasMatchSevenCondition)
        {
            gameplay.conditionalOutcomes.Add(new SkillConditionalOutcomeDataV2
            {
                condition = new SkillConditionData
                {
                    scope = SkillConditionScope.SlotBound,
                    logic = SkillConditionLogic.All,
                    clauses = new List<SkillConditionClause>
                    {
                        new SkillConditionClause
                        {
                            reference = SkillConditionReference.AnyBaseValue,
                            comparison = SkillConditionComparison.Equals,
                            value = 7
                        }
                    }
                },
                effects = new List<SkillEffectData> { matchSevenEffect }
            });
        }

        gameplay.descriptionTemplate = "{Consume} all {Burn}. Deal {base2} damage.";
    }

    [TabGroup("Tabs", "Gameplay")]
    [Button("Seed Bite The Dust Gameplay Data")]
    [ShowIf("@displayName == \"Bite the Dust\" || fireBehaviorId == FireDamageBehaviorId.BiteTheDust")]
    private void SeedBiteTheDustGameplayData()
    {
        EnsureGameplayCollections();
        gameplay.useNewGameplayPipeline = true;
        gameplay.descriptionTemplate = "{Consume} all {Burn}. Deal {base2} damage. Heal {base3} HP.";

        gameplay.requirements.Clear();
        gameplay.baseEffects.Clear();
        gameplay.conditionalOutcomes.Clear();

        gameplay.requirements.Add(BuildTargetHasBurnRequirement());

        gameplay.baseEffects.Add(new SkillEffectData
        {
            type = SkillEffectType.ConsumeStatus,
            target = SkillEffectTarget.SelectedEnemy,
            status = StatusKind.Burn,
            previewable = true
        });

        gameplay.baseEffects.Add(new SkillEffectData
        {
            type = SkillEffectType.DealDamage,
            target = SkillEffectTarget.SelectedEnemy,
            value = new SkillValueData
            {
                baseAmount = 1,
                mode = SkillValueMode.ConsumedStatusStacksScaled,
                status = StatusKind.Burn
            },
            previewable = true
        });

        gameplay.baseEffects.Add(new SkillEffectData
        {
            type = SkillEffectType.Heal,
            target = SkillEffectTarget.Self,
            value = new SkillValueData
            {
                baseAmount = 3,
                mode = SkillValueMode.ConsumedStatusStacksDividedScaled,
                status = StatusKind.Burn,
                divisor = 5
            },
            previewable = true
        });
    }

    private void EnsureGameplayCollections()
    {
        if (gameplay == null)
            gameplay = new SkillGameplayData();
        if (gameplay.requirements == null) gameplay.requirements = new List<SkillRequirementData>();
        if (gameplay.baseEffects == null) gameplay.baseEffects = new List<SkillEffectData>();
        if (gameplay.conditionalOutcomes == null) gameplay.conditionalOutcomes = new List<SkillConditionalOutcomeDataV2>();
    }

    private static SkillConditionData BuildParityCondition(SkillConditionComparison comparison)
    {
        return new SkillConditionData
        {
            scope = SkillConditionScope.SlotBound,
            logic = SkillConditionLogic.All,
            clauses = new List<SkillConditionClause>
            {
                new SkillConditionClause
                {
                    reference = SkillConditionReference.AnyBaseValue,
                    comparison = comparison,
                    value = 0
                }
            }
        };
    }

    private static SkillRequirementData BuildTargetHasBurnRequirement()
    {
        return new SkillRequirementData
        {
            type = SkillRequirementType.Condition,
            failureText = "Target needs Burn.",
            condition = new SkillConditionData
            {
                scope = SkillConditionScope.SlotBound,
                logic = SkillConditionLogic.All,
                clauses = new List<SkillConditionClause>
                {
                    new SkillConditionClause
                    {
                        reference = SkillConditionReference.TargetHasBurn,
                        comparison = SkillConditionComparison.IsTrue,
                        value = 0
                    }
                }
            }
        };
    }
    private string BuildGameplaySummary()
    {
        if (gameplay == null)
            return "No gameplay data.";

        var lines = new List<string>();
        lines.Add(gameplay.useNewGameplayPipeline ? "Pipeline: New Gameplay" : "Pipeline: Legacy fallback");

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
                SkillEffectData effect = gameplay.baseEffects[i];
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
                SkillConditionalOutcomeDataV2 branch = gameplay.conditionalOutcomes[i];
                int effectCount = branch != null && branch.effects != null ? branch.effects.Count : 0;
                lines.Add($"- Branch {i + 1}: {effectCount} effect(s)");
            }
        }

        return string.Join("\n", lines);
    }
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

    private void OnEnable()
    {
        SeedFireSlashGameplayDataIfEmpty();
        SeedConsumeBurnGameplayDataIfEmpty();
        SeedMigratedFireGameplayDataIfEmpty();
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = name;

        SeedFireSlashGameplayDataIfEmpty();
        SeedConsumeBurnGameplayDataIfEmpty();
        SeedMigratedFireGameplayDataIfEmpty();

        if (!exactConditionMigrated)
        {
            MigrateLegacyExactCondition();
            exactConditionMigrated = true;
        }

        if (exactValueX <= 0 && conditionPresetValue > 0)
            exactValueX = conditionPresetValue;

        if (string.IsNullOrWhiteSpace(exactValuePattern))
            exactValuePattern = "1-2-3";

        // CoreAction -> safety nh?
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

        // Guard lu�n self
        if (kind == SkillKind.Guard)
            target = SkillTargetRule.Self;

        // Derived AoE flags (no combo targeting)
        hitAllEnemies = (target == SkillTargetRule.RowEnemies || target == SkillTargetRule.AllEnemies);
        hitAllAllies = (target == SkillTargetRule.RowAllies || target == SkillTargetRule.AllAllies);

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
                    case DiceParityConditionPreset.Even: return "C?m dice d?u ch?n";
                    case DiceParityConditionPreset.Odd: return "C?m dice d?u l?";
                }
                break;
            case SkillConditionFamily.CritFail:
                switch (critFailConditionPreset)
                {
                    case CritFailConditionPreset.Crit: return "C?m dice d?u l� Crit";
                    case CritFailConditionPreset.Fail: return "C?m dice d?u l� Fail";
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
                    case ResourceConditionPreset.CurrentFocusGreaterOrEqualN: return $"Focus hi?n t?i >= {conditionPresetValue}";
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
                    case TargetStateConditionPreset.TargetHasBurn: return "Target dang c� Burn";
                    case TargetStateConditionPreset.TargetHasFreeze: return "Target dang Freeze";
                    case TargetStateConditionPreset.TargetHasChilled: return "Target dang Chilled";
                    case TargetStateConditionPreset.TargetHasMark: return "Target dang c� Mark";
                    case TargetStateConditionPreset.TargetHasBleed: return "Target dang c� Bleed";
                    case TargetStateConditionPreset.TargetHasStagger: return "Target dang Stagger";
                    case TargetStateConditionPreset.StatusHistoryTodo: return "Status History (TODO - chua c� runtime logic)";
                }
                break;
            case SkillConditionFamily.BoardState:
                switch (boardStateConditionPreset)
                {
                    case BoardStateConditionPreset.AliveEnemiesGreaterOrEqualN: return $"S? enemy c�n s?ng >= {conditionPresetValue}";
                    case BoardStateConditionPreset.EnemiesWithStatusGreaterOrEqualN: return $"S? enemy dang c� status >= {conditionPresetValue}";
                }
                break;
        }

        return "Chua c� condition chu?n";
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




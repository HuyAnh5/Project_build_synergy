using UnityEngine;
using UnityEngine.Serialization;
using Sirenix.OdinInspector;

public partial class SkillDamageSO
{
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

    public SkillDamageConditionalOverrides conditionalOverrides => whenConditionIsMet;
}

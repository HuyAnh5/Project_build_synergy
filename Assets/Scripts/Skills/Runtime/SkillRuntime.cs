using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resolved, runtime version of a skill for the current turn/placement.
/// Starts from the current active skill asset, then applies conditional overrides if the condition is met.
/// </summary>
[Serializable]
public class SkillRuntime
{
    public ScriptableObject sourceAsset;

    // New targeting (SkillTargetRule). Legacy flow still uses TargetRule + hitAll flags.
    public bool useV2Targeting;
    public SkillTargetRule targetRuleV2;

    // Optional tag for built-in actions (BasicStrike/BasicGuard) when coming from SkillDamageSO
    public CoreAction coreAction;

    // Identity
    public SkillKind kind;
    public TargetRule target;
    public DamageGroup group;
    public ElementType element;
    public RangeType range;

    public bool hitAllEnemies;
    public bool hitAllAllies;

    // Slots
    public int slotsRequired;

    // Cost
    public int focusCost;
    public int focusGainOnCast;

    // Attack
    public float dieMultiplier;
    public int flatDamage;
    public BaseEffectValueMode baseDamageValueMode;

    // Sunder bonus
    public bool sunderBonusIfTargetHasGuard;
    public float sunderGuardDamageMultiplier;

    // Guard
    public BaseEffectValueMode guardValueMode;
    public int guardFlat;

    // Special combat
    public bool bypassGuard;
    public bool clearsGuard;
    public bool canUseMarkMultiplier;

    // Burn spender
    public bool consumesBurn;
    public int burnDamagePerStack;
    public bool fireUseXFormula;
    public bool fireApplyBurnFromResolvedValue;
    public bool fireGrantBonusBurnOnOddBase;
    public int fireOddBaseBonusBurn;
    public bool fireApplyBurnFromLowestBase;
    public bool fireGainGuardFromHighestBase;
    public bool fireReapplyBurnPerExactBase;
    public int fireExactBaseForReapply;
    public int fireBurnPerExactMatch;
    public bool fireRequireBurnBeforeHitForReapply;
    public bool fireApplyConsumeBonusDebuff;
    public int fireConsumeBonusPerBurn;
    public int fireConsumeBonusDebuffTurns;

    // Apply status (can be used by any skill if enabled)
    public bool applyBurn;
    public int burnAddStacks;
    public int burnRefreshTurns;
    public BaseEffectValueMode baseBurnValueMode;

    public bool applyMark;

    public bool applyBleed;
    public int bleedTurns;

    public bool applyFreeze;
    public float freezeChance;

    // VFX
    public Projectile2D projectilePrefab;

    // Debug / UI
    public bool conditionMet;

    // Conditional outcome
    public bool conditionalOutcomeEnabled;
    public ConditionalOutcomeType conditionalOutcomeType;
    public ConditionalOutcomeValueMode conditionalOutcomeValueMode;
    public int conditionalOutcomeFlatValue;
    public int conditionalOutcomeBurnTurns;

    // Split-role
    public bool splitRoleEnabled;
    public SplitRoleBranchOutcome splitRoleLowestOutcome;
    public SplitRoleBranchOutcome splitRoleHighestOutcome;
    public int splitRoleBurnTurns;

    // Snapshot of base dice values for the skill's local group this turn.
    public List<int> localBaseValues;
    public List<int> localResolvedValues;
    public List<bool> localNumericFlags;
    public List<bool> localCritFlags;
    public List<bool> localFailFlags;
    public List<bool> localFailPenaltyFlags;
    public bool localCritAny;
    public bool localFailAny;
    public bool localFailPenaltyAny;

    public static SkillRuntime FromDamage(SkillDamageSO s)
    {
        if (s == null) return null;

        var rt = new SkillRuntime
        {
            sourceAsset = s,

            useV2Targeting = true,
            targetRuleV2 = s.target,
            coreAction = s.coreAction,

            kind = s.kind,
            // Legacy target is kept for old code paths. Executor will prefer targetRuleV2 when useV2Targeting=true.
            target = SkillTargetRuleUtility.IsEnemySideTarget(s.target)
                ? TargetRule.Enemy
                : TargetRule.Self,

            group = s.group,
            element = (ElementType)(int)s.element,
            range = s.range,

            hitAllEnemies = (s.target == SkillTargetRule.RowEnemies || s.target == SkillTargetRule.AllEnemies),
            hitAllAllies = (s.target == SkillTargetRule.RowAllies || s.target == SkillTargetRule.AllAllies),

            slotsRequired = Mathf.Clamp(s.slotsRequired, 1, 3),

            focusCost = Mathf.Max(0, s.focusCost),
            focusGainOnCast = s.focusGainOnCast,

            dieMultiplier = s.dieMultiplier,
            flatDamage = s.flatDamage,
            baseDamageValueMode = s.baseDamageValueMode,

            sunderBonusIfTargetHasGuard = s.sunderBonusIfTargetHasGuard,
            sunderGuardDamageMultiplier = s.sunderGuardDamageMultiplier,

            guardValueMode = s.guardValueMode,
            guardFlat = s.guardFlat,

            bypassGuard = s.bypassGuard,
            clearsGuard = s.clearsGuard,
            canUseMarkMultiplier = s.canUseMarkMultiplier,

            consumesBurn = s.consumesBurn,
            burnDamagePerStack = s.burnDamagePerStack,
            fireUseXFormula = s.fireModules != null && s.fireModules.useXFormula,
            fireApplyBurnFromResolvedValue = s.fireModules != null && s.fireModules.applyBurnFromResolvedValue,
            fireGrantBonusBurnOnOddBase = s.fireModules != null && s.fireModules.grantBonusBurnOnOddBase,
            fireOddBaseBonusBurn = s.fireModules != null ? s.fireModules.oddBaseBonusBurn : 0,
            fireApplyBurnFromLowestBase = s.fireModules != null && s.fireModules.applyBurnFromLowestBase,
            fireGainGuardFromHighestBase = s.fireModules != null && s.fireModules.gainGuardFromHighestBase,
            fireReapplyBurnPerExactBase = s.fireModules != null && s.fireModules.reapplyBurnPerExactBase,
            fireExactBaseForReapply = s.fireModules != null ? s.fireModules.exactBaseForReapply : 0,
            fireBurnPerExactMatch = s.fireModules != null ? s.fireModules.burnPerExactMatch : 0,
            fireRequireBurnBeforeHitForReapply = s.fireModules == null || s.fireModules.requireBurnBeforeHitForReapply,
            fireApplyConsumeBonusDebuff = s.fireModules != null && s.fireModules.applyConsumeBonusDebuff,
            fireConsumeBonusPerBurn = s.fireModules != null ? s.fireModules.consumeBonusPerBurn : 0,
            fireConsumeBonusDebuffTurns = s.fireModules != null ? s.fireModules.consumeBonusDebuffTurns : 0,

            applyBurn = s.applyBurn,
            burnAddStacks = s.burnAddStacks,
            burnRefreshTurns = s.burnRefreshTurns,
            baseBurnValueMode = s.baseBurnValueMode,

            applyMark = s.applyMark,

            applyBleed = s.applyBleed,
            bleedTurns = s.bleedTurns,

            applyFreeze = s.applyFreeze,
            freezeChance = s.freezeChance,

            projectilePrefab = s.projectilePrefab,

            conditionMet = false,
            conditionalOutcomeEnabled = s.conditionalOutcome != null && s.conditionalOutcome.enabled,
            conditionalOutcomeType = s.conditionalOutcome != null ? s.conditionalOutcome.type : ConditionalOutcomeType.None,
            conditionalOutcomeValueMode = s.conditionalOutcome != null ? s.conditionalOutcome.valueMode : ConditionalOutcomeValueMode.Flat,
            conditionalOutcomeFlatValue = s.conditionalOutcome != null ? s.conditionalOutcome.flatValue : 0,
            conditionalOutcomeBurnTurns = s.conditionalOutcome != null ? s.conditionalOutcome.burnTurns : 3,
            splitRoleEnabled = s.splitRole != null && s.splitRole.enabled,
            splitRoleLowestOutcome = s.splitRole != null ? s.splitRole.lowestOutcome : SplitRoleBranchOutcome.None,
            splitRoleHighestOutcome = s.splitRole != null ? s.splitRole.highestOutcome : SplitRoleBranchOutcome.None,
            splitRoleBurnTurns = s.splitRole != null ? s.splitRole.burnTurns : 3,
            localBaseValues = null,
            localResolvedValues = null,
            localNumericFlags = null,
            localCritFlags = null,
            localFailFlags = null,
            localFailPenaltyFlags = null,
            localCritAny = false,
            localFailAny = false,
            localFailPenaltyAny = false
        };

        // safety
        if (rt.kind == SkillKind.Guard || rt.coreAction == CoreAction.BasicGuard)
        {
            rt.kind = SkillKind.Guard;
            rt.targetRuleV2 = SkillTargetRule.Self;
            rt.target = TargetRule.Self;
            rt.hitAllEnemies = false;
            rt.hitAllAllies = false;
        }

        return rt;
    }

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
}

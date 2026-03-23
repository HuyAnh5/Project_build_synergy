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

    // Sunder bonus
    public bool sunderBonusIfTargetHasGuard;
    public float sunderGuardDamageMultiplier;

    // Guard
    public float guardDieMultiplier;
    public int guardFlat;

    // Special combat
    public bool bypassGuard;
    public bool clearsGuard;
    public bool canUseMarkMultiplier;

    // Burn spender
    public bool consumesBurn;
    public int burnDamagePerStack;

    // Apply status (can be used by any skill if enabled)
    public bool applyBurn;
    public int burnAddStacks;
    public int burnRefreshTurns;

    public bool applyMark;

    public bool applyBleed;
    public int bleedTurns;

    public bool applyFreeze;
    public float freezeChance;

    // VFX
    public Projectile2D projectilePrefab;

    // Debug / UI
    public bool conditionMet;

    // Snapshot of base dice values for the skill's local group this turn.
    public List<int> localBaseValues;
    public List<int> localResolvedValues;
    public bool localCritAny;
    public bool localFailAny;

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
            target = (s.target == SkillTargetRule.SingleEnemy || s.target == SkillTargetRule.AllEnemies || s.target == SkillTargetRule.AllUnits)
                ? TargetRule.Enemy
                : TargetRule.Self,

            group = s.group,
            element = (ElementType)(int)s.element,
            range = s.range,

            hitAllEnemies = (s.target == SkillTargetRule.AllEnemies || s.target == SkillTargetRule.AllUnits),
            hitAllAllies = (s.target == SkillTargetRule.AllAllies || s.target == SkillTargetRule.AllUnits),

            slotsRequired = Mathf.Clamp(s.slotsRequired, 1, 3),

            focusCost = Mathf.Max(0, s.focusCost),
            focusGainOnCast = s.focusGainOnCast,

            dieMultiplier = s.dieMultiplier,
            flatDamage = s.flatDamage,

            sunderBonusIfTargetHasGuard = s.sunderBonusIfTargetHasGuard,
            sunderGuardDamageMultiplier = s.sunderGuardDamageMultiplier,

            guardDieMultiplier = s.guardDieMultiplier,
            guardFlat = s.guardFlat,

            bypassGuard = s.bypassGuard,
            clearsGuard = s.clearsGuard,
            canUseMarkMultiplier = s.canUseMarkMultiplier,

            consumesBurn = s.consumesBurn,
            burnDamagePerStack = s.burnDamagePerStack,

            applyBurn = s.applyBurn,
            burnAddStacks = s.burnAddStacks,
            burnRefreshTurns = s.burnRefreshTurns,

            applyMark = s.applyMark,

            applyBleed = s.applyBleed,
            bleedTurns = s.bleedTurns,

            applyFreeze = s.applyFreeze,
            freezeChance = s.freezeChance,

            projectilePrefab = s.projectilePrefab,

            conditionMet = false,
            localBaseValues = null,
            localResolvedValues = null,
            localCritAny = false,
            localFailAny = false
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
        int g = Mathf.FloorToInt(dieValue * guardDieMultiplier) + guardFlat;
        return Mathf.Max(0, g);
    }
}

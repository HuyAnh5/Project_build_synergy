using System;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// "Mini SkillSO" applied ONLY when the SkillCondition is met.
/// Fields are guarded by "overrideX" toggles to keep the inspector readable.
/// </summary>
[Serializable]
public class SkillConditionalOverrides
{
    // ------------------- Slots -------------------

    [BoxGroup("Slots")]
    [ToggleLeft]
    [LabelText("Override Slots Required")]
    public bool overrideSlotsRequired;

    [BoxGroup("Slots")]
    [ShowIf(nameof(overrideSlotsRequired))]
    [MinValue(1), MaxValue(3)]
    [LabelText("Slots Required")]
    public int slotsRequired = 1;

    // ------------------- Identity -------------------

    [BoxGroup("Identity")]
    [ToggleLeft]
    [LabelText("Override Identity")]
    public bool overrideIdentity;

    [BoxGroup("Identity")]
    [ShowIf(nameof(overrideIdentity))]
    [EnumToggleButtons]
    public SkillKind kind = SkillKind.Attack;

    [BoxGroup("Identity")]
    [ShowIf(nameof(overrideIdentity))]
    [EnumToggleButtons]
    public TargetRule target = TargetRule.Enemy;

    [BoxGroup("Identity")]
    [ShowIf("@overrideIdentity && kind == SkillKind.Attack")]
    [EnumToggleButtons]
    public DamageGroup group = DamageGroup.Strike;

    [BoxGroup("Identity")]
    [ShowIf("@overrideIdentity && kind == SkillKind.Attack")]
    [EnumToggleButtons]
    public ElementType element = ElementType.Physical;

    [BoxGroup("Identity")]
    [ShowIf("@overrideIdentity && kind == SkillKind.Attack")]
    [EnumToggleButtons]
    public RangeType range = RangeType.Ranged;

    [BoxGroup("Identity")]
    [ShowIf("@overrideIdentity && kind == SkillKind.Attack && target == TargetRule.Enemy")]
    [ToggleLeft]
    [LabelText("Hit All Enemies")]
    public bool hitAllEnemies = false;

    [BoxGroup("Identity")]
    [ShowIf("@overrideIdentity && kind == SkillKind.Attack && target == TargetRule.Self")]
    [ToggleLeft]
    [LabelText("Hit All Allies")]
    public bool hitAllAllies = false;

    // ------------------- Cost -------------------

    [BoxGroup("Cost")]
    [ToggleLeft]
    [LabelText("Override Cost")]
    public bool overrideCost;

    [BoxGroup("Cost")]
    [ShowIf(nameof(overrideCost))]
    [Min(0)]
    [LabelText("Focus Cost")]
    public int focusCost = 0;

    [BoxGroup("Cost")]
    [ShowIf(nameof(overrideCost))]
    [LabelText("Focus Gain On Cast")]
    public int focusGainOnCast = 0;

    // ------------------- Damage -------------------

    [BoxGroup("Damage")]
    [ToggleLeft]
    [LabelText("Override Damage")]
    public bool overrideDamage;

    [BoxGroup("Damage")]
    [ShowIf(nameof(overrideDamage))]
    [Range(0f, 2f)]
    [LabelText("Die Multiplier")]
    public float dieMultiplier = 1f;

    [BoxGroup("Damage")]
    [ShowIf(nameof(overrideDamage))]
    [LabelText("Flat Damage")]
    public int flatDamage = 0;

    // ------------------- Sunder Bonus -------------------

    [FoldoutGroup("Sunder Bonus", expanded: false)]
    [ToggleLeft]
    [LabelText("Override Sunder Bonus")]
    public bool overrideSunderBonus;

    [FoldoutGroup("Sunder Bonus")]
    [ShowIf(nameof(overrideSunderBonus))]
    [LabelText("Bonus If Target Has Guard")]
    public bool sunderBonusIfTargetHasGuard = true;

    [FoldoutGroup("Sunder Bonus")]
    [ShowIf("@overrideSunderBonus && sunderBonusIfTargetHasGuard")]
    [Min(0f)]
    [LabelText("Guard Damage Multiplier")]
    public float sunderGuardDamageMultiplier = 2f;

    // ------------------- Guard -------------------

    [BoxGroup("Guard")]
    [ToggleLeft]
    [LabelText("Override Guard")]
    public bool overrideGuard;

    [BoxGroup("Guard")]
    [ShowIf(nameof(overrideGuard))]
    [Range(0f, 2f)]
    [LabelText("Guard Die Multiplier")]
    public float guardDieMultiplier = 1f;

    [BoxGroup("Guard")]
    [ShowIf(nameof(overrideGuard))]
    [LabelText("Guard Flat")]
    public int guardFlat = 0;

    // ------------------- Special Combat -------------------

    [BoxGroup("Special Combat")]
    [ToggleLeft]
    [LabelText("Override Special Combat")]
    public bool overrideSpecialCombat;

    [BoxGroup("Special Combat")]
    [ShowIf(nameof(overrideSpecialCombat))]
    [LabelText("Bypass Guard")]
    public bool bypassGuard = false;

    [BoxGroup("Special Combat")]
    [ShowIf(nameof(overrideSpecialCombat))]
    [LabelText("Clears Guard")]
    public bool clearsGuard = false;

    [BoxGroup("Special Combat")]
    [ShowIf(nameof(overrideSpecialCombat))]
    [LabelText("Can Use Mark Multiplier")]
    public bool canUseMarkMultiplier = true;

    // ------------------- Burn Spender -------------------

    [BoxGroup("Burn Spender (Fire)")]
    [ToggleLeft]
    [LabelText("Override Burn Spender")]
    public bool overrideBurnSpender;

    [BoxGroup("Burn Spender (Fire)")]
    [ShowIf(nameof(overrideBurnSpender))]
    [LabelText("Consumes Burn")]
    public bool consumesBurn = false;

    [BoxGroup("Burn Spender (Fire)")]
    [ShowIf("@overrideBurnSpender && consumesBurn")]
    [Min(0)]
    [LabelText("Burn Damage Per Stack")]
    public int burnDamagePerStack = 1;

    // ------------------- Apply Status -------------------

    [BoxGroup("Apply Status")]
    [ToggleLeft]
    [LabelText("Override Apply Status")]
    public bool overrideApplyStatus;

    [BoxGroup("Apply Status")]
    [ShowIf(nameof(overrideApplyStatus))]
    [ToggleLeft]
    [LabelText("Apply Burn")]
    public bool applyBurn;

    [BoxGroup("Apply Status")]
    [ShowIf("@overrideApplyStatus && applyBurn")]
    [Min(0)]
    [LabelText("Burn Add Stacks")]
    public int burnAddStacks = 2;

    [BoxGroup("Apply Status")]
    [ShowIf("@overrideApplyStatus && applyBurn")]
    [Min(0)]
    [LabelText("Burn Refresh Turns")]
    public int burnRefreshTurns = 3;

    [BoxGroup("Apply Status")]
    [ShowIf(nameof(overrideApplyStatus))]
    [ToggleLeft]
    [LabelText("Apply Mark")]
    public bool applyMark;

    [BoxGroup("Apply Status")]
    [ShowIf(nameof(overrideApplyStatus))]
    [ToggleLeft]
    [LabelText("Apply Bleed")]
    public bool applyBleed;

    [BoxGroup("Apply Status")]
    [ShowIf("@overrideApplyStatus && applyBleed")]
    [Min(0)]
    [LabelText("Bleed Turns")]
    public int bleedTurns = 3;

    [BoxGroup("Apply Status")]
    [ShowIf(nameof(overrideApplyStatus))]
    [ToggleLeft]
    [LabelText("Apply Freeze")]
    public bool applyFreeze;

    [BoxGroup("Apply Status")]
    [ShowIf("@overrideApplyStatus && applyFreeze")]
    [Range(0f, 1f)]
    [LabelText("Freeze Chance")]
    public float freezeChance = 0.4f;

    // ------------------- VFX -------------------

    [BoxGroup("VFX")]
    [ToggleLeft]
    [LabelText("Override VFX")]
    public bool overrideVfx;

    [BoxGroup("VFX")]
    [ShowIf(nameof(overrideVfx))]
    [LabelText("Projectile Prefab")]
    public Projectile2D projectilePrefab;

    // ------------------- Apply to runtime -------------------

    public void ApplyTo(ref SkillRuntime rt)
    {
        if (overrideSlotsRequired)
            rt.slotsRequired = Mathf.Clamp(slotsRequired, 1, 3);

        if (overrideIdentity)
        {
            rt.kind = kind;
            rt.target = target;
            rt.group = group;
            rt.element = element;
            rt.range = range;

            rt.hitAllEnemies = hitAllEnemies && (target == TargetRule.Enemy);
            rt.hitAllAllies = hitAllAllies && (target == TargetRule.Self);
        }

        if (overrideCost)
        {
            rt.focusCost = Mathf.Max(0, focusCost);
            rt.focusGainOnCast = focusGainOnCast;
        }

        if (overrideDamage)
        {
            rt.dieMultiplier = dieMultiplier;
            rt.flatDamage = flatDamage;
        }

        if (overrideSunderBonus)
        {
            rt.sunderBonusIfTargetHasGuard = sunderBonusIfTargetHasGuard;
            rt.sunderGuardDamageMultiplier = sunderGuardDamageMultiplier;
        }

        if (overrideGuard)
        {
            rt.guardDieMultiplier = guardDieMultiplier;
            rt.guardFlat = guardFlat;
        }

        if (overrideSpecialCombat)
        {
            rt.bypassGuard = bypassGuard;
            rt.clearsGuard = clearsGuard;
            rt.canUseMarkMultiplier = canUseMarkMultiplier;
        }

        if (overrideBurnSpender)
        {
            rt.consumesBurn = consumesBurn;
            rt.burnDamagePerStack = burnDamagePerStack;
        }

        if (overrideApplyStatus)
        {
            rt.applyBurn = applyBurn;
            rt.burnAddStacks = burnAddStacks;
            rt.burnRefreshTurns = burnRefreshTurns;

            rt.applyMark = applyMark;

            rt.applyBleed = applyBleed;
            rt.bleedTurns = bleedTurns;

            rt.applyFreeze = applyFreeze;
            rt.freezeChance = freezeChance;
        }

        if (overrideVfx)
            rt.projectilePrefab = projectilePrefab;

        // safety
        if (rt.target == TargetRule.Self) rt.hitAllEnemies = false;
        if (rt.target == TargetRule.Enemy) rt.hitAllAllies = false;
        if (rt.kind == SkillKind.Guard)
        {
            rt.target = TargetRule.Self;
            rt.hitAllEnemies = false;
            rt.hitAllAllies = false;
        }
    }
}

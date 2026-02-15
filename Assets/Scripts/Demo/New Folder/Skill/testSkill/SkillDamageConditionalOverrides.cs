// SkillDamageConditionalOverrides.cs
using System;
using UnityEngine;
using Sirenix.OdinInspector;

[Serializable]
public class SkillDamageConditionalOverrides
{
    // ------------------- Slots -------------------

    [BoxGroup("Slots")]
    [ToggleLeft, LabelText("Override Slots Required")]
    public bool overrideSlotsRequired;

    [BoxGroup("Slots")]
    [ShowIf(nameof(overrideSlotsRequired))]
    [MinValue(1), MaxValue(3)]
    [LabelText("Slots Required")]
    public int slotsRequired = 1;

    // ------------------- Identity -------------------

    [BoxGroup("Identity")]
    [ToggleLeft, LabelText("Override Identity")]
    public bool overrideIdentity;

    [BoxGroup("Identity")]
    [ShowIf(nameof(overrideIdentity))]
    [EnumToggleButtons]
    public SkillKind kind = SkillKind.Attack;

    [BoxGroup("Identity")]
    [ShowIf(nameof(overrideIdentity))]
    [EnumToggleButtons]
    public SkillTargetRule target = SkillTargetRule.SingleEnemy;

    [BoxGroup("Identity")]
    [ShowIf("@overrideIdentity && kind == SkillKind.Attack")]
    [EnumToggleButtons]
    public DamageGroup group = DamageGroup.Strike;

    [BoxGroup("Identity")]
    [ShowIf("@overrideIdentity && kind == SkillKind.Attack")]
    [EnumToggleButtons]
    public ElementTag element = ElementTag.Physical;

    [BoxGroup("Identity")]
    [ShowIf("@overrideIdentity && kind == SkillKind.Attack")]
    [EnumToggleButtons]
    public RangeType range = RangeType.Ranged;

    // Derived (kept for compatibility)
    [BoxGroup("Identity")]
    [ShowIf(nameof(overrideIdentity))]
    [ReadOnly, LabelText("Hit All Enemies (Derived)")]
    public bool hitAllEnemies;

    [BoxGroup("Identity")]
    [ShowIf(nameof(overrideIdentity))]
    [ReadOnly, LabelText("Hit All Allies (Derived)")]
    public bool hitAllAllies;

    // ------------------- Cost -------------------

    [BoxGroup("Cost")]
    [ToggleLeft, LabelText("Override Cost")]
    public bool overrideCost;

    [BoxGroup("Cost")]
    [ShowIf(nameof(overrideCost))]
    [Min(0)]
    public int focusCost = 0;

    [BoxGroup("Cost")]
    [ShowIf(nameof(overrideCost))]
    public int focusGainOnCast = 0;

    // ------------------- Damage -------------------

    [BoxGroup("Damage")]
    [ToggleLeft, LabelText("Override Damage")]
    public bool overrideDamage;

    [BoxGroup("Damage")]
    [ShowIf(nameof(overrideDamage))]
    [Range(0f, 2f)]
    public float dieMultiplier = 1f;

    [BoxGroup("Damage")]
    [ShowIf(nameof(overrideDamage))]
    public int flatDamage = 0;

    // ------------------- Sunder Bonus -------------------

    [FoldoutGroup("Sunder Bonus", expanded: false)]
    [ToggleLeft, LabelText("Override Sunder Bonus")]
    public bool overrideSunderBonus;

    [FoldoutGroup("Sunder Bonus")]
    [ShowIf(nameof(overrideSunderBonus))]
    public bool sunderBonusIfTargetHasGuard = true;

    [FoldoutGroup("Sunder Bonus")]
    [ShowIf("@overrideSunderBonus && sunderBonusIfTargetHasGuard")]
    [Min(0f)]
    public float sunderGuardDamageMultiplier = 2f;

    // ------------------- Guard -------------------

    [BoxGroup("Guard")]
    [ToggleLeft, LabelText("Override Guard")]
    public bool overrideGuard;

    [BoxGroup("Guard")]
    [ShowIf(nameof(overrideGuard))]
    [Range(0f, 2f)]
    public float guardDieMultiplier = 1f;

    [BoxGroup("Guard")]
    [ShowIf(nameof(overrideGuard))]
    public int guardFlat = 0;

    // ------------------- Special Combat -------------------

    [BoxGroup("Special Combat")]
    [ToggleLeft, LabelText("Override Special Combat")]
    public bool overrideSpecialCombat;

    [BoxGroup("Special Combat")]
    [ShowIf(nameof(overrideSpecialCombat))]
    public bool bypassGuard = false;

    [BoxGroup("Special Combat")]
    [ShowIf(nameof(overrideSpecialCombat))]
    public bool clearsGuard = false;

    [BoxGroup("Special Combat")]
    [ShowIf(nameof(overrideSpecialCombat))]
    public bool canUseMarkMultiplier = true;

    // ------------------- Burn Spender -------------------

    [BoxGroup("Burn Spender (Fire)")]
    [ToggleLeft, LabelText("Override Burn Spender")]
    public bool overrideBurnSpender;

    [BoxGroup("Burn Spender (Fire)")]
    [ShowIf(nameof(overrideBurnSpender))]
    public bool consumesBurn = false;

    [BoxGroup("Burn Spender (Fire)")]
    [ShowIf("@overrideBurnSpender && consumesBurn")]
    [Min(0)]
    public int burnDamagePerStack = 1;

    // ------------------- Apply Status -------------------

    [BoxGroup("Apply Status")]
    [ToggleLeft, LabelText("Override Apply Status")]
    public bool overrideApplyStatus;

    [BoxGroup("Apply Status")]
    [ShowIf(nameof(overrideApplyStatus))]
    public bool applyBurn;

    [BoxGroup("Apply Status")]
    [ShowIf("@overrideApplyStatus && applyBurn")]
    [Min(0)]
    public int burnAddStacks = 2;

    [BoxGroup("Apply Status")]
    [ShowIf("@overrideApplyStatus && applyBurn")]
    [Min(0)]
    public int burnRefreshTurns = 3;

    [BoxGroup("Apply Status")]
    [ShowIf(nameof(overrideApplyStatus))]
    public bool applyMark;

    [BoxGroup("Apply Status")]
    [ShowIf(nameof(overrideApplyStatus))]
    public bool applyBleed;

    [BoxGroup("Apply Status")]
    [ShowIf("@overrideApplyStatus && applyBleed")]
    [Min(0)]
    public int bleedTurns = 3;

    [BoxGroup("Apply Status")]
    [ShowIf(nameof(overrideApplyStatus))]
    public bool applyFreeze;

    [BoxGroup("Apply Status")]
    [ShowIf("@overrideApplyStatus && applyFreeze")]
    [Range(0f, 1f)]
    public float freezeChance = 0.4f;

    // ------------------- VFX -------------------

    [BoxGroup("VFX")]
    [ToggleLeft, LabelText("Override VFX")]
    public bool overrideVfx;

    [BoxGroup("VFX")]
    [ShowIf(nameof(overrideVfx))]
    public Projectile2D projectilePrefab;

    // Called by Odin when values change in inspector (nice UX)
    [OnValueChanged(nameof(RecomputeDerived))]
    private void _OnChanged() { }

    private void RecomputeDerived()
    {
        hitAllEnemies = (target == SkillTargetRule.AllEnemies || target == SkillTargetRule.AllUnits);
        hitAllAllies = (target == SkillTargetRule.AllAllies || target == SkillTargetRule.AllUnits);

        if (kind == SkillKind.Guard)
        {
            target = SkillTargetRule.Self;
            hitAllEnemies = false;
            hitAllAllies = false;
        }
    }
}

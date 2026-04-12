using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable, InlineProperty]
public class FireAttackModuleData
{
    [BoxGroup("Fire Attack"), ToggleLeft]
    [LabelText("Use X Damage")]
    public bool useXFormula = false;

    [BoxGroup("Fire Attack"), ToggleLeft]
    [LabelText("Apply Burn From X")]
    public bool applyBurnFromResolvedValue = false;

    [BoxGroup("Fire Attack"), ToggleLeft]
    [LabelText("Odd Base -> Bonus Burn")]
    public bool grantBonusBurnOnOddBase = false;

    [BoxGroup("Fire Attack")]
    [ShowIf(nameof(grantBonusBurnOnOddBase))]
    [LabelText("Bonus Burn")]
    [MinValue(0)]
    public int oddBaseBonusBurn = 2;

    [BoxGroup("Fire Attack"), ToggleLeft]
    [LabelText("Lowest Base -> Burn")]
    public bool applyBurnFromLowestBase = false;

    [BoxGroup("Fire Attack"), ToggleLeft]
    [LabelText("Highest Base -> Guard")]
    public bool gainGuardFromHighestBase = false;

    [BoxGroup("Fire Attack"), ToggleLeft]
    [LabelText("Exact Reapply Burn")]
    public bool reapplyBurnPerExactBase = false;

    [BoxGroup("Fire Attack")]
    [ShowIf(nameof(reapplyBurnPerExactBase))]
    [LabelText("Exact Base")]
    public int exactBaseForReapply = 7;

    [BoxGroup("Fire Attack")]
    [ShowIf(nameof(reapplyBurnPerExactBase))]
    [LabelText("Burn Per Match")]
    [MinValue(0)]
    public int burnPerExactMatch = 7;

    [BoxGroup("Fire Attack")]
    [ShowIf(nameof(reapplyBurnPerExactBase))]
    [ToggleLeft]
    [LabelText("Need Burn Before Hit")]
    public bool requireBurnBeforeHitForReapply = true;

    [BoxGroup("Fire Attack"), ToggleLeft]
    [LabelText("Apply Burn Consume Bonus Debuff")]
    public bool applyConsumeBonusDebuff = false;

    [BoxGroup("Fire Attack")]
    [ShowIf(nameof(applyConsumeBonusDebuff))]
    [LabelText("Bonus Per Burn")]
    [MinValue(0)]
    public int consumeBonusPerBurn = 1;

    [BoxGroup("Fire Attack")]
    [ShowIf(nameof(applyConsumeBonusDebuff))]
    [LabelText("Debuff Turns")]
    [MinValue(1)]
    public int consumeBonusDebuffTurns = 2;
}

[Serializable, InlineProperty]
public class FireBuffModuleData
{
    [BoxGroup("Fire Buff"), ToggleLeft]
    [LabelText("Grant Ember Weapon")]
    public bool grantEmberWeapon = false;

    [BoxGroup("Fire Buff")]
    [ShowIf(nameof(grantEmberWeapon))]
    [LabelText("Turns")]
    [MinValue(1)]
    public int emberWeaponTurns = 3;

    [BoxGroup("Fire Buff")]
    [ShowIf(nameof(grantEmberWeapon))]
    [LabelText("Basic Attack Bonus Damage")]
    [MinValue(0)]
    public int basicAttackBonusDamage = 1;

    [BoxGroup("Fire Buff")]
    [ShowIf(nameof(grantEmberWeapon))]
    [ToggleLeft]
    [LabelText("Basic Attack Burn = Damage")]
    public bool basicAttackAppliesBurnEqualDamage = true;

    [BoxGroup("Fire Buff")]
    [ShowIf(nameof(grantEmberWeapon))]
    [ToggleLeft]
    [LabelText("Only Apply Burn On Crit")]
    public bool basicAttackBurnOnCritOnly = false;

    [BoxGroup("Fire Buff"), ToggleLeft]
    [LabelText("Apply Consume Bonus Debuff")]
    public bool applyConsumeBonusDebuff = false;

    [BoxGroup("Fire Buff")]
    [ShowIf(nameof(applyConsumeBonusDebuff))]
    [LabelText("Bonus Per Burn")]
    [MinValue(0)]
    public int consumeBonusPerBurn = 1;

    [BoxGroup("Fire Buff")]
    [ShowIf(nameof(applyConsumeBonusDebuff))]
    [LabelText("Turns")]
    [MinValue(1)]
    public int consumeBonusDebuffTurns = 2;
}

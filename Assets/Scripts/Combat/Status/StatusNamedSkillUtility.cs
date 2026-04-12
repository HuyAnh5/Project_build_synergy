using UnityEngine;

internal static class StatusNamedSkillUtility
{
    public static bool TryApplyNamedSkillNow(StatusController owner, SkillBuffDebuffSO skill, int rolledValue)
    {
        if (owner == null || skill == null)
            return false;

        if (skill.fireModules != null)
        {
            if (skill.fireModules.grantEmberWeapon)
            {
                owner.emberWeaponTurns = Mathf.Max(owner.emberWeaponTurns, Mathf.Max(1, skill.fireModules.emberWeaponTurns));
                owner.emberWeaponBonusDamage = Mathf.Max(owner.emberWeaponBonusDamage, Mathf.Max(0, skill.fireModules.basicAttackBonusDamage));
                owner.emberWeaponBurnEqualsDamage = owner.emberWeaponBurnEqualsDamage || skill.fireModules.basicAttackAppliesBurnEqualDamage;
                owner.emberWeaponBurnOnCritOnly = owner.emberWeaponBurnOnCritOnly || skill.fireModules.basicAttackBurnOnCritOnly;
                return true;
            }

            if (skill.fireModules.applyConsumeBonusDebuff)
            {
                owner.cinderbrandTurns = Mathf.Max(owner.cinderbrandTurns, Mathf.Max(1, skill.fireModules.consumeBonusDebuffTurns));
                owner.cinderbrandBonusPerBurn = Mathf.Max(owner.cinderbrandBonusPerBurn, Mathf.Max(0, skill.fireModules.consumeBonusPerBurn));
                return true;
            }
        }

        if (skill.behaviorId == BuffBehaviorId.Fire_EmberWeapon)
        {
            owner.emberWeaponTurns = Mathf.Max(owner.emberWeaponTurns, 3);
            owner.emberWeaponBonusDamage = Mathf.Max(owner.emberWeaponBonusDamage, 1);
            owner.emberWeaponBurnEqualsDamage = true;
            owner.emberWeaponBurnOnCritOnly = true;
            return true;
        }

        if (skill.behaviorId == BuffBehaviorId.Fire_Cinderbrand)
        {
            // Cinderbrand currently shares the same runtime hook as Ember Weapon:
            // Legacy fallback preserved for old assets until Fire assets are migrated.
            owner.cinderbrandTurns = Mathf.Max(owner.cinderbrandTurns, 3);
            owner.cinderbrandBonusPerBurn = Mathf.Max(owner.cinderbrandBonusPerBurn, 1);
            return true;
        }

        return false;
    }
}

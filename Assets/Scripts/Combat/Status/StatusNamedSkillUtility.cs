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
                ApplyEmberWeapon(
                    owner,
                    skill.fireModules.emberWeaponTurns,
                    skill.fireModules.basicAttackBonusDamage,
                    skill.fireModules.basicAttackAppliesBurnEqualDamage,
                    skill.fireModules.basicAttackBurnOnCritOnly);
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
            ApplyEmberWeapon(owner, turns: 3, bonusDamage: 1, burnEqualsDamage: true, burnOnCritOnly: false);
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

    private static void ApplyEmberWeapon(StatusController owner, int turns, int bonusDamage, bool burnEqualsDamage, bool burnOnCritOnly)
    {
        owner.emberWeaponTurns = Mathf.Max(owner.emberWeaponTurns, Mathf.Max(1, turns));
        owner.emberWeaponBonusDamage = Mathf.Max(0, bonusDamage);
        owner.emberWeaponBurnEqualsDamage = burnEqualsDamage;
        owner.emberWeaponBurnOnCritOnly = burnEqualsDamage && burnOnCritOnly;
    }
}

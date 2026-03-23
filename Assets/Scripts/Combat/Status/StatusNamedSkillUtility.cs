using UnityEngine;

internal static class StatusNamedSkillUtility
{
    public static bool TryApplyNamedSkillNow(StatusController owner, SkillBuffDebuffSO skill, int rolledValue)
    {
        if (owner == null || skill == null)
            return false;

        if (skill.behaviorId == BuffBehaviorId.Fire_EmberWeapon)
        {
            owner.emberWeaponTurns = Mathf.Max(owner.emberWeaponTurns, 3);
            return true;
        }

        if (skill.behaviorId == BuffBehaviorId.Fire_Cinderbrand)
        {
            // Cinderbrand currently shares the same runtime hook as Ember Weapon:
            // Basic Attack gets +1 damage for the next 3 turns and applies Burn
            // equal to the total damage dealt.
            owner.emberWeaponTurns = Mathf.Max(owner.emberWeaponTurns, 3);
            owner.cinderbrandTurns = 0;
            return true;
        }

        return false;
    }
}

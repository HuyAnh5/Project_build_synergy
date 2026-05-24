using UnityEngine;

/// <summary>
/// Centralizes active runtime state for skill UI and recast gates.
/// For now Ember Weapon still uses the existing dedicated runtime fields on StatusController.
/// Future named buff/debuff skills can be added here without coupling UI widgets to each skill's internals.
/// </summary>
public static class SkillActiveStateUtility
{
    public static bool IsSkillActiveOnPlayer(ScriptableObject skillAsset, CombatActor player, out int remainingTurns)
    {
        remainingTurns = 0;

        if (skillAsset == null || player == null || player.status == null)
            return false;

        if (IsEmberWeapon(skillAsset))
        {
            remainingTurns = Mathf.Max(0, player.status.emberWeaponTurns);
            return remainingTurns > 0;
        }

        return false;
    }

    public static bool BlocksRecastWhileActive(ScriptableObject skillAsset)
    {
        return IsEmberWeapon(skillAsset);
    }

    private static bool IsEmberWeapon(ScriptableObject skillAsset)
    {
        return skillAsset is SkillBuffDebuffSO buff &&
               (buff.behaviorId == BuffBehaviorId.Fire_EmberWeapon ||
                buff.fireModules.grantEmberWeapon);
    }
}

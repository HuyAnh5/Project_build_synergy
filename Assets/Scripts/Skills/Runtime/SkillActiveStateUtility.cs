using UnityEngine;

/// <summary>
/// Centralizes active runtime state for skill UI and recast gates.
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
        SkillBuffDebuffSO buff = skillAsset as SkillBuffDebuffSO;
        if (buff == null || buff.gameplay == null)
            return false;

        if (ContainsEmberWeapon(buff.gameplay.baseEffects))
            return true;

        if (buff.gameplay.conditionalOutcomes == null)
            return false;

        for (int i = 0; i < buff.gameplay.conditionalOutcomes.Count; i++)
        {
            BuffDebuffFlowConditionalOutcomeData branch = buff.gameplay.conditionalOutcomes[i];
            if (branch != null && ContainsEmberWeapon(branch.effects))
                return true;
        }

        return false;
    }

    private static bool ContainsEmberWeapon(System.Collections.Generic.List<BuffDebuffFlowEffectData> effects)
    {
        if (effects == null)
            return false;

        for (int i = 0; i < effects.Count; i++)
        {
            BuffDebuffFlowEffectData effect = effects[i];
            if (effect != null && effect.type == BuffDebuffFlowEffectType.EmberWeapon)
                return true;
        }

        return false;
    }
}

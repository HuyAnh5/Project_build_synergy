using System.Collections.Generic;
using UnityEngine;

public static class TurnManagerCombatUtility
{
    public static IReadOnlyList<CombatActor> ResolveAoeTargets(SkillRuntime rt, BattlePartyManager2D party, CombatActor enemy)
    {
        if (rt == null) return null;
        if (!rt.hitAllEnemies && !rt.hitAllAllies) return null;

        if (rt.hitAllEnemies)
            return ResolveAliveEnemiesSnapshot(party, enemy);

        return null;
    }

    public static List<CombatActor> ResolveAliveEnemiesSnapshot(BattlePartyManager2D party, CombatActor enemy)
    {
        if (party != null)
            return party.GetAliveEnemies(frontOnly: false);

        var list = new List<CombatActor>(1);
        if (enemy != null && !enemy.IsDead) list.Add(enemy);
        return list;
    }

    public static void ClearAllStagger()
    {
        var actors = Object.FindObjectsOfType<CombatActor>(true);
        for (int i = 0; i < actors.Length; i++)
        {
            var actor = actors[i];
            if (actor == null || actor.status == null) continue;
            actor.status.ClearStagger();
        }
    }

    public static ElementType GetResolvedDiceElement(SkillRuntime rt, ScriptableObject asset)
    {
        if (rt != null && rt.kind == SkillKind.Attack)
            return rt.element;

        if (asset is SkillDamageSO dmg && dmg.kind == SkillKind.Attack)
            return (ElementType)(int)dmg.element;

        return ElementType.Neutral;
    }

    public static int ComputeResolvedDieSum(DiceSlotRig diceRig, CombatActor player, int start0, int span, ElementType skillElement)
    {
        if (diceRig == null || player == null) return 0;
        span = Mathf.Clamp(span, 1, 3);
        start0 = Mathf.Clamp(start0, 0, 2);

        int sum = 0;
        for (int i = start0; i < start0 + span; i++)
            sum += diceRig.GetResolvedDieValue(i, player, skillElement);

        return sum;
    }

    public static int ComputeMaxFace(DiceSlotRig diceRig, int start0, int span)
    {
        if (diceRig == null) return 6;
        span = Mathf.Clamp(span, 1, 3);
        start0 = Mathf.Clamp(start0, 0, 2);

        int max = 1;
        for (int i = start0; i < start0 + span; i++)
            max = Mathf.Max(max, diceRig.GetMaxFaceValue(i));

        return Mathf.Max(1, max);
    }
}

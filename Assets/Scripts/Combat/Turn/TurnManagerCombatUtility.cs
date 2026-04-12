using System.Collections.Generic;
using UnityEngine;

public static class TurnManagerCombatUtility
{
    public static IReadOnlyList<CombatActor> ResolveAoeTargets(SkillRuntime rt, CombatActor caster, CombatActor clicked, BattlePartyManager2D party, CombatActor enemy)
    {
        if (rt == null)
            return null;
        if (!rt.useV2Targeting || !SkillTargetRuleUtility.IsMultiTarget(rt.targetRuleV2))
            return null;

        return ResolveTargets(rt.targetRuleV2, caster, clicked, party, enemy);
    }

    public static IReadOnlyList<CombatActor> ResolveTargets(SkillTargetRule rule, CombatActor caster, CombatActor clicked, BattlePartyManager2D party, CombatActor fallbackEnemy)
    {
        switch (rule)
        {
            case SkillTargetRule.RowEnemies:
                return ResolveEnemyRowTargets(caster, clicked, party, fallbackEnemy);

            case SkillTargetRule.RowAllies:
                return ResolveAllyRowTargets(caster, clicked, party);

            case SkillTargetRule.AllEnemies:
                return ResolveEnemySideTargets(caster, party, fallbackEnemy);

            case SkillTargetRule.AllAllies:
                return ResolveAllySideTargets(caster, party);

            default:
                return null;
        }
    }

    public static List<CombatActor> ResolveAliveEnemiesSnapshot(BattlePartyManager2D party, CombatActor enemy)
    {
        if (party != null)
            return party.GetAliveEnemies(frontOnly: false);

        var list = new List<CombatActor>(1);
        if (enemy != null && !enemy.IsDead)
            list.Add(enemy);
        return list;
    }

    public static List<CombatActor> ResolveAliveEnemiesInRow(BattlePartyManager2D party, CombatActor enemy, CombatActor.RowTag row)
    {
        if (party != null)
        {
            List<CombatActor> enemies = party.GetAliveEnemies(frontOnly: false);
            return FilterActorsByRow(enemies, row);
        }

        var list = new List<CombatActor>(1);
        if (enemy != null && !enemy.IsDead && enemy.row == row)
            list.Add(enemy);
        return list;
    }

    public static List<CombatActor> ResolveAliveAlliesSnapshot(BattlePartyManager2D party, CombatActor caster)
    {
        if (caster == null)
            return new List<CombatActor>();

        if (party == null)
            return new List<CombatActor> { caster };

        if (caster.team == CombatActor.TeamSide.Ally)
            return party.GetAliveAllies(includePlayer: true);

        return party.GetAliveEnemies(frontOnly: false);
    }

    public static List<CombatActor> ResolveAliveAlliesInRow(BattlePartyManager2D party, CombatActor caster, CombatActor.RowTag row)
    {
        if (caster == null)
            return new List<CombatActor>();

        if (party == null)
        {
            var list = new List<CombatActor>(1);
            if (!caster.IsDead && caster.row == row)
                list.Add(caster);
            return list;
        }

        if (caster.team == CombatActor.TeamSide.Ally)
            return FilterActorsByRow(party.GetAliveAllies(includePlayer: true), row);

        return FilterActorsByRow(party.GetAliveEnemies(frontOnly: false), row);
    }

    private static IReadOnlyList<CombatActor> ResolveEnemySideTargets(CombatActor caster, BattlePartyManager2D party, CombatActor fallbackEnemy)
    {
        if (caster != null && caster.team == CombatActor.TeamSide.Enemy)
        {
            if (party != null)
                return party.GetAliveAllies(includePlayer: true);
            return new List<CombatActor>();
        }

        return ResolveAliveEnemiesSnapshot(party, fallbackEnemy);
    }

    private static IReadOnlyList<CombatActor> ResolveAllySideTargets(CombatActor caster, BattlePartyManager2D party)
    {
        return ResolveAliveAlliesSnapshot(party, caster);
    }

    private static IReadOnlyList<CombatActor> ResolveEnemyRowTargets(CombatActor caster, CombatActor clicked, BattlePartyManager2D party, CombatActor fallbackEnemy)
    {
        CombatActor.RowTag row = clicked != null ? clicked.row : CombatActor.RowTag.Front;

        if (caster != null && caster.team == CombatActor.TeamSide.Enemy)
        {
            if (party != null)
                return FilterActorsByRow(party.GetAliveAllies(includePlayer: true), row);

            var list = new List<CombatActor>(1);
            if (clicked != null && !clicked.IsDead && clicked.row == row)
                list.Add(clicked);
            return list;
        }

        return ResolveAliveEnemiesInRow(party, fallbackEnemy, row);
    }

    private static IReadOnlyList<CombatActor> ResolveAllyRowTargets(CombatActor caster, CombatActor clicked, BattlePartyManager2D party)
    {
        CombatActor.RowTag row = clicked != null ? clicked.row : (caster != null ? caster.row : CombatActor.RowTag.Front);
        return ResolveAliveAlliesInRow(party, caster, row);
    }

    private static List<CombatActor> FilterActorsByRow(IReadOnlyList<CombatActor> actors, CombatActor.RowTag row)
    {
        var list = new List<CombatActor>();
        if (actors == null)
            return list;

        for (int i = 0; i < actors.Count; i++)
        {
            CombatActor actor = actors[i];
            if (actor == null || actor.IsDead || actor.row != row)
                continue;
            list.Add(actor);
        }

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

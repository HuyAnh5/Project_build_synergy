using System.Collections.Generic;

internal static class TurnManagerTargetingUtility
{
    public static bool IsValidTargetForPendingSkill(
        SkillRuntime rt,
        CombatActor clicked,
        CombatActor player,
        BattlePartyManager2D party,
        CombatActor enemy)
        => TryValidateTargetForPendingSkill(rt, clicked, player, party, enemy, out _);

    public static bool TryValidateTargetForPendingSkill(
        SkillRuntime rt,
        CombatActor clicked,
        CombatActor player,
        BattlePartyManager2D party,
        CombatActor enemy,
        out string reason)
    {
        reason = "";
        if (rt == null) { reason = "rt == null"; return false; }
        if (clicked == null) { reason = "clicked == null"; return false; }
        if (player == null) { reason = "player == null"; return false; }

        if (rt.useV2Targeting)
        {
            switch (rt.targetRuleV2)
            {
                case SkillTargetRule.Self:
                    if (clicked != player) { reason = "self target but clicked != player"; return false; }
                    return true;

                case SkillTargetRule.SingleAlly:
                case SkillTargetRule.RowAllies:
                case SkillTargetRule.AllAllies:
                    if (!IsFriendlyTarget(clicked, player, party)) { reason = "ally target but clicked is not friendly"; return false; }
                    return true;

                case SkillTargetRule.SingleEnemy:
                case SkillTargetRule.RowEnemies:
                case SkillTargetRule.AllEnemies:
                    if (IsFriendlyTarget(clicked, player, party)) { reason = "enemy target but clicked is friendly"; return false; }
                    break;
            }
        }
        else
        {
            if (rt.target == TargetRule.Self)
            {
                if (clicked != player) { reason = "legacy self target but clicked != player"; return false; }
                return true;
            }

            if (rt.target == TargetRule.Enemy && IsFriendlyTarget(clicked, player, party))
            {
                reason = "legacy enemy target but clicked is friendly";
                return false;
            }
        }

        if (rt.kind == SkillKind.Attack &&
            rt.range == RangeType.Melee &&
            ShouldUseStrikeFrontBlock(rt))
        {
            bool anyFrontAlive = HasFrontEnemyAlive(party, enemy);
            if (anyFrontAlive && !IsFriendlyTarget(clicked, player, party) && clicked.row != CombatActor.RowTag.Front)
            {
                reason = "melee strike is blocked by front row";
                return false;
            }
        }

        if (rt.target == TargetRule.Enemy || rt.useV2Targeting)
            return true;

        reason = $"Unhandled target config target={rt.target} targetRuleV2={rt.targetRuleV2}";
        return false;
    }

    private static bool ShouldUseStrikeFrontBlock(SkillRuntime rt)
    {
        if (rt == null)
            return false;

        if (!rt.useV2Targeting)
            return !rt.hitAllEnemies;

        return rt.targetRuleV2 == SkillTargetRule.SingleEnemy ||
               rt.targetRuleV2 == SkillTargetRule.RowEnemies;
    }

    private static bool IsFriendlyTarget(CombatActor actor, CombatActor player, BattlePartyManager2D party)
    {
        if (actor == null || player == null)
            return false;
        if (actor == player)
            return true;
        return party != null && actor.team == player.team;
    }

    private static bool HasFrontEnemyAlive(BattlePartyManager2D party, CombatActor enemy)
    {
        if (party != null)
        {
            List<CombatActor> fronts = party.GetAliveEnemies(frontOnly: true);
            return fronts != null && fronts.Count > 0;
        }

        return enemy != null && !enemy.IsDead && enemy.row == CombatActor.RowTag.Front;
    }
}

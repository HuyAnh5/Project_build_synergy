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
        if (clicked == null) { reason = "clicked == null (không raycast trúng actor?)"; return false; }
        if (player == null) { reason = "player == null"; return false; }

        if (rt.useV2Targeting)
        {
            switch (rt.targetRuleV2)
            {
                case SkillTargetRule.Self:
                    if (clicked != player) { reason = "targetRuleV2=Self nhưng clicked != player"; return false; }
                    return true;

                case SkillTargetRule.SingleAlly:
                case SkillTargetRule.AllAllies:
                    if (clicked != player) { reason = "ally target nhưng clicked != player"; return false; }
                    return true;

                case SkillTargetRule.SingleEnemy:
                case SkillTargetRule.AllEnemies:
                    if (clicked == player) { reason = "enemy target nhưng clicked == player"; return false; }
                    break;

                case SkillTargetRule.AllUnits:
                    return true;
            }
        }
        else
        {
            if (rt.target == TargetRule.Self)
            {
                if (clicked != player) { reason = "rt.target=Self nhưng clicked != player"; return false; }
                return true;
            }

            if (rt.target == TargetRule.Enemy && clicked == player)
            {
                reason = "rt.target=Enemy nhưng clicked == player";
                return false;
            }
        }

        if (rt.kind == SkillKind.Attack && rt.range == RangeType.Melee && !rt.hitAllEnemies)
        {
            bool anyFrontAlive = HasFrontEnemyAlive(party, enemy);
            if (anyFrontAlive && clicked != player && clicked.row != CombatActor.RowTag.Front)
            {
                reason = "melee front-only: clicked không phải Front";
                return false;
            }
        }

        if (rt.target == TargetRule.Enemy || rt.useV2Targeting)
            return true;

        reason = $"Unhandled target config target={rt.target} targetRuleV2={rt.targetRuleV2}";
        return false;
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

using UnityEngine;

// Builds the condition context that standard and custom skill conditions read from.
public static partial class SkillRuntimeEvaluator
{
    // Captures dice, resource, target, and board state for condition evaluation.
    private static SkillConditionContext BuildConditionContext(
        SkillConditionScope scope,
        CombatActor owner,
        DiceSlotRig diceRig,
        int start0,
        int span,
        ElementType skillElement,
        CombatActor target)
        => BuildConditionContext(scope, owner, diceRig, start0, span, skillElement, target, -1);

    private static SkillConditionContext BuildConditionContext(
        SkillConditionScope scope,
        CombatActor owner,
        DiceSlotRig diceRig,
        int start0,
        int span,
        ElementType skillElement,
        CombatActor target,
        int paymentMask)
    {
        int gatherStart = scope == SkillConditionScope.Global ? 0 : start0;
        int gatherSpan = scope == SkillConditionScope.Global ? 3 : span;
        int gatherMask = scope == SkillConditionScope.Global ? -1 : paymentMask;
        BattlePartyManager2D party = Object.FindObjectOfType<BattlePartyManager2D>(true);
        int enemiesWithBurnCount = 0;
        int markedEnemiesCount = 0;
        int totalBleedOnBoard = 0;
        int aliveEnemiesCount = 0;
        int enemiesWithStatusCount = 0;

        CountEnemyBoardState(
            party,
            ref enemiesWithBurnCount,
            ref markedEnemiesCount,
            ref totalBleedOnBoard,
            ref aliveEnemiesCount,
            ref enemiesWithStatusCount);

        int leftmostActive = FindLeftmostActiveSlot(diceRig);
        int rightmostActive = FindRightmostActiveSlot(diceRig);
        int actionEnd0 = start0 + Mathf.Max(1, span) - 1;

        return new SkillConditionContext
        {
            scope = scope,
            localBaseValues = GatherDiceForScope(scope, diceRig, gatherStart, gatherSpan, gatherMask),
            localOutputBaseValues = GatherOutputBaseValuesForScope(diceRig, gatherStart, gatherSpan, gatherMask),
            localNumericFlags = GatherNumericFlags(diceRig, gatherStart, gatherSpan, gatherMask),
            localResolvedValues = GatherResolvedDiceForScope(diceRig, owner, gatherStart, gatherSpan, skillElement, gatherMask),
            localCritFlags = GatherCritFlags(diceRig, gatherStart, gatherSpan, gatherMask),
            localFailFlags = GatherFailFlags(diceRig, gatherStart, gatherSpan, gatherMask),
            currentFocus = owner != null ? owner.focus : 0,
            currentGuard = owner != null ? owner.guardPool : 0,
            targetGuard = target != null ? target.guardPool : 0,
            occupiedSlots = Mathf.Clamp(span, 1, MaxCombatSlots),
            remainingSlots = Mathf.Clamp(MaxCombatSlots - span, 0, MaxCombatSlots),
            enemiesWithBurnCount = enemiesWithBurnCount,
            markedEnemiesCount = markedEnemiesCount,
            totalBleedOnBoard = totalBleedOnBoard,
            aliveEnemiesCount = aliveEnemiesCount,
            enemiesWithStatusCount = enemiesWithStatusCount,
            isLeftmostAction = leftmostActive >= 0 && start0 == leftmostActive,
            isRightmostAction = rightmostActive >= 0 && actionEnd0 == rightmostActive,
            targetHasBurn = target != null && target.status != null && target.status.burnStacks > 0,
            targetHasFreeze = target != null && target.status != null && target.status.frozen,
            targetHasChilled = target != null && target.status != null && target.status.chilledTurns > 0,
            targetHasMark = target != null && target.status != null && target.status.marked,
            targetHasBleed = target != null && target.status != null && target.status.bleedStacks > 0,
            targetHasStagger = target != null && target.status != null && target.status.staggered
        };
    }

    // Counts enemy-side board state used by board axis conditions.
    private static void CountEnemyBoardState(
        BattlePartyManager2D party,
        ref int enemiesWithBurnCount,
        ref int markedEnemiesCount,
        ref int totalBleedOnBoard,
        ref int aliveEnemiesCount,
        ref int enemiesWithStatusCount)
    {
        if (party == null || party.Enemies == null)
            return;

        var enemies = party.Enemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            CombatActor enemy = enemies[i];
            if (enemy == null || enemy.IsDead || enemy.status == null)
                continue;

            aliveEnemiesCount++;
            if (enemy.status.burnStacks > 0)
                enemiesWithBurnCount++;
            if (enemy.status.marked)
                markedEnemiesCount++;
            totalBleedOnBoard += Mathf.Max(0, enemy.status.bleedStacks);
            if (enemy.status.burnStacks > 0 ||
                enemy.status.marked ||
                enemy.status.frozen ||
                enemy.status.chilledTurns > 0 ||
                enemy.status.bleedStacks > 0 ||
                enemy.status.staggered)
            {
                enemiesWithStatusCount++;
            }
        }
    }
}

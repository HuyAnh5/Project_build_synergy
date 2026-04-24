using System.Collections.Generic;

public readonly struct SkillTargetSelection
{
    public SkillTargetSelection(CombatActor primaryTarget, IReadOnlyList<CombatActor> targets)
    {
        PrimaryTarget = primaryTarget;
        Targets = targets;
    }

    public CombatActor PrimaryTarget { get; }
    public IReadOnlyList<CombatActor> Targets { get; }
}

internal sealed class SkillTargetSelectionService
{
    public SkillTargetSelection SelectExecutionTargets(
        SkillTargetRule rule,
        CombatActor caster,
        CombatActor clickedTarget,
        IReadOnlyList<CombatActor> aoeTargets)
    {
        List<CombatActor> targets = new List<CombatActor>(8);

        switch (rule)
        {
            case SkillTargetRule.Self:
                AddIfValid(targets, caster);
                return new SkillTargetSelection(caster, targets);

            case SkillTargetRule.RowEnemies:
            case SkillTargetRule.RowAllies:
            case SkillTargetRule.AllEnemies:
            case SkillTargetRule.AllAllies:
                AddTargets(targets, aoeTargets);
                if (targets.Count == 0)
                    AddIfValid(targets, clickedTarget);
                return new SkillTargetSelection(targets.Count > 0 ? targets[0] : clickedTarget, targets);

            case SkillTargetRule.SingleAlly:
            case SkillTargetRule.SingleEnemy:
            default:
                AddIfValid(targets, clickedTarget);
                return new SkillTargetSelection(clickedTarget, targets);
        }
    }

    public SkillTargetSelection SelectCombatTargets(
        SkillTargetRule rule,
        CombatActor caster,
        CombatActor clickedTarget,
        BattlePartyManager2D party,
        CombatActor fallbackEnemy)
    {
        IReadOnlyList<CombatActor> targets = null;
        if (SkillTargetRuleUtility.IsMultiTarget(rule))
            targets = TurnManagerCombatUtility.ResolveTargets(rule, caster, clickedTarget, party, fallbackEnemy);

        return SelectExecutionTargets(rule, caster, clickedTarget, targets);
    }

    private static void AddTargets(List<CombatActor> results, IReadOnlyList<CombatActor> targets)
    {
        if (targets == null)
            return;

        for (int i = 0; i < targets.Count; i++)
            AddIfValid(results, targets[i]);
    }

    private static void AddIfValid(List<CombatActor> results, CombatActor actor)
    {
        if (actor != null)
            results.Add(actor);
    }
}

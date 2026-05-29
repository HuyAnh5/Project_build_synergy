using System.Collections.Generic;
using UnityEngine;

public static partial class SkillGameplayResolver
{
    /// <summary>
    /// Resolves abstract gameplay targets like self, row, or all enemies into concrete actors.
    /// </summary>
    public static List<CombatActor> ResolveEffectTargets(SkillEffectTarget effectTarget, SkillResolveContext context)
    {
        List<CombatActor> targets = new List<CombatActor>();
        if (context == null)
        {
            return targets;
        }

        switch (effectTarget)
        {
            case SkillEffectTarget.Self:
                AddTarget(targets, context.caster);
                break;

            case SkillEffectTarget.RowEnemies:
                AddEnemyRowTargets(targets, context.caster, context.target);
                break;

            case SkillEffectTarget.AllEnemies:
                AddAllEnemyTargets(targets, context.caster, context.target);
                break;

            case SkillEffectTarget.SelectedEnemy:
            default:
                AddTarget(targets, context.target);
                break;
        }

        return targets;
    }

    private static SkillConditionContext BuildFollowUpConditionContext(
        SkillResolveContext context,
        IReadOnlyList<ResolvedEffect> resolvedEffects)
    {
        SkillConditionContext baseContext = context != null ? context.conditionContext : null;
        SkillConditionContext followUpContext = CloneConditionContext(baseContext);
        if (context == null || followUpContext == null || context.target == null)
        {
            return followUpContext;
        }

        int simulatedGuard = Mathf.Max(0, followUpContext.targetGuard);
        bool willBreakGuard = false;
        if (resolvedEffects != null)
        {
            for (int i = 0; i < resolvedEffects.Count; i++)
            {
                ResolvedEffect effect = resolvedEffects[i];
                if (effect == null || effect.sameActionFollowUp)
                {
                    continue;
                }

                CombatActor effectTarget = effect.targetActor != null ? effect.targetActor : context.target;
                if (effectTarget != context.target)
                {
                    continue;
                }

                switch (effect.type)
                {
                    case SkillEffectType.DealDamage:
                    case SkillEffectType.DealSecondaryDamage:
                    {
                        int incoming = Mathf.Max(0, effect.value);
                        if (incoming <= 0)
                        {
                            break;
                        }

                        if (simulatedGuard > 0)
                        {
                            int blocked = Mathf.Min(simulatedGuard, incoming);
                            simulatedGuard -= blocked;
                            if (blocked > 0 && simulatedGuard <= 0)
                            {
                                willBreakGuard = true;
                            }
                        }

                        break;
                    }

                    case SkillEffectType.ClearGuard:
                        if (simulatedGuard > 0)
                        {
                            willBreakGuard = true;
                        }

                        simulatedGuard = 0;
                        break;
                }
            }
        }

        followUpContext.targetGuard = simulatedGuard;
        followUpContext.targetHasStagger = followUpContext.targetHasStagger || willBreakGuard;
        return followUpContext;
    }

    private static SkillConditionContext CloneConditionContext(SkillConditionContext source)
    {
        if (source == null)
        {
            return null;
        }

        return new SkillConditionContext
        {
            scope = source.scope,
            localBaseValues = source.localBaseValues,
            localNumericFlags = source.localNumericFlags,
            localResolvedValues = source.localResolvedValues,
            localCritFlags = source.localCritFlags,
            localFailFlags = source.localFailFlags,
            currentFocus = source.currentFocus,
            currentGuard = source.currentGuard,
            targetGuard = source.targetGuard,
            occupiedSlots = source.occupiedSlots,
            remainingSlots = source.remainingSlots,
            enemiesWithBurnCount = source.enemiesWithBurnCount,
            markedEnemiesCount = source.markedEnemiesCount,
            totalBleedOnBoard = source.totalBleedOnBoard,
            aliveEnemiesCount = source.aliveEnemiesCount,
            enemiesWithStatusCount = source.enemiesWithStatusCount,
            isLeftmostAction = source.isLeftmostAction,
            isRightmostAction = source.isRightmostAction,
            targetHasBurn = source.targetHasBurn,
            targetHasFreeze = source.targetHasFreeze,
            targetHasChilled = source.targetHasChilled,
            targetHasMark = source.targetHasMark,
            targetHasBleed = source.targetHasBleed,
            targetHasStagger = source.targetHasStagger
        };
    }

    private static void AddAllEnemyTargets(List<CombatActor> targets, CombatActor caster, CombatActor fallbackTarget)
    {
        CombatActor[] actors = Object.FindObjectsOfType<CombatActor>(true);
        for (int i = 0; i < actors.Length; i++)
        {
            CombatActor actor = actors[i];
            if (actor == null || actor.IsDead)
            {
                continue;
            }

            if (caster != null && actor.team == caster.team)
            {
                continue;
            }

            if (caster == null && fallbackTarget != null && actor.team != fallbackTarget.team)
            {
                continue;
            }

            AddTarget(targets, actor);
        }

        if (targets.Count == 0)
        {
            AddTarget(targets, fallbackTarget);
        }
    }

    private static void AddEnemyRowTargets(List<CombatActor> targets, CombatActor caster, CombatActor rowAnchor)
    {
        if (rowAnchor == null)
        {
            AddTarget(targets, null);
            return;
        }

        CombatActor.RowTag row = rowAnchor.row;
        CombatActor[] actors = Object.FindObjectsOfType<CombatActor>(true);
        for (int i = 0; i < actors.Length; i++)
        {
            CombatActor actor = actors[i];
            if (actor == null || actor.IsDead || actor.row != row)
            {
                continue;
            }

            if (caster != null && actor.team == caster.team)
            {
                continue;
            }

            if (caster == null && actor.team != rowAnchor.team)
            {
                continue;
            }

            AddTarget(targets, actor);
        }

        if (targets.Count == 0)
        {
            AddTarget(targets, rowAnchor);
        }
    }

    private static void AddTarget(List<CombatActor> targets, CombatActor target)
    {
        if (target == null || target.IsDead || targets.Contains(target))
        {
            return;
        }

        targets.Add(target);
    }

    private static int CountAliveEnemies(CombatActor caster)
    {
        CombatActor[] actors = Object.FindObjectsOfType<CombatActor>();
        int count = 0;
        for (int i = 0; i < actors.Length; i++)
        {
            CombatActor actor = actors[i];
            if (actor != null && actor != caster && !actor.IsDead && actor.team == CombatActor.TeamSide.Enemy)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountEnemiesWithStatus(CombatActor caster, StatusKind status)
    {
        CombatActor[] actors = Object.FindObjectsOfType<CombatActor>();
        int count = 0;
        for (int i = 0; i < actors.Length; i++)
        {
            CombatActor actor = actors[i];
            if (actor == null || actor == caster || actor.IsDead || actor.team != CombatActor.TeamSide.Enemy || actor.status == null)
            {
                continue;
            }

            if (status == StatusKind.Burn && actor.status.burnStacks > 0)
            {
                count++;
            }
            else if (status == StatusKind.Mark && actor.status.marked)
            {
                count++;
            }
            else if (status == StatusKind.Bleed && actor.status.bleedStacks > 0)
            {
                count++;
            }
            else if (status == StatusKind.Freeze && actor.status.frozen)
            {
                count++;
            }
            else if (status == StatusKind.Chilled && actor.status.chilledTurns > 0)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountTotalBleed(CombatActor caster)
    {
        CombatActor[] actors = Object.FindObjectsOfType<CombatActor>();
        int total = 0;
        for (int i = 0; i < actors.Length; i++)
        {
            CombatActor actor = actors[i];
            if (actor != null && actor != caster && !actor.IsDead && actor.team == CombatActor.TeamSide.Enemy && actor.status != null)
            {
                total += Mathf.Max(0, actor.status.bleedStacks);
            }
        }

        return total;
    }

    private static int CountEnemiesWithAnyStatus(CombatActor caster)
    {
        CombatActor[] actors = Object.FindObjectsOfType<CombatActor>();
        int count = 0;
        for (int i = 0; i < actors.Length; i++)
        {
            CombatActor actor = actors[i];
            if (actor == null || actor == caster || actor.IsDead || actor.team != CombatActor.TeamSide.Enemy || actor.status == null)
            {
                continue;
            }

            if (actor.status.burnStacks > 0 ||
                actor.status.marked ||
                actor.status.bleedStacks > 0 ||
                actor.status.frozen ||
                actor.status.chilledTurns > 0 ||
                actor.status.staggered)
            {
                count++;
            }
        }

        return count;
    }
}

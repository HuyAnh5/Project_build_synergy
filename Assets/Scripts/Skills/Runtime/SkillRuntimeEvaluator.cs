using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds a SkillRuntime for the current placement and dice state.
/// This is what makes "condition" affect gameplay (not just UI/cost).
/// </summary>
public static class SkillRuntimeEvaluator
{
    private const int MaxCombatSlots = 3;

    public static SkillRuntime Evaluate(SkillDamageSO skill, CombatActor owner, DiceSlotRig diceRig, int span, int start0)
    {
        return Evaluate(skill, owner, diceRig, anchor0: start0, span: span, start0: start0, target: null);
    }

    /// <summary>
    /// Evaluate a SkillDamageSO into a SkillRuntime for this turn.
    /// Condition + overrides apply ONLY for SkillDamageSO.
    /// </summary>
    public static SkillRuntime Evaluate(SkillDamageSO skill, CombatActor owner, DiceSlotRig diceRig, int anchor0, int span, int start0)
    {
        return Evaluate(skill, owner, diceRig, anchor0, span, start0, target: null);
    }

    public static SkillRuntime Evaluate(SkillDamageSO skill, CombatActor owner, DiceSlotRig diceRig, int anchor0, int span, int start0, CombatActor target)
    {
        if (skill == null) return null;

        var rt = SkillRuntime.FromDamage(skill);
        if (rt == null) return null;
        rt.localBaseValues = GatherDiceForScope(SkillConditionScope.SlotBound, diceRig, start0, span);
        rt.localCritFlags = GatherCritFlags(diceRig, start0, span);
        rt.localFailFlags = GatherFailFlags(diceRig, start0, span);
        GatherCritFailFlags(diceRig, owner, start0, span, out rt.localCritAny, out rt.localFailAny);

        bool met = false;

        if (skill.hasCondition && diceRig != null)
        {
            SkillConditionContext conditionContext = BuildConditionContext(skill.condition.scope, owner, diceRig, start0, span, rt.element, target);
            met = skill.conditionEditorMode == ConditionEditorMode.Builder
                ? EvaluateBuilderCondition(skill, owner, diceRig, conditionContext)
                : SkillConditionEvaluator.Evaluate(skill.condition, conditionContext);
        }

        rt.conditionMet = met;

        // Safety: Guard always Self and not AoE
        if (rt.kind == SkillKind.Guard || rt.coreAction == CoreAction.BasicGuard)
        {
            rt.kind = SkillKind.Guard;
            rt.targetRuleV2 = SkillTargetRule.Self;
            rt.target = TargetRule.Self;
            rt.hitAllEnemies = false;
            rt.hitAllAllies = false;
        }

        rt.localResolvedValues = GatherResolvedDiceForScope(diceRig, owner, start0, span, rt.element);

        return rt;
    }

    private static SkillConditionContext BuildConditionContext(SkillConditionScope scope, CombatActor owner, DiceSlotRig diceRig, int start0, int span, ElementType skillElement, CombatActor target)
    {
        int gatherStart = scope == SkillConditionScope.Global ? 0 : start0;
        int gatherSpan = scope == SkillConditionScope.Global ? 3 : span;
        BattlePartyManager2D party = Object.FindObjectOfType<BattlePartyManager2D>(true);
        int enemiesWithBurnCount = 0;
        int markedEnemiesCount = 0;
        int totalBleedOnBoard = 0;
        int aliveEnemiesCount = 0;
        int enemiesWithStatusCount = 0;

        if (party != null)
        {
            var enemies = party.Enemies;
            if (enemies != null)
            {
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

        int leftmostActive = FindLeftmostActiveSlot(diceRig);
        int rightmostActive = FindRightmostActiveSlot(diceRig);
        int actionEnd0 = start0 + Mathf.Max(1, span) - 1;

        return new SkillConditionContext
        {
            scope = scope,
            localBaseValues = GatherDiceForScope(scope, diceRig, gatherStart, gatherSpan),
            localResolvedValues = GatherResolvedDiceForScope(diceRig, owner, gatherStart, gatherSpan, skillElement),
            localCritFlags = GatherCritFlags(diceRig, gatherStart, gatherSpan),
            localFailFlags = GatherFailFlags(diceRig, gatherStart, gatherSpan),
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

    private static bool EvaluateBuilderCondition(SkillDamageSO skill, CombatActor owner, DiceSlotRig diceRig, SkillConditionContext context)
    {
        if (skill == null || context == null)
            return false;

        switch (skill.standardConditionFamily)
        {
            case SkillConditionFamily.DiceParity:
                return skill.diceParityConditionPreset == DiceParityConditionPreset.Even
                    ? AllEven(context.localBaseValues)
                    : AllOdd(context.localBaseValues);

            case SkillConditionFamily.CritFail:
                return skill.critFailConditionPreset == CritFailConditionPreset.Crit
                    ? AllTrue(context.localCritFlags)
                    : AllTrue(context.localFailFlags);

            case SkillConditionFamily.ExactValue:
                return EvaluateExactBuilderCondition(skill, owner, diceRig, context.localBaseValues);

            case SkillConditionFamily.LocalGroupRelation:
                return EvaluateLocalGroupCondition(skill, context);

            case SkillConditionFamily.Resource:
            case SkillConditionFamily.TargetState:
            case SkillConditionFamily.BoardState:
                return skill.condition != null && SkillConditionEvaluator.Evaluate(skill.condition, context);

            default:
                return false;
        }
    }

    private static bool EvaluateExactBuilderCondition(SkillDamageSO skill, CombatActor owner, DiceSlotRig diceRig, IReadOnlyList<int> localBaseValues)
    {
        switch (skill.exactConditionMode)
        {
            case SkillExactConditionMode.DieEqualsX:
                return AllMatch(localBaseValues, skill.exactValueX);

            case SkillExactConditionMode.GroupContainsPattern:
                return ContainsAnyPatternValue(localBaseValues, ParseExactPattern(skill.exactValuePattern, ownedOnly: false, diceRig: diceRig));

            case SkillExactConditionMode.RandomExactNumberOwned:
            {
                int target = GetOrCreateRandomExactValue(skill, owner, diceRig, ownedOnly: true, fallbackValue: skill.exactValueX);
                return ContainsAnyPatternValue(localBaseValues, new List<int> { target });
            }

            case SkillExactConditionMode.RandomExactNumberRandom:
            {
                int target = GetOrCreateRandomExactValue(skill, owner, diceRig, ownedOnly: false, fallbackValue: skill.exactValueX);
                return ContainsAnyPatternValue(localBaseValues, new List<int> { target });
            }

            default:
                return false;
        }
    }

    private static bool EvaluateLocalGroupCondition(SkillDamageSO skill, SkillConditionContext context)
    {
        switch (skill.localGroupRelationMode)
        {
            case LocalGroupRelationMode.SelfPosition:
                return skill.localGroupRelationSide == LocalGroupRelationSide.Left
                    ? context.isLeftmostAction
                    : context.isRightmostAction;

            case LocalGroupRelationMode.NeighborRelation:
                return skill.localGroupRelationSide == LocalGroupRelationSide.Left
                    ? !context.isLeftmostAction
                    : !context.isRightmostAction;

            case LocalGroupRelationMode.SplitRole:
                return skill.localGroupConditionPreset == LocalGroupConditionPreset.Highest
                    ? context.localBaseValues != null && context.localBaseValues.Count > 0
                    : context.localBaseValues != null && context.localBaseValues.Count > 0;

            default:
                return false;
        }
    }

    private static int GetOrCreateRandomExactValue(SkillDamageSO skill, CombatActor owner, DiceSlotRig diceRig, bool ownedOnly, int fallbackValue)
    {
        SkillCombatState state = owner != null ? owner.GetComponent<SkillCombatState>() : null;
        List<int> candidates = ownedOnly ? GatherOwnedFaceValues(diceRig) : null;
        int min = 1;
        int max = 99;

        if (state != null)
            return state.GetOrCreateExactConditionValue(skill != null ? skill.GetInstanceID() : 0, candidates, min, max, fallbackValue);

        if (candidates != null && candidates.Count > 0)
            return candidates[0];

        return fallbackValue;
    }

    private static List<int> ParseExactPattern(string pattern, bool ownedOnly, DiceSlotRig diceRig)
    {
        var result = new List<int>();
        if (string.IsNullOrWhiteSpace(pattern))
            return result;

        List<int> owned = ownedOnly ? GatherOwnedFaceValues(diceRig) : null;
        string[] parts = pattern.Split(new[] { '-', ',', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out int parsed))
                continue;

            if (ownedOnly && (owned == null || !owned.Contains(parsed)))
                continue;

            if (!result.Contains(parsed))
                result.Add(parsed);
        }

        return result;
    }

    private static List<int> GatherOwnedFaceValues(DiceSlotRig diceRig)
    {
        var values = new List<int>();
        if (diceRig == null || diceRig.slots == null)
            return values;

        for (int i = 0; i < diceRig.slots.Length; i++)
        {
            DiceSpinnerGeneric die = diceRig.slots[i] != null ? diceRig.slots[i].dice : null;
            if (die == null || die.faces == null)
                continue;

            for (int f = 0; f < die.faces.Length; f++)
            {
                int value = die.faces[f].value;
                if (!values.Contains(value))
                    values.Add(value);
            }
        }

        return values;
    }

    private static bool ContainsAnyPatternValue(IReadOnlyList<int> localBaseValues, List<int> patternValues)
    {
        if (localBaseValues == null || localBaseValues.Count == 0 || patternValues == null || patternValues.Count == 0)
            return false;

        for (int i = 0; i < localBaseValues.Count; i++)
        {
            if (patternValues.Contains(localBaseValues[i]))
                return true;
        }

        return false;
    }

    private static bool AllMatch(IReadOnlyList<int> values, int target)
    {
        if (values == null || values.Count == 0)
            return false;

        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] != target)
                return false;
        }

        return true;
    }

    private static bool AllEven(IReadOnlyList<int> values)
    {
        if (values == null || values.Count == 0)
            return false;

        for (int i = 0; i < values.Count; i++)
        {
            if ((values[i] % 2) != 0)
                return false;
        }

        return true;
    }

    private static bool AllOdd(IReadOnlyList<int> values)
    {
        if (values == null || values.Count == 0)
            return false;

        for (int i = 0; i < values.Count; i++)
        {
            if ((values[i] % 2) == 0)
                return false;
        }

        return true;
    }

    private static bool AllTrue(IReadOnlyList<bool> values)
    {
        if (values == null || values.Count == 0)
            return false;

        for (int i = 0; i < values.Count; i++)
        {
            if (!values[i])
                return false;
        }

        return true;
    }

    private static int FindLeftmostActiveSlot(DiceSlotRig diceRig)
    {
        if (diceRig == null || diceRig.slots == null)
            return -1;

        for (int i = 0; i < diceRig.slots.Length && i < MaxCombatSlots; i++)
        {
            if (diceRig.IsSlotActive(i))
                return i;
        }

        return -1;
    }

    private static int FindRightmostActiveSlot(DiceSlotRig diceRig)
    {
        if (diceRig == null || diceRig.slots == null)
            return -1;

        for (int i = Mathf.Min(diceRig.slots.Length, MaxCombatSlots) - 1; i >= 0; i--)
        {
            if (diceRig.IsSlotActive(i))
                return i;
        }

        return -1;
    }

    private static List<int> GatherDiceForScope(SkillConditionScope scope, DiceSlotRig diceRig, int start0, int span)
    {
        var list = new List<int>(3);

        if (diceRig == null) return list;

        if (scope == SkillConditionScope.SlotBound)
        {
            for (int i = start0; i < start0 + span; i++)
            {
                if (i < 0 || i > 2) continue;
                if (!diceRig.IsSlotActive(i)) continue;
                list.Add(diceRig.GetDieValue(i));
            }
            return list;
        }

        // Global
        for (int i = 0; i < 3; i++)
        {
            if (!diceRig.IsSlotActive(i)) continue;
            list.Add(diceRig.GetDieValue(i));
        }
        return list;
    }

    private static List<int> GatherResolvedDiceForScope(DiceSlotRig diceRig, CombatActor owner, int start0, int span, ElementType skillElement)
    {
        var list = new List<int>(3);

        if (diceRig == null)
            return list;

        for (int i = start0; i < start0 + span; i++)
        {
            if (i < 0 || i > 2) continue;
            if (!diceRig.IsSlotActive(i)) continue;
            list.Add(diceRig.GetResolvedDieValue(i, owner, skillElement));
        }

        return list;
    }

    private static List<bool> GatherCritFlags(DiceSlotRig diceRig, int start0, int span)
    {
        var list = new List<bool>(3);
        if (diceRig == null)
            return list;

        for (int i = start0; i < start0 + span; i++)
        {
            if (i < 0 || i > 2) continue;
            if (!diceRig.IsSlotActive(i)) continue;
            list.Add(diceRig.IsCrit(i));
        }

        return list;
    }

    private static List<bool> GatherFailFlags(DiceSlotRig diceRig, int start0, int span)
    {
        var list = new List<bool>(3);
        if (diceRig == null)
            return list;

        for (int i = start0; i < start0 + span; i++)
        {
            if (i < 0 || i > 2) continue;
            if (!diceRig.IsSlotActive(i)) continue;
            list.Add(diceRig.IsFail(i));
        }

        return list;
    }

    private static void GatherCritFailFlags(DiceSlotRig diceRig, CombatActor owner, int start0, int span, out bool critAny, out bool failAny)
    {
        critAny = false;
        failAny = false;

        if (diceRig == null)
            return;

        for (int i = start0; i < start0 + span; i++)
        {
            if (i < 0 || i > 2) continue;
            if (!diceRig.IsSlotActive(i)) continue;
            critAny |= diceRig.IsCrit(i);
            failAny |= diceRig.IsFail(i);
        }
    }

    private static void ApplyDamageOverrides(ref SkillRuntime rt, SkillDamageConditionalOverrides ov)
    {
        if (ov == null) return;

        // Slots
        if (ov.overrideSlotsRequired)
            rt.slotsRequired = Mathf.Clamp(ov.slotsRequired, 1, 3);

        // Identity
        if (ov.overrideIdentity)
        {
            rt.kind = ov.kind;
            rt.useV2Targeting = true;
            rt.targetRuleV2 = ov.target;

            rt.group = ov.group;
            rt.element = (ElementType)(int)ov.element;
            rt.range = ov.range;

            rt.hitAllEnemies = (ov.target == SkillTargetRule.AllEnemies || ov.target == SkillTargetRule.AllUnits);
            rt.hitAllAllies = (ov.target == SkillTargetRule.AllAllies || ov.target == SkillTargetRule.AllUnits);

            rt.target = (ov.target == SkillTargetRule.SingleEnemy || ov.target == SkillTargetRule.AllEnemies || ov.target == SkillTargetRule.AllUnits)
                ? TargetRule.Enemy
                : TargetRule.Self;
        }

        // Cost
        if (ov.overrideCost)
        {
            rt.focusCost = Mathf.Max(0, ov.focusCost);
            rt.focusGainOnCast = ov.focusGainOnCast;
        }

        // Damage
        if (ov.overrideDamage)
        {
            rt.dieMultiplier = ov.dieMultiplier;
            rt.flatDamage = ov.flatDamage;
        }

        // Sunder bonus
        if (ov.overrideSunderBonus)
        {
            rt.sunderBonusIfTargetHasGuard = ov.sunderBonusIfTargetHasGuard;
            rt.sunderGuardDamageMultiplier = ov.sunderGuardDamageMultiplier;
        }

        // Guard
        if (ov.overrideGuard)
        {
            rt.guardValueMode = ov.guardValueMode;
            rt.guardFlat = ov.guardFlat;
        }

        // Special combat
        if (ov.overrideSpecialCombat)
        {
            rt.bypassGuard = ov.bypassGuard;
            rt.clearsGuard = ov.clearsGuard;
            rt.canUseMarkMultiplier = ov.canUseMarkMultiplier;
        }

        // Burn spender
        if (ov.overrideBurnSpender)
        {
            rt.consumesBurn = ov.consumesBurn;
            rt.burnDamagePerStack = ov.burnDamagePerStack;
        }

        // Apply status
        if (ov.overrideApplyStatus)
        {
            rt.applyBurn = ov.applyBurn;
            rt.burnAddStacks = ov.burnAddStacks;
            rt.burnRefreshTurns = ov.burnRefreshTurns;

            rt.applyMark = ov.applyMark;

            rt.applyBleed = ov.applyBleed;
            rt.bleedTurns = ov.bleedTurns;

            rt.applyFreeze = ov.applyFreeze;
            rt.freezeChance = ov.freezeChance;
        }

        // VFX
        if (ov.overrideVfx)
            rt.projectilePrefab = ov.projectilePrefab;

        // safety
        if (rt.kind == SkillKind.Guard)
        {
            rt.targetRuleV2 = SkillTargetRule.Self;
            rt.target = TargetRule.Self;
            rt.hitAllEnemies = false;
            rt.hitAllAllies = false;
        }
        if (rt.target == TargetRule.Self) rt.hitAllEnemies = false;
        if (rt.target == TargetRule.Enemy) rt.hitAllAllies = false;
    }
}

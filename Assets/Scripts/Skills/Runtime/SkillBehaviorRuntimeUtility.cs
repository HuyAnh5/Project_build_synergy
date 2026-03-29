using System.Collections.Generic;
using UnityEngine;

internal static class SkillBehaviorRuntimeUtility
{
    public static SkillDamageSO GetDamageSkill(SkillRuntime rt)
    {
        if (rt?.sourceAsset is SkillDamageSO skillDamage)
            return skillDamage;

        return null;
    }

    public static bool IsBehavior(SkillRuntime rt, FireDamageBehaviorId behaviorId)
        => GetDamageSkill(rt)?.IsBehavior(behaviorId) == true;

    public static bool IsBehavior(SkillRuntime rt, IceDamageBehaviorId behaviorId)
        => GetDamageSkill(rt)?.IsBehavior(behaviorId) == true;

    public static bool IsBehavior(SkillRuntime rt, LightningDamageBehaviorId behaviorId)
        => GetDamageSkill(rt)?.IsBehavior(behaviorId) == true;

    public static bool IsBehavior(SkillRuntime rt, BleedDamageBehaviorId behaviorId)
        => GetDamageSkill(rt)?.IsBehavior(behaviorId) == true;

    public static bool IsBehavior(SkillRuntime rt, PhysicalDamageBehaviorId behaviorId)
        => GetDamageSkill(rt)?.IsBehavior(behaviorId) == true;

    public static bool TryGetSingleBaseValue(SkillRuntime rt, out int baseValue)
    {
        baseValue = 0;
        if (rt == null || rt.localBaseValues == null || rt.localBaseValues.Count <= 0)
            return false;

        baseValue = rt.localBaseValues[0];
        return true;
    }

    public static int CountBaseValuesEqual(SkillRuntime rt, int expectedValue)
    {
        if (rt == null || rt.localBaseValues == null || rt.localBaseValues.Count <= 0)
            return 0;

        int count = 0;
        for (int i = 0; i < rt.localBaseValues.Count; i++)
        {
            if (rt.localBaseValues[i] == expectedValue)
                count++;
        }

        return count;
    }

    public static int GetHighestBaseValue(SkillRuntime rt)
    {
        int index = GetHighestBaseValueIndex(rt);
        if (index < 0)
            return 0;

        return rt.localBaseValues[index];
    }

    public static int GetLowestBaseValue(SkillRuntime rt)
    {
        int index = GetLowestBaseValueIndex(rt);
        if (index < 0)
            return 0;

        return rt.localBaseValues[index];
    }

    public static int GetHighestBaseValueIndex(SkillRuntime rt)
    {
        if (rt == null || rt.localBaseValues == null || rt.localBaseValues.Count <= 0)
            return -1;

        int bestIndex = 0;
        int bestValue = rt.localBaseValues[0];
        for (int i = 1; i < rt.localBaseValues.Count; i++)
        {
            if (rt.localBaseValues[i] > bestValue)
            {
                bestValue = rt.localBaseValues[i];
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    public static int GetLowestBaseValueIndex(SkillRuntime rt)
    {
        if (rt == null || rt.localBaseValues == null || rt.localBaseValues.Count <= 0)
            return -1;

        int bestIndex = 0;
        int bestValue = rt.localBaseValues[0];
        for (int i = 1; i < rt.localBaseValues.Count; i++)
        {
            if (rt.localBaseValues[i] < bestValue)
            {
                bestValue = rt.localBaseValues[i];
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    public static int GetPerDieResolvedOutput(SkillRuntime rt, int localIndex)
    {
        if (rt == null || rt.localResolvedValues == null || localIndex < 0 || localIndex >= rt.localResolvedValues.Count)
            return 0;

        int resolvedValue = Mathf.Max(0, rt.localResolvedValues[localIndex]);
        int baseValue = (rt.localBaseValues != null && localIndex < rt.localBaseValues.Count)
            ? Mathf.Max(0, rt.localBaseValues[localIndex])
            : resolvedValue;
        bool isFail = rt.localFailFlags != null && localIndex < rt.localFailFlags.Count && rt.localFailFlags[localIndex];

        return SkillOutputValueUtility.ResolvePerDieOutput(baseValue, resolvedValue, isFail);
    }

    public static int GetHighestResolvedValue(SkillRuntime rt)
    {
        if (rt == null || rt.localResolvedValues == null || rt.localResolvedValues.Count <= 0)
            return 0;

        int best = rt.localResolvedValues[0];
        for (int i = 1; i < rt.localResolvedValues.Count; i++)
            best = Mathf.Max(best, rt.localResolvedValues[i]);
        return best;
    }

    public static int CountMarkedEnemies(CombatActor caster)
    {
        int count = 0;
        CombatActor[] actors = Object.FindObjectsOfType<CombatActor>(true);
        for (int i = 0; i < actors.Length; i++)
        {
            CombatActor actor = actors[i];
            if (actor == null || actor.IsDead || actor.status == null)
                continue;
            if (caster != null && actor.team == caster.team)
                continue;
            if (actor.status.marked)
                count++;
        }

        return count;
    }

    public static int CountBleedOnEnemyTeam(CombatActor caster)
    {
        int count = 0;
        CombatActor[] actors = Object.FindObjectsOfType<CombatActor>(true);
        for (int i = 0; i < actors.Length; i++)
        {
            CombatActor actor = actors[i];
            if (actor == null || actor.IsDead || actor.status == null)
                continue;
            if (caster != null && actor.team == caster.team)
                continue;
            count += Mathf.Max(0, actor.status.bleedStacks);
        }

        return count;
    }

    public static List<CombatActor> GetOtherEnemies(CombatActor caster, CombatActor except)
    {
        List<CombatActor> list = new List<CombatActor>();
        CombatActor[] actors = Object.FindObjectsOfType<CombatActor>(true);
        for (int i = 0; i < actors.Length; i++)
        {
            CombatActor actor = actors[i];
            if (actor == null || actor.IsDead)
                continue;
            if (actor == except)
                continue;
            if (caster != null && actor.team == caster.team)
                continue;
            list.Add(actor);
        }

        return list;
    }
}

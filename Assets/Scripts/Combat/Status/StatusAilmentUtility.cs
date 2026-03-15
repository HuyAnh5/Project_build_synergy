using UnityEngine;
using Random = UnityEngine.Random;

public static class StatusAilmentUtility
{
    public static void TryApplyAilment(
        StatusController owner,
        AilmentType type,
        int durationTurns,
        CombatActor applier,
        int rolledValue,
        int maxFaceValue,
        float chanceMultiplier,
        bool forceChance100,
        bool debugLog)
    {
        if (owner == null) return;

        var actor = owner.GetComponent<CombatActor>();
        if (actor == null) return;

        float chance;
        if (forceChance100)
        {
            chance = 1f;
        }
        else
        {
            chance = AilmentChanceCalculator.ComputeChance(
                attackerIsPlayer: applier != null && applier.isPlayer,
                targetIsPlayer: actor.isPlayer,
                rolledValue: rolledValue,
                maxFaceValue: Mathf.Max(1, maxFaceValue),
                skillTuningMultiplier: Mathf.Max(0f, chanceMultiplier));
        }

        if (debugLog)
            Debug.Log($"[STATUS] TryApplyAilment target={owner.name} type={type} dur={durationTurns} chance={chance:0.###} force100={forceChance100} rolled={rolledValue} maxFace={maxFaceValue}", owner);

        if (chance <= 0f) return;

        if (chance >= 1f || Random.value <= chance)
        {
            owner.SetAilmentState(type, Mathf.Max(1, durationTurns));

            if (debugLog)
                Debug.Log($"[STATUS] APPLY ailment -> target={owner.name} type={type} turns={Mathf.Max(1, durationTurns)}", owner);
        }
        else if (debugLog)
        {
            Debug.Log($"[STATUS] FAIL ailment roll -> target={owner.name} type={type}", owner);
        }
    }
}

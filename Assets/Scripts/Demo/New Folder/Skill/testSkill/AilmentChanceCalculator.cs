// AilmentChanceCalculator.cs
using UnityEngine;

public static class AilmentChanceCalculator
{
    /// <summary>
    /// Returns final chance in [0..1].
    /// Rule:
    /// - Enemy -> Player: always 0.65 (fixed, no dice scaling)
    /// - Player -> others: 0.50 + (roll - ceil(maxFace/2))*0.05, clamp 0.10..0.90
    /// - Optional skillTuningMultiplier: multiply after base rule, then clamp 0..1
    /// </summary>
    public static float ComputeChance(
        bool attackerIsPlayer,
        bool targetIsPlayer,
        int rolledValue,
        int maxFaceValue,
        float skillTuningMultiplier = 1f)
    {
        float chance;

        if (!attackerIsPlayer && targetIsPlayer)
        {
            chance = 0.65f; // FIXED
        }
        else
        {
            int mid = Mathf.CeilToInt(maxFaceValue / 2f);
            int delta = rolledValue - mid;
            float bonus = delta * 0.05f;
            chance = Mathf.Clamp(0.50f + bonus, 0.10f, 0.90f);
        }

        chance *= skillTuningMultiplier;
        return Mathf.Clamp01(chance);
    }
}

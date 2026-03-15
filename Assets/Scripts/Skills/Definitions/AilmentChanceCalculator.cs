// AilmentChanceCalculator.cs
using UnityEngine;

public static class AilmentChanceCalculator
{
    /// <summary>
    /// Returns final chance in [0..1].
    /// Rule:
    /// - Player -> Enemy: base 0.50
    /// - Enemy -> Player: always 1.00
    /// - otherwise final = base + (roll - ceil(maxFace/2))*0.05, clamp 0.10..0.90
    /// - Optional skillTuningMultiplier: multiply after base rule, then clamp 0..1
    /// </summary>
    public static float ComputeChance(
        bool attackerIsPlayer,
        bool targetIsPlayer,
        int rolledValue,
        int maxFaceValue,
        float skillTuningMultiplier = 1f)
    {
        if (!attackerIsPlayer && targetIsPlayer)
            return 1f;

        float baseChance = 0.50f;
        int mid = Mathf.CeilToInt(maxFaceValue / 2f);
        int delta = rolledValue - mid;
        float bonus = delta * 0.05f;
        float chance = Mathf.Clamp(baseChance + bonus, 0.10f, 0.90f);

        chance *= skillTuningMultiplier;
        return Mathf.Clamp01(chance);
    }
}

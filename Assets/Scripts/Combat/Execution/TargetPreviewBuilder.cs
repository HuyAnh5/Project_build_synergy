using UnityEngine;

/// <summary>
/// Builds a TargetPreviewData snapshot from a SkillRuntime + caster + target.
/// Simulates damage resolution WITHOUT mutating any real combat state.
/// </summary>
public static class TargetPreviewBuilder
{
    /// <summary>
    /// Build preview for an Attack/Guard/BuffDebuff skill aimed at a specific target.
    /// dieValue = resolved contribution that will be used by the skill.
    /// </summary>
    public static TargetPreviewData Build(SkillRuntime rt, CombatActor caster, CombatActor target, int dieValue)
    {
        TargetPreviewData data = default;
        if (rt == null || target == null)
            return data;

        data.currentHp = target.hp;
        data.currentMaxHp = target.maxHP;
        data.currentGuard = target.guardPool;
        data.currentlyStaggered = target.status != null && target.status.staggered;

        // Default: no change
        data.previewHpAfter = target.hp;
        data.previewGuardAfter = target.guardPool;

        // Status defaults from current
        int initialBurn = target.status != null ? target.status.burnStacks : 0;
        int initialBleed = target.status != null ? target.status.bleedStacks : 0;
        bool initialMark = target.status != null && target.status.marked;
        bool initialFreeze = target.status != null && target.status.frozen;

        data.currentBurn = initialBurn;
        data.currentBleed = initialBleed;
        data.currentMarked = initialMark;
        data.currentFrozen = initialFreeze;

        data.previewBurnAfter = initialBurn;
        data.previewBleedAfter = initialBleed;
        data.previewMarkedAfter = initialMark;
        data.previewFrozenAfter = initialFreeze;

        bool isSelf = (caster == target);
        data.isSelfTarget = isSelf;

        // --- Guard skill (Self) ---
        if (rt.kind == SkillKind.Guard && isSelf)
        {
            int baseGuard = rt.CalculateGuard(dieValue);
            if (rt.guardValueMode == BaseEffectValueMode.Flat && rt.guardFlat > 0)
                baseGuard = SkillOutputValueUtility.AddActionAddedValue(rt.guardFlat, rt);

            // Apply passive multiplier preview
            float pct = 0f;
            PassiveSystem ps = caster.GetComponent<PassiveSystem>();
            if (ps != null) pct = ps.GetGuardGainPercent();
            float mult = 1f + Mathf.Max(-0.99f, pct);
            int scaledGuard = Mathf.FloorToInt(baseGuard * mult);

            data.selfGuardGain = scaledGuard;
            data.previewGuardAfter = target.guardPool + scaledGuard;
            data.valid = true;
            return data;
        }

        // --- Attack skill ---
        if (rt.kind == SkillKind.Attack)
        {
            // Use the exact same calculator the game uses for execution
            SkillExecutor.AttackPreview ap = AttackPreviewCalculator.BuildAttackPreview(rt, caster, target, dieValue);
            int totalDamage = ap.finalDamage;

            // Simulate TakeDamageDetailed without mutation
            int guardBefore = target.guardPool;
            int hpBefore = target.hp;
            int remaining = Mathf.Max(0, totalDamage);
            int guardAfter = guardBefore;
            int hpAfter = hpBefore;
            bool guardBroken = false;

            if (!rt.bypassGuard && guardBefore > 0)
            {
                int blocked = Mathf.Min(guardBefore, remaining);
                guardAfter = guardBefore - blocked;
                remaining -= blocked;
                guardBroken = (guardBefore > 0 && guardAfter <= 0 && blocked > 0);
            }

            if (rt.bypassGuard)
                guardAfter = guardBefore; // Guard untouched

            if (rt.clearsGuard)
                guardAfter = 0;

            if (remaining > 0)
                hpAfter = Mathf.Max(0, hpBefore - remaining);

            data.previewHpAfter = hpAfter;
            data.previewGuardAfter = guardAfter;
            data.hpLost = hpBefore - hpAfter;
            data.guardLost = guardBefore - guardAfter;
            data.willBreakGuard = guardBroken;
            data.willConsumeStagger = ap.consumesStagger;

            // --- Status changes from the skill ---
            // Burn
            if (rt.consumesBurn && target.status != null)
            {
                // Burn sẽ bị consume hết
                data.previewBurnAfter = 0;
            }

            if (rt.applyBurn)
            {
                int burnToAdd = GetBurnStacksToApply(rt, dieValue);
                data.previewBurnAfter += burnToAdd;
            }

            // Mark
            if (rt.applyMark)
                data.previewMarkedAfter = true;
            // Mark consumed by non-lightning hit
            if (AttackPreviewCalculator.CanUseMarkPayoff(rt, target) && rt.element != ElementType.Lightning)
                data.previewMarkedAfter = false;

            // Bleed
            if (rt.applyBleed)
                data.previewBleedAfter += Mathf.Max(1, rt.bleedTurns);

            // Freeze
            if (rt.applyFreeze)
            {
                bool canFreeze = target.status == null || (!target.status.frozen && target.status.chilledTurns <= 0);
                if (canFreeze)
                    data.previewFrozenAfter = true;
            }

            data.valid = true;
            return data;
        }

        // --- BuffDebuff / Utility skill (non-damage) ---
        if (rt.kind == SkillKind.Utility)
        {
            int healAmount = 0;
            if (rt.sourceAsset is SkillBuffDebuffSO buffDebuffAsset)
            {
                if (buffDebuffAsset.effects != null)
                {
                    for (int i = 0; i < buffDebuffAsset.effects.Count; i++)
                    {
                        BuffDebuffEffectEntry effect = buffDebuffAsset.effects[i];
                        if (effect == null)
                            continue;

                        if (effect.id == BuffDebuffEffectId.HealFlat)
                            healAmount += Mathf.Max(0, effect.GetHealAmount());
                        else if (effect.id == BuffDebuffEffectId.HealByDiceSum)
                            healAmount += Mathf.Max(0, dieValue);
                    }
                }
            }

            if (healAmount > 0)
            {
                int hpAfter = Mathf.Min(data.currentMaxHp, data.currentHp + healAmount);
                data.previewHpAfter = hpAfter;
                // hpLost âm = hồi máu
                data.hpLost = data.currentHp - hpAfter;
            }

            data.valid = true;
            return data;
        }

        data.valid = true;
        return data;
    }

    private static int GetBurnStacksToApply(SkillRuntime rt, int dieValue)
    {
        if (!rt.applyBurn)
            return 0;

        if (rt.baseBurnValueMode == BaseEffectValueMode.X)
            return Mathf.Max(0, dieValue);

        return Mathf.Max(1, rt.burnAddStacks);
    }
}

using System.Collections.Generic;
using UnityEngine;

// Shared non-mutating preview helpers for target selection, damage math, and self rewards.
public static partial class TargetPreviewBuilder
{
    // Captures a target's current combat state before preview deltas are applied.
    private static TargetPreviewData CreateBaselineData(CombatActor caster, CombatActor target)
    {
        TargetPreviewData data = default;
        if (target == null)
            return data;

        data.valid = true;
        data.currentHp = target.hp;
        data.currentMaxHp = target.maxHP;
        data.currentGuard = target.guardPool;
        data.currentlyStaggered = target.status != null && target.status.staggered;
        data.previewHpAfter = target.hp;
        data.previewGuardAfter = target.guardPool;

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

        data.isSelfTarget = caster == target;
        return data;
    }

    // Builds preview deltas for utility skills such as heals.
    private static void BuildUtilityPreview(SkillRuntime rt, int dieValue, ref TargetPreviewData data)
    {
        // Buff/debuff flow effects currently affect cast flow, dice, or owner state.
        // They do not mutate target HP in the shared target preview.
    }

    // Resolves all targets affected by the action, including AoE fallback behavior.
    private static List<CombatActor> ResolveActionTargets(
        SkillRuntime rt,
        CombatActor caster,
        CombatActor clickedTarget,
        BattlePartyManager2D party,
        CombatActor fallbackEnemy)
    {
        var list = new List<CombatActor>();
        if (rt == null)
            return list;

        IReadOnlyList<CombatActor> aoeTargets = TurnManagerCombatUtility.ResolveAoeTargets(rt, caster, clickedTarget, party, fallbackEnemy);
        if (aoeTargets != null && aoeTargets.Count > 0)
        {
            for (int i = 0; i < aoeTargets.Count; i++)
            {
                CombatActor target = aoeTargets[i];
                if (target != null && !target.IsDead)
                    list.Add(target);
            }
        }
        else if (clickedTarget != null && !clickedTarget.IsDead)
        {
            list.Add(clickedTarget);
        }

        return list;
    }

    // Applies legacy lightning Mark shock damage to the board preview.
    private static void ApplyLightningShockBoardPreview(
        SkillRuntime rt,
        CombatActor caster,
        BattlePartyManager2D party,
        CombatActor fallbackEnemy,
        int shockDamagePerProc,
        int shockProcCount,
        ref ActionPreviewBundle bundle)
    {
        IReadOnlyList<CombatActor> shockTargets = caster.team == CombatActor.TeamSide.Enemy
            ? TurnManagerCombatUtility.ResolveAliveAlliesSnapshot(party, caster)
            : TurnManagerCombatUtility.ResolveAliveEnemiesSnapshot(party, fallbackEnemy);

        if (shockTargets == null)
            return;

        for (int i = 0; i < shockTargets.Count; i++)
        {
            CombatActor target = shockTargets[i];
            if (target == null || target.IsDead || target == caster || target.team == caster.team)
                continue;

            TargetPreviewData data = bundle.targetPreviews.TryGetValue(target, out TargetPreviewData existing)
                ? existing
                : CreateBaselineData(caster, target);

            for (int proc = 0; proc < shockProcCount; proc++)
                ApplyDamageToData(target, ref data, shockDamagePerProc, bypassGuard: false, clearsGuard: false, canBreakGuard: false, canConsumeStagger: false);

            bundle.targetPreviews[target] = data;
            bundle.valid |= data.valid;
        }
    }

    // Adds accumulated Guard gain onto the caster preview.
    private static void AddCasterGuardPreview(CombatActor caster, int totalGuardGain, ref ActionPreviewBundle bundle)
    {
        if (caster == null || totalGuardGain <= 0)
            return;

        TargetPreviewData casterPreview = bundle.targetPreviews.TryGetValue(caster, out TargetPreviewData existing)
            ? existing
            : CreateBaselineData(caster, caster);

        casterPreview.isSelfTarget = true;
        casterPreview.selfGuardGain += totalGuardGain;
        casterPreview.previewGuardAfter += totalGuardGain;
        bundle.targetPreviews[caster] = casterPreview;
        bundle.valid |= casterPreview.valid;
    }

    // Adds accumulated healing onto the caster preview.
    private static void AddCasterHealPreview(CombatActor caster, int totalHealGain, ref ActionPreviewBundle bundle)
    {
        if (caster == null || totalHealGain <= 0)
            return;

        TargetPreviewData casterPreview = bundle.targetPreviews.TryGetValue(caster, out TargetPreviewData existing)
            ? existing
            : CreateBaselineData(caster, caster);

        casterPreview.isSelfTarget = true;
        casterPreview.selfHealGain += totalHealGain;
        int hpAfter = Mathf.Min(casterPreview.currentMaxHp, casterPreview.previewHpAfter + totalHealGain);
        casterPreview.previewHpAfter = hpAfter;
        casterPreview.hpLost = casterPreview.currentHp - hpAfter;
        bundle.targetPreviews[caster] = casterPreview;
        bundle.valid |= casterPreview.valid;
    }

    // Applies direct damage to preview HP/Guard without mutating the real target.
    private static void ApplyDamageToData(
        CombatActor target,
        ref TargetPreviewData data,
        int damage,
        bool bypassGuard,
        bool clearsGuard,
        bool canBreakGuard,
        bool canConsumeStagger)
    {
        if (target == null || damage <= 0)
            return;

        int guardBefore = data.previewGuardAfter;
        int hpBefore = data.previewHpAfter;
        int remaining = Mathf.Max(0, damage);
        int guardAfter = guardBefore;
        int hpAfter = hpBefore;
        bool guardBroken = false;

        if (!bypassGuard && guardBefore > 0)
        {
            int blocked = Mathf.Min(guardBefore, remaining);
            guardAfter = guardBefore - blocked;
            remaining -= blocked;
            guardBroken = canBreakGuard && guardBefore > 0 && guardAfter <= 0 && blocked > 0;
        }

        if (bypassGuard)
            guardAfter = guardBefore;

        if (clearsGuard)
            guardAfter = 0;

        if (remaining > 0)
            hpAfter = Mathf.Max(0, hpBefore - remaining);

        data.previewHpAfter = hpAfter;
        data.previewGuardAfter = guardAfter;
        data.hpLost = data.currentHp - hpAfter;
        data.guardLost = data.currentGuard - guardAfter;

        if (guardBroken)
            data.willBreakGuard = true;
        if (canConsumeStagger)
            data.willConsumeStagger = true;
    }

    private static void ApplyPassiveMeleeFollowUpPreview(
        SkillRuntime rt,
        CombatActor caster,
        CombatActor target,
        ref TargetPreviewData data)
    {
        if (rt == null || caster == null || target == null)
            return;

        PassiveSystem passiveSystem = caster.GetComponent<PassiveSystem>();
        int followUpDamage = passiveSystem != null ? passiveSystem.GetMeleeFollowUpDamage(rt) : 0;
        if (followUpDamage <= 0)
            return;

        ApplyDamageToData(
            target,
            ref data,
            followUpDamage,
            bypassGuard: false,
            clearsGuard: false,
            canBreakGuard: true,
            canConsumeStagger: false);
    }

    // Clears Guard in preview and marks the target as guard-broken.
    private static void ApplyClearGuardToData(ref TargetPreviewData data)
    {
        int guardBefore = Mathf.Max(0, data.previewGuardAfter);
        if (guardBefore <= 0)
            return;

        data.previewGuardAfter = 0;
        data.guardLost = data.currentGuard - data.previewGuardAfter;
        data.willBreakGuard = true;
    }

    // Checks whether a Lightning action should proc Mark shock.
    private static bool ShouldTriggerLightningShock(SkillRuntime rt, CombatActor target)
    {
        return rt != null &&
               rt.element == ElementType.Lightning &&
               rt.triggerLightningMarkShock &&
               AttackPreviewCalculator.CanUseMarkPayoff(rt, target);
    }

    // Calculates per-proc Lightning shock damage including passive modifiers.
    private static int GetLightningShockDamagePerProc(SkillRuntime rt, CombatActor caster)
    {
        if (rt == null)
            return 0;

        PassiveSystem ps = caster != null ? caster.GetComponent<PassiveSystem>() : null;
        float shockMul = 1f;
        if (ps != null)
            shockMul += Mathf.Max(0f, ps.GetLightningVsMarkMultiplierAdd());

        int baseShockDamage = Mathf.Max(0, rt.lightningMarkShockDamage);
        return Mathf.FloorToInt(baseShockDamage * shockMul);
    }
}

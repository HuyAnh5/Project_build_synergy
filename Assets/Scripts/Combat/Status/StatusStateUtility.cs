using System.Collections.Generic;
using UnityEngine;

internal static class StatusStateUtility
{
    public static void ClearAll(
        StatusController owner,
        List<StatusPendingBuffDebuff> pending,
        List<StatusActiveBuffDebuff> active,
        List<StatusPendingAilment> pendingAilments,
        bool debugLog)
    {
        owner.SyncBurnDisplay(0, 0);
        owner.GetBurnBatches().Clear();
        owner.marked = false;
        owner.bleedStacks = 0;
        owner.chilledTurns = 0;
        owner.frozen = false;
        owner.staggered = false;
        owner.emberWeaponTurns = 0;
        owner.emberWeaponBonusDamage = 1;
        owner.emberWeaponBurnEqualsDamage = true;
        owner.cinderbrandTurns = 0;
        owner.cinderbrandBonusPerBurn = 1;

        pending.Clear();
        active.Clear();
        pendingAilments.Clear();
        owner.SetAilmentCleared();
        owner.SetChilledJustApplied(false);

        if (debugLog)
            Debug.Log($"[STATUS] ClearAll -> {owner.name}", owner);
    }

    public static void OnOwnerTurnEnded(
        StatusController owner,
        List<StatusActiveBuffDebuff> active)
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            StatusActiveBuffDebuff a = active[i];
            if (a == null || a.entry == null)
            {
                active.RemoveAt(i);
                continue;
            }

            a.remainingTurns--;
            if (a.remainingTurns <= 0)
                active.RemoveAt(i);
        }

        owner.TickAilmentDuration();

        if (owner.emberWeaponTurns > 0)
            owner.emberWeaponTurns = Mathf.Max(0, owner.emberWeaponTurns - 1);
        if (owner.cinderbrandTurns > 0)
            owner.cinderbrandTurns = Mathf.Max(0, owner.cinderbrandTurns - 1);

        if (owner.bleedStacks > 0)
            owner.bleedStacks = Mathf.Max(0, owner.bleedStacks - 1);

        if (owner.GetChilledJustApplied())
        {
            owner.SetChilledJustApplied(false);
        }
        else if (owner.chilledTurns > 0)
        {
            owner.chilledTurns = Mathf.Max(0, owner.chilledTurns - 1);
        }
    }

    public static void ApplyBurn(StatusController owner, int addStacks, int refreshTurns)
    {
        if (owner == null)
            return;

        int stacksToAdd = Mathf.Max(0, addStacks);
        if (stacksToAdd <= 0)
            return;

        int turns = Mathf.Max(0, refreshTurns);
        if (turns <= 0)
            return;

        owner.GetBurnBatches().Add(new StatusController.BurnBatchState
        {
            stacks = stacksToAdd,
            turnsRemaining = turns
        });

        SyncBurnAggregates(owner);
    }

    public static int ConsumeAllBurn(StatusController owner)
    {
        if (owner == null)
            return 0;

        int total = Mathf.Max(0, owner.burnStacks);
        owner.GetBurnBatches().Clear();
        owner.SyncBurnDisplay(0, 0);
        return total;
    }

    public static void ApplyBleed(StatusController owner, int stacks)
    {
        if (stacks <= 0)
            return;
        owner.bleedStacks += stacks;
    }

    public static void ApplyFreeze(StatusController owner)
    {
        if (owner.frozen || owner.chilledTurns > 0)
            return;
        owner.frozen = true;
        owner.chilledTurns = 0;
    }

    public static int TickStartOfTurnDamage(StatusController owner)
    {
        int dot = 0;

        if (owner.bleedStacks > 0)
            dot += owner.bleedStacks;

        List<StatusController.BurnBatchState> burnBatches = owner.GetBurnBatches();
        for (int i = burnBatches.Count - 1; i >= 0; i--)
        {
            StatusController.BurnBatchState batch = burnBatches[i];
            if (batch == null)
            {
                burnBatches.RemoveAt(i);
                continue;
            }

            batch.turnsRemaining -= 1;
            if (batch.turnsRemaining <= 0 || batch.stacks <= 0)
                burnBatches.RemoveAt(i);
        }

        SyncBurnAggregates(owner);

        return dot;
    }

    public static int OnTurnStarted(StatusController owner, bool consumeFreezeToSkipTurn, out bool skipTurn)
    {
        skipTurn = false;

        int dot = TickStartOfTurnDamage(owner);

        if (consumeFreezeToSkipTurn && owner.frozen)
        {
            owner.frozen = false;
            skipTurn = true;
            owner.chilledTurns = 2;
            owner.SetChilledJustApplied(true);
        }

        return dot;
    }

    public static int OnHitByDamageReturnFocusReward(StatusController owner, ref DamageInfo info)
    {
        if (owner.HasAilment(out AilmentType ailment, out int turnsLeft) && turnsLeft > 0)
        {
            if (ailment == AilmentType.Sleep)
                owner.ClearAilment();
        }

        if (info.group == DamageGroup.Effect)
            return 0;

        return 0;
    }

    public static bool TryApplyFreeze(StatusController owner, float chance01)
    {
        if (owner.frozen || owner.chilledTurns > 0 || chance01 <= 0f)
            return false;

        if (chance01 >= 1f)
        {
            ApplyFreeze(owner);
            return true;
        }

        if (Random.value < chance01)
        {
            ApplyFreeze(owner);
            return true;
        }

        return false;
    }

    public static bool TryApplyFreeze(StatusController owner, int chancePercent)
    {
        if (owner.frozen || owner.chilledTurns > 0 || chancePercent <= 0)
            return false;

        if (chancePercent >= 100)
        {
            ApplyFreeze(owner);
            return true;
        }

        if (Random.Range(0, 100) < chancePercent)
        {
            ApplyFreeze(owner);
            return true;
        }

        return false;
    }

    public static void ProcessPendingAtTurnStart(
        StatusController owner,
        List<StatusPendingBuffDebuff> pending,
        List<StatusActiveBuffDebuff> active,
        List<StatusPendingAilment> pendingAilments,
        bool debugForceAilmentChance100,
        bool debugLog)
    {
        for (int i = pending.Count - 1; i >= 0; i--)
        {
            StatusPendingBuffDebuff p = pending[i];
            if (p == null || p.entry == null)
            {
                pending.RemoveAt(i);
                continue;
            }

            p.delayTurns--;
            if (p.delayTurns <= 0)
            {
                StatusBuffDebuffUtility.ApplyBuffDebuffEntryNow(owner, active, p.entry, p.applier, p.rolledValue);
                pending.RemoveAt(i);
            }
        }

        for (int i = pendingAilments.Count - 1; i >= 0; i--)
        {
            StatusPendingAilment p = pendingAilments[i];
            if (p == null)
            {
                pendingAilments.RemoveAt(i);
                continue;
            }

            p.delayTurns--;
            if (p.delayTurns <= 0)
            {
                StatusAilmentUtility.TryApplyAilment(owner, p.type, p.durationTurns, p.applier, p.rolledValue, p.maxFaceValue, p.chanceMultiplier, debugForceAilmentChance100, debugLog);
                pendingAilments.RemoveAt(i);
            }
        }
    }

    private static void SyncBurnAggregates(StatusController owner)
    {
        if (owner == null)
            return;

        List<StatusController.BurnBatchState> burnBatches = owner.GetBurnBatches();
        int totalStacks = 0;
        int maxTurns = 0;

        for (int i = burnBatches.Count - 1; i >= 0; i--)
        {
            StatusController.BurnBatchState batch = burnBatches[i];
            if (batch == null || batch.stacks <= 0 || batch.turnsRemaining <= 0)
            {
                burnBatches.RemoveAt(i);
                continue;
            }

            totalStacks += batch.stacks;
            if (batch.turnsRemaining > maxTurns)
                maxTurns = batch.turnsRemaining;
        }

        owner.SyncBurnDisplay(totalStacks, maxTurns);
    }
}

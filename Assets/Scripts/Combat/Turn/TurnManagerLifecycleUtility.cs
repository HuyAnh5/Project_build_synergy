using UnityEngine;

internal static class TurnManagerLifecycleUtility
{
    public static void EndPlayerTurnTickStatusesAndPassives(CombatActor player, bool logPhase, Object context)
    {
        if (player == null || player.IsDead)
        {
            TurnManagerCombatUtility.ClearAllStagger();
            return;
        }

        var ps = player.GetComponent<PassiveSystem>();
        if (ps != null)
        {
            int flat = ps.GetGuardFlatAtTurnEnd();
            if (flat != 0)
            {
                float mult = 1f + Mathf.Max(-0.99f, ps.GetGuardGainPercent());
                int scaled = Mathf.CeilToInt(flat * mult);
                if (scaled != 0)
                {
                    player.AddGuard(scaled);
                    if (logPhase)
                        Debug.Log($"[TM] Passive GuardFlatAtTurnEnd +{flat} (x{mult:0.##} => +{scaled}) -> guard={player.guardPool}", context);
                }
            }
        }

        if (player.status != null)
            player.status.OnOwnerTurnEnded();

        TurnManagerCombatUtility.ClearAllStagger();
    }

    public static void BeginPlayerTurnStatusesAndFocus(CombatActor player, bool logPhase, Object context)
    {
        if (player != null && player.status != null)
        {
            bool unusedSkip;
            int dot = player.status.OnTurnStarted(consumeFreezeToSkipTurn: false, out unusedSkip);
            if (logPhase)
                Debug.Log($"[TM] PlayerTurnStart dot={dot} focusBefore={player.focus} diceDelta={player.status.GetAllDiceDelta()} ailment={(player.status != null && player.status.HasAilment(out var at, out _) ? at.ToString() : "None")}", context);

            if (dot > 0)
                player.TakeDamage(dot, bypassGuard: true);
        }

        if (player == null || player.IsDead)
            return;

        var ps = player.GetComponent<PassiveSystem>();
        if (ps != null)
        {
            int bonus = ps.GetFocusBonusOnTurnStart();
            if (bonus != 0)
            {
                player.GainFocus(bonus);
                if (logPhase)
                    Debug.Log($"[TM] Passive FocusBonusOnTurnStart +{bonus} -> focus={player.focus}/{player.maxFocus}", context);
            }
        }
        else if (logPhase)
        {
            Debug.Log("[TM] PassiveSystem not found on player (passives won't apply).", context);
        }

        player.GainFocus(1);
    }

    public static void RestoreBaselineSlots(DiceSlotRig diceRig, bool[] baseSlotActive, ref int slotCollapseKeepIndex)
    {
        if (diceRig == null || diceRig.slots == null || baseSlotActive == null)
            return;

        for (int i = 0; i < 3 && i < diceRig.slots.Length && i < baseSlotActive.Length; i++)
            diceRig.slots[i].active = baseSlotActive[i];

        slotCollapseKeepIndex = -1;
    }

    public static void ApplyPlayerSlotDebuffs(DiceSlotRig diceRig, CombatActor player, bool[] baseSlotActive, ref int slotCollapseKeepIndex, bool logPhase, Object context)
    {
        if (diceRig == null || baseSlotActive == null)
            return;

        for (int i = 0; i < 3 && i < diceRig.slots.Length && i < baseSlotActive.Length; i++)
            diceRig.slots[i].active = baseSlotActive[i];

        bool collapse = player != null && player.status != null && player.status.HasSlotCollapse();
        if (collapse)
            ApplySlotCollapseToRig(diceRig, player, ref slotCollapseKeepIndex, logPhase, context);

        diceRig.ApplyActiveStates();

        if (logPhase)
            Debug.Log($"[TM] SlotCollapse={(collapse ? "ON" : "off")} activeSlots={diceRig.ActiveSlotCount()}", context);
    }

    public static void ApplySlotCollapseToRig(DiceSlotRig diceRig, CombatActor player, ref int slotCollapseKeepIndex, bool logPhase, Object context)
    {
        if (diceRig == null || diceRig.slots == null || player == null || player.status == null || !player.status.HasSlotCollapse())
            return;

        int[] actives = new int[3];
        int activeCount = 0;
        for (int i = 0; i < 3 && i < diceRig.slots.Length; i++)
        {
            if (diceRig.slots[i].active)
                actives[activeCount++] = i;
        }

        if (activeCount <= 1)
            return;

        slotCollapseKeepIndex = actives[Random.Range(0, activeCount)];
        for (int k = 0; k < activeCount; k++)
        {
            int idx = actives[k];
            if (idx != slotCollapseKeepIndex)
                diceRig.slots[idx].active = false;
        }

        if (logPhase)
            Debug.Log($"[TM] SlotCollapse ON -> keep slot {slotCollapseKeepIndex}", context);
    }
}

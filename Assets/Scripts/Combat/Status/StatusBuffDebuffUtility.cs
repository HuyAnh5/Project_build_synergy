using System.Collections.Generic;
using UnityEngine;

public static class StatusBuffDebuffUtility
{
    public static int GetAllDiceDelta(List<StatusActiveBuffDebuff> active)
    {
        int sum = 0;
        for (int i = 0; i < active.Count; i++)
        {
            var a = active[i];
            if (a == null || a.entry == null) continue;
            if (a.entry.id == BuffDebuffEffectId.DiceAllDelta)
                sum += a.entry.GetDiceAllDelta();
        }
        return sum;
    }

    public static int GetParityFocusDelta(List<StatusActiveBuffDebuff> active, int diceTotal)
    {
        bool even = (diceTotal % 2) == 0;
        int sum = 0;
        for (int i = 0; i < active.Count; i++)
        {
            var a = active[i];
            if (a == null || a.entry == null) continue;
            if (a.entry.id != BuffDebuffEffectId.ParityFocusDelta) continue;
            sum += even ? a.entry.parityEvenDelta : a.entry.parityOddDelta;
        }
        return sum;
    }

    public static bool HasSlotCollapse(List<StatusActiveBuffDebuff> active)
    {
        for (int i = 0; i < active.Count; i++)
        {
            var a = active[i];
            if (a == null || a.entry == null) continue;
            if (a.entry.id == BuffDebuffEffectId.SlotCollapse) return true;
        }
        return false;
    }

    public static float GetOutgoingDamageMultiplier(List<StatusActiveBuffDebuff> active)
    {
        float best = 1f;
        for (int i = 0; i < active.Count; i++)
        {
            var a = active[i];
            if (a == null || a.entry == null) continue;
            if (a.entry.id != BuffDebuffEffectId.DamageMultiplier) continue;
            best = Mathf.Max(best, a.entry.GetDamageMultiplier());
        }
        return best;
    }

    public static void ApplyBuffDebuffEntryNow(StatusController owner, List<StatusActiveBuffDebuff> active, BuffDebuffEffectEntry entry, CombatActor applier, int rolledValue)
    {
        if (owner == null || entry == null) return;

        var actor = owner.GetComponent<CombatActor>();

        switch (entry.id)
        {
            case BuffDebuffEffectId.HealFlat:
                if (actor != null) actor.Heal(entry.GetHealAmount());
                break;

            case BuffDebuffEffectId.HealByDiceSum:
                if (actor != null) actor.Heal(Mathf.Max(0, rolledValue));
                break;

            case BuffDebuffEffectId.FocusDelayed:
                if (actor != null) actor.GainFocus(entry.GetFocusAmount());
                break;

            case BuffDebuffEffectId.DamageMultiplier:
            case BuffDebuffEffectId.DiceAllDelta:
            case BuffDebuffEffectId.ParityFocusDelta:
            case BuffDebuffEffectId.SlotCollapse:
                {
                    int dur = Mathf.Max(0, entry.durationTurns);
                    if (dur <= 0) break;
                    active.Add(new StatusActiveBuffDebuff { entry = entry, remainingTurns = dur, applier = applier });
                }
                break;
        }
    }
}

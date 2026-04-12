using System.Collections.Generic;
using UnityEngine;

public static class DiceCombatEnchantRuntimeUtility
{
    private sealed class WholeDieCombatState
    {
        public bool usedThisCombat;
    }

    private static readonly Dictionary<DiceSpinnerGeneric, WholeDieCombatState> WholeDieStates = new Dictionary<DiceSpinnerGeneric, WholeDieCombatState>();

    public static void BeginCombat(DiceSlotRig diceRig)
    {
        if (diceRig == null || diceRig.slots == null)
            return;

        WholeDieStates.Clear();

        for (int i = 0; i < diceRig.slots.Length; i++)
        {
            DiceSpinnerGeneric die = diceRig.slots[i] != null ? diceRig.slots[i].dice : null;
            if (die == null || WholeDieStates.ContainsKey(die))
                continue;

            WholeDieStates[die] = new WholeDieCombatState();
        }
    }

    public static void MarkDieUsedInCombat(DiceSpinnerGeneric die)
    {
        if (die == null)
            return;

        if (!WholeDieStates.TryGetValue(die, out WholeDieCombatState state))
        {
            state = new WholeDieCombatState();
            WholeDieStates[die] = state;
        }

        state.usedThisCombat = true;
    }

    public static void ResolveOnRollFaceEnchants(
        DiceSlotRig diceRig,
        CombatActor caster,
        BattlePartyManager2D party,
        CombatActor fallbackEnemy)
    {
        if (diceRig == null || diceRig.slots == null || caster == null)
            return;

        for (int i = 0; i < diceRig.slots.Length; i++)
        {
            if (!diceRig.IsSlotActive(i))
                continue;

            DiceSpinnerGeneric die = diceRig.slots[i] != null ? diceRig.slots[i].dice : null;
            if (die == null)
                continue;

            if (ResolveOnRollFaceEnchant(die, caster, party, fallbackEnemy))
                MarkDieUsedInCombat(die);
        }
    }

    public static bool ApplyVictoryWholeDieEffects(DiceSlotRig diceRig)
    {
        if (diceRig == null || diceRig.slots == null)
            return false;

        bool changed = false;
        HashSet<DiceSpinnerGeneric> visited = new HashSet<DiceSpinnerGeneric>();

        for (int i = 0; i < diceRig.slots.Length; i++)
        {
            DiceSpinnerGeneric die = diceRig.slots[i] != null ? diceRig.slots[i].dice : null;
            if (die == null || !visited.Add(die))
                continue;

            if (!WholeDieStates.TryGetValue(die, out WholeDieCombatState state) || !state.usedThisCombat)
                continue;

            switch (die.GetWholeDieTag())
            {
                case DiceWholeDieTag.Patina:
                    changed |= TryApplyPatina(die);
                    break;
            }
        }

        return changed;
    }

    private static bool ResolveOnRollFaceEnchant(
        DiceSpinnerGeneric die,
        CombatActor caster,
        BattlePartyManager2D party,
        CombatActor fallbackEnemy)
    {
        if (die == null || caster == null)
            return false;

        switch (die.GetCurrentFaceEnchant())
        {
            case DiceFaceEnchantKind.GuardBoost:
                caster.AddGuard(DiceFaceEnchantUtility.GuardBoostAmount);
                return true;

            case DiceFaceEnchantKind.GoldProc:
                return TryGrantGold(DiceFaceEnchantUtility.GoldProcAmount);

            case DiceFaceEnchantKind.Fire:
                return TryApplyRandomEnemyBurn(caster, party, fallbackEnemy);

            case DiceFaceEnchantKind.Bleed:
                return TryApplyRandomEnemyBleed(caster, party, fallbackEnemy);

            default:
                return false;
        }
    }

    private static bool TryApplyRandomEnemyBurn(CombatActor caster, BattlePartyManager2D party, CombatActor fallbackEnemy)
    {
        CombatActor target = GetRandomLivingOpponent(caster, party, fallbackEnemy);
        if (target == null || target.status == null)
            return false;

        PassiveSystem passiveSystem = caster.GetComponent<PassiveSystem>();
        int stacks = DiceFaceEnchantUtility.FireBurnStacks +
                     (passiveSystem != null ? passiveSystem.GetBonusStatusStacksApplied(StatusKind.Burn) : 0);
        target.status.ApplyBurn(stacks, DiceFaceEnchantUtility.FireBurnTurns);
        return true;
    }

    private static bool TryApplyRandomEnemyBleed(CombatActor caster, BattlePartyManager2D party, CombatActor fallbackEnemy)
    {
        CombatActor target = GetRandomLivingOpponent(caster, party, fallbackEnemy);
        if (target == null || target.status == null)
            return false;

        PassiveSystem passiveSystem = caster.GetComponent<PassiveSystem>();
        int stacks = DiceFaceEnchantUtility.BleedStacks +
                     (passiveSystem != null ? passiveSystem.GetBonusStatusStacksApplied(StatusKind.Bleed) : 0);
        target.status.ApplyBleed(stacks);
        return true;
    }

    private static CombatActor GetRandomLivingOpponent(CombatActor caster, BattlePartyManager2D party, CombatActor fallbackEnemy)
    {
        List<CombatActor> candidates = ResolveLivingOpponents(caster, party, fallbackEnemy);
        if (candidates.Count <= 0)
            return null;

        int index = Random.Range(0, candidates.Count);
        return candidates[index];
    }

    private static List<CombatActor> ResolveLivingOpponents(CombatActor caster, BattlePartyManager2D party, CombatActor fallbackEnemy)
    {
        List<CombatActor> candidates = new List<CombatActor>();

        if (party != null)
        {
            IReadOnlyList<CombatActor> actors = caster != null && caster.team == CombatActor.TeamSide.Enemy
                ? party.GetAliveAllies(includePlayer: true)
                : party.GetAliveEnemies(frontOnly: false);

            if (actors != null)
            {
                for (int i = 0; i < actors.Count; i++)
                {
                    CombatActor actor = actors[i];
                    if (actor == null || actor.IsDead)
                        continue;
                    if (caster != null && actor.team == caster.team)
                        continue;
                    candidates.Add(actor);
                }
            }

            return candidates;
        }

        if (fallbackEnemy != null && !fallbackEnemy.IsDead && (caster == null || fallbackEnemy.team != caster.team))
            candidates.Add(fallbackEnemy);

        return candidates;
    }

    private static bool TryGrantGold(int amount)
    {
        if (amount <= 0)
            return false;

        RunInventoryManager inventory = Object.FindFirstObjectByType<RunInventoryManager>(FindObjectsInactive.Include);
        if (inventory == null)
            return false;

        inventory.AddGold(amount);
        return true;
    }

    private static bool TryApplyPatina(DiceSpinnerGeneric die)
    {
        if (die == null || die.faces == null || die.faces.Length <= 0)
            return false;

        int minValue = int.MaxValue;
        int maxValue = int.MinValue;
        int minCount = 0;
        int minIndex = -1;

        for (int i = 0; i < die.faces.Length; i++)
        {
            int value = die.faces[i].value;
            if (value < minValue)
            {
                minValue = value;
                minIndex = i;
                minCount = 1;
            }
            else if (value == minValue)
            {
                minCount++;
            }

            if (value > maxValue)
                maxValue = value;
        }

        if (minIndex < 0)
            return false;
        if (minCount != 1)
            return false;
        if (minValue == maxValue)
            return false;

        return die.SetFaceValue(minIndex, minValue + 1);
    }
}

using UnityEngine;

internal static class StatusNamedSkillUtility
{
    public static bool TryApplyNamedSkillNow(StatusController owner, SkillBuffDebuffSO skill, CombatActor applier, int rolledValue)
    {
        if (owner == null || skill == null)
            return false;

        if (skill.fireModules != null)
        {
            if (skill.fireModules.grantEmberWeapon)
            {
                ApplyEmberWeapon(
                    owner,
                    skill.fireModules.emberWeaponTurns,
                    skill.fireModules.basicAttackBonusDamage,
                    skill.fireModules.basicAttackAppliesBurnEqualDamage,
                    skill.fireModules.basicAttackBurnOnCritOnly);
                return true;
            }

            if (skill.fireModules.applyConsumeBonusDebuff)
            {
                owner.cinderbrandTurns = Mathf.Max(owner.cinderbrandTurns, Mathf.Max(1, skill.fireModules.consumeBonusDebuffTurns));
                owner.cinderbrandBonusPerBurn = Mathf.Max(owner.cinderbrandBonusPerBurn, Mathf.Max(0, skill.fireModules.consumeBonusPerBurn));
                return true;
            }
        }

        if (skill.behaviorId == BuffBehaviorId.Fire_EmberWeapon)
        {
            ApplyEmberWeapon(owner, turns: 3, bonusDamage: 1, burnEqualsDamage: true, burnOnCritOnly: true);
            return true;
        }

        if (skill.behaviorId == BuffBehaviorId.Fire_Cinderbrand)
        {
            // Cinderbrand currently shares the same runtime hook as Ember Weapon:
            // Legacy fallback preserved for old assets until Fire assets are migrated.
            owner.cinderbrandTurns = Mathf.Max(owner.cinderbrandTurns, 3);
            owner.cinderbrandBonusPerBurn = Mathf.Max(owner.cinderbrandBonusPerBurn, 1);
            return true;
        }

        if (skill.behaviorId == BuffBehaviorId.Bleed_Siphon)
        {
            ApplyBleedSiphon(owner, applier);
            return true;
        }

        return false;
    }

    private static void ApplyEmberWeapon(StatusController owner, int turns, int bonusDamage, bool burnEqualsDamage, bool burnOnCritOnly)
    {
        owner.emberWeaponTurns = Mathf.Max(owner.emberWeaponTurns, Mathf.Max(1, turns));
        owner.emberWeaponBonusDamage = Mathf.Max(0, bonusDamage);
        owner.emberWeaponBurnEqualsDamage = burnEqualsDamage;
        owner.emberWeaponBurnOnCritOnly = burnEqualsDamage && burnOnCritOnly;
    }

    private static void ApplyBleedSiphon(StatusController targetStatus, CombatActor applier)
    {
        if (targetStatus == null)
            return;

        int consumed = Mathf.Max(0, targetStatus.bleedStacks);
        targetStatus.bleedStacks = 0;
        if (consumed <= 0 || applier == null)
            return;

        int createCount = Mathf.Clamp(consumed / 5, 0, 3);
        if (createCount <= 0)
            return;

        RunInventoryManager inventory = ResolveInventory(applier);
        if (inventory == null)
            return;

        ConsumableDataSO[] pool = ResolveConsumablePool(inventory);
        if (pool == null || pool.Length <= 0)
            return;

        for (int i = 0; i < createCount; i++)
        {
            ConsumableDataSO chosen = pool[Random.Range(0, pool.Length)];
            if (chosen == null)
                continue;

            inventory.TryAddConsumableToFirstEmptySlot(chosen, chosen.GetStartingCharges());
        }
    }

    private static RunInventoryManager ResolveInventory(CombatActor applier)
    {
        if (applier != null)
        {
            RunInventoryManager inventory = applier.GetComponentInParent<RunInventoryManager>();
            if (inventory != null)
                return inventory;
        }

        return Object.FindFirstObjectByType<RunInventoryManager>(FindObjectsInactive.Include);
    }

    private static ConsumableDataSO[] ResolveConsumablePool(RunInventoryManager inventory)
    {
        var unique = new System.Collections.Generic.List<ConsumableDataSO>();

        RewardGachaDemoController rewardDemo = Object.FindFirstObjectByType<RewardGachaDemoController>(FindObjectsInactive.Include);
        if (rewardDemo != null && rewardDemo.pool != null && rewardDemo.pool.consumables != null)
        {
            for (int i = 0; i < rewardDemo.pool.consumables.Count; i++)
                AddUnique(unique, rewardDemo.pool.consumables[i]);
        }

        if (inventory != null)
        {
            int count = inventory.GetConsumableCount();
            for (int i = 0; i < count; i++)
                AddUnique(unique, inventory.GetConsumable(i));
        }

        return unique.ToArray();
    }

    private static void AddUnique(System.Collections.Generic.List<ConsumableDataSO> list, ConsumableDataSO data)
    {
        if (data == null || list.Contains(data))
            return;

        list.Add(data);
    }
}

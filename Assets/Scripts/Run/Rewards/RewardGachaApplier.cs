public static class RewardGachaApplier
{
    public static bool TryApply(RewardGachaCard card, RunInventoryManager inventory, out string message)
    {
        if (card == null)
        {
            message = "No reward selected.";
            return false;
        }

        if (inventory == null)
        {
            message = "No RunInventoryManager in this demo scene. Selection was recorded only.";
            return false;
        }

        switch (card.itemKind)
        {
            case RewardGachaItemKind.Gold:
                inventory.AddGold(card.amount);
                message = "Added " + card.amount + " gold.";
                return true;

            case RewardGachaItemKind.Skill:
                int skillSlot = inventory.FindFirstEmptyOwnedSlot();
                if (skillSlot >= 0 && card.asset != null)
                {
                    inventory.SetSkill(RunInventoryManager.SkillSource.Owned, skillSlot, card.asset);
                    message = "Added skill: " + card.displayName;
                    return true;
                }
                message = "No empty skill slot for " + card.displayName + ".";
                return false;

            case RewardGachaItemKind.Passive:
                if (inventory.TryAddPassiveToFirstEmptySlot(card.asset as SkillPassiveSO))
                {
                    message = "Added passive: " + card.displayName;
                    return true;
                }
                message = "No empty passive slot for " + card.displayName + ".";
                return false;

            case RewardGachaItemKind.Consumable:
                if (inventory.TryAddConsumableToFirstEmptySlot(card.asset as ConsumableDataSO, card.amount))
                {
                    message = "Added consumable: " + card.displayName;
                    return true;
                }
                message = "No empty consumable slot for " + card.displayName + ".";
                return false;

            case RewardGachaItemKind.DiceColorOre:
                message = card.displayName + " is a Forge material reward. Forge inventory is not wired yet.";
                return false;

            default:
                message = card.displayName + " is a fallback demo reward and has no runtime apply action yet.";
                return false;
        }
    }
}

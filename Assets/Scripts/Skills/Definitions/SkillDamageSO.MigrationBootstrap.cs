public partial class SkillDamageSO
{
    private void SeedConsumeBurnGameplayDataIfEmpty()
    {
        bool isConsumeBurn = string.Equals(displayName, "Burn Consume", System.StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(displayName, "Consume Burn", System.StringComparison.OrdinalIgnoreCase) ||
                             (!string.IsNullOrEmpty(name) && name.ToLowerInvariant().Contains("consume_burn"));
        if (!isConsumeBurn)
        {
            return;
        }

        if (gameplay != null && gameplay.baseEffects != null && gameplay.baseEffects.Count > 0)
        {
            gameplay.useNewGameplayPipeline = true;
            EnsureDefaultGameplayDescription();
            return;
        }

        SeedConsumeBurnGameplayData();
    }

    private void SeedFireSlashGameplayDataIfEmpty()
    {
        bool isFireSlash = fireBehaviorId == FireDamageBehaviorId.FireSlash || string.Equals(displayName, "Fire Slash", System.StringComparison.OrdinalIgnoreCase);
        if (!isFireSlash)
        {
            return;
        }

        if (gameplay != null && gameplay.baseEffects != null && gameplay.baseEffects.Count > 0)
        {
            gameplay.useNewGameplayPipeline = true;
            EnsureDefaultGameplayDescription();
            return;
        }

        SeedFireSlashGameplayData();
    }

    private void SeedMigratedFireGameplayDataIfEmpty()
    {
        bool isIgnite = fireBehaviorId == FireDamageBehaviorId.Ignite || string.Equals(displayName, "Ignite", System.StringComparison.OrdinalIgnoreCase);
        bool isHellfire = fireBehaviorId == FireDamageBehaviorId.Hellfire || string.Equals(displayName, "Hellfire", System.StringComparison.OrdinalIgnoreCase);
        bool isBiteTheDust = fireBehaviorId == FireDamageBehaviorId.BiteTheDust || string.Equals(displayName, "Bite the Dust", System.StringComparison.OrdinalIgnoreCase);
        if (!isIgnite && !isHellfire && !isBiteTheDust)
        {
            return;
        }

        if (gameplay != null && gameplay.baseEffects != null && gameplay.baseEffects.Count > 0)
        {
            gameplay.useNewGameplayPipeline = true;
            EnsureDefaultGameplayDescription();
            if (isHellfire)
            {
                MigrateHellfireMatchSevenEffectToCondition();
            }

            return;
        }

        if (isIgnite)
        {
            SeedIgniteGameplayData();
        }
        else if (isHellfire)
        {
            SeedHellfireGameplayData();
        }
        else if (isBiteTheDust)
        {
            SeedBiteTheDustGameplayData();
        }
    }
}

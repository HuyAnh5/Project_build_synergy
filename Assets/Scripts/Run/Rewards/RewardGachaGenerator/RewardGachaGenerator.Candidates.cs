using System.Collections.Generic;
using UnityEngine;

// Builds asset-backed reward candidates before the generator falls back to prototype names.
public static partial class RewardGachaGenerator
{
    // Picks one candidate for a purpose/rarity pair, falling back to economy if needed.
    private static RewardCandidate PickCandidate(
        RewardGachaPoolSource pool,
        RewardGachaPurpose purpose,
        RewardGachaRarity rarity,
        HashSet<string> usedNames)
    {
        List<RewardCandidate> candidates = GetAssetCandidates(pool, purpose, rarity, usedNames);
        if (candidates.Count == 0)
            candidates = GetFallbackCandidates(purpose, rarity);

        if (candidates.Count == 0)
            candidates = GetFallbackCandidates(RewardGachaPurpose.Economy, RewardGachaRarity.Common);

        RewardCandidate candidate = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        if (candidate.itemKind == RewardGachaItemKind.Gold && candidate.amount <= 0)
            candidate.amount = UnityEngine.Random.Range(10, 16);

        return candidate;
    }

    // Gathers candidates from authored project assets for the requested reward lane.
    private static List<RewardCandidate> GetAssetCandidates(
        RewardGachaPoolSource pool,
        RewardGachaPurpose purpose,
        RewardGachaRarity rarity,
        HashSet<string> usedNames)
    {
        List<RewardCandidate> result = new List<RewardCandidate>();
        if (pool == null)
            return result;

        if (purpose == RewardGachaPurpose.Skill)
            AddSkillCandidates(pool, rarity, usedNames, result);
        else if (purpose == RewardGachaPurpose.Passive)
            AddPassiveCandidates(pool, rarity, usedNames, result);
        else if (purpose == RewardGachaPurpose.EditDice)
            AddEditDiceCandidates(pool, rarity, usedNames, result);
        else if (purpose == RewardGachaPurpose.CombatAid)
            AddConsumableCandidates(pool, ConsumableFamily.Seal, rarity, RewardGachaPurpose.CombatAid, usedNames, result);
        else if (purpose == RewardGachaPurpose.UtilitySupport)
            AddConsumableCandidates(pool, ConsumableFamily.Rune, rarity, RewardGachaPurpose.UtilitySupport, usedNames, result);

        return result;
    }

    // Adds active skill assets that match the rolled rarity.
    private static void AddSkillCandidates(RewardGachaPoolSource pool, RewardGachaRarity rarity, HashSet<string> usedNames, List<RewardCandidate> result)
    {
        if (pool.skillDatabase == null)
            return;

        foreach (ScriptableObject asset in pool.skillDatabase.EnumerateActiveV2())
        {
            if (asset == null || MapContentRarity(GetSkillRarity(asset)) != rarity)
                continue;

            string displayName = GetSkillName(asset);
            if (IsUsed(usedNames, RewardGachaPurpose.Skill, displayName))
                continue;

            result.Add(new RewardCandidate
            {
                displayName = displayName,
                description = GetSkillDescription(asset),
                purpose = RewardGachaPurpose.Skill,
                rarity = rarity,
                itemKind = RewardGachaItemKind.Skill,
                asset = asset
            });
        }
    }

    // Adds passive assets that match the rolled rarity.
    private static void AddPassiveCandidates(RewardGachaPoolSource pool, RewardGachaRarity rarity, HashSet<string> usedNames, List<RewardCandidate> result)
    {
        if (pool.skillDatabase == null)
            return;

        for (int i = 0; i < pool.skillDatabase.passiveSkills.Count; i++)
        {
            SkillPassiveSO passive = pool.skillDatabase.passiveSkills[i];
            if (passive == null || MapContentRarity(passive.spec != null ? passive.spec.rarity : ContentRarity.Pending) != rarity)
                continue;

            string displayName = string.IsNullOrWhiteSpace(passive.displayName) ? passive.name : passive.displayName;
            if (IsUsed(usedNames, RewardGachaPurpose.Passive, displayName))
                continue;

            result.Add(new RewardCandidate
            {
                displayName = displayName,
                description = passive.description,
                purpose = RewardGachaPurpose.Passive,
                rarity = rarity,
                itemKind = RewardGachaItemKind.Passive,
                asset = passive
            });
        }
    }

    // Adds Zodiac consumables and rare whole-die ore rewards for dice editing.
    private static void AddEditDiceCandidates(RewardGachaPoolSource pool, RewardGachaRarity rarity, HashSet<string> usedNames, List<RewardCandidate> result)
    {
        AddConsumableCandidates(pool, ConsumableFamily.Zodiac, rarity, RewardGachaPurpose.EditDice, usedNames, result);

        if ((rarity != RewardGachaRarity.Rare && rarity != RewardGachaRarity.Special) || pool.diceColorOres == null)
            return;

        for (int i = 0; i < pool.diceColorOres.Count; i++)
        {
            DiceColorOreSO ore = pool.diceColorOres[i];
            if (ore == null)
                continue;

            RewardGachaRarity oreRarity = MapContentRarity(ore.rarity);
            if (oreRarity != rarity && !(rarity == RewardGachaRarity.Special && oreRarity == RewardGachaRarity.Rare))
                continue;

            string displayName = ore.GetDisplayName();
            if (IsUsed(usedNames, RewardGachaPurpose.EditDice, displayName))
                continue;

            result.Add(new RewardCandidate
            {
                displayName = displayName,
                description = ore.description,
                purpose = RewardGachaPurpose.EditDice,
                rarity = rarity,
                itemKind = RewardGachaItemKind.DiceColorOre,
                asset = ore
            });
        }
    }

    // Adds consumables from the requested family.
    private static void AddConsumableCandidates(
        RewardGachaPoolSource pool,
        ConsumableFamily family,
        RewardGachaRarity rarity,
        RewardGachaPurpose purpose,
        HashSet<string> usedNames,
        List<RewardCandidate> result)
    {
        if (rarity != RewardGachaRarity.Uncommon || pool.consumables == null)
            return;

        for (int i = 0; i < pool.consumables.Count; i++)
        {
            ConsumableDataSO data = pool.consumables[i];
            if (data == null || data.family != family)
                continue;

            string displayName = string.IsNullOrWhiteSpace(data.displayName) ? data.name : data.displayName;
            if (IsUsed(usedNames, purpose, displayName))
                continue;

            result.Add(new RewardCandidate
            {
                displayName = displayName,
                description = data.description,
                purpose = purpose,
                rarity = rarity,
                itemKind = RewardGachaItemKind.Consumable,
                asset = data,
                amount = data.GetStartingCharges()
            });
        }
    }

    // Maps content rarity metadata to gacha rarity.
    private static RewardGachaRarity MapContentRarity(ContentRarity rarity)
    {
        switch (rarity)
        {
            case ContentRarity.Uncommon:
                return RewardGachaRarity.Uncommon;
            case ContentRarity.Rare:
                return RewardGachaRarity.Rare;
            case ContentRarity.Common:
                return RewardGachaRarity.Common;
            default:
                return RewardGachaRarity.Common;
        }
    }

    // Checks duplicate protection within one rolled offer.
    private static bool IsUsed(HashSet<string> usedNames, RewardGachaPurpose purpose, string displayName)
    {
        return usedNames != null && usedNames.Contains(MakeUsedKey(purpose, displayName));
    }
}

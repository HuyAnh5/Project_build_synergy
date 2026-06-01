using System;
using System.Collections.Generic;
using UnityEngine;

// Rolls reward offers and owns public reward gacha labels/colors.
public static partial class RewardGachaGenerator
{
    private sealed class RewardCandidate
    {
        public string displayName;
        public string description;
        public RewardGachaPurpose purpose;
        public RewardGachaRarity rarity;
        public RewardGachaItemKind itemKind;
        public ScriptableObject asset;
        public int amount;
    }

    public static RewardGachaOffer RollOffer(RewardGachaEncounterMode mode, RewardGachaPoolSource pool)
    {
        RewardGachaModeConfig config = GetDefaultConfig(mode);
        RewardGachaOffer offer = new RewardGachaOffer
        {
            mode = mode,
            baseGold = UnityEngine.Random.Range(config.goldMin, config.goldMax + 1),
            picksAllowed = Mathf.Max(1, config.picks)
        };

        Dictionary<RewardGachaPurpose, int> purposeCounts = new Dictionary<RewardGachaPurpose, int>();
        HashSet<string> usedNames = new HashSet<string>();
        int count = Mathf.Max(1, config.choices);
        for (int i = 0; i < count; i++)
            offer.cards.Add(RollCard(config, pool, purposeCounts, usedNames, null));

        ApplyHighRarityGuarantee(offer, config, pool, usedNames);
        return offer;
    }

    public static RewardGachaModeConfig GetDefaultConfig(RewardGachaEncounterMode mode)
    {
        switch (mode)
        {
            case RewardGachaEncounterMode.Elite:
                return RewardGachaModeConfig.EliteDefault();
            case RewardGachaEncounterMode.Boss:
                return RewardGachaModeConfig.BossDefault();
            default:
                return RewardGachaModeConfig.CombatDefault();
        }
    }

    public static Color GetRarityColor(RewardGachaRarity rarity)
    {
        switch (rarity)
        {
            case RewardGachaRarity.Uncommon:
                return new Color32(82, 190, 137, 255);
            case RewardGachaRarity.Rare:
                return new Color32(216, 170, 74, 255);
            case RewardGachaRarity.Special:
                return new Color32(207, 82, 88, 255);
            default:
                return new Color32(152, 163, 178, 255);
        }
    }

    public static string GetRarityLabel(RewardGachaRarity rarity)
    {
        switch (rarity)
        {
            case RewardGachaRarity.Uncommon:
                return "Uncommon";
            case RewardGachaRarity.Rare:
                return "Rare";
            case RewardGachaRarity.Special:
                return "Special";
            default:
                return "Common";
        }
    }

    public static string GetPurposeLabel(RewardGachaPurpose purpose)
    {
        switch (purpose)
        {
            case RewardGachaPurpose.Skill:
                return "Skill";
            case RewardGachaPurpose.Passive:
                return "Passive";
            case RewardGachaPurpose.EditDice:
                return "Edit Dice";
            case RewardGachaPurpose.CombatAid:
                return "Combat Aid";
            case RewardGachaPurpose.UtilitySupport:
                return "Utility Support";
            default:
                return "Economy";
        }
    }

    private static RewardGachaCard RollCard(
        RewardGachaModeConfig config,
        RewardGachaPoolSource pool,
        Dictionary<RewardGachaPurpose, int> purposeCounts,
        HashSet<string> usedNames,
        RewardGachaRarity? forcedRarity)
    {
        RewardGachaRarity rarity = forcedRarity ?? RollRarity(config.rates);
        List<RewardGachaPurpose> purposes = GetEligiblePurposes(pool, rarity, purposeCounts);
        int safety = 0;

        while (!forcedRarity.HasValue && purposes.Count == 0 && safety < 40)
        {
            rarity = RollRarity(config.rates);
            purposes = GetEligiblePurposes(pool, rarity, purposeCounts);
            safety++;
        }

        if (purposes.Count == 0)
        {
            rarity = RewardGachaRarity.Rare;
            purposes = GetEligiblePurposes(pool, rarity, purposeCounts);
        }

        if (purposes.Count == 0)
        {
            rarity = RewardGachaRarity.Uncommon;
            purposes = GetEligiblePurposes(pool, rarity, purposeCounts);
        }

        if (purposes.Count == 0)
            purposes.Add(RewardGachaPurpose.Economy);

        RewardGachaPurpose purpose = purposes[UnityEngine.Random.Range(0, purposes.Count)];
        purposeCounts[purpose] = purposeCounts.TryGetValue(purpose, out int count) ? count + 1 : 1;

        RewardCandidate candidate = PickCandidate(pool, purpose, rarity, usedNames);
        usedNames.Add(MakeUsedKey(candidate.purpose, candidate.displayName));

        return new RewardGachaCard
        {
            id = Guid.NewGuid().ToString("N"),
            displayName = candidate.displayName,
            description = candidate.description,
            purpose = candidate.purpose,
            rarity = candidate.rarity,
            itemKind = candidate.itemKind,
            asset = candidate.asset,
            amount = candidate.amount
        };
    }

    private static void ApplyHighRarityGuarantee(
        RewardGachaOffer offer,
        RewardGachaModeConfig config,
        RewardGachaPoolSource pool,
        HashSet<string> usedNames)
    {
        int required = Mathf.Max(0, config.guaranteeHighRarityCount);
        if (required <= 0 || offer == null || offer.cards == null)
            return;

        int highCount = 0;
        for (int i = 0; i < offer.cards.Count; i++)
        {
            if (offer.cards[i] != null && offer.cards[i].IsHighRarity)
                highCount++;
        }

        while (highCount < required)
        {
            List<int> lowIndexes = new List<int>();
            for (int i = 0; i < offer.cards.Count; i++)
            {
                if (offer.cards[i] != null && !offer.cards[i].IsHighRarity)
                    lowIndexes.Add(i);
            }

            if (lowIndexes.Count == 0)
                break;

            int replaceIndex = lowIndexes[UnityEngine.Random.Range(0, lowIndexes.Count)];
            RewardGachaCard old = offer.cards[replaceIndex];
            if (old != null)
                usedNames.Remove(MakeUsedKey(old.purpose, old.displayName));

            Dictionary<RewardGachaPurpose, int> counts = RebuildPurposeCounts(offer.cards, replaceIndex);
            offer.cards[replaceIndex] = RollCard(config, pool, counts, usedNames, RewardGachaRarity.Rare);
            highCount++;
        }
    }

    private static Dictionary<RewardGachaPurpose, int> RebuildPurposeCounts(List<RewardGachaCard> cards, int skipIndex)
    {
        Dictionary<RewardGachaPurpose, int> counts = new Dictionary<RewardGachaPurpose, int>();
        for (int i = 0; i < cards.Count; i++)
        {
            if (i == skipIndex || cards[i] == null)
                continue;

            RewardGachaPurpose purpose = cards[i].purpose;
            counts[purpose] = counts.TryGetValue(purpose, out int count) ? count + 1 : 1;
        }

        return counts;
    }

    private static RewardGachaRarity RollRarity(RewardGachaRarityRates rates)
    {
        int total = Mathf.Max(0, rates.common) + Mathf.Max(0, rates.uncommon) + Mathf.Max(0, rates.rare) + Mathf.Max(0, rates.special);
        if (total <= 0)
            return RewardGachaRarity.Common;

        int roll = UnityEngine.Random.Range(0, total);
        if (roll < rates.common)
            return RewardGachaRarity.Common;
        roll -= rates.common;
        if (roll < rates.uncommon)
            return RewardGachaRarity.Uncommon;
        roll -= rates.uncommon;
        if (roll < rates.rare)
            return RewardGachaRarity.Rare;
        return RewardGachaRarity.Special;
    }

    private static List<RewardGachaPurpose> GetEligiblePurposes(
        RewardGachaPoolSource pool,
        RewardGachaRarity rarity,
        Dictionary<RewardGachaPurpose, int> purposeCounts)
    {
        List<RewardGachaPurpose> result = new List<RewardGachaPurpose>();
        foreach (RewardGachaPurpose purpose in Enum.GetValues(typeof(RewardGachaPurpose)))
        {
            int cap = GetPurposeCap(purpose);
            int current = purposeCounts.TryGetValue(purpose, out int count) ? count : 0;
            if (current >= cap)
                continue;

            if (HasCandidate(pool, purpose, rarity))
                result.Add(purpose);
        }

        return result;
    }

    private static int GetPurposeCap(RewardGachaPurpose purpose)
    {
        return purpose == RewardGachaPurpose.Economy ? 1 : 2;
    }

    private static bool HasCandidate(RewardGachaPoolSource pool, RewardGachaPurpose purpose, RewardGachaRarity rarity)
    {
        if (purpose == RewardGachaPurpose.Economy && rarity == RewardGachaRarity.Common)
            return true;

        if (GetAssetCandidates(pool, purpose, rarity, null).Count > 0)
            return true;

        return GetFallbackCandidates(purpose, rarity).Count > 0;
    }

    private static string MakeUsedKey(RewardGachaPurpose purpose, string displayName)
    {
        return purpose + ":" + displayName;
    }
}

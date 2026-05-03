using System;
using System.Collections.Generic;
using UnityEngine;

public static class RewardGachaGenerator
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

    private static List<RewardCandidate> GetFallbackCandidates(RewardGachaPurpose purpose, RewardGachaRarity rarity)
    {
        List<RewardCandidate> result = new List<RewardCandidate>();

        if (purpose == RewardGachaPurpose.Economy && rarity == RewardGachaRarity.Common)
        {
            result.Add(MakeFallback("Extra Gold +10", "Gold added outside base gold.", purpose, rarity, RewardGachaItemKind.Gold, 10));
            result.Add(MakeFallback("Extra Gold +12", "Gold added outside base gold.", purpose, rarity, RewardGachaItemKind.Gold, 12));
            result.Add(MakeFallback("Extra Gold +15", "Gold added outside base gold.", purpose, rarity, RewardGachaItemKind.Gold, 15));
            result.Add(MakeFallback("Small Bonus Purse", "A small economy reward.", purpose, rarity, RewardGachaItemKind.Gold, 10));
        }
        else if (purpose == RewardGachaPurpose.Skill)
        {
            AddFallbackNames(result, purpose, rarity, RewardGachaItemKind.Fallback, GetSkillFallbacks(rarity));
        }
        else if (purpose == RewardGachaPurpose.Passive)
        {
            AddFallbackNames(result, purpose, rarity, RewardGachaItemKind.Fallback, GetPassiveFallbacks(rarity));
        }
        else if (purpose == RewardGachaPurpose.EditDice)
        {
            AddFallbackNames(result, purpose, rarity, RewardGachaItemKind.Fallback, GetEditDiceFallbacks(rarity));
        }
        else if (purpose == RewardGachaPurpose.CombatAid)
        {
            AddFallbackNames(result, purpose, rarity, RewardGachaItemKind.Fallback, rarity == RewardGachaRarity.Uncommon
                ? new[] { "Final Verdict", "Cryostasis", "Ignite Spread", "Exploit Mark", "Exsanguinate" }
                : Array.Empty<string>());
        }
        else if (purpose == RewardGachaPurpose.UtilitySupport)
        {
            AddFallbackNames(result, purpose, rarity, RewardGachaItemKind.Fallback, rarity == RewardGachaRarity.Uncommon
                ? new[] { "Restore Focus", "Dice Reroll", "Cleanse", "Double Gold", "Create Last Used" }
                : Array.Empty<string>());
        }

        return result;
    }

    private static void AddFallbackNames(List<RewardCandidate> result, RewardGachaPurpose purpose, RewardGachaRarity rarity, RewardGachaItemKind kind, string[] names)
    {
        for (int i = 0; i < names.Length; i++)
            result.Add(MakeFallback(names[i], "Fallback demo reward. Replace with a real asset when authored.", purpose, rarity, kind, 0));
    }

    private static RewardCandidate MakeFallback(string name, string description, RewardGachaPurpose purpose, RewardGachaRarity rarity, RewardGachaItemKind kind, int amount)
    {
        return new RewardCandidate
        {
            displayName = name,
            description = description,
            purpose = purpose,
            rarity = rarity,
            itemKind = kind,
            amount = amount
        };
    }

    private static string[] GetSkillFallbacks(RewardGachaRarity rarity)
    {
        switch (rarity)
        {
            case RewardGachaRarity.Common:
                return new[] { "Fire Slash", "Shatter", "Brutal Smash", "Fated Sunder" };
            case RewardGachaRarity.Uncommon:
                return new[] { "Ignite+", "Cauterize", "Deep Freeze", "Static Conduit", "Winter's Bite" };
            case RewardGachaRarity.Rare:
                return new[] { "Hellfire", "Permafrost Chain", "Execution", "Adaptive Slot Attack" };
            case RewardGachaRarity.Special:
                return new[] { "Astral Verdict", "Dragon Pact Skill", "Void Technique", "Crownbreaker" };
            default:
                return Array.Empty<string>();
        }
    }

    private static string[] GetPassiveFallbacks(RewardGachaRarity rarity)
    {
        switch (rarity)
        {
            case RewardGachaRarity.Uncommon:
                return new[] { "Clear Mind", "Even Resonance", "Fail Forward", "Iron Stance" };
            case RewardGachaRarity.Rare:
                return new[] { "Elemental Catalyst", "Crit Escalation", "Dice Forging", "Burn Engine" };
            case RewardGachaRarity.Special:
                return new[] { "Fate Engine", "Perfect Resonance", "Dragonheart", "Void Contract" };
            default:
                return Array.Empty<string>();
        }
    }

    private static string[] GetEditDiceFallbacks(RewardGachaRarity rarity)
    {
        switch (rarity)
        {
            case RewardGachaRarity.Uncommon:
                return new[] { "Adjust Face +", "Adjust Face -", "Copy / Paste Face", "Value +N", "Guard Boost Enchant", "Fire Enchant", "Ice Enchant", "Bleed Enchant", "Lightning Enchant" };
            case RewardGachaRarity.Rare:
            case RewardGachaRarity.Special:
                return new[] { "Whole-die Color Ore: Patina" };
            default:
                return Array.Empty<string>();
        }
    }

    private static ContentRarity GetSkillRarity(ScriptableObject asset)
    {
        if (asset is SkillDamageSO damage && damage.spec != null)
            return damage.spec.rarity;
        if (asset is SkillBuffDebuffSO buff && buff.spec != null)
            return buff.spec.rarity;
        return ContentRarity.Pending;
    }

    private static string GetSkillName(ScriptableObject asset)
    {
        if (asset is SkillDamageSO damage)
            return string.IsNullOrWhiteSpace(damage.displayName) ? damage.name : damage.displayName;
        if (asset is SkillBuffDebuffSO buff)
            return string.IsNullOrWhiteSpace(buff.displayName) ? buff.name : buff.displayName;
        return asset != null ? asset.name : "Skill";
    }

    private static string GetSkillDescription(ScriptableObject asset)
    {
        if (asset is SkillDamageSO damage)
            return damage.description;
        if (asset is SkillBuffDebuffSO buff)
            return buff.description;
        return string.Empty;
    }

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

    private static bool IsUsed(HashSet<string> usedNames, RewardGachaPurpose purpose, string displayName)
    {
        return usedNames != null && usedNames.Contains(MakeUsedKey(purpose, displayName));
    }

    private static string MakeUsedKey(RewardGachaPurpose purpose, string displayName)
    {
        return purpose + ":" + displayName;
    }
}

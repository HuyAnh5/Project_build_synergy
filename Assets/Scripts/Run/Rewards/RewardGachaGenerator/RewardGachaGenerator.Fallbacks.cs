using System;
using System.Collections.Generic;
using UnityEngine;

// Supplies prototype fallback rewards when real authored assets are unavailable.
public static partial class RewardGachaGenerator
{
    // Builds fallback candidates for each purpose/rarity lane.
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
                ? new[] { "Restore Focus", "Cleanse", "Double Gold", "Create Last Used" }
                : Array.Empty<string>());
        }

        return result;
    }

    // Adds fallback display names using a generic demo description.
    private static void AddFallbackNames(List<RewardCandidate> result, RewardGachaPurpose purpose, RewardGachaRarity rarity, RewardGachaItemKind kind, string[] names)
    {
        for (int i = 0; i < names.Length; i++)
            result.Add(MakeFallback(names[i], "Fallback demo reward. Replace with a real asset when authored.", purpose, rarity, kind, 0));
    }

    // Creates a fallback candidate object.
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

    // Returns prototype skill names by rarity.
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

    // Returns prototype passive names by rarity.
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

    // Returns prototype dice-edit names by rarity.
    private static string[] GetEditDiceFallbacks(RewardGachaRarity rarity)
    {
        switch (rarity)
        {
            case RewardGachaRarity.Uncommon:
                return new[] { "Adjust Face +", "Adjust Face -", "Copy / Paste Face", "Set Rolled Face", "Dice Reroll", "Power", "Guard", "Charge", "Gold", "Gum", "Relay", "Double", "Repeat", "Reload", "Heavy", "Echo", "Stone" };
            case RewardGachaRarity.Rare:
            case RewardGachaRarity.Special:
                return new[] { "Whole-die Color Ore: Patina" };
            default:
                return Array.Empty<string>();
        }
    }

    // Reads rarity metadata from active skill assets.
    private static ContentRarity GetSkillRarity(ScriptableObject asset)
    {
        if (asset is SkillDamageSO damage && damage.spec != null)
            return damage.spec.rarity;
        if (asset is SkillBuffDebuffSO buff && buff.spec != null)
            return buff.spec.rarity;
        return ContentRarity.Pending;
    }

    // Reads the display name from a damage or buff skill asset.
    private static string GetSkillName(ScriptableObject asset)
    {
        if (asset is SkillDamageSO damage)
            return string.IsNullOrWhiteSpace(damage.displayName) ? damage.name : damage.displayName;
        if (asset is SkillBuffDebuffSO buff)
            return string.IsNullOrWhiteSpace(buff.displayName) ? buff.name : buff.displayName;
        return asset != null ? asset.name : "Skill";
    }

    // Reads the display description from a damage or buff skill asset.
    private static string GetSkillDescription(ScriptableObject asset)
    {
        if (asset is SkillDamageSO damage)
            return damage.GetAuthoringDescription();
        if (asset is SkillBuffDebuffSO buff)
            return buff.GetAuthoringDescription();
        return string.Empty;
    }
}

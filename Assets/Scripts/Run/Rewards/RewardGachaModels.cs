using System;
using System.Collections.Generic;
using UnityEngine;

public enum RewardGachaEncounterMode
{
    Combat,
    Elite,
    Boss
}

public enum RewardGachaRarity
{
    Common,
    Uncommon,
    Rare,
    Special
}

public enum RewardGachaPurpose
{
    Economy,
    Skill,
    Passive,
    EditDice,
    CombatAid,
    UtilitySupport
}

public enum RewardGachaItemKind
{
    Fallback,
    Gold,
    Skill,
    Passive,
    Consumable,
    DiceColorOre
}

[Serializable]
public struct RewardGachaRarityRates
{
    [Range(0, 100)] public int common;
    [Range(0, 100)] public int uncommon;
    [Range(0, 100)] public int rare;
    [Range(0, 100)] public int special;

    public RewardGachaRarityRates(int common, int uncommon, int rare, int special)
    {
        this.common = common;
        this.uncommon = uncommon;
        this.rare = rare;
        this.special = special;
    }
}

[Serializable]
public sealed class RewardGachaModeConfig
{
    public RewardGachaEncounterMode mode;
    public string label;
    public int choices;
    public int picks;
    public int goldMin;
    public int goldMax;
    public int guaranteeHighRarityCount;
    public RewardGachaRarityRates rates;

    public static RewardGachaModeConfig CombatDefault()
    {
        return new RewardGachaModeConfig
        {
            mode = RewardGachaEncounterMode.Combat,
            label = "Combat",
            choices = 3,
            picks = 1,
            goldMin = 8,
            goldMax = 14,
            guaranteeHighRarityCount = 0,
            rates = new RewardGachaRarityRates(65, 30, 5, 0)
        };
    }

    public static RewardGachaModeConfig EliteDefault()
    {
        return new RewardGachaModeConfig
        {
            mode = RewardGachaEncounterMode.Elite,
            label = "Elite",
            choices = 4,
            picks = 1,
            goldMin = 22,
            goldMax = 35,
            guaranteeHighRarityCount = 0,
            rates = new RewardGachaRarityRates(35, 45, 18, 2)
        };
    }

    public static RewardGachaModeConfig BossDefault()
    {
        return new RewardGachaModeConfig
        {
            mode = RewardGachaEncounterMode.Boss,
            label = "Boss",
            choices = 5,
            picks = 2,
            goldMin = 50,
            goldMax = 70,
            guaranteeHighRarityCount = 2,
            rates = new RewardGachaRarityRates(0, 60, 35, 5)
        };
    }
}

[Serializable]
public sealed class RewardGachaCard
{
    public string id;
    public string displayName;
    public string description;
    public RewardGachaPurpose purpose;
    public RewardGachaRarity rarity;
    public RewardGachaItemKind itemKind;
    public ScriptableObject asset;
    public int amount;

    public bool IsHighRarity => rarity == RewardGachaRarity.Rare || rarity == RewardGachaRarity.Special;
}

[Serializable]
public sealed class RewardGachaOffer
{
    public RewardGachaEncounterMode mode;
    public int baseGold;
    public int picksAllowed;
    public List<RewardGachaCard> cards = new List<RewardGachaCard>();
}

[Serializable]
public sealed class RewardGachaPoolSource
{
    public SkillDatabaseSO skillDatabase;
    public List<ConsumableDataSO> consumables = new List<ConsumableDataSO>();
    public List<DiceColorOreSO> diceColorOres = new List<DiceColorOreSO>();
}

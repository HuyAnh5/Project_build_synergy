using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Refreshes demo UI labels and tracks observed rarity stats.
public sealed partial class RewardGachaDemoController
{
    // Refreshes mode title, pick counter, base gold, and reroll interactability.
    private void RefreshHeader()
    {
        RewardGachaModeConfig config = RewardGachaGenerator.GetDefaultConfig(currentMode);
        int selected = _selectedIndexes.Count;
        int picks = _offer != null ? _offer.picksAllowed : config.picks;
        int choices = _offer != null && _offer.cards != null ? _offer.cards.Count : config.choices;
        int gold = _offer != null ? _offer.baseGold : 0;

        if (_modeTitleText != null)
            _modeTitleText.text = GetModeLabel(currentMode);
        if (_modeRuleText != null)
        {
            _modeRuleText.text = config.guaranteeHighRarityCount > 0
                ? choices + " rewards, pick " + picks + ". Boss guarantee: at least " + config.guaranteeHighRarityCount + " Rare/Special cards."
                : choices + " rewards, pick " + picks + ".";
        }
        if (_baseGoldText != null)
            _baseGoldText.text = "Base Gold +" + gold;
        if (_pickCounterText != null)
            _pickCounterText.text = "Selected " + selected + "/" + picks;
        if (_rerollButton != null)
            _rerollButton.interactable = !_locked;
    }

    // Refreshes mode button colors and locking state.
    private void RefreshModeButtons()
    {
        for (int i = 0; i < _modeButtons.Count; i++)
        {
            bool active = i == (int)currentMode;
            if (_modeButtonImages[i] != null)
                _modeButtonImages[i].color = active ? ActiveButtonColor : ButtonColor;
            if (_modeButtons[i] != null)
                _modeButtons[i].interactable = !_locked || active;
        }
    }

    // Refreshes observed rarity counts for the current mode.
    private void RefreshStats()
    {
        if (_statsText == null)
            return;

        StatBucket stats = GetStats(currentMode);
        _statsText.text =
            "Current mode: " + GetModeLabel(currentMode) + "\n\n" +
            "Cards observed: " + stats.total + "\n" +
            "Common: " + stats.common + "\n" +
            "Uncommon: " + stats.uncommon + "\n" +
            "Rare: " + stats.rare + "\n" +
            "Special: " + stats.special;
    }

    // Adds all cards in one generated offer to mode-specific stats.
    private void AddOfferToStats(RewardGachaOffer offer)
    {
        if (offer == null || offer.cards == null)
            return;

        StatBucket stats = GetStats(offer.mode);
        for (int i = 0; i < offer.cards.Count; i++)
        {
            if (offer.cards[i] != null)
                stats.Add(offer.cards[i].rarity);
        }
    }

    // Returns the stat bucket for a mode, creating it if needed.
    private StatBucket GetStats(RewardGachaEncounterMode mode)
    {
        EnsureStats();
        return _stats[mode];
    }

    // Ensures every encounter mode has a stat bucket.
    private void EnsureStats()
    {
        foreach (RewardGachaEncounterMode mode in System.Enum.GetValues(typeof(RewardGachaEncounterMode)))
        {
            if (!_stats.ContainsKey(mode))
                _stats.Add(mode, new StatBucket());
        }
    }

    // Writes the current demo status message.
    private void SetStatus(string message)
    {
        if (_statusText != null)
            _statusText.text = message;
    }

    // Converts enum values into user-facing labels.
    private static string GetModeLabel(RewardGachaEncounterMode mode)
    {
        switch (mode)
        {
            case RewardGachaEncounterMode.Elite:
                return "Elite";
            case RewardGachaEncounterMode.Boss:
                return "Boss";
            default:
                return "Combat";
        }
    }
}

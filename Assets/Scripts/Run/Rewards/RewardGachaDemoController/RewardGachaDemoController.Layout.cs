using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Builds the in-editor reward gacha demo UI hierarchy.
public sealed partial class RewardGachaDemoController
{
    // Builds the title block and mode buttons.
    private void BuildTopBar(Transform parent)
    {
        RectTransform topBar = CreatePanel("TopBar", parent, PanelColor);
        HorizontalLayoutGroup layout = GetOrAdd<HorizontalLayoutGroup>(topBar.gameObject);
        layout.padding = new RectOffset(18, 18, 14, 14);
        layout.spacing = 16f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        MapPrototypeUIFactory.AddLayoutElement(topBar.gameObject, preferredHeight: 118f);

        RectTransform titleBlock = MapPrototypeUIFactory.CreateRect("TitleBlock", topBar);
        VerticalLayoutGroup titleLayout = GetOrAdd<VerticalLayoutGroup>(titleBlock.gameObject);
        titleLayout.spacing = 5f;
        titleLayout.childControlWidth = true;
        titleLayout.childControlHeight = true;
        titleLayout.childForceExpandWidth = true;
        titleLayout.childForceExpandHeight = false;
        MapPrototypeUIFactory.AddLayoutElement(titleBlock.gameObject, flexibleWidth: 1f);

        TextMeshProUGUI title = MapPrototypeUIFactory.CreateText("Title", titleBlock, "Reward Gacha Demo", 30, FontStyles.Bold, TextColor, TextAlignmentOptions.Left);
        MapPrototypeUIFactory.AddLayoutElement(title.gameObject, preferredHeight: 38f);

        TextMeshProUGUI subtitle = MapPrototypeUIFactory.CreateText(
            "Subtitle",
            titleBlock,
            "Combat, Elite, and Boss reward rolls based on the HTML prototype. Uses real skill/passive/consumable assets when available.",
            15,
            FontStyles.Normal,
            MutedTextColor,
            TextAlignmentOptions.Left);
        MapPrototypeUIFactory.AddLayoutElement(subtitle.gameObject, preferredHeight: 50f);

        RectTransform buttons = MapPrototypeUIFactory.CreateRect("ModeButtons", topBar);
        HorizontalLayoutGroup buttonLayout = GetOrAdd<HorizontalLayoutGroup>(buttons.gameObject);
        buttonLayout.spacing = 8f;
        buttonLayout.childControlWidth = false;
        buttonLayout.childControlHeight = false;
        buttonLayout.childForceExpandWidth = false;
        buttonLayout.childForceExpandHeight = false;
        buttonLayout.childAlignment = TextAnchor.MiddleRight;
        MapPrototypeUIFactory.AddLayoutElement(buttons.gameObject, preferredWidth: 430f, preferredHeight: 54f);

        AddModeButton(buttons, "Combat", RewardGachaEncounterMode.Combat);
        AddModeButton(buttons, "Elite", RewardGachaEncounterMode.Elite);
        AddModeButton(buttons, "Boss", RewardGachaEncounterMode.Boss);
    }

    // Builds the roll header, toolbar, and card container.
    private void BuildRewardPanel(Transform parent)
    {
        RectTransform panel = CreatePanel("RewardPanel", parent, PanelColor);
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(panel.gameObject);
        layout.padding = new RectOffset(18, 18, 16, 16);
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        MapPrototypeUIFactory.AddLayoutElement(panel.gameObject, preferredHeight: 380f);

        BuildRewardHeader(panel);
        BuildRewardToolbar(panel);

        _cardsRoot = MapPrototypeUIFactory.CreateRect("Cards", panel);
        HorizontalLayoutGroup cardsLayout = GetOrAdd<HorizontalLayoutGroup>(_cardsRoot.gameObject);
        cardsLayout.spacing = 12f;
        cardsLayout.childControlWidth = true;
        cardsLayout.childControlHeight = true;
        cardsLayout.childForceExpandWidth = true;
        cardsLayout.childForceExpandHeight = true;
        MapPrototypeUIFactory.AddLayoutElement(_cardsRoot.gameObject, preferredHeight: 230f);
    }

    // Builds the current mode header and reward meta pills.
    private void BuildRewardHeader(Transform parent)
    {
        RectTransform header = MapPrototypeUIFactory.CreateRect("Header", parent);
        HorizontalLayoutGroup headerLayout = GetOrAdd<HorizontalLayoutGroup>(header.gameObject);
        headerLayout.spacing = 12f;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = true;
        headerLayout.childForceExpandHeight = false;
        MapPrototypeUIFactory.AddLayoutElement(header.gameObject, preferredHeight: 64f);

        RectTransform headerText = MapPrototypeUIFactory.CreateRect("HeaderText", header);
        VerticalLayoutGroup headerTextLayout = GetOrAdd<VerticalLayoutGroup>(headerText.gameObject);
        headerTextLayout.spacing = 3f;
        headerTextLayout.childControlWidth = true;
        headerTextLayout.childControlHeight = true;
        headerTextLayout.childForceExpandWidth = true;
        headerTextLayout.childForceExpandHeight = false;
        MapPrototypeUIFactory.AddLayoutElement(headerText.gameObject, flexibleWidth: 1f);

        _modeTitleText = MapPrototypeUIFactory.CreateText("ModeTitle", headerText, "Combat", 24, FontStyles.Bold, TextColor, TextAlignmentOptions.Left);
        _modeRuleText = MapPrototypeUIFactory.CreateText("ModeRule", headerText, "3 rewards, pick 1.", 15, FontStyles.Normal, MutedTextColor, TextAlignmentOptions.Left);

        RectTransform meta = MapPrototypeUIFactory.CreateRect("RewardMeta", header);
        HorizontalLayoutGroup metaLayout = GetOrAdd<HorizontalLayoutGroup>(meta.gameObject);
        metaLayout.spacing = 8f;
        metaLayout.childControlWidth = false;
        metaLayout.childControlHeight = false;
        metaLayout.childForceExpandWidth = false;
        metaLayout.childForceExpandHeight = false;
        metaLayout.childAlignment = TextAnchor.MiddleRight;
        MapPrototypeUIFactory.AddLayoutElement(meta.gameObject, preferredWidth: 320f, preferredHeight: 44f);

        _baseGoldText = CreatePill(meta, "Base Gold +0", new Color32(80, 62, 32, 255));
        _pickCounterText = CreatePill(meta, "Selected 0/1", new Color32(38, 72, 75, 255));
    }

    // Builds reroll/reset controls and status text.
    private void BuildRewardToolbar(Transform parent)
    {
        RectTransform toolbar = MapPrototypeUIFactory.CreateRect("Toolbar", parent);
        HorizontalLayoutGroup toolbarLayout = GetOrAdd<HorizontalLayoutGroup>(toolbar.gameObject);
        toolbarLayout.spacing = 10f;
        toolbarLayout.childControlWidth = false;
        toolbarLayout.childControlHeight = false;
        toolbarLayout.childForceExpandWidth = false;
        toolbarLayout.childForceExpandHeight = false;
        toolbarLayout.childAlignment = TextAnchor.MiddleLeft;
        MapPrototypeUIFactory.AddLayoutElement(toolbar.gameObject, preferredHeight: 48f);

        _rerollButton = CreateSmallButton(toolbar, "Reroll Test");
        _rerollButton.onClick.AddListener(RollRewards);
        Button resetButton = CreateSmallButton(toolbar, "Reset Stats");
        resetButton.onClick.AddListener(ResetCurrentStats);
        _statusText = MapPrototypeUIFactory.CreateText("Status", toolbar, "Pick a reward.", 15, FontStyles.Normal, MutedTextColor, TextAlignmentOptions.Left);
        MapPrototypeUIFactory.AddLayoutElement(_statusText.gameObject, preferredWidth: 520f, preferredHeight: 42f);
    }

    // Builds the static rule explanation and observed-stat panels.
    private void BuildBottomPanels(Transform parent)
    {
        RectTransform bottom = MapPrototypeUIFactory.CreateRect("BottomPanels", parent);
        HorizontalLayoutGroup bottomLayout = GetOrAdd<HorizontalLayoutGroup>(bottom.gameObject);
        bottomLayout.spacing = 18f;
        bottomLayout.childControlWidth = true;
        bottomLayout.childControlHeight = true;
        bottomLayout.childForceExpandWidth = true;
        bottomLayout.childForceExpandHeight = true;
        MapPrototypeUIFactory.AddLayoutElement(bottom.gameObject, flexibleHeight: 1f);

        BuildRulesPanel(bottom);
        BuildStatsPanel(bottom);
    }

    // Builds the text panel documenting reward roll rules.
    private void BuildRulesPanel(Transform parent)
    {
        RectTransform rules = CreatePanel("RulesPanel", parent, PanelColor);
        VerticalLayoutGroup rulesLayout = GetOrAdd<VerticalLayoutGroup>(rules.gameObject);
        rulesLayout.padding = new RectOffset(16, 16, 14, 14);
        rulesLayout.spacing = 8f;
        rulesLayout.childControlWidth = true;
        rulesLayout.childControlHeight = true;
        rulesLayout.childForceExpandWidth = true;
        rulesLayout.childForceExpandHeight = false;
        MapPrototypeUIFactory.AddLayoutElement(rules.gameObject, flexibleWidth: 1f);

        TextMeshProUGUI rulesTitle = MapPrototypeUIFactory.CreateText("RulesTitle", rules, "Rule / Rarity / Purpose", 20, FontStyles.Bold, TextColor, TextAlignmentOptions.Left);
        MapPrototypeUIFactory.AddLayoutElement(rulesTitle.gameObject, preferredHeight: 30f);
        TextMeshProUGUI rulesBody = MapPrototypeUIFactory.CreateText(
            "RulesBody",
            rules,
            "Combat: 3 cards, pick 1. Rates 65% Common / 30% Uncommon / 5% Rare / 0% Special.\n" +
            "Elite: 4 cards, pick 1. Rates 35% Common / 45% Uncommon / 18% Rare / 2% Special.\n" +
            "Boss: 5 cards, pick 2. Rates 0% Common / 60% Uncommon / 35% Rare / 5% Special, then guarantee at least 2 Rare/Special cards.\n\n" +
            "Purpose cap: Economy max 1 card per roll. Other purposes max 2 cards per roll.\n" +
            "Consumables use real ConsumableDataSO assets when assigned. Zodiac = Edit Dice, Seal = Combat Aid, Rune = Utility Support.\n" +
            "Whole-die color ore is an Edit Dice reward for Forge use; this demo creates Patina ore if missing.",
            15,
            FontStyles.Normal,
            MutedTextColor,
            TextAlignmentOptions.TopLeft);
        MapPrototypeUIFactory.AddLayoutElement(rulesBody.gameObject, flexibleHeight: 1f);
    }

    // Builds the observed rarity-stat panel.
    private void BuildStatsPanel(Transform parent)
    {
        RectTransform stats = CreatePanel("StatsPanel", parent, PanelColor);
        VerticalLayoutGroup statsLayout = GetOrAdd<VerticalLayoutGroup>(stats.gameObject);
        statsLayout.padding = new RectOffset(16, 16, 14, 14);
        statsLayout.spacing = 8f;
        statsLayout.childControlWidth = true;
        statsLayout.childControlHeight = true;
        statsLayout.childForceExpandWidth = true;
        statsLayout.childForceExpandHeight = false;
        MapPrototypeUIFactory.AddLayoutElement(stats.gameObject, preferredWidth: 420f);

        TextMeshProUGUI statsTitle = MapPrototypeUIFactory.CreateText("StatsTitle", stats, "Observed Stats", 20, FontStyles.Bold, TextColor, TextAlignmentOptions.Left);
        MapPrototypeUIFactory.AddLayoutElement(statsTitle.gameObject, preferredHeight: 30f);
        _statsText = MapPrototypeUIFactory.CreateText("StatsBody", stats, string.Empty, 16, FontStyles.Normal, MutedTextColor, TextAlignmentOptions.TopLeft);
        MapPrototypeUIFactory.AddLayoutElement(_statsText.gameObject, flexibleHeight: 1f);
    }
}

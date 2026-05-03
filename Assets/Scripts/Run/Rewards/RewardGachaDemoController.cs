using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RewardGachaDemoController : MonoBehaviour
{
    [Header("Data")]
    public RewardGachaPoolSource pool = new RewardGachaPoolSource();
    public RunInventoryManager runInventory;

    [Header("Demo Behavior")]
    [SerializeField] private RewardGachaEncounterMode currentMode = RewardGachaEncounterMode.Combat;
    [SerializeField] private bool autoRollOnStart = true;
    [SerializeField] private bool autoRerollAfterPick = true;
    [SerializeField] private bool applySelectionToInventory;
    [SerializeField] private float autoRerollDelay = 1f;

    private readonly HashSet<int> _selectedIndexes = new HashSet<int>();
    private readonly Dictionary<RewardGachaEncounterMode, StatBucket> _stats = new Dictionary<RewardGachaEncounterMode, StatBucket>();
    private readonly List<Button> _modeButtons = new List<Button>();
    private readonly List<Image> _modeButtonImages = new List<Image>();
    private RewardGachaOffer _offer;
    private bool _locked;
    private Coroutine _rerollRoutine;

    private TextMeshProUGUI _modeTitleText;
    private TextMeshProUGUI _modeRuleText;
    private TextMeshProUGUI _baseGoldText;
    private TextMeshProUGUI _pickCounterText;
    private TextMeshProUGUI _statusText;
    private TextMeshProUGUI _statsText;
    private RectTransform _cardsRoot;
    private Button _rerollButton;

    private static readonly Color BackgroundColor = new Color32(18, 20, 24, 255);
    private static readonly Color PanelColor = new Color32(34, 39, 48, 245);
    private static readonly Color PanelInnerColor = new Color32(44, 51, 63, 245);
    private static readonly Color TextColor = new Color32(238, 241, 246, 255);
    private static readonly Color MutedTextColor = new Color32(174, 185, 201, 255);
    private static readonly Color ButtonColor = new Color32(61, 72, 89, 255);
    private static readonly Color ActiveButtonColor = new Color32(92, 116, 151, 255);
    private static readonly Color SelectedCardColor = new Color32(50, 79, 65, 255);

    private sealed class StatBucket
    {
        public int total;
        public int common;
        public int uncommon;
        public int rare;
        public int special;

        public void Reset()
        {
            total = 0;
            common = 0;
            uncommon = 0;
            rare = 0;
            special = 0;
        }

        public void Add(RewardGachaRarity rarity)
        {
            total++;
            switch (rarity)
            {
                case RewardGachaRarity.Uncommon:
                    uncommon++;
                    break;
                case RewardGachaRarity.Rare:
                    rare++;
                    break;
                case RewardGachaRarity.Special:
                    special++;
                    break;
                default:
                    common++;
                    break;
            }
        }
    }

    private void Awake()
    {
        EnsureStats();
        if (_cardsRoot == null)
            RebuildDemoUi();
    }

    private void Start()
    {
        if (autoRollOnStart && _offer == null)
            RollRewards();
    }

    public void ConfigureData(SkillDatabaseSO skillDatabase, IEnumerable<ConsumableDataSO> consumables, IEnumerable<DiceColorOreSO> ores)
    {
        if (pool == null)
            pool = new RewardGachaPoolSource();

        pool.skillDatabase = skillDatabase;
        pool.consumables.Clear();
        pool.diceColorOres.Clear();

        if (consumables != null)
        {
            foreach (ConsumableDataSO consumable in consumables)
            {
                if (consumable != null && !pool.consumables.Contains(consumable))
                    pool.consumables.Add(consumable);
            }
        }

        if (ores != null)
        {
            foreach (DiceColorOreSO ore in ores)
            {
                if (ore != null && !pool.diceColorOres.Contains(ore))
                    pool.diceColorOres.Add(ore);
            }
        }
    }

    [ContextMenu("Rebuild Demo UI")]
    public void RebuildDemoUi()
    {
        ClearChildren(transform);
        EnsureStats();
        _modeButtons.Clear();
        _modeButtonImages.Clear();
        _cardsRoot = null;
        _rerollButton = null;

        RectTransform root = transform as RectTransform;
        if (root != null)
        {
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
        }

        Image bg = GetComponent<Image>();
        if (bg == null)
            bg = gameObject.AddComponent<Image>();
        bg.color = BackgroundColor;

        VerticalLayoutGroup rootLayout = GetOrAdd<VerticalLayoutGroup>(gameObject);
        rootLayout.padding = new RectOffset(28, 28, 22, 24);
        rootLayout.spacing = 18f;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        BuildTopBar(transform);
        BuildRewardPanel(transform);
        BuildBottomPanels(transform);
        RefreshModeButtons();
        RefreshHeader();
        RefreshStats();
    }

    [ContextMenu("Roll Rewards")]
    public void RollRewards()
    {
        if (_rerollRoutine != null)
        {
            StopCoroutine(_rerollRoutine);
            _rerollRoutine = null;
        }

        _locked = false;
        _selectedIndexes.Clear();
        _offer = RewardGachaGenerator.RollOffer(currentMode, pool);
        AddOfferToStats(_offer);
        RenderCards();
        RefreshHeader();
        RefreshModeButtons();
        RefreshStats();
        SetStatus("Pick a reward. " + GetModeLabel(currentMode) + " allows " + _offer.picksAllowed + " pick(s).");
    }

    public void SetModeCombat()
    {
        SetMode(RewardGachaEncounterMode.Combat);
    }

    public void SetModeElite()
    {
        SetMode(RewardGachaEncounterMode.Elite);
    }

    public void SetModeBoss()
    {
        SetMode(RewardGachaEncounterMode.Boss);
    }

    public void ResetCurrentStats()
    {
        GetStats(currentMode).Reset();
        RefreshStats();
    }

    private void SetMode(RewardGachaEncounterMode mode)
    {
        if (_locked)
            return;

        currentMode = mode;
        RollRewards();
    }

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

        RectTransform header = MapPrototypeUIFactory.CreateRect("Header", panel);
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

        RectTransform toolbar = MapPrototypeUIFactory.CreateRect("Toolbar", panel);
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

        _cardsRoot = MapPrototypeUIFactory.CreateRect("Cards", panel);
        HorizontalLayoutGroup cardsLayout = GetOrAdd<HorizontalLayoutGroup>(_cardsRoot.gameObject);
        cardsLayout.spacing = 12f;
        cardsLayout.childControlWidth = true;
        cardsLayout.childControlHeight = true;
        cardsLayout.childForceExpandWidth = true;
        cardsLayout.childForceExpandHeight = true;
        MapPrototypeUIFactory.AddLayoutElement(_cardsRoot.gameObject, preferredHeight: 230f);
    }

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

        RectTransform rules = CreatePanel("RulesPanel", bottom, PanelColor);
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

        RectTransform stats = CreatePanel("StatsPanel", bottom, PanelColor);
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

    private void AddModeButton(Transform parent, string label, RewardGachaEncounterMode mode)
    {
        Button button = CreateSmallButton(parent, label);
        Image image = button.GetComponent<Image>();
        _modeButtons.Add(button);
        _modeButtonImages.Add(image);
        button.onClick.AddListener(() => SetMode(mode));
    }

    private void RenderCards()
    {
        if (_cardsRoot == null)
            return;

        ClearChildren(_cardsRoot);
        if (_offer == null || _offer.cards == null)
            return;

        for (int i = 0; i < _offer.cards.Count; i++)
        {
            int index = i;
            RewardGachaCard card = _offer.cards[i];
            Button button = CreateCard(_cardsRoot, card, index);
            button.onClick.AddListener(() => ToggleCard(index));
        }
    }

    private Button CreateCard(Transform parent, RewardGachaCard card, int index)
    {
        Image image = MapPrototypeUIFactory.CreateImage("Card" + index, parent, PanelInnerColor, true);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        MapPrototypeUIFactory.AddLayoutElement(image.gameObject, preferredHeight: 224f, flexibleWidth: 1f);

        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(image.gameObject);
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 7f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Image stripe = MapPrototypeUIFactory.CreateImage("RarityStripe", image.transform, RewardGachaGenerator.GetRarityColor(card.rarity), false);
        MapPrototypeUIFactory.AddLayoutElement(stripe.gameObject, preferredHeight: 7f);

        TextMeshProUGUI rarity = MapPrototypeUIFactory.CreateText("Rarity", image.transform, RewardGachaGenerator.GetRarityLabel(card.rarity), 13, FontStyles.Bold, RewardGachaGenerator.GetRarityColor(card.rarity), TextAlignmentOptions.Left);
        MapPrototypeUIFactory.AddLayoutElement(rarity.gameObject, preferredHeight: 22f);

        TextMeshProUGUI title = MapPrototypeUIFactory.CreateText("Title", image.transform, card.displayName, 20, FontStyles.Bold, TextColor, TextAlignmentOptions.Left);
        MapPrototypeUIFactory.AddLayoutElement(title.gameObject, preferredHeight: 54f);

        TextMeshProUGUI purpose = MapPrototypeUIFactory.CreateText(
            "Purpose",
            image.transform,
            RewardGachaGenerator.GetPurposeLabel(card.purpose) + " / " + card.itemKind,
            14,
            FontStyles.Normal,
            MutedTextColor,
            TextAlignmentOptions.Left);
        MapPrototypeUIFactory.AddLayoutElement(purpose.gameObject, preferredHeight: 32f);

        string description = string.IsNullOrWhiteSpace(card.description) ? "No description yet." : card.description;
        TextMeshProUGUI body = MapPrototypeUIFactory.CreateText("Description", image.transform, description, 13, FontStyles.Normal, MutedTextColor, TextAlignmentOptions.TopLeft);
        MapPrototypeUIFactory.AddLayoutElement(body.gameObject, flexibleHeight: 1f);

        RefreshCardVisual(image, index);
        return button;
    }

    private void ToggleCard(int index)
    {
        if (_locked || _offer == null || index < 0 || index >= _offer.cards.Count)
            return;

        if (_selectedIndexes.Contains(index))
            _selectedIndexes.Remove(index);
        else
        {
            if (_selectedIndexes.Count >= _offer.picksAllowed)
                return;
            _selectedIndexes.Add(index);
        }

        RefreshHeader();
        RefreshCardsOnly();

        if (_selectedIndexes.Count >= _offer.picksAllowed)
            LockSelection();
    }

    private void LockSelection()
    {
        _locked = true;
        List<string> messages = new List<string>();
        foreach (int index in _selectedIndexes)
        {
            RewardGachaCard card = _offer.cards[index];
            if (applySelectionToInventory)
            {
                RewardGachaApplier.TryApply(card, runInventory, out string message);
                messages.Add(message);
            }
            else
            {
                messages.Add("Selected " + card.displayName + ".");
            }
        }

        SetStatus(string.Join(" ", messages));
        if (autoRerollAfterPick)
            _rerollRoutine = StartCoroutine(AutoRerollRoutine());
    }

    private IEnumerator AutoRerollRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, autoRerollDelay));
        _rerollRoutine = null;
        RollRewards();
    }

    private void RefreshCardsOnly()
    {
        if (_cardsRoot == null)
            return;

        for (int i = 0; i < _cardsRoot.childCount; i++)
        {
            Image image = _cardsRoot.GetChild(i).GetComponent<Image>();
            if (image != null)
                RefreshCardVisual(image, i);
        }
    }

    private void RefreshCardVisual(Image image, int index)
    {
        bool selected = _selectedIndexes.Contains(index);
        image.color = selected ? SelectedCardColor : PanelInnerColor;
    }

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

    private StatBucket GetStats(RewardGachaEncounterMode mode)
    {
        EnsureStats();
        return _stats[mode];
    }

    private void EnsureStats()
    {
        foreach (RewardGachaEncounterMode mode in System.Enum.GetValues(typeof(RewardGachaEncounterMode)))
        {
            if (!_stats.ContainsKey(mode))
                _stats.Add(mode, new StatBucket());
        }
    }

    private void SetStatus(string message)
    {
        if (_statusText != null)
            _statusText.text = message;
    }

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

    private static RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        Image image = MapPrototypeUIFactory.CreateImage(name, parent, color, false);
        return image.rectTransform;
    }

    private static Button CreateSmallButton(Transform parent, string label)
    {
        Button button = MapPrototypeUIFactory.CreateButton(label.Replace(" ", "") + "Button", parent, label, ButtonColor, TextColor, 16);
        MapPrototypeUIFactory.AddLayoutElement(button.gameObject, preferredWidth: 128f, preferredHeight: 42f);
        return button;
    }

    private static TextMeshProUGUI CreatePill(Transform parent, string text, Color color)
    {
        Image image = MapPrototypeUIFactory.CreateImage(text.Replace(" ", "") + "Pill", parent, color, false);
        MapPrototypeUIFactory.AddLayoutElement(image.gameObject, preferredWidth: 142f, preferredHeight: 34f);
        TextMeshProUGUI label = MapPrototypeUIFactory.CreateText("Label", image.transform, text, 14, FontStyles.Bold, TextColor, TextAlignmentOptions.Center);
        MapPrototypeUIFactory.Stretch(label.rectTransform, Vector2.zero, Vector2.zero);
        return label;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }
}

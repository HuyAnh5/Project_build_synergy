using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed partial class RewardGachaDemoController : MonoBehaviour
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

}

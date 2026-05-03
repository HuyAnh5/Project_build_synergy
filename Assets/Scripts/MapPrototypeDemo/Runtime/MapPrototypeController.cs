using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed partial class MapPrototypeController : MonoBehaviour
{
    [Header("Prototype Rules")]
    [SerializeField] private MapPrototypeConfig config = new MapPrototypeConfig();
    [SerializeField] private bool autoGenerateOnStart = true;
    [SerializeField] private bool verboseLogging = true;

    [Header("Runtime UI")]
    [SerializeField] private RectTransform topBar;
    [SerializeField] private RectTransform mapCard;
    [SerializeField] private RectTransform sidebar;
    [SerializeField] private ScrollRect mapScrollRect;
    [SerializeField] private RectTransform mapViewport;
    [SerializeField] private RectTransform mapContent;
    [SerializeField] private RectTransform linesLayer;
    [SerializeField] private RectTransform nodesLayer;

    [SerializeField] private TextMeshProUGUI bossIconText;
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private TextMeshProUGUI bossHintText;
    [SerializeField] private TextMeshProUGUI currentNodeTitleText;
    [SerializeField] private TextMeshProUGUI currentNodeMetaText;
    [SerializeField] private RectTransform statusPillsRoot;
    [SerializeField] private Button startOverButton;
    [SerializeField] private Button hintToggleButton;
    [SerializeField] private TextMeshProUGUI hintToggleLabel;

    [SerializeField] private CanvasGroup modalCanvasGroup;
    [SerializeField] private TextMeshProUGUI modalIconText;
    [SerializeField] private TextMeshProUGUI modalTitleText;
    [SerializeField] private TextMeshProUGUI modalBodyText;
    [SerializeField] private RectTransform modalActionsRoot;

    private MapPrototypeData _map;
    private string _currentNodeId;
    private Dictionary<string, List<string>> _travelOptions = new Dictionary<string, List<string>>();
    private HashSet<string> _safeReachable = new HashSet<string>();
    private HashSet<string> _frontierIds = new HashSet<string>();
    private int _hintsCollected;
    private bool _bossRevealed;
    private bool _showHintNodes;
    private bool _modalLocked;
    private bool _isAnimating;
    private Vector2 _playerPos;
    private RectTransform _playerTokenRect;

    private static readonly Color AppBackground = new Color32(18, 13, 12, 245);
    private static readonly Color PanelColor = new Color32(44, 31, 28, 228);
    private static readonly Color PanelInnerColor = new Color32(56, 40, 35, 234);
    private static readonly Color BorderColor = new Color32(199, 163, 101, 60);
    private static readonly Color InkColor = new Color32(239, 226, 196, 255);
    private static readonly Color MutedColor = new Color32(189, 168, 138, 255);
    private static readonly Color GoldColor = new Color32(216, 170, 95, 255);
    private static readonly Color DangerColor = new Color32(124, 63, 56, 255);
    private static readonly Color EdgeColor = new Color32(211, 184, 126, 90);
    private static readonly Color EdgeReachableColor = new Color32(251, 231, 179, 230);
    private static readonly Color EdgeTraversableColor = new Color32(220, 196, 147, 150);
    private static readonly Color LockedOverlay = new Color(1f, 1f, 1f, 0.42f);
    private static readonly Color ClearedFill = new Color32(28, 21, 20, 210);
    private static readonly Color ClearedRingColor = new Color32(241, 224, 194, 220);
    private static readonly Color AvailableBorderColor = new Color32(248, 221, 161, 220);
    private static readonly Color BacktrackBorderColor = new Color32(173, 147, 104, 160);
    private static readonly Color CurrentBorderColor = new Color32(255, 228, 177, 245);
    private static readonly Color WaitingBorderColor = new Color32(220, 145, 119, 200);
    private static readonly Color DefaultBorderColor = new Color32(230, 199, 139, 60);
    private static readonly Color HintBadgeColor = new Color32(217, 181, 107, 255);
    private static readonly Color HintBadgeTextColor = new Color32(35, 22, 7, 255);

    [ContextMenu("Apply HTML Source-of-Truth Defaults")]
    public void ApplyHtmlSourceOfTruthDefaults()
    {
        if (config == null)
            config = new MapPrototypeConfig();

        config.ApplyHtmlSourceOfTruthDefaults();
    }

    private void Reset()
    {
        ApplyHtmlSourceOfTruthDefaults();

        RectTransform rect = GetComponent<RectTransform>();
        if (rect == null)
            rect = gameObject.AddComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void Awake()
    {
        ApplyHtmlSourceOfTruthDefaults();
        EnsureRuntimeEventSystem();
        EnsureUiHierarchy(false);
        WireButtons();
        LogMap("Awake complete.");
    }

    private void Start()
    {
        if (!autoGenerateOnStart)
            return;

        ResetAct();
    }

    [ContextMenu("Rebuild Prototype UI")]
    public void RebuildPrototypeUi()
    {
        ApplyHtmlSourceOfTruthDefaults();
        EnsureUiHierarchy(true);
        WireButtons();
        RenderHintToggle();
        LogMap("Prototype UI rebuilt.");
    }

    public void ResetAct()
    {
        LogMap("ResetAct requested.");

        try
        {
            ApplyHtmlSourceOfTruthDefaults();
            EnsureUiHierarchy(false);
            WireButtons();

            _map = MapPrototypeGenerator.GenerateAct(config);
            MapPrototypeNodeData startNode = _map.nodes.First(node => node.type == MapPrototypeNodeType.Start);
            _currentNodeId = startNode.id;
            _hintsCollected = 0;
            _bossRevealed = false;
            _modalLocked = false;
            _isAnimating = false;
            _playerPos = new Vector2(startNode.x, startNode.y);

            foreach (MapPrototypeNodeData node in _map.nodes)
            {
                node.visited = node.type == MapPrototypeNodeType.Start;
                node.safeVisited = node.type == MapPrototypeNodeType.Start;
                node.cleared = node.type == MapPrototypeNodeType.Start;
                node.hintTaken = false;
                node.shopHintBought = false;
                node.ranSkipped = false;
            }

            CloseModal();
            ComputeTravelOptions();
            RenderHintToggle();
            RenderAll();
            CenterOnCurrent(true);
            NotifyMapStateChanged();
            LogMap($"Generated map with {_map.nodes.Count} nodes and {_map.edges.Count} edges. Travel options from start: {_travelOptions.Count}.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex, this);
            if (_map != null)
            {
                LogMap("ResetAct failed; keeping the previous map.");
                ComputeTravelOptions();
                RenderHintToggle();
                RenderAll();
                NotifyMapStateChanged();
                return;
            }

            throw;
        }
    }

    private void ToggleHintDebug()
    {
        _showHintNodes = !_showHintNodes;
        RenderHintToggle();
        RenderMap();
        LogMap($"Hint debug toggled: {(_showHintNodes ? "On" : "Off")}.");
    }

    private void WireButtons()
    {
        if (startOverButton != null)
        {
            startOverButton.onClick.RemoveAllListeners();
            startOverButton.onClick.AddListener(() =>
            {
                LogMap("StartOver button clicked.");
                ResetAct();
            });
        }

        if (hintToggleButton != null)
        {
            hintToggleButton.onClick.RemoveAllListeners();
            hintToggleButton.onClick.AddListener(() =>
            {
                LogMap("Hint toggle button clicked.");
                ToggleHintDebug();
            });
        }

        LogMap($"Buttons wired. StartOver={(startOverButton != null)}, HintToggle={(hintToggleButton != null)}.");
    }
}

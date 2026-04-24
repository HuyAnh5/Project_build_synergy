using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

[DisallowMultipleComponent]
public sealed class MapPrototypeController : MonoBehaviour
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

    private sealed class ModalAction
    {
        public string label;
        public bool danger;
        public bool disabled;
        public Action handler;
    }

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

    private void RenderHintToggle()
    {
        if (hintToggleLabel != null)
            hintToggleLabel.text = _showHintNodes ? "On" : "Off";

        if (hintToggleButton == null)
            return;

        Image image = hintToggleButton.GetComponent<Image>();
        if (image != null)
            image.color = _showHintNodes ? new Color32(155, 115, 63, 255) : new Color32(74, 55, 42, 230);
    }

    private void ComputeTravelOptions()
    {
        string originId = _currentNodeId;
        Dictionary<string, string> safePrev = new Dictionary<string, string> { [originId] = null };
        Queue<string> queue = new Queue<string>();
        queue.Enqueue(originId);

        while (queue.Count > 0)
        {
            string currentId = queue.Dequeue();
            if (!_map.adjacency.TryGetValue(currentId, out HashSet<string> neighbors))
                continue;

            foreach (string neighborId in neighbors)
            {
                if (safePrev.ContainsKey(neighborId))
                    continue;
                if (!IsTraversableNode(neighborId, originId))
                    continue;

                safePrev[neighborId] = currentId;
                queue.Enqueue(neighborId);
            }
        }

        _safeReachable = new HashSet<string>(safePrev.Keys);
        Dictionary<string, List<string>> travel = new Dictionary<string, List<string>>();

        foreach (string id in _safeReachable)
        {
            if (id == originId)
                continue;
            travel[id] = ReconstructPath(safePrev, id);
        }

        foreach (string safeId in _safeReachable)
        {
            List<string> basePath = safeId == originId ? new List<string> { originId } : ReconstructPath(safePrev, safeId);
            foreach (string neighborId in _map.adjacency[safeId])
            {
                if (_safeReachable.Contains(neighborId))
                    continue;

                List<string> candidatePath = new List<string>(basePath) { neighborId };
                if (!travel.TryGetValue(neighborId, out List<string> existing) || candidatePath.Count < existing.Count)
                    travel[neighborId] = candidatePath;
            }
        }

        _travelOptions = travel;
        _frontierIds = new HashSet<string>(_travelOptions.Keys.Where(id => !_safeReachable.Contains(id)));
    }

    private bool IsTraversableNode(string id, string originId)
    {
        MapPrototypeNodeData node = MapPrototypeGenerator.GetNodeById(_map, id);
        return id == originId || (node != null && node.safeVisited);
    }

    private static List<string> ReconstructPath(Dictionary<string, string> previousMap, string targetId)
    {
        List<string> path = new List<string>();
        string current = targetId;
        while (current != null)
        {
            path.Add(current);
            previousMap.TryGetValue(current, out current);
        }

        path.Reverse();
        return path;
    }

    private void RenderAll()
    {
        RenderBossPanel();
        RenderStatusPanel();
        RenderMap();
    }

    private void RenderBossPanel()
    {
        MapPrototypeNodeData bossNode = _map.nodes.First(node => node.type == MapPrototypeNodeType.Boss);
        bool revealed = _hintsCollected >= config.bossHintsRequired;
        _bossRevealed = revealed;

        if (bossHintText != null)
            bossHintText.text = $"Boss Hint: {Mathf.Min(_hintsCollected, config.bossHintsRequired)}/{config.bossHintsRequired}";

        if (bossIconText != null)
            bossIconText.text = revealed && bossNode.bossData != null ? bossNode.bossData.badgeText : "?";

        if (bossNameText != null)
            bossNameText.text = revealed && bossNode.bossData != null ? bossNode.bossData.bossName : "Unknown Boss";
    }

    private void RenderStatusPanel()
    {
        MapPrototypeNodeData current = GetCurrentNode();
        if (current == null)
            return;

        if (currentNodeTitleText != null)
        {
            string title = current.type == MapPrototypeNodeType.Boss && _bossRevealed && current.bossData != null
                ? $"{current.bossData.bossName} - Boss"
                : MapPrototypeNodeCatalog.GetLabel(current.type);
            currentNodeTitleText.text = title;
        }

        if (currentNodeMetaText != null)
            currentNodeMetaText.text = GetCurrentNodeDetail(current);

        ClearChildren(statusPillsRoot);
        int intermediateNodeCount = _map != null
            ? _map.nodes.Count(node => node.type != MapPrototypeNodeType.Start && node.type != MapPrototypeNodeType.Boss && !node.specialLeaf)
            : 0;
        List<string> pills = new List<string>
        {
            $"Node Count: {intermediateNodeCount}",
            $"Clickable: {_travelOptions.Count}",
            $"Safe Path Nodes: {Mathf.Max(0, _safeReachable.Count - 1)}"
        };

        foreach (string pill in pills)
            CreatePill(statusPillsRoot, pill);
    }

    private string GetCurrentNodeDetail(MapPrototypeNodeData current)
    {
        string detail = MapPrototypeNodeCatalog.GetDescription(current.type);
        if (current.type == MapPrototypeNodeType.Start)
            return detail;

        if (current.type == MapPrototypeNodeType.Shop && current.safeVisited)
        {
            return current.shopHintBought
                ? "You have already visited this shop and bought its hint. It stays as a safe landmark."
                : "You have visited this shop. It stays safe and you can come back to Buy Hint later.";
        }

        if (current.type == MapPrototypeNodeType.Forge && current.safeVisited)
            return "This forge remains a safe landmark after the first visit.";

        if (current.cleared)
            return "This node has been cleared and now acts like a safe path node.";

        if (MapPrototypeNodeCatalog.IsHostile(current.type) && current.visited)
            return "The enemy here is still alive. Fight clears the node, Run leaves it blocking safe backtrack.";

        if (current.visited)
            return "You have already stepped on this node once.";

        return detail;
    }

    private void RenderMap()
    {
        if (_map == null || mapContent == null || linesLayer == null || nodesLayer == null)
            return;

        mapContent.sizeDelta = new Vector2(config.mapWidth, config.mapHeight);
        ClearChildren(linesLayer);
        ClearChildren(nodesLayer);
        _playerTokenRect = null;

        foreach (MapPrototypeEdgeData edge in _map.edges)
        {
            MapPrototypeNodeData from = MapPrototypeGenerator.GetNodeById(_map, edge.from);
            MapPrototypeNodeData to = MapPrototypeGenerator.GetNodeById(_map, edge.to);
            if (from == null || to == null)
                continue;

            CreateEdgeVisual(from, to);
        }

        foreach (MapPrototypeNodeData node in _map.nodes)
            CreateNodeVisual(node);

        CreatePlayerToken();
    }

    private void CreateEdgeVisual(MapPrototypeNodeData from, MapPrototypeNodeData to)
    {
        Image line = MapPrototypeUIFactory.CreateImage("Edge", linesLayer, GetEdgeColor(from, to), false);
        RectTransform rect = line.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);

        Vector2 start = MapToUiPoint(from.x, from.y);
        Vector2 end = MapToUiPoint(to.x, to.y);
        Vector2 delta = end - start;
        rect.sizeDelta = new Vector2(delta.magnitude, IsReachableEdge(from, to) ? 4.5f : 4f);
        rect.anchoredPosition = start;
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private Color GetEdgeColor(MapPrototypeNodeData from, MapPrototypeNodeData to)
    {
        bool fromSafe = _safeReachable.Contains(from.id);
        bool toSafe = _safeReachable.Contains(to.id);
        bool fromFrontier = _frontierIds.Contains(from.id);
        bool toFrontier = _frontierIds.Contains(to.id);

        if (fromSafe && toSafe)
            return EdgeTraversableColor;
        if ((fromSafe && toFrontier) || (toSafe && fromFrontier))
            return EdgeReachableColor;
        return EdgeColor;
    }

    private bool IsReachableEdge(MapPrototypeNodeData from, MapPrototypeNodeData to)
    {
        bool fromSafe = _safeReachable.Contains(from.id);
        bool toSafe = _safeReachable.Contains(to.id);
        bool fromFrontier = _frontierIds.Contains(from.id);
        bool toFrontier = _frontierIds.Contains(to.id);
        return (fromSafe && toFrontier) || (toSafe && fromFrontier);
    }

    private void CreateNodeVisual(MapPrototypeNodeData node)
    {
        RectTransform anchor = MapPrototypeUIFactory.CreateRect($"Node_{node.id}", nodesLayer);
        anchor.anchorMin = new Vector2(0f, 1f);
        anchor.anchorMax = new Vector2(0f, 1f);
        anchor.pivot = new Vector2(0.5f, 0.5f);
        anchor.sizeDelta = new Vector2(96f, 104f);
        anchor.anchoredPosition = MapToUiPoint(node.x, node.y);

        Image border = MapPrototypeUIFactory.CreateImage("Border", anchor, DefaultBorderColor, false);
        RectTransform borderRect = border.rectTransform;
        borderRect.anchorMin = new Vector2(0.5f, 0.5f);
        borderRect.anchorMax = new Vector2(0.5f, 0.5f);
        borderRect.pivot = new Vector2(0.5f, 0.5f);
        borderRect.sizeDelta = new Vector2(70f, 70f);
        borderRect.anchoredPosition = Vector2.zero;

        Button button = MapPrototypeUIFactory.CreateButton(
            "Button",
            border.transform,
            MapPrototypeNodeCatalog.GetBadge(node.type, _bossRevealed, node.bossData),
            MapPrototypeNodeCatalog.GetFillColor(node.type),
            InkColor,
            20);
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(62f, 62f);
        buttonRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI badgeText = button.GetComponentInChildren<TextMeshProUGUI>();
        badgeText.fontSize = node.type == MapPrototypeNodeType.Boss && _bossRevealed ? 16 : 20;

        TextMeshProUGUI clearedRing = MapPrototypeUIFactory.CreateText("ClearedRing", button.transform, "O", 30, FontStyles.Bold, ClearedRingColor, TextAlignmentOptions.Center);
        MapPrototypeUIFactory.Stretch(clearedRing.rectTransform, Vector2.zero, Vector2.zero);
        clearedRing.gameObject.SetActive(false);

        Image hintBadge = MapPrototypeUIFactory.CreateImage("HintBadge", button.transform, HintBadgeColor, false);
        RectTransform hintRect = hintBadge.rectTransform;
        hintRect.anchorMin = new Vector2(1f, 1f);
        hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.pivot = new Vector2(0.5f, 0.5f);
        hintRect.sizeDelta = new Vector2(18f, 18f);
        hintRect.anchoredPosition = new Vector2(-4f, -4f);

        TextMeshProUGUI hintText = MapPrototypeUIFactory.CreateText("HintText", hintBadge.transform, "H", 12, FontStyles.Bold, HintBadgeTextColor, TextAlignmentOptions.Center);
        MapPrototypeUIFactory.Stretch(hintText.rectTransform, Vector2.zero, Vector2.zero);
        hintBadge.gameObject.SetActive(_showHintNodes && node.hasHint && !node.hintTaken);

        TextMeshProUGUI label = MapPrototypeUIFactory.CreateText(
            "Label",
            anchor,
            node.type == MapPrototypeNodeType.Boss && _bossRevealed && node.bossData != null
                ? node.bossData.bossName
                : MapPrototypeNodeCatalog.GetLabel(node.type),
            15,
            FontStyles.Normal,
            MutedColor,
            TextAlignmentOptions.Center);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0.5f, 1f);
        labelRect.anchorMax = new Vector2(0.5f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.sizeDelta = new Vector2(120f, 28f);
        labelRect.anchoredPosition = new Vector2(0f, -72f);

        ApplyNodeVisualState(node, button, border, badgeText, clearedRing, label);

        bool isClickable = node.id == _currentNodeId || _travelOptions.ContainsKey(node.id);
        button.interactable = isClickable;
        button.onClick.AddListener(() => HandleNodeClicked(node.id));
    }

    private void ApplyNodeVisualState(MapPrototypeNodeData node, Button button, Image border, TextMeshProUGUI badgeText, TextMeshProUGUI clearedRing, TextMeshProUGUI label)
    {
        Image fill = button.GetComponent<Image>();
        fill.color = MapPrototypeNodeCatalog.GetFillColor(node.type);
        badgeText.color = InkColor;
        label.color = MutedColor;
        border.color = DefaultBorderColor;

        if (node.cleared)
        {
            fill.color = ClearedFill;
            badgeText.gameObject.SetActive(false);
            clearedRing.gameObject.SetActive(true);
            border.color = new Color32(229, 207, 165, 100);
        }
        else
        {
            badgeText.gameObject.SetActive(true);
            clearedRing.gameObject.SetActive(false);
        }

        if ((node.type == MapPrototypeNodeType.Shop || node.type == MapPrototypeNodeType.Forge) && node.safeVisited)
            fill.color = Color.Lerp(fill.color, Color.black, 0.08f);

        if (node.id == _currentNodeId)
            border.color = CurrentBorderColor;
        else if (_frontierIds.Contains(node.id))
            border.color = AvailableBorderColor;
        else if (_safeReachable.Contains(node.id))
            border.color = BacktrackBorderColor;

        if (MapPrototypeNodeCatalog.IsHostile(node.type) && node.visited && !node.cleared)
            border.color = WaitingBorderColor;

        bool isClickable = node.id == _currentNodeId || _travelOptions.ContainsKey(node.id);
        if (!isClickable)
        {
            fill.color *= LockedOverlay;
            badgeText.color *= LockedOverlay;
            label.color *= LockedOverlay;
            border.color *= LockedOverlay;
        }
    }

    private void CreatePlayerToken()
    {
        Image bg = MapPrototypeUIFactory.CreateImage("PlayerToken", nodesLayer, new Color32(241, 209, 144, 255), false);
        RectTransform rect = bg.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(28f, 28f);
        bg.raycastTarget = false;

        TextMeshProUGUI token = MapPrototypeUIFactory.CreateText("Label", bg.transform, "P", 20, FontStyles.Bold, new Color32(44, 27, 5, 255), TextAlignmentOptions.Center);
        MapPrototypeUIFactory.Stretch(token.rectTransform, Vector2.zero, Vector2.zero);
        bg.transform.SetAsLastSibling();

        _playerTokenRect = rect;
        UpdatePlayerToken();
        LogMap("Player token created.");
    }

    private void UpdatePlayerToken()
    {
        if (_playerTokenRect == null)
            return;

        _playerTokenRect.anchoredPosition = MapToUiPoint(_playerPos.x, _playerPos.y - 38f);
    }

    private void HandleNodeClicked(string targetId)
    {
        if (_modalLocked || _isAnimating || _map == null)
        {
            LogMap($"Click ignored. modalLocked={_modalLocked}, isAnimating={_isAnimating}, hasMap={_map != null}.");
            return;
        }

        MapPrototypeNodeData current = GetCurrentNode();
        if (current == null)
            return;

        if (targetId == current.id)
        {
            LogMap($"Clicked current node {targetId}.");
            MaybeOpenEncounter(current);
            return;
        }

        if (!_travelOptions.TryGetValue(targetId, out List<string> path))
        {
            LogMap($"Clicked non-travelable node {targetId}.");
            return;
        }

        LogMap($"Clicked travelable node {targetId}. Path length={path.Count}.");
        StartCoroutine(MoveAlongPathRoutine(path));
    }

    private IEnumerator MoveAlongPathRoutine(List<string> path)
    {
        _isAnimating = true;
        CloseModal();
        LogMap($"MoveAlongPath started. Path={string.Join(" -> ", path)}");

        if (path.Count >= 2)
        {
            for (int i = 1; i < path.Count; i++)
            {
                MapPrototypeNodeData from = MapPrototypeGenerator.GetNodeById(_map, path[i - 1]);
                MapPrototypeNodeData to = MapPrototypeGenerator.GetNodeById(_map, path[i]);
                yield return TweenPlayerRoutine(from, to, 0.17f);
            }
        }

        _currentNodeId = path[path.Count - 1];
        MapPrototypeNodeData node = GetCurrentNode();
        _playerPos = new Vector2(node.x, node.y);
        node.visited = true;
        _isAnimating = false;
        ComputeTravelOptions();
        RenderAll();
        CenterOnCurrent(false);
        LogMap($"Arrived at node {node.id} ({node.type}).");
        MaybeOpenEncounter(node);
    }

    private IEnumerator TweenPlayerRoutine(MapPrototypeNodeData fromNode, MapPrototypeNodeData toNode, float duration)
    {
        if (fromNode == null || toNode == null)
            yield break;

        float elapsed = 0f;
        Vector2 from = new Vector2(fromNode.x, fromNode.y);
        Vector2 to = new Vector2(toNode.x, toNode.y);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
            _playerPos = Vector2.LerpUnclamped(from, to, ease);
            UpdatePlayerToken();
            yield return null;
        }

        _playerPos = to;
        UpdatePlayerToken();
    }

    private void MaybeOpenEncounter(MapPrototypeNodeData node)
    {
        LogMap($"MaybeOpenEncounter for node {node.id} ({node.type}). Cleared={node.cleared}, SafeVisited={node.safeVisited}, HasHint={node.hasHint}, HintTaken={node.hintTaken}.");

        if (node.type == MapPrototypeNodeType.Start)
        {
            CloseModal();
            RenderAll();
            return;
        }

        if (MapPrototypeNodeCatalog.IsHostile(node.type) && !node.cleared)
        {
            OpenHostileModal(node);
            return;
        }

        if (node.type == MapPrototypeNodeType.Shop)
        {
            node.safeVisited = true;
            ComputeTravelOptions();
            RenderAll();
            OpenShopModal(node);
            return;
        }

        if (node.type == MapPrototypeNodeType.Forge)
        {
            node.safeVisited = true;
            ComputeTravelOptions();
            RenderAll();
            OpenForgeModal(node);
            return;
        }

        if (!MapPrototypeNodeCatalog.IsHostile(node.type) && !node.safeVisited)
        {
            node.safeVisited = true;
            node.cleared = true;
            CollectHintIfAny(node);
            ComputeTravelOptions();
            RenderAll();
            OpenPassiveModal(node);
            return;
        }

        CloseModal();
        RenderAll();
    }

    private void OpenHostileModal(MapPrototypeNodeData node)
    {
        string title = node.type == MapPrototypeNodeType.Boss
            ? (_bossRevealed && node.bossData != null ? node.bossData.bossName : "Unknown Boss")
            : MapPrototypeNodeCatalog.GetLabel(node.type);

        string body = node.type == MapPrototypeNodeType.Boss && _bossRevealed && node.bossData != null
            ? node.bossData.description
            : MapPrototypeNodeCatalog.GetDescription(node.type);

        if (node.hasHint && !node.hintTaken)
            body += " This node is holding one boss hint if you fight and clear it.";
        if (node.ranSkipped)
            body += " You already ran from this node once, so it still blocks safe backtrack.";

        List<ModalAction> actions = new List<ModalAction>();
        if (node.type != MapPrototypeNodeType.Boss)
        {
            actions.Add(new ModalAction
            {
                label = "Run",
                handler = () => ResolveRun(node)
            });
        }

        actions.Add(new ModalAction
        {
            label = node.type == MapPrototypeNodeType.Boss ? "Fight Boss" : "Fight",
            danger = true,
            handler = () => ResolveFight(node)
        });

        ShowModal(
            MapPrototypeNodeCatalog.GetBadge(node.type, _bossRevealed, node.bossData),
            title,
            body,
            actions);
    }

    private void OpenShopModal(MapPrototypeNodeData node)
    {
        string body = MapPrototypeNodeCatalog.GetDescription(node.type);
        if (node.shopHintBought)
            body += " You already bought the hint from this shop.";
        else if (node.hasHint && !node.hintTaken)
            body += " Hint is not auto-collected here. You must press Buy Hint.";

        ShowModal("SH", "Shop", body, new List<ModalAction>
        {
            new ModalAction
            {
                label = node.shopHintBought ? "Hint Bought" : "Buy Hint",
                disabled = node.shopHintBought,
                handler = () => BuyShopHint(node)
            },
            new ModalAction
            {
                label = "Leave Shop",
                handler = CloseModal
            }
        });
    }

    private void OpenForgeModal(MapPrototypeNodeData node)
    {
        string body = MapPrototypeNodeCatalog.GetDescription(node.type)
            + " Forge does not disappear after the first visit; it becomes a safe landmark.";

        ShowModal("FG", "Forge", body, new List<ModalAction>
        {
            new ModalAction
            {
                label = "Leave Forge",
                handler = CloseModal
            }
        });
    }

    private void OpenPassiveModal(MapPrototypeNodeData node)
    {
        string body = MapPrototypeNodeCatalog.GetDescription(node.type)
            + " After the first visit this node becomes a safe path node.";
        if (node.hasHint && node.hintTaken)
            body += " You collected one boss hint here.";

        ShowModal(
            MapPrototypeNodeCatalog.GetBadge(node.type, false, null),
            MapPrototypeNodeCatalog.GetLabel(node.type),
            body,
            new List<ModalAction>
            {
                new ModalAction
                {
                    label = "Continue",
                    handler = CloseModal
                }
            });
    }

    private void ShowModal(string icon, string title, string body, List<ModalAction> actions)
    {
        _modalLocked = true;
        LogMap($"ShowModal: {title} ({actions.Count} actions).");
        if (modalIconText != null) modalIconText.text = icon;
        if (modalTitleText != null) modalTitleText.text = title;
        if (modalBodyText != null) modalBodyText.text = body;

        ClearChildren(modalActionsRoot);
        foreach (ModalAction action in actions)
        {
            Button button = MapPrototypeUIFactory.CreateButton(
                "ModalButton",
                modalActionsRoot,
                action.label,
                action.danger ? DangerColor : new Color32(109, 78, 50, 235),
                InkColor,
                20);
            MapPrototypeUIFactory.AddLayoutElement(button.gameObject, preferredWidth: 120f, preferredHeight: 40f);
            button.interactable = !action.disabled;
            button.onClick.AddListener(() => action.handler?.Invoke());
        }

        if (modalCanvasGroup != null)
        {
            modalCanvasGroup.alpha = 1f;
            modalCanvasGroup.interactable = true;
            modalCanvasGroup.blocksRaycasts = true;
        }
    }

    private void CloseModal()
    {
        _modalLocked = false;
        if (modalCanvasGroup != null)
        {
            modalCanvasGroup.alpha = 0f;
            modalCanvasGroup.interactable = false;
            modalCanvasGroup.blocksRaycasts = false;
        }

        LogMap("CloseModal.");
    }

    private void ResolveRun(MapPrototypeNodeData node)
    {
        LogMap($"ResolveRun on node {node.id} ({node.type}).");
        node.visited = true;
        node.ranSkipped = true;
        CloseModal();
        ComputeTravelOptions();
        RenderAll();
    }

    private void ResolveFight(MapPrototypeNodeData node)
    {
        LogMap($"ResolveFight on node {node.id} ({node.type}).");
        node.visited = true;
        node.safeVisited = true;
        node.cleared = true;
        node.ranSkipped = false;
        CollectHintIfAny(node);
        CloseModal();
        ComputeTravelOptions();
        RenderAll();

        if (node.type == MapPrototypeNodeType.Boss)
        {
            ShowModal("CL", "Act Clear", "Prototype ends here. You cleared the boss node for this act.", new List<ModalAction>
            {
                new ModalAction { label = "Close", handler = CloseModal },
                new ModalAction { label = "Start Over", danger = true, handler = ResetAct }
            });
        }
    }

    private void CollectHintIfAny(MapPrototypeNodeData node)
    {
        if (!node.hasHint || node.hintTaken)
            return;

        node.hintTaken = true;
        _hintsCollected = Mathf.Min(config.bossHintsRequired, _hintsCollected + 1);
    }

    private void BuyShopHint(MapPrototypeNodeData node)
    {
        if (node.shopHintBought)
        {
            LogMap($"BuyShopHint ignored; already bought on node {node.id}.");
            return;
        }

        node.shopHintBought = true;
        CollectHintIfAny(node);
        ComputeTravelOptions();
        RenderAll();
        OpenShopModal(node);
        LogMap($"Bought shop hint on node {node.id}. Total hints={_hintsCollected}.");
    }

    private void CenterOnCurrent(bool instant)
    {
        if (mapScrollRect == null || mapContent == null || mapViewport == null)
            return;

        MapPrototypeNodeData current = GetCurrentNode();
        if (current == null)
            return;

        float targetTop = Mathf.Max(0f, current.y - mapViewport.rect.height * 0.62f);
        float maxTop = Mathf.Max(0f, mapContent.rect.height - mapViewport.rect.height);
        targetTop = Mathf.Clamp(targetTop, 0f, maxTop);

        if (instant)
        {
            Vector2 anchored = mapContent.anchoredPosition;
            anchored.y = targetTop;
            mapContent.anchoredPosition = anchored;
            return;
        }

        StopCoroutine(nameof(SmoothCenterRoutine));
        StartCoroutine(SmoothCenterRoutine(targetTop));
    }

    private IEnumerator SmoothCenterRoutine(float targetTop)
    {
        float duration = 0.18f;
        float elapsed = 0f;
        float start = mapContent.anchoredPosition.y;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
            Vector2 anchored = mapContent.anchoredPosition;
            anchored.y = Mathf.Lerp(start, targetTop, eased);
            mapContent.anchoredPosition = anchored;
            yield return null;
        }

        Vector2 final = mapContent.anchoredPosition;
        final.y = targetTop;
        mapContent.anchoredPosition = final;
    }

    private MapPrototypeNodeData GetCurrentNode()
    {
        return _map == null ? null : MapPrototypeGenerator.GetNodeById(_map, _currentNodeId);
    }

    private static Vector2 MapToUiPoint(float x, float y)
    {
        return new Vector2(x, -y);
    }

    private void CreatePill(Transform parent, string text)
    {
        Image bg = MapPrototypeUIFactory.CreateImage("Pill", parent, new Color(0.29f, 0.22f, 0.18f, 0.42f), false);
        bg.rectTransform.sizeDelta = new Vector2(130f, 24f);
        MapPrototypeUIFactory.AddLayoutElement(bg.gameObject, preferredWidth: 130f, preferredHeight: 24f);
        TextMeshProUGUI label = MapPrototypeUIFactory.CreateText("Text", bg.transform, text, 12, FontStyles.Normal, InkColor, TextAlignmentOptions.Center);
        MapPrototypeUIFactory.Stretch(label.rectTransform, Vector2.zero, Vector2.zero);
    }

    private void EnsureUiHierarchy(bool forceRebuild)
    {
        RectTransform root = GetComponent<RectTransform>();
        if (root == null)
            root = gameObject.AddComponent<RectTransform>();

        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        if (forceRebuild)
        {
            ClearChildren(root);
            topBar = null;
            mapCard = null;
            sidebar = null;
            mapScrollRect = null;
            mapViewport = null;
            mapContent = null;
            linesLayer = null;
            nodesLayer = null;
            bossIconText = null;
            bossNameText = null;
            bossHintText = null;
            currentNodeTitleText = null;
            currentNodeMetaText = null;
            statusPillsRoot = null;
            startOverButton = null;
            hintToggleButton = null;
            hintToggleLabel = null;
            modalCanvasGroup = null;
            modalIconText = null;
            modalTitleText = null;
            modalBodyText = null;
            modalActionsRoot = null;
        }

        Image background = GetComponent<Image>();
        if (background == null)
            background = gameObject.AddComponent<Image>();
        background.color = AppBackground;

        if (topBar != null && mapCard != null && sidebar != null && mapScrollRect != null && modalCanvasGroup != null)
            return;

        BuildStaticUi(root);
        LogMap("Static UI hierarchy built.");
    }

    private void EnsureRuntimeEventSystem()
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject go = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem = go.GetComponent<EventSystem>();
            LogMap("Created runtime EventSystem.");
        }

#if ENABLE_INPUT_SYSTEM
        InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
        {
            inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            LogMap("Added InputSystemUIInputModule.");
        }
        inputModule.enabled = true;

        StandaloneInputModule standaloneInput = eventSystem.GetComponent<StandaloneInputModule>();
        if (standaloneInput != null)
            standaloneInput.enabled = false;
#else
        StandaloneInputModule standaloneInput = eventSystem.GetComponent<StandaloneInputModule>();
        if (standaloneInput == null)
        {
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            LogMap("Added StandaloneInputModule.");
        }
        else
        {
            standaloneInput.enabled = true;
        }
#endif
    }

    private void LogMap(string message)
    {
        Debug.Log($"[MapPrototype] {message}", this);
    }

    private void BuildStaticUi(RectTransform root)
    {
        topBar = MapPrototypeUIFactory.CreateRect("TopBar", root);
        MapPrototypeUIFactory.SetTopStretch(topBar, 110f, 16f, 16f, 16f);
        Image topBarBg = topBar.gameObject.AddComponent<Image>();
        topBarBg.color = PanelColor;
        HorizontalLayoutGroup topLayout = topBar.gameObject.AddComponent<HorizontalLayoutGroup>();
        MapPrototypeUIFactory.ConfigureLayoutGroup(topLayout, 16f, new RectOffset(16, 16, 14, 14), true, true, false, false);

        RectTransform titlePanel = MapPrototypeUIFactory.CreateRect("TitlePanel", topBar);
        MapPrototypeUIFactory.AddLayoutElement(titlePanel.gameObject, flexibleWidth: 1f, preferredHeight: 82f);
        VerticalLayoutGroup titleLayout = titlePanel.gameObject.AddComponent<VerticalLayoutGroup>();
        MapPrototypeUIFactory.ConfigureLayoutGroup(titleLayout, 6f, new RectOffset(0, 0, 0, 0), true, true, true, false);
        TextMeshProUGUI title = MapPrototypeUIFactory.CreateText("Title", titlePanel, "Act Map Prototype", 28, FontStyles.Bold, InkColor, TextAlignmentOptions.Left);
        MapPrototypeUIFactory.AddLayoutElement(title.gameObject, preferredHeight: 34f);
        TextMeshProUGUI subtitle = MapPrototypeUIFactory.CreateText(
            "Subtitle",
            titlePanel,
            "STS-like act graph with split/merge paths, backtrack on safe routes, persistent Shop and Forge, and boss intel hints.",
            16,
            FontStyles.Normal,
            MutedColor,
            TextAlignmentOptions.Left);
        MapPrototypeUIFactory.AddLayoutElement(subtitle.gameObject, flexibleHeight: 1f);

        RectTransform bossPanel = MapPrototypeUIFactory.CreateRect("BossPanel", topBar);
        MapPrototypeUIFactory.AddLayoutElement(bossPanel.gameObject, preferredWidth: 360f, preferredHeight: 82f);
        Image bossPanelBg = bossPanel.gameObject.AddComponent<Image>();
        bossPanelBg.color = PanelInnerColor;
        HorizontalLayoutGroup bossLayout = bossPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
        MapPrototypeUIFactory.ConfigureLayoutGroup(bossLayout, 12f, new RectOffset(14, 14, 10, 10), true, true, false, false);

        Image bossIconBg = MapPrototypeUIFactory.CreateImage("BossIconBg", bossPanel, new Color32(24, 18, 17, 242), false);
        RectTransform bossIconBgRect = bossIconBg.rectTransform;
        MapPrototypeUIFactory.AddLayoutElement(bossIconBg.gameObject, preferredWidth: 62f, preferredHeight: 62f);
        bossIconText = MapPrototypeUIFactory.CreateText("BossIcon", bossIconBg.transform, "?", 24, FontStyles.Bold, InkColor, TextAlignmentOptions.Center);
        MapPrototypeUIFactory.Stretch(bossIconText.rectTransform, Vector2.zero, Vector2.zero);

        RectTransform bossInfo = MapPrototypeUIFactory.CreateRect("BossInfo", bossPanel);
        MapPrototypeUIFactory.AddLayoutElement(bossInfo.gameObject, flexibleWidth: 1f);
        VerticalLayoutGroup bossInfoLayout = bossInfo.gameObject.AddComponent<VerticalLayoutGroup>();
        MapPrototypeUIFactory.ConfigureLayoutGroup(bossInfoLayout, 4f, new RectOffset(0, 0, 0, 0), true, true, true, false);
        bossNameText = MapPrototypeUIFactory.CreateText("BossName", bossInfo, "Unknown Boss", 18, FontStyles.Bold, InkColor, TextAlignmentOptions.Left);
        bossHintText = MapPrototypeUIFactory.CreateText("BossHint", bossInfo, "Boss Hint: 0/3", 15, FontStyles.Normal, MutedColor, TextAlignmentOptions.Left);
        TextMeshProUGUI bossMini = MapPrototypeUIFactory.CreateText(
            "BossMini",
            bossInfo,
            "Shop is placed on an early side branch. Forge is placed late and both remain on the map after visiting.",
            13,
            FontStyles.Normal,
            MutedColor,
            TextAlignmentOptions.Left);

        RectTransform actionsPanel = MapPrototypeUIFactory.CreateRect("ActionsPanel", topBar);
        MapPrototypeUIFactory.AddLayoutElement(actionsPanel.gameObject, preferredWidth: 150f, preferredHeight: 82f);
        VerticalLayoutGroup actionsLayout = actionsPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        MapPrototypeUIFactory.ConfigureLayoutGroup(actionsLayout, 8f, new RectOffset(0, 0, 0, 0), true, true, true, false);
        startOverButton = MapPrototypeUIFactory.CreateButton("StartOverButton", actionsPanel, "Start Over", DangerColor, InkColor, 18);
        MapPrototypeUIFactory.AddLayoutElement(startOverButton.gameObject, preferredHeight: 44f);

        mapCard = MapPrototypeUIFactory.CreateRect("MapCard", root);
        mapCard.anchorMin = Vector2.zero;
        mapCard.anchorMax = Vector2.one;
        mapCard.offsetMin = new Vector2(16f, 16f);
        mapCard.offsetMax = new Vector2(-332f, -134f);
        Image mapCardBg = mapCard.gameObject.AddComponent<Image>();
        mapCardBg.color = PanelColor;

        sidebar = MapPrototypeUIFactory.CreateRect("Sidebar", root);
        sidebar.anchorMin = new Vector2(1f, 0f);
        sidebar.anchorMax = new Vector2(1f, 1f);
        sidebar.pivot = new Vector2(1f, 1f);
        sidebar.sizeDelta = new Vector2(300f, 0f);
        sidebar.anchoredPosition = new Vector2(-16f, -134f);
        sidebar.offsetMin = new Vector2(-300f, 16f);
        sidebar.offsetMax = new Vector2(0f, -134f);
        VerticalLayoutGroup sidebarLayout = sidebar.gameObject.AddComponent<VerticalLayoutGroup>();
        MapPrototypeUIFactory.ConfigureLayoutGroup(sidebarLayout, 14f, new RectOffset(0, 0, 0, 0), true, false, false, false);

        BuildMapViewport(mapCard);
        BuildSidebar(sidebar);
        BuildModal(root);
    }

    private void BuildMapViewport(RectTransform parent)
    {
        RectTransform frame = MapPrototypeUIFactory.CreateRect("ViewportFrame", parent);
        MapPrototypeUIFactory.SetStretch(frame, 12f, 12f, 12f, 12f);

        Image viewportImage = MapPrototypeUIFactory.CreateImage("Viewport", frame, new Color(0.13f, 0.1f, 0.1f, 0.7f), true);
        mapViewport = viewportImage.rectTransform;
        MapPrototypeUIFactory.Stretch(mapViewport, Vector2.zero, Vector2.zero);
        mapViewport.gameObject.AddComponent<RectMask2D>();

        mapScrollRect = mapViewport.gameObject.AddComponent<ScrollRect>();
        mapScrollRect.horizontal = false;
        mapScrollRect.vertical = true;
        mapScrollRect.movementType = ScrollRect.MovementType.Clamped;
        mapScrollRect.scrollSensitivity = 24f;

        mapContent = MapPrototypeUIFactory.CreateRect("Content", mapViewport);
        mapContent.anchorMin = new Vector2(0f, 1f);
        mapContent.anchorMax = new Vector2(0f, 1f);
        mapContent.pivot = new Vector2(0f, 1f);
        mapContent.sizeDelta = new Vector2(config.mapWidth, config.mapHeight);
        mapContent.anchoredPosition = Vector2.zero;

        linesLayer = MapPrototypeUIFactory.CreateRect("LinesLayer", mapContent);
        linesLayer.anchorMin = new Vector2(0f, 1f);
        linesLayer.anchorMax = new Vector2(0f, 1f);
        linesLayer.pivot = new Vector2(0f, 1f);
        linesLayer.sizeDelta = mapContent.sizeDelta;
        linesLayer.anchoredPosition = Vector2.zero;

        nodesLayer = MapPrototypeUIFactory.CreateRect("NodesLayer", mapContent);
        nodesLayer.anchorMin = new Vector2(0f, 1f);
        nodesLayer.anchorMax = new Vector2(0f, 1f);
        nodesLayer.pivot = new Vector2(0f, 1f);
        nodesLayer.sizeDelta = mapContent.sizeDelta;
        nodesLayer.anchoredPosition = Vector2.zero;

        mapScrollRect.viewport = mapViewport;
        mapScrollRect.content = mapContent;
    }

    private void BuildSidebar(RectTransform parent)
    {
        RectTransform statusPanel = CreateSidebarPanel(parent, 250f);
        currentNodeTitleText = MapPrototypeUIFactory.CreateText("CurrentNodeTitle", statusPanel, "Start", 20, FontStyles.Bold, InkColor, TextAlignmentOptions.Left);
        MapPrototypeUIFactory.AddLayoutElement(currentNodeTitleText.gameObject, preferredHeight: 28f);
        currentNodeMetaText = MapPrototypeUIFactory.CreateText("CurrentNodeMeta", statusPanel, "Choose a route to begin climbing the map.", 14, FontStyles.Normal, MutedColor, TextAlignmentOptions.Left);
        MapPrototypeUIFactory.AddLayoutElement(currentNodeMetaText.gameObject, preferredHeight: 96f);

        statusPillsRoot = MapPrototypeUIFactory.CreateRect("StatusPills", statusPanel);
        GridLayoutGroup pillsGrid = statusPillsRoot.gameObject.AddComponent<GridLayoutGroup>();
        pillsGrid.cellSize = new Vector2(130f, 24f);
        pillsGrid.spacing = new Vector2(8f, 8f);
        pillsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        pillsGrid.constraintCount = 2;
        MapPrototypeUIFactory.AddLayoutElement(statusPillsRoot.gameObject, preferredHeight: 88f);

        TextMeshProUGUI statusFoot = MapPrototypeUIFactory.CreateText(
            "StatusFooter",
            statusPanel,
            "Event and Rest are one-shot nodes. Shop and Forge remain as landmarks. Combat nodes keep their enemy until Fight clears them.",
            12,
            FontStyles.Normal,
            MutedColor,
            TextAlignmentOptions.Left);

        RectTransform legendPanel = CreateSidebarPanel(parent, 220f);
        CreateSectionTitle(legendPanel, "Legend");
        CreateLegendItem(legendPanel, MapPrototypeNodeCatalog.GetFillColor(MapPrototypeNodeType.Combat), "Combat");
        CreateLegendItem(legendPanel, MapPrototypeNodeCatalog.GetFillColor(MapPrototypeNodeType.Elite), "Elite");
        CreateLegendItem(legendPanel, MapPrototypeNodeCatalog.GetFillColor(MapPrototypeNodeType.Event), "Event");
        CreateLegendItem(legendPanel, MapPrototypeNodeCatalog.GetFillColor(MapPrototypeNodeType.Shop), "Shop");
        CreateLegendItem(legendPanel, MapPrototypeNodeCatalog.GetFillColor(MapPrototypeNodeType.Rest), "Rest");
        CreateLegendItem(legendPanel, MapPrototypeNodeCatalog.GetFillColor(MapPrototypeNodeType.Forge), "Forge / Hub");
        CreateLegendItem(legendPanel, MapPrototypeNodeCatalog.GetFillColor(MapPrototypeNodeType.Boss), "Boss");

        RectTransform debugPanel = CreateSidebarPanel(parent, 120f);
        CreateSectionTitle(debugPanel, "Debug");
        TextMeshProUGUI debugLabel = MapPrototypeUIFactory.CreateText("DebugLabel", debugPanel, "Show Hint Nodes", 16, FontStyles.Bold, InkColor, TextAlignmentOptions.Left);
        TextMeshProUGUI debugMini = MapPrototypeUIFactory.CreateText("DebugMini", debugPanel, "Default is Off. Toggle it to reveal where hint sources were placed on the map.", 12, FontStyles.Normal, MutedColor, TextAlignmentOptions.Left);
        hintToggleButton = MapPrototypeUIFactory.CreateButton("HintToggleButton", debugPanel, "Off", new Color32(74, 55, 42, 230), InkColor, 18);
        MapPrototypeUIFactory.AddLayoutElement(hintToggleButton.gameObject, preferredWidth: 96f, preferredHeight: 36f);
        hintToggleLabel = hintToggleButton.GetComponentInChildren<TextMeshProUGUI>();

        RectTransform rulesPanel = CreateSidebarPanel(parent, 150f);
        CreateSectionTitle(rulesPanel, "Movement Rules");
        CreateMiniLine(rulesPanel, "Move to highlighted adjacent nodes.");
        CreateMiniLine(rulesPanel, "Going up opens new route choices.");
        CreateMiniLine(rulesPanel, "Going down only uses already safe paths.");
        CreateMiniLine(rulesPanel, "Cleared nodes become empty travel nodes.");
    }

    private RectTransform CreateSidebarPanel(RectTransform parent, float preferredHeight)
    {
        Image panel = MapPrototypeUIFactory.CreateImage("Panel", parent, PanelColor, false);
        RectTransform rect = panel.rectTransform;
        MapPrototypeUIFactory.AddLayoutElement(rect.gameObject, preferredHeight: preferredHeight, flexibleWidth: 1f);
        VerticalLayoutGroup layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
        MapPrototypeUIFactory.ConfigureLayoutGroup(layout, 8f, new RectOffset(14, 14, 14, 14), true, false, true, false);
        return rect;
    }

    private void CreateSectionTitle(RectTransform parent, string title)
    {
        TextMeshProUGUI label = MapPrototypeUIFactory.CreateText("SectionTitle", parent, title, 18, FontStyles.Bold, InkColor, TextAlignmentOptions.Left);
        MapPrototypeUIFactory.AddLayoutElement(label.gameObject, preferredHeight: 24f);
    }

    private void CreateLegendItem(RectTransform parent, Color color, string label)
    {
        RectTransform row = MapPrototypeUIFactory.CreateRect("LegendItem", parent);
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        MapPrototypeUIFactory.ConfigureLayoutGroup(layout, 10f, new RectOffset(0, 0, 0, 0), false, true, false, false);
        MapPrototypeUIFactory.AddLayoutElement(row.gameObject, preferredHeight: 22f);

        Image dot = MapPrototypeUIFactory.CreateImage("Dot", row, color, false);
        MapPrototypeUIFactory.AddLayoutElement(dot.gameObject, preferredWidth: 18f, preferredHeight: 18f);
        TextMeshProUGUI text = MapPrototypeUIFactory.CreateText("Label", row, label, 14, FontStyles.Normal, InkColor, TextAlignmentOptions.Left);
        MapPrototypeUIFactory.AddLayoutElement(text.gameObject, flexibleWidth: 1f, preferredHeight: 20f);
    }

    private void CreateMiniLine(RectTransform parent, string text)
    {
        TextMeshProUGUI label = MapPrototypeUIFactory.CreateText("Mini", parent, text, 12, FontStyles.Normal, MutedColor, TextAlignmentOptions.Left);
        MapPrototypeUIFactory.AddLayoutElement(label.gameObject, preferredHeight: 20f);
    }

    private void BuildModal(RectTransform root)
    {
        RectTransform overlay = MapPrototypeUIFactory.CreateRect("ModalOverlay", root);
        MapPrototypeUIFactory.Stretch(overlay, Vector2.zero, Vector2.zero);
        Image overlayImage = overlay.gameObject.AddComponent<Image>();
        overlayImage.color = new Color(0.05f, 0.04f, 0.05f, 0.58f);
        modalCanvasGroup = overlay.gameObject.AddComponent<CanvasGroup>();
        modalCanvasGroup.alpha = 0f;
        modalCanvasGroup.interactable = false;
        modalCanvasGroup.blocksRaycasts = false;

        RectTransform modalPanel = MapPrototypeUIFactory.CreateRect("ModalPanel", overlay);
        modalPanel.anchorMin = new Vector2(0.5f, 0.5f);
        modalPanel.anchorMax = new Vector2(0.5f, 0.5f);
        modalPanel.pivot = new Vector2(0.5f, 0.5f);
        modalPanel.sizeDelta = new Vector2(520f, 340f);
        Image modalBg = modalPanel.gameObject.AddComponent<Image>();
        modalBg.color = PanelInnerColor;
        VerticalLayoutGroup layout = modalPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        MapPrototypeUIFactory.ConfigureLayoutGroup(layout, 12f, new RectOffset(18, 18, 18, 18), true, false, true, false);

        Image iconBg = MapPrototypeUIFactory.CreateImage("ModalIconBg", modalPanel, new Color32(30, 23, 20, 225), false);
        MapPrototypeUIFactory.AddLayoutElement(iconBg.gameObject, preferredWidth: 68f, preferredHeight: 68f);
        modalIconText = MapPrototypeUIFactory.CreateText("ModalIcon", iconBg.transform, "C", 24, FontStyles.Bold, InkColor, TextAlignmentOptions.Center);
        MapPrototypeUIFactory.Stretch(modalIconText.rectTransform, Vector2.zero, Vector2.zero);

        modalTitleText = MapPrototypeUIFactory.CreateText("ModalTitle", modalPanel, "Encounter", 24, FontStyles.Bold, InkColor, TextAlignmentOptions.Left);
        MapPrototypeUIFactory.AddLayoutElement(modalTitleText.gameObject, preferredHeight: 30f);
        modalBodyText = MapPrototypeUIFactory.CreateText("ModalBody", modalPanel, "...", 15, FontStyles.Normal, new Color32(216, 198, 165, 255), TextAlignmentOptions.TopLeft);
        MapPrototypeUIFactory.AddLayoutElement(modalBodyText.gameObject, flexibleHeight: 1f, preferredHeight: 140f);

        modalActionsRoot = MapPrototypeUIFactory.CreateRect("ModalActions", modalPanel);
        HorizontalLayoutGroup actionsLayout = modalActionsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        MapPrototypeUIFactory.ConfigureLayoutGroup(actionsLayout, 10f, new RectOffset(0, 0, 0, 0), true, true, false, false);
        actionsLayout.childAlignment = TextAnchor.MiddleRight;
        MapPrototypeUIFactory.AddLayoutElement(modalActionsRoot.gameObject, preferredHeight: 44f);
    }

    private void ClearChildren(Transform parent)
    {
        if (parent == null)
            return;

        List<GameObject> detached = new List<GameObject>();
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            child.SetParent(null, false);
            detached.Add(child.gameObject);
        }

        for (int i = 0; i < detached.Count; i++)
        {
            if (Application.isPlaying)
                Destroy(detached[i]);
            else
                DestroyImmediate(detached[i]);
        }
    }
}

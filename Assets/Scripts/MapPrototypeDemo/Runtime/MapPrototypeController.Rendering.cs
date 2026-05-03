using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class MapPrototypeController
{
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

    private void CreatePill(Transform parent, string text)
    {
        Image bg = MapPrototypeUIFactory.CreateImage("Pill", parent, new Color(0.29f, 0.22f, 0.18f, 0.42f), false);
        bg.rectTransform.sizeDelta = new Vector2(130f, 24f);
        MapPrototypeUIFactory.AddLayoutElement(bg.gameObject, preferredWidth: 130f, preferredHeight: 24f);
        TextMeshProUGUI label = MapPrototypeUIFactory.CreateText("Text", bg.transform, text, 12, FontStyles.Normal, InkColor, TextAlignmentOptions.Center);
        MapPrototypeUIFactory.Stretch(label.rectTransform, Vector2.zero, Vector2.zero);
    }
}

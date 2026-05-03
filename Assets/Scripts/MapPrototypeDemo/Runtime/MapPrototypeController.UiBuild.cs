using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public sealed partial class MapPrototypeController
{
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

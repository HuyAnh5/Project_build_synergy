using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class ConsumableUiSetupTool
{
    private const string RootName = "ConsumableHudRoot";

    [MenuItem("Tools/Build Synergy/Setup Consumable HUD")]
    public static void SetupConsumableHud()
    {
        Canvas canvas = FindOrCreateCanvas();
        EnsureEventSystem();

        GameObject root = FindOrCreateChild(canvas.transform, RootName, typeof(RectTransform));
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.5f, 0f);
        rootRt.anchorMax = new Vector2(0.5f, 0f);
        rootRt.pivot = new Vector2(0.5f, 0f);
        rootRt.anchoredPosition = new Vector2(0f, 32f);
        rootRt.sizeDelta = new Vector2(520f, 180f);

        ConsumableBarUIManager manager = root.GetComponent<ConsumableBarUIManager>();
        if (manager == null)
            manager = Undo.AddComponent<ConsumableBarUIManager>(root);

        RectTransform dragLayer = BuildOrGetDragLayer(canvas.transform);

        GameObject row = FindOrCreateChild(root.transform, "ConsumableRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0.5f, 0f);
        rowRt.anchorMax = new Vector2(0.5f, 0f);
        rowRt.pivot = new Vector2(0.5f, 0f);
        rowRt.anchoredPosition = new Vector2(0f, 0f);
        rowRt.sizeDelta = new Vector2(520f, 140f);

        HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        rowLayout.enabled = false;
        rowLayout.spacing = 16f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        ContentSizeFitter rowFitter = row.GetComponent<ContentSizeFitter>();
        rowFitter.enabled = false;
        rowFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        manager.layoutContainer = rowRt;
        manager.slots = new ConsumableBarUIManager.ConsumableSlotView[RunInventoryManager.DEFAULT_CONSUMABLE_CAPACITY];
        for (int i = 0; i < RunInventoryManager.DEFAULT_CONSUMABLE_CAPACITY; i++)
            manager.slots[i] = BuildOrGetSlot(row.transform, i, manager);

        RectTransform actionPanel = BuildOrGetActionPanel(root.transform, manager, out Button useButton, out TMP_Text useLabel, out Button sellButton, out TMP_Text sellLabel);
        RectTransform tooltip = BuildOrGetTooltip(root.transform, out TMP_Text tooltipTitle, out TMP_Text tooltipBody);

        manager.actionPanelRoot = actionPanel;
        manager.useButton = useButton;
        manager.useButtonText = useLabel;
        manager.sellButton = sellButton;
        manager.sellButtonText = sellLabel;
        manager.tooltipRoot = tooltip;
        manager.tooltipTitleText = tooltipTitle;
        manager.tooltipBodyText = tooltipBody;
        manager.dragLayer = dragLayer;
        manager.cardSize = new Vector2(96f, 128f);
        manager.relaxedSpacing = 112f;
        manager.minStackedSpacing = 42f;
        manager.fallbackRowWidth = 520f;
        manager.autoCreateMissingCards = true;
        manager.runInventory = Object.FindObjectOfType<RunInventoryManager>(true);
        manager.turnManager = Object.FindObjectOfType<TurnManager>(true);
        manager.combatHud = Object.FindObjectOfType<CombatHUD>(true);
        if (manager.turnManager != null)
            manager.player = manager.turnManager.player;
        else if (manager.combatHud != null)
            manager.player = manager.combatHud.player;

        manager.RebindUi();

        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = root;
    }

    private static ConsumableBarUIManager.ConsumableSlotView BuildOrGetSlot(Transform parent, int index, ConsumableBarUIManager manager)
    {
        GameObject slotGo = FindOrCreateChild(parent, $"Slot{index + 1}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(ConsumableSlotInteractionProxy));
        RectTransform slotRt = slotGo.GetComponent<RectTransform>();
        slotRt.sizeDelta = new Vector2(96f, 128f);

        LayoutElement slotLayout = slotGo.GetComponent<LayoutElement>();
        slotLayout.preferredWidth = 96f;
        slotLayout.preferredHeight = 128f;

        Image background = slotGo.GetComponent<Image>();
        background.color = new Color(0.16f, 0.19f, 0.25f, 0.96f);

        Button button = slotGo.GetComponent<Button>();
        button.targetGraphic = background;

        ConsumableSlotInteractionProxy proxy = slotGo.GetComponent<ConsumableSlotInteractionProxy>();
        proxy.manager = manager;
        proxy.slotIndex = index;

        VerticalLayoutGroup layout = slotGo.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = Undo.AddComponent<VerticalLayoutGroup>(slotGo);
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        GameObject iconGo = FindOrCreateChild(slotGo.transform, "Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        LayoutElement iconLayout = iconGo.GetComponent<LayoutElement>();
        iconLayout.preferredWidth = 72f;
        iconLayout.preferredHeight = 72f;
        Image icon = iconGo.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.color = new Color(1f, 1f, 1f, 0f);

        TMP_Text titleText = GetOrCreateText(slotGo.transform, "Title", string.Empty, 14, FontStyles.Bold, TextAlignmentOptions.Center);
        TMP_Text chargesText = GetOrCreateText(slotGo.transform, "Charges", string.Empty, 13, FontStyles.Normal, TextAlignmentOptions.Center);

        return new ConsumableBarUIManager.ConsumableSlotView
        {
            root = slotRt,
            button = button,
            background = background,
            icon = icon,
            titleText = titleText,
            chargesText = chargesText,
            interactionProxy = proxy
        };
    }

    private static RectTransform BuildOrGetActionPanel(Transform parent, ConsumableBarUIManager manager, out Button useButton, out TMP_Text useLabel, out Button sellButton, out TMP_Text sellLabel)
    {
        GameObject panelGo = FindOrCreateChild(parent, "ActionPanel", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(Image));
        RectTransform panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0f);
        panelRt.anchorMax = new Vector2(0.5f, 0f);
        panelRt.pivot = new Vector2(0f, 0.5f);
        panelRt.sizeDelta = new Vector2(100f, 96f);

        Image panelImage = panelGo.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.09f, 0.12f, 0.94f);

        VerticalLayoutGroup layout = panelGo.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        useButton = BuildOrGetActionButton(panelGo.transform, "UseButton", "USE", new Color(0.73f, 0.18f, 0.18f, 1f), out useLabel);
        sellButton = BuildOrGetActionButton(panelGo.transform, "SellButton", "SELL", new Color(0.17f, 0.7f, 0.54f, 1f), out sellLabel);
        panelGo.SetActive(false);
        return panelRt;
    }

    private static Button BuildOrGetActionButton(Transform parent, string name, string labelText, Color color, out TMP_Text label)
    {
        GameObject buttonGo = FindOrCreateChild(parent, name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        LayoutElement layout = buttonGo.GetComponent<LayoutElement>();
        layout.preferredWidth = 88f;
        layout.preferredHeight = 34f;

        Image image = buttonGo.GetComponent<Image>();
        image.color = color;

        Button button = buttonGo.GetComponent<Button>();
        button.targetGraphic = image;

        label = GetOrCreateText(buttonGo.transform, "Label", labelText, 18, FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform labelRt = label.rectTransform;
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        return button;
    }

    private static RectTransform BuildOrGetTooltip(Transform parent, out TMP_Text title, out TMP_Text body)
    {
        GameObject tooltipGo = FindOrCreateChild(parent, "Tooltip", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        RectTransform tooltipRt = tooltipGo.GetComponent<RectTransform>();
        tooltipRt.anchorMin = new Vector2(0.5f, 0f);
        tooltipRt.anchorMax = new Vector2(0.5f, 0f);
        tooltipRt.pivot = new Vector2(0.5f, 1f);
        tooltipRt.sizeDelta = new Vector2(240f, 120f);

        Image tooltipImage = tooltipGo.GetComponent<Image>();
        tooltipImage.color = new Color(0.08f, 0.1f, 0.14f, 0.95f);
        tooltipImage.raycastTarget = false;

        VerticalLayoutGroup layout = tooltipGo.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        title = GetOrCreateText(tooltipGo.transform, "Title", "Consumable", 18, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        body = GetOrCreateText(tooltipGo.transform, "Body", "Description", 15, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        title.raycastTarget = false;
        body.raycastTarget = false;
        tooltipGo.SetActive(false);
        return tooltipRt;
    }

    private static RectTransform BuildOrGetDragLayer(Transform parent)
    {
        GameObject layerGo = FindOrCreateChild(parent, "ConsumableDragLayer", typeof(RectTransform));
        RectTransform layerRt = layerGo.GetComponent<RectTransform>();
        layerRt.anchorMin = Vector2.zero;
        layerRt.anchorMax = Vector2.one;
        layerRt.offsetMin = Vector2.zero;
        layerRt.offsetMax = Vector2.zero;
        layerRt.SetAsLastSibling();
        return layerRt;
    }

    private static TMP_Text GetOrCreateText(Transform parent, string name, string defaultText, float fontSize, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject textGo = FindOrCreateChild(parent, name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
        text.text = defaultText;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.enableWordWrapping = true;

        LayoutElement layout = textGo.GetComponent<LayoutElement>();
        layout.flexibleWidth = 1f;
        layout.minHeight = fontSize + 6f;
        return text;
    }

    private static Canvas FindOrCreateCanvas()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>(true);
        if (canvas != null)
            return canvas;

        GameObject canvasGo = new GameObject("CombatUiCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create Combat UI Canvas");

        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>(true) != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
    }

    private static GameObject FindOrCreateChild(Transform parent, string name, params System.Type[] components)
    {
        GameObject existing = FindChildByName(parent, name);
        if (existing != null)
            return existing;

        GameObject go = new GameObject(name, components);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject FindChildByName(Transform parent, string name)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name)
                return child.gameObject;
        }

        return null;
    }
}

#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class CombatLabPrototypeSetupTool
{
    private const string RootName = "CombatLabPrototypeRoot";
    private const string UiRootName = "CombatLabPrototypeUI";
    private const string ResetButtonName = "ResetGameButton";
    private const string ConfigFolder = "Assets/GameData/Prototype/CombatLab";
    private const string RewardScreenName = "PrototypeConsumableRewardScreen";
    private const string ConsumableSlotPrefabPath = "Assets/Prefabs/UI/Combat/ConsumableSlotCard.prefab";

    [MenuItem("Tools/Build Synergy/Prototype/Setup Combat Lab Prototype In Current Scene")]
    public static void SetupCombatLabPrototypeInCurrentScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogError("[CombatLabPrototypeSetupTool] No active scene.");
            return;
        }

        EnsureEventSystem();

        CombatLabPrototypeController controller = FindOrCreateController();
        CombatLabPrototypeConfigSO config = ResolveOrCreateConfigAsset(controller, scene.name);
        BattlePartyManager2D party = Object.FindFirstObjectByType<BattlePartyManager2D>(FindObjectsInactive.Include);
        RunInventoryManager inventory = Object.FindFirstObjectByType<RunInventoryManager>(FindObjectsInactive.Include);
        TurnManager turnManager = Object.FindFirstObjectByType<TurnManager>(FindObjectsInactive.Include);
        PrototypeConsumableRewardScreen rewardScreen = FindOrCreateRewardScreen();

        if (party == null || inventory == null || turnManager == null)
        {
            Debug.LogWarning("[CombatLabPrototypeSetupTool] Missing BattlePartyManager2D, RunInventoryManager, or TurnManager in scene. Tool still created root/config, but wiring is incomplete.");
        }

        WireController(controller, config, party, inventory, turnManager, rewardScreen);
        CreateOrUpdateResetButton(controller);
        DisableLegacyRewardDemoUi();

        EditorUtility.SetDirty(controller);
        if (config != null)
            EditorUtility.SetDirty(config);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = controller.gameObject;
    }

    private static CombatLabPrototypeController FindOrCreateController()
    {
        CombatLabPrototypeController existing = Object.FindFirstObjectByType<CombatLabPrototypeController>(FindObjectsInactive.Include);
        if (existing != null)
            return existing;

        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Combat Lab Prototype Root");
        }

        CombatLabPrototypeController controller = root.GetComponent<CombatLabPrototypeController>();
        if (controller == null)
            controller = Undo.AddComponent<CombatLabPrototypeController>(root);

        return controller;
    }

    private static CombatLabPrototypeConfigSO ResolveOrCreateConfigAsset(CombatLabPrototypeController controller, string sceneName)
    {
        if (Selection.activeObject is CombatLabPrototypeConfigSO selected)
            return selected;

        CombatLabPrototypeConfigSO assigned = GetAssignedConfig(controller);
        if (assigned != null)
            return assigned;

        return FindOrCreateConfigAsset(sceneName);
    }

    private static CombatLabPrototypeConfigSO GetAssignedConfig(CombatLabPrototypeController controller)
    {
        if (controller == null)
            return null;

        SerializedObject so = new SerializedObject(controller);
        return so.FindProperty("config").objectReferenceValue as CombatLabPrototypeConfigSO;
    }

    private static CombatLabPrototypeConfigSO FindOrCreateConfigAsset(string sceneName)
    {
        EnsureFolder(ConfigFolder);

        string safeSceneName = string.IsNullOrWhiteSpace(sceneName) ? "PrototypeScene" : sceneName;
        string assetPath = $"{ConfigFolder}/CombatLabPrototypeConfig_{safeSceneName}.asset";

        CombatLabPrototypeConfigSO config = AssetDatabase.LoadAssetAtPath<CombatLabPrototypeConfigSO>(assetPath);
        if (config != null)
            return config;

        config = ScriptableObject.CreateInstance<CombatLabPrototypeConfigSO>();
        AssetDatabase.CreateAsset(config, assetPath);
        AssetDatabase.SaveAssets();
        return config;
    }

    private static void WireController(
        CombatLabPrototypeController controller,
        CombatLabPrototypeConfigSO config,
        BattlePartyManager2D party,
        RunInventoryManager inventory,
        TurnManager turnManager,
        PrototypeConsumableRewardScreen rewardScreen)
    {
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("config").objectReferenceValue = config;
        so.FindProperty("party").objectReferenceValue = party;
        so.FindProperty("runInventory").objectReferenceValue = inventory;
        so.FindProperty("turnManager").objectReferenceValue = turnManager;
        so.FindProperty("rewardScreen").objectReferenceValue = rewardScreen;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static PrototypeConsumableRewardScreen FindOrCreateRewardScreen()
    {
        PrototypeConsumableRewardScreen existing = Object.FindFirstObjectByType<PrototypeConsumableRewardScreen>(FindObjectsInactive.Include);
        bool createdRoot = existing == null;

        Canvas canvas = FindOrCreateCanvas();
        GameObject root = existing != null
            ? existing.gameObject
            : FindOrCreateChild(canvas.transform, RewardScreenName, typeof(RectTransform), typeof(Image));
        RectTransform rootRt = root.GetComponent<RectTransform>();
        if (createdRoot)
        {
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            rootRt.SetAsLastSibling();
        }

        PrototypeConsumableRewardScreen screen = GetOrAdd<PrototypeConsumableRewardScreen>(root);

        Image blocker = GetOrAdd<Image>(root);
        if (createdRoot)
            blocker.color = new Color(0f, 0f, 0f, 0.64f);
        blocker.raycastTarget = true;

        bool createdContent = root.transform.Find("Content") == null;
        GameObject content = FindOrCreateChild(root.transform, "Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        RectTransform contentRt = content.GetComponent<RectTransform>();
        if (createdContent)
        {
            contentRt.anchorMin = new Vector2(0.5f, 0.5f);
            contentRt.anchorMax = new Vector2(0.5f, 0.5f);
            contentRt.pivot = new Vector2(0.5f, 0.5f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(760f, 330f);
        }

        VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
        if (createdContent)
        {
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = 120f;
            contentLayout.childAlignment = TextAnchor.MiddleCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
        }

        bool createdTitle = content.transform.Find("Title") == null;
        TMP_Text title = CreateText(content.transform, "Title", "Choose", 30f, FontStyles.Normal, TextAlignmentOptions.Center, new Color32(245, 247, 250, 255), createdTitle);
        LayoutElement titleLayout = GetOrAdd<LayoutElement>(title.gameObject);
        if (createdContent)
            titleLayout.preferredHeight = 54f;

        bool createdCardsRoot = content.transform.Find("Cards") == null;
        GameObject cardsRootGo = FindOrCreateChild(content.transform, "Cards", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        RectTransform cardsRoot = cardsRootGo.GetComponent<RectTransform>();
        if (createdCardsRoot)
            cardsRoot.sizeDelta = new Vector2(980f, 150f);
        LayoutElement cardsLayoutElement = GetOrAdd<LayoutElement>(cardsRootGo);
        if (createdCardsRoot)
        {
            cardsLayoutElement.preferredHeight = 150f;
            cardsLayoutElement.preferredWidth = 980f;
        }

        HorizontalLayoutGroup cardsLayout = cardsRootGo.GetComponent<HorizontalLayoutGroup>();
        if (createdCardsRoot)
        {
            cardsLayout.spacing = 280f;
            cardsLayout.childAlignment = TextAnchor.MiddleCenter;
            cardsLayout.childControlWidth = false;
            cardsLayout.childControlHeight = false;
            cardsLayout.childForceExpandWidth = false;
            cardsLayout.childForceExpandHeight = false;
        }

        RectTransform slotPrefab = LoadConsumableSlotCardPrefab();
        CleanupLegacyRewardCards(cardsRootGo.transform);
        ConsumableBarUIManager rewardBar = GetOrAdd<ConsumableBarUIManager>(cardsRootGo);
        rewardBar.layoutContainer = cardsRoot;
        rewardBar.slotTemplatePrefab = slotPrefab;
        rewardBar.cardSize = new Vector2(96f, 128f);
        rewardBar.relaxedSpacing = 390f;
        rewardBar.fallbackRowWidth = 980f;
        rewardBar.autoCreateMissingCards = true;

        SerializedObject so = new SerializedObject(screen);
        so.FindProperty("root").objectReferenceValue = rootRt;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("rewardBar").objectReferenceValue = rewardBar;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (createdRoot)
            root.SetActive(false);
        EditorUtility.SetDirty(screen);
        EditorUtility.SetDirty(rewardBar);
        return screen;
    }

    private static void CleanupLegacyRewardCards(Transform cardsRoot)
    {
        if (cardsRoot == null)
            return;

        for (int i = cardsRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = cardsRoot.GetChild(i);
            if (child != null && child.name.StartsWith("RewardCard", System.StringComparison.Ordinal))
                Undo.DestroyObjectImmediate(child.gameObject);
        }
    }

    private static RectTransform LoadConsumableSlotCardPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConsumableSlotPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[CombatLabPrototypeSetupTool] Missing ConsumableSlotCard prefab at {ConsumableSlotPrefabPath}. Run the consumable HUD setup tool or assign slotTemplatePrefab manually.");
            return null;
        }

        return prefab.GetComponent<RectTransform>();
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color, bool applyStyle = true)
    {
        GameObject go = FindOrCreateChild(parent, name, typeof(RectTransform), typeof(TextMeshProUGUI));
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        if (applyStyle)
        {
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
        }
        return tmp;
    }

    private static void DisableLegacyRewardDemoUi()
    {
        RewardGachaDemoController[] legacy = Object.FindObjectsByType<RewardGachaDemoController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < legacy.Length; i++)
        {
            if (legacy[i] != null)
                legacy[i].gameObject.SetActive(false);
        }
    }

    private static void CreateOrUpdateResetButton(CombatLabPrototypeController controller)
    {
        Canvas canvas = FindOrCreateCanvas();
        GameObject uiRoot = FindOrCreateChild(canvas.transform, UiRootName, typeof(RectTransform));
        RectTransform uiRootRect = uiRoot.GetComponent<RectTransform>();
        uiRootRect.anchorMin = Vector2.zero;
        uiRootRect.anchorMax = Vector2.one;
        uiRootRect.offsetMin = Vector2.zero;
        uiRootRect.offsetMax = Vector2.zero;

        GameObject buttonGo = FindOrCreateChild(uiRoot.transform, ResetButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform buttonRect = buttonGo.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.anchoredPosition = new Vector2(-24f, -24f);
        buttonRect.sizeDelta = new Vector2(180f, 54f);

        Image buttonImage = buttonGo.GetComponent<Image>();
        buttonImage.color = new Color(0.76f, 0.18f, 0.18f, 0.96f);

        Button button = buttonGo.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = buttonImage;

        GameObject labelGo = FindOrCreateChild(buttonGo.transform, "Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 8f);
        labelRect.offsetMax = new Vector2(-8f, -8f);

        TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
        label.text = "RESET GAME";
        label.fontSize = 24f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;

        while (button.onClick.GetPersistentEventCount() > 0)
            UnityEventTools.RemovePersistentListener(button.onClick, 0);
        UnityEventTools.AddPersistentListener(button.onClick, controller.ResetGame);
        EditorUtility.SetDirty(button);
    }

    private static Canvas FindOrCreateCanvas()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas != null)
            return canvas;

        GameObject canvasGo = new GameObject("CombatLabPrototypeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create Combat Lab Prototype Canvas");

        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (eventSystem == null)
        {
            GameObject go = new GameObject("EventSystem", typeof(EventSystem));
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
            eventSystem = go.GetComponent<EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
            inputModule = Undo.AddComponent<InputSystemUIInputModule>(eventSystem.gameObject);
        inputModule.enabled = true;

        StandaloneInputModule standalone = eventSystem.GetComponent<StandaloneInputModule>();
        if (standalone != null)
            standalone.enabled = false;
#else
        StandaloneInputModule standalone = eventSystem.GetComponent<StandaloneInputModule>();
        if (standalone == null)
            standalone = Undo.AddComponent<StandaloneInputModule>(eventSystem.gameObject);
        standalone.enabled = true;
#endif
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T existing = go.GetComponent<T>();
        if (existing != null)
            return existing;

        return Undo.AddComponent<T>(go);
    }

    private static GameObject FindOrCreateChild(Transform parent, string name, params System.Type[] components)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing.gameObject;

        GameObject go = new GameObject(name, components);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.transform.SetParent(parent, false);
        return go;
    }
}
#endif

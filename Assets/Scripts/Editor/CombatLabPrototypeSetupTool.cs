#if UNITY_EDITOR
using System.IO;
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
        CombatLabPrototypeConfigSO config = FindOrCreateConfigAsset(scene.name);
        BattlePartyManager2D party = Object.FindFirstObjectByType<BattlePartyManager2D>(FindObjectsInactive.Include);
        RunInventoryManager inventory = Object.FindFirstObjectByType<RunInventoryManager>(FindObjectsInactive.Include);
        TurnManager turnManager = Object.FindFirstObjectByType<TurnManager>(FindObjectsInactive.Include);

        if (party == null || inventory == null || turnManager == null)
        {
            Debug.LogWarning("[CombatLabPrototypeSetupTool] Missing BattlePartyManager2D, RunInventoryManager, or TurnManager in scene. Tool still created root/config, but wiring is incomplete.");
        }

        WireController(controller, config, party, inventory, turnManager);
        CreateOrUpdateResetButton(controller);

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
        TurnManager turnManager)
    {
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("config").objectReferenceValue = config;
        so.FindProperty("party").objectReferenceValue = party;
        so.FindProperty("runInventory").objectReferenceValue = inventory;
        so.FindProperty("turnManager").objectReferenceValue = turnManager;
        so.ApplyModifiedPropertiesWithoutUndo();
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

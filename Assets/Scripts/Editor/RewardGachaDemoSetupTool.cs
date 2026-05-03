#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class RewardGachaDemoSetupTool
{
    private const string ScenePath = "Assets/Scenes/RewardGachaDemo.unity";
    private const string OreFolder = "Assets/GameData/ForgeOres";
    private const string PatinaOrePath = OreFolder + "/Ore_Patina.asset";
    private const string SkillDatabasePath = "Assets/Scripts/Skills/SkillDatabase_SpecBacked.asset";

    [MenuItem("Tools/Build Synergy/Create Reward Gacha Demo Scene")]
    public static void CreateRewardGachaDemoScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SetupRewardGachaDemoInCurrentScene();

        string directory = Path.GetDirectoryName(ScenePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Build Synergy/Setup Reward Gacha Demo In Current Scene")]
    public static void SetupRewardGachaDemoInCurrentScene()
    {
        Canvas canvas = FindOrCreateCanvas();
        EnsureEventSystem();

        RewardGachaDemoController controller = Object.FindObjectOfType<RewardGachaDemoController>(true);
        if (controller == null)
        {
            GameObject root = new GameObject("RewardGachaDemo", typeof(RectTransform), typeof(Image), typeof(RewardGachaDemoController));
            Undo.RegisterCreatedObjectUndo(root, "Create Reward Gacha Demo");
            root.transform.SetParent(canvas.transform, false);
            controller = root.GetComponent<RewardGachaDemoController>();
        }

        RectTransform rootRt = controller.transform as RectTransform;
        if (rootRt != null)
        {
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
        }

        RunInventoryManager inventory = Object.FindObjectOfType<RunInventoryManager>(true);
        if (inventory == null)
        {
            GameObject inventoryGo = new GameObject("DemoRunInventory", typeof(RunInventoryManager));
            Undo.RegisterCreatedObjectUndo(inventoryGo, "Create Demo Run Inventory");
            inventory = inventoryGo.GetComponent<RunInventoryManager>();
        }

        SkillDatabaseSO skillDatabase = LoadSkillDatabase();
        List<ConsumableDataSO> consumables = LoadConsumables();
        DiceColorOreSO patinaOre = EnsurePatinaOreAsset();

        Undo.RecordObject(controller, "Configure Reward Gacha Demo");
        controller.runInventory = inventory;
        controller.ConfigureData(skillDatabase, consumables, new[] { patinaOre });
        controller.RebuildDemoUi();
        controller.RollRewards();

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = controller.gameObject;
    }

    private static Canvas FindOrCreateCanvas()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>(true);
        if (canvas != null)
            return canvas;

        GameObject go = new GameObject("RewardGachaCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(go, "Create Reward Gacha Canvas");

        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1440f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = Object.FindObjectOfType<EventSystem>(true);
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

    private static SkillDatabaseSO LoadSkillDatabase()
    {
        SkillDatabaseSO database = AssetDatabase.LoadAssetAtPath<SkillDatabaseSO>(SkillDatabasePath);
        if (database != null)
            return database;

        string[] guids = AssetDatabase.FindAssets("t:SkillDatabaseSO");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            database = AssetDatabase.LoadAssetAtPath<SkillDatabaseSO>(path);
            if (database != null)
                return database;
        }

        return null;
    }

    private static List<ConsumableDataSO> LoadConsumables()
    {
        List<ConsumableDataSO> result = new List<ConsumableDataSO>();
        string[] guids = AssetDatabase.FindAssets("t:ConsumableDataSO", new[] { "Assets/GameData/Consumables" });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            ConsumableDataSO data = AssetDatabase.LoadAssetAtPath<ConsumableDataSO>(path);
            if (data != null && !result.Contains(data))
                result.Add(data);
        }

        return result;
    }

    private static DiceColorOreSO EnsurePatinaOreAsset()
    {
        if (!AssetDatabase.IsValidFolder("Assets/GameData"))
            AssetDatabase.CreateFolder("Assets", "GameData");
        if (!AssetDatabase.IsValidFolder(OreFolder))
            AssetDatabase.CreateFolder("Assets/GameData", "ForgeOres");

        DiceColorOreSO ore = AssetDatabase.LoadAssetAtPath<DiceColorOreSO>(PatinaOrePath);
        if (ore != null)
            return ore;

        ore = ScriptableObject.CreateInstance<DiceColorOreSO>();
        ore.displayName = "Whole-die Color Ore: Patina";
        ore.description = "Forge material used to recolor a whole die to Patina. Runtime Forge inventory is not wired yet.";
        ore.targetTag = DiceWholeDieTag.Patina;
        ore.rarity = ContentRarity.Rare;
        ore.displayColor = new Color(0.45f, 0.72f, 0.62f, 1f);
        AssetDatabase.CreateAsset(ore, PatinaOrePath);
        AssetDatabase.SaveAssets();
        return ore;
    }
}
#endif

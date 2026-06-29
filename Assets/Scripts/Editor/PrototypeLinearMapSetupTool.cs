#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrototypeLinearMapSetupTool
{
    [MenuItem("Tools/Build Synergy/Prototype/Setup Linear Prototype Map Flow In Current Scene")]
    public static void SetupLinearPrototypeMapFlowInCurrentScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogWarning("[PrototypeLinearMapSetupTool] No active scene to setup.");
            return;
        }

        CombatLabPrototypeSetupTool.SetupCombatLabPrototypeInCurrentScene();
        MapPrototypeSetupTool.SetupMapPrototypeDemo();

        CombatLabPrototypeController combatController = Object.FindFirstObjectByType<CombatLabPrototypeController>(FindObjectsInactive.Include);
        MapPrototypeController mapController = Object.FindFirstObjectByType<MapPrototypeController>(FindObjectsInactive.Include);
        TurnManager turnManager = Object.FindFirstObjectByType<TurnManager>(FindObjectsInactive.Include);

        if (combatController == null || mapController == null)
        {
            Debug.LogWarning("[PrototypeLinearMapSetupTool] Missing CombatLabPrototypeController or MapPrototypeController after setup.");
            return;
        }

        PrototypeLinearMapFlowController flowController = combatController.GetComponent<PrototypeLinearMapFlowController>();
        if (flowController == null)
            flowController = Undo.AddComponent<PrototypeLinearMapFlowController>(combatController.gameObject);

        ConfigureCombatController(combatController);
        ConfigureMapController(mapController);
        ConfigureFlowController(flowController, combatController, mapController, turnManager);

        mapController.gameObject.SetActive(true);
        EditorUtility.SetDirty(combatController);
        EditorUtility.SetDirty(mapController);
        EditorUtility.SetDirty(flowController);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = flowController.gameObject;
    }

    private static void ConfigureCombatController(CombatLabPrototypeController controller)
    {
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("autoRunPrototypeFlow").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureMapController(MapPrototypeController controller)
    {
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("autoGenerateOnStart").boolValue = true;
        so.FindProperty("useExternalHostileFlow").boolValue = true;

        SerializedProperty config = so.FindProperty("config");
        config.FindPropertyRelative("useLinearPrototypeLayout").boolValue = true;
        config.FindPropertyRelative("linearCombatNodeCount").intValue = 4;
        config.FindPropertyRelative("columns").intValue = 3;
        config.FindPropertyRelative("intermediateRows").intValue = 4;
        config.FindPropertyRelative("pathCount").intValue = 1;
        config.FindPropertyRelative("mapHeight").floatValue = 840f;
        config.FindPropertyRelative("mapWidth").floatValue = 560f;
        config.FindPropertyRelative("padX").floatValue = 120f;
        config.FindPropertyRelative("padY").floatValue = 90f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureFlowController(
        PrototypeLinearMapFlowController flowController,
        CombatLabPrototypeController combatController,
        MapPrototypeController mapController,
        TurnManager turnManager)
    {
        SerializedObject so = new SerializedObject(flowController);
        so.FindProperty("combatController").objectReferenceValue = combatController;
        so.FindProperty("mapController").objectReferenceValue = mapController;
        so.FindProperty("turnManager").objectReferenceValue = turnManager;
        so.FindProperty("grantOpeningReward").boolValue = false;
        so.FindProperty("grantRewardAfterBoss").boolValue = false;
        so.FindProperty("grantPreCombatOneReward").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif

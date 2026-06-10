using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Editor utility for creating skill UI prefabs and wiring the current scene.
public static partial class SkillUiPrefabSetupTool
{
    private const string PrefabFolder = "Assets/Prefabs/UI";
    private const string TooltipResourcesFolder = "Assets/Resources/UI";
    private const string SkillSlotPrefabPath = PrefabFolder + "/SkillSlotLayout.prefab";
    private const string SkillTooltipPrefabPath = TooltipResourcesFolder + "/SkillTooltipLayout.prefab";
    private const string UiAssetFolder = "Assets/GameData/UI";
    private const string IconLibraryAssetPath = UiAssetFolder + "/SkillUiIconLibrary.asset";
    private const string RuntimeRootName = "SkillUiRuntimeRoot";
    private const string OwnedRowName = "OwnedSkillsRow";
    private const string TooltipInstanceName = "SkillTooltip";
    private const string WorldUiPrefabPath = "Assets/Prefabs/Entities/world-ui.prefab";

    [MenuItem("Tools/Build Synergy/Create Skill UI Layout Prefabs")]
    public static void CreatePrefabs()
    {
        Directory.CreateDirectory(PrefabFolder);
        Directory.CreateDirectory(TooltipResourcesFolder);
        Directory.CreateDirectory(UiAssetFolder);

        SkillUiIconLibrarySO iconLibrary = EnsureIconLibraryAsset();
        CreateSkillSlotPrefab(iconLibrary);
        CreateSkillTooltipPrefab();
        AssignWorldUiPrefabLibrary(iconLibrary);
        SetupCurrentScene(iconLibrary);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Build Synergy/Setup Skill UI In Current Scene")]
    public static void SetupCurrentSceneMenu()
    {
        SkillUiIconLibrarySO iconLibrary = EnsureIconLibraryAsset();
        AssignWorldUiPrefabLibrary(iconLibrary);
        SetupCurrentScene(iconLibrary);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static SkillUiIconLibrarySO EnsureIconLibraryAsset()
    {
        SkillUiIconLibrarySO library = AssetDatabase.LoadAssetAtPath<SkillUiIconLibrarySO>(IconLibraryAssetPath);
        if (library != null)
            return library;

        library = ScriptableObject.CreateInstance<SkillUiIconLibrarySO>();
        AssetDatabase.CreateAsset(library, IconLibraryAssetPath);
        EditorUtility.SetDirty(library);
        return library;
    }

    private static void SetupCurrentScene(SkillUiIconLibrarySO iconLibrary)
    {
        Canvas canvas = FindOrCreateCanvas();
        RectTransform runtimeRoot = FindOrCreateRuntimeRoot(canvas.transform);
        RectTransform ownedRow = FindOrCreateRow(runtimeRoot, OwnedRowName, new Vector2(0f, 0f));

        SetupTooltipInstance(canvas.transform);

        RunInventoryManager[] inventories = Object.FindObjectsByType<RunInventoryManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        TurnManager turnManager = Object.FindFirstObjectByType<TurnManager>(FindObjectsInactive.Include);
        ActorWorldUI[] worldUis = Object.FindObjectsByType<ActorWorldUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (RunInventoryManager inventory in inventories)
            SetupInventoryIcons(inventory, ownedRow, turnManager, iconLibrary);

        foreach (ActorWorldUI worldUi in worldUis)
        {
            if (worldUi == null)
                continue;

            worldUi.iconLibrary = iconLibrary;
            EditorUtility.SetDirty(worldUi);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private static void AssignWorldUiPrefabLibrary(SkillUiIconLibrarySO iconLibrary)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(WorldUiPrefabPath);
        if (prefabRoot == null)
            return;

        try
        {
            ActorWorldUI worldUi = prefabRoot.GetComponent<ActorWorldUI>();
            if (worldUi == null)
                return;

            worldUi.iconLibrary = iconLibrary;
            EditorUtility.SetDirty(worldUi);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, WorldUiPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void SetupInventoryIcons(RunInventoryManager inventory, RectTransform ownedRow, TurnManager turnManager, SkillUiIconLibrarySO iconLibrary)
    {
        if (inventory == null)
            return;

        SerializedObject inventorySo = new SerializedObject(inventory);
        SerializedProperty ownedSlots = inventorySo.FindProperty("ownedSlots");

        SetupSlotArray(inventory, ownedSlots, ownedRow, turnManager, iconLibrary);

        inventorySo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(inventory);
    }

    private static void SetupSlotArray(
        RunInventoryManager inventory,
        SerializedProperty slotArray,
        RectTransform parentRow,
        TurnManager turnManager,
        SkillUiIconLibrarySO iconLibrary)
    {
        if (slotArray == null)
            return;

        for (int i = 0; i < slotArray.arraySize; i++)
        {
            SerializedProperty binding = slotArray.GetArrayElementAtIndex(i);
            SerializedProperty uiIconProp = binding.FindPropertyRelative("uiIcon");
            DraggableSkillIcon existing = uiIconProp.objectReferenceValue as DraggableSkillIcon;
            DraggableSkillIcon resolved = EnsureSkillSlotInstance(existing, parentRow, i, turnManager, iconLibrary);
            uiIconProp.objectReferenceValue = resolved;

            if (resolved != null)
            {
                resolved.SetBindToInventory(inventory, i);
                resolved.Refresh();
                EditorUtility.SetDirty(resolved);
            }
        }
    }

    private static DraggableSkillIcon EnsureSkillSlotInstance(
        DraggableSkillIcon existing,
        RectTransform parentRow,
        int index,
        TurnManager turnManager,
        SkillUiIconLibrarySO iconLibrary)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkillSlotPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"Missing skill slot prefab at {SkillSlotPrefabPath}.");
            return existing;
        }

        if (existing != null && PrefabUtility.GetCorrespondingObjectFromSource(existing.gameObject) == prefab)
        {
            ConfigureSkillIcon(existing, turnManager, iconLibrary, index);
            existing.transform.SetParent(parentRow, false);
            existing.name = BuildSlotName(index);
            return existing;
        }

        Vector3 worldPos = Vector3.zero;
        Quaternion worldRot = Quaternion.identity;
        Vector3 localScale = Vector3.one;
        int siblingIndex = parentRow.childCount;

        if (existing != null)
        {
            RectTransform oldRt = existing.transform as RectTransform;
            if (oldRt != null)
            {
                worldPos = oldRt.position;
                worldRot = oldRt.rotation;
                localScale = oldRt.localScale;
                siblingIndex = oldRt.GetSiblingIndex();
            }

            existing.gameObject.SetActive(false);
            existing.gameObject.name = existing.gameObject.name + "_Legacy";
            EditorUtility.SetDirty(existing.gameObject);
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parentRow) as GameObject;
        if (instance == null)
            return existing;

        instance.name = BuildSlotName(index);
        RectTransform rt = instance.transform as RectTransform;
        if (rt != null)
        {
            if (existing != null)
            {
                rt.position = worldPos;
                rt.rotation = worldRot;
                rt.localScale = localScale;
                rt.SetSiblingIndex(siblingIndex);
            }
            else
            {
                rt.localScale = Vector3.one;
            }
        }

        DraggableSkillIcon icon = instance.GetComponent<DraggableSkillIcon>();
        ConfigureSkillIcon(icon, turnManager, iconLibrary, index);
        return icon;
    }

    private static void ConfigureSkillIcon(DraggableSkillIcon icon, TurnManager turnManager, SkillUiIconLibrarySO iconLibrary, int index)
    {
        if (icon == null)
            return;

        SkillSlotLayout layout = icon.GetComponent<SkillSlotLayout>();
        if (layout != null)
            icon.BindLayout(layout);

        SerializedObject iconSo = new SerializedObject(icon);
        iconSo.FindProperty("turn").objectReferenceValue = turnManager;
        iconSo.FindProperty("iconLibrary").objectReferenceValue = iconLibrary;
        iconSo.FindProperty("bindToInventorySlot").boolValue = true;
        iconSo.FindProperty("inventorySource").enumValueIndex = (int)RunInventoryManager.SkillSource.Owned;
        iconSo.FindProperty("inventoryIndex").intValue = index;
        iconSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetupTooltipInstance(Transform canvasRoot)
    {
        if (canvasRoot == null)
            return;

        SkillTooltipUI existing = canvasRoot.GetComponentInChildren<SkillTooltipUI>(true);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkillTooltipPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"Missing tooltip prefab at {SkillTooltipPrefabPath}.");
            return;
        }

        if (existing != null && PrefabUtility.GetCorrespondingObjectFromSource(existing.gameObject) == prefab)
        {
            existing.gameObject.name = TooltipInstanceName;
            existing.gameObject.SetActive(false);
            EditorUtility.SetDirty(existing.gameObject);
            return;
        }

        if (existing != null)
        {
            existing.gameObject.SetActive(false);
            existing.gameObject.name = existing.gameObject.name + "_Legacy";
            EditorUtility.SetDirty(existing.gameObject);
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, canvasRoot) as GameObject;
        if (instance == null)
            return;

        instance.name = TooltipInstanceName;
        instance.SetActive(false);
        instance.transform.SetAsLastSibling();
        EditorUtility.SetDirty(instance);
    }

    private static RectTransform FindOrCreateRuntimeRoot(Transform canvasRoot)
    {
        Transform existing = canvasRoot.Find(RuntimeRootName);
        if (existing != null)
            return existing as RectTransform;

        GameObject root = new GameObject(RuntimeRootName, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        Undo.RegisterCreatedObjectUndo(root, "Create Skill UI Runtime Root");
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.SetParent(canvasRoot, false);
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 140f);
        rt.sizeDelta = new Vector2(700f, 160f);

        VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childAlignment = TextAnchor.LowerCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = root.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rt;
    }

    private static RectTransform FindOrCreateRow(RectTransform parent, string name, Vector2 defaultPos)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing as RectTransform;

        GameObject row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        Undo.RegisterCreatedObjectUndo(row, $"Create {name}");
        RectTransform rt = row.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = defaultPos;
        rt.sizeDelta = new Vector2(640f, 72f);

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 16f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = row.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rt;
    }

    private static string BuildSlotName(int index)
    {
        return "OwnedSkill_" + (index + 1);
    }

    private static Canvas FindOrCreateCanvas()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas != null)
            return canvas;

        GameObject canvasGo = new GameObject("SkillUiCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create Skill UI Canvas");

        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

}

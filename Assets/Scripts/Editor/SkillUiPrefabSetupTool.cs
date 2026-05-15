using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SkillUiPrefabSetupTool
{
    private const string PrefabFolder = "Assets/Prefabs/UI";
    private const string TooltipResourcesFolder = "Assets/Resources/UI";
    private const string SkillSlotPrefabPath = PrefabFolder + "/SkillSlotLayout.prefab";
    private const string SkillTooltipPrefabPath = TooltipResourcesFolder + "/SkillTooltipLayout.prefab";
    private const string UiAssetFolder = "Assets/GameData/UI";
    private const string IconLibraryAssetPath = UiAssetFolder + "/CombatUiIconLibrary.asset";
    private const string RuntimeRootName = "SkillUiRuntimeRoot";
    private const string FixedRowName = "FixedSkillsRow";
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
        RectTransform fixedRow = FindOrCreateRow(runtimeRoot, FixedRowName, new Vector2(0f, 84f));
        RectTransform ownedRow = FindOrCreateRow(runtimeRoot, OwnedRowName, new Vector2(0f, 0f));

        SetupTooltipInstance(canvas.transform);

        RunInventoryManager[] inventories = Object.FindObjectsByType<RunInventoryManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        TurnManager turnManager = Object.FindFirstObjectByType<TurnManager>(FindObjectsInactive.Include);
        ActorWorldUI[] worldUis = Object.FindObjectsByType<ActorWorldUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (RunInventoryManager inventory in inventories)
            SetupInventoryIcons(inventory, fixedRow, ownedRow, turnManager, iconLibrary);

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

    private static void SetupInventoryIcons(RunInventoryManager inventory, RectTransform fixedRow, RectTransform ownedRow, TurnManager turnManager, SkillUiIconLibrarySO iconLibrary)
    {
        if (inventory == null)
            return;

        SerializedObject inventorySo = new SerializedObject(inventory);
        SerializedProperty fixedSlots = inventorySo.FindProperty("fixedSlots");
        SerializedProperty ownedSlots = inventorySo.FindProperty("ownedSlots");

        SetupSlotArray(inventory, fixedSlots, fixedRow, isFixed: true, turnManager, iconLibrary);
        SetupSlotArray(inventory, ownedSlots, ownedRow, isFixed: false, turnManager, iconLibrary);

        inventorySo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(inventory);
    }

    private static void SetupSlotArray(
        RunInventoryManager inventory,
        SerializedProperty slotArray,
        RectTransform parentRow,
        bool isFixed,
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
            DraggableSkillIcon resolved = EnsureSkillSlotInstance(existing, parentRow, isFixed, i, turnManager, iconLibrary);
            uiIconProp.objectReferenceValue = resolved;

            if (resolved != null)
            {
                resolved.SetBindToInventory(inventory, isFixed, i);
                resolved.Refresh();
                EditorUtility.SetDirty(resolved);
            }
        }
    }

    private static DraggableSkillIcon EnsureSkillSlotInstance(
        DraggableSkillIcon existing,
        RectTransform parentRow,
        bool isFixed,
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
            ConfigureSkillIcon(existing, turnManager, iconLibrary, isFixed, index);
            existing.transform.SetParent(parentRow, false);
            existing.name = BuildSlotName(isFixed, index);
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

        instance.name = BuildSlotName(isFixed, index);
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
        ConfigureSkillIcon(icon, turnManager, iconLibrary, isFixed, index);
        return icon;
    }

    private static void ConfigureSkillIcon(DraggableSkillIcon icon, TurnManager turnManager, SkillUiIconLibrarySO iconLibrary, bool isFixed, int index)
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
        iconSo.FindProperty("inventorySource").enumValueIndex = isFixed ? 0 : 1;
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

    private static string BuildSlotName(bool isFixed, int index)
    {
        return (isFixed ? "FixedSkill_" : "OwnedSkill_") + (index + 1);
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

    private static void CreateSkillSlotPrefab(SkillUiIconLibrarySO iconLibrary)
    {
        GameObject root = new GameObject("SkillSlotLayout", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(DraggableSkillIcon), typeof(SkillSlotLayout));
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(120f, 120f);

        Image rootImage = root.GetComponent<Image>();
        rootImage.color = Color.white;
        rootImage.raycastTarget = true;

        SkillSlotLayout layout = root.GetComponent<SkillSlotLayout>();

        GameObject artGo = CreateChild(root.transform, "Art", typeof(RectTransform), typeof(Image));
        RectTransform artRt = artGo.GetComponent<RectTransform>();
        artRt.anchorMin = new Vector2(0f, 0f);
        artRt.anchorMax = new Vector2(1f, 1f);
        artRt.offsetMin = new Vector2(4f, 4f);
        artRt.offsetMax = new Vector2(-4f, -4f);
        Image artImage = artGo.GetComponent<Image>();
        artImage.preserveAspect = true;

        GameObject titleGo = CreateChild(root.transform, "Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0f);
        titleRt.anchorMax = new Vector2(1f, 0f);
        titleRt.pivot = new Vector2(0.5f, 0f);
        titleRt.sizeDelta = new Vector2(0f, 26f);
        titleRt.anchoredPosition = new Vector2(0f, -28f);
        TMP_Text title = titleGo.GetComponent<TMP_Text>();
        title.text = "Skill";
        title.fontSize = 16f;
        title.alignment = TextAlignmentOptions.Center;
        title.enableWordWrapping = false;
        title.raycastTarget = false;

        Image focusBg = CreateBadge(root.transform, "FocusCostBadge", new Vector2(0f, 1f), new Vector2(6f, -6f), new Color(0.1f, 0.22f, 0.35f, 0.92f), out TMP_Text focusText);
        Image diceBg = CreateBadge(root.transform, "SlotCostBadge", new Vector2(1f, 1f), new Vector2(-6f, -6f), new Color(0.28f, 0.2f, 0.55f, 0.92f), out TMP_Text diceFallback);

        GameObject diceIconGo = CreateChild(diceBg.transform, "Icon", typeof(RectTransform), typeof(Image));
        RectTransform diceIconRt = diceIconGo.GetComponent<RectTransform>();
        diceIconRt.anchorMin = Vector2.zero;
        diceIconRt.anchorMax = Vector2.one;
        diceIconRt.offsetMin = new Vector2(3f, 3f);
        diceIconRt.offsetMax = new Vector2(-3f, -3f);
        Image diceIcon = diceIconGo.GetComponent<Image>();
        diceIcon.preserveAspect = true;
        diceIcon.raycastTarget = false;

        GameObject elementGo = CreateChild(root.transform, "ElementBadge", typeof(RectTransform), typeof(Image));
        RectTransform elementRt = elementGo.GetComponent<RectTransform>();
        elementRt.anchorMin = new Vector2(1f, 0f);
        elementRt.anchorMax = new Vector2(1f, 0f);
        elementRt.pivot = new Vector2(1f, 0f);
        elementRt.sizeDelta = new Vector2(24f, 24f);
        elementRt.anchoredPosition = new Vector2(-6f, 6f);
        elementRt.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Image elementBg = elementGo.GetComponent<Image>();
        elementBg.color = new Color(0.65f, 0.2f, 0.1f, 0.95f);
        elementBg.raycastTarget = false;

        GameObject elementIconGo = CreateChild(elementGo.transform, "Icon", typeof(RectTransform), typeof(Image));
        RectTransform elementIconRt = elementIconGo.GetComponent<RectTransform>();
        elementIconRt.anchorMin = Vector2.zero;
        elementIconRt.anchorMax = Vector2.one;
        elementIconRt.offsetMin = new Vector2(4f, 4f);
        elementIconRt.offsetMax = new Vector2(-4f, -4f);
        elementIconRt.localRotation = Quaternion.Euler(0f, 0f, -45f);
        Image elementIcon = elementIconGo.GetComponent<Image>();
        elementIcon.preserveAspect = true;
        elementIcon.raycastTarget = false;

        SerializedObject layoutSo = new SerializedObject(layout);
        layoutSo.FindProperty("backgroundImage").objectReferenceValue = rootImage;
        layoutSo.FindProperty("skillArt").objectReferenceValue = artImage;
        layoutSo.FindProperty("titleText").objectReferenceValue = title;
        layoutSo.FindProperty("focusBadgeBackground").objectReferenceValue = focusBg;
        layoutSo.FindProperty("focusBadgeText").objectReferenceValue = focusText;
        layoutSo.FindProperty("diceBadgeBackground").objectReferenceValue = diceBg;
        layoutSo.FindProperty("diceBadgeIcon").objectReferenceValue = diceIcon;
        layoutSo.FindProperty("diceBadgeFallbackText").objectReferenceValue = diceFallback;
        layoutSo.FindProperty("elementBadgeBackground").objectReferenceValue = elementBg;
        layoutSo.FindProperty("elementBadgeIcon").objectReferenceValue = elementIcon;
        layoutSo.ApplyModifiedPropertiesWithoutUndo();

        DraggableSkillIcon icon = root.GetComponent<DraggableSkillIcon>();
        SerializedObject iconSo = new SerializedObject(icon);
        iconSo.FindProperty("iconLibrary").objectReferenceValue = iconLibrary;
        iconSo.FindProperty("skillSlotLayout").objectReferenceValue = layout;
        iconSo.FindProperty("nameText").objectReferenceValue = title;
        iconSo.FindProperty("focusCostBadgeBackground").objectReferenceValue = focusBg;
        iconSo.FindProperty("focusCostBadgeText").objectReferenceValue = focusText;
        iconSo.FindProperty("slotCostBadgeBackground").objectReferenceValue = diceBg;
        iconSo.FindProperty("slotCostBadgeIcon").objectReferenceValue = diceIcon;
        iconSo.FindProperty("slotCostBadgeText").objectReferenceValue = diceFallback;
        iconSo.FindProperty("elementBadgeBackground").objectReferenceValue = elementBg;
        iconSo.FindProperty("elementBadgeIcon").objectReferenceValue = elementIcon;
        iconSo.FindProperty("skillBackgroundImage").objectReferenceValue = rootImage;
        iconSo.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, SkillSlotPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static void CreateSkillTooltipPrefab()
    {
        GameObject root = new GameObject("SkillTooltipLayout", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(SkillTooltipUI), typeof(SkillTooltipLayout));
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(220f, 120f);
        rootRt.pivot = new Vector2(0f, 1f);

        Image bg = root.GetComponent<Image>();
        bg.color = new Color(0.075f, 0.085f, 0.11f, 0.97f);
        bg.raycastTarget = false;

        VerticalLayoutGroup layoutGroup = root.GetComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(12, 12, 10, 10);
        layoutGroup.spacing = 7f;
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;

        ContentSizeFitter fitter = root.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text title = CreateTooltipText(root.transform, "Title", 19f, FontStyles.Bold);
        TMP_Text body = CreateTooltipText(root.transform, "Body", 14f, FontStyles.Normal);
        body.color = new Color(0.91f, 0.93f, 0.96f, 1f);

        SkillTooltipLayout tooltipLayout = root.GetComponent<SkillTooltipLayout>();
        SerializedObject so = new SerializedObject(tooltipLayout);
        so.FindProperty("background").objectReferenceValue = bg;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("bodyText").objectReferenceValue = body;
        so.FindProperty("verticalLayout").objectReferenceValue = layoutGroup;
        so.FindProperty("contentSizeFitter").objectReferenceValue = fitter;
        so.FindProperty("minContentWidth").floatValue = 170f;
        so.FindProperty("maxContentWidth").floatValue = 320f;
        so.FindProperty("minContentHeight").floatValue = 0f;
        so.FindProperty("maxContentHeight").floatValue = 0f;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, SkillTooltipPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static Image CreateBadge(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition, Color color, out TMP_Text text)
    {
        GameObject badgeGo = CreateChild(parent, name, typeof(RectTransform), typeof(Image));
        RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
        badgeRt.anchorMin = anchor;
        badgeRt.anchorMax = anchor;
        badgeRt.pivot = anchor;
        badgeRt.sizeDelta = new Vector2(28f, 22f);
        badgeRt.anchoredPosition = anchoredPosition;

        Image bg = badgeGo.GetComponent<Image>();
        bg.color = color;
        bg.raycastTarget = false;

        GameObject valueGo = CreateChild(badgeGo.transform, "Value", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform valueRt = valueGo.GetComponent<RectTransform>();
        valueRt.anchorMin = Vector2.zero;
        valueRt.anchorMax = Vector2.one;
        valueRt.offsetMin = Vector2.zero;
        valueRt.offsetMax = Vector2.zero;

        text = valueGo.GetComponent<TMP_Text>();
        text.text = "1";
        text.fontSize = 16f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        return bg;
    }

    private static TMP_Text CreateTooltipText(Transform parent, string name, float size, FontStyles style)
    {
        GameObject textGo = CreateChild(parent, name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        TMP_Text text = textGo.GetComponent<TMP_Text>();
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = size - 3f;
        text.fontSizeMax = size + 2f;
        text.fontStyle = style;
        text.color = Color.white;
        text.enableWordWrapping = true;
        text.richText = true;
        text.raycastTarget = false;

        LayoutElement layout = textGo.GetComponent<LayoutElement>();
        layout.preferredWidth = 196f;
        return text;
    }

    private static GameObject CreateChild(Transform parent, string name, params System.Type[] components)
    {
        GameObject child = new GameObject(name, components);
        RectTransform rt = child.transform as RectTransform;
        rt.SetParent(parent, false);
        return child;
    }
}

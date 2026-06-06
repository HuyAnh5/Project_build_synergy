using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class GameplayDiceEditSceneSetupTool
{
    private const string ControllerName = "GameplayDiceEditController";
    private const string PanelRootName = "GameplayDiceEditPanel";
    private const string AnchorName = "GameplayDiceEditInspectAnchor";

    [MenuItem("Tools/Build Synergy/Legacy/Setup Gameplay Dice Edit Panel")]
    public static void SetupGameplayDiceEditPanel()
    {
        Canvas canvas = FindOrCreateCanvas();
        EnsureEventSystem();
        GameplayDiceEditPanelUI panelUi = FindOrCreatePanel(canvas.transform);
        Transform anchor = FindOrCreateInspectAnchor();
        GameplayDiceEditController controller = FindOrCreateController();

        SerializedObject controllerSo = new SerializedObject(controller);
        controllerSo.FindProperty("panelUi").objectReferenceValue = panelUi;
        controllerSo.FindProperty("inspectAnchor").objectReferenceValue = anchor;
        controllerSo.FindProperty("runInventory").objectReferenceValue = Object.FindFirstObjectByType<RunInventoryManager>(FindObjectsInactive.Include);
        controllerSo.FindProperty("diceRig").objectReferenceValue = Object.FindFirstObjectByType<DiceSlotRig>(FindObjectsInactive.Include);
        controllerSo.ApplyModifiedPropertiesWithoutUndo();

        ConsumableBarUIManager bar = Object.FindFirstObjectByType<ConsumableBarUIManager>(FindObjectsInactive.Include);
        if (bar != null)
        {
            SerializedObject barSo = new SerializedObject(bar);
            barSo.FindProperty("diceEditController").objectReferenceValue = controller;
            barSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bar);
        }

        DiceFaceHighlightMetadataSetupTool.GenerateForSceneDice();
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(panelUi.gameObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = controller.gameObject;
    }

    [MenuItem("Tools/Build Synergy/Prototype/Setup Scene Local Dice Edit UI Layers")]
    public static void SetupSceneLocalDiceEditUiLayers()
    {
        EnsureEventSystem();

        Transform uiRoot = FindOrCreateSceneRoot("SceneLocalCombatUIRoot");
        uiRoot.gameObject.SetActive(true);

        Canvas panelCanvas = FindOrCreatePanelCanvas(uiRoot, "GameplayDiceEditPanelCanvas", 30000);
        Canvas tooltipCanvas = FindOrCreateOverlayCanvas(uiRoot, "SkillTooltipOverlayCanvas", short.MaxValue);

        GameplayDiceEditPanelUI panelUi = FindOrCreatePanel(panelCanvas.transform);
        Transform fallbackAnchor = FindOrCreateInspectAnchor();
        GameplayDiceEditController controller = FindOrCreateController();

        SerializedObject controllerSo = new SerializedObject(controller);
        controllerSo.FindProperty("panelUi").objectReferenceValue = panelUi;
        controllerSo.FindProperty("inspectAnchor").objectReferenceValue = fallbackAnchor;
        controllerSo.FindProperty("runInventory").objectReferenceValue = Object.FindFirstObjectByType<RunInventoryManager>(FindObjectsInactive.Include);
        controllerSo.FindProperty("diceRig").objectReferenceValue = Object.FindFirstObjectByType<DiceSlotRig>(FindObjectsInactive.Include);
        controllerSo.ApplyModifiedPropertiesWithoutUndo();

        ConsumableBarUIManager bar = Object.FindFirstObjectByType<ConsumableBarUIManager>(FindObjectsInactive.Include);
        if (bar != null)
        {
            SerializedObject barSo = new SerializedObject(bar);
            barSo.FindProperty("diceEditController").objectReferenceValue = controller;
            barSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bar);
        }

        panelCanvas.gameObject.SetActive(true);
        tooltipCanvas.gameObject.SetActive(true);
        DisableLegacyInspectPreviewObjects();
        DiceFaceHighlightMetadataSetupTool.GenerateForSceneDice();

        EditorUtility.SetDirty(panelUi);
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(panelCanvas);
        EditorUtility.SetDirty(tooltipCanvas);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = uiRoot.gameObject;
    }

    private static GameplayDiceEditController FindOrCreateController()
    {
        GameplayDiceEditController existing = Object.FindFirstObjectByType<GameplayDiceEditController>(FindObjectsInactive.Include);
        if (existing != null)
            return existing;

        GameObject go = new GameObject(ControllerName);
        Undo.RegisterCreatedObjectUndo(go, "Create Gameplay Dice Edit Controller");
        return Undo.AddComponent<GameplayDiceEditController>(go);
    }

    private static GameObject FindSceneGameObjectByName(string name)
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform transform = transforms[i];
            if (transform != null && transform.name == name && transform.gameObject.scene.IsValid())
                return transform.gameObject;
        }

        return null;
    }

    private static GameplayDiceEditPanelUI FindOrCreatePanel(Transform canvas)
    {
        Transform existing = canvas != null ? canvas.Find(PanelRootName) : null;
        GameObject root = existing != null ? existing.gameObject : FindSceneGameObjectByName(PanelRootName);
        if (root == null)
            root = new GameObject(PanelRootName, typeof(RectTransform), typeof(Image));
        if (canvas != null && root.transform.parent != canvas)
            Undo.SetTransformParent(root.transform, canvas, "Move Gameplay Dice Edit Panel");

        RectTransform rootRt = root.GetComponent<RectTransform>() ?? Undo.AddComponent<RectTransform>(root);
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.pivot = new Vector2(0.5f, 0.5f);
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        Image bg = root.GetComponent<Image>() ?? Undo.AddComponent<Image>(root);
        bg.color = new Color(0f, 0f, 0f, 0f);
        bg.raycastTarget = false;

        GameplayDiceEditPanelUI panelUi = root.GetComponent<GameplayDiceEditPanelUI>();
        if (panelUi == null)
            panelUi = Undo.AddComponent<GameplayDiceEditPanelUI>(root);

        GameObject backdrop = FindOrCreateChild(root.transform, "Backdrop", typeof(RectTransform), typeof(Image));
        RectTransform backdropRt = backdrop.GetComponent<RectTransform>();
        backdropRt.anchorMin = Vector2.zero;
        backdropRt.anchorMax = Vector2.one;
        backdropRt.pivot = new Vector2(0.5f, 0.5f);
        backdropRt.offsetMin = Vector2.zero;
        backdropRt.offsetMax = Vector2.zero;
        Image backdropImage = backdrop.GetComponent<Image>();
        backdropImage.color = new Color(0f, 0f, 0f, 0.62f);
        backdropImage.raycastTarget = false;

        GameObject card = FindOrCreateChild(root.transform, "Card", typeof(RectTransform), typeof(Image));
        RectTransform cardRt = card.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(1f, 0.5f);
        cardRt.anchorMax = new Vector2(1f, 0.5f);
        cardRt.pivot = new Vector2(1f, 0.5f);
        cardRt.anchoredPosition = new Vector2(-36f, 0f);
        cardRt.sizeDelta = new Vector2(420f, 420f);
        Image cardImage = card.GetComponent<Image>();
        cardImage.color = new Color(0.08f, 0.1f, 0.14f, 0.96f);
        cardImage.raycastTarget = true;

        TMP_Text title = BuildText(card.transform, "ZodiacName", 28f, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Vector2(24f, -20f), new Vector2(-24f, -86f), "No Zodiac");
        TMP_Text effect = BuildText(card.transform, "EffectText", 18f, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Vector2(24f, -96f), new Vector2(-24f, -180f), "Effect");

        Button upright = BuildButton(card.transform, "AutoUprightButton", "UPRIGHT", new Vector2(24f, 164f), new Vector2(178f, 52f));
        Button roll = BuildButton(card.transform, "RollButton", "ROLL", new Vector2(218f, 164f), new Vector2(178f, 52f));
        Button use = BuildButton(card.transform, "UseButton", "USE", new Vector2(24f, 100f), new Vector2(178f, 52f));
        Button cancel = BuildButton(card.transform, "CancelButton", "CANCEL", new Vector2(218f, 100f), new Vector2(178f, 52f));

        SerializedObject so = new SerializedObject(panelUi);
        so.FindProperty("panelRoot").objectReferenceValue = rootRt;
        so.FindProperty("zodiacNameText").objectReferenceValue = title;
        so.FindProperty("effectText").objectReferenceValue = effect;
        so.FindProperty("useButton").objectReferenceValue = use;
        so.FindProperty("cancelButton").objectReferenceValue = cancel;
        so.FindProperty("autoUprightButton").objectReferenceValue = upright;
        so.FindProperty("rollButton").objectReferenceValue = roll;
        so.ApplyModifiedPropertiesWithoutUndo();

        root.SetActive(false);
        return panelUi;
    }

    private static Transform FindOrCreateInspectAnchor()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject fallback = new GameObject(AnchorName);
            Undo.RegisterCreatedObjectUndo(fallback, "Create Gameplay Dice Edit Anchor");
            fallback.transform.position = new Vector3(0f, 0f, 0f);
            return fallback.transform;
        }

        Transform existing = cam.transform.Find(AnchorName);
        if (existing != null)
            return existing;

        GameObject go = new GameObject(AnchorName);
        Undo.RegisterCreatedObjectUndo(go, "Create Gameplay Dice Edit Anchor");
        go.transform.SetParent(cam.transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, 4f);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go.transform;
    }


    private static Transform FindOrCreateSceneRoot(string name)
    {
        GameObject existing = FindSceneGameObjectByName(name);
        if (existing != null)
            return existing.transform;

        GameObject root = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(root, "Create " + name);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return root.transform;
    }

    private static Canvas FindOrCreateOverlayCanvas(Transform parent, string name, int sortingOrder)
    {
        Transform existing = parent != null ? parent.Find(name) : null;
        GameObject canvasGo = existing != null ? existing.gameObject : FindSceneGameObjectByName(name);
        if (canvasGo == null)
        {
            canvasGo = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create " + name);
        }

        if (parent != null && canvasGo.transform.parent != parent)
            Undo.SetTransformParent(canvasGo.transform, parent, "Move " + name);

        RectTransform rect = canvasGo.GetComponent<RectTransform>() ?? Undo.AddComponent<RectTransform>(canvasGo);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        Canvas canvas = canvasGo.GetComponent<Canvas>() ?? Undo.AddComponent<Canvas>(canvasGo);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;
        canvas.pixelPerfect = false;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>() ?? Undo.AddComponent<CanvasScaler>(canvasGo);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvasGo.GetComponent<GraphicRaycaster>() ?? Undo.AddComponent<GraphicRaycaster>(canvasGo);
        raycaster.enabled = true;
        return canvas;
    }

    private static Canvas FindOrCreatePanelCanvas(Transform parent, string name, int sortingOrder)
    {
        Canvas canvas = FindOrCreateOverlayCanvas(parent, name, sortingOrder);
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCamera;
            canvas.planeDistance = 7.5f;
        }

        return canvas;
    }

    private static void DisableLegacyInspectPreviewObjects()
    {
        GameObject imageGo = FindSceneGameObjectByName("InspectDiceRawImage");
        if (imageGo != null)
            imageGo.SetActive(false);

        GameObject cameraGo = FindSceneGameObjectByName("DicePreviewCamera");
        if (cameraGo != null)
        {
            Camera camera = cameraGo.GetComponent<Camera>();
            if (camera != null)
                camera.targetTexture = null;
            cameraGo.SetActive(false);
        }
    }

    private static Canvas FindOrCreateCanvas()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas != null)
            return canvas;

        GameObject canvasGo = new GameObject("GameplayDiceEditCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create Gameplay Dice Edit Canvas");

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
        if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
    }

    private static TMP_Text BuildText(Transform parent, string name, float size, FontStyles style, TextAlignmentOptions alignment, Vector2 offsetMin, Vector2 offsetMax, string text)
    {
        GameObject go = FindOrCreateChild(parent, name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(offsetMin.x, offsetMax.y);
        rt.offsetMax = new Vector2(offsetMax.x, offsetMin.y);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    private static Button BuildButton(Transform parent, string name, string labelText, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject go = FindOrCreateChild(parent, name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.92f, 0.92f, 0.92f, 1f);

        TMP_Text label = BuildText(go.transform, "Label", 20f, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(8f, -8f), new Vector2(-8f, -8f), labelText);
        label.color = Color.black;
        return go.GetComponent<Button>();
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

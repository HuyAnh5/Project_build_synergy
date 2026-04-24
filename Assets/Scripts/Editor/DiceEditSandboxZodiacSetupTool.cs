using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class DiceEditSandboxZodiacSetupTool
{
    private const string RootName = "SampleSceneZodiacPanel";

    [MenuItem("Tools/Build Synergy/Setup SampleScene Zodiac Panel")]
    public static void SetupSampleSceneZodiacPanel()
    {
        Canvas canvas = FindOrCreateCanvas();
        EnsureEventSystem();

        GameObject root = FindOrCreateChild(canvas.transform, RootName, typeof(RectTransform), typeof(Image));
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0f, 0f);
        rootRt.anchorMax = new Vector2(0f, 0f);
        rootRt.pivot = new Vector2(0f, 0f);
        rootRt.anchoredPosition = new Vector2(24f, 24f);
        rootRt.sizeDelta = new Vector2(860f, 420f);

        Image rootImage = root.GetComponent<Image>();
        rootImage.color = new Color(1f, 1f, 1f, 0f);
        rootImage.raycastTarget = false;

        DiceEditSandboxZodiacPanelUI panelUi = root.GetComponent<DiceEditSandboxZodiacPanelUI>();
        if (panelUi == null)
            panelUi = Undo.AddComponent<DiceEditSandboxZodiacPanelUI>(root);

        RectTransform zodiacBox = BuildOrGetFrame(root.transform, "ZodiacBox", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(340f, 96f));
        TMP_Text zodiacPickerHint = GetOrCreateText(zodiacBox, "InspectorHint", "Pick Zodiac in inspector", 22, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        StretchToFill(zodiacPickerHint.rectTransform, new Vector2(20f, 12f), new Vector2(-20f, -12f));

        RectTransform infoBox = BuildOrGetFrame(root.transform, "InfoBox", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -124f), new Vector2(420f, 156f));
        TMP_Text zodiacName = GetOrCreateText(infoBox, "ZodiacName", "No Zodiac", 28, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        StretchToFill(zodiacName.rectTransform, new Vector2(20f, 16f), new Vector2(-20f, -92f));

        TMP_Text targetText = GetOrCreateText(infoBox, "TargetStatus", "Target: no zodiac selected", 20, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        StretchToFill(targetText.rectTransform, new Vector2(20f, 58f), new Vector2(-20f, -52f));

        TMP_Text ruleText = GetOrCreateText(infoBox, "SelectionRule", "Assign one Zodiac asset directly on DiceEditSandboxZodiacPanelUI in the inspector.", 18, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        StretchToFill(ruleText.rectTransform, new Vector2(20f, 94f), new Vector2(-20f, -16f));

        RectTransform actionColumn = FindOrCreateChild(root.transform, "ActionColumn", typeof(RectTransform)).GetComponent<RectTransform>();
        actionColumn.anchorMin = new Vector2(1f, 0.5f);
        actionColumn.anchorMax = new Vector2(1f, 0.5f);
        actionColumn.pivot = new Vector2(1f, 0.5f);
        actionColumn.anchoredPosition = new Vector2(-24f, 0f);
        actionColumn.sizeDelta = new Vector2(164f, 180f);

        Button useButton = BuildOrGetActionButton(actionColumn, "UseButton", "USE", new Vector2(0f, 44f), new Color(0.92f, 0.92f, 0.92f, 1f), out TMP_Text useLabel, out Image useBackground);
        Button cancelButton = BuildOrGetActionButton(actionColumn, "CancelButton", "CANCEL", new Vector2(0f, -44f), new Color(0.92f, 0.92f, 0.92f, 1f), out TMP_Text cancelLabel, out Image cancelBackground);
        Button autoUprightButton = BuildOrGetActionButton(actionColumn, "AutoUprightButton", "UPRIGHT", new Vector2(0f, 132f), new Color(0.92f, 0.92f, 0.92f, 1f), out _, out _);
        Button rollButton = BuildOrGetActionButton(actionColumn, "RollButton", "ROLL", new Vector2(0f, -132f), new Color(0.92f, 0.92f, 0.92f, 1f), out _, out _);

        SerializedObject so = new SerializedObject(panelUi);
        so.FindProperty("zodiacNameText").objectReferenceValue = zodiacName;
        so.FindProperty("targetStatusText").objectReferenceValue = targetText;
        so.FindProperty("selectionRuleText").objectReferenceValue = ruleText;
        so.FindProperty("useButton").objectReferenceValue = useButton;
        so.FindProperty("useButtonText").objectReferenceValue = useLabel;
        so.FindProperty("useButtonBackground").objectReferenceValue = useBackground;
        so.FindProperty("cancelButton").objectReferenceValue = cancelButton;
        so.FindProperty("cancelButtonText").objectReferenceValue = cancelLabel;
        so.FindProperty("cancelButtonBackground").objectReferenceValue = cancelBackground;
        so.FindProperty("autoUprightButton").objectReferenceValue = autoUprightButton;
        so.FindProperty("rollButton").objectReferenceValue = rollButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(root);
        DiceFaceHighlightMetadataSetupTool.GenerateForSceneDice();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = root;
    }

    private static RectTransform BuildOrGetFrame(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject go = FindOrCreateChild(parent, name, typeof(RectTransform), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = Color.white;
        return rt;
    }

    private static Button BuildOrGetActionButton(Transform parent, string name, string text, Vector2 anchoredPosition, Color backgroundColor, out TMP_Text label, out Image image)
    {
        GameObject buttonGo = FindOrCreateChild(parent, name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = buttonGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = new Vector2(144f, 56f);

        image = buttonGo.GetComponent<Image>();
        image.color = backgroundColor;

        Button button = buttonGo.GetComponent<Button>();
        button.targetGraphic = image;

        label = GetOrCreateText(buttonGo.transform, "Label", text, 22, FontStyles.Bold, TextAlignmentOptions.Center);
        StretchToFill(label.rectTransform, new Vector2(8f, 8f), new Vector2(-8f, -8f));
        return button;
    }

    private static TMP_Text GetOrCreateText(Transform parent, string name, string text, float size, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject go = FindOrCreateChild(parent, name, typeof(RectTransform), typeof(TextMeshProUGUI));
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.color = Color.black;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void StretchToFill(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    private static Canvas FindOrCreateCanvas()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>(true);
        if (canvas != null)
            return canvas;

        GameObject canvasGo = new GameObject("SampleSceneUiCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create SampleScene UI Canvas");

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
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing.gameObject;

        GameObject go = new GameObject(name, components);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.transform.SetParent(parent, false);
        return go;
    }
}

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class MapPrototypeSetupTool
{
    [MenuItem("Tools/Build Synergy/Setup Map Prototype Demo")]
    public static void SetupMapPrototypeDemo()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
            canvas = CreateCanvas();

        EnsureEventSystem();

        MapPrototypeController controller = Object.FindObjectOfType<MapPrototypeController>();
        if (controller == null)
        {
            GameObject root = new GameObject("MapPrototypeDemo", typeof(RectTransform), typeof(Image), typeof(MapPrototypeController));
            Undo.RegisterCreatedObjectUndo(root, "Create Map Prototype Demo");
            root.transform.SetParent(canvas.transform, false);
            controller = root.GetComponent<MapPrototypeController>();
        }

        Undo.RecordObject(controller, "Apply Map Prototype HTML Defaults");
        controller.ApplyHtmlSourceOfTruthDefaults();
        controller.RebuildPrototypeUi();
        controller.ResetAct();
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = controller.gameObject;
    }

    private static Canvas CreateCanvas()
    {
        GameObject go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(go, "Create Canvas");

        Canvas canvas = go.GetComponent<Canvas>();
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
        EventSystem eventSystem = Object.FindObjectOfType<EventSystem>();
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
}
#endif

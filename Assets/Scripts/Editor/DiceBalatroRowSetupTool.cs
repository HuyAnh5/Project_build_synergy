using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class DiceBalatroRowSetupTool
{
    private const string PanelName = "DicePanel";
    private const string RowName = "DiceRow";
    private const string DragLayerName = "DiceDragLayer";

    [MenuItem("Tools/Build Synergy/Setup Balatro Dice Row")]
    public static void SetupBalatroDiceRow()
    {
        Canvas canvas = FindOrCreateCanvas();
        EnsureEventSystem();

        DiceEquipUIManager manager = Object.FindFirstObjectByType<DiceEquipUIManager>(FindObjectsInactive.Include);
        if (manager == null)
            manager = CreateManager(canvas.transform);

        RectTransform panel = manager.transform as RectTransform;
        ConfigurePanel(panel);

        RectTransform row = FindOrCreateChild(panel, RowName, typeof(RectTransform)).GetComponent<RectTransform>();
        ConfigureRow(row);

        RectTransform dragLayer = FindOrCreateChild(canvas.transform, DragLayerName, typeof(RectTransform)).GetComponent<RectTransform>();
        ConfigureDragLayer(dragLayer);
        dragLayer.SetAsLastSibling();

        CleanupLegacyDiceUi(panel, row, dragLayer);
        ApplyManagerBindings(manager, row, dragLayer);

        List<DiceSpinnerGeneric> orderedDice = CollectOrderedDice(manager);
        for (int i = 0; i < orderedDice.Count; i++)
            CreateDiceUiCard(row, manager, orderedDice[i], i);

        manager.RebuildFromChildren();
        manager.SyncOutputs();

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(panel.gameObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = manager.gameObject;
    }

    private static DiceEquipUIManager CreateManager(Transform canvas)
    {
        GameObject panelGo = FindOrCreateChild(canvas, PanelName, typeof(RectTransform));
        ConfigurePanel(panelGo.GetComponent<RectTransform>());

        DiceEquipUIManager manager = panelGo.GetComponent<DiceEquipUIManager>();
        if (manager == null)
            manager = Undo.AddComponent<DiceEquipUIManager>(panelGo);

        return manager;
    }

    private static void ApplyManagerBindings(DiceEquipUIManager manager, RectTransform row, RectTransform dragLayer)
    {
        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("layoutContainer").objectReferenceValue = row;
        so.FindProperty("dragLayer").objectReferenceValue = dragLayer;
        so.FindProperty("runInventory").objectReferenceValue = Object.FindFirstObjectByType<RunInventoryManager>(FindObjectsInactive.Include);
        so.FindProperty("diceRig").objectReferenceValue = Object.FindFirstObjectByType<DiceSlotRig>(FindObjectsInactive.Include);
        so.FindProperty("turnManager").objectReferenceValue = Object.FindFirstObjectByType<TurnManager>(FindObjectsInactive.Include);
        so.FindProperty("spacing").floatValue = 190f;
        so.FindProperty("rowY").floatValue = 0f;
        so.FindProperty("diceUiSize").vector2Value = new Vector2(100f, 100f);
        so.FindProperty("autoCreateMissingUi").boolValue = true;
        so.FindProperty("lockWhenCombatManagerExists").boolValue = true;
        so.FindProperty("enableGroupedSkillDiceReorder").boolValue = true;

        SerializedProperty anchors = so.FindProperty("equipSlotAnchors");
        anchors.arraySize = 0;

        SerializedProperty linkedCombat = so.FindProperty("linkedCombatSlotAnchors");
        RectTransform[] combatSlots = FindCombatSlotAnchors();
        if (combatSlots.Length > 0)
        {
            linkedCombat.arraySize = combatSlots.Length;
            for (int i = 0; i < combatSlots.Length; i++)
                linkedCombat.GetArrayElementAtIndex(i).objectReferenceValue = combatSlots[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static List<DiceSpinnerGeneric> CollectOrderedDice(DiceEquipUIManager manager)
    {
        List<DiceSpinnerGeneric> dice = new List<DiceSpinnerGeneric>(RunInventoryManager.EQUIPPED_DICE_COUNT);
        RunInventoryManager inventory = manager.runInventory != null
            ? manager.runInventory
            : Object.FindFirstObjectByType<RunInventoryManager>(FindObjectsInactive.Include);

        if (inventory != null)
        {
            for (int i = 0; i < RunInventoryManager.EQUIPPED_DICE_COUNT; i++)
            {
                DiceSpinnerGeneric equipped = inventory.GetEquippedDice(i);
                if (equipped != null)
                    dice.Add(equipped);
            }
        }

        if (dice.Count > 0)
            return dice;

        DiceSlotRig rig = manager.diceRig != null
            ? manager.diceRig
            : Object.FindFirstObjectByType<DiceSlotRig>(FindObjectsInactive.Include);

        if (rig != null && rig.slots != null)
        {
            for (int i = 0; i < rig.slots.Length; i++)
            {
                DiceSpinnerGeneric equipped = rig.slots[i] != null ? rig.slots[i].dice : null;
                if (equipped != null)
                    dice.Add(equipped);
            }
        }

        return dice;
    }

    private static void CreateDiceUiCard(RectTransform row, DiceEquipUIManager manager, DiceSpinnerGeneric dice, int orderIndex)
    {
        if (row == null || manager == null || dice == null)
            return;

        GameObject go = new GameObject($"DiceCard_{dice.name}", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(DiceDraggableUI));
        Undo.RegisterCreatedObjectUndo(go, $"Create Dice UI for {dice.name}");
        go.layer = row.gameObject.layer;
        go.transform.SetParent(row, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = manager.diceUiSize;

        Image image = go.GetComponent<Image>();
        image.color = DiceEquipUIManager.GetDefaultDiceUiColor(orderIndex);
        image.raycastTarget = true;

        DiceDraggableUI diceUi = go.GetComponent<DiceDraggableUI>();
        diceUi.dice = dice;
        diceUi.manager = manager;
        diceUi.backgroundImage = image;
        diceUi.tweenDuration = manager.itemSnapDuration;
        EditorUtility.SetDirty(go);
    }

    private static void CleanupLegacyDiceUi(RectTransform panel, RectTransform row, RectTransform dragLayer)
    {
        HashSet<GameObject> toDestroy = new HashSet<GameObject>();

        if (panel != null)
        {
            DiceDraggableUI[] ui = panel.GetComponentsInChildren<DiceDraggableUI>(true);
            for (int i = 0; i < ui.Length; i++)
            {
                if (ui[i] != null)
                    toDestroy.Add(ui[i].gameObject);
            }

            DiceEquipDropZone[] dropZones = panel.GetComponentsInChildren<DiceEquipDropZone>(true);
            for (int i = 0; i < dropZones.Length; i++)
            {
                if (dropZones[i] != null)
                    toDestroy.Add(dropZones[i].gameObject);
            }

            foreach (Transform child in panel)
            {
                if (child == null || child == row)
                    continue;

                if (child.name.StartsWith("DiceSlot"))
                    toDestroy.Add(child.gameObject);
            }
        }

        if (dragLayer != null)
        {
            DiceDraggableUI[] dragUi = dragLayer.GetComponentsInChildren<DiceDraggableUI>(true);
            for (int i = 0; i < dragUi.Length; i++)
            {
                if (dragUi[i] != null)
                    toDestroy.Add(dragUi[i].gameObject);
            }
        }

        foreach (GameObject go in toDestroy)
        {
            if (go == null || go == row.gameObject || go == panel.gameObject || (dragLayer != null && go == dragLayer.gameObject))
                continue;

            Undo.DestroyObjectImmediate(go);
        }
    }

    private static RectTransform[] FindCombatSlotAnchors()
    {
        List<RectTransform> found = new List<RectTransform>(3);
        string[] names = { "Slot1", "Slot2", "Slot3" };

        RectTransform[] all = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < names.Length; i++)
        {
            for (int j = 0; j < all.Length; j++)
            {
                RectTransform rt = all[j];
                if (rt != null && rt.name == names[i])
                {
                    found.Add(rt);
                    break;
                }
            }
        }

        return found.ToArray();
    }

    private static void ConfigurePanel(RectTransform panel)
    {
        if (panel == null)
            return;

        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        if (panel.anchoredPosition.sqrMagnitude < 0.01f)
            panel.anchoredPosition = new Vector2(0f, -160f);
        if (panel.sizeDelta.x < 1f || panel.sizeDelta.y < 1f)
            panel.sizeDelta = new Vector2(640f, 240f);
    }

    private static void ConfigureRow(RectTransform row)
    {
        if (row == null)
            return;

        row.anchorMin = Vector2.zero;
        row.anchorMax = Vector2.one;
        row.pivot = new Vector2(0.5f, 0.5f);
        row.offsetMin = Vector2.zero;
        row.offsetMax = Vector2.zero;
        row.anchoredPosition = Vector2.zero;
    }

    private static void ConfigureDragLayer(RectTransform dragLayer)
    {
        if (dragLayer == null)
            return;

        dragLayer.anchorMin = Vector2.zero;
        dragLayer.anchorMax = Vector2.one;
        dragLayer.pivot = new Vector2(0.5f, 0.5f);
        dragLayer.offsetMin = Vector2.zero;
        dragLayer.offsetMax = Vector2.zero;
        dragLayer.anchoredPosition = Vector2.zero;
    }

    private static Canvas FindOrCreateCanvas()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
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
        if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
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

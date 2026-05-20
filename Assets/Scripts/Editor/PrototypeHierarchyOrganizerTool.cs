#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrototypeHierarchyOrganizerTool
{
    private const string PrototypeScenePath = "Assets/Scenes/Prototype.unity";
    private const string WorldGroupName = "----- WORLD -----";
    private const string SystemsGroupName = "----- SYSTEMS -----";
    private const string UiGroupName = "----- UI -----";
    private const string OtherGroupName = "----- OTHER -----";
    private const string CanvasHudGroupName = "----- HUD -----";
    private const string CanvasCombatUiGroupName = "----- COMBAT UI -----";
    private const string CanvasDragGroupName = "----- DRAG -----";
    private const string CanvasTooltipGroupName = "----- TOOLTIP -----";
    private const string CanvasPrototypeGroupName = "----- PROTOTYPE -----";

    [MenuItem("Tools/Build Synergy/Prototype/Organize Prototype Hierarchy")]
    public static void OrganizePrototypeHierarchy()
    {
        Scene scene = EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[PrototypeHierarchyOrganizerTool] Failed to open scene at {PrototypeScenePath}.");
            return;
        }

        Dictionary<string, Transform> groups = EnsureGroups(scene);
        ReparentRoots(scene, groups);
        OrganizeCanvasChildren(groups[UiGroupName]);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[PrototypeHierarchyOrganizerTool] Prototype hierarchy organized.");
    }

    private static Dictionary<string, Transform> EnsureGroups(Scene scene)
    {
        Dictionary<string, Transform> groups = new Dictionary<string, Transform>(StringComparer.Ordinal)
        {
            [WorldGroupName] = FindOrCreateRoot(scene, WorldGroupName),
            [SystemsGroupName] = FindOrCreateRoot(scene, SystemsGroupName),
            [UiGroupName] = FindOrCreateRoot(scene, UiGroupName),
            [OtherGroupName] = FindOrCreateRoot(scene, OtherGroupName),
        };

        groups[WorldGroupName].SetSiblingIndex(0);
        groups[SystemsGroupName].SetSiblingIndex(1);
        groups[UiGroupName].SetSiblingIndex(2);
        groups[OtherGroupName].SetSiblingIndex(3);
        return groups;
    }

    private static void ReparentRoots(Scene scene, IReadOnlyDictionary<string, Transform> groups)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        List<Transform> world = new List<Transform>();
        List<Transform> systems = new List<Transform>();
        List<Transform> ui = new List<Transform>();
        List<Transform> other = new List<Transform>();

        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null)
                continue;

            string name = root.name;
            if (IsGroupName(name))
                continue;

            Transform targetGroup = ClassifyRoot(root, groups);
            if (targetGroup == null)
                continue;

            root.transform.SetParent(targetGroup, true);

            if (targetGroup == groups[WorldGroupName]) world.Add(root.transform);
            else if (targetGroup == groups[SystemsGroupName]) systems.Add(root.transform);
            else if (targetGroup == groups[UiGroupName]) ui.Add(root.transform);
            else other.Add(root.transform);
        }

        SortChildren(world);
        SortChildren(systems);
        SortChildren(ui);
        SortChildren(other);
    }

    private static Transform ClassifyRoot(GameObject root, IReadOnlyDictionary<string, Transform> groups)
    {
        string name = root.name;

        if (IsUiRoot(name, root))
            return groups[UiGroupName];

        if (IsWorldRoot(name))
            return groups[WorldGroupName];

        if (IsSystemRoot(name))
            return groups[SystemsGroupName];

        return groups[OtherGroupName];
    }

    private static bool IsUiRoot(string name, GameObject root)
    {
        return root.GetComponent<Canvas>() != null ||
               string.Equals(name, "EventSystem", StringComparison.OrdinalIgnoreCase) ||
               name.IndexOf("Canvas", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("Screen_", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsWorldRoot(string name)
    {
        return string.Equals(name, "Main Camera", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(name, "dice", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(name, "PoolBoss", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSystemRoot(string name)
    {
        return string.Equals(name, "BattleSystems", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(name, "RunManager", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(name, "GameplayDiceEditController", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(name, "CombatLabPrototypeRoot", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGroupName(string name)
    {
        return string.Equals(name, WorldGroupName, StringComparison.Ordinal) ||
               string.Equals(name, SystemsGroupName, StringComparison.Ordinal) ||
               string.Equals(name, UiGroupName, StringComparison.Ordinal) ||
               string.Equals(name, OtherGroupName, StringComparison.Ordinal);
    }

    private static Transform FindOrCreateRoot(Scene scene, string name)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && string.Equals(roots[i].name, name, StringComparison.Ordinal))
                return roots[i].transform;
        }

        GameObject go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return go.transform;
    }

    private static void SortChildren(List<Transform> children)
    {
        children.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        for (int i = 0; i < children.Count; i++)
            children[i].SetSiblingIndex(i);
    }

    private static void OrganizeCanvasChildren(Transform uiGroup)
    {
        if (uiGroup == null)
            return;

        Canvas canvas = uiGroup.GetComponentInChildren<Canvas>(true);
        if (canvas == null)
            return;

        Transform canvasTransform = canvas.transform;
        Dictionary<string, Transform> canvasGroups = EnsureCanvasGroups(canvasTransform);
        List<Transform> hud = new List<Transform>();
        List<Transform> combatUi = new List<Transform>();
        List<Transform> drag = new List<Transform>();
        List<Transform> tooltip = new List<Transform>();
        List<Transform> prototype = new List<Transform>();

        List<Transform> directChildren = new List<Transform>();
        for (int i = 0; i < canvasTransform.childCount; i++)
            directChildren.Add(canvasTransform.GetChild(i));

        for (int i = 0; i < directChildren.Count; i++)
        {
            Transform child = directChildren[i];
            if (child == null || IsCanvasGroupName(child.name))
                continue;

            Transform targetGroup = ClassifyCanvasChild(child, canvasGroups);
            child.SetParent(targetGroup, true);

            if (targetGroup == canvasGroups[CanvasHudGroupName]) hud.Add(child);
            else if (targetGroup == canvasGroups[CanvasCombatUiGroupName]) combatUi.Add(child);
            else if (targetGroup == canvasGroups[CanvasDragGroupName]) drag.Add(child);
            else if (targetGroup == canvasGroups[CanvasTooltipGroupName]) tooltip.Add(child);
            else prototype.Add(child);
        }

        SortChildren(hud);
        SortChildren(combatUi);
        SortChildren(drag);
        SortChildren(tooltip);
        SortChildren(prototype);
    }

    private static Dictionary<string, Transform> EnsureCanvasGroups(Transform canvasTransform)
    {
        Dictionary<string, Transform> groups = new Dictionary<string, Transform>(StringComparer.Ordinal)
        {
            [CanvasHudGroupName] = FindOrCreateChild(canvasTransform, CanvasHudGroupName),
            [CanvasCombatUiGroupName] = FindOrCreateChild(canvasTransform, CanvasCombatUiGroupName),
            [CanvasDragGroupName] = FindOrCreateChild(canvasTransform, CanvasDragGroupName),
            [CanvasTooltipGroupName] = FindOrCreateChild(canvasTransform, CanvasTooltipGroupName),
            [CanvasPrototypeGroupName] = FindOrCreateChild(canvasTransform, CanvasPrototypeGroupName),
        };

        groups[CanvasHudGroupName].SetSiblingIndex(0);
        groups[CanvasCombatUiGroupName].SetSiblingIndex(1);
        groups[CanvasDragGroupName].SetSiblingIndex(2);
        groups[CanvasTooltipGroupName].SetSiblingIndex(3);
        groups[CanvasPrototypeGroupName].SetSiblingIndex(4);
        return groups;
    }

    private static Transform ClassifyCanvasChild(Transform child, IReadOnlyDictionary<string, Transform> groups)
    {
        string name = child.name;
        if (name.IndexOf("DragLayer", StringComparison.OrdinalIgnoreCase) >= 0)
            return groups[CanvasDragGroupName];

        if (name.IndexOf("Tooltip", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("RuntimeRoot", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return groups[CanvasTooltipGroupName];
        }

        if (string.Equals(name, "GameplayDiceEditPanel", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "CombatLabPrototypeUI", StringComparison.OrdinalIgnoreCase))
        {
            return groups[CanvasPrototypeGroupName];
        }

        if (string.Equals(name, "SkillBar", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "PlayerStats", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "EnemyStats", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "ConsumableHudRoot", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "PopUpDMG", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "Selfzone_Cast", StringComparison.OrdinalIgnoreCase))
        {
            return groups[CanvasCombatUiGroupName];
        }

        return groups[CanvasHudGroupName];
    }

    private static bool IsCanvasGroupName(string name)
    {
        return string.Equals(name, CanvasHudGroupName, StringComparison.Ordinal) ||
               string.Equals(name, CanvasCombatUiGroupName, StringComparison.Ordinal) ||
               string.Equals(name, CanvasDragGroupName, StringComparison.Ordinal) ||
               string.Equals(name, CanvasTooltipGroupName, StringComparison.Ordinal) ||
               string.Equals(name, CanvasPrototypeGroupName, StringComparison.Ordinal);
    }

    private static Transform FindOrCreateChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing;

        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.transform.SetParent(parent, false);
        return go.transform;
    }
}
#endif

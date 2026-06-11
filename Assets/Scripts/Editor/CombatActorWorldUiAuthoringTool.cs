using UnityEditor;
using UnityEngine;

public static class CombatActorWorldUiAuthoringTool
{
    [MenuItem("Tools/Build Synergy/Combat/Mark Selected Actors As Boss World UI")]
    public static void MarkSelectedActorsAsBossWorldUi()
    {
        ApplyWorldUiTagToSelection("Boss", alignAnchorToVisualCenter: false);
    }

    [MenuItem("Tools/Build Synergy/Combat/Mark Selected Actors As Standard World UI")]
    public static void MarkSelectedActorsAsStandardWorldUi()
    {
        ApplyWorldUiTagToSelection(CombatActor.DefaultWorldUiTag, alignAnchorToVisualCenter: true);
    }

    [MenuItem("Tools/Build Synergy/Combat/Create Or Refresh UIAnchor For Selected Actors")]
    public static void RefreshSelectedActorUiAnchors()
    {
        CombatActor[] actors = Selection.GetFiltered<CombatActor>(SelectionMode.Editable | SelectionMode.TopLevel);
        if (actors.Length == 0)
        {
            Debug.LogWarning("Select one or more CombatActor roots first.");
            return;
        }

        foreach (CombatActor actor in actors)
        {
            EnsureUiAnchor(actor, alignToVisualCenter: true);
            EditorUtility.SetDirty(actor);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Refreshed UIAnchor for {actors.Length} CombatActor object(s).");
    }

    private static void ApplyWorldUiTagToSelection(string tag, bool alignAnchorToVisualCenter)
    {
        CombatActor[] actors = Selection.GetFiltered<CombatActor>(SelectionMode.Editable | SelectionMode.TopLevel);
        if (actors.Length == 0)
        {
            Debug.LogWarning("Select one or more CombatActor roots first.");
            return;
        }

        foreach (CombatActor actor in actors)
        {
            Undo.RecordObject(actor, $"Set {nameof(CombatActor.worldUiTag)}");
            actor.worldUiTag = string.IsNullOrWhiteSpace(tag) ? CombatActor.DefaultWorldUiTag : tag.Trim();
            EnsureUiAnchor(actor, alignAnchorToVisualCenter);
            EditorUtility.SetDirty(actor);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Updated {actors.Length} CombatActor object(s) to world UI tag '{tag}'.");
    }

    private static void EnsureUiAnchor(CombatActor actor, bool alignToVisualCenter)
    {
        if (actor == null)
            return;

        if (actor.uiAnchor == null)
        {
            Transform existing = actor.transform.Find(actor.uiAnchorName);
            if (existing != null)
            {
                actor.uiAnchor = existing;
            }
            else
            {
                GameObject anchorGo = new GameObject(actor.uiAnchorName);
                Undo.RegisterCreatedObjectUndo(anchorGo, "Create UIAnchor");
                actor.uiAnchor = anchorGo.transform;
                actor.uiAnchor.SetParent(actor.transform, false);
            }
        }

        actor.autoSetupUiAnchor = false;

        if (!alignToVisualCenter || actor.uiAnchor == null)
            return;

        actor.uiAnchor.localPosition = ComputeVisualCenterLocalPosition(actor.transform, actor.uiAnchor);
        actor.uiAnchor.localRotation = Quaternion.identity;
        actor.uiAnchor.localScale = Vector3.one;
    }

    private static Vector3 ComputeVisualCenterLocalPosition(Transform root, Transform ignoredAnchor)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Bounds combined = default;
        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
                continue;

            if (ignoredAnchor != null && renderer.transform == ignoredAnchor)
                continue;

            if (renderer is ParticleSystemRenderer)
                continue;

            if (!hasBounds)
            {
                combined = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combined.Encapsulate(renderer.bounds);
            }
        }

        Vector3 worldCenter = hasBounds ? combined.center : root.position;
        return root.InverseTransformPoint(worldCenter);
    }
}

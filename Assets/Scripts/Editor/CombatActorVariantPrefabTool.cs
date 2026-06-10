using System.IO;
using UnityEditor;
using UnityEngine;

public static class CombatActorVariantPrefabTool
{
    private const string BossWorldUiSourcePath = "Assets/Prefabs/Entities/world-ui.prefab";

    [MenuItem("Tools/Build Synergy/Combat/Create Enemy And Boss Variants From Selected Prefab")]
    public static void CreateVariantsFromSelectedPrefab()
    {
        GameObject selectedPrefab = Selection.activeObject as GameObject;
        if (selectedPrefab == null)
        {
            Debug.LogWarning("Select a CombatActor prefab asset first.");
            return;
        }

        string selectedPath = AssetDatabase.GetAssetPath(selectedPrefab);
        if (string.IsNullOrWhiteSpace(selectedPath) || !selectedPath.EndsWith(".prefab"))
        {
            Debug.LogWarning("Selected object is not a prefab asset.");
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(selectedPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"Could not load prefab contents at {selectedPath}.");
            return;
        }

        try
        {
            CombatActor sourceActor = prefabRoot.GetComponent<CombatActor>();
            if (sourceActor == null)
            {
                Debug.LogWarning("Selected prefab does not contain a CombatActor on the root.");
                return;
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        string folder = Path.GetDirectoryName(selectedPath)?.Replace('\\', '/');
        string baseName = Path.GetFileNameWithoutExtension(selectedPath);

        string enemyPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{baseName}_Enemy.prefab");
        string bossPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{baseName}_Boss.prefab");

        if (!AssetDatabase.CopyAsset(selectedPath, enemyPath))
        {
            Debug.LogError($"Failed to create enemy variant at {enemyPath}.");
            return;
        }

        if (!AssetDatabase.CopyAsset(selectedPath, bossPath))
        {
            Debug.LogError($"Failed to create boss variant at {bossPath}.");
            return;
        }

        ConfigureVariant(enemyPath, CombatActor.WorldUiMode.Standard, embedBossWorldUi: false);
        ConfigureVariant(bossPath, CombatActor.WorldUiMode.Boss, embedBossWorldUi: true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Object bossAsset = AssetDatabase.LoadAssetAtPath<Object>(bossPath);
        if (bossAsset != null)
            Selection.activeObject = bossAsset;

        Debug.Log($"Created variants:\nEnemy: {enemyPath}\nBoss: {bossPath}");
    }

    private static void ConfigureVariant(string prefabPath, CombatActor.WorldUiMode worldUiMode, bool embedBossWorldUi)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
            return;

        try
        {
            CombatActor actor = root.GetComponent<CombatActor>();
            if (actor == null)
            {
                Debug.LogWarning($"Prefab at {prefabPath} no longer contains a CombatActor.");
                return;
            }

            actor.worldUiMode = worldUiMode;
            EnsureUiAnchor(actor, alignToVisualCenter: worldUiMode == CombatActor.WorldUiMode.Standard);

            if (embedBossWorldUi)
                EnsureEmbeddedBossWorldUi(root, actor);
            else
                RemoveEmbeddedWorldUis(root);

            EditorUtility.SetDirty(actor);
            EditorUtility.SetDirty(root);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void EnsureEmbeddedBossWorldUi(GameObject root, CombatActor actor)
    {
        if (actor == null)
            return;

        ActorWorldUI existingUi = FindEmbeddedWorldUi(actor);
        if (existingUi != null)
        {
            existingUi.gameObject.name = "BossWorldUI";
            return;
        }

        GameObject worldUiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossWorldUiSourcePath);
        if (worldUiPrefab == null)
        {
            Debug.LogWarning($"Missing boss world UI source prefab at {BossWorldUiSourcePath}.");
            return;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(worldUiPrefab) as GameObject;
        if (instance == null)
            return;

        instance.name = "BossWorldUI";
        Transform parent = actor.uiAnchor != null ? actor.uiAnchor : actor.transform;
        instance.transform.SetParent(parent, false);

        ActorWorldUI worldUi = instance.GetComponent<ActorWorldUI>();
        if (worldUi != null)
        {
            worldUi.actor = actor;
            worldUi.Bind(actor);
            EditorUtility.SetDirty(worldUi);
        }
    }

    private static void RemoveEmbeddedWorldUis(GameObject root)
    {
        ActorWorldUI[] worldUis = root.GetComponentsInChildren<ActorWorldUI>(true);
        for (int i = 0; i < worldUis.Length; i++)
        {
            ActorWorldUI ui = worldUis[i];
            if (ui == null)
                continue;

            if (ui.transform == root.transform)
                continue;

            Object.DestroyImmediate(ui.gameObject);
        }
    }

    private static ActorWorldUI FindEmbeddedWorldUi(CombatActor actor)
    {
        ActorWorldUI[] worldUis = actor.GetComponentsInChildren<ActorWorldUI>(true);
        for (int i = 0; i < worldUis.Length; i++)
        {
            ActorWorldUI ui = worldUis[i];
            if (ui == null)
                continue;

            if (ui.transform == actor.transform)
                continue;

            return ui;
        }

        return null;
    }

    private static void EnsureUiAnchor(CombatActor actor, bool alignToVisualCenter)
    {
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

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
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

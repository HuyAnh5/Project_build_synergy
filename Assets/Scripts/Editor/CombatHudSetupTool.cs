using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CombatHudSetupTool
{
    [MenuItem("Tools/Build Synergy/Legacy/Setup Player AP Bar")]
    public static void SetupPlayerApBar()
    {
        CombatHUD hud = Object.FindFirstObjectByType<CombatHUD>(FindObjectsInactive.Include);
        if (hud == null)
        {
            Debug.LogWarning("[CombatHudSetupTool] No CombatHUD found in the active scene.");
            return;
        }

        hud.SetupPlayerFocusBarUi();
        EditorUtility.SetDirty(hud);
        if (hud.playerFocusBarRoot != null)
            EditorUtility.SetDirty(hud.playerFocusBarRoot.gameObject);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = hud.playerFocusBarRoot != null ? hud.playerFocusBarRoot.gameObject : hud.gameObject;
    }
}

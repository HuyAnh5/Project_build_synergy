using UnityEngine;
using UnityEngine.SceneManagement;

public static class DiceEditRuntimeBootstrap
{
    private const string SampleSceneName = "SampleScene";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || activeScene.name != SampleSceneName)
            return;

        if (Object.FindFirstObjectByType<DiceEditSandboxController>(FindObjectsInactive.Include) != null)
            return;

        GameObject root = new GameObject("DiceEditSandboxRuntime");
        Object.DontDestroyOnLoad(root);

        DiceEditSandboxController controller = root.AddComponent<DiceEditSandboxController>();
        controller.InitializeForScene(activeScene);
    }
}

using UnityEngine;

[DisallowMultipleComponent]
public sealed class SkillTooltipPrefabProvider : MonoBehaviour
{
    [SerializeField] private GameObject skillTooltipPrefab;
    [SerializeField] private SkillTooltipKeywordTooltipTemplate keywordTooltipPrefab;

    public GameObject SkillTooltipPrefab => skillTooltipPrefab;
    public SkillTooltipKeywordTooltipTemplate KeywordTooltipPrefab => keywordTooltipPrefab;

    private void Awake()
    {
        SkillTooltipPrefabProviderRegistry.Register(this);
    }

    private void OnDestroy()
    {
        SkillTooltipPrefabProviderRegistry.Unregister(this);
    }
}

internal static class SkillTooltipPrefabProviderRegistry
{
    private static SkillTooltipPrefabProvider _instance;

    public static void Register(SkillTooltipPrefabProvider provider)
    {
        if (provider == null)
            return;

        _instance = provider;
    }

    public static void Unregister(SkillTooltipPrefabProvider provider)
    {
        if (_instance == provider)
            _instance = null;
    }

    public static SkillTooltipPrefabProvider Get()
    {
        if (_instance != null)
            return _instance;

#if UNITY_2023_1_OR_NEWER
        _instance = Object.FindFirstObjectByType<SkillTooltipPrefabProvider>(FindObjectsInactive.Include);
#else
        _instance = Object.FindObjectOfType<SkillTooltipPrefabProvider>(true);
#endif
        return _instance;
    }
}

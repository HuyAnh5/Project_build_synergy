using UnityEngine;

[CreateAssetMenu(menuName = "UI/Skill Tooltip Prefab Settings", fileName = "SkillTooltipPrefabSettings")]
public sealed class SkillTooltipPrefabSettingsSO : ScriptableObject
{
    [SerializeField] private GameObject skillTooltipPrefab;
    [SerializeField] private SkillTooltipKeywordTooltipTemplate keywordTooltipPrefab;

    public GameObject SkillTooltipPrefab => skillTooltipPrefab;
    public SkillTooltipKeywordTooltipTemplate KeywordTooltipPrefab => keywordTooltipPrefab;
}

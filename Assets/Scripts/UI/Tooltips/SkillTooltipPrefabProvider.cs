using UnityEngine;

[DisallowMultipleComponent]
public sealed class SkillTooltipPrefabProvider : MonoBehaviour
{
    [SerializeField] private GameObject skillTooltipPrefab;
    [SerializeField] private SkillTooltipKeywordTooltipTemplate keywordTooltipPrefab;

    public GameObject SkillTooltipPrefab => skillTooltipPrefab;
    public SkillTooltipKeywordTooltipTemplate KeywordTooltipPrefab => keywordTooltipPrefab;
}

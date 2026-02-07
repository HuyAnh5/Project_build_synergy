using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Game/Skill Database")]
public class SkillDatabaseSO : ScriptableObject
{
    [Title("All Skills")]
    [ListDrawerSettings(Expanded = true, DraggableItems = true, ShowIndexLabels = true)]
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    public List<SkillSO> skills = new();

#if UNITY_EDITOR
    [Button(ButtonSizes.Medium)]
    private void SortByName()
    {
        skills.Sort((a, b) => string.Compare(a ? a.name : "", b ? b.name : "", System.StringComparison.Ordinal));
    }
#endif
}

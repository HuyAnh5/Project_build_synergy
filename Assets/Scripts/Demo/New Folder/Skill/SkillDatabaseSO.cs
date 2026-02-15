using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Game/Skill Database", fileName = "Database_Skills")]
public class SkillDatabaseSO : ScriptableObject
{
    [Title("Skill Database (V2)", bold: true)]
    [InfoBox("New skills should be created in Damage/BuffDebuff/Passive lists. The Legacy SkillSO list is kept temporarily for backward compatibility while refactoring runtime.", InfoMessageType.Info)]
    [PropertySpace(6)]

    [TabGroup("Skills", "Damage")]
    [ListDrawerSettings(Expanded = true, DraggableItems = true, ShowIndexLabels = true)]
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    public List<SkillDamageSO> damageSkills = new();

    [TabGroup("Skills", "BuffDebuff")]
    [ListDrawerSettings(Expanded = true, DraggableItems = true, ShowIndexLabels = true)]
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    public List<SkillBuffDebuffSO> buffDebuffSkills = new();

    [TabGroup("Skills", "Passive")]
    [ListDrawerSettings(Expanded = true, DraggableItems = true, ShowIndexLabels = true)]
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    public List<SkillPassiveSO> passiveSkills = new();

    // Kept to avoid breaking current code until you refactor all references away from SkillSO.
    [TabGroup("Skills", "Legacy (SkillSO)")]
    [InfoBox("Legacy list: old SkillSO assets. Keep for now if your current UI/runtime still reads SkillSO.", InfoMessageType.Warning)]
    [ListDrawerSettings(Expanded = true, DraggableItems = true, ShowIndexLabels = true)]
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    public List<SkillSO> skills = new();

#if UNITY_EDITOR
    [TabGroup("Skills", "Damage")]
    [Button(ButtonSizes.Medium)]
    private void SortDamageByName() =>
        damageSkills.Sort((a, b) => string.Compare(a ? a.name : "", b ? b.name : "", System.StringComparison.Ordinal));

    [TabGroup("Skills", "BuffDebuff")]
    [Button(ButtonSizes.Medium)]
    private void SortBuffDebuffByName() =>
        buffDebuffSkills.Sort((a, b) => string.Compare(a ? a.name : "", b ? b.name : "", System.StringComparison.Ordinal));

    [TabGroup("Skills", "Passive")]
    [Button(ButtonSizes.Medium)]
    private void SortPassiveByName() =>
        passiveSkills.Sort((a, b) => string.Compare(a ? a.name : "", b ? b.name : "", System.StringComparison.Ordinal));

    [TabGroup("Skills", "Legacy (SkillSO)")]
    [Button(ButtonSizes.Medium)]
    private void SortLegacyByName() =>
        skills.Sort((a, b) => string.Compare(a ? a.name : "", b ? b.name : "", System.StringComparison.Ordinal));

    [Title("Tools", bold: true)]
    [Button(ButtonSizes.Large)]
    private void SortAllByName()
    {
        SortDamageByName();
        SortBuffDebuffByName();
        SortPassiveByName();
        SortLegacyByName();
    }
#endif
}

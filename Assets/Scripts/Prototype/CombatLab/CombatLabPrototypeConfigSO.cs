using System;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "CombatLabPrototypeConfig",
    menuName = "Build Synergy/Prototype/Combat Lab Config")]
public class CombatLabPrototypeConfigSO : ScriptableObject
{
    [Serializable]
    public sealed class EnemyEntry
    {
        public bool enabled = true;
        public CombatActor prefab;
        public CombatActor.RowTag row = CombatActor.RowTag.Front;
        public int orderInRow;
    }

    [Serializable]
    public sealed class SkillPairEntry
    {
        public string label;
        public ScriptableObject skillA;
        public ScriptableObject skillB;
        public ScriptableObject skillC;
        public ScriptableObject skillD;
    }

    [Header("Encounter")]
    public EnemyEntry[] enemies = new EnemyEntry[3];
    [Header("Dice Prototype Pool")]
    public DiceSpinnerGeneric d4Prefab;
    public DiceSpinnerGeneric d8Prefab;

    [Header("Random Skill Groups")]
    [Tooltip("Author each entry as a 4-skill themed group, such as 4 Fire skills or 4 Ice skills. Each reset, prototype picks 1 group and fills all 4 owned skill slots from it.")]
    [FormerlySerializedAs("skillPairs")]
    public SkillPairEntry[] skillGroups = Array.Empty<SkillPairEntry>();

    [Header("Fixed Consumables")]
    public ConsumableDataSO[] consumables = Array.Empty<ConsumableDataSO>();
}

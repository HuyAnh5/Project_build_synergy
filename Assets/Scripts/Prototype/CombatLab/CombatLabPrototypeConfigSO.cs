using System;
using UnityEngine;

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
    }

    [Header("Encounter")]
    public EnemyEntry[] enemies = new EnemyEntry[3];

    [Header("Dice Prototype Pool")]
    public DiceSpinnerGeneric d4Prefab;
    public DiceSpinnerGeneric d8Prefab;

    [Header("Random Skill Pairs")]
    [Tooltip("Author each pair with 2 skills only. Each reset, prototype picks 2 distinct pairs and fills 4 owned skill slots.")]
    public SkillPairEntry[] skillPairs = Array.Empty<SkillPairEntry>();

    [Header("Fixed Consumables")]
    public ConsumableDataSO[] consumables = Array.Empty<ConsumableDataSO>();
}

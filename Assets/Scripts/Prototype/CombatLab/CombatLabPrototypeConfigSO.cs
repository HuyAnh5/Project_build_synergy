using System;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(
    fileName = "CombatLabPrototypeConfig",
    menuName = "Build Synergy/Prototype/Combat Lab Config")]
public class CombatLabPrototypeConfigSO : ScriptableObject
{
    public const int PrototypeCombatCount = 4;

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

    [Serializable]
    public sealed class EncounterEntry
    {
        public string label;
        public bool isBoss;
        public EnemyEntry[] enemies = new EnemyEntry[3];
    }

    [Header("Encounter")]
    public EnemyEntry[] enemies = new EnemyEntry[3];

    [Header("Prototype Run Enemies")]
    [Tooltip("Enemy setup for Combat 1.")]
    public EncounterEntry combat1 = CreateEncounter("Combat 1", false);
    [Tooltip("Enemy setup for Combat 2.")]
    public EncounterEntry combat2 = CreateEncounter("Combat 2", false);
    [Tooltip("Enemy setup for Combat 3.")]
    public EncounterEntry combat3 = CreateEncounter("Combat 3", false);
    [Tooltip("Enemy setup for Combat 4 / Boss.")]
    public EncounterEntry combat4Boss = CreateEncounter("Combat 4 Boss", true);

    [HideInInspector]
    public EncounterEntry[] runEncounters = new EncounterEntry[PrototypeCombatCount];

    [Header("Dice Prototype Pool")]
    public DiceSpinnerGeneric d4Prefab;
    public DiceSpinnerGeneric d8Prefab;

    [Header("Random Skill Groups")]
    [Tooltip("Author each entry as a 4-skill themed group, such as 4 Fire skills or 4 Ice skills. Each reset, prototype picks 1 group and fills all 4 owned skill slots from it.")]
    [FormerlySerializedAs("skillPairs")]
    public SkillPairEntry[] skillGroups = Array.Empty<SkillPairEntry>();

    [Header("Fixed Consumables")]
    public ConsumableDataSO[] consumables = Array.Empty<ConsumableDataSO>();

    [Header("Prototype Consumable Rewards")]
    [Tooltip("Drag a project folder here. In the editor, every ConsumableDataSO inside that folder becomes the reward pool.")]
    public UnityEngine.Object consumableRewardFolder;
    [Tooltip("Consumables that can appear in prototype reward choices. This is filled from Consumable Reward Folder in the editor, and is what runtime uses.")]
    public ConsumableDataSO[] consumableRewardPool = Array.Empty<ConsumableDataSO>();

    public EncounterEntry GetRunEncounter(int index)
    {
        switch (index)
        {
            case 0: return combat1;
            case 1: return combat2;
            case 2: return combat3;
            case 3: return combat4Boss;
            default: return null;
        }
    }

    private void OnValidate()
    {
        EnsureEncounterDefaults();
#if UNITY_EDITOR
        RefreshConsumableRewardPoolFromFolder();
#endif
    }

    private void EnsureEncounterDefaults()
    {
        combat1 = EnsureEncounter(combat1, "Combat 1", false);
        combat2 = EnsureEncounter(combat2, "Combat 2", false);
        combat3 = EnsureEncounter(combat3, "Combat 3", false);
        combat4Boss = EnsureEncounter(combat4Boss, "Combat 4 Boss", true);

        MigrateLegacyEncounter(0, combat1);
        MigrateLegacyEncounter(1, combat2);
        MigrateLegacyEncounter(2, combat3);
        MigrateLegacyEncounter(3, combat4Boss);
    }

    private void MigrateLegacyEncounter(int index, EncounterEntry target)
    {
        if (target == null || HasAnyEnemy(target.enemies))
            return;
        if (runEncounters == null || index < 0 || index >= runEncounters.Length)
            return;

        EncounterEntry source = runEncounters[index];
        if (source == null || !HasAnyEnemy(source.enemies))
            return;

        target.enemies = source.enemies;
    }

    private static EncounterEntry EnsureEncounter(EncounterEntry entry, string label, bool isBoss)
    {
        if (entry == null)
            entry = CreateEncounter(label, isBoss);
        if (string.IsNullOrWhiteSpace(entry.label))
            entry.label = label;
        entry.isBoss = isBoss;
        if (entry.enemies == null || entry.enemies.Length != 3)
            entry.enemies = new EnemyEntry[3];
        return entry;
    }

    private static EncounterEntry CreateEncounter(string label, bool isBoss)
    {
        return new EncounterEntry
        {
            label = label,
            isBoss = isBoss,
            enemies = new EnemyEntry[3]
        };
    }

    private static bool HasAnyEnemy(EnemyEntry[] entries)
    {
        if (entries == null)
            return false;

        for (int i = 0; i < entries.Length; i++)
        {
            EnemyEntry entry = entries[i];
            if (entry != null && entry.enabled && entry.prefab != null)
                return true;
        }

        return false;
    }

#if UNITY_EDITOR
    private void RefreshConsumableRewardPoolFromFolder()
    {
        if (consumableRewardFolder == null)
            return;

        string folderPath = AssetDatabase.GetAssetPath(consumableRewardFolder);
        if (string.IsNullOrWhiteSpace(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
            return;

        string[] guids = AssetDatabase.FindAssets("t:ConsumableDataSO", new[] { folderPath });
        ConsumableDataSO[] result = new ConsumableDataSO[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            result[i] = AssetDatabase.LoadAssetAtPath<ConsumableDataSO>(path);
        }

        consumableRewardPool = result;
    }
#endif
}

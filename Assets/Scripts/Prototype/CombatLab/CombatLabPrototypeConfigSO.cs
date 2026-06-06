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
    public const int DefaultPrototypeCombatCount = 4;

    [Serializable]
    public sealed class EnemyEntry
    {
        [HideInInspector] public bool enabled = true;
        public CombatActor prefab;
        public CombatActor.RowTag row = CombatActor.RowTag.Front;
        [HideInInspector] public int orderInRow;
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

    [HideInInspector]
    public EnemyEntry[] enemies = new EnemyEntry[3];

    [Header("Prototype Combats")]
    [Tooltip("Prototype combat sequence. Player gets a consumable reward before Combat 1 and after each combat except the final combat.")]
    public EncounterEntry[] combats = CreateDefaultCombats();

    [HideInInspector]
    [Tooltip("Enemy setup for Combat 1.")]
    public EncounterEntry combat1 = CreateEncounter("Combat 1", false);
    [HideInInspector]
    [Tooltip("Enemy setup for Combat 2.")]
    public EncounterEntry combat2 = CreateEncounter("Combat 2", false);
    [HideInInspector]
    [Tooltip("Enemy setup for Combat 3.")]
    public EncounterEntry combat3 = CreateEncounter("Combat 3", false);
    [HideInInspector]
    [Tooltip("Enemy setup for Combat 4 / Boss.")]
    public EncounterEntry combat4Boss = CreateEncounter("Combat 4 Boss", true);

    [HideInInspector]
    public EncounterEntry[] runEncounters = new EncounterEntry[DefaultPrototypeCombatCount];

    [Header("Dice Prototype Pool")]
    [Tooltip("Dice prefabs that can be randomly equipped for the prototype. Drag any DiceSpinnerGeneric prefab here.")]
    public DiceSpinnerGeneric[] dicePrototypePool = Array.Empty<DiceSpinnerGeneric>();

    [HideInInspector]
    public DiceSpinnerGeneric d4Prefab;
    [HideInInspector]
    public DiceSpinnerGeneric d8Prefab;

    [Header("Random Skill Groups")]
    [Tooltip("Author each entry as a 4-skill themed group, such as 4 Fire skills or 4 Ice skills. Each reset, prototype picks 1 group and fills all 4 owned skill slots from it.")]
    [FormerlySerializedAs("skillPairs")]
    public SkillPairEntry[] skillGroups = Array.Empty<SkillPairEntry>();

    [HideInInspector]
    public ConsumableDataSO[] consumables = Array.Empty<ConsumableDataSO>();

    [Header("Prototype Consumable Rewards")]
    [Tooltip("Drag a project folder here. In the editor, every ConsumableDataSO inside that folder becomes the reward pool.")]
    public UnityEngine.Object consumableRewardFolder;
    [Tooltip("Consumables that can appear in prototype reward choices. This is filled from Consumable Reward Folder in the editor, and is what runtime uses.")]
    public ConsumableDataSO[] consumableRewardPool = Array.Empty<ConsumableDataSO>();

    public EncounterEntry GetRunEncounter(int index)
    {
        if (combats == null || index < 0 || index >= combats.Length)
            return null;

        return combats[index];
    }

    public int GetCombatCount()
    {
        return combats != null ? combats.Length : 0;
    }

    public int GetFinalCombatIndex()
    {
        return Mathf.Max(0, GetCombatCount() - 1);
    }

    private void OnValidate()
    {
        EnsureEncounterDefaults();
        MigrateLegacyDicePrototypePool();
#if UNITY_EDITOR
        RefreshConsumableRewardPoolFromFolder();
#endif
    }

    private void MigrateLegacyDicePrototypePool()
    {
        if (dicePrototypePool != null && dicePrototypePool.Length > 0)
            return;

        int count = 0;
        if (d4Prefab != null)
            count++;
        if (d8Prefab != null && d8Prefab != d4Prefab)
            count++;
        if (count <= 0)
            return;

        DiceSpinnerGeneric[] migrated = new DiceSpinnerGeneric[count];
        int index = 0;
        if (d4Prefab != null)
            migrated[index++] = d4Prefab;
        if (d8Prefab != null && d8Prefab != d4Prefab)
            migrated[index] = d8Prefab;

        dicePrototypePool = migrated;
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

        if (combats == null || combats.Length == 0 || !HasAnyEncounter(combats))
            combats = CreateDefaultCombatsFromLegacy();

        for (int i = 0; i < combats.Length; i++)
        {
            string label = "Combat " + (i + 1);
            bool isFinal = i == combats.Length - 1;
            combats[i] = EnsureEncounter(combats[i], isFinal ? label + " Boss" : label, isFinal);
        }
    }

    private EncounterEntry[] CreateDefaultCombatsFromLegacy()
    {
        EncounterEntry[] result = CreateDefaultCombats();
        result[0] = combat1;
        result[1] = combat2;
        result[2] = combat3;
        result[3] = combat4Boss;
        return result;
    }

    private static EncounterEntry[] CreateDefaultCombats()
    {
        return new[]
        {
            CreateEncounter("Combat 1", false),
            CreateEncounter("Combat 2", false),
            CreateEncounter("Combat 3", false),
            CreateEncounter("Combat 4 Boss", true)
        };
    }

    private static bool HasAnyEncounter(EncounterEntry[] entries)
    {
        if (entries == null)
            return false;

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] != null && HasAnyEnemy(entries[i].enemies))
                return true;
        }

        return false;
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
            if (entry != null && entry.prefab != null)
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

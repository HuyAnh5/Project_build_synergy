using System;
using UnityEngine;

public enum MapRunAct
{
    Act1 = 1,
    Act2 = 2,
    Act3 = 3
}

public enum MapEncounterKind
{
    Combat,
    Elite,
    Boss,
    Event
}

public enum MapEncounterLayerPhase
{
    Early,
    Mid,
    Late
}

[CreateAssetMenu(
    fileName = "MapEncounter_",
    menuName = "Build Synergy/Map/Encounter Definition")]
public sealed class MapEncounterDefinitionSO : ScriptableObject
{
    [Serializable]
    public sealed class EnemySpawnEntry
    {
        public bool enabled = true;
        public CombatActor prefab;
        public CombatActor.RowTag row = CombatActor.RowTag.Front;
    }

    [Header("Identity")]
    [SerializeField] private string encounterId = "encounter_";
    [SerializeField] private string displayName = "Encounter";
    [SerializeField] private MapRunAct act = MapRunAct.Act1;
    [SerializeField] private MapEncounterKind kind = MapEncounterKind.Combat;

    [Header("Selection")]
    [SerializeField, Min(0)] private int weight = 10;
    [SerializeField, Min(0)] private int earlyWeight = 10;
    [SerializeField, Min(0)] private int midWeight = 10;
    [SerializeField, Min(0)] private int lateWeight = 10;
    [SerializeField, Min(0)] private int minLayer;
    [SerializeField, Min(0)] private int maxLayer;

    [Header("Combat")]
    [SerializeField] private EnemySpawnEntry[] enemies = CreateDefaultEnemies();

    [Header("Event")]
    [SerializeField, TextArea(2, 5)] private string eventSummary;

    public string EncounterId => encounterId;
    public string DisplayName => displayName;
    public MapRunAct Act => act;
    public MapEncounterKind Kind => kind;
    public int Weight => weight;
    public int EarlyWeight => earlyWeight;
    public int MidWeight => midWeight;
    public int LateWeight => lateWeight;
    public int MinLayer => minLayer;
    public int MaxLayer => maxLayer;
    public EnemySpawnEntry[] Enemies => enemies;
    public string EventSummary => eventSummary;

    public bool CanAppearIn(MapRunAct targetAct, MapEncounterKind targetKind, int layer)
    {
        if (act != targetAct || kind != targetKind || weight <= 0)
            return false;
        if (minLayer > 0 && layer < minLayer)
            return false;
        if (maxLayer > 0 && layer > maxLayer)
            return false;

        return true;
    }

    public int GetEffectiveWeight(MapEncounterLayerPhase phase)
    {
        int phaseWeight;
        switch (phase)
        {
            case MapEncounterLayerPhase.Early:
                phaseWeight = earlyWeight;
                break;
            case MapEncounterLayerPhase.Mid:
                phaseWeight = midWeight;
                break;
            case MapEncounterLayerPhase.Late:
                phaseWeight = lateWeight;
                break;
            default:
                phaseWeight = 0;
                break;
        }

        if (weight <= 0 || phaseWeight <= 0)
            return 0;

        return weight * phaseWeight;
    }

    public int GetValidEnemyCount()
    {
        if (enemies == null)
            return 0;

        int count = 0;
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemySpawnEntry entry = enemies[i];
            if (entry != null && entry.enabled && entry.prefab != null)
                count += 1;
        }

        return count;
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(encounterId))
            encounterId = name;
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = name;
        if (enemies == null)
            enemies = CreateDefaultEnemies();
    }

    private static EnemySpawnEntry[] CreateDefaultEnemies()
    {
        return new[]
        {
            new EnemySpawnEntry(),
            new EnemySpawnEntry(),
            new EnemySpawnEntry()
        };
    }
}

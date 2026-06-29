using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "MapEncounterDatabase",
    menuName = "Build Synergy/Map/Encounter Database")]
public sealed class MapEncounterDatabaseSO : ScriptableObject
{
    [SerializeField] private MapEncounterPoolSO[] pools = System.Array.Empty<MapEncounterPoolSO>();

    public IReadOnlyList<MapEncounterPoolSO> Pools => pools;

    public MapEncounterDefinitionSO PickRandom(MapRunAct act, MapEncounterKind kind, int layer, int maxLayer, MapEncounterPickHistory history)
    {
        MapEncounterPoolSO pool = FindPool(act, kind);
        if (pool == null)
            return null;

        return pool.PickRandom(layer, maxLayer, history);
    }

    public MapEncounterDefinitionSO PickRandom(MapRunAct act, MapEncounterKind kind, int layer)
    {
        return PickRandom(act, kind, layer, Mathf.Max(1, layer), null);
    }

    public List<MapEncounterDefinitionSO> GetCandidates(MapRunAct act, MapEncounterKind kind, int layer, int maxLayer, MapEncounterPickHistory history = null)
    {
        MapEncounterPoolSO pool = FindPool(act, kind);
        if (pool == null)
            return new List<MapEncounterDefinitionSO>();

        return pool.GetCandidates(layer, maxLayer, history);
    }

    public MapEncounterPoolSO FindPool(MapRunAct act, MapEncounterKind kind)
    {
        if (pools == null)
            return null;

        for (int i = 0; i < pools.Length; i++)
        {
            MapEncounterPoolSO pool = pools[i];
            if (pool != null && pool.Matches(act, kind))
                return pool;
        }

        return null;
    }

    public bool HasAnyFor(MapRunAct act, MapEncounterKind kind, int layer, int maxLayer)
    {
        MapEncounterPoolSO pool = FindPool(act, kind);
        if (pool == null)
            return false;

        return pool.GetCandidates(layer, maxLayer).Count > 0;
    }
}

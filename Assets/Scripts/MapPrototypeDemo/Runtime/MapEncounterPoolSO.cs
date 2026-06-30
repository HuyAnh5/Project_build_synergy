using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "MapEncounterPool_",
    menuName = "Build Synergy/Map/Encounter Pool")]
public sealed class MapEncounterPoolSO : ScriptableObject
{
    [SerializeField] private MapRunAct act = MapRunAct.Act1;
    [SerializeField] private MapEncounterKind kind = MapEncounterKind.Combat;
    [SerializeField] private MapEncounterDefinitionSO[] encounters = System.Array.Empty<MapEncounterDefinitionSO>();

    public MapRunAct Act => act;
    public MapEncounterKind Kind => kind;
    public IReadOnlyList<MapEncounterDefinitionSO> Encounters => encounters;

    public MapEncounterDefinitionSO PickRandom(int layer, int maxLayer, MapEncounterPickHistory history)
    {
        MapEncounterLayerPhase phase = GetLayerPhase(layer, maxLayer);
        List<MapEncounterDefinitionSO> candidates = GetCandidates(layer, phase, history, enforceHistory: true);
        if (candidates.Count == 0)
            candidates = GetCandidates(layer, phase, history, enforceHistory: false);
        if (candidates.Count == 0)
            return null;

        MapEncounterDefinitionSO picked = PickWeighted(candidates, phase);
        history?.Record(picked);
        return picked;
    }

    public List<MapEncounterDefinitionSO> GetCandidates(int layer, int maxLayer, MapEncounterPickHistory history = null)
    {
        return GetCandidates(layer, GetLayerPhase(layer, maxLayer), history, enforceHistory: true);
    }

    public bool Matches(MapRunAct targetAct, MapEncounterKind targetKind)
    {
        return act == targetAct && kind == targetKind;
    }

    public static MapEncounterLayerPhase GetLayerPhase(int layer, int maxLayer)
    {
        int safeMaxLayer = Mathf.Max(1, maxLayer);
        int earlyMax = Mathf.Max(1, Mathf.CeilToInt(safeMaxLayer * 0.375f));
        int midMax = Mathf.Max(earlyMax, Mathf.CeilToInt(safeMaxLayer * 0.75f));

        if (layer <= earlyMax)
            return MapEncounterLayerPhase.Early;
        if (layer <= midMax)
            return MapEncounterLayerPhase.Mid;

        return MapEncounterLayerPhase.Late;
    }

    private List<MapEncounterDefinitionSO> GetCandidates(
        int layer,
        MapEncounterLayerPhase phase,
        MapEncounterPickHistory history,
        bool enforceHistory)
    {
        List<MapEncounterDefinitionSO> result = new List<MapEncounterDefinitionSO>();
        if (encounters == null)
            return result;

        bool enforceCombatCooldown = enforceHistory && kind == MapEncounterKind.Combat;
        bool enforceOncePerAct = enforceHistory
            && (kind == MapEncounterKind.Event || kind == MapEncounterKind.Elite || kind == MapEncounterKind.Boss);

        for (int i = 0; i < encounters.Length; i++)
        {
            MapEncounterDefinitionSO encounter = encounters[i];
            if (encounter == null)
                continue;
            if (!encounter.CanAppearIn(act, kind, layer))
                continue;
            if (encounter.GetEffectiveWeight(phase) <= 0)
                continue;
            if (history != null && !history.CanPick(encounter, enforceCombatCooldown, enforceOncePerAct))
                continue;

            result.Add(encounter);
        }

        return result;
    }

    private static MapEncounterDefinitionSO PickWeighted(List<MapEncounterDefinitionSO> candidates, MapEncounterLayerPhase phase)
    {
        int totalWeight = 0;
        for (int i = 0; i < candidates.Count; i++)
            totalWeight += Mathf.Max(0, candidates[i].GetEffectiveWeight(phase));

        if (totalWeight <= 0)
            return candidates[Random.Range(0, candidates.Count)];

        int roll = Random.Range(0, totalWeight);
        for (int i = 0; i < candidates.Count; i++)
        {
            roll -= Mathf.Max(0, candidates[i].GetEffectiveWeight(phase));
            if (roll < 0)
                return candidates[i];
        }

        return candidates[candidates.Count - 1];
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class MapPrototypeController
{
    public event Action<MapPrototypeController> MapStateChanged;
    public event Action<MapPrototypeNodeData> HostileNodeOpened;
    public event Action<MapPrototypeNodeData, RunCombatResult> HostileNodeResolved;

    public MapPrototypeData CurrentMap => _map;
    public string CurrentNodeId => _currentNodeId;
    public int BossHintsCollected => _hintsCollected;
    public bool BossRevealed => _bossRevealed;
    public MapRunAct CurrentAct => currentAct;
    public MapEncounterDatabaseSO EncounterDatabase => encounterDatabase;

    public void WriteStateTo(RunState state)
    {
        if (state == null)
            return;

        state.SetMapSnapshot(_map, _currentNodeId, _hintsCollected, _bossRevealed);
    }

    [Obsolete("Use WriteStateTo instead.")]
    public void WriteProgressTo(RunState state)
    {
        WriteStateTo(state);
    }

    private void NotifyMapStateChanged()
    {
        MapStateChanged?.Invoke(this);
    }

    private void NotifyHostileNodeOpened(MapPrototypeNodeData node)
    {
        HostileNodeOpened?.Invoke(node);
    }

    private void NotifyHostileNodeResolved(MapPrototypeNodeData node, RunCombatResult result)
    {
        HostileNodeResolved?.Invoke(node, result);
    }

    private void AssignEncountersToGeneratedMap()
    {
        if (_map == null)
            return;

        foreach (MapPrototypeNodeData node in _map.nodes)
            node.encounterDefinition = null;

        if (encounterDatabase == null)
        {
            LogMap("No encounter database assigned; generated map will use prototype fallback encounters.");
            return;
        }

        MapEncounterPickHistory history = new MapEncounterPickHistory();
        int maxLayer = Mathf.Max(1, config != null ? config.intermediateRows : 1);
        List<MapPrototypeNodeData> nodes = _map.nodes
            .Where(node => node.type == MapPrototypeNodeType.Combat
                || node.type == MapPrototypeNodeType.Elite
                || node.type == MapPrototypeNodeType.Event
                || node.type == MapPrototypeNodeType.Boss)
            .OrderByDescending(node => node.row)
            .ToList();

        foreach (MapPrototypeNodeData node in nodes)
        {
            MapEncounterKind kind = GetEncounterKind(node.type);
            int layer = Mathf.Clamp(Mathf.RoundToInt(node.row), 1, maxLayer);
            MapEncounterDefinitionSO encounter = encounterDatabase.PickRandom(currentAct, kind, layer, maxLayer, history);
            node.encounterDefinition = encounter;

            if (encounter == null)
                LogMap($"No encounter found for {currentAct}/{kind} at layer {layer}. Node={node.id}");
        }
    }

    private static MapEncounterKind GetEncounterKind(MapPrototypeNodeType nodeType)
    {
        switch (nodeType)
        {
            case MapPrototypeNodeType.Elite:
                return MapEncounterKind.Elite;
            case MapPrototypeNodeType.Boss:
                return MapEncounterKind.Boss;
            case MapPrototypeNodeType.Event:
                return MapEncounterKind.Event;
            default:
                return MapEncounterKind.Combat;
        }
    }
}

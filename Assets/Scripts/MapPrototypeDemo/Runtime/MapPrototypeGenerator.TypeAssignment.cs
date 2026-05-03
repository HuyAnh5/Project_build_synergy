using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static partial class MapPrototypeGenerator
{
    private static bool AttachTypesAndHints(MapPrototypeConfig config, MapPrototypeData map)
    {
        List<MapPrototypeNodeData> nodes = map.nodes
            .Where(node => node.type != MapPrototypeNodeType.Start && node.type != MapPrototypeNodeType.Boss)
            .ToList();

        MapPrototypeNodeData shopNode = map.nodes.FirstOrDefault(node => node.type == MapPrototypeNodeType.Shop);
        MapPrototypeNodeData forgeNode = map.nodes.FirstOrDefault(node => node.type == MapPrototypeNodeType.Forge);
        List<MapPrototypeNodeData> coreNodes = nodes.Where(node => node != shopNode && node != forgeNode).ToList();

        foreach (MapPrototypeNodeData node in coreNodes)
        {
            node.type = MapPrototypeNodeType.Combat;
            node.hasHint = false;
            node.hintTaken = false;
        }
        if (shopNode != null)
        {
            shopNode.hasHint = true;
            shopNode.hintTaken = false;
        }
        if (forgeNode != null)
        {
            forgeNode.hasHint = false;
            forgeNode.hintTaken = false;
        }

        List<MapPrototypeNodeData> lockedCombatNodes = coreNodes.Where(node => node.lockedCombat).ToList();
        foreach (MapPrototypeNodeData node in lockedCombatNodes)
            node.type = MapPrototypeNodeType.Combat;

        List<MapPrototypeNodeData> restCandidates = coreNodes
            .Where(node => !node.lockedCombat && node.row >= 2f && node.row <= 7f)
            .ToList();
        int targetRestCount = GetTargetRestCount(config, coreNodes.Count);
        List<MapPrototypeNodeData> chosenRests = PickSpacedNodes(map, restCandidates, targetRestCount, config.minRestNodeGap, config.minRestRowGap);
        if (chosenRests.Count != targetRestCount)
            return false;
        foreach (MapPrototypeNodeData node in chosenRests)
            node.type = MapPrototypeNodeType.Rest;

        List<MapPrototypeNodeData> eliteCandidates = coreNodes
            .Where(node => !node.lockedCombat && node.row >= 4f && node.row <= 7f && node.type == MapPrototypeNodeType.Combat)
            .ToList();
        int targetEliteCount = GetTargetEliteCount(config, coreNodes.Count);
        List<MapPrototypeNodeData> chosenElites = PickSpacedNodes(map, eliteCandidates, targetEliteCount, config.eliteMinNodeGap, 0);
        if (chosenElites.Count != targetEliteCount)
            return false;
        foreach (MapPrototypeNodeData node in chosenElites)
            node.type = MapPrototypeNodeType.Elite;

        List<MapPrototypeNodeData> eventCandidates = coreNodes
            .Where(node => !node.lockedCombat && node.row >= 1f && node.row <= 7f && node.type == MapPrototypeNodeType.Combat)
            .ToList();
        int eventTargetBase = Mathf.RoundToInt(nodes.Count * config.eventRatioTarget);
        int desiredEventCount = Mathf.Max(4, Mathf.Min(eventCandidates.Count, eventTargetBase + RandInt(config.eventVarianceMin, config.eventVarianceMax)));

        List<MapPrototypeNodeData> firstRowCandidates = eventCandidates.Where(node => Mathf.Approximately(node.row, 1f)).ToList();
        List<MapPrototypeNodeData> nonFirstRowCandidates = eventCandidates.Where(node => !Mathf.Approximately(node.row, 1f)).ToList();
        List<MapPrototypeNodeData> chosenEvents = AssignEventNodes(config, map, firstRowCandidates, nonFirstRowCandidates, desiredEventCount);
        if (chosenEvents.Count < desiredEventCount)
            return false;
        if (!ValidateEventAdjacencyRules(config, map))
            return false;

        MapPrototypeNodeData bossNode = map.nodes.FirstOrDefault(node => node.type == MapPrototypeNodeType.Boss);
        if (bossNode != null)
            bossNode.bossData = map.boss;

        int latestHintRow = Mathf.Min(config.latestHintRow, Mathf.FloorToInt(config.intermediateRows * 0.75f));
        List<MapPrototypeNodeData> hintEligibleEvents = coreNodes
            .Where(node => node.type == MapPrototypeNodeType.Event && node.row > 1f && node.row <= latestHintRow)
            .ToList();
        List<MapPrototypeNodeData> chosenHints = PickSpacedNodes(map, hintEligibleEvents, config.extraHintSources, config.hintMinNodesBetween, 0);
        if (chosenHints.Count != config.extraHintSources)
            return false;
        foreach (MapPrototypeNodeData node in chosenHints)
            node.hasHint = true;

        return true;
    }

    private static bool ValidateRestSpacing(MapPrototypeConfig config, MapPrototypeData map)
    {
        int expectedRestCount = GetTargetRestCount(config, CountIntermediateNodes(map));
        List<MapPrototypeNodeData> rests = map.nodes.Where(node => node.type == MapPrototypeNodeType.Rest).ToList();
        for (int i = 0; i < rests.Count; i++)
        {
            for (int j = i + 1; j < rests.Count; j++)
            {
                MapPrototypeNodeData a = rests[i];
                MapPrototypeNodeData b = rests[j];
                if (Mathf.Abs(a.row - b.row) < config.minRestRowGap)
                    return false;
                if (PathNodesBetween(map, a.id, b.id) < config.minRestNodeGap)
                    return false;
            }
        }

        return rests.Count == expectedRestCount;
    }

    private static bool ValidateHintRules(MapPrototypeConfig config, MapPrototypeData map)
    {
        List<MapPrototypeNodeData> hintNodes = map.nodes.Where(node => node.hasHint).ToList();
        List<MapPrototypeNodeData> shopHintNodes = hintNodes.Where(node => node.type == MapPrototypeNodeType.Shop).ToList();
        List<MapPrototypeNodeData> eventHintNodes = hintNodes.Where(node => node.type == MapPrototypeNodeType.Event).ToList();
        if (shopHintNodes.Count != 1)
            return false;
        if (eventHintNodes.Count != config.extraHintSources)
            return false;
        if (eventHintNodes.Any(node => node.row <= 1f))
            return false;

        List<MapPrototypeNodeData> firstRowEvents = map.nodes.Where(node => Mathf.Approximately(node.row, 1f) && node.type == MapPrototypeNodeType.Event).ToList();
        if (firstRowEvents.Count > config.firstRowMaxEvents)
            return false;
        if (!ValidateEventAdjacencyRules(config, map))
            return false;

        for (int i = 0; i < eventHintNodes.Count; i++)
        {
            for (int j = i + 1; j < eventHintNodes.Count; j++)
            {
                if (PathNodesBetween(map, eventHintNodes[i].id, eventHintNodes[j].id) < config.hintMinNodesBetween)
                    return false;
            }
        }

        return true;
    }

    private static bool ValidateEliteSpacing(MapPrototypeConfig config, MapPrototypeData map)
    {
        int expectedEliteCount = GetTargetEliteCount(config, CountIntermediateNodes(map));
        List<MapPrototypeNodeData> elites = map.nodes.Where(node => node.type == MapPrototypeNodeType.Elite).ToList();
        for (int i = 0; i < elites.Count; i++)
        {
            for (int j = i + 1; j < elites.Count; j++)
            {
                if (PathNodesBetween(map, elites[i].id, elites[j].id) < config.eliteMinNodeGap)
                    return false;
            }
        }

        return elites.Count == expectedEliteCount;
    }

    private static bool ValidateEventAdjacencyRules(MapPrototypeConfig config, MapPrototypeData map)
    {
        List<MapPrototypeNodeData> eventNodes = map.nodes.Where(node => node.type == MapPrototypeNodeType.Event).ToList();
        if (eventNodes.Any(node => CountDirectEventNeighbors(map, node.id) > 1))
            return false;

        List<MapPrototypeNodeData> firstRowEvents = eventNodes.Where(node => Mathf.Approximately(node.row, 1f)).ToList();
        return firstRowEvents.Count <= config.firstRowMaxEvents;
    }

    private static int CountDirectEventNeighbors(MapPrototypeData map, string nodeId)
    {
        return GetDirectEventNeighbors(map, nodeId).Count;
    }

    private static List<MapPrototypeNodeData> GetDirectEventNeighbors(MapPrototypeData map, string nodeId)
    {
        if (!map.adjacency.TryGetValue(nodeId, out HashSet<string> neighbors))
            return new List<MapPrototypeNodeData>();

        return neighbors
            .Select(id => GetNodeById(map, id))
            .Where(node => node != null && node.type == MapPrototypeNodeType.Event)
            .ToList();
    }

    private static bool CanAssignEventNode(MapPrototypeConfig config, MapPrototypeData map, MapPrototypeNodeData node)
    {
        if (node == null || node.type != MapPrototypeNodeType.Combat)
            return false;
        if (Mathf.Approximately(node.row, 1f))
        {
            int currentFirstRowEvents = map.nodes.Count(candidate => Mathf.Approximately(candidate.row, 1f) && candidate.type == MapPrototypeNodeType.Event);
            if (currentFirstRowEvents >= config.firstRowMaxEvents)
                return false;
        }

        List<MapPrototypeNodeData> directEventNeighbors = GetDirectEventNeighbors(map, node.id);
        if (directEventNeighbors.Count > 1)
            return false;
        if (directEventNeighbors.Any(neighbor => CountDirectEventNeighbors(map, neighbor.id) >= 1))
            return false;

        return true;
    }

    private static List<MapPrototypeNodeData> AssignEventNodes(
        MapPrototypeConfig config,
        MapPrototypeData map,
        List<MapPrototypeNodeData> firstRowCandidates,
        List<MapPrototypeNodeData> nonFirstRowCandidates,
        int desiredEventCount)
    {
        List<MapPrototypeNodeData> allCandidates = new List<MapPrototypeNodeData>(firstRowCandidates.Concat(nonFirstRowCandidates));
        int maxFirstRowEvents = Mathf.Min(config.firstRowMaxEvents, firstRowCandidates.Count, desiredEventCount);
        List<string> bestIds = new List<string>();

        for (int attempt = 0; attempt < 96; attempt++)
        {
            foreach (MapPrototypeNodeData node in allCandidates)
            {
                if (node.type == MapPrototypeNodeType.Event)
                    node.type = MapPrototypeNodeType.Combat;
            }

            List<MapPrototypeNodeData> picked = new List<MapPrototypeNodeData>();
            int targetFirstRowEvents = maxFirstRowEvents > 0 ? RandInt(0, maxFirstRowEvents) : 0;
            List<MapPrototypeNodeData> firstRowPool = Shuffle(new List<MapPrototypeNodeData>(firstRowCandidates));
            List<MapPrototypeNodeData> nonFirstRowPool = Shuffle(new List<MapPrototypeNodeData>(nonFirstRowCandidates));

            foreach (MapPrototypeNodeData node in firstRowPool)
            {
                if (picked.Count(candidate => Mathf.Approximately(candidate.row, 1f)) >= targetFirstRowEvents)
                    break;
                if (!CanAssignEventNode(config, map, node))
                    continue;

                node.type = MapPrototypeNodeType.Event;
                picked.Add(node);
            }

            foreach (MapPrototypeNodeData node in nonFirstRowPool)
            {
                if (picked.Count >= desiredEventCount)
                    break;
                if (!CanAssignEventNode(config, map, node))
                    continue;

                node.type = MapPrototypeNodeType.Event;
                picked.Add(node);
            }

            if (picked.Count > bestIds.Count)
                bestIds = picked.Select(node => node.id).ToList();

            if (picked.Count >= desiredEventCount)
                return picked;
        }

        foreach (MapPrototypeNodeData node in allCandidates)
        {
            if (node.type == MapPrototypeNodeType.Event)
                node.type = MapPrototypeNodeType.Combat;
        }

        Dictionary<string, MapPrototypeNodeData> byId = allCandidates.ToDictionary(node => node.id, node => node);
        List<MapPrototypeNodeData> bestNodes = bestIds
            .Where(byId.ContainsKey)
            .Select(id => byId[id])
            .ToList();
        foreach (MapPrototypeNodeData node in bestNodes)
            node.type = MapPrototypeNodeType.Event;

        return bestNodes;
    }

    private static List<MapPrototypeNodeData> PickEliteNodes(MapPrototypeData map, List<MapPrototypeNodeData> candidates, int count)
    {
        List<MapPrototypeNodeData> picked = new List<MapPrototypeNodeData>();
        foreach (MapPrototypeNodeData candidate in Shuffle(new List<MapPrototypeNodeData>(candidates)))
        {
            bool valid = picked.All(chosen =>
                !(map.adjacency.TryGetValue(candidate.id, out HashSet<string> neighbors) && neighbors.Contains(chosen.id))
                && ShortestNodeDistance(map, candidate.id, chosen.id) >= 2);
            if (!valid)
                continue;
            picked.Add(candidate);
            if (picked.Count >= count)
                break;
        }
        return picked;
    }

    private static List<MapPrototypeNodeData> PickEventNodes(MapPrototypeData map, List<MapPrototypeNodeData> candidates, int count)
    {
        List<MapPrototypeNodeData> picked = new List<MapPrototypeNodeData>();
        foreach (MapPrototypeNodeData candidate in candidates)
        {
            int adjacentPicked = map.adjacency.TryGetValue(candidate.id, out HashSet<string> neighbors)
                ? neighbors.Count(id => picked.Any(p => p.id == id))
                : 0;
            if (adjacentPicked > 1)
                continue;

            bool createsTriple = picked.Any(pickedNode =>
                map.adjacency.TryGetValue(candidate.id, out HashSet<string> candidateNeighbors)
                && candidateNeighbors.Contains(pickedNode.id)
                && map.adjacency.TryGetValue(pickedNode.id, out HashSet<string> pickedNeighbors)
                && pickedNeighbors.Any(id => picked.Any(other => other.id == id && other.id != candidate.id)));
            if (createsTriple)
                continue;

            picked.Add(candidate);
            if (picked.Count >= count)
                break;
        }
        return picked;
    }

    private static List<MapPrototypeNodeData> PickHintEventNodes(MapPrototypeData map, List<MapPrototypeNodeData> candidates, int count)
    {
        List<MapPrototypeNodeData> picked = new List<MapPrototypeNodeData>();
        foreach (MapPrototypeNodeData candidate in Shuffle(new List<MapPrototypeNodeData>(candidates)))
        {
            if (picked.Any(chosen => ShortestNodeDistance(map, candidate.id, chosen.id) < 2))
                continue;
            picked.Add(candidate);
            if (picked.Count >= count)
                break;
        }
        return picked;
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static partial class MapPrototypeGenerator
{
    private static bool ValidateMap(MapPrototypeConfig config, MapPrototypeData map)
    {
        HashSet<float> rowsCovered = new HashSet<float>(map.nodes
            .Where(node => node.type != MapPrototypeNodeType.Start && node.type != MapPrototypeNodeType.Boss)
            .Select(node => node.row));

        if (rowsCovered.Count != config.intermediateRows)
            return false;

        int intermediateCount = map.nodes.Count(node => node.type != MapPrototypeNodeType.Start && node.type != MapPrototypeNodeType.Boss);
        if (intermediateCount < 20 || intermediateCount > 30)
            return false;

        return ValidateNodeDegrees(config, map);
    }

    private sealed class RouteValidationResult
    {
        public bool ok;
        public RhythmEvaluationResult rhythm;
    }

    private sealed class RhythmEvaluationResult
    {
        public bool ok;
        public float score;
        public int maxStreak;
        public int exactFourCount;
        public int fivePlusCount;
        public int maxHostileStreak;
        public int fiveHostileCount;
        public float penalty;
    }

    private static RouteValidationResult ValidateRoutes(MapPrototypeConfig config, MapPrototypeData map)
    {
        MapPrototypeNodeData start = map.nodes.FirstOrDefault(node => node.type == MapPrototypeNodeType.Start);
        MapPrototypeNodeData boss = map.nodes.FirstOrDefault(node => node.type == MapPrototypeNodeType.Boss);
        Dictionary<string, List<string>> forward = BuildForwardAdjacency(map);
        List<List<string>> allPaths = new List<List<string>>();
        DfsPaths(start.id, boss.id, forward, new List<string>(), allPaths, map.nodes.Count + 5);
        if (allPaths.Count == 0)
        {
            return new RouteValidationResult
            {
                ok = false,
                rhythm = new RhythmEvaluationResult { ok = false, score = float.PositiveInfinity }
            };
        }

        Dictionary<string, MapPrototypeNodeData> nodeById = map.nodes.ToDictionary(node => node.id, node => node);
        bool hasNoEliteRoute = allPaths.Any(path => path.All(id => nodeById[id].type != MapPrototypeNodeType.Elite));
        bool hasEliteRoute = allPaths.Any(path => path.Any(id => nodeById[id].type == MapPrototypeNodeType.Elite));
        bool hasShop = map.nodes.Any(node => node.type == MapPrototypeNodeType.Shop);
        bool hasForge = map.nodes.Any(node => node.type == MapPrototypeNodeType.Forge);
        RhythmEvaluationResult rhythm = EvaluateDecisionRhythm(config, map, allPaths, forward);

        return new RouteValidationResult
        {
            ok = hasNoEliteRoute && hasEliteRoute && hasShop && hasForge && rhythm.ok,
            rhythm = rhythm
        };
    }

    private static RhythmEvaluationResult EvaluateDecisionRhythm(MapPrototypeConfig config, MapPrototypeData map, List<List<string>> allPaths, Dictionary<string, List<string>> forward)
    {
        Dictionary<string, MapPrototypeNodeData> nodeById = map.nodes.ToDictionary(node => node.id, node => node);
        Dictionary<string, int> indegree = map.nodes.ToDictionary(node => node.id, node => 0);

        foreach (KeyValuePair<string, List<string>> pair in forward)
        {
            foreach (string toId in pair.Value)
                indegree[toId] = indegree[toId] + 1;
        }

        int maxStreak = 0;
        int exactFourCount = 0;
        int fivePlusCount = 0;
        int maxHostileStreak = 0;
        int fiveHostileCount = 0;
        float penalty = 0f;

        foreach (List<string> path in allPaths)
        {
            int streak = 0;
            int hostileStreak = 0;

            void FlushLinear()
            {
                if (streak <= 0)
                    return;

                maxStreak = Mathf.Max(maxStreak, streak);
                if (streak == 4) exactFourCount += 1;
                if (streak == 3) penalty += 0.5f;
                if (streak == 4) penalty += 1.5f;
                if (streak == 5) penalty += 4f;
                if (streak > 5)
                {
                    fivePlusCount += 1;
                    penalty += 18f + (streak - 6) * 10f;
                }

                streak = 0;
            }

            void FlushHostile()
            {
                if (hostileStreak <= 0)
                    return;

                maxHostileStreak = Mathf.Max(maxHostileStreak, hostileStreak);
                if (hostileStreak == 4) penalty += 1.5f;
                if (hostileStreak == 5)
                {
                    fiveHostileCount += 1;
                    penalty += 12f;
                }
                if (hostileStreak > config.maxHostileStreak)
                    penalty += 40f + (hostileStreak - config.maxHostileStreak) * 20f;

                hostileStreak = 0;
            }

            for (int i = 1; i < path.Count - 1; i++)
            {
                MapPrototypeNodeData node = nodeById[path[i]];
                if (node.specialLeaf)
                {
                    FlushLinear();
                    FlushHostile();
                    continue;
                }

                int outCount = (forward.TryGetValue(node.id, out List<string> nextIds) ? nextIds : new List<string>())
                    .Count(nextId => !nodeById[nextId].specialLeaf);
                int inDeg = indegree[node.id];
                bool isLinear = inDeg == 1 && outCount == 1;

                if (isLinear)
                    streak += 1;
                else
                    FlushLinear();

                if (node.type == MapPrototypeNodeType.Combat || node.type == MapPrototypeNodeType.Elite)
                    hostileStreak += 1;
                else
                    FlushHostile();
            }

            FlushLinear();
            FlushHostile();
        }

        bool ok = maxStreak <= config.maxForcedLinearStreak && maxHostileStreak <= config.maxHostileStreak;

        return new RhythmEvaluationResult
        {
            ok = ok,
            score = penalty
                + Mathf.Max(0, maxStreak - 3) * 3f
                + exactFourCount * 1.5f
                + fivePlusCount * 10f
                + Mathf.Max(0, maxHostileStreak - 3) * 2f
                + fiveHostileCount * 4f,
            maxStreak = maxStreak,
            exactFourCount = exactFourCount,
            fivePlusCount = fivePlusCount,
            maxHostileStreak = maxHostileStreak,
            fiveHostileCount = fiveHostileCount,
            penalty = penalty
        };
    }

    private static bool ValidateLeafSpacing(MapPrototypeConfig config, MapPrototypeData map)
    {
        List<MapPrototypeNodeData> leaves = map.nodes
            .Where(node => node.specialLeaf && (node.type == MapPrototypeNodeType.Shop || node.type == MapPrototypeNodeType.Forge))
            .ToList();

        return leaves.All(node =>
        {
            MapPrototypeEdgeData edge = map.edges.FirstOrDefault(candidate => candidate.from == node.id || candidate.to == node.id);
            if (edge == null)
                return false;

            MapPrototypeNodeData parent = GetNodeById(map, edge.from == node.id ? edge.to : edge.from);
            if (parent == null)
                return false;

            float dist = Distance(node.x, node.y, parent.x, parent.y);
            return dist >= config.leafMinDistance && dist <= config.leafMaxDistance;
        });
    }

    private static float LeafSpacingScore(MapPrototypeConfig config, MapPrototypeData map)
    {
        List<MapPrototypeNodeData> leaves = map.nodes
            .Where(node => node.specialLeaf && (node.type == MapPrototypeNodeType.Shop || node.type == MapPrototypeNodeType.Forge))
            .ToList();

        if (leaves.Count == 0)
            return 999f;

        float score = 0f;
        foreach (MapPrototypeNodeData node in leaves)
        {
            MapPrototypeEdgeData edge = map.edges.FirstOrDefault(candidate => candidate.from == node.id || candidate.to == node.id);
            if (edge == null)
            {
                score += 999f;
                continue;
            }

            MapPrototypeNodeData parent = GetNodeById(map, edge.from == node.id ? edge.to : edge.from);
            if (parent == null)
            {
                score += 999f;
                continue;
            }

            float dist = Distance(node.x, node.y, parent.x, parent.y);
            score += Mathf.Abs(dist - config.leafIdealDistance);
        }

        return score;
    }

    private static Dictionary<string, List<string>> BuildForwardAdjacency(MapPrototypeData map)
    {
        Dictionary<string, List<string>> forward = map.nodes.ToDictionary(node => node.id, node => new List<string>());
        Dictionary<string, MapPrototypeNodeData> byId = map.nodes.ToDictionary(node => node.id, node => node);

        foreach (MapPrototypeEdgeData edge in map.edges)
        {
            MapPrototypeNodeData a = byId[edge.from];
            MapPrototypeNodeData b = byId[edge.to];
            if (a.row < b.row)
                forward[a.id].Add(b.id);
            else if (b.row < a.row)
                forward[b.id].Add(a.id);
        }

        return forward;
    }

    private static void DfsPaths(string current, string goal, Dictionary<string, List<string>> forward, List<string> path, List<List<string>> output, int cap)
    {
        if (path.Count > cap)
            return;

        List<string> nextPath = new List<string>(path) { current };
        if (current == goal)
        {
            output.Add(nextPath);
            return;
        }

        if (!forward.TryGetValue(current, out List<string> next))
            return;

        foreach (string id in next)
            DfsPaths(id, goal, forward, nextPath, output, cap);
    }

    private static int ShortestNodeDistance(MapPrototypeData map, string startId, string goalId)
    {
        if (startId == goalId)
            return 0;

        Queue<(string id, int dist)> queue = new Queue<(string id, int dist)>();
        HashSet<string> visited = new HashSet<string> { startId };
        queue.Enqueue((startId, 0));

        while (queue.Count > 0)
        {
            (string current, int dist) = queue.Dequeue();
            if (!map.adjacency.TryGetValue(current, out HashSet<string> nextIds))
                continue;

            foreach (string nextId in nextIds)
            {
                if (nextId == goalId)
                    return dist + 1;
                if (!visited.Add(nextId))
                    continue;

                queue.Enqueue((nextId, dist + 1));
            }
        }

        return int.MaxValue;
    }


    private static int CountIntermediateNodes(MapPrototypeData map)
    {
        return map.nodes.Count(node => node.type != MapPrototypeNodeType.Start && node.type != MapPrototypeNodeType.Boss && !node.specialLeaf);
    }

    private static int GetTargetRestCount(MapPrototypeConfig config, int intermediateNodeCount)
    {
        return intermediateNodeCount >= config.restThreeThresholdIntermediateNodes ? 3 : 2;
    }

    private static int GetTargetEliteCount(MapPrototypeConfig config, int intermediateNodeCount)
    {
        return intermediateNodeCount >= config.eliteThreeThresholdIntermediateNodes ? 3 : 2;
    }

    private static int PathNodesBetween(MapPrototypeData map, string startId, string goalId)
    {
        int dist = ShortestNodeDistance(map, startId, goalId);
        if (dist == int.MaxValue)
            return int.MaxValue;
        return Mathf.Max(0, dist - 1);
    }

    private static List<string> GetBossParentIds(MapPrototypeData map)
    {
        MapPrototypeNodeData boss = map.nodes.FirstOrDefault(node => node.type == MapPrototypeNodeType.Boss);
        if (boss == null || !map.adjacency.TryGetValue(boss.id, out HashSet<string> neighbors))
            return new List<string>();

        return neighbors
            .Select(id => GetNodeById(map, id))
            .Where(node => node != null && !node.specialLeaf)
            .Select(node => node.id)
            .ToList();
    }

    private static float LastSharedRow(List<string> pathA, List<string> pathB, Dictionary<string, MapPrototypeNodeData> nodeById)
    {
        int length = Mathf.Min(pathA.Count, pathB.Count);
        float sharedRow = -1f;
        for (int i = 0; i < length; i++)
        {
            if (pathA[i] != pathB[i])
                break;

            if (nodeById.TryGetValue(pathA[i], out MapPrototypeNodeData node) && node != null)
                sharedRow = node.row;
        }

        return sharedRow;
    }

    private static bool ValidateBossEntranceRoutes(MapPrototypeConfig config, MapPrototypeData map)
    {
        MapPrototypeNodeData start = map.nodes.FirstOrDefault(node => node.type == MapPrototypeNodeType.Start);
        if (start == null)
            return false;

        Dictionary<string, List<string>> forward = BuildForwardAdjacency(map);
        Dictionary<string, MapPrototypeNodeData> nodeById = map.nodes.ToDictionary(node => node.id, node => node);
        List<string> bossParentIds = GetBossParentIds(map);
        if (bossParentIds.Count < 2)
            return false;

        for (int i = 0; i < bossParentIds.Count; i++)
        {
            for (int j = i + 1; j < bossParentIds.Count; j++)
            {
                List<List<string>> pathsA = new List<List<string>>();
                List<List<string>> pathsB = new List<List<string>>();
                DfsPaths(start.id, bossParentIds[i], forward, new List<string>(), pathsA, map.nodes.Count + 5);
                DfsPaths(start.id, bossParentIds[j], forward, new List<string>(), pathsB, map.nodes.Count + 5);

                foreach (List<string> pathA in pathsA)
                {
                    foreach (List<string> pathB in pathsB)
                    {
                        if (LastSharedRow(pathA, pathB, nodeById) <= config.bossRouteMaxSharedRow)
                            return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool ValidateNodeDegrees(MapPrototypeConfig config, MapPrototypeData map)
    {
        return map.nodes.All(node => GetNodeDegree(map, node.id) <= config.maxNodeDegree);
    }

    private static bool HasBadEdgeNodeOverlap(MapPrototypeData map)
    {
        foreach (MapPrototypeEdgeData edge in map.edges)
        {
            MapPrototypeNodeData from = GetNodeById(map, edge.from);
            MapPrototypeNodeData to = GetNodeById(map, edge.to);
            if (from == null || to == null)
                continue;

            foreach (MapPrototypeNodeData node in map.nodes)
            {
                if (node.id == from.id || node.id == to.id)
                    continue;

                float clearance = node.specialLeaf ? 66f : 56f;
                if (PointToSegmentDistance(node.x, node.y, from.x, from.y, to.x, to.y) < clearance)
                    return true;
            }
        }

        return false;
    }

    private static int GetNodeDegree(MapPrototypeData map, string id)
    {
        return map.adjacency.TryGetValue(id, out HashSet<string> neighbors) ? neighbors.Count : 0;
    }

}

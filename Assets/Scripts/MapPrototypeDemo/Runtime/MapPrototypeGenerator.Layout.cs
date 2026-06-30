using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static partial class MapPrototypeGenerator
{
    private static void RebalanceMapLayout(MapPrototypeConfig config, MapPrototypeData map)
    {
        Dictionary<string, Vector2> bestPositions = null;
        float bestScore = float.PositiveInfinity;
        const int candidateCount = 24;

        for (int i = 0; i < candidateCount; i++)
        {
            AssignLayeredOrganicPositions(config, map);
            RelaxLayout(config, map);
            float score = ScoreLayout(config, map);
            if (score < bestScore)
            {
                bestScore = score;
                bestPositions = CapturePositions(map);
            }
        }

        if (bestPositions != null)
            RestorePositions(map, bestPositions);

        foreach (MapPrototypeNodeData node in map.nodes.Where(n => n.specialLeaf))
        {
            MapPrototypeEdgeData parentEdge = map.edges.FirstOrDefault(edge => edge.to == node.id || edge.from == node.id);
            if (parentEdge == null)
                continue;

            MapPrototypeNodeData parent = GetNodeById(map, parentEdge.from == node.id ? parentEdge.to : parentEdge.from);
            if (parent == null)
                continue;

            MapPrototypeData tempMap = new MapPrototypeData();
            foreach (MapPrototypeNodeData candidate in map.nodes.Where(n => n.id != node.id))
                tempMap.nodes.Add(candidate);
            foreach (MapPrototypeEdgeData edge in map.edges.Where(e => e.from != node.id && e.to != node.id))
                tempMap.edges.Add(edge);
            foreach (KeyValuePair<string, HashSet<string>> pair in map.adjacency)
            {
                if (pair.Key == node.id)
                    continue;

                tempMap.adjacency[pair.Key] = new HashSet<string>(pair.Value.Where(id => id != node.id));
            }

            LeafPlacement placement = FindLeafPlacement(config, tempMap, parent, node.type, node.id);
            if (placement == null)
                continue;

            node.row = parent.row + 1f;
            node.x = placement.x;
            node.y = placement.y;
            node.side = placement.side;
        }
    }

    private static void AssignLayeredOrganicPositions(MapPrototypeConfig config, MapPrototypeData map)
    {
        float left = config.padX;
        float right = config.mapWidth - config.padX;
        float usableWidth = Mathf.Max(1f, right - left);
        float centerX = config.mapWidth * 0.5f;
        float defaultLayerGap = (config.mapHeight - config.padY * 2f) / (config.intermediateRows + 1f);
        float layerGap = Mathf.Clamp(defaultLayerGap, config.layerHeightMin, config.layerHeightMax);

        MapPrototypeNodeData start = map.nodes.FirstOrDefault(node => node.type == MapPrototypeNodeType.Start);
        MapPrototypeNodeData boss = map.nodes.FirstOrDefault(node => node.type == MapPrototypeNodeType.Boss);
        if (start != null)
        {
            start.x = centerX;
            start.y = config.mapHeight - config.padY + 8f;
        }
        if (boss != null)
        {
            boss.x = centerX;
            boss.y = config.padY - 12f;
        }

        Dictionary<float, List<MapPrototypeNodeData>> byLayer = map.nodes
            .Where(node => node.type != MapPrototypeNodeType.Start && node.type != MapPrototypeNodeType.Boss && !node.specialLeaf)
            .GroupBy(node => node.row)
            .ToDictionary(group => group.Key, group => group.OrderBy(node => node.col).ToList());

        foreach (KeyValuePair<float, List<MapPrototypeNodeData>> pair in byLayer.OrderBy(pair => pair.Key))
        {
            List<MapPrototypeNodeData> layerNodes = pair.Value;
            int count = layerNodes.Count;
            float densityWidth = Mathf.Min(usableWidth, config.targetLayerWidth * UnityEngine.Random.Range(0.82f, 1.16f));
            if (count > 1)
            {
                float minWidth = config.minNodeSpacing * (count - 1);
                float maxWidth = config.maxNodeSpacingInLayer * (count - 1);
                densityWidth = Mathf.Clamp(densityWidth, minWidth, Mathf.Min(maxWidth, usableWidth));
            }
            else
            {
                densityWidth = 0f;
            }

            float layerCenter = centerX + Mathf.Sin(pair.Key * 1.37f + UnityEngine.Random.value * 2.2f) * usableWidth * 0.10f;
            layerCenter += UnityEngine.Random.Range(-usableWidth * 0.08f, usableWidth * 0.08f);
            layerCenter = Mathf.Clamp(layerCenter, left + densityWidth * 0.5f, right - densityWidth * 0.5f);

            for (int i = 0; i < count; i++)
            {
                MapPrototypeNodeData node = layerNodes[i];
                float t = count <= 1 ? 0.5f : i / (float)(count - 1);
                float laneBend = Mathf.Sin((pair.Key + i) * 0.91f) * config.jitterX * 0.45f;
                node.x = layerCenter - densityWidth * 0.5f + densityWidth * t + laneBend + UnityEngine.Random.Range(-config.jitterX, config.jitterX);
                node.x = Mathf.Clamp(node.x, left, right);
                node.y = config.mapHeight - config.padY - pair.Key * layerGap + UnityEngine.Random.Range(-config.jitterY, config.jitterY);
            }
        }
    }

    private static void RelaxLayout(MapPrototypeConfig config, MapPrototypeData map)
    {
        List<MapPrototypeNodeData> nodes = map.nodes
            .Where(node => node.type != MapPrototypeNodeType.Start && node.type != MapPrototypeNodeType.Boss && !node.specialLeaf)
            .ToList();
        float left = config.padX;
        float right = config.mapWidth - config.padX;

        for (int pass = 0; pass < 5; pass++)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    MapPrototypeNodeData a = nodes[i];
                    MapPrototypeNodeData b = nodes[j];
                    float dx = b.x - a.x;
                    float dy = b.y - a.y;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist >= config.minNodeSpacing || dist <= 0.001f)
                        continue;

                    float push = (config.minNodeSpacing - dist) * 0.32f;
                    float nx = dx / dist;
                    float ny = dy / dist;
                    a.x = Mathf.Clamp(a.x - nx * push, left, right);
                    b.x = Mathf.Clamp(b.x + nx * push, left, right);
                    a.y -= ny * push * 0.18f;
                    b.y += ny * push * 0.18f;
                }
            }

            PullConnectedNodesIntoOrganicBands(config, map);
            CompressSparseLayers(config, map);
        }
    }

    private static void PullConnectedNodesIntoOrganicBands(MapPrototypeConfig config, MapPrototypeData map)
    {
        Dictionary<string, MapPrototypeNodeData> byId = map.nodes.ToDictionary(node => node.id, node => node);
        foreach (MapPrototypeEdgeData edge in map.edges)
        {
            if (!byId.TryGetValue(edge.from, out MapPrototypeNodeData from) || !byId.TryGetValue(edge.to, out MapPrototypeNodeData to))
                continue;
            if (from.specialLeaf || to.specialLeaf)
                continue;
            if (Mathf.Abs(from.row - to.row) > 1.01f)
                continue;

            float dx = to.x - from.x;
            float maxRun = config.maxNodeSpacingInLayer * 0.72f;
            if (Mathf.Abs(dx) <= maxRun)
                continue;

            float pull = (Mathf.Abs(dx) - maxRun) * 0.08f;
            float sign = Mathf.Sign(dx);
            if (from.type != MapPrototypeNodeType.Start && from.type != MapPrototypeNodeType.Boss)
                from.x += sign * pull;
            if (to.type != MapPrototypeNodeType.Start && to.type != MapPrototypeNodeType.Boss)
                to.x -= sign * pull;
        }
    }

    private static void CompressSparseLayers(MapPrototypeConfig config, MapPrototypeData map)
    {
        float centerX = config.mapWidth * 0.5f;
        float left = config.padX;
        float right = config.mapWidth - config.padX;
        IEnumerable<IGrouping<float, MapPrototypeNodeData>> layers = map.nodes
            .Where(node => node.type != MapPrototypeNodeType.Start && node.type != MapPrototypeNodeType.Boss && !node.specialLeaf)
            .GroupBy(node => node.row);

        foreach (IGrouping<float, MapPrototypeNodeData> layer in layers)
        {
            List<MapPrototypeNodeData> rowNodes = layer.OrderBy(node => node.x).ToList();
            if (rowNodes.Count < 2)
                continue;

            float span = rowNodes[rowNodes.Count - 1].x - rowNodes[0].x;
            float desired = Mathf.Max(config.minNodeSpacing * (rowNodes.Count - 1), Mathf.Min(config.targetLayerWidth, config.maxNodeSpacingInLayer * (rowNodes.Count - 1)));
            if (span <= desired)
                continue;

            float mean = rowNodes.Sum(node => node.x) / rowNodes.Count;
            float factor = Mathf.Lerp(1f, desired / Mathf.Max(1f, span), 0.18f);
            foreach (MapPrototypeNodeData node in rowNodes)
                node.x = Mathf.Clamp(mean + (node.x - mean) * factor + (centerX - mean) * 0.02f, left, right);
        }
    }

    private static float ScoreLayout(MapPrototypeConfig config, MapPrototypeData map)
    {
        float score = 0f;
        List<MapPrototypeNodeData> nodes = map.nodes.Where(node => !node.specialLeaf).ToList();
        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = i + 1; j < nodes.Count; j++)
            {
                float dist = Distance(nodes[i].x, nodes[i].y, nodes[j].x, nodes[j].y);
                if (dist < config.minNodeSpacing)
                    score += (config.minNodeSpacing - dist) * config.nodeOverlapPenalty;
            }
        }

        foreach (IGrouping<float, MapPrototypeNodeData> layer in nodes
            .Where(node => node.type != MapPrototypeNodeType.Start && node.type != MapPrototypeNodeType.Boss)
            .GroupBy(node => node.row))
        {
            List<MapPrototypeNodeData> rowNodes = layer.OrderBy(node => node.x).ToList();
            for (int i = 1; i < rowNodes.Count; i++)
            {
                float gap = rowNodes[i].x - rowNodes[i - 1].x;
                if (gap > config.maxNodeSpacingInLayer)
                    score += (gap - config.maxNodeSpacingInLayer) * config.tooSparsePenalty;
            }

            if (rowNodes.Count > 1)
            {
                float span = rowNodes[rowNodes.Count - 1].x - rowNodes[0].x;
                float desired = Mathf.Clamp(config.targetLayerWidth, config.minNodeSpacing * (rowNodes.Count - 1), config.maxNodeSpacingInLayer * (rowNodes.Count - 1));
                score += Mathf.Abs(span - desired) * config.tooSparsePenalty * 0.55f;
            }
        }

        Dictionary<string, MapPrototypeNodeData> byId = map.nodes.ToDictionary(node => node.id, node => node);
        for (int i = 0; i < map.edges.Count; i++)
        {
            if (!byId.TryGetValue(map.edges[i].from, out MapPrototypeNodeData a) || !byId.TryGetValue(map.edges[i].to, out MapPrototypeNodeData b))
                continue;

            float length = Distance(a.x, a.y, b.x, b.y);
            score += Mathf.Max(0f, length - config.maxNodeSpacingInLayer * 1.15f) * config.edgeLengthPenalty;
            float run = Mathf.Abs(a.x - b.x);
            if (run < config.minNodeSpacing * 0.28f && Mathf.Abs(a.row - b.row) <= 1.01f)
                score += (config.minNodeSpacing * 0.28f - run) * 0.25f;

            for (int j = i + 1; j < map.edges.Count; j++)
            {
                if (SharesEndpoint(map.edges[i], map.edges[j]))
                    continue;
                if (!byId.TryGetValue(map.edges[j].from, out MapPrototypeNodeData c) || !byId.TryGetValue(map.edges[j].to, out MapPrototypeNodeData d))
                    continue;
                if (SegmentsIntersect(a.x, a.y, b.x, b.y, c.x, c.y, d.x, d.y))
                    score += config.edgeCrossingPenalty;
            }
        }

        return score;
    }

    private static bool SharesEndpoint(MapPrototypeEdgeData a, MapPrototypeEdgeData b)
    {
        return a.from == b.from || a.from == b.to || a.to == b.from || a.to == b.to;
    }

    private static Dictionary<string, Vector2> CapturePositions(MapPrototypeData map)
    {
        return map.nodes.ToDictionary(node => node.id, node => new Vector2(node.x, node.y));
    }

    private static void RestorePositions(MapPrototypeData map, Dictionary<string, Vector2> positions)
    {
        foreach (MapPrototypeNodeData node in map.nodes)
        {
            if (!positions.TryGetValue(node.id, out Vector2 pos))
                continue;

            node.x = pos.x;
            node.y = pos.y;
        }
    }

    private static bool InsertSpecialSideBranches(MapPrototypeConfig config, MapPrototypeData map)
    {
        List<MapPrototypeNodeData> mainNodes = map.nodes
            .Where(node => node.type != MapPrototypeNodeType.Start && node.type != MapPrototypeNodeType.Boss)
            .ToList();

        List<MapPrototypeNodeData> earlyParents = Shuffle(mainNodes
            .Where(node => (Mathf.Approximately(node.row, 1f) || Mathf.Approximately(node.row, 2f))
                && GetNodeDegree(map, node.id) < config.maxNodeDegree)
            .ToList());

        MapPrototypeNodeData shopParent = null;
        bool shopPlaced = false;
        bool forgePlaced = false;

        foreach (MapPrototypeNodeData candidate in earlyParents)
        {
            LeafPlacement placement = FindLeafPlacement(config, map, candidate, MapPrototypeNodeType.Shop, null);
            if (placement == null)
                continue;

            candidate.lockedCombat = true;
            AddLeafNode(config, map, candidate, MapPrototypeNodeType.Shop, placement);
            shopParent = candidate;
            shopPlaced = true;
            break;
        }

        List<MapPrototypeNodeData> lateParents = Shuffle(mainNodes
            .Where(node => node.row >= config.intermediateRows - 2
                && node.row <= config.intermediateRows - 1
                && node != shopParent
                && GetNodeDegree(map, node.id) < config.maxNodeDegree)
            .ToList());

        foreach (MapPrototypeNodeData candidate in lateParents)
        {
            LeafPlacement placement = FindLeafPlacement(config, map, candidate, MapPrototypeNodeType.Forge, null);
            if (placement == null)
                continue;

            candidate.lockedCombat = true;
            AddLeafNode(config, map, candidate, MapPrototypeNodeType.Forge, placement);
            forgePlaced = true;
            break;
        }

        return shopPlaced && forgePlaced;
    }

    private sealed class LeafPlacement
    {
        public float x;
        public float y;
        public float distance;
        public int side;
        public float rowOffset = 1f;
    }

    private static MapPrototypeNodeData AddLeafNode(MapPrototypeConfig config, MapPrototypeData map, MapPrototypeNodeData parent, MapPrototypeNodeType type, LeafPlacement placement)
    {
        if (GetNodeDegree(map, parent.id) >= config.maxNodeDegree)
            return null;

        float colGap = (config.mapWidth - config.padX * 2f) / (config.columns - 1);
        MapPrototypeNodeData node = new MapPrototypeNodeData
        {
            id = $"{type}-{parent.id}-{MakeSuffix()}",
            key = $"{type}:{parent.key}",
            row = parent.row + 1f,
            col = (placement.x - config.padX) / colGap,
            type = type,
            x = placement.x,
            y = placement.y,
            visited = false,
            safeVisited = false,
            cleared = false,
            hasHint = false,
            hintTaken = false,
            shopHintBought = false,
            ranSkipped = false,
            specialLeaf = true,
            side = placement.side
        };

        map.nodes.Add(node);
        map.edges.Add(new MapPrototypeEdgeData { from = parent.id, to = node.id });

        if (!map.adjacency.ContainsKey(parent.id))
            map.adjacency[parent.id] = new HashSet<string>();
        if (!map.adjacency.ContainsKey(node.id))
            map.adjacency[node.id] = new HashSet<string>();

        map.adjacency[parent.id].Add(node.id);
        map.adjacency[node.id].Add(parent.id);
        return node;
    }

    private static LeafPlacement FindLeafPlacement(MapPrototypeConfig config, MapPrototypeData map, MapPrototypeNodeData parent, MapPrototypeNodeType type, string excludeNodeId)
    {
        float rowGap = (config.mapHeight - config.padY * 2f) / (config.intermediateRows + 1f);
        float targetRow = Mathf.Min(config.intermediateRows, parent.row + 1f);
        float targetY = config.mapHeight - config.padY - targetRow * rowGap;
        List<LeafPlacement> candidates = new List<LeafPlacement>();
        List<int> preferredSides = parent.x < config.mapWidth * 0.5f
            ? new List<int> { 1, -1 }
            : new List<int> { -1, 1 };

        List<int> sides = Shuffle(preferredSides);
        List<float> distanceSteps = new List<float>
        {
            config.leafIdealDistance - 12f,
            config.leafIdealDistance,
            config.leafIdealDistance + 12f,
            config.leafIdealDistance + 20f
        };
        List<int> lateralSigns = new List<int> { 1, -1 };
        List<float> yOffsets = new List<float> { 0f, -4f, 4f, -8f, 8f };

        foreach (int side in sides)
        {
            foreach (float distance in distanceSteps)
            {
                foreach (int sign in lateralSigns)
                {
                    foreach (float yOffset in yOffsets)
                    {
                        float y = targetY + yOffset;
                        float rise = Mathf.Abs(parent.y - y);
                        if (rise < config.leafMinRise || rise >= distance)
                            continue;

                        float runBase = Mathf.Sqrt(Mathf.Max(0f, distance * distance - rise * rise));
                        float run = Mathf.Max(config.leafMinRun, runBase);

                        candidates.Add(new LeafPlacement
                        {
                            x = parent.x + side * sign * run,
                            y = y,
                            distance = distance,
                            side = side * sign
                        });
                    }
                }
            }
        }

        LeafPlacement best = null;
        float bestScore = float.NegativeInfinity;
        foreach (LeafPlacement candidate in candidates)
        {
            if (!IsLeafPlacementSafe(config, map, parent, candidate, excludeNodeId))
                continue;

            float score = LeafPlacementScore(config, map, parent, candidate, excludeNodeId);
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    private static bool IsLeafPlacementSafe(MapPrototypeConfig config, MapPrototypeData map, MapPrototypeNodeData parent, LeafPlacement candidate, string excludeNodeId)
    {
        const float marginX = 86f;
        const float marginY = 96f;

        if (candidate.x < marginX || candidate.x > config.mapWidth - marginX)
            return false;
        if (candidate.y < config.padY - 18f || candidate.y > config.mapHeight - marginY)
            return false;

        float dx = candidate.x - parent.x;
        float dy = candidate.y - parent.y;
        float rise = Mathf.Abs(dy);
        float run = Mathf.Abs(dx);
        float distance = Mathf.Sqrt(dx * dx + dy * dy);

        if (dy >= -42f)
            return false;
        if (distance < config.leafMinDistance || distance > config.leafMaxDistance)
            return false;
        if (run < config.leafMinRun || rise < config.leafMinRise)
            return false;

        foreach (MapPrototypeNodeData node in map.nodes)
        {
            if (node.id == parent.id || node.id == excludeNodeId)
                continue;

            float minDist = node.specialLeaf ? 204f : 176f;
            if (Distance(node.x, node.y, candidate.x, candidate.y) < minDist)
                return false;
            if (PointToSegmentDistance(node.x, node.y, parent.x, parent.y, candidate.x, candidate.y) < 100f)
                return false;
        }

        foreach (MapPrototypeEdgeData edge in map.edges)
        {
            if (edge.from == excludeNodeId || edge.to == excludeNodeId)
                continue;

            MapPrototypeNodeData from = GetNodeById(map, edge.from);
            MapPrototypeNodeData to = GetNodeById(map, edge.to);
            if (from == null || to == null)
                continue;
            if (from.id == parent.id || to.id == parent.id)
                continue;

            if (SegmentsIntersect(parent.x, parent.y, candidate.x, candidate.y, from.x, from.y, to.x, to.y))
                return false;
            if (PointToSegmentDistance(candidate.x, candidate.y, from.x, from.y, to.x, to.y) < 116f)
                return false;
        }

        return true;
    }

    private static float LeafPlacementScore(MapPrototypeConfig config, MapPrototypeData map, MapPrototypeNodeData parent, LeafPlacement candidate, string excludeNodeId)
    {
        float minNodeDist = float.PositiveInfinity;
        float minEdgeDist = float.PositiveInfinity;

        foreach (MapPrototypeNodeData node in map.nodes)
        {
            if (node.id == parent.id || node.id == excludeNodeId)
                continue;

            minNodeDist = Mathf.Min(minNodeDist, Distance(node.x, node.y, candidate.x, candidate.y));
            minEdgeDist = Mathf.Min(minEdgeDist, PointToSegmentDistance(node.x, node.y, parent.x, parent.y, candidate.x, candidate.y));
        }

        foreach (MapPrototypeEdgeData edge in map.edges)
        {
            if (edge.from == excludeNodeId || edge.to == excludeNodeId)
                continue;

            MapPrototypeNodeData from = GetNodeById(map, edge.from);
            MapPrototypeNodeData to = GetNodeById(map, edge.to);
            if (from == null || to == null)
                continue;
            if (from.id == parent.id || to.id == parent.id)
                continue;

            minEdgeDist = Mathf.Min(minEdgeDist, PointToSegmentDistance(candidate.x, candidate.y, from.x, from.y, to.x, to.y));
        }

        float dx = candidate.x - parent.x;
        float dy = candidate.y - parent.y;
        float distance = Mathf.Sqrt(dx * dx + dy * dy);
        float distPenalty = Mathf.Abs(distance - config.leafIdealDistance) * 4.2f;
        float tooFlatPenalty = Mathf.Max(0f, config.leafMinRise - Mathf.Abs(dy)) * 2.6f;
        float tooNarrowPenalty = Mathf.Max(0f, config.leafMinRun - Mathf.Abs(dx)) * 2.1f;
        float centerBias = 36f - Mathf.Abs(candidate.x - config.mapWidth * 0.5f) * 0.05f;

        return minNodeDist * 1.1f + minEdgeDist * 0.9f + centerBias - distPenalty - tooFlatPenalty - tooNarrowPenalty;
    }

    private static void PositionNodes(MapPrototypeConfig config, List<MapPrototypeNodeData> nodes)
    {
        float rowGap = (config.mapHeight - config.padY * 2f) / (config.intermediateRows + 1f);
        float colGap = (config.mapWidth - config.padX * 2f) / (config.columns - 1f);
        Dictionary<float, List<MapPrototypeNodeData>> byRow = new Dictionary<float, List<MapPrototypeNodeData>>();

        foreach (MapPrototypeNodeData node in nodes)
        {
            if (!byRow.TryGetValue(node.row, out List<MapPrototypeNodeData> rowNodes))
            {
                rowNodes = new List<MapPrototypeNodeData>();
                byRow[node.row] = rowNodes;
            }

            rowNodes.Add(node);
        }

        foreach (KeyValuePair<float, List<MapPrototypeNodeData>> pair in byRow)
        {
            List<MapPrototypeNodeData> rowNodes = pair.Value;
            rowNodes.Sort((a, b) => a.col.CompareTo(b.col));
            foreach (MapPrototypeNodeData node in rowNodes)
            {
                float baseX = config.padX + node.col * colGap;
                float jitter = (node.type == MapPrototypeNodeType.Start || node.type == MapPrototypeNodeType.Boss)
                    ? 0f
                    : RandInt(-18, 18);

                node.x = Mathf.Clamp(baseX + jitter, config.padX - 12f, config.mapWidth - config.padX + 12f);
                node.y = config.mapHeight - config.padY - node.row * rowGap + RandInt(-5, 5);
            }
        }

        MapPrototypeNodeData start = nodes.FirstOrDefault(node => node.type == MapPrototypeNodeType.Start);
        MapPrototypeNodeData boss = nodes.FirstOrDefault(node => node.type == MapPrototypeNodeType.Boss);
        if (start != null)
        {
            start.x = config.mapWidth * 0.5f;
            start.y = config.mapHeight - config.padY + 8f;
        }
        if (boss != null)
        {
            boss.x = config.mapWidth * 0.5f;
            boss.y = config.padY - 12f;
        }
    }

    private static void ResolveGlobalNodeSpacing(MapPrototypeConfig config, MapPrototypeData map)
    {
        List<MapPrototypeNodeData> layoutNodes = map.nodes.Where(node => node.type != MapPrototypeNodeType.Start && node.type != MapPrototypeNodeType.Boss).ToList();
        const float minDist = 98f;
        for (int pass = 0; pass < 4; pass++)
        {
            bool moved = false;
            for (int i = 0; i < layoutNodes.Count; i++)
            {
                for (int j = i + 1; j < layoutNodes.Count; j++)
                {
                    MapPrototypeNodeData a = layoutNodes[i];
                    MapPrototypeNodeData b = layoutNodes[j];
                    float dx = b.x - a.x;
                    float dy = b.y - a.y;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist >= minDist || dist <= 0.001f)
                        continue;

                    float push = (minDist - dist) * 0.5f;
                    float nx = dx / Mathf.Max(1f, dist);
                    float ny = dy / Mathf.Max(1f, dist);
                    if (Mathf.Abs(ny) > 0.8f)
                        ny *= 0.2f;

                    a.x = Mathf.Clamp(a.x - nx * push, config.padX + 8f, config.mapWidth - config.padX - 8f);
                    b.x = Mathf.Clamp(b.x + nx * push, config.padX + 8f, config.mapWidth - config.padX - 8f);
                    if (!a.specialLeaf) a.y += -ny * push * 0.12f;
                    if (!b.specialLeaf) b.y += ny * push * 0.12f;
                    moved = true;
                }
            }
            if (!moved)
                break;
        }
    }

    private static List<MapPrototypeNodeData> PickSpacedNodes(MapPrototypeData map, List<MapPrototypeNodeData> candidates, int count, int minNodeGap, int minRowGap)
    {
        List<MapPrototypeNodeData> pool = Shuffle(new List<MapPrototypeNodeData>(candidates));
        List<MapPrototypeNodeData> picked = new List<MapPrototypeNodeData>();

        foreach (MapPrototypeNodeData candidate in pool)
        {
            bool valid = picked.All(chosen =>
                Mathf.Abs(candidate.row - chosen.row) >= minRowGap
                && PathNodesBetween(map, candidate.id, chosen.id) >= minNodeGap);

            if (!valid)
                continue;

            picked.Add(candidate);
            if (picked.Count >= count)
                break;
        }

        return picked;
    }

    private static void SeparateNearVerticalLinks(MapPrototypeConfig config, MapPrototypeData map)
    {
        const float minimumRun = 74f;
        const float pushAmount = 42f;
        float left = config.padX + 18f;
        float right = config.mapWidth - config.padX - 18f;

        Dictionary<string, MapPrototypeNodeData> byId = map.nodes.ToDictionary(node => node.id, node => node);
        for (int pass = 0; pass < 2; pass++)
        {
            foreach (MapPrototypeEdgeData edge in map.edges)
            {
                if (!byId.TryGetValue(edge.from, out MapPrototypeNodeData from) || !byId.TryGetValue(edge.to, out MapPrototypeNodeData to))
                    continue;
                if (from.specialLeaf || to.specialLeaf)
                    continue;
                if (from.type == MapPrototypeNodeType.Start || from.type == MapPrototypeNodeType.Boss)
                    continue;
                if (to.type == MapPrototypeNodeType.Start || to.type == MapPrototypeNodeType.Boss)
                    continue;
                if (Mathf.Abs(from.row - to.row) > 1.01f)
                    continue;

                float dx = to.x - from.x;
                if (Mathf.Abs(dx) >= minimumRun)
                    continue;

                float direction = Mathf.Approximately(dx, 0f)
                    ? (Mathf.RoundToInt(to.row) % 2 == 0 ? 1f : -1f)
                    : Mathf.Sign(dx);
                to.x = Mathf.Clamp(to.x + direction * pushAmount, left, right);
            }
        }
    }

}

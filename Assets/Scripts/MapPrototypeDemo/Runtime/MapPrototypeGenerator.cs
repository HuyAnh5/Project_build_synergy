using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class MapPrototypeGenerator
{
    private sealed class WalkerState
    {
        public int col;
        public int lastDelta;
        public int flatStreak;
    }

    private struct WeightedInt
    {
        public int value;
        public float weight;
    }

    public static MapPrototypeData GenerateAct(MapPrototypeConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        MapPrototypeData bestMap = null;
        float bestScore = float.PositiveInfinity;
        int highestIntermediateSeen = 0;
        int highestPostLayoutIntermediateSeen = 0;
        int builtCount = 0;
        int layoutRejectedCount = 0;
        int typingRejectedCount = 0;
        int routeRejectedCount = 0;

        for (int attempt = 0; attempt < config.maxAttempts; attempt++)
        {
            MapPrototypeData map = BuildStsStyleMap(config);
            if (map == null)
                continue;

            builtCount += 1;
            highestIntermediateSeen = Mathf.Max(highestIntermediateSeen, CountIntermediateNodes(map));
            if (!ValidateMap(config, map))
                continue;

            if (!InsertSpecialSideBranches(config, map) || !ValidateNodeDegrees(config, map))
                continue;

            RebalanceMapLayout(config, map);
            highestPostLayoutIntermediateSeen = Mathf.Max(highestPostLayoutIntermediateSeen, CountIntermediateNodes(map));
            if (HasBadEdgeNodeOverlap(map) || !ValidateLeafSpacing(config, map) || !ValidateNodeDegrees(config, map))
            {
                layoutRejectedCount += 1;
                continue;
            }

            bool typesAttached = AttachTypesAndHints(config, map);
            if (!typesAttached || !ValidateRestSpacing(config, map) || !ValidateHintRules(config, map) || !ValidateEventAdjacencyRules(config, map))
            {
                typingRejectedCount += 1;
                continue;
            }

            RouteValidationResult routeCheck = ValidateRoutes(config, map);
            if (!routeCheck.ok
                || HasBadEdgeNodeOverlap(map)
                || !ValidateLeafSpacing(config, map)
                || !ValidateNodeDegrees(config, map)
                || !ValidateRestSpacing(config, map)
                || !ValidateHintRules(config, map)
                || !ValidateEventAdjacencyRules(config, map))
            {
                routeRejectedCount += 1;
                continue;
            }

            float combinedScore = routeCheck.rhythm.score + LeafSpacingScore(config, map) * 0.08f;
            if (combinedScore < bestScore)
            {
                bestScore = combinedScore;
                bestMap = map;
            }

            if (combinedScore <= 3f)
                return map;
        }

        if (bestMap != null)
            return bestMap;

        throw new InvalidOperationException(
            $"Could not generate a valid map after many attempts. built={builtCount}, maxIntermediateBuilt={highestIntermediateSeen}, maxIntermediateAfterLayout={highestPostLayoutIntermediateSeen}, layoutRejected={layoutRejectedCount}, typingRejected={typingRejectedCount}, routeRejected={routeRejectedCount}");
    }

    private static MapPrototypeData BuildStsStyleMap(MapPrototypeConfig config)
    {
        int rows = config.intermediateRows;
        int cols = config.columns;
        MapPrototypeNodeData startNode = MakeNode(MapPrototypeNodeType.Start, 0f, Mathf.Floor(cols / 2f));
        MapPrototypeNodeData bossNode = MakeNode(MapPrototypeNodeType.Boss, rows + 1f, Mathf.Floor(cols / 2f));

        List<int> startCandidates = Enumerable.Range(1, cols - 2).ToList();
        List<int> uniqueStarts = Sample(startCandidates, RandInt(3, 4)).OrderBy(v => v).ToList();

        List<WalkerState> walkers = new List<WalkerState>();
        for (int i = 0; i < config.pathCount; i++)
        {
            walkers.Add(new WalkerState
            {
                col = uniqueStarts[i % uniqueStarts.Count],
                lastDelta = 0,
                flatStreak = 0
            });
        }

        walkers.Sort((a, b) => a.col.CompareTo(b.col));

        Dictionary<int, List<int>> rowColumns = new Dictionary<int, List<int>>();
        HashSet<string> edgesSet = new HashSet<string>();

        for (int row = 1; row <= rows; row++)
        {
            List<int> currentCols = walkers.Select(w => w.col).OrderBy(v => v).ToList();
            if (row == 1)
                rowColumns[1] = currentCols.Distinct().ToList();

            if (row == rows)
                break;

            List<int> nextCols = new List<int>();
            for (int i = 0; i < walkers.Count; i++)
            {
                WalkerState walker = walkers[i];
                int col = walker.col;
                int lowerBound = i == 0 ? 0 : nextCols[i - 1];
                int upperBound = i == walkers.Count - 1
                    ? cols - 1
                    : Mathf.Min(cols - 1, walkers[i + 1].col + 1);

                List<int> options = new List<int> { col - 1, col, col + 1 }
                    .Where(c => c >= 0 && c < cols && c >= lowerBound && c <= upperBound)
                    .ToList();

                if (options.Count == 0)
                    options.Add(Mathf.Clamp(col, lowerBound, upperBound));

                List<WeightedInt> weighted = new List<WeightedInt>();
                foreach (int option in options)
                {
                    int delta = option - col;
                    float weight = delta == 0 ? 0.96f : 1f;
                    if (walker.flatStreak >= 2 && delta == 0) weight -= 0.1f;
                    if (walker.flatStreak >= 3 && delta == 0) weight -= 0.14f;
                    if (walker.lastDelta != 0 && delta == walker.lastDelta) weight -= 0.12f;
                    if (walker.lastDelta != 0 && delta == -walker.lastDelta) weight += 0.12f;
                    if (col <= 1 && delta < 0) weight = 0.12f;
                    if (col >= cols - 2 && delta > 0) weight = 0.12f;

                    weighted.Add(new WeightedInt
                    {
                        value = option,
                        weight = Mathf.Max(0.12f, weight)
                    });
                }

                nextCols.Add(WeightedChoice(weighted));
            }

            for (int i = 0; i < walkers.Count; i++)
            {
                int prevCol = walkers[i].col;
                int nextCol = nextCols[i];
                edgesSet.Add($"{row}:{prevCol}->{row + 1}:{nextCol}");
                walkers[i].col = nextCol;
                walkers[i].flatStreak = nextCol == prevCol ? walkers[i].flatStreak + 1 : 0;
                walkers[i].lastDelta = nextCol - prevCol;
            }

            rowColumns[row + 1] = nextCols.Distinct().ToList();
        }

        Dictionary<string, MapPrototypeNodeData> nodesByKey = new Dictionary<string, MapPrototypeNodeData>();

        MapPrototypeNodeData EnsureNode(MapPrototypeNodeType type, int row, int col)
        {
            string key = $"{row}:{col}";
            if (!nodesByKey.TryGetValue(key, out MapPrototypeNodeData node))
            {
                node = MakeNode(type, row, col);
                nodesByKey[key] = node;
            }

            return node;
        }

        nodesByKey[startNode.key] = startNode;
        nodesByKey[bossNode.key] = bossNode;

        foreach (KeyValuePair<int, List<int>> pair in rowColumns)
        {
            foreach (int col in pair.Value)
                EnsureNode(MapPrototypeNodeType.Combat, pair.Key, col);
        }

        List<MapPrototypeEdgeData> edges = new List<MapPrototypeEdgeData>();
        List<int> firstRowTargets = rowColumns.TryGetValue(1, out List<int> firstTargets)
            ? new List<int>(firstTargets)
            : new List<int>();

        if (firstRowTargets.Count < 2)
            return null;

        foreach (int col in firstRowTargets)
        {
            edges.Add(new MapPrototypeEdgeData
            {
                from = startNode.key,
                to = $"1:{col}"
            });
        }

        foreach (string token in edgesSet)
        {
            string[] split = token.Split(new[] { "->" }, StringSplitOptions.None);
            edges.Add(new MapPrototypeEdgeData
            {
                from = split[0],
                to = split[1]
            });
        }

        List<int> lastRowCols = rowColumns.TryGetValue(rows, out List<int> lastCols)
            ? new List<int>(lastCols)
            : new List<int>();

        if (lastRowCols.Count < 2)
            return null;

        foreach (int col in lastRowCols)
        {
            edges.Add(new MapPrototypeEdgeData
            {
                from = $"{rows}:{col}",
                to = bossNode.key
            });
        }

        List<MapPrototypeNodeData> nodes = nodesByKey.Values.ToList();
        PositionNodes(config, nodes);
        return BuildGraph(config, nodes, edges);
    }

    private static void RebalanceMapLayout(MapPrototypeConfig config, MapPrototypeData map)
    {
        float width = config.mapWidth;
        float margin = config.padX + 8f;
        float minGap = 108f;

        Dictionary<float, List<MapPrototypeNodeData>> byRow = new Dictionary<float, List<MapPrototypeNodeData>>();
        foreach (MapPrototypeNodeData node in map.nodes)
        {
            if (node.type == MapPrototypeNodeType.Start || node.type == MapPrototypeNodeType.Boss || node.specialLeaf)
                continue;

            if (!byRow.TryGetValue(node.row, out List<MapPrototypeNodeData> rowNodes))
            {
                rowNodes = new List<MapPrototypeNodeData>();
                byRow[node.row] = rowNodes;
            }

            rowNodes.Add(node);
        }

        foreach (float row in byRow.Keys.OrderBy(v => v))
        {
            List<MapPrototypeNodeData> rowNodes = byRow[row];
            rowNodes.Sort((a, b) => a.x.CompareTo(b.x));
            if (rowNodes.Count == 0)
                continue;

            float mean = rowNodes.Sum(node => node.x) / rowNodes.Count;
            float shift = width * 0.5f - mean;
            shift = Mathf.Clamp(shift, -72f, 72f);
            foreach (MapPrototypeNodeData node in rowNodes)
                node.x = Mathf.Clamp(node.x + shift, margin, width - margin);

            rowNodes.Sort((a, b) => a.x.CompareTo(b.x));
            for (int i = 1; i < rowNodes.Count; i++)
            {
                MapPrototypeNodeData prev = rowNodes[i - 1];
                MapPrototypeNodeData current = rowNodes[i];
                if (current.x - prev.x < minGap)
                    current.x = prev.x + minGap;
            }

            for (int i = rowNodes.Count - 2; i >= 0; i--)
            {
                MapPrototypeNodeData next = rowNodes[i + 1];
                MapPrototypeNodeData current = rowNodes[i];
                if (next.x > width - margin)
                {
                    float overflow = next.x - (width - margin);
                    next.x -= overflow;
                    current.x -= overflow;
                }

                if (next.x - current.x < minGap)
                    current.x = next.x - minGap;
            }

            float left = rowNodes[0].x;
            float right = rowNodes[rowNodes.Count - 1].x;
            if (left < margin)
            {
                float push = margin - left;
                foreach (MapPrototypeNodeData node in rowNodes)
                    node.x += push;
            }

            if (right > width - margin)
            {
                float push = right - (width - margin);
                foreach (MapPrototypeNodeData node in rowNodes)
                    node.x -= push;
            }
        }

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

        List<MapPrototypeNodeData> preferredLateParents = Shuffle(mainNodes
            .Where(node => node.row >= config.intermediateRows - 2
                && node.row <= config.intermediateRows - 1
                && node != shopParent
                && GetNodeDegree(map, node.id) < config.maxNodeDegree)
            .ToList());

        List<MapPrototypeNodeData> fallbackLateParents = Shuffle(mainNodes
            .Where(node => node.row >= config.intermediateRows - 3
                && node.row <= config.intermediateRows - 1
                && node != shopParent
                && GetNodeDegree(map, node.id) < config.maxNodeDegree)
            .ToList());

        List<MapPrototypeNodeData> lateParents = new List<MapPrototypeNodeData>(preferredLateParents);
        lateParents.AddRange(fallbackLateParents.Where(node => !preferredLateParents.Contains(node)));

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

            float minDist = node.specialLeaf ? 162f : 138f;
            if (Distance(node.x, node.y, candidate.x, candidate.y) < minDist)
                return false;
            if (PointToSegmentDistance(node.x, node.y, parent.x, parent.y, candidate.x, candidate.y) < 78f)
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
            if (PointToSegmentDistance(candidate.x, candidate.y, from.x, from.y, to.x, to.y) < 84f)
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
                    : RandInt(-10, 10);

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

    private static MapPrototypeData BuildGraph(MapPrototypeConfig config, List<MapPrototypeNodeData> nodes, List<MapPrototypeEdgeData> edges)
    {
        Dictionary<string, MapPrototypeNodeData> byKey = nodes.ToDictionary(node => node.key, node => node);
        Dictionary<string, HashSet<string>> adjacency = nodes.ToDictionary(node => node.id, node => new HashSet<string>());
        List<MapPrototypeEdgeData> resolvedEdges = new List<MapPrototypeEdgeData>();

        foreach (MapPrototypeEdgeData edge in edges)
        {
            if (!byKey.TryGetValue(edge.from, out MapPrototypeNodeData fromNode))
                continue;
            if (!byKey.TryGetValue(edge.to, out MapPrototypeNodeData toNode))
                continue;
            if (fromNode.key == toNode.key)
                continue;

            resolvedEdges.Add(new MapPrototypeEdgeData { from = fromNode.id, to = toNode.id });
            adjacency[fromNode.id].Add(toNode.id);
            adjacency[toNode.id].Add(fromNode.id);
        }

        Dictionary<string, MapPrototypeEdgeData> dedup = new Dictionary<string, MapPrototypeEdgeData>();
        foreach (MapPrototypeEdgeData edge in resolvedEdges)
        {
            string token = string.CompareOrdinal(edge.from, edge.to) < 0
                ? $"{edge.from}|{edge.to}"
                : $"{edge.to}|{edge.from}";
            dedup[token] = edge;
        }

        MapPrototypeData map = new MapPrototypeData();
        map.nodes.AddRange(nodes);
        map.edges.AddRange(dedup.Values);
        foreach (KeyValuePair<string, HashSet<string>> pair in adjacency)
            map.adjacency[pair.Key] = pair.Value;

        map.boss = Choice(config.bosses);
        if (!ValidateNodeDegrees(config, map))
            return null;

        return map;
    }

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
        foreach (MapPrototypeNodeData node in Sample(eliteCandidates, RandInt(2, 3)))
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
        bool hasValidBossEntrance = ValidateBossEntrance(allPaths, nodeById);
        RhythmEvaluationResult rhythm = EvaluateDecisionRhythm(config, map, allPaths, forward);

        return new RouteValidationResult
        {
            ok = hasNoEliteRoute && hasEliteRoute && hasShop && hasForge && hasValidBossEntrance && rhythm.ok,
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
        float penalty = 0f;

        foreach (List<string> path in allPaths)
        {
            int streak = 0;

            void Flush()
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

            for (int i = 1; i < path.Count - 1; i++)
            {
                MapPrototypeNodeData node = nodeById[path[i]];
                if (node.specialLeaf)
                {
                    Flush();
                    continue;
                }

                int outCount = (forward.TryGetValue(node.id, out List<string> nextIds) ? nextIds : new List<string>())
                    .Count(nextId => !nodeById[nextId].specialLeaf);
                int inDeg = indegree[node.id];
                bool isLinear = inDeg == 1 && outCount == 1;

                if (isLinear)
                    streak += 1;
                else
                    Flush();
            }

            Flush();
        }

        bool ok = maxStreak <= config.maxForcedLinearStreak;

        return new RhythmEvaluationResult
        {
            ok = ok,
            score = penalty + Mathf.Max(0, maxStreak - 3) * 3f + exactFourCount * 1.5f + fivePlusCount * 10f,
            maxStreak = maxStreak,
            exactFourCount = exactFourCount,
            fivePlusCount = fivePlusCount,
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

    private static int PathNodesBetween(MapPrototypeData map, string startId, string goalId)
    {
        int dist = ShortestNodeDistance(map, startId, goalId);
        if (dist == int.MaxValue)
            return int.MaxValue;
        return Mathf.Max(0, dist - 1);
    }

    private static int ChooseTargetIntermediateCount(MapPrototypeConfig config)
    {
        float roll = UnityEngine.Random.value;
        if (roll < 0.28f)
            return RandInt(25, 30);
        if (roll < 0.65f)
            return RandInt(23, 28);
        return RandInt(20, 26);
    }

    private static void ExpandIntermediateCoverage(MapPrototypeConfig config, Dictionary<int, List<int>> rowColumns, HashSet<string> edgesSet, int targetIntermediateCount)
    {
        int cols = config.columns;
        int rows = config.intermediateRows;
        int current = rowColumns.Values.Sum(list => list.Distinct().Count());
        int attempts = 0;
        while (current < targetIntermediateCount && attempts < 120)
        {
            attempts += 1;
            List<int> rowPool = Enumerable.Range(1, rows)
                .Where(row => rowColumns.ContainsKey(row) && rowColumns[row].Distinct().Count() < Mathf.Min(cols - 1, 5))
                .OrderBy(row => Mathf.Abs(row - Mathf.CeilToInt(rows * 0.5f)))
                .ToList();
            if (rowPool.Count == 0)
                break;

            int row = Choice(rowPool);
            HashSet<int> used = new HashSet<int>(rowColumns[row]);
            List<int> available = Enumerable.Range(0, cols)
                .Where(col => !used.Contains(col))
                .Where(col => row == 1 || rowColumns[row - 1].Any(prev => Mathf.Abs(prev - col) <= 1))
                .Where(col => row == rows || rowColumns[row + 1].Any(next => Mathf.Abs(next - col) <= 1))
                .ToList();
            if (available.Count == 0)
                continue;

            available = available.OrderBy(col => ColumnAddScore(rowColumns, row, col)).ToList();
            int bestCol = available[0];
            rowColumns[row].Add(bestCol);
            rowColumns[row] = rowColumns[row].Distinct().OrderBy(v => v).ToList();
            current += 1;

            if (row > 1)
            {
                int prev = rowColumns[row - 1].OrderBy(col => Mathf.Abs(col - bestCol)).First();
                edgesSet.Add($"{row - 1}:{prev}->{row}:{bestCol}");
            }
            if (row < rows)
            {
                int next = rowColumns[row + 1].OrderBy(col => Mathf.Abs(col - bestCol)).First();
                edgesSet.Add($"{row}:{bestCol}->{row + 1}:{next}");
            }
        }
    }

    private static float ColumnAddScore(Dictionary<int, List<int>> rowColumns, int row, int col)
    {
        List<int> same = rowColumns[row].Distinct().OrderBy(v => v).ToList();
        float samePenalty = same.Count == 0 ? 0f : same.Min(existing => Mathf.Abs(existing - col)) * -2.2f;
        float prevPenalty = rowColumns.ContainsKey(row - 1) ? rowColumns[row - 1].Min(existing => Mathf.Abs(existing - col)) * 1.3f : 0f;
        float nextPenalty = rowColumns.ContainsKey(row + 1) ? rowColumns[row + 1].Min(existing => Mathf.Abs(existing - col)) * 1.3f : 0f;
        return samePenalty - prevPenalty - nextPenalty + UnityEngine.Random.value * 3f;
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

    private static bool ValidateBossEntrance(List<List<string>> allPaths, Dictionary<string, MapPrototypeNodeData> nodeById)
    {
        HashSet<string> preBossParents = new HashSet<string>();
        HashSet<string> lateSuffixes = new HashSet<string>();
        foreach (List<string> path in allPaths)
        {
            if (path.Count < 3)
                continue;
            string parent = path[path.Count - 2];
            if (nodeById[parent].specialLeaf)
                continue;
            preBossParents.Add(parent);

            int startIndex = Mathf.Max(1, path.Count - 5);
            List<string> suffix = path.Skip(startIndex).Take(path.Count - 1 - startIndex).Where(id => !nodeById[id].specialLeaf).ToList();
            lateSuffixes.Add(string.Join("|", suffix));
        }
        return preBossParents.Count >= 2 && lateSuffixes.Count >= 2;
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

    public static MapPrototypeNodeData GetNodeById(MapPrototypeData map, string id)
    {
        if (map == null || string.IsNullOrWhiteSpace(id))
            return null;

        for (int i = 0; i < map.nodes.Count; i++)
        {
            if (map.nodes[i].id == id)
                return map.nodes[i];
        }

        return null;
    }

    private static MapPrototypeNodeData MakeNode(MapPrototypeNodeType type, float row, float col)
    {
        return new MapPrototypeNodeData
        {
            id = $"{type}-{row}-{col}-{MakeSuffix()}",
            key = $"{row:0.##}:{col:0.##}",
            row = row,
            col = col,
            type = type,
            x = 0f,
            y = 0f,
            visited = false,
            safeVisited = type == MapPrototypeNodeType.Start,
            cleared = type == MapPrototypeNodeType.Start,
            hasHint = false,
            hintTaken = false,
            shopHintBought = false,
            ranSkipped = false
        };
    }

    private static string MakeSuffix()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 5);
    }

    private static int RandInt(int min, int max)
    {
        return UnityEngine.Random.Range(min, max + 1);
    }

    private static T Choice<T>(IList<T> items)
    {
        return items[UnityEngine.Random.Range(0, items.Count)];
    }

    private static int WeightedChoice(List<WeightedInt> items)
    {
        float total = 0f;
        for (int i = 0; i < items.Count; i++)
            total += items[i].weight;

        float roll = UnityEngine.Random.value * total;
        for (int i = 0; i < items.Count; i++)
        {
            roll -= items[i].weight;
            if (roll <= 0f)
                return items[i].value;
        }

        return items[items.Count - 1].value;
    }

    private static List<T> Sample<T>(IList<T> source, int count)
    {
        List<T> clone = new List<T>(source);
        List<T> output = new List<T>();
        while (clone.Count > 0 && output.Count < count)
        {
            int index = UnityEngine.Random.Range(0, clone.Count);
            output.Add(clone[index]);
            clone.RemoveAt(index);
        }

        return output;
    }

    private static List<T> Shuffle<T>(List<T> items)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            T tmp = items[i];
            items[i] = items[j];
            items[j] = tmp;
        }

        return items;
    }

    private static float Distance(float ax, float ay, float bx, float by)
    {
        return Mathf.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by));
    }

    private static float PointToSegmentDistance(float px, float py, float x1, float y1, float x2, float y2)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        if (Mathf.Approximately(dx, 0f) && Mathf.Approximately(dy, 0f))
            return Distance(px, py, x1, y1);

        float t = Mathf.Clamp01(((px - x1) * dx + (py - y1) * dy) / (dx * dx + dy * dy));
        float projX = x1 + t * dx;
        float projY = y1 + t * dy;
        return Distance(px, py, projX, projY);
    }

    private static int Orientation(float ax, float ay, float bx, float by, float cx, float cy)
    {
        float value = (by - ay) * (cx - bx) - (bx - ax) * (cy - by);
        if (Mathf.Abs(value) < 0.01f)
            return 0;
        return value > 0f ? 1 : 2;
    }

    private static bool OnSegment(float ax, float ay, float bx, float by, float cx, float cy)
    {
        return bx <= Mathf.Max(ax, cx) + 0.01f && bx + 0.01f >= Mathf.Min(ax, cx)
            && by <= Mathf.Max(ay, cy) + 0.01f && by + 0.01f >= Mathf.Min(ay, cy);
    }

    private static bool SegmentsIntersect(float ax, float ay, float bx, float by, float cx, float cy, float dx, float dy)
    {
        int o1 = Orientation(ax, ay, bx, by, cx, cy);
        int o2 = Orientation(ax, ay, bx, by, dx, dy);
        int o3 = Orientation(cx, cy, dx, dy, ax, ay);
        int o4 = Orientation(cx, cy, dx, dy, bx, by);

        if (o1 != o2 && o3 != o4)
            return true;
        if (o1 == 0 && OnSegment(ax, ay, cx, cy, bx, by))
            return true;
        if (o2 == 0 && OnSegment(ax, ay, dx, dy, bx, by))
            return true;
        if (o3 == 0 && OnSegment(cx, cy, ax, ay, dx, dy))
            return true;
        if (o4 == 0 && OnSegment(cx, cy, bx, by, dx, dy))
            return true;

        return false;
    }
}

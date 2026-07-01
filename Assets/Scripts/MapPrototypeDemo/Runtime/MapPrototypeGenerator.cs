using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static partial class MapPrototypeGenerator
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
            config = new MapPrototypeConfig();

        if (config.useLinearPrototypeLayout)
            return BuildLinearPrototypeAct(config);

        MapPrototypeData bestMap = null;
        float bestScore = float.PositiveInfinity;
        int highestIntermediateSeen = 0;
        int highestPostLayoutIntermediateSeen = 0;
        int builtCount = 0;
        int graphNullCount = 0;
        int layoutRejectedCount = 0;
        int routeRejectedCount = 0;
        int specialPlacementRejectedCount = 0;

        for (int attempt = 0; attempt < config.maxAttempts; attempt++)
        {
            MapPrototypeData map = BuildBudgetedLaneMap(config);
            if (map == null)
            {
                graphNullCount += 1;
                continue;
            }

            builtCount += 1;
            highestIntermediateSeen = Mathf.Max(highestIntermediateSeen, CountIntermediateNodes(map));
            if (!ValidateMap(config, map))
                continue;

            if (!InsertSpecialSideBranches(config, map) || !ValidateNodeDegrees(config, map))
            {
                specialPlacementRejectedCount += 1;
                continue;
            }

            if (!FinalizeFixedGridLayout(config, map))
            {
                layoutRejectedCount += 1;
                continue;
            }
            highestPostLayoutIntermediateSeen = Mathf.Max(highestPostLayoutIntermediateSeen, CountIntermediateNodes(map));
            if (!ValidateNodeDegrees(config, map))
            {
                layoutRejectedCount += 1;
                continue;
            }

            if (!ValidateSpecialTopology(map) || GetBossParentIds(map).Count < 2)
                continue;

            AttachTypesAndHints(config, map);
            RouteValidationResult routeCheck = ValidateRoutes(config, map);
            if (!routeCheck.ok)
                routeRejectedCount += 1;

            int restTarget = CountIntermediateNodes(map) <= 26 ? 2 : 3;
            int eliteTarget = CountIntermediateNodes(map) <= 26 ? 2 : 3;
            int restCount = map.nodes.Count(node => node.type == MapPrototypeNodeType.Rest);
            int eliteCount = map.nodes.Count(node => node.type == MapPrototypeNodeType.Elite);
            int eventHintCount = map.nodes.Count(node => node.type == MapPrototypeNodeType.Event && node.hasHint);
            float combinedScore = routeCheck.rhythm.score
                + LeafSpacingScore(config, map) * 0.08f
                + Mathf.Abs(restTarget - restCount) * 4f
                + Mathf.Abs(eliteTarget - eliteCount) * 5f
                + Mathf.Abs(config.extraHintSources - eventHintCount) * 3f
                + (routeCheck.ok ? 0f : 18f);
            if (combinedScore < bestScore)
            {
                bestScore = combinedScore;
                bestMap = map;
            }

            if (routeCheck.ok && combinedScore <= 6f)
                return map;
        }

        if (bestMap != null)
            return bestMap;

        Debug.LogWarning(
            $"Map generation used emergency fallback. built={builtCount}, graphNull={graphNullCount}, maxIntermediateBuilt={highestIntermediateSeen}, maxIntermediateAfterLayout={highestPostLayoutIntermediateSeen}, layoutRejected={layoutRejectedCount}, specialPlacementRejected={specialPlacementRejectedCount}, routeSoftFail={routeRejectedCount}");
        return BuildEmergencyFallbackAct(config);
    }

    private static bool ValidateSpecialTopology(MapPrototypeData map)
    {
        List<MapPrototypeNodeData> leaves = map.nodes
            .Where(node => node.type == MapPrototypeNodeType.Shop || node.type == MapPrototypeNodeType.Forge)
            .ToList();
        return leaves.Count == 2
            && leaves.Count(node => node.type == MapPrototypeNodeType.Shop) == 1
            && leaves.Count(node => node.type == MapPrototypeNodeType.Forge) == 1
            && leaves.All(node => node.specialLeaf && GetNodeDegree(map, node.id) == 1);
    }

    private static MapPrototypeData BuildEmergencyFallbackAct(MapPrototypeConfig config)
    {
        const int rows = 9;
        MapPrototypeNodeData start = MakeNode(MapPrototypeNodeType.Start, 0f, 3f);
        MapPrototypeNodeData boss = MakeNode(MapPrototypeNodeType.Boss, rows + 1f, 3f);
        List<MapPrototypeNodeData> nodes = new List<MapPrototypeNodeData> { start, boss };
        Dictionary<int, List<int>> rowColumns = new Dictionary<int, List<int>>();

        for (int row = 1; row <= rows; row++)
        {
            rowColumns[row] = row <= 7
                ? new List<int> { 1, 3, 5 }
                : new List<int> { 1, 5 };
            foreach (int column in rowColumns[row])
                nodes.Add(MakeNode(MapPrototypeNodeType.Combat, row, column));
        }

        List<MapPrototypeEdgeData> edges = new List<MapPrototypeEdgeData>();
        foreach (int column in rowColumns[1])
            AddKeyEdge(edges, start.key, $"1:{column}");
        for (int row = 1; row < rows; row++)
            ConnectBudgetedRows(edges, row, rowColumns[row], rowColumns[row + 1]);
        foreach (int column in rowColumns[rows])
            AddKeyEdge(edges, $"{rows}:{column}", boss.key);

        PositionNodes(config, nodes);
        MapPrototypeData map = BuildGraph(config, nodes, edges);
        if (map == null)
            return BuildLinearPrototypeAct(config);

        MapPrototypeNodeData shopParent = nodes.First(node => Mathf.Approximately(node.row, 3f) && Mathf.Approximately(node.col, 5f));
        MapPrototypeNodeData forgeParent = nodes.First(node => Mathf.Approximately(node.row, 7f) && Mathf.Approximately(node.col, 5f));
        shopParent.lockedCombat = true;
        forgeParent.lockedCombat = true;
        AddLeafNode(config, map, shopParent, MapPrototypeNodeType.Shop, new LeafPlacement
        {
            x = shopParent.x + 142f,
            y = shopParent.y - 54f,
            side = 1,
            rowOffset = 0.45f
        });
        AddLeafNode(config, map, forgeParent, MapPrototypeNodeType.Forge, new LeafPlacement
        {
            x = forgeParent.x + 142f,
            y = forgeParent.y - 54f,
            side = 1,
            rowOffset = 0.45f
        });
        AttachTypesAndHints(config, map);
        return map;
    }

    private static MapPrototypeData BuildLinearPrototypeAct(MapPrototypeConfig config)
    {
        int combatNodeCount = Mathf.Max(1, config.linearCombatNodeCount);
        int totalNodeCount = combatNodeCount + 1;
        float centerCol = Mathf.Floor(Mathf.Max(1, config.columns) * 0.5f);

        List<MapPrototypeNodeData> nodes = new List<MapPrototypeNodeData>(combatNodeCount + 1);
        List<MapPrototypeEdgeData> edges = new List<MapPrototypeEdgeData>(combatNodeCount);

        MapPrototypeNodeData startNode = MakeNode(MapPrototypeNodeType.Start, 0f, centerCol);
        nodes.Add(startNode);

        MapPrototypeNodeData previous = startNode;
        for (int i = 0; i < combatNodeCount; i++)
        {
            bool isBoss = i == combatNodeCount - 1;
            MapPrototypeNodeData node = MakeNode(
                isBoss ? MapPrototypeNodeType.Boss : MapPrototypeNodeType.Combat,
                i + 1,
                centerCol);
            node.encounterIndex = i;
            nodes.Add(node);
            edges.Add(new MapPrototypeEdgeData
            {
                from = previous.key,
                to = node.key
            });
            previous = node;
        }

        float usableHeight = Mathf.Max(1f, config.mapHeight - config.padY * 2f);
        float rowGap = totalNodeCount > 1 ? usableHeight / (totalNodeCount - 1) : 0f;
        float centerX = config.mapWidth * 0.5f;
        for (int i = 0; i < nodes.Count; i++)
        {
            MapPrototypeNodeData node = nodes[i];
            node.x = centerX;
            node.y = config.mapHeight - config.padY - rowGap * i;
        }

        return BuildGraph(config, nodes, edges);
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
                    float weight = delta == 0 ? 0.38f : 1f;
                    if (walker.flatStreak >= 1 && delta == 0) weight -= 0.16f;
                    if (walker.flatStreak >= 2 && delta == 0) weight -= 0.18f;
                    if (walker.flatStreak >= 3 && delta == 0) weight = 0.02f;
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

        ExpandIntermediateCoverage(config, rowColumns, edgesSet, ChooseTargetIntermediateCount(config));

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

}

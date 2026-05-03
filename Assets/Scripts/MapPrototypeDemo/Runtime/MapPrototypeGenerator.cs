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
            if (!typesAttached
                || !ValidateRestSpacing(config, map)
                || !ValidateHintRules(config, map)
                || !ValidateEventAdjacencyRules(config, map)
                || !ValidateEliteSpacing(config, map)
                || !ValidateBossEntranceRoutes(config, map))
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
                || !ValidateEventAdjacencyRules(config, map)
                || !ValidateEliteSpacing(config, map)
                || !ValidateBossEntranceRoutes(config, map))
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

}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static partial class MapPrototypeGenerator
{
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

}

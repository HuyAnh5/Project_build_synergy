using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static partial class MapPrototypeGenerator
{
    private static MapPrototypeData BuildBudgetedLaneMap(MapPrototypeConfig config)
    {
        int rows = Mathf.Max(1, config.intermediateRows);
        int columns = Mathf.Max(7, config.columns);
        int target = ChooseTargetIntermediateCount(config);
        int[] quotas = AllocateRowQuotas(rows, target);
        Dictionary<int, List<int>> rowColumns = new Dictionary<int, List<int>>();

        for (int row = 1; row <= rows; row++)
        {
            List<int> previous = row > 1 ? rowColumns[row - 1] : null;
            rowColumns[row] = BuildLaneColumns(columns, quotas[row - 1], previous);
        }

        MapPrototypeNodeData start = MakeNode(MapPrototypeNodeType.Start, 0f, columns / 2f);
        MapPrototypeNodeData boss = MakeNode(MapPrototypeNodeType.Boss, rows + 1f, columns / 2f);
        List<MapPrototypeNodeData> nodes = new List<MapPrototypeNodeData> { start, boss };
        Dictionary<string, MapPrototypeNodeData> byKey = new Dictionary<string, MapPrototypeNodeData>
        {
            [start.key] = start,
            [boss.key] = boss
        };

        for (int row = 1; row <= rows; row++)
        {
            foreach (int column in rowColumns[row])
            {
                MapPrototypeNodeData node = MakeNode(MapPrototypeNodeType.Combat, row, column);
                nodes.Add(node);
                byKey[node.key] = node;
            }
        }

        List<MapPrototypeEdgeData> edges = new List<MapPrototypeEdgeData>();
        foreach (int column in rowColumns[1])
            AddKeyEdge(edges, start.key, $"{1}:{column}");

        for (int row = 1; row < rows; row++)
            ConnectBudgetedRows(edges, row, rowColumns[row], rowColumns[row + 1]);

        int braidOffset = RandInt(1, 2);
        for (int row = braidOffset; row < rows; row += RandInt(2, 3))
            TryAddBraidedConnection(edges, row, rowColumns[row], rowColumns[row + 1]);

        if (!HasMiddleLaneChange(edges, rows))
        {
            List<int> middleRows = Enumerable.Range(2, Mathf.Max(1, rows - 3))
                .OrderBy(row => Mathf.Abs(row - rows * 0.5f))
                .ToList();
            bool addedMiddleBraid = middleRows.Any(row =>
                row < rows && TryAddBraidedConnection(edges, row, rowColumns[row], rowColumns[row + 1]));
            if (!addedMiddleBraid && !HasMiddleLaneChange(edges, rows))
                return null;
        }

        foreach (int column in rowColumns[rows])
            AddKeyEdge(edges, $"{rows}:{column}", boss.key);

        PositionNodes(config, nodes);
        return BuildGraph(config, nodes, edges);
    }

    private static int[] AllocateRowQuotas(int rows, int target)
    {
        int[] quotas = Enumerable.Repeat(2, rows).ToArray();
        int remaining = Mathf.Max(0, target - rows * 2);

        if (remaining > 0)
        {
            quotas[0] += 1;
            remaining -= 1;
        }
        if (remaining > 0 && rows > 1)
        {
            quotas[rows - 1] += 1;
            remaining -= 1;
        }

        while (remaining > 0)
        {
            List<int> candidates = Enumerable.Range(0, rows)
                .Where(index => quotas[index] < 4)
                .ToList();
            if (candidates.Count == 0)
                break;

            float center = (rows - 1) * 0.5f;
            List<WeightedInt> weighted = candidates
                .Select(index => new WeightedInt
                {
                    value = index,
                    weight = 1f + Mathf.Max(0f, center - Mathf.Abs(index - center)) * 0.35f
                })
                .ToList();
            quotas[WeightedChoice(weighted)] += 1;
            remaining -= 1;
        }

        return quotas;
    }

    private static List<int> BuildLaneColumns(int columns, int quota, List<int> previousColumns)
    {
        List<List<int>> valid = new List<List<int>>();
        int maskLimit = 1 << columns;
        for (int mask = 0; mask < maskLimit; mask++)
        {
            List<int> candidate = new List<int>();
            for (int column = 0; column < columns; column++)
            {
                if ((mask & (1 << column)) != 0)
                    candidate.Add(column);
            }
            if (candidate.Count != quota)
                continue;
            if (previousColumns == null)
            {
                if (candidate[candidate.Count - 1] - candidate[0] >= 3)
                    valid.Add(candidate);
                continue;
            }
            if (candidate.All(column => previousColumns.Any(previous => Mathf.Abs(previous - column) <= 1))
                && previousColumns.All(previous => candidate.Any(column => Mathf.Abs(previous - column) <= 1)))
                valid.Add(candidate);
        }

        if (valid.Count == 0)
        {
            if (quota <= 2)
                return new List<int> { 2, 4 };
            if (quota == 3)
                return new List<int> { 1, 3, 5 };
            return new List<int> { 1, 2, 4, 5 };
        }

        return valid
            .OrderByDescending(candidate => candidate.Sum(column => Mathf.Min(
                Mathf.Abs(column - 1),
                Mathf.Abs(column - 3),
                Mathf.Abs(column - 5))) * -1f + UnityEngine.Random.value * 2.5f)
            .Take(Mathf.Min(6, valid.Count))
            .OrderBy(_ => UnityEngine.Random.value)
            .First();
    }

    private static void ConnectBudgetedRows(List<MapPrototypeEdgeData> edges, int row, List<int> fromColumns, List<int> toColumns)
    {
        foreach (int from in fromColumns)
        {
            int nearest = toColumns.OrderBy(to => Mathf.Abs(to - from)).First();
            AddKeyEdge(edges, $"{row}:{from}", $"{row + 1}:{nearest}");
        }

        foreach (int to in toColumns)
        {
            int nearest = fromColumns.OrderBy(from => Mathf.Abs(from - to)).First();
            AddKeyEdge(edges, $"{row}:{nearest}", $"{row + 1}:{to}");
        }

    }

    private static bool TryAddBraidedConnection(
        List<MapPrototypeEdgeData> edges,
        int row,
        List<int> fromColumns,
        List<int> toColumns)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();
        foreach (int from in fromColumns)
        {
            foreach (int to in toColumns)
            {
                if (from == to || Mathf.Abs(from - to) > 1)
                    continue;

                string fromKey = $"{row}:{from}";
                string toKey = $"{row + 1}:{to}";
                if (edges.Any(edge => edge.from == fromKey && edge.to == toKey))
                    continue;
                if (CountKeyDegree(edges, fromKey) >= 4 || CountKeyDegree(edges, toKey) >= 4)
                    continue;
                if (WouldCrossRowEdge(edges, row, from, to))
                    continue;

                candidates.Add(new Vector2Int(from, to));
            }
        }

        if (candidates.Count == 0)
            return false;

        Vector2Int picked = Choice(candidates);
        AddKeyEdge(edges, $"{row}:{picked.x}", $"{row + 1}:{picked.y}");
        return true;
    }

    private static bool HasMiddleLaneChange(List<MapPrototypeEdgeData> edges, int rows)
    {
        int firstMiddleRow = Mathf.Max(2, Mathf.FloorToInt(rows * 0.33f));
        int lastMiddleRow = Mathf.Min(rows - 1, Mathf.CeilToInt(rows * 0.67f));
        foreach (MapPrototypeEdgeData edge in edges)
        {
            string[] fromParts = edge.from.Split(':');
            string[] toParts = edge.to.Split(':');
            if (fromParts.Length != 2 || toParts.Length != 2)
                continue;
            if (!int.TryParse(fromParts[0], out int fromRow)
                || !int.TryParse(fromParts[1], out int fromColumn)
                || !int.TryParse(toParts[0], out int toRow)
                || !int.TryParse(toParts[1], out int toColumn))
                continue;
            if (fromRow < firstMiddleRow || fromRow > lastMiddleRow || toRow != fromRow + 1)
                continue;
            if (Mathf.Abs(toColumn - fromColumn) == 1)
                return true;
        }

        return false;
    }

    private static int CountKeyDegree(List<MapPrototypeEdgeData> edges, string key)
    {
        return edges.Count(edge => edge.from == key || edge.to == key);
    }

    private static bool WouldCrossRowEdge(List<MapPrototypeEdgeData> edges, int row, int candidateFrom, int candidateTo)
    {
        string fromPrefix = $"{row}:";
        string toPrefix = $"{row + 1}:";
        foreach (MapPrototypeEdgeData edge in edges)
        {
            if (!edge.from.StartsWith(fromPrefix, StringComparison.Ordinal)
                || !edge.to.StartsWith(toPrefix, StringComparison.Ordinal))
                continue;

            int existingFrom = int.Parse(edge.from.Substring(fromPrefix.Length));
            int existingTo = int.Parse(edge.to.Substring(toPrefix.Length));
            if (existingFrom == candidateFrom || existingTo == candidateTo)
                continue;
            if ((existingFrom < candidateFrom && existingTo > candidateTo)
                || (existingFrom > candidateFrom && existingTo < candidateTo))
                return true;
        }

        return false;
    }

    private static void AddKeyEdge(List<MapPrototypeEdgeData> edges, string from, string to)
    {
        if (edges.Any(edge => edge.from == from && edge.to == to))
            return;
        edges.Add(new MapPrototypeEdgeData { from = from, to = to });
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

    private static int ChooseTargetIntermediateCount(MapPrototypeConfig config)
    {
        int min = Mathf.Max(config.intermediateRows, config.minMainNodeCount);
        int max = Mathf.Max(min, config.maxMainNodeCount);
        float roll = UnityEngine.Random.value;
        if (roll < 0.35f && 27 >= min && 27 <= max)
            return 27;
        if (roll < 0.70f && 28 >= min && 28 <= max)
            return 28;
        return RandInt(min, max);
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

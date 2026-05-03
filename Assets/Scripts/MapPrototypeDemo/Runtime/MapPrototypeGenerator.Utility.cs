using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class MapPrototypeGenerator
{
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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class MapPrototypeController
{
    private void ComputeTravelOptions()
    {
        string originId = _currentNodeId;
        Dictionary<string, string> safePrev = new Dictionary<string, string> { [originId] = null };
        Queue<string> queue = new Queue<string>();
        queue.Enqueue(originId);

        while (queue.Count > 0)
        {
            string currentId = queue.Dequeue();
            if (!_map.adjacency.TryGetValue(currentId, out HashSet<string> neighbors))
                continue;

            foreach (string neighborId in neighbors)
            {
                if (safePrev.ContainsKey(neighborId))
                    continue;
                if (!IsTraversableNode(neighborId, originId))
                    continue;

                safePrev[neighborId] = currentId;
                queue.Enqueue(neighborId);
            }
        }

        _safeReachable = new HashSet<string>(safePrev.Keys);
        Dictionary<string, List<string>> travel = new Dictionary<string, List<string>>();

        foreach (string id in _safeReachable)
        {
            if (id == originId)
                continue;
            travel[id] = ReconstructPath(safePrev, id);
        }

        foreach (string safeId in _safeReachable)
        {
            List<string> basePath = safeId == originId ? new List<string> { originId } : ReconstructPath(safePrev, safeId);
            foreach (string neighborId in _map.adjacency[safeId])
            {
                if (_safeReachable.Contains(neighborId))
                    continue;

                List<string> candidatePath = new List<string>(basePath) { neighborId };
                if (!travel.TryGetValue(neighborId, out List<string> existing) || candidatePath.Count < existing.Count)
                    travel[neighborId] = candidatePath;
            }
        }

        _travelOptions = travel;
        _frontierIds = new HashSet<string>(_travelOptions.Keys.Where(id => !_safeReachable.Contains(id)));
    }

    private bool IsTraversableNode(string id, string originId)
    {
        MapPrototypeNodeData node = MapPrototypeGenerator.GetNodeById(_map, id);
        return id == originId || (node != null && node.safeVisited);
    }

    private static List<string> ReconstructPath(Dictionary<string, string> previousMap, string targetId)
    {
        List<string> path = new List<string>();
        string current = targetId;
        while (current != null)
        {
            path.Add(current);
            previousMap.TryGetValue(current, out current);
        }

        path.Reverse();
        return path;
    }

    private void HandleNodeClicked(string targetId)
    {
        if (_modalLocked || _isAnimating || _map == null)
        {
            LogMap($"Click ignored. modalLocked={_modalLocked}, isAnimating={_isAnimating}, hasMap={_map != null}.");
            return;
        }

        MapPrototypeNodeData current = GetCurrentNode();
        if (current == null)
            return;

        if (targetId == current.id)
        {
            LogMap($"Clicked current node {targetId}.");
            MaybeOpenEncounter(current);
            return;
        }

        if (!_travelOptions.TryGetValue(targetId, out List<string> path))
        {
            LogMap($"Clicked non-travelable node {targetId}.");
            return;
        }

        LogMap($"Clicked travelable node {targetId}. Path length={path.Count}.");
        StartCoroutine(MoveAlongPathRoutine(path));
    }

    private IEnumerator MoveAlongPathRoutine(List<string> path)
    {
        _isAnimating = true;
        CloseModal();
        LogMap($"MoveAlongPath started. Path={string.Join(" -> ", path)}");

        if (path.Count >= 2)
        {
            for (int i = 1; i < path.Count; i++)
            {
                MapPrototypeNodeData from = MapPrototypeGenerator.GetNodeById(_map, path[i - 1]);
                MapPrototypeNodeData to = MapPrototypeGenerator.GetNodeById(_map, path[i]);
                yield return TweenPlayerRoutine(from, to, 0.17f);
            }
        }

        _currentNodeId = path[path.Count - 1];
        MapPrototypeNodeData node = GetCurrentNode();
        _playerPos = new Vector2(node.x, node.y);
        node.visited = true;
        _isAnimating = false;
        ComputeTravelOptions();
        RenderAll();
        CenterOnCurrent(false);
        NotifyMapStateChanged();
        LogMap($"Arrived at node {node.id} ({node.type}).");
        MaybeOpenEncounter(node);
    }

    private IEnumerator TweenPlayerRoutine(MapPrototypeNodeData fromNode, MapPrototypeNodeData toNode, float duration)
    {
        if (fromNode == null || toNode == null)
            yield break;

        float elapsed = 0f;
        Vector2 from = new Vector2(fromNode.x, fromNode.y);
        Vector2 to = new Vector2(toNode.x, toNode.y);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
            _playerPos = Vector2.LerpUnclamped(from, to, ease);
            UpdatePlayerToken();
            yield return null;
        }

        _playerPos = to;
        UpdatePlayerToken();
    }

    private void CenterOnCurrent(bool instant)
    {
        if (mapScrollRect == null || mapContent == null || mapViewport == null)
            return;

        MapPrototypeNodeData current = GetCurrentNode();
        if (current == null)
            return;

        float targetTop = Mathf.Max(0f, current.y - mapViewport.rect.height * 0.62f);
        float maxTop = Mathf.Max(0f, mapContent.rect.height - mapViewport.rect.height);
        targetTop = Mathf.Clamp(targetTop, 0f, maxTop);

        if (instant)
        {
            Vector2 anchored = mapContent.anchoredPosition;
            anchored.y = targetTop;
            mapContent.anchoredPosition = anchored;
            return;
        }

        StopCoroutine(nameof(SmoothCenterRoutine));
        StartCoroutine(SmoothCenterRoutine(targetTop));
    }

    private IEnumerator SmoothCenterRoutine(float targetTop)
    {
        float duration = 0.18f;
        float elapsed = 0f;
        float start = mapContent.anchoredPosition.y;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
            Vector2 anchored = mapContent.anchoredPosition;
            anchored.y = Mathf.Lerp(start, targetTop, eased);
            mapContent.anchoredPosition = anchored;
            yield return null;
        }

        Vector2 final = mapContent.anchoredPosition;
        final.y = targetTop;
        mapContent.anchoredPosition = final;
    }

    private MapPrototypeNodeData GetCurrentNode()
    {
        return _map == null ? null : MapPrototypeGenerator.GetNodeById(_map, _currentNodeId);
    }

    private static Vector2 MapToUiPoint(float x, float y)
    {
        return new Vector2(x, -y);
    }
}

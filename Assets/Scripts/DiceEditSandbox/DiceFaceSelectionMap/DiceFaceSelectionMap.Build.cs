using System.Collections.Generic;
using UnityEngine;

// Builds logical dice face groups from readable mesh data or author-provided metadata.
public partial class DiceFaceSelectionMap
{
    // Rebuilds triangle-to-face maps and falls back when mesh data is unavailable.
    private void BuildFaceGroups()
    {
        _faceGroups.Clear();
        _triangleToGroup.Clear();
        _logicalToGroup.Clear();
        _usingFallbackGroups = false;

        if (BuildFaceGroupsFromMetadata())
            return;

        EnsureMeshSource();
        MeshFilter filter = _meshTransform != null ? _meshTransform.GetComponent<MeshFilter>() : null;
        if (filter == null)
        {
            BuildFallbackFaceGroups();
            return;
        }

        _mesh = filter.sharedMesh;
        if (_mesh == null || !_mesh.isReadable)
        {
            BuildFallbackFaceGroups();
            return;
        }

        Vector3[] vertices = _mesh.vertices;
        int[] triangles = _mesh.triangles;
        if (vertices == null || triangles == null || triangles.Length < 3)
        {
            BuildFallbackFaceGroups();
            return;
        }

        BuildNormalGroups(vertices, triangles);

        if (_spinner == null || _spinner.faces == null)
            return;

        if (_faceGroups.Count == 0)
        {
            BuildFallbackFaceGroups();
            return;
        }

        MapLogicalFacesToMeshGroups();
    }

    // Groups mesh triangles by local normal so each flat dice face can be selected as one unit.
    private void BuildNormalGroups(Vector3[] vertices, int[] triangles)
    {
        for (int tri = 0; tri < triangles.Length; tri += 3)
        {
            int i0 = triangles[tri];
            int i1 = triangles[tri + 1];
            int i2 = triangles[tri + 2];

            Vector3 a = vertices[i0];
            Vector3 b = vertices[i1];
            Vector3 c = vertices[i2];
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            if (normal.sqrMagnitude <= 0.0001f)
                continue;

            int groupIndex = FindBestNormalGroup(normal);
            if (groupIndex < 0)
            {
                groupIndex = _faceGroups.Count;
                _faceGroups.Add(new FaceGroup(normal));
            }

            FaceGroup group = _faceGroups[groupIndex];
            group.AddVertex(vertices[i0]);
            group.AddVertex(vertices[i1]);
            group.AddVertex(vertices[i2]);
            group.AddArea(Vector3.Cross(b - a, c - a).magnitude * 0.5f);
            group.AddTriangle(i0, i1, i2);
            _triangleToGroup[tri / 3] = groupIndex;
        }
    }

    // Maps authored logical face indices to the best matching mesh face group.
    private void MapLogicalFacesToMeshGroups()
    {
        _logicalToGroup.Clear();
        if (_spinner == null || _spinner.faces == null || _faceGroups.Count == 0)
            return;

        List<int> candidateGroups = BuildPrimaryFaceCandidateGroups(_spinner.faces.Length);
        HashSet<int> claimedGroups = new HashSet<int>();
        for (int faceIndex = 0; faceIndex < _spinner.faces.Length; faceIndex++)
        {
            Vector3 expectedNormal = GetExpectedFaceNormalInSpinnerLocal(faceIndex);
            float bestScore = float.NegativeInfinity;
            int bestGroup = -1;

            for (int i = 0; i < candidateGroups.Count; i++)
            {
                int groupIndex = candidateGroups[i];
                if (claimedGroups.Contains(groupIndex))
                    continue;

                Vector3 groupNormal = GetGroupNormalInSpinnerLocal(groupIndex);
                float score = Vector3.Dot(groupNormal, expectedNormal);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestGroup = groupIndex;
                }
            }

            if (bestGroup >= 0)
            {
                claimedGroups.Add(bestGroup);
                _logicalToGroup[faceIndex] = bestGroup;
            }
        }
    }

    // Builds face groups directly from metadata when a dice mesh is not reliable for selection.
    private bool BuildFaceGroupsFromMetadata()
    {
        if (_metadata == null || !_metadata.UseMetadataWhenAvailable || _spinner == null || _spinner.faces == null)
            return false;
        if (_metadata.FaceCount != _spinner.faces.Length)
            return false;

        _usingFallbackGroups = true;
        Bounds bounds = GetApproximateBounds();
        float extent = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z));
        _fallbackFaceRadius = Mathf.Max(0.05f, extent);

        for (int faceIndex = 0; faceIndex < _spinner.faces.Length; faceIndex++)
        {
            DiceFaceHighlightMetadata.FacePresentation face = _metadata.GetFace(faceIndex);
            FaceGroup group = new FaceGroup(
                face.localNormal.sqrMagnitude > 0f ? face.localNormal.normalized : Vector3.back,
                face.localCenter,
                Mathf.Max(3, face.polygonSides),
                Mathf.Max(0.001f, face.polygonRadius),
                face.rotationOffsetDeg);
            _faceGroups.Add(group);
            _logicalToGroup[faceIndex] = faceIndex;
        }

        return true;
    }

    // Finds or creates the readable mesh/collider source used for raycast face picking.
    private void EnsureMeshSource()
    {
        if (_meshTransform == null)
        {
            MeshFilter filter = GetComponentInChildren<MeshFilter>(includeInactive: true);
            if (filter != null)
                _meshTransform = filter.transform;
        }

        if (_meshTransform == null)
            return;

        MeshFilter meshFilter = _meshTransform.GetComponent<MeshFilter>();
        _mesh = meshFilter != null ? meshFilter.sharedMesh : null;
        if (_mesh == null)
            return;

        _meshCollider = _meshTransform.GetComponent<MeshCollider>();
        if (_meshCollider == null)
            _meshCollider = _meshTransform.gameObject.AddComponent<MeshCollider>();

        _meshCollider.sharedMesh = null;
        _meshCollider.sharedMesh = _mesh;
        _meshCollider.convex = false;
    }

    // Guesses the logical face that best matches a hit mesh group.
    private int GuessLogicalFaceFromGroup(int groupIndex, Camera cam)
    {
        if (_spinner == null || _spinner.faces == null || groupIndex < 0 || groupIndex >= _faceGroups.Count)
            return -1;

        Vector3 groupNormal = GetGroupNormalInSpinnerLocal(groupIndex);
        float bestScore = float.NegativeInfinity;
        int bestFace = -1;

        for (int faceIndex = 0; faceIndex < _spinner.faces.Length; faceIndex++)
        {
            Vector3 expectedNormal = GetExpectedFaceNormalInSpinnerLocal(faceIndex);
            float score = Vector3.Dot(groupNormal, expectedNormal);
            if (score > bestScore)
            {
                bestScore = score;
                bestFace = faceIndex;
            }
        }

        return bestFace;
    }

    // Finds the existing normal group that should receive a triangle.
    private int FindBestNormalGroup(Vector3 normal)
    {
        float bestDot = NormalGroupDotThreshold;
        int bestGroup = -1;

        for (int i = 0; i < _faceGroups.Count; i++)
        {
            float dot = Vector3.Dot(_faceGroups[i].localNormal, normal);
            if (dot <= bestDot)
                continue;

            bestDot = dot;
            bestGroup = i;
        }

        return bestGroup;
    }

    // Keeps the largest face candidates so bevels or small mesh fragments are ignored.
    private List<int> BuildPrimaryFaceCandidateGroups(int desiredCount)
    {
        List<int> candidates = new List<int>(_faceGroups.Count);
        for (int i = 0; i < _faceGroups.Count; i++)
            candidates.Add(i);

        candidates.Sort((a, b) => _faceGroups[b].area.CompareTo(_faceGroups[a].area));

        if (desiredCount > 0 && candidates.Count > desiredCount)
            candidates.RemoveRange(desiredCount, candidates.Count - desiredCount);

        return candidates;
    }
}

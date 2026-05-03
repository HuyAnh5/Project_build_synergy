using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class DiceFaceSelectionMap : MonoBehaviour
{
    private const float NormalGroupDotThreshold = 0.995f;

    private readonly List<FaceGroup> _faceGroups = new List<FaceGroup>();
    private readonly Dictionary<int, int> _triangleToGroup = new Dictionary<int, int>();
    private readonly Dictionary<int, int> _logicalToGroup = new Dictionary<int, int>();

    private DiceSpinnerGeneric _spinner;
    private DiceFaceHighlightMetadata _metadata;
    private Mesh _mesh;
    private Transform _meshTransform;
    private MeshCollider _meshCollider;
    private bool _isConfigured;
    private bool _usingFallbackGroups;
    private float _fallbackFaceRadius = 0.35f;

    public Transform MeshTransform => _meshTransform;
    public Transform HighlightTransform => _usingFallbackGroups ? transform : (_meshTransform != null ? _meshTransform : transform);
    public bool IsConfiguredFor(DiceSpinnerGeneric spinner) => _isConfigured && _spinner == spinner;

    public void Configure(DiceSpinnerGeneric spinner)
    {
        if (_isConfigured && _spinner == spinner)
            return;

        _spinner = spinner;
        _metadata = spinner != null ? spinner.GetComponent<DiceFaceHighlightMetadata>() : null;
        EnsureMeshSource();
        BuildFaceGroups();
        _isConfigured = true;
    }

    public bool TryResolveLogicalFace(RaycastHit hit, Camera cam, out int logicalFaceIndex)
    {
        logicalFaceIndex = -1;
        if (hit.collider == null || _meshTransform == null)
            return false;

        if (!hit.collider.transform.IsChildOf(transform))
            return false;

        if (!_triangleToGroup.ContainsKey(hit.triangleIndex))
        {
            Debug.Log($"[DiceEditSelect] Triangle {hit.triangleIndex} on '{hit.collider.name}' was not mapped to any face group.");
            return false;
        }

        logicalFaceIndex = ResolveLogicalFaceIndex(hit.triangleIndex, cam);
        if (logicalFaceIndex >= 0)
            Debug.Log($"[DiceEditSelect] Resolved triangle {hit.triangleIndex} -> logicalFaceIndex={logicalFaceIndex}.");
        return logicalFaceIndex >= 0;
    }

    public int ResolveLogicalFaceIndex(int triangleIndex, Camera cam)
    {
        if (!_triangleToGroup.TryGetValue(triangleIndex, out int groupIndex))
            return -1;

        foreach (KeyValuePair<int, int> pair in _logicalToGroup)
        {
            if (pair.Value == groupIndex)
                return pair.Key;
        }

        return GuessLogicalFaceFromGroup(groupIndex, cam);
    }

    public Vector3 GetLocalFaceNormal(int logicalFaceIndex)
    {
        if (!_logicalToGroup.TryGetValue(logicalFaceIndex, out int groupIndex))
            return Vector3.forward;

        if (groupIndex < 0 || groupIndex >= _faceGroups.Count)
            return Vector3.forward;

        return _faceGroups[groupIndex].localNormal;
    }

    public int GetBestFacingLogicalFace(Transform pivot, Camera cam)
    {
        if (_spinner == null || _spinner.faces == null || pivot == null)
            return -1;

        float bestScore = float.NegativeInfinity;
        int bestFace = -1;

        for (int faceIndex = 0; faceIndex < _spinner.faces.Length; faceIndex++)
        {
            float score = GetFacingScore(faceIndex, pivot, cam);
            if (score > bestScore)
            {
                bestScore = score;
                bestFace = faceIndex;
            }
        }

        return bestFace;
    }

    public bool TryGetNearestVisibleLogicalFace(Vector2 screenPosition, Camera cam, out int logicalFaceIndex, float maxScreenDistance = 120f, float minFacingScore = 0.15f)
    {
        logicalFaceIndex = -1;
        if (_spinner == null || _spinner.faces == null || cam == null || _meshTransform == null)
            return false;

        float bestDistanceSqr = maxScreenDistance * maxScreenDistance;
        Transform pivot = _spinner.pivot != null ? _spinner.pivot : _spinner.transform;

        for (int faceIndex = 0; faceIndex < _spinner.faces.Length; faceIndex++)
        {
            float facingScore = GetFacingScore(faceIndex, pivot, cam);
            if (facingScore < minFacingScore)
                continue;

            Vector3 worldCenter = GetWorldFaceCenter(faceIndex);
            Vector3 screenPoint = cam.WorldToScreenPoint(worldCenter);
            if (screenPoint.z <= 0f)
                continue;

            Vector2 delta = (Vector2)screenPoint - screenPosition;
            float distanceSqr = delta.sqrMagnitude;
            if (distanceSqr >= bestDistanceSqr)
                continue;

            bestDistanceSqr = distanceSqr;
            logicalFaceIndex = faceIndex;
        }

        return logicalFaceIndex >= 0;
    }

    public float GetFacingScore(int logicalFaceIndex, Transform pivot, Camera cam)
    {
        if (pivot == null)
            return float.NegativeInfinity;

        Vector3 desiredNormal = cam != null ? -cam.transform.forward : Vector3.forward;
        if (!_logicalToGroup.TryGetValue(logicalFaceIndex, out int groupIndex))
            return float.NegativeInfinity;

        return Vector3.Dot(GetGroupWorldNormal(groupIndex), desiredNormal);
    }

    public Vector3 GetWorldFaceCenter(int logicalFaceIndex)
    {
        if (!_logicalToGroup.TryGetValue(logicalFaceIndex, out int groupIndex))
            return _meshTransform != null ? _meshTransform.position : transform.position;

        if (groupIndex < 0 || groupIndex >= _faceGroups.Count)
            return _meshTransform != null ? _meshTransform.position : transform.position;

        Vector3 localCenter = _faceGroups[groupIndex].localCenter;
        Transform referenceTransform = _usingFallbackGroups ? transform : (_meshTransform != null ? _meshTransform : transform);
        return referenceTransform != null ? referenceTransform.TransformPoint(localCenter) : transform.TransformPoint(localCenter);
    }

    public Mesh BuildHighlightMesh(int logicalFaceIndex)
    {
        if (!_logicalToGroup.TryGetValue(logicalFaceIndex, out int groupIndex))
            return null;

        if (groupIndex < 0 || groupIndex >= _faceGroups.Count)
            return null;

        if (_usingFallbackGroups || _mesh == null || !_mesh.isReadable)
            return BuildFallbackHighlightMesh(groupIndex, logicalFaceIndex);

        FaceGroup group = _faceGroups[groupIndex];
        if (group.triangleIndices == null || group.triangleIndices.Length == 0)
            return null;

        Vector3[] sourceVertices = _mesh.vertices;
        Vector3[] sourceNormals = _mesh.normals;
        Vector2[] sourceUv = _mesh.uv;

        Vector3[] vertices = new Vector3[group.vertexIndices.Length];
        Vector3[] normals = new Vector3[group.vertexIndices.Length];
        Vector2[] uv = new Vector2[group.vertexIndices.Length];
        Dictionary<int, int> remap = new Dictionary<int, int>(group.vertexIndices.Length);

        for (int i = 0; i < group.vertexIndices.Length; i++)
        {
            int sourceIndex = group.vertexIndices[i];
            remap[sourceIndex] = i;
            vertices[i] = sourceVertices[sourceIndex];
            normals[i] = sourceNormals != null && sourceNormals.Length == sourceVertices.Length ? sourceNormals[sourceIndex] : group.localNormal;
            uv[i] = sourceUv != null && sourceUv.Length == sourceVertices.Length ? sourceUv[sourceIndex] : Vector2.zero;
        }

        int[] triangles = new int[group.triangleIndices.Length];
        for (int i = 0; i < group.triangleIndices.Length; i++)
            triangles[i] = remap[group.triangleIndices[i]];

        Mesh mesh = new Mesh();
        mesh.name = $"{name}_Face_{logicalFaceIndex}_Highlight";
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

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
        if (_mesh == null)
        {
            BuildFallbackFaceGroups();
            return;
        }
        if (!_mesh.isReadable)
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

        if (_spinner == null || _spinner.faces == null)
            return;

        if (_faceGroups.Count == 0)
        {
            BuildFallbackFaceGroups();
            return;
        }

        MapLogicalFacesToMeshGroups();
    }

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

    private void BuildFallbackFaceGroups()
    {
        _usingFallbackGroups = true;
        _faceGroups.Clear();
        _triangleToGroup.Clear();
        _logicalToGroup.Clear();

        if (_spinner == null || _spinner.faces == null || _spinner.faces.Length == 0)
            return;

        Bounds bounds = GetApproximateBounds();
        float extent = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z));
        _fallbackFaceRadius = Mathf.Max(0.05f, extent);

        for (int faceIndex = 0; faceIndex < _spinner.faces.Length; faceIndex++)
        {
            Quaternion inverseRotation = Quaternion.Inverse(Quaternion.Euler(_spinner.faces[faceIndex].localEuler));
            Vector3 localNormal = inverseRotation * Vector3.back;
            Vector3 localCenter = inverseRotation * (Vector3.back * GetFallbackFaceDepth());
            FaceGroup group = new FaceGroup(
                localNormal.normalized,
                localCenter,
                GetFallbackPolygonSides(),
                GetFallbackPolygonRadius(),
                GetFallbackRotationOffset());
            _faceGroups.Add(group);
            _logicalToGroup[faceIndex] = faceIndex;
        }
    }

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

    private Vector3 GetExpectedFaceNormalInSpinnerLocal(int logicalFaceIndex)
    {
        if (_spinner == null || _spinner.faces == null || logicalFaceIndex < 0 || logicalFaceIndex >= _spinner.faces.Length)
            return Vector3.back;

        Quaternion faceRotation = Quaternion.Euler(_spinner.faces[logicalFaceIndex].localEuler);
        return (Quaternion.Inverse(faceRotation) * Vector3.back).normalized;
    }

    private Vector3 GetGroupNormalInSpinnerLocal(int groupIndex)
    {
        if (groupIndex < 0 || groupIndex >= _faceGroups.Count)
            return Vector3.forward;

        if (_usingFallbackGroups || _meshTransform == null)
            return _faceGroups[groupIndex].localNormal.normalized;

        Transform reference = GetSpinnerReferenceTransform();
        Vector3 worldNormal = _meshTransform.TransformDirection(_faceGroups[groupIndex].localNormal);
        return reference != null
            ? reference.InverseTransformDirection(worldNormal).normalized
            : worldNormal.normalized;
    }

    private Vector3 GetGroupWorldNormal(int groupIndex)
    {
        if (groupIndex < 0 || groupIndex >= _faceGroups.Count)
            return Vector3.forward;

        if (_usingFallbackGroups)
        {
            Transform reference = GetSpinnerReferenceTransform();
            return reference != null
                ? reference.TransformDirection(_faceGroups[groupIndex].localNormal).normalized
                : transform.TransformDirection(_faceGroups[groupIndex].localNormal).normalized;
        }

        return _meshTransform != null
            ? _meshTransform.TransformDirection(_faceGroups[groupIndex].localNormal).normalized
            : transform.TransformDirection(_faceGroups[groupIndex].localNormal).normalized;
    }

    private Transform GetSpinnerReferenceTransform()
    {
        if (_spinner == null)
            return transform;

        return _spinner.pivot != null ? _spinner.pivot : _spinner.transform;
    }

    private Bounds GetApproximateBounds()
    {
        if (_mesh != null)
            return _mesh.bounds;

        Renderer renderer = _meshTransform != null ? _meshTransform.GetComponent<Renderer>() : null;
        if (renderer != null)
        {
            Bounds worldBounds = renderer.bounds;
            Vector3 localCenter = _meshTransform != null ? _meshTransform.InverseTransformPoint(worldBounds.center) : worldBounds.center;
            Vector3 localSize = _meshTransform != null ? Vector3.Scale(worldBounds.size, new Vector3(
                SafeInverse(_meshTransform.lossyScale.x),
                SafeInverse(_meshTransform.lossyScale.y),
                SafeInverse(_meshTransform.lossyScale.z))) : worldBounds.size;
            return new Bounds(localCenter, localSize);
        }

        return new Bounds(Vector3.zero, Vector3.one);
    }

    private Mesh BuildFallbackHighlightMesh(int groupIndex, int logicalFaceIndex)
    {
        if (groupIndex < 0 || groupIndex >= _faceGroups.Count)
            return null;

        if (_spinner == null || _spinner.faces == null || logicalFaceIndex < 0 || logicalFaceIndex >= _spinner.faces.Length)
            return null;

        FaceGroup group = _faceGroups[groupIndex];
        Vector3 normal = group.localNormal.normalized;
        Vector3 tangent = Vector3.Cross(normal, Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.9f ? Vector3.right : Vector3.up).normalized;
        if (tangent.sqrMagnitude < 0.0001f)
            tangent = Vector3.right;
        Vector3 bitangent = Vector3.Cross(normal, tangent).normalized;
        Vector3 center = group.localCenter;
        int sides = Mathf.Max(3, group.polygonSides);
        float radius = Mathf.Max(0.001f, group.polygonRadius);

        Vector3[] vertices = new Vector3[sides];
        Vector3[] normals = new Vector3[sides];
        Vector2[] uv = new Vector2[sides];
        float startAngle = group.rotationOffsetDeg;

        for (int i = 0; i < sides; i++)
        {
            float angle = Mathf.Deg2Rad * (startAngle + (360f / sides) * i);
            Vector3 offset = tangent * Mathf.Cos(angle) * radius + bitangent * Mathf.Sin(angle) * radius;
            vertices[i] = center + offset;
            normals[i] = normal;
            uv[i] = new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f);
        }

        int triangleCount = (sides - 2) * 2;
        int[] triangles = new int[triangleCount * 3];
        int cursor = 0;
        for (int i = 1; i < sides - 1; i++)
        {
            triangles[cursor++] = 0;
            triangles[cursor++] = i;
            triangles[cursor++] = i + 1;
        }

        for (int i = 1; i < sides - 1; i++)
        {
            triangles[cursor++] = 0;
            triangles[cursor++] = i + 1;
            triangles[cursor++] = i;
        }

        Mesh mesh = new Mesh();
        mesh.name = $"{name}_FallbackFace_{logicalFaceIndex}_Highlight";
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    private int GetFallbackPolygonSides()
    {
        int faceCount = _spinner != null && _spinner.faces != null ? _spinner.faces.Length : 0;
        switch (faceCount)
        {
            case 4:
            case 8:
            case 20:
                return 3;
            case 6:
                return 4;
            case 12:
                return 5;
            default:
                return 4;
        }
    }

    private float GetFallbackRotationOffset()
    {
        int sides = GetFallbackPolygonSides();
        switch (sides)
        {
            case 3:
                return -90f;
            case 4:
                return 45f;
            default:
                return 90f;
        }
    }

    private float GetFallbackFaceDepth()
    {
        int faceCount = _spinner != null && _spinner.faces != null ? _spinner.faces.Length : 0;
        switch (faceCount)
        {
            case 4:
                return _fallbackFaceRadius * 0.33f;
            case 6:
                return _fallbackFaceRadius * 0.95f;
            case 8:
                return _fallbackFaceRadius * 0.58f;
            case 12:
            case 20:
                return _fallbackFaceRadius * 0.8f;
            default:
                return _fallbackFaceRadius * 0.6f;
        }
    }

    private float GetFallbackPolygonRadius()
    {
        int faceCount = _spinner != null && _spinner.faces != null ? _spinner.faces.Length : 0;
        switch (faceCount)
        {
            case 4:
                return _fallbackFaceRadius * 0.42f;
            case 6:
                return _fallbackFaceRadius * 0.56f;
            case 8:
                return _fallbackFaceRadius * 0.36f;
            case 12:
                return _fallbackFaceRadius * 0.34f;
            case 20:
                return _fallbackFaceRadius * 0.24f;
            default:
                return _fallbackFaceRadius * 0.35f;
        }
    }

    private static float SafeInverse(float value)
    {
        return Mathf.Approximately(value, 0f) ? 1f : 1f / value;
    }

    private sealed class FaceGroup
    {
        private readonly List<int> _triangleIndices = new List<int>();
        private readonly HashSet<int> _vertexIndexSet = new HashSet<int>();
        private int[] _cachedVertexIndices;
        private Vector3 _vertexSum;
        private int _vertexCount;
        private float _area;

        public Vector3 localNormal { get; }
        public float area => _area;
        private readonly bool _hasManualCenter;
        private readonly Vector3 _manualCenter;
        public int polygonSides { get; }
        public float polygonRadius { get; }
        public float rotationOffsetDeg { get; }
        public Vector3 localCenter => _hasManualCenter ? _manualCenter : (_vertexCount > 0 ? _vertexSum / _vertexCount : Vector3.zero);
        public int[] triangleIndices => _triangleIndices.ToArray();
        public int[] vertexIndices
        {
            get
            {
                if (_cachedVertexIndices == null)
                {
                    _cachedVertexIndices = new int[_vertexIndexSet.Count];
                    _vertexIndexSet.CopyTo(_cachedVertexIndices);
                }

                return _cachedVertexIndices;
            }
        }

        public FaceGroup(Vector3 localNormal)
        {
            this.localNormal = localNormal.normalized;
            polygonSides = 4;
            polygonRadius = 0.1f;
            rotationOffsetDeg = 45f;
        }

        public FaceGroup(Vector3 localNormal, Vector3 localCenter, int polygonSides, float polygonRadius, float rotationOffsetDeg)
        {
            this.localNormal = localNormal.normalized;
            _manualCenter = localCenter;
            _hasManualCenter = true;
            this.polygonSides = Mathf.Max(3, polygonSides);
            this.polygonRadius = Mathf.Max(0.001f, polygonRadius);
            this.rotationOffsetDeg = rotationOffsetDeg;
        }

        public void AddTriangle(int i0, int i1, int i2)
        {
            _triangleIndices.Add(i0);
            _triangleIndices.Add(i1);
            _triangleIndices.Add(i2);
            _vertexIndexSet.Add(i0);
            _vertexIndexSet.Add(i1);
            _vertexIndexSet.Add(i2);
            _cachedVertexIndices = null;
        }

        public void AddArea(float area)
        {
            _area += Mathf.Max(0f, area);
        }

        public void AddVertex(Vector3 vertex)
        {
            _vertexSum += vertex;
            _vertexCount++;
        }
    }
}

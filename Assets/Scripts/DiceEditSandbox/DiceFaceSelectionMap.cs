using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class DiceFaceSelectionMap : MonoBehaviour
{
    private readonly List<FaceGroup> _faceGroups = new List<FaceGroup>();
    private readonly Dictionary<int, int> _triangleToGroup = new Dictionary<int, int>();
    private readonly Dictionary<int, int> _logicalToGroup = new Dictionary<int, int>();

    private DiceSpinnerGeneric _spinner;
    private Mesh _mesh;
    private Transform _meshTransform;
    private MeshCollider _meshCollider;
    private bool _isConfigured;

    public Transform MeshTransform => _meshTransform;
    public bool IsConfiguredFor(DiceSpinnerGeneric spinner) => _isConfigured && _spinner == spinner;

    public void Configure(DiceSpinnerGeneric spinner)
    {
        if (_isConfigured && _spinner == spinner)
            return;

        _spinner = spinner;
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

    public float GetFacingScore(int logicalFaceIndex, Transform pivot, Camera cam)
    {
        if (pivot == null)
            return float.NegativeInfinity;

        Vector3 desiredNormal = cam != null ? -cam.transform.forward : Vector3.forward;
        Vector3 localNormal = GetLocalFaceNormal(logicalFaceIndex);
        Vector3 worldNormal = pivot.rotation * localNormal;
        return Vector3.Dot(worldNormal, desiredNormal);
    }

    public Mesh BuildHighlightMesh(int logicalFaceIndex)
    {
        if (!_logicalToGroup.TryGetValue(logicalFaceIndex, out int groupIndex))
            return null;

        if (groupIndex < 0 || groupIndex >= _faceGroups.Count || _mesh == null)
            return null;

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

        EnsureMeshSource();
        MeshFilter filter = _meshTransform != null ? _meshTransform.GetComponent<MeshFilter>() : null;
        if (filter == null)
            return;

        _mesh = filter.sharedMesh;
        if (_mesh == null)
            return;

        Vector3[] vertices = _mesh.vertices;
        int[] triangles = _mesh.triangles;
        if (vertices == null || triangles == null || triangles.Length < 3)
            return;

        Dictionary<string, int> normalKeyToGroup = new Dictionary<string, int>();

        for (int tri = 0; tri < triangles.Length; tri += 3)
        {
            int i0 = triangles[tri];
            int i1 = triangles[tri + 1];
            int i2 = triangles[tri + 2];

            Vector3 a = vertices[i0];
            Vector3 b = vertices[i1];
            Vector3 c = vertices[i2];
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            string key = QuantizeNormalKey(normal);

            if (!normalKeyToGroup.TryGetValue(key, out int groupIndex))
            {
                groupIndex = _faceGroups.Count;
                normalKeyToGroup[key] = groupIndex;
                _faceGroups.Add(new FaceGroup(normal));
            }

            FaceGroup group = _faceGroups[groupIndex];
            group.AddTriangle(i0, i1, i2);
            _triangleToGroup[tri / 3] = groupIndex;
        }

        Camera cam = Camera.main;
        if (_spinner == null || _spinner.faces == null)
            return;

        HashSet<int> claimedGroups = new HashSet<int>();
        Vector3 desiredNormal = cam != null ? -cam.transform.forward : Vector3.back;

        for (int faceIndex = 0; faceIndex < _spinner.faces.Length; faceIndex++)
        {
            Quaternion rotation = Quaternion.Euler(_spinner.faces[faceIndex].localEuler);
            float bestScore = float.NegativeInfinity;
            int bestGroup = -1;

            for (int groupIndex = 0; groupIndex < _faceGroups.Count; groupIndex++)
            {
                if (claimedGroups.Contains(groupIndex))
                    continue;

                Vector3 rotated = rotation * _faceGroups[groupIndex].localNormal;
                float score = Vector3.Dot(rotated, desiredNormal);
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

        Vector3 desiredNormal = cam != null ? -cam.transform.forward : Vector3.back;
        float bestScore = float.NegativeInfinity;
        int bestFace = -1;

        for (int faceIndex = 0; faceIndex < _spinner.faces.Length; faceIndex++)
        {
            Quaternion rotation = Quaternion.Euler(_spinner.faces[faceIndex].localEuler);
            Vector3 rotated = rotation * _faceGroups[groupIndex].localNormal;
            float score = Vector3.Dot(rotated, desiredNormal);
            if (score > bestScore)
            {
                bestScore = score;
                bestFace = faceIndex;
            }
        }

        return bestFace;
    }

    private static string QuantizeNormalKey(Vector3 normal)
    {
        Vector3 n = normal.normalized;
        return $"{Mathf.RoundToInt(n.x * 1000f)}_{Mathf.RoundToInt(n.y * 1000f)}_{Mathf.RoundToInt(n.z * 1000f)}";
    }

    private sealed class FaceGroup
    {
        private readonly List<int> _triangleIndices = new List<int>();
        private readonly HashSet<int> _vertexIndexSet = new HashSet<int>();
        private int[] _cachedVertexIndices;

        public Vector3 localNormal { get; }
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
    }
}

using System.Collections.Generic;
using UnityEngine;

// Maps dice mesh triangles to logical face indices so dice edit UI can select and highlight faces.
[DisallowMultipleComponent]
public partial class DiceFaceSelectionMap : MonoBehaviour
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

    // Binds this selection map to a dice spinner and rebuilds face lookup data.
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

    // Resolves a physics raycast hit into the dice face index used by gameplay data.
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

    // Converts a mesh triangle index to a logical face, falling back to normal matching if needed.
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

    // Returns the local normal used for highlight placement on a logical face.
    public Vector3 GetLocalFaceNormal(int logicalFaceIndex)
    {
        if (!_logicalToGroup.TryGetValue(logicalFaceIndex, out int groupIndex))
            return Vector3.forward;

        if (groupIndex < 0 || groupIndex >= _faceGroups.Count)
            return Vector3.forward;

        return _faceGroups[groupIndex].localNormal;
    }

    // Finds the logical face currently facing the camera most directly.
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

    // Finds the closest visible face to a screen position when triangle raycast selection is ambiguous.
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

    // Scores how much a face normal points toward the camera.
    public float GetFacingScore(int logicalFaceIndex, Transform pivot, Camera cam)
    {
        if (pivot == null)
            return float.NegativeInfinity;

        Vector3 desiredNormal = cam != null ? -cam.transform.forward : Vector3.forward;
        if (!_logicalToGroup.TryGetValue(logicalFaceIndex, out int groupIndex))
            return float.NegativeInfinity;

        return Vector3.Dot(GetGroupWorldNormal(groupIndex), desiredNormal);
    }

    // Returns the world-space center used for screen-distance picking.
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

    // Builds a standalone mesh overlay for the selected logical face.
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

}

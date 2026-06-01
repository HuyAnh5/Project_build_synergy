using UnityEngine;

// Provides coordinate conversion and fallback mesh generation for dice face highlights.
public partial class DiceFaceSelectionMap
{
    // Converts an authored logical face rotation into spinner-local normal direction.
    private Vector3 GetExpectedFaceNormalInSpinnerLocal(int logicalFaceIndex)
    {
        if (_spinner == null || _spinner.faces == null || logicalFaceIndex < 0 || logicalFaceIndex >= _spinner.faces.Length)
            return Vector3.back;

        Quaternion faceRotation = Quaternion.Euler(_spinner.faces[logicalFaceIndex].localEuler);
        return (Quaternion.Inverse(faceRotation) * Vector3.back).normalized;
    }

    // Converts a mesh group normal into spinner-local space.
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

    // Converts a face group normal into world space for camera-facing scoring.
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

    // Returns the transform that owns dice face rotations.
    private Transform GetSpinnerReferenceTransform()
    {
        if (_spinner == null)
            return transform;

        return _spinner.pivot != null ? _spinner.pivot : _spinner.transform;
    }

    // Estimates local dice bounds from mesh or renderer data for fallback highlights.
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

    // Builds a simple polygon highlight when the real mesh cannot be used.
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

    // Avoids division by zero when converting world renderer bounds to local size.
    private static float SafeInverse(float value)
    {
        return Mathf.Approximately(value, 0f) ? 1f : 1f / value;
    }
}

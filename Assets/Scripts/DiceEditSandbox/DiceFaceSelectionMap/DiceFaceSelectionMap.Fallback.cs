using UnityEngine;

// Creates approximate selectable face groups when dice mesh triangles are unavailable.
public partial class DiceFaceSelectionMap
{
    // Builds logical face polygons from authored face rotations and dice bounds.
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

    // Picks an approximate polygon side count based on common tabletop dice shapes.
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

    // Rotates fallback polygons into a readable default orientation.
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

    // Estimates the distance from dice center to each fallback face.
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

    // Estimates the visible fallback polygon radius for each dice shape.
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
}

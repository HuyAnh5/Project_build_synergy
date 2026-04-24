using System;
using UnityEngine;

[DisallowMultipleComponent]
public class DiceFaceHighlightMetadata : MonoBehaviour
{
    [Serializable]
    public struct FacePresentation
    {
        public Vector3 localCenter;
        public Vector3 localNormal;
        [Min(3)] public int polygonSides;
        [Min(0.001f)] public float polygonRadius;
        public float rotationOffsetDeg;
    }

    [SerializeField] private bool useMetadataWhenAvailable = true;
    [SerializeField] private FacePresentation[] faces = Array.Empty<FacePresentation>();

    public bool UseMetadataWhenAvailable => useMetadataWhenAvailable;
    public int FaceCount => faces != null ? faces.Length : 0;

    public bool HasFace(int logicalFaceIndex)
    {
        return faces != null && logicalFaceIndex >= 0 && logicalFaceIndex < faces.Length;
    }

    public FacePresentation GetFace(int logicalFaceIndex)
    {
        if (!HasFace(logicalFaceIndex))
            return default;

        return faces[logicalFaceIndex];
    }

    public void SetFaces(FacePresentation[] entries)
    {
        faces = entries ?? Array.Empty<FacePresentation>();
    }
}

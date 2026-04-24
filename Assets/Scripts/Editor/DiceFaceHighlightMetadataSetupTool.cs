using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DiceFaceHighlightMetadataSetupTool
{
    [MenuItem("Tools/Build Synergy/Generate Dice Face Highlight Metadata/For Selected Dice")]
    public static void GenerateForSelectedDice()
    {
        DiceSpinnerGeneric[] dice = Selection.GetFiltered<DiceSpinnerGeneric>(SelectionMode.Editable | SelectionMode.ExcludePrefab);
        GenerateMetadata(dice);
    }

    [MenuItem("Tools/Build Synergy/Generate Dice Face Highlight Metadata/For All Dice In Scene")]
    public static void GenerateForSceneDice()
    {
        DiceSpinnerGeneric[] dice = Object.FindObjectsByType<DiceSpinnerGeneric>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        GenerateMetadata(dice);
    }

    private static void GenerateMetadata(IReadOnlyList<DiceSpinnerGeneric> dice)
    {
        if (dice == null || dice.Count == 0)
        {
            Debug.LogWarning("[DiceFaceHighlightMetadataSetup] No dice found to generate metadata for.");
            return;
        }

        int updated = 0;
        for (int i = 0; i < dice.Count; i++)
        {
            DiceSpinnerGeneric die = dice[i];
            if (die == null || die.faces == null || die.faces.Length == 0)
                continue;

            DiceFaceHighlightMetadata metadata = die.GetComponent<DiceFaceHighlightMetadata>();
            if (metadata == null)
                metadata = Undo.AddComponent<DiceFaceHighlightMetadata>(die.gameObject);

            Undo.RecordObject(metadata, "Generate Dice Face Highlight Metadata");
            metadata.SetFaces(BuildEntries(die));
            EditorUtility.SetDirty(metadata);
            updated++;
        }

        if (updated > 0)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"[DiceFaceHighlightMetadataSetup] Generated metadata for {updated} dice.");
    }

    private static DiceFaceHighlightMetadata.FacePresentation[] BuildEntries(DiceSpinnerGeneric die)
    {
        int faceCount = die.faces.Length;
        Bounds bounds = GetApproximateBounds(die);
        float extent = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z));
        float depth = GetDepth(faceCount, extent);
        float radius = GetRadius(faceCount, extent);
        int sides = GetPolygonSides(faceCount);
        float rotationOffset = GetRotationOffset(sides);

        DiceFaceHighlightMetadata.FacePresentation[] entries = new DiceFaceHighlightMetadata.FacePresentation[faceCount];
        for (int faceIndex = 0; faceIndex < faceCount; faceIndex++)
        {
            Quaternion inverseRotation = Quaternion.Inverse(Quaternion.Euler(die.faces[faceIndex].localEuler));
            entries[faceIndex] = new DiceFaceHighlightMetadata.FacePresentation
            {
                localNormal = (inverseRotation * Vector3.back).normalized,
                localCenter = inverseRotation * (Vector3.back * depth),
                polygonSides = sides,
                polygonRadius = radius,
                rotationOffsetDeg = rotationOffset
            };
        }

        return entries;
    }

    private static Bounds GetApproximateBounds(DiceSpinnerGeneric die)
    {
        MeshFilter filter = die.GetComponentInChildren<MeshFilter>(true);
        if (filter != null && filter.sharedMesh != null)
            return filter.sharedMesh.bounds;

        Renderer renderer = die.GetComponentInChildren<Renderer>(true);
        if (renderer != null)
        {
            Bounds worldBounds = renderer.bounds;
            Transform t = renderer.transform;
            Vector3 localCenter = t.InverseTransformPoint(worldBounds.center);
            Vector3 localSize = Vector3.Scale(
                worldBounds.size,
                new Vector3(SafeInverse(t.lossyScale.x), SafeInverse(t.lossyScale.y), SafeInverse(t.lossyScale.z)));
            return new Bounds(localCenter, localSize);
        }

        return new Bounds(Vector3.zero, Vector3.one);
    }

    private static int GetPolygonSides(int faceCount)
    {
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

    private static float GetDepth(int faceCount, float extent)
    {
        float safeExtent = Mathf.Max(0.05f, extent);
        switch (faceCount)
        {
            case 4:
                return safeExtent * 0.33f;
            case 6:
                return safeExtent * 0.95f;
            case 8:
                return safeExtent * 0.58f;
            case 12:
            case 20:
                return safeExtent * 0.8f;
            default:
                return safeExtent * 0.6f;
        }
    }

    private static float GetRadius(int faceCount, float extent)
    {
        float safeExtent = Mathf.Max(0.05f, extent);
        switch (faceCount)
        {
            case 4:
                return safeExtent * 0.42f;
            case 6:
                return safeExtent * 0.56f;
            case 8:
                return safeExtent * 0.36f;
            case 12:
                return safeExtent * 0.34f;
            case 20:
                return safeExtent * 0.24f;
            default:
                return safeExtent * 0.35f;
        }
    }

    private static float GetRotationOffset(int polygonSides)
    {
        switch (polygonSides)
        {
            case 3:
                return -90f;
            case 4:
                return 45f;
            default:
                return 90f;
        }
    }

    private static float SafeInverse(float value)
    {
        return Mathf.Approximately(value, 0f) ? 1f : 1f / value;
    }
}

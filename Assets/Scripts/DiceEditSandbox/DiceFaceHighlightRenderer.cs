using UnityEngine;

[DisallowMultipleComponent]
public class DiceFaceHighlightRenderer : MonoBehaviour
{
    [SerializeField] private Color previewColor = new Color(1f, 0.83f, 0.18f, 0.65f);
    [SerializeField] private Color committedColor = new Color(0.2f, 1f, 0.75f, 0.7f);
    [SerializeField] private float normalOffset = 0.015f;

    private DiceSpinnerGeneric _spinner;
    private DiceFaceSelectionMap _selectionMap;
    private MeshFilter _highlightFilter;
    private MeshRenderer _highlightRenderer;
    private Material _previewMaterial;
    private Material _committedMaterial;
    private int _currentFaceIndex = -2;
    private bool _currentCommitted;
    private bool _isConfigured;

    public void Configure(DiceSpinnerGeneric spinner, DiceFaceSelectionMap selectionMap)
    {
        if (_isConfigured && _spinner == spinner && _selectionMap == selectionMap)
            return;

        _spinner = spinner;
        _selectionMap = selectionMap;
        EnsureHighlightObject();
        _isConfigured = true;
    }

    public void ShowFace(int logicalFaceIndex, bool committed)
    {
        EnsureHighlightObject();

        if (logicalFaceIndex < 0)
        {
            _highlightRenderer.enabled = false;
            _currentFaceIndex = -1;
            return;
        }

        if (_currentFaceIndex != logicalFaceIndex || _currentCommitted != committed)
        {
            Mesh mesh = _selectionMap != null ? _selectionMap.BuildHighlightMesh(logicalFaceIndex) : null;
            if (mesh == null)
            {
                Debug.Log($"[DiceEditHighlight] Failed to build highlight mesh for logicalFaceIndex={logicalFaceIndex}.");
                _highlightRenderer.enabled = false;
                _currentFaceIndex = -1;
                return;
            }

            OffsetMesh(mesh);
            _highlightFilter.sharedMesh = mesh;
            _highlightRenderer.sharedMaterial = committed ? _committedMaterial : _previewMaterial;
            _currentFaceIndex = logicalFaceIndex;
            _currentCommitted = committed;
        }

        Debug.Log($"[DiceEditHighlight] Showing highlight for logicalFaceIndex={logicalFaceIndex}, committed={committed}.");
        _highlightRenderer.enabled = true;
    }

    private void EnsureHighlightObject()
    {
        if (_highlightFilter != null && _highlightRenderer != null)
            return;

        GameObject go = new GameObject("DiceFaceHighlight");
        Transform parent = _selectionMap != null && _selectionMap.MeshTransform != null
            ? _selectionMap.MeshTransform
            : transform;
        go.transform.SetParent(parent, false);
        go.layer = gameObject.layer;

        _highlightFilter = go.AddComponent<MeshFilter>();
        _highlightRenderer = go.AddComponent<MeshRenderer>();
        _highlightRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _highlightRenderer.receiveShadows = false;

        _previewMaterial = BuildMaterial(previewColor);
        _committedMaterial = BuildMaterial(committedColor);
        _highlightRenderer.enabled = false;
    }

    private Material BuildMaterial(Color color)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    private void OffsetMesh(Mesh mesh)
    {
        if (mesh == null)
            return;

        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        if (vertices == null || normals == null || vertices.Length != normals.Length)
            return;

        for (int i = 0; i < vertices.Length; i++)
            vertices[i] += normals[i].normalized * normalOffset;

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
    }
}

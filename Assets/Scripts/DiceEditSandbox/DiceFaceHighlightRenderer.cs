using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class DiceFaceHighlightRenderer : MonoBehaviour
{
    [SerializeField] private Color previewColor = new Color(1f, 0.83f, 0.18f, 0.65f);
    [SerializeField] private Color committedColor = new Color(0.2f, 1f, 0.75f, 0.7f);
    [SerializeField] private Color alternateColor = new Color(0.24f, 0.88f, 0.36f, 0.72f);
    [SerializeField] private float normalOffset = 0.003f;

    private DiceSpinnerGeneric _spinner;
    private DiceFaceSelectionMap _selectionMap;
    private Material _previewMaterial;
    private Material _committedMaterial;
    private Material _alternateMaterial;
    private readonly List<Entry> _entries = new List<Entry>();
    private readonly List<int> _currentFaces = new List<int>();
    private bool _currentCommitted;
    private bool _isConfigured;

    public void Configure(DiceSpinnerGeneric spinner, DiceFaceSelectionMap selectionMap)
    {
        if (_isConfigured && _spinner == spinner && _selectionMap == selectionMap)
            return;

        _spinner = spinner;
        _selectionMap = selectionMap;
        EnsureMaterials();
        _isConfigured = true;
    }

    public void ShowFace(int logicalFaceIndex, bool committed)
    {
        if (logicalFaceIndex < 0)
        {
            Clear();
            return;
        }

        _currentFaces.Clear();
        _currentFaces.Add(logicalFaceIndex);
        ShowFaces(_currentFaces, committed);
    }

    public void ShowFaces(IReadOnlyList<int> logicalFaceIndices, bool committed)
    {
        EnsureMaterials();
        CleanupDeadEntries();

        if (logicalFaceIndices == null || logicalFaceIndices.Count == 0)
        {
            Clear();
            return;
        }

        EnsureEntryCount(logicalFaceIndices.Count);
        Material material = committed ? _committedMaterial : _previewMaterial;

        for (int i = 0; i < _entries.Count; i++)
        {
            bool enabled = i < logicalFaceIndices.Count;
            Entry entry = _entries[i];
            if (entry == null || entry.filter == null || entry.renderer == null)
                continue;
            if (!enabled)
            {
                entry.renderer.enabled = false;
                if (entry.filter != null)
                    entry.filter.sharedMesh = null;
                continue;
            }

            Mesh mesh = _selectionMap != null ? _selectionMap.BuildHighlightMesh(logicalFaceIndices[i]) : null;
            if (mesh == null)
            {
                entry.renderer.enabled = false;
                if (entry.filter != null)
                    entry.filter.sharedMesh = null;
                continue;
            }

            OffsetMesh(mesh);
            entry.filter.sharedMesh = mesh;
            entry.renderer.sharedMaterial = material;
            entry.renderer.enabled = true;
        }

        _currentFaces.Clear();
        for (int i = 0; i < logicalFaceIndices.Count; i++)
            _currentFaces.Add(logicalFaceIndices[i]);
        _currentCommitted = committed;
    }

    public void ShowFacesWithKinds(
        IReadOnlyList<int> logicalFaceIndices,
        IReadOnlyList<DiceEditSandboxController.SandboxFaceHighlightKind> highlightKinds)
    {
        EnsureMaterials();
        CleanupDeadEntries();

        if (logicalFaceIndices == null || highlightKinds == null || logicalFaceIndices.Count == 0 || logicalFaceIndices.Count != highlightKinds.Count)
        {
            Clear();
            return;
        }

        EnsureEntryCount(logicalFaceIndices.Count);

        for (int i = 0; i < _entries.Count; i++)
        {
            bool enabled = i < logicalFaceIndices.Count;
            Entry entry = _entries[i];
            if (entry == null || entry.filter == null || entry.renderer == null)
                continue;
            if (!enabled)
            {
                entry.renderer.enabled = false;
                if (entry.filter != null)
                    entry.filter.sharedMesh = null;
                continue;
            }

            Mesh mesh = _selectionMap != null ? _selectionMap.BuildHighlightMesh(logicalFaceIndices[i]) : null;
            if (mesh == null)
            {
                entry.renderer.enabled = false;
                if (entry.filter != null)
                    entry.filter.sharedMesh = null;
                continue;
            }

            OffsetMesh(mesh);
            entry.filter.sharedMesh = mesh;
            entry.renderer.sharedMaterial = ResolveMaterial(highlightKinds[i]);
            entry.renderer.enabled = true;
        }

        _currentFaces.Clear();
        for (int i = 0; i < logicalFaceIndices.Count; i++)
            _currentFaces.Add(logicalFaceIndices[i]);
        _currentCommitted = false;
    }

    public void Clear()
    {
        HideAll();
        _currentFaces.Clear();
        _currentCommitted = false;

        for (int i = 0; i < _entries.Count; i++)
        {
            Entry entry = _entries[i];
            if (entry == null)
                continue;

            if (entry.renderer != null)
                entry.renderer.enabled = false;
            if (entry.filter != null)
            {
                Mesh mesh = entry.filter.sharedMesh;
                entry.filter.sharedMesh = null;
                if (mesh != null)
                {
                    if (Application.isPlaying)
                        Object.Destroy(mesh);
                    else
                        Object.DestroyImmediate(mesh);
                }
            }
        }

        Transform parent = _selectionMap != null && _selectionMap.HighlightTransform != null
            ? _selectionMap.HighlightTransform
            : transform;

        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        for (int i = children.Length - 1; i >= 0; i--)
        {
            Transform child = children[i];
            if (child == null || child == parent)
                continue;

            if (!child.name.StartsWith("DiceFaceHighlight"))
                continue;

            MeshFilter filter = child.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                Mesh mesh = filter.sharedMesh;
                filter.sharedMesh = null;
                if (Application.isPlaying)
                    Object.Destroy(mesh);
                else
                    Object.DestroyImmediate(mesh);
            }

            MeshRenderer renderer = child.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.enabled = false;

            child.gameObject.SetActive(false);

            if (Application.isPlaying)
                Object.Destroy(child.gameObject);
            else
                Object.DestroyImmediate(child.gameObject);
        }

        _entries.Clear();
    }

    private void EnsureMaterials()
    {
        if (_previewMaterial != null && _committedMaterial != null && _alternateMaterial != null)
            return;

        _previewMaterial = BuildMaterial(previewColor);
        _committedMaterial = BuildMaterial(committedColor);
        _alternateMaterial = BuildMaterial(alternateColor);
    }

    private Material ResolveMaterial(DiceEditSandboxController.SandboxFaceHighlightKind highlightKind)
    {
        switch (highlightKind)
        {
            case DiceEditSandboxController.SandboxFaceHighlightKind.Committed:
                return _committedMaterial;
            case DiceEditSandboxController.SandboxFaceHighlightKind.CopyTarget:
                return _alternateMaterial;
            case DiceEditSandboxController.SandboxFaceHighlightKind.Preview:
            case DiceEditSandboxController.SandboxFaceHighlightKind.CopySource:
            default:
                return _previewMaterial;
        }
    }

    private void EnsureEntryCount(int count)
    {
        CleanupDeadEntries();

        while (_entries.Count < count)
            _entries.Add(CreateEntry(_entries.Count));
    }

    private Entry CreateEntry(int index)
    {
        GameObject go = new GameObject($"DiceFaceHighlight_{index}");
        Transform parent = _selectionMap != null && _selectionMap.HighlightTransform != null
            ? _selectionMap.HighlightTransform
            : transform;
        go.transform.SetParent(parent, false);
        go.layer = gameObject.layer;

        MeshFilter filter = go.AddComponent<MeshFilter>();
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.enabled = false;

        return new Entry
        {
            filter = filter,
            renderer = renderer
        };
    }

    private void HideAll()
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i] != null && _entries[i].renderer != null)
                _entries[i].renderer.enabled = false;
        }
    }

    private void CleanupDeadEntries()
    {
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            Entry entry = _entries[i];
            if (entry == null || entry.filter == null || entry.renderer == null)
                _entries.RemoveAt(i);
        }
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

    private sealed class Entry
    {
        public MeshFilter filter;
        public MeshRenderer renderer;
    }
}

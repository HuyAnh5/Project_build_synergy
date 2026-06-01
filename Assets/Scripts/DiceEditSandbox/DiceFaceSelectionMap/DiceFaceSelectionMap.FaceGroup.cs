using System.Collections.Generic;
using UnityEngine;

// Stores one selectable dice face group as mesh triangles or a fallback polygon.
public partial class DiceFaceSelectionMap
{
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

        // Adds a source mesh triangle to this selectable face group.
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

        // Accumulates face area so the main face candidates can be sorted above bevels.
        public void AddArea(float area)
        {
            _area += Mathf.Max(0f, area);
        }

        // Accumulates vertex positions for the generated highlight center.
        public void AddVertex(Vector3 vertex)
        {
            _vertexSum += vertex;
            _vertexCount++;
        }
    }
}

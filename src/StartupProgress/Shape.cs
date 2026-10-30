using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StartupProgress
{
    /// <summary>
    /// A UI graphic made of flat polygons: the emblem, the rings and the corner brackets.
    /// UI meshes get no anti-aliasing, so every edge carries a fringe one pixel wide that fades to
    /// clear; that is what keeps the diagonals and the turning rings from looking jagged.
    ///
    /// Build it once with the helpers, then <see cref="Commit"/>. Animate it through its
    /// transform and <c>canvasRenderer.SetAlpha</c>, which move or fade it without rebuilding
    /// the mesh.
    /// </summary>
    internal class Shape : MaskableGraphic
    {
        private readonly List<UIVertex> _vertices = new List<UIVertex>();
        private readonly List<int> _indices = new List<int>();

        /// <summary>Width of the soft edge, in canvas units. One pixel on a pixel-sized canvas.</summary>
        internal float Feather = 1f;

        internal static Shape Create(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var shape = go.AddComponent<Shape>();
            shape.raycastTarget = false;
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
            return shape;
        }

        internal void Clear()
        {
            _vertices.Clear();
            _indices.Clear();
        }

        internal void Commit() => SetVerticesDirty();

        /// <summary>A filled convex polygon, either winding, with a soft outer edge.</summary>
        internal void Polygon(Vector2[] points, Color32 color)
        {
            int n = points.Length;
            float area = 0f;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % n];
                area += a.x * b.y - b.x * a.y;
            }
            float outward = area > 0f ? 1f : -1f;

            // Each corner's fringe point sits along the mitre of its two edges.
            var outer = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 previous = points[(i + n - 1) % n];
                Vector2 here = points[i];
                Vector2 next = points[(i + 1) % n];
                Vector2 n0 = EdgeNormal(previous, here) * outward;
                Vector2 n1 = EdgeNormal(here, next) * outward;
                Vector2 mitre = (n0 + n1).normalized;
                float length = Feather / Mathf.Max(Vector2.Dot(mitre, n1), 0.25f);
                outer[i] = here + mitre * length;
            }

            Color32 clear = Clear(color);
            int start = _vertices.Count;
            for (int i = 0; i < n; i++)
            {
                Add(points[i], color);
            }
            for (int i = 0; i < n; i++)
            {
                Add(outer[i], clear);
            }
            for (int i = 1; i < n - 1; i++)
            {
                Index(start, start + i, start + i + 1);
            }
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                Index(start + i, start + n + i, start + n + j);
                Index(start + i, start + n + j, start + j);
            }
        }

        internal void Tri(Vector2 a, Vector2 b, Vector2 c, Color32 color) => Polygon(new[] { a, b, c }, color);

        internal void Line(Vector2 a, Vector2 b, float width, Color32 color)
        {
            Vector2 side = new Vector2(-(b - a).y, (b - a).x).normalized * (width * 0.5f);
            Polygon(new[] { a - side, a + side, b + side, b - side }, color);
        }

        /// <summary>A band between two radii, clockwise from <paramref name="startDegrees"/> (0 = up), soft on every side.</summary>
        internal void Arc(float radius, float thickness, float startDegrees, float sweepDegrees, Color32 color)
        {
            if (sweepDegrees <= 0f)
            {
                return;
            }
            bool closed = sweepDegrees >= 359.99f;
            int steps = Mathf.Max(2, Mathf.CeilToInt(sweepDegrees / 3f));
            float inner = radius - thickness * 0.5f;
            float outer = radius + thickness * 0.5f;
            Color32 clear = Clear(color);

            // Four rings of vertices per step: clear, solid, solid, clear.
            int start = _vertices.Count;
            for (int i = 0; i <= steps; i++)
            {
                Vector2 d = Direction(startDegrees + sweepDegrees * i / steps);
                Add(d * (inner - Feather), clear);
                Add(d * inner, color);
                Add(d * outer, color);
                Add(d * (outer + Feather), clear);
            }
            for (int i = 0; i < steps; i++)
            {
                int a = start + i * 4;
                int b = a + 4;
                for (int k = 0; k < 3; k++)
                {
                    Index(a + k, a + k + 1, b + k + 1);
                    Index(a + k, b + k + 1, b + k);
                }
            }

            if (!closed)
            {
                // Soft ends, pushed out along the tangent.
                Cap(startDegrees, inner, outer, -1f, color);
                Cap(startDegrees + sweepDegrees, inner, outer, 1f, color);
            }
        }

        private void Cap(float degrees, float inner, float outer, float sign, Color32 color)
        {
            Vector2 d = Direction(degrees);
            Vector2 tangent = new Vector2(d.y, -d.x) * (sign * Feather);
            Color32 clear = Clear(color);
            int start = _vertices.Count;
            Add(d * inner, color);
            Add(d * outer, color);
            Add(d * outer + tangent, clear);
            Add(d * inner + tangent, clear);
            Index(start, start + 1, start + 2);
            Index(start, start + 2, start + 3);
        }

        private static Vector2 EdgeNormal(Vector2 a, Vector2 b)
        {
            Vector2 e = (b - a).normalized;
            return new Vector2(e.y, -e.x);
        }

        private static Vector2 Direction(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
        }

        private static Color32 Clear(Color32 color) => new Color32(color.r, color.g, color.b, 0);

        private void Add(Vector2 position, Color32 color)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            _vertices.Add(vertex);
        }

        private void Index(int a, int b, int c)
        {
            _indices.Add(a);
            _indices.Add(b);
            _indices.Add(c);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            for (int i = 0; i < _vertices.Count; i++)
            {
                vh.AddVert(_vertices[i]);
            }
            for (int i = 0; i < _indices.Count; i += 3)
            {
                vh.AddTriangle(_indices[i], _indices[i + 1], _indices[i + 2]);
            }
        }
    }
}

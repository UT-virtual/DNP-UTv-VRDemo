using System.Collections.Generic;
using UnityEngine;

namespace DNPDemo.Common
{
    /// <summary>
    /// Builds a unit icosphere (radius 1, centred at the origin): an icosahedron whose faces are split
    /// <c>subdivisions</c> times and pushed onto the sphere. It spreads its triangles evenly, so it looks round with far
    /// fewer triangles than a UV sphere. Subdivisions 0..3 give 20, 80, 320 and 1280 triangles.
    /// Positions only; meant for unlit, single-colour drawing.
    /// </summary>
    public static class IcosphereMesh
    {
        public const int MaxSubdivisions = 3;

        public static Mesh Create(int subdivisions, string name = "Icosphere")
        {
            subdivisions = Mathf.Clamp(subdivisions, 0, MaxSubdivisions);
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            var vertices = new List<Vector3>
            {
                new(-1, t, 0), new(1, t, 0), new(-1, -t, 0), new(1, -t, 0),
                new(0, -1, t), new(0, 1, t), new(0, -1, -t), new(0, 1, -t),
                new(t, 0, -1), new(t, 0, 1), new(-t, 0, -1), new(-t, 0, 1),
            };
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] = vertices[i].normalized;
            }
            var triangles = new List<int>
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
                1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
                4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
            };

            var midpoints = new Dictionary<long, int>();
            for (int s = 0; s < subdivisions; s++)
            {
                var next = new List<int>(triangles.Count * 4);
                midpoints.Clear();
                for (int i = 0; i < triangles.Count; i += 3)
                {
                    int a = triangles[i];
                    int b = triangles[i + 1];
                    int c = triangles[i + 2];
                    int ab = Midpoint(a, b, vertices, midpoints);
                    int bc = Midpoint(b, c, vertices, midpoints);
                    int ca = Midpoint(c, a, vertices, midpoints);
                    next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                triangles = next;
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
            mesh.UploadMeshData(markNoLongerReadable: true);
            return mesh;
        }

        static int Midpoint(int a, int b, List<Vector3> vertices, Dictionary<long, int> cache)
        {
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (cache.TryGetValue(key, out int index))
            {
                return index;
            }
            index = vertices.Count;
            vertices.Add(((vertices[a] + vertices[b]) * 0.5f).normalized);
            cache.Add(key, index);
            return index;
        }
    }
}

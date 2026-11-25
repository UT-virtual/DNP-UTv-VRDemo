using System.Collections.Generic;
using UnityEngine;

namespace DNPDemo.Interaction.Hint
{
    /// <summary>
    /// Meshes of the <see cref="DetectionZone"/> shapes, in the zone's local space, for <see cref="HintZoneView"/>.
    /// Positions only; meant for unlit, single-colour drawing. The sphere is <see cref="Common.IcosphereMesh"/>.
    /// </summary>
    public static class ZoneShapeMeshes
    {
        /// <summary>A cube of side 1 centred at the origin; scale it by the box size.</summary>
        public static Mesh CreateBox()
        {
            var vertices = new List<Vector3>();
            for (int i = 0; i < 8; i++)
            {
                vertices.Add(new Vector3((i & 1) == 0 ? -0.5f : 0.5f, (i & 2) == 0 ? -0.5f : 0.5f, (i & 4) == 0 ? -0.5f : 0.5f));
            }
            // Corner indices of each face, as bits x=1, y=2, z=4.
            int[][] faces =
            {
                new[] { 0, 2, 3, 1 }, new[] { 4, 5, 7, 6 }, // -z, +z
                new[] { 0, 4, 6, 2 }, new[] { 1, 3, 7, 5 }, // -x, +x
                new[] { 0, 1, 5, 4 }, new[] { 2, 6, 7, 3 }, // -y, +y
            };
            var triangles = new List<int>();
            foreach (int[] face in faces)
            {
                AddQuad(triangles, face[0], face[1], face[2], face[3]);
            }
            return Build("ZoneBox", vertices, triangles);
        }

        /// <summary>
        /// A capsule along the local Y axis with the zone's <paramref name="radius"/> and total <paramref name="height"/>
        /// (the height includes both caps, as in <see cref="DetectionZone"/>).
        /// </summary>
        public static Mesh CreateCapsule(float radius, float height, int segments = 16, int capRings = 6)
        {
            radius = Mathf.Max(0f, radius);
            float halfSegment = Mathf.Max(0f, height * 0.5f - radius);
            segments = Mathf.Max(3, segments);
            capRings = Mathf.Max(1, capRings);

            var vertices = new List<Vector3> { new(0f, halfSegment + radius, 0f) };
            var rings = new List<(float ringRadius, float y)>();
            for (int r = 1; r <= capRings; r++)
            {
                float angle = 0.5f * Mathf.PI * r / capRings;
                rings.Add((radius * Mathf.Sin(angle), halfSegment + radius * Mathf.Cos(angle)));
            }
            for (int r = 0; r < capRings; r++)
            {
                float angle = 0.5f * Mathf.PI * r / capRings;
                rings.Add((radius * Mathf.Cos(angle), -halfSegment - radius * Mathf.Sin(angle)));
            }
            foreach ((float ringRadius, float y) in rings)
            {
                for (int s = 0; s < segments; s++)
                {
                    float around = 2f * Mathf.PI * s / segments;
                    vertices.Add(new Vector3(ringRadius * Mathf.Cos(around), y, ringRadius * Mathf.Sin(around)));
                }
            }
            int bottom = vertices.Count;
            vertices.Add(new Vector3(0f, -halfSegment - radius, 0f));

            var triangles = new List<int>();
            for (int s = 0; s < segments; s++)
            {
                int next = (s + 1) % segments;
                triangles.AddRange(new[] { 0, 1 + s, 1 + next });
                for (int r = 0; r < rings.Count - 1; r++)
                {
                    int upper = 1 + r * segments;
                    int lower = upper + segments;
                    AddQuad(triangles, upper + s, lower + s, lower + next, upper + next);
                }
                int last = 1 + (rings.Count - 1) * segments;
                triangles.AddRange(new[] { bottom, last + next, last + s });
            }
            return Build("ZoneCapsule", vertices, triangles);
        }

        static void AddQuad(List<int> triangles, int a, int b, int c, int d)
        {
            triangles.AddRange(new[] { a, b, c, a, c, d });
        }

        /// <summary>
        /// Every shape is convex around the origin, so each triangle is turned to face away from the origin. Unity draws
        /// the side where the corners run clockwise; that is the side the cross product of the edges points to.
        /// </summary>
        static Mesh Build(string name, List<Vector3> vertices, List<int> triangles)
        {
            for (int i = 0; i < triangles.Count; i += 3)
            {
                Vector3 a = vertices[triangles[i]];
                Vector3 b = vertices[triangles[i + 1]];
                Vector3 c = vertices[triangles[i + 2]];
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), a + b + c) < 0f)
                {
                    (triangles[i + 1], triangles[i + 2]) = (triangles[i + 2], triangles[i + 1]);
                }
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

﻿using System.ComponentModel;

using UnityEngine;

namespace TheGoldenMule.Geo
{
    /// <summary>
    /// Builds geometry for a circle.
    /// </summary>
    [DisplayName("Circle")]
    [Description("Builds a circle with the desired number of edges.")]
    public class CircleGeometryBuilder : StandardGeometryBuilder
    {
        /// <summary>
        /// Builds geometry for a circle.
        /// </summary>
        public override void Build(Mesh mesh, GeometryBuilderSettings settings)
        {
            var polygonSettings = (CircleGeometryBuilderSettings) settings;

            var numSides = Mathf.Max(3, polygonSettings.NumSides);

            int numVerts, numTriangles;
            CalculateBufferLength(numSides, out numVerts, out numTriangles);
            var numIndices = numTriangles * 3;

            var vertices = new Vector3[numVerts];
            var triangles = new int[numIndices];

            BuildCircle(
                numSides,
                ref vertices,
                ref triangles);

            settings.Vertex.TransformAndApply(mesh, ref vertices, ref triangles);

            ApplyAllDefaults(mesh, settings);
        }

        /// <summary>
        /// Calculates the number of verts and triangles needed.
        /// </summary>
        public static void CalculateBufferLength(
            int numSides,
            out int numVerts,
            out int numTriangles)
        {
            numVerts = numSides + 1;
            numTriangles = numSides;
        }

        /// <summary>
        /// Actually builds the vertex and index buffers.
        /// </summary>
        public static void BuildCircle(
            int numSides,
            ref Vector3[] vertices,
            ref int[] triangles)
        {
            int numVerts, numTriangles;
            CalculateBufferLength(numSides, out numVerts, out numTriangles);

            vertices[0] = Vector3.zero;

            var radians = 2f * Mathf.PI / numSides;
            for (var i = 1; i < numVerts; i++)
            {
                vertices[i] = 0.5f * new Vector3(
                        Mathf.Cos(radians * (i - 1)),
                        0f,
                        Mathf.Sin(radians * (i - 1)));
            }

            for (var i = 0; i < numTriangles * 3; i += 3)
            {
                var vertIndex = i / 3 + 1;

                triangles[i] = vertIndex;
                triangles[i + 1] = 0;

                vertIndex = vertIndex + 1;
                if (vertIndex >= numVerts)
                {
                    vertIndex = 1;
                }

                triangles[i + 2] = vertIndex;
            }
        }

        /// <summary>
        /// Custom factory method for CircleGeometry settings.
        /// </summary>
        /// <returns></returns>
        [CustomFactory(typeof(CircleGeometryBuilder))]
        private static GeometryBuilderSettings Factory()
        {
            return new CircleGeometryBuilderSettings();
        }
    }
}

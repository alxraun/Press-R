using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator
{
    public class ThingRenderData
    {
        public Mesh Mesh { get; set; }

        public Matrix4x4 Matrix { get; set; }

        public Material Material { get; set; }

        public ThingRenderData(Mesh mesh, Matrix4x4 matrix, Material material)
        {
            Mesh = mesh;
            Matrix = matrix;
            Material = material;
        }
    }
}

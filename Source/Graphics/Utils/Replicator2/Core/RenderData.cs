using UnityEngine;

namespace PressR.Graphics.Utils.Replicator2.Core
{
    public class RenderData
    {
        public Mesh Mesh { get; }
        public Matrix4x4 Matrix { get; }
        public Material Material { get; }

        public RenderData(Mesh mesh, Matrix4x4 matrix, Material material)
        {
            Mesh = mesh;
            Matrix = matrix;
            Material = material;
        }
    }
}

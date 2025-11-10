using UnityEngine;

namespace Microtools.Graphics.Replicator
{
    public class ThingRenderData(Mesh mesh, Matrix4x4 matrix, Material material)
    {
        public Mesh Mesh { get; set; } = mesh;

        public Matrix4x4 Matrix { get; set; } = matrix;

        public Material Material { get; set; } = material;
    }
}

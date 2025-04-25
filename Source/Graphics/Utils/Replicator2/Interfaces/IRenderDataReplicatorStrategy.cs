using PressR.Graphics.Utils.Replicator2.Core;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator2.Interfaces
{
    public interface IRenderDataReplicatorStrategy
    {
        bool CanHandle(Thing thing);

        Mesh GetBaseMesh(RenderContext context);

        Material GetBaseMaterial(RenderContext context);

        Vector3 GetBaseOffset(RenderContext context);

        Quaternion GetBaseRotation(RenderContext context);

        Vector3 GetBaseScale(RenderContext context);
    }
}

using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator
{
    public interface IRenderDataReplicatorStrategy
    {
        bool CanHandle(Thing thing);

        Mesh GetMesh(Thing thing, Rot4 rot);
        Material GetMaterial(Thing thing, Rot4 rot);
        Quaternion GetRotation(Thing thing, Rot4 rot, float extraRotation);
        Vector3 GetPositionOffset(Thing thing, Rot4 rot);
        Vector3 GetScale(Thing thing);

        string GetStrategyName();
        bool IsEnabled();
    }
}

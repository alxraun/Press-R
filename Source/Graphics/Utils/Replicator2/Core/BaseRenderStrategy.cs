using PressR.Graphics.Utils.Replicator2.Interfaces;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator2.Core
{
    public abstract class BaseRenderStrategy : IRenderDataReplicatorStrategy
    {
        public abstract bool CanHandle(Thing thing);

        public virtual Mesh GetBaseMesh(RenderContext context)
        {
            return context.Thing.Graphic?.MeshAt(context.BaseRot);
        }

        public virtual Material GetBaseMaterial(RenderContext context)
        {
            return context.Thing.Graphic?.MatSingleFor(context.Thing);
        }

        public virtual Vector3 GetBaseOffset(RenderContext context)
        {
            return context.Thing.Graphic?.DrawOffset(context.BaseRot) ?? Vector3.zero;
        }

        public virtual Quaternion GetBaseRotation(RenderContext context)
        {
            return Quaternion.AngleAxis(context.BaseRot.AsAngle, Vector3.up);
        }

        public virtual Vector3 GetBaseScale(RenderContext context)
        {
            if (context.Thing.Graphic != null)
            {
                return new Vector3(
                    context.Thing.Graphic.drawSize.x,
                    1f,
                    context.Thing.Graphic.drawSize.y
                );
            }
            return Vector3.one;
        }
    }
}

using PressR.Graphics.Utils.Replicator2.Core;
using PressR.Graphics.Utils.Replicator2.Interfaces;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator2.Decorators.Graphics
{
    public static class MultiGraphicDecorators
    {
        [DecoratorPriority(ReplicatorConstants.Priority_BaseScale + 10)]
        public class HorizontalScaleSwapDecorator : BaseDecorator, IScaleDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_BaseScale + 10;

            public override bool CanApply(RenderContext context)
            {
                return context.Thing?.Graphic is Graphic_Multi && context.BaseRot.IsHorizontal;
            }

            public Vector3 ModifyScale(RenderContext context, Vector3 currentScale)
            {
                float tempX = currentScale.x;
                currentScale.x = currentScale.z;
                currentScale.z = tempX;
                return currentScale;
            }
        }
    }
}

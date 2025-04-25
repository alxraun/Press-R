using PressR.Graphics.Utils.Replicator2.Core;
using PressR.Graphics.Utils.Replicator2.Interfaces;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator2.Decorators.Pawn
{
    public static class PawnPostureDecorators
    {
        [DecoratorPriority(ReplicatorConstants.Priority_ContextRotation + 20)]
        public class PostureRotationDecorator : BaseDecorator, IRotationDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_ContextRotation + 20;

            public override bool CanApply(RenderContext context)
            {
                return context.Pawn != null
                    && !(context.Thing is Corpse)
                    && context.Pawn.GetPosture() != PawnPosture.Standing;
            }

            public Quaternion ModifyRotation(RenderContext context, Quaternion currentRotation)
            {
                if (context.Pawn?.Drawer?.renderer != null)
                {
                    float bodyAngle = context.Pawn.Drawer.renderer.BodyAngle(PawnRenderFlags.None);
                    return Quaternion.AngleAxis(bodyAngle, Vector3.up);
                }
                return currentRotation;
            }
        }
    }
}

using PressR.Graphics.Utils.Replicator2.Core;
using PressR.Graphics.Utils.Replicator2.Interfaces;
using RimWorld;
using UnityEngine;
using Verse;
using BasePawn = Verse.Pawn;

namespace PressR.Graphics.Utils.Replicator2.Decorators.Pawn
{
    public static class PawnCarriedDecorators
    {
        private static class CarriedConstants
        {
            public const float CarriedPawnAngleWestNorth = 290f;
            public const float CarriedPawnAngleEastSouth = 70f;

            public const float CarriedPawnYOffset = 0.035f;
        }

        [DecoratorPriority(ReplicatorConstants.Priority_ContextRotation + 40)]
        public class CarriedRotationDecorator : BaseDecorator, IRotationDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_ContextRotation + 40;

            public override bool CanApply(RenderContext context)
            {
                return context.Thing.ParentHolder is Pawn_CarryTracker;
            }

            public Quaternion ModifyRotation(RenderContext context, Quaternion currentRotation)
            {
                if (
                    context.Thing.ParentHolder is Pawn_CarryTracker carrierTracker
                    && carrierTracker.pawn != null
                )
                {
                    BasePawn carrier = carrierTracker.pawn;
                    Rot4 carrierRot = carrier.Rotation;
                    float carrierBodyAngle = carrier.Drawer.renderer.BodyAngle(
                        PawnRenderFlags.None
                    );

                    float angle =
                        (carrierRot == Rot4.West || carrierRot == Rot4.North)
                            ? CarriedConstants.CarriedPawnAngleWestNorth
                            : CarriedConstants.CarriedPawnAngleEastSouth;

                    angle += carrierBodyAngle;

                    return Quaternion.AngleAxis(angle, Vector3.up);
                }
                return currentRotation;
            }
        }

        [DecoratorPriority(ReplicatorConstants.Priority_ContextOffset + 40)]
        public class CarriedPositionDecorator : BaseDecorator, IPositionOffsetDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_ContextOffset + 40;

            public override bool CanApply(RenderContext context)
            {
                return context.Thing.ParentHolder is Pawn_CarryTracker;
            }

            public Vector3 GetPositionOffsetDelta(RenderContext context)
            {
                return new Vector3(0f, CarriedConstants.CarriedPawnYOffset, 0f);
            }
        }
    }
}

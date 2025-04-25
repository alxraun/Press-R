using PressR.Graphics.Utils.Replicator2;
using PressR.Graphics.Utils.Replicator2.Core;
using PressR.Graphics.Utils.Replicator2.Interfaces;
using PressR.Graphics.Utils.Replicator2.Registry;
using PressR.Graphics.Utils.Replicator2.Strategies;
using RimWorld;
using UnityEngine;
using Verse;
using BasePawn = Verse.Pawn;

namespace PressR.Graphics.Utils.Replicator2.Decorators.Context
{
    public static class CarriedItemDecorators
    {
        private static class CarriedItemConstants
        {
            public static readonly Vector3 BaseCarriedOffset = new Vector3(0.0f, 0.0f, -0.1f);
            public const float NorthZOffset = 0.0f;
            public const float EastXOffset = 0.18f;
            public const float WestXOffset = -0.18f;
            public const float ChildZOffset = -0.1f;
            public const float NorthAltitudeOffset = -0.03846154f;
            public const float OtherAltitudeOffset = 0.03846154f;

            public const float DefaultMaxRandomAngle = 35f;
            public const float RandomAngleMultiplier = 542f;
        }

        [DecoratorPriority(ReplicatorConstants.Priority_ContextRotation + 70)]
        public class CarriedItemRotationDecorator : BaseDecorator, IRotationDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_ContextRotation + 70;

            public override bool CanApply(RenderContext context)
            {
                return !(context.Thing is BasePawn || context.Thing is Corpse)
                    && IsCarried(context.Thing);
            }

            public Quaternion ModifyRotation(RenderContext context, Quaternion currentRotation)
            {
                if (
                    context.Thing.ParentHolder is Pawn_CarryTracker carrierTracker
                    && carrierTracker.pawn != null
                )
                {
                    BasePawn carrier = carrierTracker.pawn;
                    float carrierBodyAngle = carrier.Drawer.renderer.BodyAngle(
                        PawnRenderFlags.None
                    );

                    var strategy = RenderStrategyFactory2.GetStrategy(context.Thing);
                    if (strategy is RandomRotatedStrategy2)
                    {
                        float randomRot = ReplicatorHelper2.GetRandomRotationAngle(context.Thing);
                        return Quaternion.AngleAxis(carrierBodyAngle + randomRot, Vector3.up);
                    }
                    else
                    {
                        return Quaternion.AngleAxis(carrierBodyAngle, Vector3.up);
                    }
                }
                return currentRotation;
            }
        }

        [DecoratorPriority(ReplicatorConstants.Priority_ContextOffset + 70)]
        public class CarriedItemPositionDecorator : BaseDecorator, IPositionOffsetDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_ContextOffset + 70;

            public override bool CanApply(RenderContext context)
            {
                return !(context.Thing is BasePawn || context.Thing is Corpse)
                    && IsCarried(context.Thing);
            }

            public Vector3 GetPositionOffsetDelta(RenderContext context)
            {
                if (
                    context.Thing.ParentHolder is Pawn_CarryTracker carrierTracker
                    && carrierTracker.pawn != null
                )
                {
                    BasePawn carrier = carrierTracker.pawn;
                    Rot4 carrierRot = carrier.Rotation;

                    Vector3 offsetDelta = CarriedItemConstants.BaseCarriedOffset;

                    switch (carrierRot.AsInt)
                    {
                        case 0:
                            offsetDelta.z = CarriedItemConstants.NorthZOffset;
                            break;
                        case 1:
                            offsetDelta.x = CarriedItemConstants.EastXOffset;
                            break;
                        case 3:
                            offsetDelta.x = CarriedItemConstants.WestXOffset;
                            break;
                    }

                    if (carrier.DevelopmentalStage == DevelopmentalStage.Child)
                    {
                        offsetDelta.z += CarriedItemConstants.ChildZOffset;
                    }

                    float altitudeOffset =
                        (carrierRot == Rot4.North)
                            ? CarriedItemConstants.NorthAltitudeOffset
                            : CarriedItemConstants.OtherAltitudeOffset;

                    offsetDelta.y = altitudeOffset;

                    return offsetDelta;
                }

                return Vector3.zero;
            }
        }
    }
}

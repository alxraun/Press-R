using PressR.Graphics.Utils.Replicator2.Core;
using PressR.Graphics.Utils.Replicator2.Interfaces;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator2.Decorators.Pawn
{
    public static class PawnCorpseDecorators
    {
        private static class CorpseConstants
        {
            public const float CorpseBaseYOffset = 0.04f;
            public const float CorpseDessicatedYOffsetAddition = 0.025f;
            public const float CorpseRottingYOffsetAddition = 0.015f;
        }

        [DecoratorPriority(ReplicatorConstants.Priority_ContextRotation + 10)]
        public class CorpseRotationDecorator : BaseDecorator, IRotationDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_ContextRotation + 10;

            public override bool CanApply(RenderContext context)
            {
                return context.Thing is Corpse;
            }

            public Quaternion ModifyRotation(RenderContext context, Quaternion currentRotation)
            {
                if (context.Pawn?.Drawer?.renderer?.wiggler != null)
                {
                    float downedAngle = context.Pawn.Drawer.renderer.wiggler.downedAngle;
                    return Quaternion.AngleAxis(downedAngle, Vector3.up);
                }
                return currentRotation;
            }
        }

        [DecoratorPriority(ReplicatorConstants.Priority_ContextOffset + 10)]
        public class CorpsePositionDecorator : BaseDecorator, IPositionOffsetDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_ContextOffset + 10;

            public override bool CanApply(RenderContext context)
            {
                return context.Thing is Corpse;
            }

            public Vector3 GetPositionOffsetDelta(RenderContext context)
            {
                float yOffset = CorpseConstants.CorpseBaseYOffset;

                if (context.Thing is Corpse corpse)
                {
                    switch (corpse.CurRotDrawMode)
                    {
                        case RotDrawMode.Dessicated:
                            yOffset += CorpseConstants.CorpseDessicatedYOffsetAddition;
                            break;
                        case RotDrawMode.Rotting:

                            break;
                    }
                }
                return new Vector3(0f, yOffset, 0f);
            }
        }
    }
}

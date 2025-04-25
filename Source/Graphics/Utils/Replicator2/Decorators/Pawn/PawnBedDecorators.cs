using PressR.Graphics.Utils.Replicator2.Core;
using PressR.Graphics.Utils.Replicator2.Interfaces;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator2.Decorators.Pawn
{
    public static class PawnBedDecorators
    {
        private static class BedConstants
        {
            public const float PawnInBedYOffset = 0.025f;
        }

        [DecoratorPriority(ReplicatorConstants.Priority_ContextOffset + 30)]
        public class BedPositionDecorator : BaseDecorator, IPositionOffsetDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_ContextOffset + 30;

            public override bool CanApply(RenderContext context)
            {
                return context.Pawn != null && context.Pawn.InBed();
            }

            public Vector3 GetPositionOffsetDelta(RenderContext context)
            {
                return new Vector3(0f, BedConstants.PawnInBedYOffset, 0f);
            }
        }
    }
}

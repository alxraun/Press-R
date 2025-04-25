using PressR.Graphics.Utils.Replicator2.Core;
using PressR.Graphics.Utils.Replicator2.Interfaces;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator2.Decorators.Pawn
{
    public static class PawnEquipmentDecorators
    {
        private static class EquipmentConstants
        {
            public const float EquipmentYOffset = 0.1f;
        }

        [DecoratorPriority(ReplicatorConstants.Priority_ContextRotation + 50)]
        public class EquipmentRotationDecorator : BaseDecorator, IRotationDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_ContextRotation + 50;

            public override bool CanApply(RenderContext context)
            {
                return IsEquipped(context.Thing);
            }

            public Quaternion ModifyRotation(RenderContext context, Quaternion currentRotation)
            {
                if (context.Pawn != null)
                {
                    float bodyAngle = context.Pawn.Drawer.renderer.BodyAngle(PawnRenderFlags.None);
                    return Quaternion.AngleAxis(bodyAngle, Vector3.up);
                }
                return currentRotation;
            }
        }

        [DecoratorPriority(ReplicatorConstants.Priority_ContextOffset + 50)]
        public class EquipmentPositionDecorator : BaseDecorator, IPositionOffsetDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_ContextOffset + 50;

            public override bool CanApply(RenderContext context)
            {
                return IsEquipped(context.Thing);
            }

            public Vector3 GetPositionOffsetDelta(RenderContext context)
            {
                return new Vector3(0f, EquipmentConstants.EquipmentYOffset, 0f);
            }
        }
    }
}

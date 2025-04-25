using PressR.Graphics.Utils.Replicator2.Core;
using PressR.Graphics.Utils.Replicator2.Interfaces;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator2.Decorators.Graphics
{
    public static class RandomRotatedDecorators
    {
        [DecoratorPriority(ReplicatorConstants.Priority_BaseRotation + 10)]
        public class BaseRandomRotationDecorator : BaseDecorator, IRotationDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_BaseRotation + 10;

            private bool ShouldUseFixedShelfWeaponRotation(RenderContext context)
            {
                return IsInStorage(context.Thing)
                    && context.Thing.def.IsWeapon
                    && HasMultipleItemsInSameCell(context.Thing);
            }

            public override bool CanApply(RenderContext context)
            {
                return context.Thing?.Graphic is Graphic_RandomRotated
                    && !IsCarried(context.Thing)
                    && !ShouldUseFixedShelfWeaponRotation(context);
            }

            public Quaternion ModifyRotation(RenderContext context, Quaternion currentRotation)
            {
                float randomRot = ReplicatorHelper2.GetRandomRotationAngle(context.Thing);
                return currentRotation * Quaternion.AngleAxis(randomRot, Vector3.up);
            }
        }
    }
}

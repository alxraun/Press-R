using PressR.Graphics.Utils.Replicator2.Core;
using PressR.Graphics.Utils.Replicator2.Decorators.Graphics;
using PressR.Graphics.Utils.Replicator2.Interfaces;
using PressR.Graphics.Utils.Replicator2.Registry;
using PressR.Graphics.Utils.Replicator2.Strategies;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator2.Decorators.Context
{
    public static class ShelfDecorators
    {
        private static class ShelfConstants
        {
            public const float WeaponStackRotationAngle = -90f;
            public const int MinItemsForStackRotation = 2;
            public const float MultiItemsPerCellDrawSizeFactor = 0.8f;
            public const float HorizontalApparelRotationAngle = 90f;
            public const float GeneralShelfRotationAngle = -90f;

            public static readonly Quaternion WeaponRelativeRotation = Quaternion.Euler(
                0f,
                WeaponStackRotationAngle,
                0f
            );
            public static readonly Quaternion ApparelHorizontalRelativeRotation = Quaternion.Euler(
                0f,
                HorizontalApparelRotationAngle,
                0f
            );
            public static readonly Quaternion GeneralRelativeRotation = Quaternion.Euler(
                0f,
                GeneralShelfRotationAngle,
                0f
            );

            public static readonly Quaternion WeaponFixedRotation = Quaternion.AngleAxis(
                -90f,
                Vector3.up
            );
            public static readonly Quaternion AlignToWorldNorth = Quaternion.identity;
        }

        private static Rot4 GetShelfRotation(RenderContext context)
        {
            return (
                    context.Thing.Position.GetEdifice(context.Thing.Map) as Building_Storage
                )?.Rotation ?? Rot4.North;
        }

        [DecoratorPriority(ReplicatorConstants.Priority_ContextRotation + 10)]
        public class WeaponRotationDecorator : BaseDecorator, IRotationDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_ContextRotation + 10;

            public override bool CanApply(RenderContext context)
            {
                return IsInStorage(context.Thing)
                    && context.Thing.def.IsWeapon
                    && context.Thing.def.rotateInShelves
                    && HasMultipleItemsInSameCell(context.Thing);
            }

            public Quaternion ModifyRotation(RenderContext context, Quaternion currentRotation)
            {
                return ShelfConstants.WeaponFixedRotation;
            }
        }

        [DecoratorPriority(ReplicatorConstants.Priority_ContextRotation + 11)]
        public class ShelfWeaponAlignDecorator : BaseDecorator, IRotationDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_ContextRotation + 11;

            public override bool CanApply(RenderContext context)
            {
                return IsInStorage(context.Thing)
                    && context.Thing.def.IsWeapon
                    && !context.Thing.def.rotateInShelves
                    && HasMultipleItemsInSameCell(context.Thing);
            }

            public Quaternion ModifyRotation(RenderContext context, Quaternion currentRotation)
            {
                return ShelfConstants.AlignToWorldNorth;
            }
        }

        [DecoratorPriority(ReplicatorConstants.Priority_ContextRotation + 15)]
        public class ApparelRotationDecorator : BaseDecorator, IRotationDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_ContextRotation + 15;

            public override bool CanApply(RenderContext context)
            {
                return IsInStorage(context.Thing) && context.Thing.def.IsApparel;
            }

            public Quaternion ModifyRotation(RenderContext context, Quaternion currentRotation)
            {
                Rot4 shelfRotation = GetShelfRotation(context);
                Quaternion shelfRotationQuat = Quaternion.AngleAxis(
                    shelfRotation.AsAngle,
                    Vector3.up
                );

                if (shelfRotation.IsHorizontal)
                {
                    return shelfRotationQuat * ShelfConstants.ApparelHorizontalRelativeRotation;
                }
                else
                {
                    return shelfRotationQuat;
                }
            }
        }

        [DecoratorPriority(ReplicatorConstants.Priority_ContextRotation + 18)]
        public class GraphicMultiShelfRotationDecorator : BaseDecorator, IRotationDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_ContextRotation + 18;

            public override bool CanApply(RenderContext context)
            {
                return IsInStorage(context.Thing) && context.Thing.Graphic is Graphic_Multi;
            }

            public Quaternion ModifyRotation(RenderContext context, Quaternion currentRotation)
            {
                Rot4 shelfRotation = GetShelfRotation(context);
                Quaternion shelfRotationQuat = Quaternion.AngleAxis(
                    shelfRotation.AsAngle,
                    Vector3.up
                );

                return shelfRotationQuat;
            }
        }

        [DecoratorPriority(ReplicatorConstants.Priority_ContextRotation + 20)]
        public class GeneralRotateInShelvesDecorator : BaseDecorator, IRotationDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_ContextRotation + 20;

            public override bool CanApply(RenderContext context)
            {
                return IsInStorage(context.Thing)
                    && context.Thing.def.rotateInShelves
                    && !context.Thing.def.IsWeapon
                    && !context.Thing.def.IsApparel
                    && !(context.Thing.Graphic is Graphic_Multi);
            }

            public Quaternion ModifyRotation(RenderContext context, Quaternion currentRotation)
            {
                Rot4 shelfRot4 = GetShelfRotation(context);
                Quaternion shelfRotationQuat = Quaternion.AngleAxis(shelfRot4.AsAngle, Vector3.up);

                return shelfRotationQuat * ShelfConstants.GeneralRelativeRotation;
            }
        }

        [DecoratorPriority(ReplicatorConstants.Priority_ContextScale + 10)]
        public class MultiItemScaleDecorator : BaseDecorator, IScaleDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_ContextScale + 10;

            public override bool CanApply(RenderContext context)
            {
                return IsInStorage(context.Thing) && HasMultipleItemsInSameCell(context.Thing);
            }

            public Vector3 ModifyScale(RenderContext context, Vector3 currentScale)
            {
                return currentScale * ShelfConstants.MultiItemsPerCellDrawSizeFactor;
            }
        }
    }
}

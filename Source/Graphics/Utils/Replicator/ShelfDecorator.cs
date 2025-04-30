using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator
{
    public class ShelfDecorator : BaseDecorator
    {
        private const float MultiItemsPerCellDrawSizeFactor = 0.8f;
        private const float DefaultMaxRandomAngle = 35f;
        private const float RandomAngleMultiplier = 542f;
        private const float HorizontalApparelRotationAngle = 90f;
        private const float WeaponStackRotationAngle = -90f;
        private const int MinItemsForStackRotation = 2;

#if DEBUG
        [TweakValue("PressR.Replicator", 0f, 1f)]
        private static bool EnableShelfDecorator = true;

        [TweakValue("PressR.Replicator", 0f, 1f)]
        private static bool EnableGraphicMultiDecorator = true;

        [TweakValue("PressR.Replicator", 0f, 1f)]
        private static bool EnableWeaponRotationDecorator = true;

        [TweakValue("PressR.Replicator", 0f, 1f)]
        private static bool EnableApparelRotationDecorator = true;
#endif

        private readonly List<IRotationDecorator> _rotationDecorators;

        public ShelfDecorator()
        {
            _rotationDecorators = new List<IRotationDecorator>
            {
                new WeaponRotationDecorator(),
                new ApparelRotationDecorator(),
                new GraphicMultiDecorator(),
            };
        }

        public override string GetDecoratorName() => "ShelfDecorator";

        public override bool IsEnabled()
        {
#if DEBUG
            return EnableShelfDecorator;
#else
            return true;
#endif
        }

        public override bool CanApply(Thing thing)
        {
#if DEBUG
            if (!EnableShelfDecorator)
                return false;
#endif

            return thing.def.category == ThingCategory.Item
                && thing.Spawned
                && IsInStorage(thing)
                && !(thing is Book);
        }

        public override ThingRenderData Decorate(ThingRenderData renderData, Thing thing)
        {
#if DEBUG
            if (!EnableShelfDecorator)
                return renderData;
#endif

            if (
                !(thing.Position.GetEdifice(thing.Map) is Building_Storage storage)
                || thing is Book
            )
                return renderData;

            Quaternion rotation = GetShelfRotation(thing, storage, renderData.Matrix.rotation);

            Vector3 scale = GetShelfScale(thing, renderData.Matrix.lossyScale);

            Vector3 position = renderData.Matrix.GetColumn(3);
            Matrix4x4 newMatrix = Matrix4x4.TRS(position, rotation, scale);

            renderData.Matrix = newMatrix;

            return renderData;
        }

        private Quaternion GetShelfRotation(
            Thing thing,
            Building_Storage storage,
            Quaternion currentRotation
        )
        {
            Rot4 shelfRotation = storage.Rotation;

            foreach (var decorator in _rotationDecorators)
            {
#if DEBUG
                bool isEnabled = decorator.GetDecoratorName() switch
                {
                    "GraphicMultiDecorator" => EnableGraphicMultiDecorator,
                    "WeaponRotationDecorator" => EnableWeaponRotationDecorator,
                    "ApparelRotationDecorator" => EnableApparelRotationDecorator,
                    _ => true,
                };

                if (!isEnabled)
                    continue;
#endif

                if (decorator.CanApply(thing, shelfRotation))
                {
                    return decorator.Decorate(thing, shelfRotation, currentRotation);
                }
            }

            return currentRotation;
        }

        private Vector3 GetShelfScale(Thing thing, Vector3 currentScale)
        {
            if (thing is Book)
            {
                return currentScale;
            }

            Vector3 scale = currentScale;
            if (thing.Position.GetItemCount(thing.Map) >= MinItemsForStackRotation)
            {
                scale *= MultiItemsPerCellDrawSizeFactor;
            }
            return scale;
        }

        private interface IRotationDecorator
        {
            bool CanApply(Thing thing, Rot4 shelfRotation);
            Quaternion Decorate(Thing thing, Rot4 shelfRotation, Quaternion currentRotation);
            string GetDecoratorName();
        }

        private abstract class BaseRotationDecorator : IRotationDecorator
        {
            public virtual bool CanApply(Thing thing, Rot4 shelfRotation)
            {
                return true;
            }

            public virtual Quaternion Decorate(
                Thing thing,
                Rot4 shelfRotation,
                Quaternion currentRotation
            )
            {
                return currentRotation;
            }

            public abstract string GetDecoratorName();
        }

        private class ApparelRotationDecorator : BaseRotationDecorator
        {
            public override bool CanApply(Thing thing, Rot4 shelfRotation)
            {
                return thing.def.IsApparel;
            }

            public override Quaternion Decorate(
                Thing thing,
                Rot4 shelfRotation,
                Quaternion currentRotation
            )
            {
                float randomRot = ReplicatorHelper.GetRandomRotationAngle(
                    thing.Graphic,
                    thing,
                    DefaultMaxRandomAngle,
                    RandomAngleMultiplier
                );

                if (thing.Graphic is Graphic_RandomRotated)
                {
                    return Quaternion.AngleAxis(randomRot, Vector3.up);
                }
                else
                {
                    return shelfRotation.IsHorizontal
                        ? Quaternion.identity
                        : Quaternion.AngleAxis(HorizontalApparelRotationAngle, Vector3.up);
                }
            }

            public override string GetDecoratorName() => "ApparelRotationDecorator";
        }

        private class WeaponRotationDecorator : BaseRotationDecorator
        {
            public override bool CanApply(Thing thing, Rot4 shelfRotation)
            {
                return thing.def.IsWeapon
                    && !thing.def.defName.Contains("Wood")
                    && thing.def.rotateInShelves;
            }

            public override Quaternion Decorate(
                Thing thing,
                Rot4 shelfRotation,
                Quaternion currentRotation
            )
            {
                int itemCount = thing.Position.GetItemCount(thing.Map);

                if (itemCount < MinItemsForStackRotation)
                {
                    float randomRot = ReplicatorHelper.GetRandomRotationAngle(
                        thing.Graphic,
                        thing,
                        DefaultMaxRandomAngle,
                        RandomAngleMultiplier
                    );

                    if (thing.Graphic is Graphic_RandomRotated)
                    {
                        return Quaternion.AngleAxis(randomRot, Vector3.up);
                    }

                    return currentRotation;
                }

                return Quaternion.AngleAxis(WeaponStackRotationAngle, Vector3.up);
            }

            public override string GetDecoratorName() => "WeaponRotationDecorator";
        }

        private class GraphicMultiDecorator : BaseRotationDecorator
        {
            public override bool CanApply(Thing thing, Rot4 shelfRotation)
            {
                return thing.Graphic is Graphic_Multi
                    && !thing.def.IsApparel
                    && !thing.def.IsWeapon;
            }

            public override Quaternion Decorate(
                Thing thing,
                Rot4 shelfRotation,
                Quaternion currentRotation
            )
            {
                return shelfRotation.AsQuat;
            }

            public override string GetDecoratorName() => "GraphicMultiDecorator";
        }
    }
}

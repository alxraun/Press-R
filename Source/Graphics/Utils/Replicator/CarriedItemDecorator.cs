using System.Collections.Generic;
using System.Reflection;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator
{
    public class CarriedItemDecorator : BaseDecorator
    {
        private static readonly Vector3 BaseCarriedOffset = new Vector3(0.0f, 0.0f, -0.1f);
        private const float BaseCarriedNorthZOffset = 0.0f;
        private const float BaseCarriedEastXOffset = 0.18f;
        private const float BaseCarriedWestXOffset = -0.18f;
        private const float ChildOffsetZ = -0.1f;
        private const float DefaultMaxRandomAngle = 35f;
        private const float RandomAngleMultiplier = 542f;

#if DEBUG
        [TweakValue("PressR.Replicator", 0f, 1f)]
        private static bool EnableCarriedItemDecorator = true;
#endif

        private readonly List<ICarriedItemDecoratorPart> _decoratorParts;

        public CarriedItemDecorator()
        {
            _decoratorParts = new List<ICarriedItemDecoratorPart>
            {
                new CarriedItemPositionOffsetDecorator(),
                new CarriedItemCorpseRotationDecorator(),
                new CarriedItemRandomRotatedRotationDecorator(),
                new CarriedItemPawnRotationDecorator(),
            };
        }

        public override string GetDecoratorName() => "CarriedItemDecorator";

        public override bool IsEnabled()
        {
#if DEBUG
            return EnableCarriedItemDecorator;
#else
            return true;
#endif
        }

        public override bool CanApply(Thing thing)
        {
#if DEBUG
            if (!EnableCarriedItemDecorator)
                return false;
#endif

            return thing?.ParentHolder is Pawn_CarryTracker;
        }

        private interface ICarriedItemDecoratorPart
        {
            ThingRenderData DecoratePart(ThingRenderData renderData, Thing thing, Pawn pawn);
            string GetDecoratorPartName();
        }

        private abstract class BaseCarriedItemDecoratorPart : ICarriedItemDecoratorPart
        {
            public abstract string GetDecoratorPartName();

            public virtual ThingRenderData DecoratePart(
                ThingRenderData renderData,
                Thing thing,
                Pawn pawn
            )
            {
                return renderData;
            }
        }

        private class CarriedItemCorpseRotationDecorator : BaseCarriedItemDecoratorPart
        {
            public override string GetDecoratorPartName() => "CarriedItemCorpseRotationDecorator";

            public override ThingRenderData DecoratePart(
                ThingRenderData renderData,
                Thing thing,
                Pawn pawn
            )
            {
                if (thing is Corpse)
                {
                    return renderData;
                }
                return renderData;
            }
        }

        private class CarriedItemRandomRotatedRotationDecorator : BaseCarriedItemDecoratorPart
        {
            public override string GetDecoratorPartName() =>
                "CarriedItemRandomRotatedRotationDecorator";

            public override ThingRenderData DecoratePart(
                ThingRenderData renderData,
                Thing thing,
                Pawn pawn
            )
            {
                float randomAngle = ReplicatorHelper.GetRandomRotationAngle(
                    thing.Graphic,
                    thing,
                    DefaultMaxRandomAngle,
                    RandomAngleMultiplier
                );

                if (randomAngle != 0f)
                {
                    Quaternion finalRot = Quaternion.AngleAxis(randomAngle, Vector3.up);
                    Matrix4x4 newMatrix = Matrix4x4.TRS(
                        renderData.Matrix.GetColumn(3),
                        finalRot,
                        renderData.Matrix.lossyScale
                    );
                    renderData.Matrix = newMatrix;
                }
                return renderData;
            }
        }

        private class CarriedItemPawnRotationDecorator : BaseCarriedItemDecoratorPart
        {
            public override string GetDecoratorPartName() => "CarriedItemPawnRotationDecorator";

            public override ThingRenderData DecoratePart(
                ThingRenderData renderData,
                Thing thing,
                Pawn pawn
            )
            {
                if (!(thing is Corpse) && !(thing.Graphic is Graphic_RandomRotated))
                {
                    return renderData;
                }

                return renderData;
            }
        }

        private class CarriedItemPositionOffsetDecorator : BaseCarriedItemDecoratorPart
        {
            private const float VanillaCarriedYOffsetValue = 0.03846154f;
            private const float NorthAltitudeIncrementDivisor = 2f;

            public override string GetDecoratorPartName() => "CarriedItemPositionOffsetDecorator";

            public override ThingRenderData DecoratePart(
                ThingRenderData renderData,
                Thing thing,
                Pawn pawn
            )
            {
                Vector3 currentOffset = BaseCarriedOffset;
                Rot4 pawnRotation = pawn.Rotation;
                switch (pawnRotation.AsInt)
                {
                    case 0:
                        currentOffset.z = BaseCarriedNorthZOffset;
                        break;
                    case 1:
                        currentOffset.x = BaseCarriedEastXOffset;
                        break;
                    case 3:
                        currentOffset.x = BaseCarriedWestXOffset;
                        break;
                }
                if (pawn.DevelopmentalStage == DevelopmentalStage.Child)
                {
                    currentOffset.z += ChildOffsetZ;
                }
                float finalX = pawn.DrawPos.x + currentOffset.x;
                float finalZ = pawn.DrawPos.z + currentOffset.z;

                float baseOriginalItemY = pawn.DrawPos.y;
                float targetY;

                if (pawnRotation == Rot4.North)
                {
                    baseOriginalItemY -= VanillaCarriedYOffsetValue;
                    targetY = baseOriginalItemY;
                }
                else
                {
                    baseOriginalItemY += VanillaCarriedYOffsetValue;
                    targetY = baseOriginalItemY + Altitudes.AltInc;
                }

                Vector3 finalPos = new Vector3(finalX, targetY, finalZ);

                Matrix4x4 newMatrix = Matrix4x4.TRS(
                    finalPos,
                    renderData.Matrix.rotation,
                    renderData.Matrix.lossyScale
                );
                renderData.Matrix = newMatrix;
                return renderData;
            }
        }

        public override ThingRenderData Decorate(ThingRenderData renderData, Thing thing)
        {
#if DEBUG
            if (!EnableCarriedItemDecorator)
                return renderData;
#endif

            if (!(thing.ParentHolder is Pawn_CarryTracker carryTracker))
                return renderData;

            Pawn pawn = carryTracker.pawn;
            if (pawn == null)
                return renderData;

            var currentRenderData = renderData;
            foreach (var decoratorPart in _decoratorParts)
            {
                currentRenderData = decoratorPart.DecoratePart(currentRenderData, thing, pawn);
            }

            return currentRenderData;
        }
    }
}

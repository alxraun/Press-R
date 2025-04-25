using System;
using System.Linq;
using PressR.Graphics.Utils.Replicator2.Core;
using PressR.Graphics.Utils.Replicator2.Interfaces;
using PressR.Graphics.Utils.Replicator2.Registry;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator2
{
    public static class ThingRenderDataReplicator2
    {
#if DEBUG
        private static float GlobalAltitudeOffset = 0f;
#else
        private const float GlobalAltitudeOffset = 0f;
#endif

        public static RenderData GetRenderData(Thing thing)
        {
            if (thing == null || thing.Graphic == null)
            {
                return null;
            }

            var strategy = RenderStrategyFactory2.GetStrategy(thing);
            Rot4 baseRot = thing.Rotation;

            Mesh baseMesh = null;
            Material baseMat = null;
            Vector3 baseOffset = Vector3.zero;
            Quaternion baseRotQuat = Quaternion.identity;
            Vector3 baseScale = Vector3.one;

            RenderContext context = null;

            try
            {
                context = new RenderContext(
                    thing,
                    baseRot,
                    thing.DrawPos,
                    Quaternion.identity,
                    Vector3.one,
                    null,
                    null
                );

                baseMesh = strategy.GetBaseMesh(context);
                baseMat = strategy.GetBaseMaterial(context);
                baseOffset = strategy.GetBaseOffset(context);
                baseRotQuat = strategy.GetBaseRotation(context);
                baseScale = strategy.GetBaseScale(context);

                context.CurrentMesh = baseMesh;
                context.CurrentMaterial = baseMat;
                context.CurrentPos += baseOffset;
                context.CurrentRot = baseRotQuat;
                context.CurrentScale = baseScale;

                if (baseMesh == null || baseMat == null)
                {
                    return null;
                }

                Vector3 positionOffsetDeltaSum = Vector3.zero;

                foreach (var decorator in DecoratorRegistry2.AllSortedDecorators)
                {
                    if (decorator.IsEnabled() && decorator.CanApply(context))
                    {
#if DEBUG

                        context.RecordAppliedDecorator(decorator);

#endif
                        if (decorator is IPositionOffsetDecorator posDecorator)
                        {
                            positionOffsetDeltaSum += posDecorator.GetPositionOffsetDelta(context);
                        }
                        if (decorator is IRotationDecorator rotDecorator)
                        {
                            context.CurrentRot = rotDecorator.ModifyRotation(
                                context,
                                context.CurrentRot
                            );
                        }
                        if (decorator is IScaleDecorator scaleDecorator)
                        {
                            context.CurrentScale = scaleDecorator.ModifyScale(
                                context,
                                context.CurrentScale
                            );
                        }
                        if (decorator is IMeshDecorator meshDecorator)
                        {
                            context.CurrentMesh = meshDecorator.ModifyMesh(
                                context,
                                context.CurrentMesh
                            );
                        }
                        if (decorator is IMaterialDecorator matDecorator)
                        {
                            context.CurrentMaterial = matDecorator.ModifyMaterial(
                                context,
                                context.CurrentMaterial
                            );
                        }
                    }
                }

                context.CurrentPos += positionOffsetDeltaSum;

                Vector3 finalPos = context.CurrentPos;
                finalPos.y += GlobalAltitudeOffset;
                context.CurrentPos = finalPos;

                if (context.CurrentMesh == null || context.CurrentMaterial == null)
                {
                    return null;
                }

                Matrix4x4 matrix = Matrix4x4.TRS(
                    context.CurrentPos,
                    context.CurrentRot,
                    context.CurrentScale
                );

                return new RenderData(context.CurrentMesh, matrix, context.CurrentMaterial);
            }
            catch (System.Exception)
            {
#if DEBUG
                if (context?.AppliedDecoratorNames != null && context.AppliedDecoratorNames.Any())
                { }
#endif
                return null;
            }
        }
    }
}

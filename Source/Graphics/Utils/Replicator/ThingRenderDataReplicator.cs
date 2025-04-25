using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using PressR.Graphics.Utils.Replicator;
using PressR.Interfaces;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator
{
    public static class ThingRenderDataReplicator
    {
        private const float YPositionChangeThreshold = 0.001f;

        private const float YPositionResetThreshold = 0.0001f;

#if DEBUG
        [TweakValue("PressR.Replicator.Altitude", -1f, 1f)]
        private static float GlobalAltitudeOffset = 0f;
#else
        private const float GlobalAltitudeOffset = 0f;
#endif

        public static ThingRenderData GetRenderData(
            Thing thing,
            bool returnOriginalMaterial = false
        )
        {
            if (thing?.Graphic == null)
            {
                return new ThingRenderData(null, Matrix4x4.identity, null);
            }

            Rot4 rot = thing.Rotation;
            Vector3 drawPos = thing.DrawPos;
            const float extraRotation = 0f;

            IRenderDataReplicatorStrategy strategy = RenderStrategyFactory.GetStrategy(
                thing.Graphic,
                thing
            );

            Mesh initialMeshRef = strategy.GetMesh(thing, rot);
            Material initialMaterialRef = strategy.GetMaterial(thing, rot);

            if (initialMeshRef == null || initialMaterialRef == null)
            {
                return new ThingRenderData(null, Matrix4x4.identity, null);
            }

            Quaternion quat = strategy.GetRotation(thing, rot, extraRotation);
            Vector3 positionOffset = strategy.GetPositionOffset(thing, rot);
            Vector3 scale = strategy.GetScale(thing);

            Vector3 baseDrawPos = drawPos + positionOffset;

            Matrix4x4 initialMatrix = Matrix4x4.TRS(baseDrawPos, quat, scale);

            var tempRenderData = new ThingRenderData(
                initialMeshRef,
                initialMatrix,
                initialMaterialRef
            );

            foreach (var decorator in DecoratorRegistry.GetApplicableDecorators(thing))
            {
                tempRenderData = decorator.Decorate(tempRenderData, thing);
            }

            Mesh finalMeshRef = tempRenderData.Mesh;
            Material finalMaterialRef = tempRenderData.Material;
            Matrix4x4 matrixAfterDecorators = tempRenderData.Matrix;

            Material finalMaterialToReturn = returnOriginalMaterial
                ? finalMaterialRef
                : (finalMaterialRef != null ? new Material(finalMaterialRef) : null);

            Vector3 finalPos = matrixAfterDecorators.GetColumn(3);
            Quaternion finalRot = matrixAfterDecorators.rotation;
            Vector3 finalScale = matrixAfterDecorators.lossyScale;

            float finalBaseY = baseDrawPos.y;
            float currentY = finalPos.y;
            float deltaY = currentY - finalBaseY;

            float targetY = finalBaseY + GlobalAltitudeOffset;

            if (Math.Abs(deltaY) > YPositionChangeThreshold)
            {
                targetY += deltaY;
            }
            else if (Math.Abs(currentY - targetY) > YPositionResetThreshold) { }

            finalPos.y = targetY;

            Matrix4x4 finalMatrix = Matrix4x4.TRS(finalPos, finalRot, finalScale);

            return new ThingRenderData(finalMeshRef, finalMatrix, finalMaterialToReturn);
        }
    }
}

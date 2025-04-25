using PressR.Graphics.Utils.Replicator2.Core;
using PressR.Graphics.Utils.Replicator2.Interfaces;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator2.Decorators.Pawn
{
    public static class PawnApparelDecorators
    {
        [DecoratorPriority(ReplicatorConstants.Priority_ContextRotation + 60)]
        public class ApparelRotationDecorator : BaseDecorator, IRotationDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_ContextRotation + 60;

            public override bool CanApply(RenderContext context)
            {
                return context.Thing is Apparel apparel
                    && apparel.ParentHolder is Pawn_ApparelTracker
                    && !apparel.def.apparel.LastLayer.IsUtilityLayer;
            }

            public Quaternion ModifyRotation(RenderContext context, Quaternion currentRotation)
            {
                Verse.Pawn wearer =
                    (context.Thing.ParentHolder as Pawn_ApparelTracker)?.pawn ?? context.Pawn;

                if (wearer != null)
                {
                    float bodyAngle = wearer.Drawer.renderer.BodyAngle(PawnRenderFlags.None);
                    return Quaternion.AngleAxis(bodyAngle, Vector3.up);
                }
                return currentRotation;
            }
        }

        [DecoratorPriority(ReplicatorConstants.Priority_ContextOffset + 60)]
        public class ApparelWornGraphicDataOffsetDecorator : BaseDecorator, IPositionOffsetDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_ContextOffset + 60;

            public override bool CanApply(RenderContext context)
            {
                return context.Thing is Apparel apparel
                    && apparel.ParentHolder is Pawn_ApparelTracker
                    && apparel.def.apparel.wornGraphicData != null
                    && !apparel.RenderAsPack();
            }

            public Vector3 GetPositionOffsetDelta(RenderContext context)
            {
                Verse.Pawn wearer =
                    (context.Thing.ParentHolder as Pawn_ApparelTracker)?.pawn ?? context.Pawn;

                if (
                    context.Thing is Apparel apparel
                    && wearer != null
                    && apparel.def.apparel.wornGraphicData != null
                )
                {
                    Vector2 beltOffset = apparel.def.apparel.wornGraphicData.BeltOffsetAt(
                        wearer.Rotation,
                        wearer.story.bodyType
                    );

                    return new Vector3(beltOffset.x, 0f, beltOffset.y);
                }
                return Vector3.zero;
            }
        }

        [DecoratorPriority(ReplicatorConstants.Priority_ContextScale + 60)]
        public class ApparelWornGraphicDataScaleDecorator : BaseDecorator, IScaleDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_ContextScale + 60;

            public override bool CanApply(RenderContext context)
            {
                return context.Thing is Apparel apparel
                    && apparel.ParentHolder is Pawn_ApparelTracker
                    && apparel.def.apparel.wornGraphicData != null
                    && !apparel.RenderAsPack();
            }

            public Vector3 ModifyScale(RenderContext context, Vector3 currentScale)
            {
                Verse.Pawn wearer =
                    (context.Thing.ParentHolder as Pawn_ApparelTracker)?.pawn ?? context.Pawn;

                if (
                    context.Thing is Apparel apparel
                    && wearer != null
                    && apparel.def.apparel.wornGraphicData != null
                )
                {
                    Vector2 beltScale = apparel.def.apparel.wornGraphicData.BeltScaleAt(
                        wearer.Rotation,
                        wearer.story.bodyType
                    );

                    return new Vector3(
                        currentScale.x * beltScale.x,
                        currentScale.y,
                        currentScale.z * beltScale.y
                    );
                }
                return currentScale;
            }
        }

        [DecoratorPriority(ReplicatorConstants.Priority_ContextScale + 65)]
        public class ApparelDrawDataScaleDecorator : BaseDecorator, IScaleDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_ContextScale + 65;

            public override bool CanApply(RenderContext context)
            {
                return context.Thing is Apparel apparel
                    && apparel.ParentHolder is Pawn_ApparelTracker
                    && apparel.def.apparel.drawData != null;
            }

            public Vector3 ModifyScale(RenderContext context, Vector3 currentScale)
            {
                Verse.Pawn wearer =
                    (context.Thing.ParentHolder as Pawn_ApparelTracker)?.pawn ?? context.Pawn;

                if (
                    context.Thing is Apparel apparel
                    && wearer != null
                    && apparel.def.apparel.drawData != null
                )
                {
                    float drawDataScale = apparel.def.apparel.drawData.ScaleFor(wearer);
                    return currentScale * drawDataScale;
                }
                return currentScale;
            }
        }

        [DecoratorPriority(ReplicatorConstants.Priority_Material + 10)]
        public class ApparelStyleColorDecorator : BaseDecorator, IMaterialDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_Material + 10;

            public override bool CanApply(RenderContext context)
            {
                return context.Thing is Apparel;
            }

            public Material ModifyMaterial(RenderContext context, Material currentMaterial)
            {
                if (context.Thing is Apparel apparel)
                {
                    Color apparelColor = apparel.DrawColor;

                    if (
                        currentMaterial != null
                        && apparelColor != currentMaterial.color
                        && apparelColor.a > 0
                    )
                    {
                        Material coloredMaterial = new Material(currentMaterial);
                        coloredMaterial.color = apparelColor;
                        return coloredMaterial;
                    }
                }
                return currentMaterial;
            }
        }

        [DecoratorPriority(ReplicatorConstants.Priority_Material + 20)]
        public class ApparelCorpseColorDecorator : BaseDecorator, IMaterialDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_Material + 20;

            public override bool CanApply(RenderContext context)
            {
                return context.Thing is Apparel apparel && apparel.WornByCorpse;
            }

            public Material ModifyMaterial(RenderContext context, Material currentMaterial)
            {
                if (
                    currentMaterial != null
                    && context.Thing is Apparel apparel
                    && apparel.WornByCorpse
                )
                {
                    Color rottenColor = PawnRenderUtility.GetRottenColor(currentMaterial.color);
                    if (rottenColor != currentMaterial.color)
                    {
                        Material rottenMaterial = new Material(currentMaterial);
                        rottenMaterial.color = rottenColor;
                        return rottenMaterial;
                    }
                }
                return currentMaterial;
            }
        }
    }
}

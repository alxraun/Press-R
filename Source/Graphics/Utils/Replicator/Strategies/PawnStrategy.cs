using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using PressR.Interfaces;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator
{
    [StaticConstructorOnStartup]
    public class PawnStrategy : BaseRenderStrategy
    {
        private readonly List<IPawnRenderDecorator> _decorators;

        private const float HumanlikeBodyBaseWidth = 1.5f;
        private const float OverlayScaleFactor = 1.0f;
        private const float CorpseYOffset = 0.04f;
        private const int FreshRenderQueue = 3600;
        private const int DessicatedRenderQueue = 3650;
        private const float HumanlikeBaseYOffset = -0.06f;

#if DEBUG
        [TweakValue("PressR.Replicator", 0f, 1f)]
        private static bool EnablePawnStrategy = true;
#else
        private static bool EnablePawnStrategy = true;
#endif

        private interface IPawnRenderDecorator
        {
            bool CanApply(Thing thing, Pawn pawn);
            ThingRenderData Decorate(ThingRenderData renderData, Thing thing, Pawn pawn);
            string GetDecoratorName();
            bool IsEnabled();
        }

        private abstract class BasePawnRenderDecorator : IPawnRenderDecorator
        {
            public virtual bool CanApply(Thing thing, Pawn pawn) => true;

            public virtual ThingRenderData Decorate(
                ThingRenderData renderData,
                Thing thing,
                Pawn pawn
            )
            {
                return renderData;
            }

            public abstract string GetDecoratorName();
            public abstract bool IsEnabled();
        }

        [StaticConstructorOnStartup]
        private class PawnHumanoidTransparencyDecorator : BasePawnRenderDecorator
        {
#if DEBUG
            [TweakValue("PressR.Replicator", 0f, 1f)]
            public static bool EnablePawnHumanoidTransparency = true;
#else
            public static bool EnablePawnHumanoidTransparency = true;
#endif

            public override string GetDecoratorName() => "PawnHumanoidTransparencyDecorator";

            public override bool IsEnabled() => EnablePawnHumanoidTransparency;

            public override bool CanApply(Thing thing, Pawn pawn)
            {
                return pawn != null && pawn.RaceProps.Humanlike;
            }

            public override ThingRenderData Decorate(
                ThingRenderData renderData,
                Thing thing,
                Pawn pawn
            )
            {
                return renderData;
            }
        }

        private class PawnCorpseRotationDecorator : BasePawnRenderDecorator
        {
#if DEBUG
            [TweakValue("PressR.Replicator", 0f, 1f)]
            public static bool EnablePawnCorpseRotationDecorator = true;
#else
            public static bool EnablePawnCorpseRotationDecorator = true;
#endif

            public override string GetDecoratorName() => "PawnCorpseRotationDecorator";

            public override bool IsEnabled() => EnablePawnCorpseRotationDecorator;

            public override bool CanApply(Thing thing, Pawn pawn) => thing is Corpse;

            public override ThingRenderData Decorate(
                ThingRenderData renderData,
                Thing thing,
                Pawn pawn
            )
            {
                if (pawn.Drawer?.renderer?.wiggler == null)
                    return renderData;

                Quaternion rotation = Quaternion.AngleAxis(
                    pawn.Drawer.renderer.wiggler.downedAngle,
                    Vector3.up
                );
                Matrix4x4 newMatrix = Matrix4x4.TRS(
                    renderData.Matrix.GetColumn(3),
                    rotation,
                    renderData.Matrix.lossyScale
                );
                renderData.Matrix = newMatrix;
                return renderData;
            }
        }

        private class PawnPostureRotationDecorator : BasePawnRenderDecorator
        {
#if DEBUG
            [TweakValue("PressR.Replicator", 0f, 1f)]
            public static bool EnablePawnPostureRotationDecorator = true;
#else
            public static bool EnablePawnPostureRotationDecorator = true;
#endif

            public override string GetDecoratorName() => "PawnPostureRotationDecorator";

            public override bool IsEnabled() => EnablePawnPostureRotationDecorator;

            public override bool CanApply(Thing thing, Pawn pawn) =>
                pawn.GetPosture() != PawnPosture.Standing;

            public override ThingRenderData Decorate(
                ThingRenderData renderData,
                Thing thing,
                Pawn pawn
            )
            {
                float angle = pawn.Drawer.renderer.BodyAngle(PawnRenderFlags.None);
                Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);

                Matrix4x4 newMatrix = Matrix4x4.TRS(
                    renderData.Matrix.GetColumn(3),
                    rotation,
                    renderData.Matrix.lossyScale
                );
                renderData.Matrix = newMatrix;
                return renderData;
            }
        }

        private class PawnCorpsePositionDecorator : BasePawnRenderDecorator
        {
            private const float CorpseBaseYOffset = 0.04f;
            private const float CorpseDessicatedYOffsetAddition = 0.025f;
            private const float CorpseRottingYOffsetAddition = 0.015f;

#if DEBUG
            [TweakValue("PressR.Replicator", 0f, 1f)]
            public static bool EnablePawnCorpsePositionDecorator = true;
#else
            public static bool EnablePawnCorpsePositionDecorator = true;
#endif

            public override string GetDecoratorName() => "PawnCorpsePositionDecorator";

            public override bool IsEnabled() => EnablePawnCorpsePositionDecorator;

            public override bool CanApply(Thing thing, Pawn pawn) => thing is Corpse;

            public override ThingRenderData Decorate(
                ThingRenderData renderData,
                Thing thing,
                Pawn pawn
            )
            {
                Vector3 position = renderData.Matrix.GetColumn(3);
                position.y += CorpseYOffset;
                if ((thing as Corpse).CurRotDrawMode != RotDrawMode.Fresh)
                {
                    position.y +=
                        ((thing as Corpse).CurRotDrawMode == RotDrawMode.Dessicated)
                            ? 0.025f
                            : 0.015f;
                }
                Matrix4x4 newMatrix = Matrix4x4.TRS(
                    position,
                    renderData.Matrix.rotation,
                    renderData.Matrix.lossyScale
                );
                renderData.Matrix = newMatrix;
                return renderData;
            }
        }

        private class PawnBedPositionDecorator : BasePawnRenderDecorator
        {
            private const float PawnInBedYOffset = 0.025f;

#if DEBUG
            [TweakValue("PressR.Replicator", 0f, 1f)]
            public static bool EnablePawnBedPositionDecorator = true;
#else
            public static bool EnablePawnBedPositionDecorator = true;
#endif

            public override string GetDecoratorName() => "PawnBedPositionDecorator";

            public override bool IsEnabled() => EnablePawnBedPositionDecorator;

            public override bool CanApply(Thing thing, Pawn pawn) =>
                pawn.GetPosture() != PawnPosture.Standing && pawn.CurrentBed() != null;

            public override ThingRenderData Decorate(
                ThingRenderData renderData,
                Thing thing,
                Pawn pawn
            )
            {
                Vector3 position = renderData.Matrix.GetColumn(3);
                position.y += 0.025f;
                Matrix4x4 newMatrix = Matrix4x4.TRS(
                    position,
                    renderData.Matrix.rotation,
                    renderData.Matrix.lossyScale
                );
                renderData.Matrix = newMatrix;
                return renderData;
            }
        }

        private class PawnCarriedPositionDecorator : BasePawnRenderDecorator
        {
            private const float CarriedPawnYOffset = 0.035f;

#if DEBUG
            [TweakValue("PressR.Replicator", 0f, 1f)]
            public static bool EnablePawnCarriedPositionDecorator = true;
#else
            public static bool EnablePawnCarriedPositionDecorator = true;
#endif

            public override string GetDecoratorName() => "PawnCarriedPositionDecorator";

            public override bool IsEnabled() => EnablePawnCarriedPositionDecorator;

            public override bool CanApply(Thing thing, Pawn pawn) =>
                pawn.ParentHolder is Pawn_CarryTracker;

            public override ThingRenderData Decorate(
                ThingRenderData renderData,
                Thing thing,
                Pawn pawn
            )
            {
                Vector3 position = renderData.Matrix.GetColumn(3);
                position.y += 0.035f;
                Matrix4x4 newMatrix = Matrix4x4.TRS(
                    position,
                    renderData.Matrix.rotation,
                    renderData.Matrix.lossyScale
                );
                renderData.Matrix = newMatrix;
                return renderData;
            }
        }

        public PawnStrategy()
        {
            _decorators = new List<IPawnRenderDecorator>
            {
                new PawnHumanoidTransparencyDecorator(),
                new PawnCorpseRotationDecorator(),
                new PawnPostureRotationDecorator(),
                new PawnCorpsePositionDecorator(),
                new PawnBedPositionDecorator(),
                new PawnCarriedPositionDecorator(),
            };
        }

        public override string GetStrategyName() => "PawnStrategy";

        public override bool IsEnabled() => EnablePawnStrategy;

        public override bool CanHandle(Thing thing) => thing is Pawn || thing is Corpse;

        private Pawn GetPawn(Thing thing) =>
            thing is Pawn pawn ? pawn : (thing is Corpse corpse ? corpse.InnerPawn : null);

        public override Mesh GetMesh(Thing thing, Rot4 rot)
        {
            Pawn pawn = GetPawn(thing);
            if (pawn == null)
                return base.GetMesh(thing, rot);

            if (!pawn.RaceProps.Humanlike)
            {
                if (thing is Corpse && pawn.Drawer?.renderer?.renderTree?.BodyGraphic != null)
                {
                    Mesh corpseAnimalMesh = pawn.Drawer.renderer.renderTree.BodyGraphic.MeshAt(
                        pawn.Drawer.renderer.LayingFacing()
                    );
                    return ApplyDecorators(
                        new ThingRenderData(corpseAnimalMesh, Matrix4x4.identity, null),
                        thing,
                        pawn,
                        DecoratorTarget.Mesh
                    ).Mesh;
                }
                Mesh animalMesh = pawn.Graphic.MeshAt(rot);
                return ApplyDecorators(
                    new ThingRenderData(animalMesh, Matrix4x4.identity, null),
                    thing,
                    pawn,
                    DecoratorTarget.Mesh
                ).Mesh;
            }

            Rot4 facing = GetFacing(thing, pawn, rot);
            Mesh humanoidMesh = HumanlikeMeshPoolUtility
                .GetHumanlikeBodySetForPawn(pawn)
                .MeshAt(facing);
            return ApplyDecorators(
                new ThingRenderData(humanoidMesh, Matrix4x4.identity, null),
                thing,
                pawn,
                DecoratorTarget.Mesh
            ).Mesh;
        }

        public override Material GetMaterial(Thing thing, Rot4 rot)
        {
            Pawn pawn = GetPawn(thing);
            if (pawn == null)
                return base.GetMaterial(thing, rot);

            Material baseMat = null;
            if (!pawn.RaceProps.Humanlike)
            {
                Rot4 facing = GetFacing(thing, pawn, rot);
                if (thing is Corpse && pawn.Drawer?.renderer?.renderTree?.BodyGraphic != null)
                {
                    baseMat = pawn.Drawer.renderer.renderTree.BodyGraphic.MatAt(facing);
                }
                else if (pawn.Graphic != null)
                {
                    Rot4 matRot = (thing is Corpse) ? facing : rot;
                    baseMat = pawn.Graphic.MatAt(matRot);
                }
            }
            else
            {
                RotDrawMode rotDrawMode = GetRotDrawMode(thing);
                Rot4 facing = GetFacing(thing, pawn, rot);
                Graphic bodyGraphic = GetHumanlikeBodyGraphic(pawn, rotDrawMode);
                if (bodyGraphic != null)
                {
                    baseMat = bodyGraphic.MatAt(facing);
                }
            }

            if (baseMat == null)
            {
                baseMat = base.GetMaterial(thing, rot);
            }

            return ApplyDecorators(
                new ThingRenderData(null, Matrix4x4.identity, baseMat),
                thing,
                pawn,
                DecoratorTarget.Material
            ).Material;
        }

        public override Quaternion GetRotation(Thing thing, Rot4 rot, float extraRotation)
        {
            Pawn pawn = GetPawn(thing);
            if (pawn == null)
                return base.GetRotation(thing, rot, extraRotation);

            Quaternion baseRotation = Quaternion.AngleAxis(rot.AsAngle + extraRotation, Vector3.up);

            return ApplyDecorators(
                new ThingRenderData(
                    null,
                    Matrix4x4.TRS(Vector3.zero, baseRotation, Vector3.one),
                    null
                ),
                thing,
                pawn,
                DecoratorTarget.Matrix
            ).Matrix.rotation;
        }

        public override Vector3 GetPositionOffset(Thing thing, Rot4 rot)
        {
            Pawn pawn = GetPawn(thing);
            if (pawn == null)
                return base.GetPositionOffset(thing, rot);

            Vector3 baseOffset = Vector3.zero;

            baseOffset.y += pawn.Drawer.SeededYOffset;

            if (pawn.RaceProps.Humanlike)
            {
                baseOffset.y += HumanlikeBaseYOffset;
            }

            return ApplyDecorators(
                new ThingRenderData(
                    null,
                    Matrix4x4.TRS(baseOffset, Quaternion.identity, Vector3.one),
                    null
                ),
                thing,
                pawn,
                DecoratorTarget.Matrix
            )
                .Matrix.GetColumn(3);
        }

        public override Vector3 GetScale(Thing thing)
        {
            Pawn pawn = GetPawn(thing);
            if (pawn == null)
                return base.GetScale(thing);

            Vector3 baseScale = base.GetScale(thing);

            if (pawn.RaceProps.Humanlike)
            {
                baseScale = Vector3.one;
            }
            return ApplyDecorators(
                new ThingRenderData(
                    null,
                    Matrix4x4.TRS(Vector3.zero, Quaternion.identity, baseScale),
                    null
                ),
                thing,
                pawn,
                DecoratorTarget.Matrix
            ).Matrix.lossyScale;
        }

        private RotDrawMode GetRotDrawMode(Thing thing)
        {
            if (thing is Corpse corpse)
            {
                return corpse.CurRotDrawMode;
            }
            return RotDrawMode.Fresh;
        }

        private Rot4 GetFacing(Thing thing, Pawn pawn, Rot4 rot)
        {
            return thing is Corpse || pawn.GetPosture() != PawnPosture.Standing
                ? pawn.Drawer.renderer.LayingFacing()
                : rot;
        }

        private Graphic GetHumanlikeBodyGraphic(Pawn pawn, RotDrawMode rotDrawMode)
        {
            if (pawn.Drawer?.renderer?.renderTree?.BodyGraphic != null)
            {
                return pawn.Drawer.renderer.renderTree.BodyGraphic;
            }
            else if (rotDrawMode == RotDrawMode.Dessicated && pawn.story?.bodyType != null)
            {
                return GraphicDatabase.Get<Graphic_Multi>(
                    pawn.story.bodyType.bodyDessicatedGraphicPath,
                    ShaderDatabase.Cutout,
                    Vector2.one,
                    Color.white
                );
            }
            else if (pawn.story?.bodyType != null)
            {
                Shader shader = ShaderUtility.GetSkinShader(pawn);
                return GraphicDatabase.Get<Graphic_Multi>(
                    pawn.story.bodyType.bodyNakedGraphicPath,
                    shader,
                    Vector2.one,
                    pawn.story.SkinColor
                );
            }
            return null;
        }

        private enum DecoratorTarget
        {
            Mesh,
            Material,
            Matrix,
        }

        private ThingRenderData ApplyDecorators(
            ThingRenderData initialData,
            Thing thing,
            Pawn pawn,
            DecoratorTarget target
        )
        {
            var currentData = initialData;
            foreach (var decorator in _decorators)
            {
                bool isDecoratorGloballyEnabled = true;
#if DEBUG
                isDecoratorGloballyEnabled = decorator.GetDecoratorName() switch
                {
                    "PawnHumanoidTransparencyDecorator" =>
                        PawnHumanoidTransparencyDecorator.EnablePawnHumanoidTransparency,
                    "PawnCorpseRotationDecorator" =>
                        PawnCorpseRotationDecorator.EnablePawnCorpseRotationDecorator,
                    "PawnPostureRotationDecorator" =>
                        PawnPostureRotationDecorator.EnablePawnPostureRotationDecorator,
                    "PawnCorpsePositionDecorator" =>
                        PawnCorpsePositionDecorator.EnablePawnCorpsePositionDecorator,
                    "PawnBedPositionDecorator" =>
                        PawnBedPositionDecorator.EnablePawnBedPositionDecorator,
                    "PawnCarriedPositionDecorator" =>
                        PawnCarriedPositionDecorator.EnablePawnCarriedPositionDecorator,
                    _ => true,
                };
#endif

                if (
                    isDecoratorGloballyEnabled
                    && decorator.IsEnabled()
                    && decorator.CanApply(thing, pawn)
                )
                {
                    currentData = decorator.Decorate(currentData, thing, pawn);
                }
            }
            return currentData;
        }
    }
}

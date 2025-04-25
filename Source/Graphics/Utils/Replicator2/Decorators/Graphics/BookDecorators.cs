using PressR.Graphics.Utils.Replicator2.Core;
using PressR.Graphics.Utils.Replicator2.Interfaces;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator2.Decorators.Graphics
{
    public static class BookDecorators
    {
        private static class BookConstants
        {
            public const int MinItemsForStackedBookScaleCheck = 2;
            public const float StackedBookScaleMultiplier = 1.0f;
        }

        [DecoratorPriority(ReplicatorConstants.Priority_ContextScale + 10)]
        public class StackedBookScaleDecorator : BaseDecorator, IScaleDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_ContextScale + 10;

            public override bool CanApply(RenderContext context)
            {
                return context.Thing is Book book && !book.IsOpen && IsInStorage(book);
            }

            public Vector3 ModifyScale(RenderContext context, Vector3 currentScale)
            {
                if (
                    context.Thing.Map != null
                    && context.Thing.Position.GetItemCount(context.Thing.Map)
                        >= BookConstants.MinItemsForStackedBookScaleCheck
                )
                {
                    return currentScale * BookConstants.StackedBookScaleMultiplier;
                }
                return currentScale;
            }
        }

        [DecoratorPriority(ReplicatorConstants.Priority_Mesh - 10)]
        public class OpenBookMeshDecorator : BaseDecorator, IMeshDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_Mesh - 10;

            public override bool CanApply(RenderContext context)
            {
                return context.Thing is Book book
                    && book.IsOpen
                    && book.BookComp?.Props?.openGraphic?.Graphic != null;
            }

            public Mesh ModifyMesh(RenderContext context, Mesh currentMesh)
            {
                if (
                    context.Thing is Book book
                    && book.BookComp?.Props?.openGraphic?.Graphic is Graphic openGraphic
                )
                {
                    Rot4 parentRot = Rot4.North;
                    if (book.ParentHolder is Pawn_CarryTracker tracker && tracker.pawn != null)
                    {
                        parentRot = tracker.pawn.Rotation;
                    }

                    return openGraphic.MeshAt(parentRot);
                }
                return currentMesh;
            }
        }

        [DecoratorPriority(ReplicatorConstants.Priority_Material - 10)]
        public class OpenBookMaterialDecorator : BaseDecorator, IMaterialDecorator
        {
            public override int Priority => ReplicatorConstants.Priority_Material - 10;

            public override bool CanApply(RenderContext context)
            {
                return context.Thing is Book book
                    && book.IsOpen
                    && book.BookComp?.Props?.openGraphic?.Graphic != null;
            }

            public Material ModifyMaterial(RenderContext context, Material currentMaterial)
            {
                if (
                    context.Thing is Book book
                    && book.BookComp?.Props?.openGraphic?.Graphic is Graphic openGraphic
                )
                {
                    Rot4 parentRot = Rot4.North;
                    if (book.ParentHolder is Pawn_CarryTracker tracker && tracker.pawn != null)
                    {
                        parentRot = tracker.pawn.Rotation;
                    }

                    return openGraphic.MatAt(parentRot, book);
                }
                return currentMaterial;
            }
        }
    }
}

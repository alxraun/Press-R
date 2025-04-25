using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator
{
    public class OpenBookDecorator : BaseDecorator
    {
#if DEBUG


        public static bool EnableOpenBookDecorator = true;
#endif

        public override string GetDecoratorName()
        {
            return "OpenBookDecorator";
        }

        public override bool IsEnabled()
        {
#if DEBUG
            return EnableOpenBookDecorator;
#else
            return true;
#endif
        }

        public override bool CanApply(Thing thing)
        {
            return thing is Book book && book.IsOpen && book.BookComp?.Props?.openGraphic != null;
        }

        public override ThingRenderData Decorate(ThingRenderData renderData, Thing thing)
        {
            if (!(thing is Book book) || !book.IsOpen)
                return renderData;

            CompBook bookComp = book.BookComp;
            CompProperties_Book bookProps = bookComp?.Props;
            if (bookProps?.openGraphic?.Graphic == null)
                return renderData;

            Graphic openGraphic = bookProps.openGraphic.Graphic;

            Rot4 parentRot = Rot4.North;
            if (book.ParentHolder is Pawn_CarryTracker tracker && tracker.pawn != null)
            {
                parentRot = tracker.pawn.Rotation;
            }

            Mesh mesh = openGraphic.MeshAt(parentRot);
            Material material = openGraphic.MatAt(parentRot, book);

            Vector3 position = renderData.Matrix.GetColumn(3);
            Quaternion rotation = Quaternion.identity;
            Vector3 scale = renderData.Matrix.lossyScale;

            Matrix4x4 newMatrix = Matrix4x4.TRS(position, rotation, scale);

            renderData.Mesh = mesh;
            renderData.Material = material;
            renderData.Matrix = newMatrix;

            return renderData;
        }
    }
}

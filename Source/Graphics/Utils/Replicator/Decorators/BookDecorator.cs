using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator
{
    public class BookDecorator : BaseDecorator
    {
#if DEBUG
        [TweakValue("PressR.Replicator", 0f, 1f)]
        private static bool EnableBookRenderingDecorator = true;
#endif

        private const int MinItemsForScaleCheck = 2;
        private const float StackedBookScaleMultiplier = 1.0f;

        public override string GetDecoratorName() => "BookRenderingDecorator";

        public override bool IsEnabled()
        {
#if DEBUG
            return EnableBookRenderingDecorator;
#else
            return true;
#endif
        }

        public override bool CanApply(Thing thing)
        {
#if DEBUG
            if (!EnableBookRenderingDecorator)
                return false;
#endif

            return thing is Book && thing.Spawned && IsInStorage(thing);
        }

        public override ThingRenderData Decorate(ThingRenderData renderData, Thing thing)
        {
#if DEBUG
            if (!EnableBookRenderingDecorator)
                return renderData;
#endif

            if (!(thing.Position.GetEdifice(thing.Map) is Building_Storage))
                return renderData;

            Vector3 scale = renderData.Matrix.lossyScale;

            if (thing.Position.GetItemCount(thing.Map) >= MinItemsForScaleCheck)
            {
                scale *= StackedBookScaleMultiplier;
            }

            Material material = renderData.Material;
            Mesh mesh = renderData.Mesh;
            Vector3 position = renderData.Matrix.GetColumn(3);

            Quaternion rotation = Quaternion.identity;

            Matrix4x4 newMatrix = Matrix4x4.TRS(position, rotation, scale);
            renderData.Matrix = newMatrix;

            return renderData;
        }
    }
}

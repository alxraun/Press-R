using System.Reflection;
using PressR.Graphics.Utils.Replicator2.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator2.Strategies
{
    public class CollectionStrategy2 : BaseRenderStrategy
    {
        public override bool CanHandle(Thing thing)
        {
            return thing?.Graphic is Graphic_Collection && !(thing.Graphic is Graphic_Linked);
        }

        public override Mesh GetBaseMesh(RenderContext context)
        {
            var subGraphic = GetSubGraphicFor(context.Thing);
            return subGraphic?.MeshAt(context.BaseRot) ?? base.GetBaseMesh(context);
        }

        public override Material GetBaseMaterial(RenderContext context)
        {
            var subGraphic = GetSubGraphicFor(context.Thing);

            return subGraphic?.MatSingleFor(context.Thing) ?? base.GetBaseMaterial(context);
        }

        private Graphic GetSubGraphicFor(Thing thing)
        {
            if (thing == null || !(thing.Graphic is Graphic_Collection graphicCollection))
                return null;

            if (graphicCollection is Graphic_StackCount graphicStackCount)
            {
                return graphicStackCount.SubGraphicFor(thing);
            }
            if (graphicCollection is Graphic_Random graphicRandom)
            {
                return graphicRandom.SubGraphicFor(thing);
            }

            if (graphicCollection is Graphic_Indexed graphicIndexed)
            {
                return graphicIndexed.SubGraphicFor(thing);
            }
            if (graphicCollection is Graphic_MealVariants graphicMealVariants)
            {
                return graphicMealVariants.SubGraphicFor(thing);
            }
            if (graphicCollection is Graphic_Genepack graphicGenepack)
            {
                if (ModsConfig.BiotechActive)
                {
                    return graphicGenepack.SubGraphicFor(thing);
                }
                return null;
            }

            try
            {
                MethodInfo subGraphicForMethodInfo = graphicCollection
                    .GetType()
                    .GetMethod(
                        "SubGraphicFor",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        new[] { typeof(Thing) },
                        null
                    );

                if (subGraphicForMethodInfo != null)
                {
                    return subGraphicForMethodInfo.Invoke(graphicCollection, new object[] { thing })
                        as Graphic;
                }
            }
            catch (System.Exception) { }

            return null;
        }
    }
}

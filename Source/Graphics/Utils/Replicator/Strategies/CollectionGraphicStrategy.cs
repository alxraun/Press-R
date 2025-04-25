using System.Linq;
using System.Reflection;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator
{
    public class CollectionGraphicStrategy : BaseRenderStrategy
    {
#if DEBUG
        [TweakValue("PressR.Replicator", 0f, 1f)]
        private static bool EnableCollectionGraphicStrategy = true;
#endif

        public override string GetStrategyName() => "CollectionGraphicStrategy";

        public override bool IsEnabled()
        {
#if DEBUG
            return EnableCollectionGraphicStrategy;
#else
            return true;
#endif
        }

        public override bool CanHandle(Thing thing)
        {
            return base.CanHandle(thing) && thing.Graphic is Graphic_Collection;
        }

        public override Mesh GetMesh(Thing thing, Rot4 rot)
        {
            Graphic subGraphic = GetSubGraphicFor(thing);
            return subGraphic?.MeshAt(rot) ?? base.GetMesh(thing, rot);
        }

        public override Material GetMaterial(Thing thing, Rot4 rot)
        {
            Graphic subGraphic = GetSubGraphicFor(thing);
            Material originalMaterial = subGraphic?.MatSingleFor(thing);

            if (originalMaterial != null)
            {
                return originalMaterial;
            }
            else
            {
                return base.GetMaterial(thing, rot);
            }
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
                MethodInfo subGraphicForMethodInfo = typeof(Graphic_Collection).GetMethod(
                    "SubGraphicFor",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(Thing) },
                    null
                );

                if (subGraphicForMethodInfo != null)
                {
                    return subGraphicForMethodInfo.Invoke(graphicCollection, new object[] { thing })
                        as Graphic;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}

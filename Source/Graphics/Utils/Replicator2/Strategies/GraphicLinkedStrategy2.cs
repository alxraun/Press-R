using PressR.Graphics.Utils.Replicator2.Core;
using PressR.Graphics.Utils.Replicator2.Interfaces;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Utils.Replicator2.Strategies
{
    public class GraphicLinkedStrategy2 : BaseRenderStrategy
    {
        public override bool CanHandle(Thing thing)
        {
            return thing?.Graphic is Graphic_Linked;
        }

        public override Material GetBaseMaterial(RenderContext context)
        {
            if (context.Thing?.Graphic is Graphic_Linked linkedGraphic)
            {
                return linkedGraphic.MatSingleFor(context.Thing);
            }

            return base.GetBaseMaterial(context);
        }
    }
}

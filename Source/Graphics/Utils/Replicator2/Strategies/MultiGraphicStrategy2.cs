using PressR.Graphics.Utils.Replicator2.Core;
using Verse;

namespace PressR.Graphics.Utils.Replicator2.Strategies
{
    public class MultiGraphicStrategy2 : BaseRenderStrategy
    {
        public override bool CanHandle(Thing thing)
        {
            return thing?.Graphic is Graphic_Multi;
        }
    }
}

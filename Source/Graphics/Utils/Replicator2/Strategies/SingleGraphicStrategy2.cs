using PressR.Graphics.Utils.Replicator2.Core;
using Verse;

namespace PressR.Graphics.Utils.Replicator2.Strategies
{
    public class SingleGraphicStrategy2 : BaseRenderStrategy
    {
        public override bool CanHandle(Thing thing)
        {
            return thing?.Graphic != null
                && (thing.Graphic is Graphic_Single || IsFallbackCandidate(thing));
        }

        private bool IsFallbackCandidate(Thing thing)
        {
            Graphic g = thing?.Graphic;
            return g != null
                && !(thing is Pawn)
                && !(thing is Corpse)
                && !(g is Graphic_Linked)
                && !(g is Graphic_Collection)
                && !(g is Graphic_Multi)
                && !(g is Graphic_RandomRotated);
        }
    }
}

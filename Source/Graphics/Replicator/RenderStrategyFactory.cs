using System.Collections.Generic;
using LudeonTK;
using Verse;

namespace Microtools.Graphics.Replicator
{
    internal static class RenderStrategyFactory
    {
        private static readonly List<IRenderDataReplicatorStrategy> _strategies =
        [
            new PawnStrategy(),
            new SingleGraphicStrategy(),
            new RandomRotatedStrategy(),
            new MultiGraphicStrategy(),
            new CollectionGraphicStrategy(),
        ];

#if DEBUG
        [TweakValue("Microtools.Replicator", 0f, 1f)]
        private static bool EnableStrategyFactory = true;
#endif

        public static IRenderDataReplicatorStrategy GetStrategy(Graphic graphic, Thing thing = null)
        {
#if DEBUG
            if (!EnableStrategyFactory)
                return new SingleGraphicStrategy();
#endif

            if (thing != null)
            {
                foreach (var strategy in _strategies)
                {
                    if (strategy.IsEnabled() && strategy.CanHandle(thing))
                    {
                        return strategy;
                    }
                }
            }

            return new SingleGraphicStrategy();
        }
    }
}

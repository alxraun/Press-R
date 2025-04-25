using System.Collections.Generic;
using System.Linq;
using PressR.Graphics.Utils.Replicator2.Interfaces;
using PressR.Graphics.Utils.Replicator2.Strategies;
using Verse;

namespace PressR.Graphics.Utils.Replicator2.Registry
{
    [StaticConstructorOnStartup]
    public static class RenderStrategyFactory2
    {
        private static readonly List<IRenderDataReplicatorStrategy> _strategies;
        private static readonly IRenderDataReplicatorStrategy _fallbackStrategy;

        static RenderStrategyFactory2()
        {
            _strategies = new List<IRenderDataReplicatorStrategy>
            {
                new PawnStrategy2(),
                new GraphicLinkedStrategy2(),
                new CollectionStrategy2(),
                new MultiGraphicStrategy2(),
                new RandomRotatedStrategy2(),
                new SingleGraphicStrategy2(),
            };

            _fallbackStrategy = _strategies.OfType<SingleGraphicStrategy2>().First();
        }

        public static IRenderDataReplicatorStrategy GetStrategy(Thing thing)
        {
            if (thing?.Graphic == null)
                return _fallbackStrategy;

            foreach (var strategy in _strategies)
            {
                if (strategy.CanHandle(thing))
                {
                    return strategy;
                }
            }

            return _fallbackStrategy;
        }
    }
}

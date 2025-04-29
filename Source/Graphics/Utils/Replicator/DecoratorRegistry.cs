using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PressR.Graphics.Utils.Replicator
{
    internal static class DecoratorRegistry
    {
        private static readonly List<IRenderDataReplicatorDecorator> _decorators =
            new List<IRenderDataReplicatorDecorator>();

        static DecoratorRegistry()
        {
            RegisterDecorator(new EquipmentDecorator());
            RegisterDecorator(new ShelfDecorator());
            RegisterDecorator(new CarriedItemDecorator());
            RegisterDecorator(new BookDecorator());
            RegisterDecorator(new OpenBookDecorator());
        }

        public static void RegisterDecorator(IRenderDataReplicatorDecorator decorator)
        {
            if (decorator != null && !_decorators.Contains(decorator))
            {
                _decorators.Add(decorator);
            }
        }

        public static IEnumerable<IRenderDataReplicatorDecorator> GetApplicableDecorators(
            Thing thing
        )
        {
            return _decorators.Where(d => d.IsEnabled() && d.CanApply(thing));
        }
    }
}

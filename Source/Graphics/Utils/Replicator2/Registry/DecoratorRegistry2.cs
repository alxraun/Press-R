using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PressR.Graphics.Utils.Replicator2.Core;
using PressR.Graphics.Utils.Replicator2.Interfaces;
using Verse;

namespace PressR.Graphics.Utils.Replicator2.Registry
{
    [StaticConstructorOnStartup]
    public static class DecoratorRegistry2
    {
        private static readonly List<IRenderDataDecorator> _sortedDecorators;

        static DecoratorRegistry2()
        {
            _sortedDecorators = LoadAndSortDecorators();
        }

        private static List<IRenderDataDecorator> LoadAndSortDecorators()
        {
            var decorators = new List<IRenderDataDecorator>();
            var types = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(t => !t.IsAbstract && typeof(IRenderDataDecorator).IsAssignableFrom(t));

            foreach (var type in types)
            {
                try
                {
                    if (Activator.CreateInstance(type) is IRenderDataDecorator instance)
                    {
                        decorators.Add(instance);
                    }
                }
                catch (Exception) { }
            }

            return decorators.OrderBy(d => d.Priority).ToList();
        }

        public static IReadOnlyList<IRenderDataDecorator> AllSortedDecorators => _sortedDecorators;
    }
}

using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PressR.Utils
{
    public static class SelectionUtils
    {
        public static IEnumerable<Thing> GetSelectedHaulableThings()
        {
            return Find
                .Selector.SelectedObjectsListForReading.OfType<Thing>()
                .Where(t => t.Spawned && t.def.EverHaulable);
        }

        public static bool HasSelectedHaulableThings()
        {
            return GetSelectedHaulableThings().Any();
        }
    }
}

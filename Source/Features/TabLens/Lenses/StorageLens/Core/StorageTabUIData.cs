using System.Reflection;
using RimWorld;
using Verse;

namespace PressR.Features.TabLens.Lenses.StorageLens.Core
{
    public class StorageTabUIData
    {
        public object QuickSearchWidget { get; }
        public object QuickSearchFilter { get; }
        public PropertyInfo QuickSearchTextProperty { get; }

        public MainTabWindow_Inspect Inspector { get; }
        public Selector Selector { get; }
        public object ThingFilterState { get; }

        public StorageTabUIData(
            object quickSearchWidget,
            object quickSearchFilter,
            PropertyInfo quickSearchTextProperty,
            MainTabWindow_Inspect inspector,
            Selector selector,
            object thingFilterState
        )
        {
            QuickSearchWidget = quickSearchWidget;
            QuickSearchFilter = quickSearchFilter;
            QuickSearchTextProperty = quickSearchTextProperty;
            Inspector = inspector;
            Selector = selector;
            ThingFilterState = thingFilterState;
        }

        public bool IsValid =>
            QuickSearchWidget != null
            && QuickSearchFilter != null
            && QuickSearchTextProperty != null
            && Inspector != null
            && Selector != null
            && ThingFilterState != null;
    }
}

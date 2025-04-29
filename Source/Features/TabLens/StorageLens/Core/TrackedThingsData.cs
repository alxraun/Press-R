using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace PressR.Features.TabLens.StorageLens.Core
{
    public class TrackedThingsData
    {
        public HashSet<Thing> CurrentThings { get; internal set; } = new HashSet<Thing>();

        public Dictionary<Thing, bool> AllowanceStates { get; internal set; } =
            new Dictionary<Thing, bool>();

        public Thing HoveredThing { get; internal set; } = null;

        public void Clear()
        {
            CurrentThings.Clear();
            AllowanceStates.Clear();
            HoveredThing = null;
        }

        public bool GetAllowanceState(Thing thing)
        {
            return AllowanceStates != null
                && AllowanceStates.TryGetValue(thing, out bool isAllowed)
                && isAllowed;
        }
    }
}

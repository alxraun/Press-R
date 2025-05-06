using PressR.Graphics;
using Verse;

namespace PressR.Features.TabLens.StorageLens.Core
{
    public readonly struct StorageLensUpdateContext
    {
        public readonly Map CurrentMap;
        public readonly TrackedThingsData TrackedThingsData;

        public StorageLensUpdateContext(Map currentMap, TrackedThingsData trackedThingsData)
        {
            CurrentMap = currentMap;
            TrackedThingsData = trackedThingsData;
        }
    }
}

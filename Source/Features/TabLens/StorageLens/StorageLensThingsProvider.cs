using System.Collections.Generic;
using Verse;

namespace PressR.Features.TabLens.StorageLens
{
    public class StorageLensThingsProvider
    {
        public HashSet<Thing> GetVisibleItemsInViewRect(Map map)
        {
            if (map == null)
            {
                return new HashSet<Thing>();
            }

            HashSet<Thing> visibleItems = new HashSet<Thing>();
            CellRect viewRect = Find.CameraDriver.CurrentViewRect;

            List<Thing> allThingsOnMap = map.listerThings.AllThings;
            for (int i = 0; i < allThingsOnMap.Count; i++)
            {
                Thing thing = allThingsOnMap[i];
                if (thing.def.category == ThingCategory.Item)
                {
                    IntVec3 position = thing.PositionHeld;
                    if (
                        position.IsValid
                        && viewRect.Contains(position)
                        && !map.fogGrid.IsFogged(position)
                    )
                    {
                        visibleItems.Add(thing);
                    }
                }
            }

            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                IntVec3 pawnPosition = pawn.PositionHeld;

                if (
                    pawnPosition.IsValid
                    && viewRect.Contains(pawnPosition)
                    && !map.fogGrid.IsFogged(pawnPosition)
                )
                {
                    Thing carriedThing = pawn.carryTracker?.CarriedThing;
                    if (carriedThing != null && carriedThing.def.category == ThingCategory.Item)
                    {
                        visibleItems.Add(carriedThing);
                    }
                }
            }
            return visibleItems;
        }
    }
}

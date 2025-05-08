using System.Collections.Generic;
using System.Linq;
using PressR.Utils.Throttler;
using RimWorld;
using Verse;

namespace PressR.Features.TabLens.StorageLens
{
    public class StorageLensThingsProvider
    {
        private const int FullRecalculationIntervalTicks = 5;

        private readonly HashSet<Thing> _visibleItemsCache;
        private Dictionary<Thing, int> _lastListerSnapshotWithCounts;
        private Map _processedMap;
        private int _lastHaulableEverHash;

        private readonly Throttler _fullRecalculationThrottler;

        public StorageLensThingsProvider()
        {
            _visibleItemsCache = new HashSet<Thing>();
            _lastListerSnapshotWithCounts = new Dictionary<Thing, int>();
            _processedMap = null;
            _lastHaulableEverHash = 0;

            _fullRecalculationThrottler = new Throttler(FullRecalculationIntervalTicks, true);
        }

        public HashSet<Thing> GetVisibleItemsInViewRect(Map map)
        {
            if (map == null)
            {
                _visibleItemsCache.Clear();
                _lastListerSnapshotWithCounts.Clear();
                _processedMap = null;
                _lastHaulableEverHash = 0;
                return new HashSet<Thing>();
            }

            if (_processedMap != map)
            {
                _visibleItemsCache.Clear();
                _lastListerSnapshotWithCounts.Clear();
                _fullRecalculationThrottler.ForceNextExecutionAndResetInterval();
                _processedMap = map;
                _lastHaulableEverHash = 0;
            }

            CellRect currentViewRect = Find.CameraDriver.CurrentViewRect;

            if (_fullRecalculationThrottler.ShouldExecute())
            {
                PerformFullRecalculation(map, currentViewRect);
            }
            else
            {
                PerformDeltaUpdate(map, currentViewRect);
            }

            return new HashSet<Thing>(_visibleItemsCache);
        }

        private void PerformFullRecalculation(Map map, CellRect currentViewRect)
        {
            _visibleItemsCache.Clear();
            _lastListerSnapshotWithCounts.Clear();

            List<Thing> storableThingsOnGround = new List<Thing>();
            List<Thing> haulableThings = map.listerThings.ThingsInGroup(
                ThingRequestGroup.HaulableEver
            );
            foreach (Thing t in haulableThings)
            {
                if (t.def.EverStorable(false))
                {
                    storableThingsOnGround.Add(t);
                }
            }

            foreach (Thing thing in storableThingsOnGround)
            {
                _lastListerSnapshotWithCounts[thing] = thing.stackCount;
                TryProcessSingleThingOnGround(thing, currentViewRect, map, _visibleItemsCache);
            }

            _lastHaulableEverHash = map.listerThings.StateHashOfGroup(
                ThingRequestGroup.HaulableEver
            );

            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                TryProcessCarriedThingByPawn(pawns[i], currentViewRect, map, _visibleItemsCache);
            }
        }

        private void PerformDeltaUpdate(Map map, CellRect currentViewRect)
        {
            bool changed = false;
            int currentHaulableEverHash = map.listerThings.StateHashOfGroup(
                ThingRequestGroup.HaulableEver
            );

            if (currentHaulableEverHash != _lastHaulableEverHash)
            {
                changed = true;
            }
            else
            {
                foreach (KeyValuePair<Thing, int> entry in _lastListerSnapshotWithCounts)
                {
                    if (entry.Key.stackCount != entry.Value)
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (changed)
            {
                PerformFullRecalculation(map, currentViewRect);
                _fullRecalculationThrottler.ForceNextExecutionAndResetInterval();
            }
        }

        private void TryProcessSingleThingOnGround(
            Thing thing,
            CellRect viewRect,
            Map map,
            HashSet<Thing> targetSet
        )
        {
            IntVec3 position = thing.PositionHeld;
            if (position.IsValid && viewRect.Contains(position) && !map.fogGrid.IsFogged(position))
            {
                targetSet.Add(thing);
            }
        }

        private void TryProcessCarriedThingByPawn(
            Pawn pawn,
            CellRect viewRect,
            Map map,
            HashSet<Thing> targetSet
        )
        {
            IntVec3 pawnPosition = pawn.PositionHeld;
            if (
                pawnPosition.IsValid
                && viewRect.Contains(pawnPosition)
                && !map.fogGrid.IsFogged(pawnPosition)
            )
            {
                Thing carriedThing = pawn.carryTracker?.CarriedThing;
                if (carriedThing != null && carriedThing.def.EverStorable(false))
                {
                    targetSet.Add(carriedThing);
                }
            }
        }
    }
}

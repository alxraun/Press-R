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
        private IStoreSettingsParent _processedStorageParent;
        private int _lastHaulableEverHash;

        private readonly Throttler _fullRecalculationThrottler;

        public StorageLensThingsProvider()
        {
            _visibleItemsCache = new HashSet<Thing>();
            _lastListerSnapshotWithCounts = new Dictionary<Thing, int>();
            _processedMap = null;
            _processedStorageParent = null;
            _lastHaulableEverHash = 0;

            _fullRecalculationThrottler = new Throttler(FullRecalculationIntervalTicks, true);
        }

        public void UpdateVisibleAllowedByParentHaulableThingsInSet(
            HashSet<Thing> targetSet,
            Map map,
            IStoreSettingsParent storageParent
        )
        {
            if (map == null)
            {
                targetSet.Clear();
                _visibleItemsCache.Clear();
                _lastListerSnapshotWithCounts.Clear();
                _processedMap = null;
                _processedStorageParent = null;
                _lastHaulableEverHash = 0;
                return;
            }

            if (_processedMap != map || _processedStorageParent != storageParent)
            {
                _visibleItemsCache.Clear();
                _lastListerSnapshotWithCounts.Clear();
                _fullRecalculationThrottler.ForceNextExecutionAndResetInterval();
                _processedMap = map;
                _processedStorageParent = storageParent;
                _lastHaulableEverHash = 0;
            }

            CellRect currentViewRect = Find.CameraDriver.CurrentViewRect;
            ThingFilter parentSettingsFilter = storageParent?.GetParentStoreSettings()?.filter;

            if (_fullRecalculationThrottler.ShouldExecute())
            {
                PerformFullRecalculation(map, currentViewRect, parentSettingsFilter);
            }
            else
            {
                PerformDeltaUpdate(map, currentViewRect, parentSettingsFilter);
            }

            targetSet.Clear();
            foreach (var thing in _visibleItemsCache)
            {
                targetSet.Add(thing);
            }
        }

        private void PerformFullRecalculation(
            Map map,
            CellRect currentViewRect,
            ThingFilter parentSettingsFilter
        )
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
                    if (parentSettingsFilter == null || parentSettingsFilter.Allows(t.def))
                    {
                        storableThingsOnGround.Add(t);
                    }
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
                TryProcessCarriedThingByPawn(
                    pawns[i],
                    currentViewRect,
                    map,
                    _visibleItemsCache,
                    parentSettingsFilter
                );
            }
        }

        private void PerformDeltaUpdate(
            Map map,
            CellRect currentViewRect,
            ThingFilter parentSettingsFilter
        )
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
                    if (
                        entry.Key == null
                        || entry.Key.Destroyed
                        || entry.Key.stackCount != entry.Value
                    )
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (changed)
            {
                PerformFullRecalculation(map, currentViewRect, parentSettingsFilter);
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
            HashSet<Thing> targetSet,
            ThingFilter parentSettingsFilter
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
                if (
                    carriedThing != null
                    && carriedThing.def != null
                    && carriedThing.def.EverStorable(false)
                )
                {
                    if (
                        parentSettingsFilter == null
                        || parentSettingsFilter.Allows(carriedThing.def)
                    )
                    {
                        targetSet.Add(carriedThing);
                    }
                }
            }
        }
    }
}

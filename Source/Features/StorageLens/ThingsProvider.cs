using System.Collections.Generic;
using Verse;

namespace Microtools.Features.StorageLens
{
    public class ThingsProvider(State state)
    {
        private const int FullRecalculationIntervalTicks = 5;

        private readonly State _state = state;
        private readonly Throttler _fullRecalculationThrottler = new(
            FullRecalculationIntervalTicks,
            true
        );

        private readonly HashSet<Thing> _visibleItemsCache = [];
        private Dictionary<Thing, int> _lastListerSnapshotWithCounts = [];
        private int _lastHaulableEverHash;

        public void Update()
        {
            var map = _state.CurrentMap;
            var storageParent = _state.SelectedStorage;

            if (map == null)
            {
                ClearCache();
                _state.StorableForSelectedStorageInView.Clear();
                return;
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

            _state.StorableForSelectedStorageInView.Clear();
            foreach (var thing in _visibleItemsCache)
            {
                _state.StorableForSelectedStorageInView.Add(thing);
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

            List<Thing> haulableThings = map.listerThings.ThingsInGroup(
                ThingRequestGroup.HaulableEver
            );

            foreach (Thing t in haulableThings)
            {
                if (!t.def.EverStorable(false))
                {
                    continue;
                }

                if (parentSettingsFilter != null && !parentSettingsFilter.Allows(t.def))
                {
                    continue;
                }

                _lastListerSnapshotWithCounts[t] = t.stackCount;
                TryProcessSingleThingOnGround(t, currentViewRect, map, _visibleItemsCache);
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

        private static void TryProcessSingleThingOnGround(
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

        private static void TryProcessCarriedThingByPawn(
            Pawn pawn,
            CellRect viewRect,
            Map map,
            HashSet<Thing> targetSet,
            ThingFilter parentSettingsFilter
        )
        {
            IntVec3 pawnPosition = pawn.PositionHeld;
            if (
                !pawnPosition.IsValid
                || !viewRect.Contains(pawnPosition)
                || map.fogGrid.IsFogged(pawnPosition)
            )
            {
                return;
            }

            Thing carriedThing = pawn.carryTracker?.CarriedThing;
            if (
                carriedThing == null
                || carriedThing.def == null
                || !carriedThing.def.EverStorable(false)
            )
            {
                return;
            }

            if (parentSettingsFilter == null || parentSettingsFilter.Allows(carriedThing.def))
            {
                targetSet.Add(carriedThing);
            }
        }

        private void ClearCache()
        {
            _visibleItemsCache.Clear();
            _lastListerSnapshotWithCounts.Clear();
            _lastHaulableEverHash = 0;
        }
    }
}

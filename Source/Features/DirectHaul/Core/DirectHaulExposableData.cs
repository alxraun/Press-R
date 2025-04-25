using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PressR.Features.DirectHaul.Core
{
    public enum DirectHaulStatus : byte
    {
        None,
        Pending,
        Held,
    }

    public class DirectHaulExposableData : IExposable
    {
        private class ThingState : IExposable
        {
            public LocalTargetInfo TargetCell = LocalTargetInfo.Invalid;
            public DirectHaulStatus Status = DirectHaulStatus.None;
            public bool IsHighPriority = false;

            public ThingState() { }

            public ThingState(LocalTargetInfo targetCell, bool isHighPriority = false)
            {
                TargetCell = targetCell;
                Status = DirectHaulStatus.Pending;
                IsHighPriority = isHighPriority;
            }

            public ThingState(DirectHaulStatus status)
            {
                TargetCell = LocalTargetInfo.Invalid;
                Status = status;
                IsHighPriority = false;
            }

            public void ExposeData()
            {
                Scribe_TargetInfo.Look(ref TargetCell, "targetCell");
                Scribe_Values.Look(ref Status, "status", defaultValue: DirectHaulStatus.None);
                Scribe_Values.Look(ref IsHighPriority, "isHighPriority", defaultValue: false);
            }
        }

        private Dictionary<Thing, ThingState> _trackedThings = new Dictionary<Thing, ThingState>();
        private Map _mapReference;

        private List<Thing> _trackedThingsKeysWorkingList = null;
        private List<ThingState> _trackedThingsValuesWorkingList = null;

        public DirectHaulExposableData(Map map)
        {
            _mapReference = map;
        }

        public DirectHaulExposableData() { }

        public DirectHaulStatus GetStatusForThing(Thing thing)
        {
            _trackedThings ??= new Dictionary<Thing, ThingState>();
            if (thing != null && _trackedThings.TryGetValue(thing, out ThingState state))
            {
                return state.Status;
            }
            return DirectHaulStatus.None;
        }

        public bool TryGetInfoFromPending(
            Thing thing,
            out LocalTargetInfo targetCell,
            out bool isHighPriority
        )
        {
            _trackedThings ??= new Dictionary<Thing, ThingState>();
            if (
                thing != null
                && _trackedThings.TryGetValue(thing, out ThingState state)
                && state.Status == DirectHaulStatus.Pending
            )
            {
                targetCell = state.TargetCell;
                isHighPriority = state.IsHighPriority;
                return targetCell.IsValid;
            }
            targetCell = LocalTargetInfo.Invalid;
            isHighPriority = false;
            return false;
        }

        public void MarkThingAsPending(
            Thing thing,
            LocalTargetInfo targetCell,
            bool isHighPriority = false
        )
        {
            _trackedThings ??= new Dictionary<Thing, ThingState>();
            if (thing == null)
            {
                return;
            }
            if (!targetCell.IsValid)
            {
                return;
            }

            _trackedThings[thing] = new ThingState(targetCell, isHighPriority);
        }

        public void MarkThingAsHeld(Thing thing)
        {
            if (!_trackedThings.ContainsKey(thing))
            {
                return;
            }
            _trackedThings[thing].Status = DirectHaulStatus.Held;
            _trackedThings[thing].TargetCell = LocalTargetInfo.Invalid;
        }

        public void SetThingAsHeldAt(Thing placedThing, IntVec3 targetCell, bool jobWasHighPriority)
        {
            if (placedThing == null || !targetCell.IsValid)
                return;

            if (_trackedThings.TryGetValue(placedThing, out ThingState existingState))
            {
                existingState.Status = DirectHaulStatus.Held;
                existingState.TargetCell = targetCell;
                existingState.IsHighPriority = jobWasHighPriority;
            }
            else
            {
                var newState = new ThingState(targetCell, jobWasHighPriority)
                {
                    Status = DirectHaulStatus.Held,
                };
                _trackedThings.Add(placedThing, newState);
            }
        }

        public bool IsThingInTracking(Thing thing)
        {
            return GetStatusForThing(thing) != DirectHaulStatus.None;
        }

        public void RemoveThingFromTracking(Thing thing)
        {
            _trackedThings ??= new Dictionary<Thing, ThingState>();
            if (thing != null)
            {
                _trackedThings.Remove(thing);
            }
        }

        public IEnumerable<Thing> GetThingsWithStatus(DirectHaulStatus status)
        {
            _trackedThings ??= new Dictionary<Thing, ThingState>();
            return _trackedThings
                .Where(kvp => kvp.Key != null && kvp.Value?.Status == status)
                .Select(kvp => kvp.Key);
        }

        public Dictionary<Thing, LocalTargetInfo> GetPendingThingsAndTargets()
        {
            _trackedThings ??= new Dictionary<Thing, ThingState>();
            return _trackedThings
                .Where(kvp =>
                    kvp.Key != null
                    && !kvp.Key.Destroyed
                    && kvp.Value?.Status == DirectHaulStatus.Pending
                    && kvp.Value.TargetCell.IsValid
                )
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.TargetCell);
        }

        public HashSet<IntVec3> GetPendingTargetCells()
        {
            _trackedThings ??= new Dictionary<Thing, ThingState>();
            return _trackedThings
                .Where(kvp =>
                    kvp.Value?.Status == DirectHaulStatus.Pending
                    && kvp.Value.TargetCell.IsValid
                    && kvp.Key != null
                    && !kvp.Key.Destroyed
                )
                .Select(kvp => kvp.Value.TargetCell.Cell)
                .ToHashSet();
        }

        public bool IsCellPendingTarget(IntVec3 cell, Thing excludeThing = null)
        {
            _trackedThings ??= new Dictionary<Thing, ThingState>();
            return _trackedThings.Any(kvp =>
                kvp.Key != excludeThing
                && kvp.Value.Status == DirectHaulStatus.Pending
                && kvp.Value.TargetCell.IsValid
                && kvp.Value.TargetCell.Cell == cell
            );
        }

        public IEnumerable<Thing> GetAllTrackedThings()
        {
            _trackedThings ??= new Dictionary<Thing, ThingState>();

            return _trackedThings.Keys.ToList();
        }

        public void CleanupData()
        {
            _trackedThings ??= new Dictionary<Thing, ThingState>();
            int initialCount = _trackedThings.Count;
            _trackedThings = _trackedThings
                .Where(kvp => kvp.Key != null && !kvp.Key.Destroyed)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            int removedCount = initialCount - _trackedThings.Count;
        }

        private void CleanupInvalidEntries()
        {
            if (_trackedThings == null || _trackedThings.Count == 0)
                return;

            int initialCount = _trackedThings.Count;
            List<Thing> keysToRemove = _trackedThings
                .Where(kvp => kvp.Key == null || kvp.Key.Destroyed)
                .Select(kvp => kvp.Key)
                .ToList();

            if (keysToRemove.Any())
            {
                foreach (Thing key in keysToRemove)
                {
                    _trackedThings.Remove(key);
                }
                int removedCount = keysToRemove.Count;
            }
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref _mapReference, "mapReference");

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                CleanupInvalidEntries();

                _trackedThingsKeysWorkingList = _trackedThings.Keys.ToList();
                _trackedThingsValuesWorkingList = _trackedThings.Values.ToList();
            }

            Scribe_Collections.Look(
                ref _trackedThingsKeysWorkingList,
                "trackedThings_keys_v2",
                LookMode.Reference
            );
            Scribe_Collections.Look(
                ref _trackedThingsValuesWorkingList,
                "trackedThings_values_v2",
                LookMode.Deep
            );

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                _trackedThings = new Dictionary<Thing, ThingState>();

                if (
                    _trackedThingsKeysWorkingList == null
                    || _trackedThingsValuesWorkingList == null
                ) { }
                else if (
                    _trackedThingsKeysWorkingList.Count != _trackedThingsValuesWorkingList.Count
                ) { }
                else
                {
                    var validPairs = _trackedThingsKeysWorkingList
                        .Zip(
                            _trackedThingsValuesWorkingList,
                            (key, value) => new { Key = key, Value = value }
                        )
                        .Where(pair =>
                            pair.Key != null && !pair.Key.Destroyed && pair.Value != null
                        )
                        .ToList();

                    int totalLoaded = _trackedThingsKeysWorkingList.Count;
                    int validLoaded = validPairs.Count;

                    var groupedByKey = validPairs.GroupBy(pair => pair.Key);

                    foreach (var group in groupedByKey)
                    {
                        _trackedThings.Add(group.Key, group.First().Value);
                    }
                }

                _trackedThingsKeysWorkingList = null;
                _trackedThingsValuesWorkingList = null;
            }
        }
    }
}

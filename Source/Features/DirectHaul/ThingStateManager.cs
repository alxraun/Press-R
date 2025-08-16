using System;
using System.Collections.Generic;
using Verse;

namespace PressR.Features.DirectHaul
{
    public class ThingStateManager(State state)
    {
        private readonly State _state = state;

        private readonly HashSet<Thing> _allPendingThings = [];
        private readonly HashSet<Thing> _allHeldThings = [];
        private readonly HashSet<IntVec3> _pendingTargetCells = [];

        public event Action<Thing, LocalTargetInfo> PendingAdded;
        public event Action<Thing, LocalTargetInfo, LocalTargetInfo> PendingTargetChanged;
        public event Action<Thing, LocalTargetInfo> PendingRemoved;
        public event Action<Thing> HeldAdded;
        public event Action<Thing> HeldRemoved;
        public event Action CachesRecalculated;

        public IReadOnlyCollection<Thing> AllPendingThings => _allPendingThings;
        public IReadOnlyCollection<Thing> AllHeldThings => _allHeldThings;
        public IReadOnlyCollection<Thing> AllTrackedThings => _state.TrackedThings.Keys;
        public IReadOnlyCollection<IntVec3> PendingTargetCells => _pendingTargetCells;

        public IEnumerable<Thing> PendingSelectedThings
        {
            get
            {
                foreach (var thing in _state.SelectedThings)
                {
                    if (_allPendingThings.Contains(thing))
                    {
                        yield return thing;
                    }
                }
            }
        }

        public IEnumerable<Thing> HeldSelectedThings
        {
            get
            {
                foreach (var thing in _state.SelectedThings)
                {
                    if (_allHeldThings.Contains(thing))
                    {
                        yield return thing;
                    }
                }
            }
        }

        public IEnumerable<Thing> UntrackedSelectedThings
        {
            get
            {
                foreach (var thing in _state.SelectedThings)
                {
                    if (!_state.TrackedThings.ContainsKey(thing))
                    {
                        yield return thing;
                    }
                }
            }
        }

        public DirectHaulStatus GetStatus(Thing thing)
        {
            return _state.TrackedThings.TryGetValue(thing, out var info)
                ? info.Status
                : DirectHaulStatus.None;
        }

        public LocalTargetInfo GetTarget(Thing thing)
        {
            if (_state.TrackedThings.TryGetValue(thing, out var info))
            {
                return info.TargetCell;
            }

            return LocalTargetInfo.Invalid;
        }

        internal void SetPending(Thing thing, LocalTargetInfo targetCell)
        {
            var hadInfo = _state.TrackedThings.TryGetValue(thing, out var oldInfo);
            var previousStatus = hadInfo ? oldInfo.Status : DirectHaulStatus.None;
            var previousTarget = hadInfo ? oldInfo.TargetCell : LocalTargetInfo.Invalid;
            if (hadInfo)
            {
                if (oldInfo.Status == DirectHaulStatus.Pending)
                {
                    _pendingTargetCells.Remove(oldInfo.TargetCell.Cell);
                }
                else if (oldInfo.Status == DirectHaulStatus.Held)
                {
                    _allHeldThings.Remove(thing);
                    _allPendingThings.Add(thing);
                }
            }
            else
            {
                _allPendingThings.Add(thing);
            }

            _state.TrackedThings[thing] = (DirectHaulStatus.Pending, targetCell);
            _pendingTargetCells.Add(targetCell.Cell);

            if (hadInfo)
            {
                if (previousStatus == DirectHaulStatus.Pending)
                {
                    if (previousTarget != targetCell)
                    {
                        PendingTargetChanged?.Invoke(thing, previousTarget, targetCell);
                    }
                }
                else if (previousStatus == DirectHaulStatus.Held)
                {
                    HeldRemoved?.Invoke(thing);
                    PendingAdded?.Invoke(thing, targetCell);
                }
            }
            else
            {
                PendingAdded?.Invoke(thing, targetCell);
            }
        }

        internal void SetHeld(Thing thing)
        {
            if (
                !_state.TrackedThings.TryGetValue(thing, out var info)
                || info.Status == DirectHaulStatus.Held
            )
            {
                return;
            }

            var previousStatus = info.Status;
            var previousTarget = info.TargetCell;

            if (info.Status == DirectHaulStatus.Pending)
            {
                _allPendingThings.Remove(thing);
                _pendingTargetCells.Remove(info.TargetCell.Cell);
                _allHeldThings.Add(thing);
            }

            _state.TrackedThings[thing] = (DirectHaulStatus.Held, LocalTargetInfo.Invalid);

            if (previousStatus == DirectHaulStatus.Pending)
            {
                PendingRemoved?.Invoke(thing, previousTarget);
            }
            HeldAdded?.Invoke(thing);
        }

        internal void Remove(Thing thing)
        {
            if (!_state.TrackedThings.Remove(thing, out var info))
            {
                return;
            }

            var previousStatus = info.Status;
            var previousTarget = info.TargetCell;

            if (info.Status == DirectHaulStatus.Pending)
            {
                _allPendingThings.Remove(thing);
                _pendingTargetCells.Remove(info.TargetCell.Cell);
                PendingRemoved?.Invoke(thing, previousTarget);
            }
            else if (info.Status == DirectHaulStatus.Held)
            {
                _allHeldThings.Remove(thing);
                HeldRemoved?.Invoke(thing);
            }
        }

        internal void Remove(List<Thing> things)
        {
            foreach (var thing in things)
            {
                Remove(thing);
            }
        }

        internal void RecalculateCaches()
        {
            _allPendingThings.Clear();
            _allHeldThings.Clear();
            _pendingTargetCells.Clear();
            foreach (var (thing, (status, targetCell)) in _state.TrackedThings)
            {
                if (status == DirectHaulStatus.Pending)
                {
                    _allPendingThings.Add(thing);
                    _pendingTargetCells.Add(targetCell.Cell);
                }
                else if (status == DirectHaulStatus.Held)
                {
                    _allHeldThings.Add(thing);
                }
            }

            CachesRecalculated?.Invoke();
        }
    }
}
